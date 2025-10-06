using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class SessionRepository : ISessionRepository
    {
        private readonly DbHelper _db;
        public SessionRepository(DbHelper db) => _db = db;

        public bool RequestExists(int requestId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var chk = new SqlCommand("SELECT COUNT(*) FROM Requests WHERE RequestId=@id", conn);
            chk.Parameters.AddWithValue("@id", requestId);
            return Convert.ToInt32(chk.ExecuteScalar()) > 0;
        }

        public bool UserExists(int userId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var chk = new SqlCommand("SELECT COUNT(*) FROM Users WHERE UserId=@id", conn);
            chk.Parameters.AddWithValue("@id", userId);
            return Convert.ToInt32(chk.ExecuteScalar()) > 0;
        }

        public void Insert(Session session)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"
                INSERT INTO Sessions (RequestId, TutorId, ScheduledAt, Status)
                VALUES (@rid, @tid, @scheduled, @status)", conn);

            cmd.Parameters.AddWithValue("@rid", session.RequestId);
            cmd.Parameters.AddWithValue("@tid", session.TutorId);
            cmd.Parameters.AddWithValue("@scheduled", (object?)session.ScheduledAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@status", (object?)session.Status ?? "PENDING");

            cmd.ExecuteNonQuery();
        }

        public List<Session> GetAll()
        {
            var list = new List<Session>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Sessions", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new Session
                {
                    SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    TutorId = reader.GetInt32(reader.GetOrdinal("TutorId")),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ScheduledAt")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
            return list;
        }

        public Session? GetById(int id)
        {
            Session? data = null;
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Sessions WHERE SessionId=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                data = new Session
                {
                    SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    TutorId = reader.GetInt32(reader.GetOrdinal("TutorId")),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ScheduledAt")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                };
            }
            return data;
        }

        public List<Session> GetByTutorId(int tutorId)
        {
            var list = new List<Session>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT * FROM Sessions WHERE TutorId=@id", conn);
            cmd.Parameters.AddWithValue("@id", tutorId);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Session
                {
                    SessionId = reader.GetInt32(reader.GetOrdinal("SessionId")),
                    RequestId = reader.GetInt32(reader.GetOrdinal("RequestId")),
                    TutorId = reader.GetInt32(reader.GetOrdinal("TutorId")),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? null : reader.GetDateTime(reader.GetOrdinal("ScheduledAt")),
                    Status = reader.GetString(reader.GetOrdinal("Status")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                });
            }
            return list;
        }

        public void UpdateStatus(int sessionId, string status)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("UPDATE Sessions SET Status=@status WHERE SessionId=@id", conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", sessionId);
            cmd.ExecuteNonQuery();
        }

        public void Delete(int sessionId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM Sessions WHERE SessionId=@id", conn);
            cmd.Parameters.AddWithValue("@id", sessionId);
            cmd.ExecuteNonQuery();
        }
    }
}
