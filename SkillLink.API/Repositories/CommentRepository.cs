using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class CommentRepository : ICommentRepository
    {
        private readonly DbHelper _db;
        public CommentRepository(DbHelper db) => _db = db;

        public List<CommentRow> GetComments(string postType, int postId)
        {
            var list = new List<CommentRow>();
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"
                SELECT 
                c.CommentId, c.Content, c.CreatedAt, 
                u.UserId, u.FullName, u.ProfilePicture AS ProfilePicture
                FROM PostComments c
                JOIN Users u ON u.UserId = c.UserId
                WHERE c.PostType=@pt AND c.PostId=@pid
                ORDER BY c.CreatedAt ASC";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CommentRow
                {
                    CommentId = reader.GetInt32(reader.GetOrdinal("CommentId")),
                    Content = reader.GetString(reader.GetOrdinal("Content")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    ProfilePicture = reader.IsDBNull(reader.GetOrdinal("ProfilePicture")) ? null : reader.GetString(reader.GetOrdinal("ProfilePicture"))
                });
            }
            return list;
        }

        public void Insert(string postType, int postId, int userId, string content)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var sql = @"INSERT INTO PostComments (PostType, PostId, UserId, Content) VALUES (@pt,@pid,@uid,@c)";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@c", content);
            cmd.ExecuteNonQuery();
        }

        public void DeleteById(int commentId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var del = new SqlCommand("DELETE FROM PostComments WHERE CommentId=@cid", conn);
            del.Parameters.AddWithValue("@cid", commentId);
            del.ExecuteNonQuery();
        }

        public CommentMeta? GetCommentMeta(int commentId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var infoSql = "SELECT PostType, PostId, UserId FROM PostComments WHERE CommentId=@cid";
            using var infoCmd = new SqlCommand(infoSql, conn);
            infoCmd.Parameters.AddWithValue("@cid", commentId);
            using var r = infoCmd.ExecuteReader();
            if (!r.Read()) return null;

            var meta = new CommentMeta
            {
                PostType = r.GetString(r.GetOrdinal("PostType")),
                PostId = r.GetInt32(r.GetOrdinal("PostId")),
                OwnerUserId = r.GetInt32(r.GetOrdinal("UserId"))
            };
            return meta;
        }

        public int? GetPostOwnerId(string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            string sql = postType.ToUpperInvariant() switch
            {
                "REQUEST" => "SELECT LearnerId AS OwnerId FROM Requests WHERE RequestId=@pid",
                "LESSON"  => "SELECT TutorId AS OwnerId FROM TutorPosts WHERE PostId=@pid",
                "TUTOR"   => "SELECT TutorId AS OwnerId FROM TutorPosts WHERE PostId=@pid",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(sql)) return null;

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", postId);
            var owner = cmd.ExecuteScalar();
            return owner == null ? (int?)null : Convert.ToInt32(owner);
        }

        public int Count(string postType, int postId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            var cmd = new SqlCommand("SELECT COUNT(*) FROM PostComments WHERE PostType=@pt AND PostId=@pid", conn);
            cmd.Parameters.AddWithValue("@pt", postType);
            cmd.Parameters.AddWithValue("@pid", postId);
            return Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }
    }
}
