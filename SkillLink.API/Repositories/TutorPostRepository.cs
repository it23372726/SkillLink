using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class TutorPostRepository : ITutorPostRepository
    {
        private readonly DbHelper _dbHelper;

        public TutorPostRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public int Create(TutorPost post)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var cmd = new SqlCommand(@"
                INSERT INTO TutorPosts (TutorId, Title, Description, MaxParticipants, Status)
                VALUES (@t, @ti, @d, @m, 'Open');
                SELECT CAST(SCOPE_IDENTITY() as int);", conn);

            cmd.Parameters.AddWithValue("@t", post.TutorId);
            cmd.Parameters.AddWithValue("@ti", post.Title);
            cmd.Parameters.AddWithValue("@d", (object?)post.Description ?? "");
            cmd.Parameters.AddWithValue("@m", post.MaxParticipants);

            var id = cmd.ExecuteScalar();
            return Convert.ToInt32(id);
        }

        public void SetImageUrl(int postId, string imageUrl)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand("UPDATE TutorPosts SET ImageUrl=@u WHERE PostId=@id", conn);
            cmd.Parameters.AddWithValue("@u", imageUrl);
            cmd.Parameters.AddWithValue("@id", postId);
            cmd.ExecuteNonQuery();
        }

        public List<TutorPostWithUser> GetAll()
        {
            var list = new List<TutorPostWithUser>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT 
                    p.PostId, p.TutorId, p.Title, p.Description, p.MaxParticipants, p.Status, 
                    p.CreatedAt, p.ScheduledAt, p.ImageUrl,
                    u.FullName AS TutorName, u.Email,
                    (SELECT COUNT(*) FROM TutorPostParticipants tp WHERE tp.PostId = p.PostId) AS CurrentParticipants
                FROM TutorPosts p
                JOIN Users u ON p.TutorId = u.UserId
                ORDER BY p.CreatedAt DESC;";

            using var cmd = new SqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TutorPostWithUser
                {
                    PostId = reader.GetInt32(reader.GetOrdinal("PostId")),
                    TutorId = reader.GetInt32(reader.GetOrdinal("TutorId")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                    MaxParticipants = reader.GetInt32(reader.GetOrdinal("MaxParticipants")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ScheduledAt")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                    TutorName = reader.GetString(reader.GetOrdinal("TutorName")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    CurrentParticipants = reader.GetInt32(reader.GetOrdinal("CurrentParticipants"))
                });
            }
            return list;
        }

        public TutorPostWithUser? GetById(int postId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT 
                    p.PostId, p.TutorId, p.Title, p.Description, p.MaxParticipants, p.Status,
                    p.CreatedAt, p.ScheduledAt, p.ImageUrl,
                    u.FullName AS TutorName, u.Email,
                    (SELECT COUNT(*) FROM TutorPostParticipants tp WHERE tp.PostId = p.PostId) AS CurrentParticipants
                FROM TutorPosts p
                JOIN Users u ON p.TutorId = u.UserId
                WHERE p.PostId = @pid;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", postId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new TutorPostWithUser
            {
                PostId = reader.GetInt32(reader.GetOrdinal("PostId")),
                TutorId = reader.GetInt32(reader.GetOrdinal("TutorId")),
                Title = reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                MaxParticipants = reader.GetInt32(reader.GetOrdinal("MaxParticipants")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ScheduledAt")),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                TutorName = reader.GetString(reader.GetOrdinal("TutorName")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                CurrentParticipants = reader.GetInt32(reader.GetOrdinal("CurrentParticipants"))
            };
        }

        public bool IsParticipant(int postId, int userId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var dup = new SqlCommand(
                "SELECT COUNT(*) FROM TutorPostParticipants WHERE PostId=@pid AND UserId=@uid", conn);
            dup.Parameters.AddWithValue("@pid", postId);
            dup.Parameters.AddWithValue("@uid", userId);
            var already = Convert.ToInt32(dup.ExecuteScalar());
            return already > 0;
        }

        // 🔧 FIXED: avoid reserved keyword alias 'Current'
        public (int TutorId, int MaxParticipants, string Status, int Current)? GetPostMeta(int postId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var meta = new SqlCommand(@"
                SELECT p.TutorId, p.MaxParticipants, p.Status,
                       (SELECT COUNT(*) FROM TutorPostParticipants tpp WHERE tpp.PostId = p.PostId) AS CurrentParticipants
                FROM TutorPosts p
                WHERE p.PostId = @pid;", conn);

            meta.Parameters.AddWithValue("@pid", postId);

            using var r = meta.ExecuteReader();
            if (!r.Read()) return null;

            return (
                r.GetInt32(r.GetOrdinal("TutorId")),
                r.GetInt32(r.GetOrdinal("MaxParticipants")),
                r.GetString(r.GetOrdinal("Status")),
                r.GetInt32(r.GetOrdinal("CurrentParticipants")) // map to tuple name 'Current'
            );
        }

        public void AddParticipant(int postId, int userId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var ins = new SqlCommand(
                "INSERT INTO TutorPostParticipants (PostId, UserId) VALUES (@pid,@uid)", conn);
            ins.Parameters.AddWithValue("@pid", postId);
            ins.Parameters.AddWithValue("@uid", userId);
            ins.ExecuteNonQuery();
        }

        public void UpdateStatus(int postId, string status)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var upd = new SqlCommand("UPDATE TutorPosts SET Status=@st WHERE PostId=@pid", conn);
            upd.Parameters.AddWithValue("@st", status);
            upd.Parameters.AddWithValue("@pid", postId);
            upd.ExecuteNonQuery();
        }

        public void Schedule(int postId, DateTime scheduledAt)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var cmd = new SqlCommand(
                "UPDATE TutorPosts SET Status='Scheduled', ScheduledAt=@dt WHERE PostId=@pid", conn);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@dt", scheduledAt);
            cmd.ExecuteNonQuery();
        }

        public int? GetOwnerId(int postId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var ownerCmd = new SqlCommand("SELECT TutorId FROM TutorPosts WHERE PostId=@pid", conn);
            ownerCmd.Parameters.AddWithValue("@pid", postId);
            var owner = ownerCmd.ExecuteScalar();
            return owner == null ? null : Convert.ToInt32(owner);
        }

        public int GetCurrentParticipantsCount(int postId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var countCmd = new SqlCommand("SELECT COUNT(*) FROM TutorPostParticipants WHERE PostId=@pid", conn);
            countCmd.Parameters.AddWithValue("@pid", postId);
            return Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);
        }

        public void UpdatePost(int postId, string title, string? description, int maxParticipants)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                UPDATE TutorPosts 
                SET Title=@title, Description=@desc, MaxParticipants=@max
                WHERE PostId=@pid";

            var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", title);
            cmd.Parameters.AddWithValue("@desc", (object?)description ?? "");
            cmd.Parameters.AddWithValue("@max", maxParticipants);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int postId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var del = new SqlCommand("DELETE FROM TutorPosts WHERE PostId=@pid", conn);
            del.Parameters.AddWithValue("@pid", postId);
            del.ExecuteNonQuery();
        }
    }
}
