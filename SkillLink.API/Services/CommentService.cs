using MySql.Data.MySqlClient;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;


namespace SkillLink.API.Services
{
    public class CommentService : ICommentService
    {
        private readonly DbHelper _db;
        public CommentService(DbHelper db) { _db = db; }

        public List<dynamic> GetComments(string postType, int postId)
        {
            var list = new List<dynamic>();
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"
                SELECT c.CommentId, c.Content, c.CreatedAt, u.UserId, u.FullName
                FROM PostComments c
                JOIN Users u ON u.UserId = c.UserId
                WHERE c.PostType=@pt AND c.PostId=@pid
                ORDER BY c.CreatedAt ASC";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new {
                    CommentId = reader.GetInt32("CommentId"),
                    Content = reader.GetString("Content"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    UserId = reader.GetInt32("UserId"),
                    FullName = reader.GetString("FullName")
                });
            }
            return list;
        }

        public void Add(string postType, int postId, int userId, string content)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"INSERT INTO PostComments (PostType, PostId, UserId, Content) VALUES (@pt,@pid,@uid,@c)";
            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@c", content);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int commentId, int userId, bool isAdmin)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            // first get the comment info
            var infoSql = "SELECT PostType, PostId, UserId FROM PostComments WHERE CommentId=@cid";
            using var infoCmd = new MySqlCommand(infoSql, conn);
            infoCmd.Parameters.AddWithValue("@cid", commentId);
            using var reader = infoCmd.ExecuteReader();
            if (!reader.Read()) return;
            var postType = reader.GetString("PostType");
            var postId = reader.GetInt32("PostId");
            var commentOwnerId = reader.GetInt32("UserId");
            reader.Close();

            // admin can delete any
            if (isAdmin)
            {
                var del = new MySqlCommand("DELETE FROM PostComments WHERE CommentId=@cid", conn);
                del.Parameters.AddWithValue("@cid", commentId);
                del.ExecuteNonQuery();
                return;
            }

            // check if user is post owner
            bool isPostOwner = false;
            string postTable = postType switch
            {
                "TUTOR" => "TutorPosts",
                "REQUEST" => "Requests",
                "LESSON" => "TutorPosts",
                _ => null
            };

            if (postTable != null)
            {
                var ownerCmd = new MySqlCommand($"SELECT TutorId AS OwnerId FROM {postTable} WHERE PostId=@pid", conn);
                ownerCmd.Parameters.AddWithValue("@pid", postId);
                var owner = ownerCmd.ExecuteScalar();
                if (owner != null && Convert.ToInt32(owner) == userId)
                {
                    isPostOwner = true;
                }
            }

            if (userId == commentOwnerId || isPostOwner)
            {
                var del = new MySqlCommand("DELETE FROM PostComments WHERE CommentId=@cid", conn);
                del.Parameters.AddWithValue("@cid", commentId);
                del.ExecuteNonQuery();
            }
        }

        public int Count(string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand("SELECT COUNT(*) FROM PostComments WHERE PostType=@pt AND PostId=@pid", conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
    }
}
