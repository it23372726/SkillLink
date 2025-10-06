using Microsoft.Data.SqlClient;
using SkillLink.API.Models;
using SkillLink.API.Data;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class AuthRepository : IAuthRepository
    {
        private readonly DbHelper _db;

        public AuthRepository(DbHelper db)
        {
            _db = db;
        }

        public User? GetUserById(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE UserId = @userId", conn);
            cmd.Parameters.AddWithValue("@userId", id);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapUser(reader) : null;
        }

        public User? GetUserByEmail(string email)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Users WHERE Email=@e", conn);
            cmd.Parameters.AddWithValue("@e", email);
            using var reader = cmd.ExecuteReader();
            return reader.Read() ? MapUser(reader) : null;
        }

        public bool EmailExists(string email)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var check = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Email=@e", conn);
            check.Parameters.AddWithValue("@e", email);
            var count = Convert.ToInt32(check.ExecuteScalar());
            return count > 0;
        }

        public int CreateUser(RegisterRequest req, string passwordHash, string emailVerificationToken, DateTime expiresUtc)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                @"INSERT INTO Users 
                  (FullName, Email, PasswordHash, Role, ProfilePicture, CreatedAt, 
                   IsActive, ReadyToTeach, EmailVerified, EmailVerificationToken, EmailVerificationExpires)
                  VALUES (@FullName, @Email, @PasswordHash, @Role, @ProfilePicture, GETDATE(), 
                          1, 0, 0, @Token, @Expires);
                  SELECT CAST(SCOPE_IDENTITY() as int);", conn);

            cmd.Parameters.AddWithValue("@FullName", req.FullName);
            cmd.Parameters.AddWithValue("@Email", req.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", passwordHash);
            cmd.Parameters.AddWithValue("@Role", string.IsNullOrWhiteSpace(req.Role) ? "Learner" : req.Role);
            cmd.Parameters.AddWithValue("@ProfilePicture", (object?)req.ProfilePicturePath ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Token", emailVerificationToken);
            cmd.Parameters.AddWithValue("@Expires", expiresUtc);

            var idObj = cmd.ExecuteScalar();
            return Convert.ToInt32(idObj);
        }

        public bool VerifyEmailByToken(string token, out int? userId)
        {
            userId = null;

            using var conn = _db.GetConnection();
            conn.Open();

            DateTime expires;

            using (var get = new SqlCommand(
                @"SELECT UserId, EmailVerificationExpires 
                  FROM Users 
                  WHERE EmailVerificationToken=@t AND EmailVerified=0", conn))
            {
                get.Parameters.AddWithValue("@t", token);
                using var r = get.ExecuteReader();
                if (!r.Read()) return false;

                userId = r.GetInt32(r.GetOrdinal("UserId"));
                expires = r.GetDateTime(r.GetOrdinal("EmailVerificationExpires"));
            }

            if (expires < DateTime.UtcNow) return false;

            using var upd = new SqlCommand(
                @"UPDATE Users 
                  SET EmailVerified=1, EmailVerificationToken=NULL, EmailVerificationExpires=NULL
                  WHERE UserId=@id", conn);
            upd.Parameters.AddWithValue("@id", userId!.Value);
            return upd.ExecuteNonQuery() > 0;
        }

        public User? GetProfile(int userId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                @"SELECT UserId, FullName, Email, Role, CreatedAt, Bio, Location, 
                         ProfilePicture, ReadyToTeach, IsActive, EmailVerified
                  FROM Users WHERE UserId=@userId", conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                Bio = reader.IsDBNull(reader.GetOrdinal("Bio")) ? null : reader.GetString(reader.GetOrdinal("Bio")),
                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString(reader.GetOrdinal("ProfilePicture")),
                ReadyToTeach = reader.GetBoolean(reader.GetOrdinal("ReadyToTeach")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                EmailVerified = reader.GetBoolean(reader.GetOrdinal("EmailVerified"))
            };
        }

        public bool UpdateProfile(int userId, string fullName, string? bio, string? location)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                @"UPDATE Users 
                  SET FullName=@fullName, Bio=@bio, Location=@location 
                  WHERE UserId=@userId", conn);

            cmd.Parameters.AddWithValue("@fullName", fullName);
            cmd.Parameters.AddWithValue("@bio", (object?)bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@location", (object?)location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@userId", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateTeachMode(int userId, bool readyToTeach, string role)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                @"UPDATE Users 
                  SET ReadyToTeach=@r, Role=@role 
                  WHERE UserId=@id", conn);

            cmd.Parameters.AddWithValue("@r", readyToTeach ? 1 : 0);
            cmd.Parameters.AddWithValue("@role", role);
            cmd.Parameters.AddWithValue("@id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool SetActive(int userId, bool isActive)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                @"UPDATE Users 
                  SET IsActive=@isActive 
                  WHERE UserId=@id", conn);

            cmd.Parameters.AddWithValue("@isActive", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", userId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdateProfilePicture(int userId, string? path)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Users SET ProfilePicture=@p WHERE UserId=@id", conn);
            cmd.Parameters.AddWithValue("@p", (object?)path ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public void DeleteUserWithRules(int id)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var tx = conn.BeginTransaction();

            // lock the row like MySQL FOR UPDATE => use UPDLOCK + HOLDLOCK
            string? role = null;
            using (var get = new SqlCommand("SELECT Role FROM Users WITH (UPDLOCK, HOLDLOCK) WHERE UserId=@id", conn, tx))
            {
                get.Parameters.AddWithValue("@id", id);
                using var r = get.ExecuteReader();
                if (!r.Read())
                    throw new KeyNotFoundException("User not found.");

                role = r.GetString(r.GetOrdinal("Role"));
            }

            if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase))
            {
                using var c = new SqlCommand("SELECT COUNT(*) FROM Users WHERE Role='Admin'", conn, tx);
                var adminCount = Convert.ToInt32(c.ExecuteScalar());
                if (adminCount <= 1)
                    throw new InvalidOperationException("Cannot delete the last admin.");
            }

            using (var cmd = new SqlCommand("DELETE FROM Users WHERE UserId = @id", conn, tx))
            {
                cmd.Parameters.AddWithValue("@id", id);
                var affected = cmd.ExecuteNonQuery();
                if (affected == 0)
                    throw new KeyNotFoundException("User not found.");
            }

            tx.Commit();
        }

        private static User MapUser(SqlDataReader reader)
        {
            return new User
            {
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                PasswordHash = reader.IsDBNull(reader.GetOrdinal("PasswordHash")) ? "" : reader.GetString(reader.GetOrdinal("PasswordHash")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                Bio = reader.IsDBNull(reader.GetOrdinal("Bio")) ? null : reader.GetString(reader.GetOrdinal("Bio")),
                Location = reader.IsDBNull(reader.GetOrdinal("Location")) ? null : reader.GetString(reader.GetOrdinal("Location")),
                ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString(reader.GetOrdinal("ProfilePicture")),
                ReadyToTeach = reader.IsDBNull(reader.GetOrdinal("ReadyToTeach")) ? false : reader.GetBoolean(reader.GetOrdinal("ReadyToTeach")),
                EmailVerified = reader.IsDBNull(reader.GetOrdinal("EmailVerified")) ? false : reader.GetBoolean(reader.GetOrdinal("EmailVerified")),
                IsActive = reader.IsDBNull(reader.GetOrdinal("IsActive")) ? true : reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }
    }
}
