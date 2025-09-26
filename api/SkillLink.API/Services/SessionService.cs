using MySql.Data.MySqlClient;
using SkillLink.API.Models;

namespace SkillLink.API.Services
{
    public class SessionService
    {
        private readonly DbHelper _dbHelper;

        public SessionService(DbHelper dbHelper)
        {
            _dbHelper = dbHelper;
        }

        // Create
        public void AddSession(Session session)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand(
                "INSERT INTO Sessions (RequestId, TutorId, ScheduledAt, Status) VALUES (@requestId, @tutorId, @scheduledAt, @status)", conn);

            cmd.Parameters.AddWithValue("@requestId", session.RequestId);
            cmd.Parameters.AddWithValue("@tutorId", session.TutorId);
            cmd.Parameters.AddWithValue("@scheduledAt", session.ScheduledAt ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("@status", session.Status);

            cmd.ExecuteNonQuery();
        }

        // Get All
        public List<Session> GetAllSessions()
        {
            var list = new List<Session>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM Sessions", conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new Session
                {
                    SessionId = reader.GetInt32("SessionId"),
                    RequestId = reader.GetInt32("RequestId"),
                    TutorId = reader.GetInt32("TutorId"),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? null : reader.GetDateTime("ScheduledAt"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }
            return list;
        }

        // Get By Id
        public Session? GetById(int id)
        {
            Session? data = null;
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM Sessions WHERE SessionId=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                data = new Session
                {
                    SessionId = reader.GetInt32("SessionId"),
                    RequestId = reader.GetInt32("RequestId"),
                    TutorId = reader.GetInt32("TutorId"),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? null : reader.GetDateTime("ScheduledAt"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                };
            }
            return data;
        }
        // Get By Tutor Id
        public List<Session>? GetByTutorId(int id){
            var list = new List<Session>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            using var cmd = new MySqlCommand("SELECT * FROM Sessions WHERE TutorId=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Session{
                    SessionId = reader.GetInt32("SessionId"),
                    RequestId = reader.GetInt32("RequestId"),
                    TutorId = reader.GetInt32("TutorId"),
                    ScheduledAt = reader.IsDBNull(reader.GetOrdinal("ScheduledAt")) ? null : reader.GetDateTime("ScheduledAt"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt")
                });
            }
            return list;
        }

        // Update Status
        public void UpdateStatus(int sessionId, string status)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand("UPDATE Sessions SET Status=@status WHERE SessionId=@id", conn);
            cmd.Parameters.AddWithValue("@status", status);
            cmd.Parameters.AddWithValue("@id", sessionId);
            cmd.ExecuteNonQuery();
        }

        // Delete
        public void Delete(int sessionId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();
            var cmd = new MySqlCommand("DELETE FROM Sessions WHERE SessionId=@id", conn);
            cmd.Parameters.AddWithValue("@id", sessionId);
            cmd.ExecuteNonQuery();
        }
    }
}
