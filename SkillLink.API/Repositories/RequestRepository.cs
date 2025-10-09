using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class RequestRepository : IRequestRepository
    {
        private readonly DbHelper _dbHelper;
        public RequestRepository(DbHelper dbHelper) => _dbHelper = dbHelper;

        public List<RequestWithUser> GetAllVisibleWithUser(int? viewerUserId)
        {
            var list = new List<RequestWithUser>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            // Show:
            //  - public, non-cancelled requests to everyone
            //  - ALL requests (including CANCELLED) to the requester (owner)
            //  - private requests where viewer is requester or targeted tutor
            var sql = @"
                SELECT r.*, u.FullName, u.Email
                FROM Requests r
                JOIN Users u ON r.LearnerId = u.UserId
                WHERE
                    (
                        r.IsPrivate = 0
                        AND r.Status <> 'CANCELLED'
                    )
                    OR (
                        @viewer IS NOT NULL
                        AND (
                                r.LearnerId = @viewer
                                OR r.PreferredTutorId = @viewer
                            )
                    )
                ORDER BY r.CreatedAt DESC, r.RequestId DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@viewer", (object?)viewerUserId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadRequestWithUser(reader));

            return list;
        }
        private static RequestWithUser ReadRequestWithUser(SqlDataReader reader) => new()
        {
            RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
            LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
            SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
            Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic")),
            Status = reader.GetString(reader.GetOrdinal("Status")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
            Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
            FullName = reader.GetString(reader.GetOrdinal("FullName")),
            Email = reader.GetString(reader.GetOrdinal("Email")),
            PreferredTutorId = reader.IsDBNull(reader.GetOrdinal("PreferredTutorId")) ? null : reader.GetInt32(reader.GetOrdinal("PreferredTutorId")),
            IsPrivate = !reader.IsDBNull(reader.GetOrdinal("IsPrivate")) && reader.GetBoolean(reader.GetOrdinal("IsPrivate"))
        };

        public RequestWithUser? GetByIdWithUser(int requestId)
        {
            RequestWithUser? data = null;
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                "SELECT r.*, u.FullName, u.Email FROM Requests r JOIN Users u ON r.LearnerId = u.UserId WHERE r.RequestId = @requestid",
                conn);
            cmd.Parameters.AddWithValue("@requestid", requestId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                data = new RequestWithUser
                {
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
                    SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    PreferredTutorId = reader.IsDBNull(reader.GetOrdinal("PreferredTutorId")) ? null : reader.GetInt32(reader.GetOrdinal("PreferredTutorId")),
                    IsPrivate = !reader.IsDBNull(reader.GetOrdinal("IsPrivate")) && reader.GetBoolean(reader.GetOrdinal("IsPrivate")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                };
            }
            return data;
        }

        public List<RequestWithUser> GetByLearnerIdWithUser(int learnerId)
        {
            var list = new List<RequestWithUser>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                "SELECT r.*, u.FullName, u.Email FROM Requests r JOIN Users u ON r.LearnerId = u.UserId WHERE r.LearnerId = @learnerId",
                conn);
            cmd.Parameters.AddWithValue("@learnerId", learnerId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RequestWithUser
                {
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
                    SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
            return list;
        }

        public List<RequestWithUser> GetAllWithUser()
        {
            var list = new List<RequestWithUser>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                "SELECT r.*, u.FullName, u.Email FROM Requests r JOIN Users u ON r.LearnerId = u.UserId ORDER BY r.CreatedAt DESC, r.RequestId DESC",
                conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new RequestWithUser
                {
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    LearnerId = reader.GetInt32(reader.GetOrdinal("LearnerId")),
                    SkillName = reader.GetString(reader.GetOrdinal("SkillName")),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString(reader.GetOrdinal("Topic")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString(reader.GetOrdinal("Description")),
                    FullName = reader.GetString(reader.GetOrdinal("FullName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
            return list;
        }

        public void Insert(Request req)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand(@"
                INSERT INTO Requests (LearnerId, SkillName, Topic, Description, Status, PreferredTutorId, IsPrivate)
                VALUES (@learnerId, @skillName, @topic, @description, @status, @preferredTutorId, @isPrivate)", conn);
            cmd.Parameters.AddWithValue("@learnerId", req.LearnerId);
            cmd.Parameters.AddWithValue("@skillName", req.SkillName);
            cmd.Parameters.AddWithValue("@topic", (object?)req.Topic ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", req.Status);
            cmd.Parameters.AddWithValue("@preferredTutorId", (object?)req.PreferredTutorId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@isPrivate", req.IsPrivate);
            cmd.ExecuteNonQuery();
        }
        public void Update(int requestId, Request req)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand(
                "UPDATE Requests SET SkillName=@skillName, Topic=@topic, Description=@description WHERE RequestId=@id",
                conn);
            cmd.Parameters.AddWithValue("@skillName", req.SkillName);
            cmd.Parameters.AddWithValue("@topic", (object?)req.Topic ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@description", (object?)req.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
        }

        public void UpdateStatus(int requestId, string status)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand(
                "UPDATE Requests SET Status=@status WHERE RequestId=@id",
                conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int requestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new SqlCommand("DELETE FROM Requests WHERE RequestId=@id", conn);
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
        }

       public List<RequestWithUser> SearchVisibleWithUser(string query, int? viewerUserId)
        {
            var list = new List<RequestWithUser>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            var sql = @"
                SELECT r.*, u.FullName, u.Email
                FROM Requests r
                JOIN Users u ON r.LearnerId = u.UserId
                WHERE (r.SkillName LIKE @q OR r.Topic LIKE @q OR r.Description LIKE @q OR u.FullName LIKE @q)
                AND (
                        (r.IsPrivate = 0 AND r.Status <> 'CANCELLED')
                    OR (@viewer IS NOT NULL AND (r.LearnerId = @viewer OR r.PreferredTutorId = @viewer))
                )
                ORDER BY r.CreatedAt DESC, r.RequestId DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@q", $"%{query}%");
            cmd.Parameters.AddWithValue("@viewer", (object?)viewerUserId ?? DBNull.Value);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                list.Add(ReadRequestWithUser(reader));

            return list;
        }
        public void RemovePreferredTutor(int requestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                "UPDATE Requests SET PreferredTutorId = NULL, IsPrivate = 0 WHERE RequestId = @id",
                conn);
            cmd.Parameters.AddWithValue("@id", requestId);
            cmd.ExecuteNonQuery();
        }
        public void CancelDirected(int requestId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var tx = conn.BeginTransaction();

            try
            {
                using (var cmd = new SqlCommand(
                    @"UPDATE Requests
                    SET Status = 'CANCELLED',
                        PreferredTutorId = NULL,
                        IsPrivate = 0
                    WHERE RequestId = @id", conn, tx))
                {
                    cmd.Parameters.AddWithValue("@id", requestId);
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
