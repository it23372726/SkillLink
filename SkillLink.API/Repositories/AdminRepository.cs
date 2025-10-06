using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class AdminRepository : IAdminRepository
    {
        private readonly DbHelper _dbHelper;

        public AdminRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public List<User> GetUsers(string? search = null)
        {
            var users = new List<User>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = "SELECT UserId, FullName, Email, Role, CreatedAt, IsActive, ReadyToTeach FROM Users";
            if (!string.IsNullOrWhiteSpace(search))
                sql += " WHERE FullName LIKE @s OR Email LIKE @s";

            using var cmd = new SqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@s", $"%{search}%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive")),
                    ReadyToTeach = reader.GetBoolean(reader.GetOrdinal("ReadyToTeach"))
                });
            }
            return users;
        }

        public bool SetUserActive(int userId, bool isActive)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("UPDATE Users SET IsActive=@a WHERE UserId=@id", conn);
            cmd.Parameters.AddWithValue("@a", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool SetUserRole(int userId, string role)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("UPDATE Users SET Role=@r WHERE UserId=@id", conn);
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
