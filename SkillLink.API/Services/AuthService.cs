using SkillLink.API.Models;
using MySql.Data.MySqlClient;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;

namespace SkillLink.API.Services
{
    public class AuthService
    {
        private readonly DbHelper _dbHelper;
        private readonly IConfiguration _config;
        private readonly EmailService _email;

        public AuthService(DbHelper dbHelper, IConfiguration config, EmailService email)
        {
            _dbHelper = dbHelper;
            _config = config;
            _email = email;
        }

        // ------------------- Current User -------------------
        public User? CurrentUser(ClaimsPrincipal user)
        {
            if (user.Identity == null || !user.Identity.IsAuthenticated)
                return null;

            var id =
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (!int.TryParse(id, out int userId))
                return null;

            var dbUser = GetUserById(userId);
            if (dbUser == null)
                return null;

            return new User
            {
                UserId = dbUser.UserId,
                FullName = dbUser.FullName,
                Email = dbUser.Email,
                Role = dbUser.Role
            };
        }

        // ------------------- Get User by Id -------------------
        public User? GetUserById(int id)
        {
            User? data = null;
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM Users WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", id);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                data = new User
                {
                    UserId = reader.GetInt32("UserId"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email"),
                    PasswordHash = reader.GetString("PasswordHash"),
                    Role = reader.GetString("Role"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Bio = reader.IsDBNull(reader.GetOrdinal("Bio")) ? null : reader.GetString("Bio"),
                    Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString("Location"),
                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString("ProfilePicture")
                };
            }
            return data;
        }

        // ------------------- Utilities -------------------
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private bool VerifyPassword(string password, string storedHash) =>
            HashPassword(password) == storedHash;

        private string GenerateJwtToken(User user)
        {
            var claims = new[]
            {
                // Make NameIdentifier resolvable for CurrentUser()
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Email, user.Email)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expireMinutes = Convert.ToDouble(_config["Jwt:ExpireMinutes"]);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expireMinutes),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private string CreateToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(32);
            return Convert.ToBase64String(bytes)
                        .Replace("+", "-")
                        .Replace("/", "_")
                        .Replace("=", "");
        }

        // Reject common disposable domains (lightweight)
        private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
        {
            "mailinator.com", "tempmail.com", "10minutemail.com", "guerrillamail.com",
            "trashmail.com", "yopmail.com", "getnada.com"
        };

        private bool IsDisposableEmail(string email)
        {
            try
            {
                var parts = email.Split('@');
                if (parts.Length != 2) return true;
                var domain = parts[1];
                return DisposableDomains.Contains(domain);
            }
            catch { return true; }
        }

        // ------------------- Register -------------------
        public void Register(RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FullName) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
            {
                throw new ArgumentException("Full name, email and password are required.");
            }

            // Optional: block disposable email domains
            if (IsDisposableEmail(req.Email))
                throw new InvalidOperationException("Disposable or temporary emails are not allowed.");

            using var conn = _dbHelper.GetConnection();
            conn.Open();

