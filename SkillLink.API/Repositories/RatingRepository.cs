using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class RatingRepository : IRatingRepository
    {
        private readonly DbHelper _db;

        public RatingRepository(DbHelper db) => _db = db;

        public void Create(Rating rating)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            const string sql = @"
INSERT INTO Ratings (AcceptedRequestId, TutorId, LearnerId, Rating, Comment, CreatedAt)
VALUES (@arid, @tutor, @learner, @rating, @comment, @created)";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@arid", rating.AcceptedRequestId);
            cmd.Parameters.AddWithValue("@tutor", rating.TutorId);
            cmd.Parameters.AddWithValue("@learner", rating.LearnerId);
            cmd.Parameters.AddWithValue("@rating", rating.Score);
            cmd.Parameters.AddWithValue("@comment", (object?)rating.Comment ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created", rating.CreatedAt);
            cmd.ExecuteNonQuery();
        }

        public bool ExistsForAccepted(int acceptedRequestId, int learnerId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            const string sql = @"SELECT 1 FROM Ratings WHERE AcceptedRequestId=@arid AND LearnerId=@learner";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@arid", acceptedRequestId);
            cmd.Parameters.AddWithValue("@learner", learnerId);

            using var r = cmd.ExecuteReader();
            return r.Read();
        }

        public RatingSummaryDto? SummaryForTutor(int tutorId)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            const string sql = @"
SELECT COUNT(*) as Cnt, AVG(CAST(Rating as float)) as AvgScore
FROM Ratings
WHERE TutorId = @tutor";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tutor", tutorId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            var cnt = reader.IsDBNull(reader.GetOrdinal("Cnt")) ? 0 : reader.GetInt32(reader.GetOrdinal("Cnt"));
            var avg = reader.IsDBNull(reader.GetOrdinal("AvgScore")) ? 0.0 : reader.GetDouble(reader.GetOrdinal("AvgScore"));

            return new RatingSummaryDto
            {
                TutorId = tutorId,
                Count = cnt,
                Average = Math.Round(avg, 2)
            };
        }

        public List<RatingViewDto> ListReceived(int tutorId, int limit)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            const string sql = @"
SELECT TOP (@limit)
    r.RatingId,
    r.AcceptedRequestId,
    r.Rating,
    r.Comment,
    r.CreatedAt,
    req.SkillName,
    uLearner.FullName AS FromUserName,
    uTutor.FullName   AS ToUserName
FROM Ratings r
JOIN AcceptedRequests ar ON ar.AcceptedRequestId = r.AcceptedRequestId
JOIN Requests req        ON req.RequestId = ar.RequestId
JOIN Users uTutor        ON uTutor.UserId = r.TutorId
JOIN Users uLearner      ON uLearner.UserId = r.LearnerId
WHERE r.TutorId = @tutor
ORDER BY r.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@tutor", tutorId);
            cmd.Parameters.AddWithValue("@limit", limit);

            var list = new List<RatingViewDto>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new RatingViewDto
                {
                    RatingId = rd.GetInt32(rd.GetOrdinal("RatingId")),
                    AcceptedRequestId = rd.GetInt32(rd.GetOrdinal("AcceptedRequestId")),
                    Rating = rd.GetInt32(rd.GetOrdinal("Rating")),
                    Comment = rd.IsDBNull(rd.GetOrdinal("Comment")) ? "" : rd.GetString(rd.GetOrdinal("Comment")),
                    CreatedAt = rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
                    SkillName = rd.IsDBNull(rd.GetOrdinal("SkillName")) ? "" : rd.GetString(rd.GetOrdinal("SkillName")),
                    FromUserName = rd.IsDBNull(rd.GetOrdinal("FromUserName")) ? "" : rd.GetString(rd.GetOrdinal("FromUserName")),
                    ToUserName = rd.IsDBNull(rd.GetOrdinal("ToUserName")) ? "" : rd.GetString(rd.GetOrdinal("ToUserName")),
                });
            }

            return list;
        }

        public List<RatingViewDto> ListGiven(int learnerId, int limit)
        {
            using var conn = _db.GetConnection();
            conn.Open();

            const string sql = @"
SELECT TOP (@limit)
    r.RatingId,
    r.AcceptedRequestId,
    r.Rating,
    r.Comment,
    r.CreatedAt,
    req.SkillName,
    uLearner.FullName AS FromUserName,
    uTutor.FullName   AS ToUserName
FROM Ratings r
JOIN AcceptedRequests ar ON ar.AcceptedRequestId = r.AcceptedRequestId
JOIN Requests req        ON req.RequestId = ar.RequestId
JOIN Users uTutor        ON uTutor.UserId = r.TutorId
JOIN Users uLearner      ON uLearner.UserId = r.LearnerId
WHERE r.LearnerId = @learner
ORDER BY r.CreatedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@learner", learnerId);
            cmd.Parameters.AddWithValue("@limit", limit);

            var list = new List<RatingViewDto>();
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new RatingViewDto
                {
                    RatingId = rd.GetInt32(rd.GetOrdinal("RatingId")),
                    AcceptedRequestId = rd.GetInt32(rd.GetOrdinal("AcceptedRequestId")),
                    Rating = rd.GetInt32(rd.GetOrdinal("Rating")),
                    Comment = rd.IsDBNull(rd.GetOrdinal("Comment")) ? "" : rd.GetString(rd.GetOrdinal("Comment")),
                    CreatedAt = rd.GetDateTime(rd.GetOrdinal("CreatedAt")),
                    SkillName = rd.IsDBNull(rd.GetOrdinal("SkillName")) ? "" : rd.GetString(rd.GetOrdinal("SkillName")),
                    FromUserName = rd.IsDBNull(rd.GetOrdinal("FromUserName")) ? "" : rd.GetString(rd.GetOrdinal("FromUserName")),
                    ToUserName = rd.IsDBNull(rd.GetOrdinal("ToUserName")) ? "" : rd.GetString(rd.GetOrdinal("ToUserName")),
                });
            }

            return list;
        }
    }
}
