using MySql.Data.MySqlClient;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;


namespace SkillLink.API.Services
{
    public class TutorPostService : ITutorPostService
    {
        private readonly DbHelper _dbHelper;

        public TutorPostService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        /* ---------- Create ---------- */
        public int CreatePost(TutorPost post)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var cmd = new MySqlCommand(@"
                INSERT INTO TutorPosts (TutorId, Title, Description, MaxParticipants, Status)
                VALUES (@t, @ti, @d, @m, 'Open')", conn);

            cmd.Parameters.AddWithValue("@t", post.TutorId);
            cmd.Parameters.AddWithValue("@ti", post.Title);
            cmd.Parameters.AddWithValue("@d", post.Description ?? "");
            cmd.Parameters.AddWithValue("@m", post.MaxParticipants);

            cmd.ExecuteNonQuery();
            return (int)cmd.LastInsertedId; // ok to keep
        }

        public void SetImageUrl(int postId, string imageUrl)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand("UPDATE TutorPosts SET ImageUrl=@u WHERE PostId=@id", conn);
            cmd.Parameters.AddWithValue("@u", imageUrl);
            cmd.Parameters.AddWithValue("@id", postId);
            cmd.ExecuteNonQuery();
        }

        /* ---------- Read (All with Tutor + CurrentParticipants) ---------- */
        public List<TutorPostWithUser> GetPosts()
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

