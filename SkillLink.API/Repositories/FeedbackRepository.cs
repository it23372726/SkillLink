using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class FeedbackRepository : IFeedbackRepository
    {
        private readonly DbHelper _db;

        public FeedbackRepository(DbHelper dbHelper)
        {
            _db = dbHelper;
        }

        public int Insert(FeedbackCreateDto dto, int? userId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(@"
                INSERT INTO Feedback (UserId, Subject, Message, Page, UserAgent, CreatedAt, IsRead)
                OUTPUT INSERTED.FeedbackId
                VALUES (@uid, @subj, @msg, @page, @ua, SYSUTCDATETIME(), 0);
            ", conn);

            cmd.Parameters.AddWithValue("@uid", (object?)userId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@subj", (object?)dto.Subject ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@msg", dto.Message);
            cmd.Parameters.AddWithValue("@page", (object?)dto.Page ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ua", (object?)dto.UserAgent ?? DBNull.Value);

            var id = (int)cmd.ExecuteScalar()!;
            return id;
        }

        public List<FeedbackItem> List(bool? isRead = null, int? limit = null, int? offset = null)
        {
            var list = new List<FeedbackItem>();
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"
                SELECT f.FeedbackId, f.UserId, u.FullName AS UserName,
                       f.Subject, f.Message, f.Page, f.UserAgent, f.CreatedAt, f.IsRead
                FROM Feedback f
                LEFT JOIN Users u ON f.UserId = u.UserId
                WHERE (@isRead IS NULL OR f.IsRead = @isRead)
                ORDER BY f.CreatedAt DESC
                OFFSET @offset ROWS FETCH NEXT @limit ROWS ONLY;
            ";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@isRead", (object?)isRead ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@offset", (object?)(offset ?? 0) ?? 0);
            cmd.Parameters.AddWithValue("@limit", (object?)(limit ?? 50) ?? 50);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new FeedbackItem
                {
                    FeedbackId = rd.GetInt32(rd.GetOrdinal("FeedbackId")),
                    UserId = rd.IsDBNull(rd.GetOrdinal("UserId")) ? null : rd.GetInt32(rd.GetOrdinal("UserId")),
                    UserName = rd.IsDBNull(rd.GetOrdinal("UserName")) ? null : rd.GetString(rd.GetOrdinal("UserName")),
                    Subject = rd.IsDBNull(rd.GetOrdinal("Subject")) ? null : rd.GetString(rd.GetOrdinal("Subject")),
                    Message = rd.GetString(rd.GetOrdinal("Message")),
                    Page = rd.IsDBNull(rd.GetOrdinal("Page")) ? null : rd.GetString(rd.GetOrdinal("Page")),
                    UserAgent = rd.IsDBNull(rd.GetOrdinal("UserAgent")) ? null : rd.GetString(rd.GetOrdinal("UserAgent")),
                    CreatedAt = rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
                    IsRead = rd.GetBoolean(rd.GetOrdinal("IsRead"))
                });
            }

            return list;
        }

        public void MarkRead(int feedbackId, bool isRead)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(@"
                UPDATE Feedback SET IsRead = @isRead WHERE FeedbackId = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@id", feedbackId);
            cmd.Parameters.AddWithValue("@isRead", isRead);
            cmd.ExecuteNonQuery();
        }
    }
}
