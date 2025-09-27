using MySql.Data.MySqlClient;
using SkillLink.API.Models;
using SkillLink.API.Services.Abstractions;

namespace SkillLink.API.Services
{
    public class FeedService : IFeedService
    {
        private readonly DbHelper _db;
        private readonly IReactionService _reactions;
        private readonly ICommentService _comments;

        public FeedService(DbHelper db, IReactionService reactions, ICommentService comments)
        {
            _db = db;
            _reactions = reactions;
            _comments = comments;
        }

        public List<FeedItemDto> GetFeed(int me, int page, int pageSize, string? q = null)
        {
            var list = new List<FeedItemDto>();
            using var conn = _db.GetConnection();
            conn.Open();

            bool hasSearch = !string.IsNullOrWhiteSpace(q);
            string like = $"%{q?.Trim()}%";

            var sql = $@"
                (SELECT 
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
                    tp.ScheduledAt AS ScheduledAt
                FROM TutorPosts tp
                JOIN Users u ON u.UserId = tp.TutorId
                {(hasSearch ? @"WHERE (tp.Title LIKE @q OR tp.Description LIKE @q OR u.FullName LIKE @q OR u.Email LIKE @q)" : "")})

                UNION ALL

                (SELECT 
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
                    NULL AS ScheduledAt
                FROM Requests r
                JOIN Users u ON u.UserId = r.LearnerId
                {(hasSearch ? @"WHERE (r.SkillName LIKE @q OR r.Topic LIKE @q OR r.Description LIKE @q OR u.FullName LIKE @q OR u.Email LIKE @q)" : "")})

                ORDER BY CreatedAt DESC
                LIMIT @offset, @ps";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            if (hasSearch)
                cmd.Parameters.AddWithValue("@q", like);

            using var reader = cmd.ExecuteReader();
            var temp = new List<FeedItemDto>();
            while (reader.Read())
            {
                temp.Add(new FeedItemDto
                {
                    PostType = reader.GetString("PostType"),
                    PostId = reader.GetInt32("PostId"),
                    AuthorId = reader.GetInt32("AuthorId"),
                    AuthorName = reader.GetString("AuthorName"),
                    AuthorEmail = reader.GetString("AuthorEmail"),
                    AuthorPic = reader.IsDBNull(reader.GetOrdinal("AuthorPic")) ? null : reader.GetString("AuthorPic"),
                    Title = reader.GetString("Title"),
                    Subtitle = reader.GetString("Subtitle"),
                    Body = reader.GetString("Body"),
                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? null : reader.GetString("ImageUrl"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Status = reader.GetString("Status"),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? (DateTime?)null : reader.GetDateTime("ScheduledAt"),
                });
            }
            reader.Close();

            // augment with counts + my reaction
            foreach (var it in temp)
            {
                var (likes, dislikes, my) = _reactions.GetReactionSummary(me, it.PostType, it.PostId);
                it.Likes = likes;
                it.Dislikes = dislikes;
                it.MyReaction = my;
                it.CommentCount = _comments.Count(it.PostType, it.PostId);
                list.Add(it);
            }

            return list;
        }

    }
}
