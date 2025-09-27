using MySql.Data.MySqlClient;
using SkillLink.API.Models;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly DbHelper _dbHelper;

        public FriendshipService(DbHelper db) => _dbHelper = db;

        public List<User> GetFollowers(int userId)
        {
            var list = new List<User>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var cmd = new MySqlCommand(@"
                SELECT u.UserId, u.FullName, u.Email
                FROM Friendships f
                JOIN Users u ON f.FollowerId = u.UserId
                WHERE f.FollowedId = @uid
                ORDER BY u.FullName ASC", conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new User
                {
                    UserId = r.GetInt32("UserId"),
                    FullName = r.GetString("FullName"),
                    Email = r.GetString("Email")
                });
            }
            return list;
        }


        public void Follow(int followerId, int followedId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var check = new MySqlCommand(
                "SELECT COUNT(*) FROM Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            check.Parameters.AddWithValue("@f", followerId);
            check.Parameters.AddWithValue("@fd", followedId);

            if (Convert.ToInt32(check.ExecuteScalar()) > 0)
                throw new InvalidOperationException("Already following");

            var cmd = new MySqlCommand(
                "INSERT INTO Friendships (FollowerId, FollowedId) VALUES (@f, @fd)", conn);
            cmd.Parameters.AddWithValue("@f", followerId);
            cmd.Parameters.AddWithValue("@fd", followedId);
            cmd.ExecuteNonQuery();
        }

        public void Unfollow(int followerId, int followedId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand(
                "DELETE FROM Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            cmd.Parameters.AddWithValue("@f", followerId);
            cmd.Parameters.AddWithValue("@fd", followedId);
            cmd.ExecuteNonQuery();
        }

        public List<User> GetMyFriends(int userId)
        {
            var list = new List<User>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var cmd = new MySqlCommand(@"
                SELECT u.UserId, u.FullName, u.Email, u.ProfilePicture
                FROM Friendships f
                JOIN Users u ON f.FollowedId = u.UserId
                WHERE f.FollowerId = @uid", conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new User
                {
                    UserId = r.GetInt32("UserId"),
                    FullName = r.GetString("FullName"),
                    Email = r.GetString("Email"),
                    ProfilePicture = r.IsDBNull(r.GetOrdinal("ProfilePicture")) ? null : r.GetString("ProfilePicture"),
                });
            }
            return list;
        }

        public List<User> SearchUsers(string query, int currentUserId)
        {
            var list = new List<User>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT UserId, FullName, Email , profilePicture
                FROM Users
                WHERE (FullName LIKE @q OR Email LIKE @q)
                AND UserId <> @me
                ORDER BY FullName ASC
                LIMIT 20";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", $"%{query}%");
            cmd.Parameters.AddWithValue("@me", currentUserId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new User {
                    UserId = reader.GetInt32("UserId"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email"),
                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString("ProfilePicture"),
                });
            }
            return list;
        }

    }
}
