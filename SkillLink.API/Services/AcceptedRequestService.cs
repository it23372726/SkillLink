using MySql.Data.MySqlClient;
using System;

public class AcceptedRequestService
{
    private readonly DbHelper _dbHelper;

    public AcceptedRequestService(DbHelper dbHelper)
    {
        _dbHelper = dbHelper;
    }

    // Accept a request
    public void AcceptRequest(int requestId, int acceptorId)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        
        // Check if already accepted
        using var checkCmd = new MySqlCommand(
            "SELECT COUNT(*) FROM AcceptedRequests WHERE RequestId = @requestId AND AcceptorId = @acceptorId", 
            conn
        );
        checkCmd.Parameters.AddWithValue("@requestId", requestId);
        checkCmd.Parameters.AddWithValue("@acceptorId", acceptorId);
        
        var count = Convert.ToInt32(checkCmd.ExecuteScalar());
        if (count > 0)
        {
            throw new Exception("Request already accepted");
        }

        // Insert acceptance
        using var cmd = new MySqlCommand(
            "INSERT INTO AcceptedRequests (RequestId, AcceptorId) VALUES (@requestId, @acceptorId)", 
            conn
        );
        cmd.Parameters.AddWithValue("@requestId", requestId);
        cmd.Parameters.AddWithValue("@acceptorId", acceptorId);
        cmd.ExecuteNonQuery();
    }

    // Get accepted requests by user
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
            
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AcceptedRequestWithDetails
            {
                AcceptedRequestId = reader.GetInt32("AcceptedRequestId"),
                RequestId = reader.GetInt32("RequestId"),
                AcceptorId = reader.GetInt32("AcceptorId"),
                AcceptedAt = reader.GetDateTime("AcceptedAt"),
                Status = reader.GetString("Status"),
                ScheduleDate = reader.IsDBNull(reader.GetOrdinal("ScheduleDate")) ? null : (DateTime?)reader.GetDateTime("ScheduleDate"),
                MeetingType = reader.IsDBNull(reader.GetOrdinal("MeetingType")) ? null : reader.GetString("MeetingType"),
                MeetingLink = reader.IsDBNull(reader.GetOrdinal("MeetingLink")) ? null : reader.GetString("MeetingLink"),
                SkillName = reader.GetString("SkillName"),
                Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                RequesterName = reader.GetString("RequesterName"),
                RequesterEmail = reader.GetString("RequesterEmail"),
                RequesterId = reader.GetInt32("RequesterId")
            });
        }
        
        return list;
    }

    // Update acceptance status
    public void UpdateAcceptanceStatus(int acceptedRequestId, string status)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        
        using var cmd = new MySqlCommand(
            "UPDATE AcceptedRequests SET Status = @status WHERE AcceptedRequestId = @id", 
            conn
        );
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@id", acceptedRequestId);
        cmd.ExecuteNonQuery();
    }

    // Check if user has accepted a request
    public bool HasUserAcceptedRequest(int userId, int requestId)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        
        using var cmd = new MySqlCommand(
            "SELECT COUNT(*) FROM AcceptedRequests WHERE AcceptorId = @userId AND RequestId = @requestId", 
            conn
        );
        cmd.Parameters.AddWithValue("@userId", userId);
        cmd.Parameters.AddWithValue("@requestId", requestId);
        
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        return count > 0;
    }

    // Schedule a meeting for an accepted request
    public void ScheduleMeeting(int acceptedRequestId, DateTime scheduleDate, string meetingType, string meetingLink)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        
        using var cmd = new MySqlCommand(
            "UPDATE AcceptedRequests SET ScheduleDate = @scheduleDate, MeetingType = @meetingType, MeetingLink = @meetingLink, Status = 'SCHEDULED' WHERE AcceptedRequestId = @id", 
            conn
        );
        cmd.Parameters.AddWithValue("@scheduleDate", scheduleDate);
        cmd.Parameters.AddWithValue("@meetingType", meetingType);
        cmd.Parameters.AddWithValue("@meetingLink", meetingLink);
        cmd.Parameters.AddWithValue("@id", acceptedRequestId);
        cmd.ExecuteNonQuery();
    }

    // Get requests that the user asked for (as requester)
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
            
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@userId", userId);
        
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new AcceptedRequestWithDetails
            {
                AcceptedRequestId = reader.GetInt32("AcceptedRequestId"),
                RequestId = reader.GetInt32("RequestId"),
                AcceptorId = reader.GetInt32("AcceptorId"),
                AcceptedAt = reader.GetDateTime("AcceptedAt"),
                Status = reader.GetString("Status"),
                ScheduleDate = reader.IsDBNull(reader.GetOrdinal("ScheduleDate")) ? null : (DateTime?)reader.GetDateTime("ScheduleDate"),
                MeetingType = reader.IsDBNull(reader.GetOrdinal("MeetingType")) ? null : reader.GetString("MeetingType"),
                MeetingLink = reader.IsDBNull(reader.GetOrdinal("MeetingLink")) ? null : reader.GetString("MeetingLink"),
                SkillName = reader.GetString("SkillName"),
                Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                RequesterName = reader.GetString("AcceptorName"), // This is actually the acceptor's name
                RequesterEmail = reader.GetString("AcceptorEmail") // This is actually the acceptor's email
            });
        }
        
        return list;
    }
}