            using var cmd = new MySqlCommand(sql, conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new TutorPostWithUser
                {
                    PostId = reader.GetInt32("PostId"),
                    TutorId = reader.GetInt32("TutorId"),
                    Title = reader.GetString("Title"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                    MaxParticipants = reader.GetInt32("MaxParticipants"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) 
                        ? (DateTime?)null 
                        : reader.GetDateTime("ScheduledAt"),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) 
                        ? null 
                        : reader.GetString("ImageUrl"),
                    TutorName = reader.GetString("TutorName"),
                    Email = reader.GetString("Email"),
                    CurrentParticipants = reader.GetInt32("CurrentParticipants")
                });
            }
            return list;
        }

        /* ---------- Read by Id ---------- */
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

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@pid", postId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new TutorPostWithUser
            {
                PostId = reader.GetInt32("PostId"),
                TutorId = reader.GetInt32("TutorId"),
                Title = reader.GetString("Title"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString("Description"),
                MaxParticipants = reader.GetInt32("MaxParticipants"),
                Status = reader.GetString("Status"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? (DateTime?)null : reader.GetDateTime("ScheduledAt"),
                ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) 
                    ? null 
                    : reader.GetString("ImageUrl"),
                TutorName = reader.GetString("TutorName"),
                Email = reader.GetString("Email"),
                CurrentParticipants = reader.GetInt32("CurrentParticipants")
            };
        }

        /* ---------- Accept ---------- */

        public void AcceptPost(int postId, int userId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            // 1) Duplicate check first (unchanged)
            using (var dup = new MySqlCommand(
                "SELECT COUNT(*) FROM TutorPostParticipants WHERE PostId=@pid AND UserId=@uid", conn))
            {
                dup.Parameters.AddWithValue("@pid", postId);
                dup.Parameters.AddWithValue("@uid", userId);
                var already = Convert.ToInt32(dup.ExecuteScalar());
                if (already > 0)
                    throw new InvalidOperationException("You already accepted this post.");
            }

            // 2) Load post meta + current participants (single roundtrip)
            int tutorId, max, current;
            string status;

            using (var meta = new MySqlCommand(@"
                SELECT p.TutorId, p.MaxParticipants, p.Status,
                    (SELECT COUNT(*) FROM TutorPostParticipants tpp WHERE tpp.PostId = p.PostId) AS Current
                FROM TutorPosts p
                WHERE p.PostId = @pid;", conn))
            {
                meta.Parameters.AddWithValue("@pid", postId);
                using var r = meta.ExecuteReader();
                if (!r.Read())
                    throw new KeyNotFoundException("Post not found.");

                tutorId = r.GetInt32("TutorId");
                max     = r.GetInt32("MaxParticipants");
                status  = r.GetString("Status");
                current = r.GetInt32("Current");
            }

            // 3) Self-accept check
            if (tutorId == userId)
                throw new InvalidOperationException("You cannot accept your own post.");

            // 4) ✅ PRIORITY: Full check BEFORE Closed/Scheduled
            bool isFull = (max <= 0) || (current >= max);
            if (isFull)
                throw new InvalidOperationException("Post is full.");

            // 5) Status checks (after the full check)
            if (string.Equals(status, "Closed", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Post is already closed.");

            if (string.Equals(status, "Scheduled", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Post is already scheduled.");

            // 6) Insert participant
            using (var ins = new MySqlCommand(
                "INSERT INTO TutorPostParticipants (PostId, UserId) VALUES (@pid,@uid)", conn))
            {
                ins.Parameters.AddWithValue("@pid", postId);
                ins.Parameters.AddWithValue("@uid", userId);
                ins.ExecuteNonQuery();
            }

            // 7) If it just became full, close the post
            if (current + 1 >= max)
            {
                using var upd = new MySqlCommand(
                    "UPDATE TutorPosts SET Status='Closed' WHERE PostId=@pid", conn);
                upd.Parameters.AddWithValue("@pid", postId);
                upd.ExecuteNonQuery();
            }
        }


        /* ---------- Schedule ---------- */
        public void Schedule(int postId, DateTime scheduledAt)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var cmd = new MySqlCommand(
                "UPDATE TutorPosts SET Status='Scheduled', ScheduledAt=@dt WHERE PostId=@pid", conn);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.Parameters.AddWithValue("@dt", scheduledAt);
            cmd.ExecuteNonQuery();
        }

        /* ---------- Update (Owner only) ---------- */
        public void UpdatePost(int postId, int tutorId, UpdateTutorPostDto dto)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var ownerCmd = new MySqlCommand("SELECT TutorId FROM TutorPosts WHERE PostId=@pid", conn);
            ownerCmd.Parameters.AddWithValue("@pid", postId);
            var owner = ownerCmd.ExecuteScalar();
            if (owner == null) throw new KeyNotFoundException("Post not found.");
            if (Convert.ToInt32(owner) != tutorId)
                throw new UnauthorizedAccessException("You can only update your own posts.");

            var countCmd = new MySqlCommand("SELECT COUNT(*) FROM TutorPostParticipants WHERE PostId=@pid", conn);
            countCmd.Parameters.AddWithValue("@pid", postId);
            var currentCount = Convert.ToInt32(countCmd.ExecuteScalar() ?? 0);

            if (dto.MaxParticipants < currentCount)
                throw new InvalidOperationException(
                    $"MaxParticipants ({dto.MaxParticipants}) cannot be less than current participants ({currentCount}).");

            var sql = @"
                UPDATE TutorPosts 
                SET Title=@title, Description=@desc, MaxParticipants=@max
                WHERE PostId=@pid";
            var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@title", dto.Title);
            cmd.Parameters.AddWithValue("@desc", (object?)dto.Description ?? "");
            cmd.Parameters.AddWithValue("@max", dto.MaxParticipants);
            cmd.Parameters.AddWithValue("@pid", postId);
            cmd.ExecuteNonQuery();

            if (!string.IsNullOrWhiteSpace(dto.Status))
            {
                var statusCmd = new MySqlCommand("UPDATE TutorPosts SET Status=@st WHERE PostId=@pid", conn);
                statusCmd.Parameters.AddWithValue("@st", dto.Status);
                statusCmd.Parameters.AddWithValue("@pid", postId);
                statusCmd.ExecuteNonQuery();
            }
        }

        /* ---------- Delete (Owner only) ---------- */
        public void DeletePost(int postId, int tutorId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var ownerCmd = new MySqlCommand("SELECT TutorId FROM TutorPosts WHERE PostId=@pid", conn);
            ownerCmd.Parameters.AddWithValue("@pid", postId);
            var owner = ownerCmd.ExecuteScalar();
            if (owner == null) throw new KeyNotFoundException("Post not found.");
            if (Convert.ToInt32(owner) != tutorId)
                throw new UnauthorizedAccessException("You can only delete your own posts.");

            var del = new MySqlCommand("DELETE FROM TutorPosts WHERE PostId=@pid", conn);
            del.Parameters.AddWithValue("@pid", postId);
            del.ExecuteNonQuery();
        }
    }
}
