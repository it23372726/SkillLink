using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class AcceptedRequestRepository : IAcceptedRequestRepository
    {
        private readonly DbHelper _dbHelper;

        public AcceptedRequestRepository(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        public bool HasUserAcceptedRequest(int userId, int requestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM AcceptedRequests WHERE AcceptorId = @userId AND RequestId = @requestId",
                conn
            );
            cmd.Parameters.AddWithValue("@userId", userId);
            cmd.Parameters.AddWithValue("@requestId", requestId);

            var count = Convert.ToInt32(cmd.ExecuteScalar());
            return count > 0;
        }

        public void InsertAcceptance(int requestId, int acceptorId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                "INSERT INTO AcceptedRequests (RequestId, AcceptorId) VALUES (@requestId, @acceptorId)",
                conn
            );
            cmd.Parameters.AddWithValue("@requestId", requestId);
            cmd.Parameters.AddWithValue("@acceptorId", acceptorId);
            cmd.ExecuteNonQuery();
        }

        public List<AcceptedRequestWithDetails> GetAcceptedRequestsByUser(int userId)
        {
            var list = new List<AcceptedRequestWithDetails>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT ar.*, r.SkillName, r.Topic, r.Description, 
                       u.FullName as RequesterName, u.Email as RequesterEmail, u.UserId as RequesterId
                FROM AcceptedRequests ar
                JOIN Requests r ON ar.RequestId = r.RequestId
                JOIN Users u ON r.LearnerId = u.UserId
                WHERE ar.AcceptorId = @userId
                ORDER BY ar.AcceptedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AcceptedRequestWithDetails
                {
                    AcceptedRequestId = reader.GetInt32(reader.GetOrdinal("AcceptedRequestId")),
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    AcceptorId = reader.GetInt32(reader.GetOrdinal("AcceptorId")),
                    AcceptedAt = reader.GetDateTime(reader.GetOrdinal("AcceptedAt")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    ScheduleDate = reader.IsDBNull(reader.GetOrdinal("ScheduleDate")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ScheduleDate")),
                    MeetingType = reader.IsDBNull(reader.GetOrdinal("MeetingType")) ? null : reader.GetString(reader.GetOrdinal("MeetingType")),
                    MeetingLink = reader.IsDBNull(reader.GetOrdinal("MeetingLink")) ? null : reader.GetString(reader.GetOrdinal("MeetingLink")),
                    SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    RequesterName = reader.GetString(reader.GetOrdinal("RequesterName")),
                    RequesterEmail = reader.GetString(reader.GetOrdinal("RequesterEmail")),
                    RequesterId = reader.GetInt32(reader.GetOrdinal("RequesterId"))
                });
            }

            return list;
        }

        public void UpdateAcceptanceStatus(int acceptedRequestId, string status)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new SqlCommand(
                "UPDATE AcceptedRequests SET Status = @status WHERE AcceptedRequestId = @id",
                conn
            );
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", acceptedRequestId);
            cmd.ExecuteNonQuery();
        }

        public void ScheduleMeeting(int acceptedRequestId, DateTime scheduleDate, string meetingType, string meetingLink)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var tx = conn.BeginTransaction();
            try
            {
                // Update the accepted record
                using (var cmd = new SqlCommand(
                    @"UPDATE AcceptedRequests 
                    SET ScheduleDate = @scheduleDate, 
                        MeetingType = @meetingType, 
                        MeetingLink = @meetingLink, 
                        Status = 'SCHEDULED' 
                    WHERE AcceptedRequestId = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@scheduleDate", scheduleDate);
                    cmd.Parameters.AddWithValue("@meetingType", (object?)meetingType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@meetingLink", (object?)meetingLink ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@id", acceptedRequestId);
                    cmd.ExecuteNonQuery();
                }

                // Also flip the parent Request to SCHEDULED so both sides see it
                using (var cmd = new SqlCommand(
                    @"UPDATE Requests 
                    SET Status = 'SCHEDULED' 
                    WHERE RequestId = (SELECT RequestId FROM AcceptedRequests WHERE AcceptedRequestId = @id)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", acceptedRequestId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }


        public List<AcceptedRequestWithDetails> GetRequestsIAskedFor(int userId)
        {
            var list = new List<AcceptedRequestWithDetails>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT ar.*, r.SkillName, r.Topic, r.Description, 
                       u.FullName as AcceptorName, u.Email as AcceptorEmail
                FROM AcceptedRequests ar
                JOIN Requests r ON ar.RequestId = r.RequestId
                JOIN Users u ON ar.AcceptorId = u.UserId
                WHERE r.LearnerId = @userId
                ORDER BY ar.AcceptedAt DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@userId", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AcceptedRequestWithDetails
                {
                    AcceptedRequestId = reader.GetInt32(reader.GetOrdinal("AcceptedRequestId")),
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    AcceptorId = reader.GetInt32(reader.GetOrdinal("AcceptorId")),
                    AcceptedAt = reader.GetDateTime(reader.GetOrdinal("AcceptedAt")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    ScheduleDate = reader.IsDBNull(reader.GetOrdinal("ScheduleDate")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ScheduleDate")),
                    MeetingType = reader.IsDBNull(reader.GetOrdinal("MeetingType")) ? null : reader.GetString(reader.GetOrdinal("MeetingType")),
                    MeetingLink = reader.IsDBNull(reader.GetOrdinal("MeetingLink")) ? null : reader.GetString(reader.GetOrdinal("MeetingLink")),
                    SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    // Keeping your original mapping note:
                    RequesterName = reader.GetString(reader.GetOrdinal("AcceptorName")),
                    RequesterEmail = reader.GetString(reader.GetOrdinal("AcceptorEmail"))
                });
            }

            return list;
        }

                public AcceptedRequestWithDetails? GetAcceptedDetails(int acceptedRequestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT 
                    ar.*, 
                    r.SkillName, r.Topic, r.Description,
                    uReq.FullName as RequesterName, uReq.Email as RequesterEmail, r.LearnerId as RequesterId
                FROM AcceptedRequests ar
                JOIN Requests r ON r.RequestId = ar.RequestId
                JOIN Users uReq ON uReq.UserId = r.LearnerId
                WHERE ar.AcceptedRequestId = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", acceptedRequestId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return new AcceptedRequestWithDetails
            {
                AcceptedRequestId = reader.GetInt32(reader.GetOrdinal("AcceptedRequestId")),
                RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                AcceptorId = reader.GetInt32(reader.GetOrdinal("AcceptorId")),
                AcceptedAt = reader.GetDateTime(reader.GetOrdinal("AcceptedAt")),
                Status = reader.GetString(reader.GetOrdinal("Status")),
                ScheduleDate = reader.IsDBNull(reader.GetOrdinal("ScheduleDate")) ? null : (DateTime?)reader.GetDateTime(reader.GetOrdinal("ScheduleDate")),
                MeetingType = reader.IsDBNull(reader.GetOrdinal("MeetingType")) ? "" : reader.GetString(reader.GetOrdinal("MeetingType")),
                MeetingLink = reader.IsDBNull(reader.GetOrdinal("MeetingLink")) ? "" : reader.GetString(reader.GetOrdinal("MeetingLink")),
                SkillName = reader.IsDBNull(reader.GetOrdinal("SkillName")) ? "" : reader.GetString(reader.GetOrdinal("SkillName")),
                Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? "" : reader.GetString(reader.GetOrdinal("Topic")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                RequesterName = reader.IsDBNull(reader.GetOrdinal("RequesterName")) ? "" : reader.GetString(reader.GetOrdinal("RequesterName")),
                RequesterEmail = reader.IsDBNull(reader.GetOrdinal("RequesterEmail")) ? "" : reader.GetString(reader.GetOrdinal("RequesterEmail")),
                RequesterId = reader.GetInt32(reader.GetOrdinal("RequesterId"))
            };
        }

        public AcceptedMeta? GetAcceptedMeta(int acceptedRequestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT 
                    ar.AcceptedRequestId,
                    ar.RequestId, 
                    r.LearnerId AS RequesterId, 
                    ar.AcceptorId, 
                    ar.Status
                FROM AcceptedRequests ar
                JOIN Requests r ON r.RequestId = ar.RequestId
                WHERE ar.AcceptedRequestId = @id";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@id", acceptedRequestId);

            using var rd = cmd.ExecuteReader();
            if (!rd.Read()) return null;

            return new AcceptedMeta
            {
                AcceptedRequestId = rd.GetInt32(rd.GetOrdinal("AcceptedRequestId")),
                RequestId    = rd.GetInt32(rd.GetOrdinal("RequestId")),
                RequesterId  = rd.GetInt32(rd.GetOrdinal("RequesterId")),
                AcceptorId   = rd.GetInt32(rd.GetOrdinal("AcceptorId")),
                Status       = rd.IsDBNull(rd.GetOrdinal("Status")) ? "" : rd.GetString(rd.GetOrdinal("Status"))
            };
        }


        public void Complete(int acceptedRequestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();
            try
            {
                // mark accepted row
                using (var cmd = new SqlCommand(
                    @"UPDATE AcceptedRequests
                    SET Status = 'COMPLETED'
                    WHERE AcceptedRequestId = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", acceptedRequestId);
                    cmd.ExecuteNonQuery();
                }

                // mark parent request
                using (var cmd = new SqlCommand(
                    @"UPDATE Requests
                    SET Status = 'COMPLETED'
                    WHERE RequestId = (SELECT RequestId FROM AcceptedRequests WHERE AcceptedRequestId = @id)", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", acceptedRequestId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }
        }

    }
}
