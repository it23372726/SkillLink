using MySql.Data.MySqlClient;

public class RequestService
{
    private readonly DbHelper _dbHelper;

    public RequestService(DbHelper dbHelper){
        _dbHelper = dbHelper;
    }

    //GET by Id with user info
    public RequestWithUser? GetById(int requestId){
        RequestWithUser? data = null;
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            "SELECT r.*, u.FullName, u.Email FROM Requests r JOIN Users u ON r.LearnerId = u.UserId WHERE r.RequestId = @requestid", 
            conn
        );
        cmd.Parameters.AddWithValue("@requestid", requestId);
        using var reader = cmd.ExecuteReader();
        if(reader.Read()){
            data = new RequestWithUser {
                RequestId = reader.GetInt32("RequestId"),
                LearnerId = reader.GetInt32("LearnerId"),
                SkillName = reader.GetString("SkillName"),
                Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                Status = reader.GetString("Status"),
                CreatedAt = reader.GetDateTime("CreatedAt"),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                FullName = reader.GetString("FullName"),
                Email = reader.GetString("Email")
            };
        }
        return data;
    }

    //GET all request by learnerId with user info
    public List<RequestWithUser> GetByLearnerId(int learnerId){
        var list = new List<RequestWithUser>();
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            "SELECT r.*, u.FullName, u.Email FROM Requests r JOIN Users u ON r.LearnerId = u.UserId WHERE r.LearnerId = @learnerId", 
            conn
        );
        cmd.Parameters.AddWithValue("@learnerId",learnerId);
        using var reader = cmd.ExecuteReader();
        while(reader.Read()){
            list.Add(
                new RequestWithUser
                {
                    RequestId = reader.GetInt32("RequestId"),
                    LearnerId = reader.GetInt32("LearnerId"),
                    SkillName = reader.GetString("SkillName"),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email")
                }
            );
        }
        return list;
    }

    //GET all requests with user info
    public List<RequestWithUser> GetAllRequests(){
        var list = new List<RequestWithUser>();
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand(
            "SELECT r.*, u.FullName, u.Email FROM Requests r JOIN Users u ON r.LearnerId = u.UserId ORDER BY r.CreatedAt DESC", 
            conn
        );
        using var reader = cmd.ExecuteReader();
        while(reader.Read()){
            list.Add(
                new RequestWithUser
                {
                    RequestId = reader.GetInt32("RequestId"),
                    LearnerId = reader.GetInt32("LearnerId"),
                    SkillName = reader.GetString("SkillName"),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email")
                }
            );
        }
        return list;
    }

    // POST create request
    public void AddRequest(Request req)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        var cmd = new MySqlCommand(
            "INSERT INTO Requests (LearnerId, SkillName, Topic, Description) VALUES (@learnerId, @skillName, @topic, @description)", 
            conn
        );
        cmd.Parameters.AddWithValue("@learnerId", req.LearnerId);
        cmd.Parameters.AddWithValue("@skillName", req.SkillName);
        cmd.Parameters.AddWithValue("@topic", req.Topic);
        cmd.Parameters.AddWithValue("@description", req.Description);
        cmd.ExecuteNonQuery();
    }

    // PUT update entire request
    public void UpdateRequest(int requestId, Request req)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        var cmd = new MySqlCommand(
            "UPDATE Requests SET SkillName=@skillName, Topic=@topic, Description=@description WHERE RequestId=@id", 
            conn
        );
        cmd.Parameters.AddWithValue("@skillName", req.SkillName);
        cmd.Parameters.AddWithValue("@topic", (object)req.Topic ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@description", (object)req.Description ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@id", requestId);
        cmd.ExecuteNonQuery();
    }

    // PATCH update status only
    public void UpdateStatus(int requestId, string status)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        var cmd = new MySqlCommand(
            "UPDATE Requests SET Status=@status WHERE RequestId=@id", 
            conn
        );
        cmd.Parameters.AddWithValue("@status", status);
        cmd.Parameters.AddWithValue("@id", requestId);
        cmd.ExecuteNonQuery();
    }

    // DELETE request
    public void DeleteRequest(int requestId)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        var cmd = new MySqlCommand("DELETE FROM Requests WHERE RequestId=@id", conn);
        cmd.Parameters.AddWithValue("@id", requestId);
        cmd.ExecuteNonQuery();
    }

    // Search requests by skill, topic, or user name
    public List<RequestWithUser> SearchRequests(string query)
    {
        var list = new List<RequestWithUser>();
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        
        var sql = @"
            SELECT r.*, u.FullName, u.Email 
            FROM Requests r 
            JOIN Users u ON r.LearnerId = u.UserId 
            WHERE r.SkillName LIKE @query 
               OR r.Topic LIKE @query 
               OR r.Description LIKE @query 
               OR u.FullName LIKE @query 
            ORDER BY r.CreatedAt DESC";
            
        using var cmd = new MySqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@query", $"%{query}%");
        
        using var reader = cmd.ExecuteReader();
        while(reader.Read()){
            list.Add(
                new RequestWithUser
                {
                    RequestId = reader.GetInt32("RequestId"),
                    LearnerId = reader.GetInt32("LearnerId"),
                    SkillName = reader.GetString("SkillName"),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description"),
                    FullName = reader.GetString("FullName"),
                    Email = reader.GetString("Email")
                }
            );
        }
        return list;
    }
}