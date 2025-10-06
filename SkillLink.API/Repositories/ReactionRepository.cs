using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class ReactionRepository : IReactionRepository
    {
        private readonly DbHelper _db;
        public ReactionRepository(DbHelper db) => _db = db;

        public void Upsert(int userId, string postType, int postId, string reaction)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"
                UPDATE PostReactions
                SET Reaction = @r, CreatedAt = GETDATE()
                WHERE PostType=@pt AND PostId=@pid AND UserId=@uid;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO PostReactions (PostType, PostId, UserId, Reaction, CreatedAt)
                    VALUES (@pt, @pid, @uid, @r, GETDATE());
                END";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@r", reaction);
            cmd.ExecuteNonQuery();
        }

        public void Remove(int userId, string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"DELETE FROM PostReactions WHERE PostType=@pt AND PostId=@pid AND UserId=@uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.ExecuteNonQuery();
        }

        public (int Likes, int Dislikes) GetCounts(string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"
                SELECT 
                    SUM(CASE WHEN Reaction='LIKE' THEN 1 ELSE 0 END) AS Likes,
                    SUM(CASE WHEN Reaction='DISLIKE' THEN 1 ELSE 0 END) AS Dislikes
                FROM PostReactions
                WHERE PostType=@pt AND PostId=@pid";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return (0, 0);

            int likes = r.IsDBNull(r.GetOrdinal("Likes")) ? 0 : Convert.ToInt32(r["Likes"]);
            int dislikes = r.IsDBNull(r.GetOrdinal("Dislikes")) ? 0 : Convert.ToInt32(r["Dislikes"]);
            return (likes, dislikes);
        }

        public string? GetMyReaction(int userId, string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"SELECT TOP 1 Reaction 
                        FROM PostReactions 
                        WHERE PostType=@pt AND PostId=@pid AND UserId=@uid";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);

            var res = cmd.ExecuteScalar();
            return res == null ? null : Convert.ToString(res);
        }
    }
}