            // Email duplication check
            using (var check = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Email=@e", conn))
            {
                check.Parameters.AddWithValue("@e", req.Email);
                var count = Convert.ToInt32(check.ExecuteScalar());
                if (count > 0) throw new InvalidOperationException("Email already exists.");
            }

            // Generate email verification token
            var token = CreateToken();
            var expires = DateTime.UtcNow.AddHours(24);

            using var cmd = new MySqlCommand(
                @"INSERT INTO Users 
                  (FullName, Email, PasswordHash, Role, ProfilePicture, CreatedAt, 
                   IsActive, ReadyToTeach, EmailVerified, EmailVerificationToken, EmailVerificationExpires)
                  VALUES (@FullName, @Email, @PasswordHash, @Role, @ProfilePicture, NOW(), 
                          1, 0, 0, @Token, @Expires)", conn);

            cmd.Parameters.AddWithValue("@FullName", req.FullName);
            cmd.Parameters.AddWithValue("@Email", req.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", HashPassword(req.Password));
            cmd.Parameters.AddWithValue("@Role", string.IsNullOrWhiteSpace(req.Role) ? "Learner" : req.Role);
            cmd.Parameters.AddWithValue("@ProfilePicture", (object?)req.ProfilePicturePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Token", token);
            cmd.Parameters.AddWithValue("@Expires", expires);

            cmd.ExecuteNonQuery();

            try
            {
                var apiBase = _config["Api:BaseUrl"] ?? "http://localhost:5159";
                var verifyUrl = $"{apiBase}/api/auth/verify-email?token={Uri.EscapeDataString(token)}";

                var html = $@"
                    <h2>Verify your email</h2>
                    <p>Hi {WebUtility.HtmlEncode(req.FullName)},</p>
                    <p>Thanks for registering at SkillLink. Please verify your email by clicking the link below:</p>
                    <p><a href=""{verifyUrl}"">Verify my email</a></p>
                    <p>This link will expire in 24 hours.</p>";

                _email.SendAsync(req.Email, "Verify your SkillLink email", html).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email send failed: {ex.Message}");
            }
        }

        // ------------------- Verify Email -------------------
        public bool VerifyEmailByToken(string token)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            int? userId = null;
            DateTime expires;

            using (var get = new MySqlCommand(
                @"SELECT UserId, EmailVerificationExpires 
                  FROM Users 
                  WHERE EmailVerificationToken=@t AND EmailVerified=0", conn))
            {
                get.Parameters.AddWithValue("@t", token);
                using var r = get.ExecuteReader();
                if (!r.Read()) return false;

                userId = r.GetInt32("UserId");
                expires = r.GetDateTime("EmailVerificationExpires");
            }

            if (expires < DateTime.UtcNow) return false;
            if (userId is null) return false;

            using var upd = new MySqlCommand(
                @"UPDATE Users 
                  SET EmailVerified=1, EmailVerificationToken=NULL, EmailVerificationExpires=NULL
                  WHERE UserId=@id", conn);
            upd.Parameters.AddWithValue("@id", userId.Value);
            return upd.ExecuteNonQuery() > 0;
        }

        // ------------------- Login (requires verified + active) -------------------
        public string? Login(LoginRequest req)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand("SELECT * FROM Users WHERE Email=@e", conn);
            cmd.Parameters.AddWithValue("@e", req.Email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var isActive = reader.GetBoolean(reader.GetOrdinal("IsActive"));
            var isVerified = reader.GetBoolean(reader.GetOrdinal("EmailVerified"));
            if (!isActive || !isVerified) return null;

            var user = new User
            {
                UserId = reader.GetInt32("UserId"),
                FullName = reader.GetString("FullName"),
                Email = reader.GetString("Email"),
                PasswordHash = reader.GetString("PasswordHash"),
                Role = reader.GetString("Role"),
                ReadyToTeach = reader.GetBoolean(reader.GetOrdinal("ReadyToTeach")),
                EmailVerified = isVerified
            };

            if (!VerifyPassword(req.Password, user.PasswordHash)) return null;
            return GenerateJwtToken(user);
        }

        // ------------------- Profile -------------------
        public User? GetUserProfile(int userId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                @"SELECT UserId, FullName, Email, Role, CreatedAt, Bio, Location, 
                         ProfilePicture, ReadyToTeach, IsActive, EmailVerified
                  FROM Users WHERE UserId=@userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new User
            {
                UserId = reader.GetInt32("UserId"),
                FullName = reader.GetString("FullName"),
                Email = reader.GetString("Email"),
                Role = reader.GetString("Role"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                Bio = reader.IsDBNull(reader.GetOrdinal("Bio")) ? null : reader.GetString("Bio"),
                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString("Location"),
                ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString("ProfilePicture"),
                ReadyToTeach = reader.GetBoolean(reader.GetOrdinal("ReadyToTeach")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                EmailVerified = reader.GetBoolean(reader.GetOrdinal("EmailVerified"))
            };
        }

        public bool UpdateUserProfile(int userId, UpdateProfileRequest request)
        {
            try
            {
                using var conn = _dbHelper.GetConnection();
                conn.Open();
                using var cmd = new MySqlCommand(
                    @"UPDATE Users 
                      SET FullName=@fullName, Bio=@bio, Location=@location 
                      WHERE UserId=@userId", conn);

                cmd.Parameters.AddWithValue("@fullName", request.FullName);
                cmd.Parameters.AddWithValue("@bio", (object?)request.Bio ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@location", (object?)request.Location ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@userId", userId);

                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }

        // ------------------- Tutor toggle (also role) -------------------
        public bool UpdateTeachMode(int userId, bool readyToTeach)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                @"UPDATE Users 
                  SET ReadyToTeach=@r, Role=@role 
                  WHERE UserId=@id", conn);

            cmd.Parameters.AddWithValue("@r", readyToTeach ? 1 : 0);
            cmd.Parameters.AddWithValue("@role", readyToTeach ? "Tutor" : "Learner");
            cmd.Parameters.AddWithValue("@id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // ------------------- Activate/Deactivate -------------------
        public bool SetActive(int userId, bool isActive)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(
                @"UPDATE Users 
                  SET IsActive=@isActive 
                  WHERE UserId=@id", conn);

            cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        // --- Delete User ---
        public void DeleteUserFromDB(int id){
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM Users WHERE UserId = @id" ,conn);
            cmd.Parameters.AddWithValue("@id" , id);
            cmd.ExecuteNonQuery();
        }
    }
}
