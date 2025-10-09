using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class FeedRepository : IFeedRepository
    {
        private readonly DbHelper _db;
        public FeedRepository(DbHelper db) => _db = db;

        public List<FeedItemDto> GetFeedPage(int page, int pageSize, string? q)
        {
            var list = new List<FeedItemDto>();
            using var conn = _db.GetConnection();
            conn.Open();

            bool hasSearch = !string.IsNullOrWhiteSpace(q);
            string like = $"%{q?.Trim()}%";

            // The SQL query has been corrected to ensure both SELECT statements have 15 columns.
            var sql = $@"
                SELECT *
                FROM (
                    SELECT 
                        'LESSON' AS PostType,
                        tp.PostId AS PostId,
                        u.UserId AS AuthorId,
                        u.FullName AS AuthorName,
                        u.ProfilePicture as AuthorPic,
                        u.Email AS AuthorEmail,
                        tp.Title AS Title,
                        '' AS Subtitle,
                        COALESCE(tp.Description,'') AS Body,
                        tp.ImageUrl AS ImageUrl,
                        tp.CreatedAt AS CreatedAt,
                        tp.Status AS Status,
                        CAST(0 AS BIT) AS IsPrivate,           -- Added this line (placeholder for IsPrivate)
                        NULL AS PreferredTutorId,               -- Added this line (placeholder for PreferredTutorId)
                        tp.ScheduledAt AS ScheduledAt
                    FROM TutorPosts tp
                    JOIN Users u ON u.UserId = tp.TutorId
                    {(hasSearch ? @"WHERE (tp.Title LIKE @q OR tp.Description LIKE @q OR u.FullName LIKE @q OR u.Email LIKE @q)" : "")}

                    UNION ALL

                    SELECT 
                        'REQUEST' AS PostType,
                        r.RequestId AS PostId,
                        u.UserId AS AuthorId,
                        u.FullName AS AuthorName,
                        u.ProfilePicture as AuthorPic,
                        u.Email AS AuthorEmail,
                        r.SkillName AS Title,
                        COALESCE(r.Topic,'') AS Subtitle,
                        COALESCE(r.Description,'') AS Body,
                        NULL AS ImageUrl,
                        r.CreatedAt AS CreatedAt,
                        r.Status AS Status,
                        r.IsPrivate AS IsPrivate,
                        r.PreferredTutorId AS PreferredTutorId,
                        NULL AS ScheduledAt
                    FROM Requests r
                    JOIN Users u ON u.UserId = r.LearnerId
                    {(hasSearch ? @"WHERE (r.SkillName LIKE @q OR r.Topic LIKE @q OR r.Description LIKE @q OR u.FullName LIKE @q OR u.Email LIKE @q)" : "")}
                ) AS X
                ORDER BY CreatedAt DESC
                OFFSET @offset ROWS FETCH NEXT @ps ROWS ONLY;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            if (hasSearch) cmd.Parameters.AddWithValue("@q", like);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FeedItemDto
                {
                    PostType = reader.GetString(reader.GetOrdinal("PostType")),
                    PostId = reader.GetInt32(reader.GetOrdinal("PostId")),
                    AuthorId = reader.GetInt32(reader.GetOrdinal("AuthorId")),
                    AuthorName = reader.GetString(reader.GetOrdinal("AuthorName")),
                    AuthorEmail = reader.GetString(reader.GetOrdinal("AuthorEmail")),
                    AuthorPic = reader.IsDBNull(reader.GetOrdinal("AuthorPic")) ? "" : reader.GetString(reader.GetOrdinal("AuthorPic")),
                    PreferredTutorId = reader.IsDBNull(reader.GetOrdinal("PreferredTutorId")) ? null : reader.GetInt32(reader.GetOrdinal("PreferredTutorId")),
                    IsPrivate = reader.GetBoolean(reader.GetOrdinal("IsPrivate")),
                    Title = reader.GetString(reader.GetOrdinal("Title")),
                    Subtitle = reader.GetString(reader.GetOrdinal("Subtitle")),
                    Body = reader.GetString(reader.GetOrdinal("Body")),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString(reader.GetOrdinal("ImageUrl")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("ScheduledAt")),
                });
            }

            return list;
        }
    }
}