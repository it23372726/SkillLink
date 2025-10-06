using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class FriendshipRepository : IFriendshipRepository
    {
        private readonly DbHelper _db;
        public FriendshipRepository(DbHelper db) => _db = db;

        public List<User> GetFollowersBasic(int userId)
        {
            var list = new List<User>();
            using var conn = _db.GetConnection();
            conn.Open();

            var cmd = new SqlCommand(@"
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
                    UserId = r.GetInt32(r.GetOrdinal("UserId")),
                    FullName = r.GetString(r.GetOrdinal("FullName")),
                    Email = r.GetString(r.GetOrdinal("Email"))
                });
            }
            return list;
        }

        public bool IsFollowing(int followerId, int followedId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var check = new SqlCommand(
                "SELECT COUNT(*) FROM Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            check.Parameters.AddWithValue("@f", followerId);
            check.Parameters.AddWithValue("@fd", followedId);
            return Convert.ToInt32(check.ExecuteScalar()) > 0;
        }

        public void InsertFollow(int followerId, int followedId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = new SqlCommand(
                "INSERT INTO Friendships (FollowerId, FollowedId) VALUES (@f, @fd)", conn);
            cmd.Parameters.AddWithValue("@f", followerId);
            cmd.Parameters.AddWithValue("@fd", followedId);
            cmd.ExecuteNonQuery();
        }

        public void DeleteFollow(int followerId, int followedId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = new SqlCommand(
                "DELETE FROM Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            cmd.Parameters.AddWithValue("@f", followerId);
            cmd.Parameters.AddWithValue("@fd", followedId);
            cmd.ExecuteNonQuery();
        }

        public List<User> GetMyFriendsBasic(int userId)
        {
            var list = new List<User>();
            using var conn = _db.GetConnection();
            conn.Open();

            var cmd = new SqlCommand(@"
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
                    UserId = r.GetInt32(r.GetOrdinal("UserId")),
                    FullName = r.GetString(r.GetOrdinal("FullName")),
                    Email = r.GetString(r.GetOrdinal("Email")),
                    ProfilePicture = r.IsDBNull(r.GetOrdinal("ProfilePicture")) ? null : r.GetString(r.GetOrdinal("ProfilePicture")),
                });
            }
            return list;
        }

        public List<User> SearchUsersBasic(string query, int currentUserId)
        {
            var list = new List<User>();
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"
                SELECT TOP 20 UserId, FullName, Email, ProfilePicture
                FROM Users
                WHERE (FullName LIKE @q OR Email LIKE @q)
                AND UserId <> @me
                ORDER BY FullName ASC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", $"%{query}%");
            cmd.Parameters.AddWithValue("@me", currentUserId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new User
                {
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString(reader.GetOrdinal("ProfilePicture")),
                });
            }
            return list;
        }
    }
}
