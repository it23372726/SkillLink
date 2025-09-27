using MySql.Data.MySqlClient;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;


namespace SkillLink.API.Services
{
    public class ReactionService : IReactionService
    {
        private readonly DbHelper _db;
        public ReactionService(DbHelper db) { _db = db; }

        public void UpsertReaction(int userId, string postType, int postId, string reaction)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            // Either insert or update to given reaction
            var sql = @"
                INSERT INTO PostReactions (PostType, PostId, UserId, Reaction)
                VALUES (@pt, @pid, @uid, @r)
                ON DUPLICATE KEY UPDATE Reaction=@r, CreatedAt=NOW()";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@r", reaction);
            cmd.ExecuteNonQuery();
        }

        public void RemoveReaction(int userId, string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand("DELETE FROM PostReactions WHERE PostType=@pt AND PostId=@pid AND UserId=@uid", conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.ExecuteNonQuery();
        }

        public (int likes, int dislikes, string? my) GetReactionSummary(int userId, string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            var sumSql = @"SELECT 
                            SUM(Reaction='LIKE') as Likes,
                            SUM(Reaction='DISLIKE') as Dislikes
                        FROM PostReactions
                        WHERE PostType=@pt AND PostId=@pid";
            using var sumCmd = new MySqlCommand(sumSql, conn);
            sumCmd.Parameters.AddWithValue("@pt", postType);
            sumCmd.Parameters.AddWithValue("@pid", postId);

            int likes = 0, dislikes = 0;
            using (var reader = sumCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    likes = reader["Likes"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Likes"]);
                    dislikes = reader["Dislikes"] == DBNull.Value ? 0 : Convert.ToInt32(reader["Dislikes"]);
                }
            }

            var mySql = @"SELECT Reaction 
                        FROM PostReactions 
                        WHERE PostType=@pt AND PostId=@pid AND UserId=@uid";
            using var myCmd = new MySqlCommand(mySql, conn);
            myCmd.Parameters.AddWithValue("@pt", postType);
            myCmd.Parameters.AddWithValue("@pid", postId);
            myCmd.Parameters.AddWithValue("@uid", userId);
            var my = myCmd.ExecuteScalar()?.ToString();

            return (likes, dislikes, my);
        }

    }
}
