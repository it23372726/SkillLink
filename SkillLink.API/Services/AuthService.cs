using SkillLink.API.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Net;
using SkillLink.API.Services.Abstractions;
using SkillLink.API.Repositories.Abstractions;
using SkillLink.API.Dtos.Auth;

namespace SkillLink.API.Services
{
    public class AuthService : IAuthService
    {
        private readonly IConfiguration _config;
        private readonly EmailService _email;
        private readonly IAuthRepository _repo;

        public AuthService(IConfiguration config, EmailService email, IAuthRepository repo)
        {
            _config = config;
            _email = email;
            _repo = repo;
        }



        public Task<PublicUserDto?> GetPublicUserAsync(int userId)
            => _repo.GetPublicByIdAsync(userId);

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

            var dbUser = _repo.GetUserById(userId);
            if (dbUser == null) return null;

            return new User
            {
                UserId = dbUser.UserId,
                FullName = dbUser.FullName,
                Email = dbUser.Email,
                Role = dbUser.Role
            };
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

        // ------------------- Get User by Id -------------------
        public User? GetUserById(int id) => _repo.GetUserById(id);

        // ------------------- Register -------------------
        public void Register(RegisterRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.FullName) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
            {
                throw new ArgumentException("Full name, email and password are required.");
            }

            if (IsDisposableEmail(req.Email))
                throw new InvalidOperationException("Disposable or temporary emails are not allowed.");

            if (_repo.EmailExists(req.Email))
                throw new InvalidOperationException("Email already exists.");

            var token = CreateToken();
            var expires = DateTime.UtcNow.AddHours(24);
            var hash = HashPassword(req.Password);

            var newUserId = _repo.CreateUser(req, hash, token, expires);

            // Fire & forget email (do not fail registration if email fails)
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
            var ok = _repo.VerifyEmailByToken(token, out _);
            return ok;
        }

        // ------------------- Login -------------------
        public string? Login(LoginRequest req)
        {
            var dbUser = _repo.GetUserByEmail(req.Email);
            if (dbUser == null) return null;

            if (!dbUser.IsActive || !dbUser.EmailVerified) return null;

            if (!VerifyPassword(req.Password, dbUser.PasswordHash ?? "")) return null;

            return GenerateJwtToken(dbUser);
        }

        // ------------------- Profile -------------------
        public User? GetUserProfile(int userId) => _repo.GetProfile(userId);

        public bool UpdateUserProfile(int userId, UpdateProfileRequest request)
        {
            try
            {
                return _repo.UpdateProfile(userId, request.FullName, request.Bio, request.Location);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating profile: {ex.Message}");
                return false;
            }
        }

        public bool UpdateTeachMode(int userId, bool readyToTeach)
        {
            var role = readyToTeach ? "Tutor" : "Learner";
            return _repo.UpdateTeachMode(userId, readyToTeach, role);
        }

        public bool SetActive(int userId, bool isActive) => _repo.SetActive(userId, isActive);

        public void DeleteUserFromDB(int id) => _repo.DeleteUserWithRules(id);

        public bool UpdateProfilePicture(int userId, string? path) => _repo.UpdateProfilePicture(userId, path);
    }
}
