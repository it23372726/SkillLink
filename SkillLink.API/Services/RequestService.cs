using MySql.Data.MySqlClient;

public class RequestService
{
    private readonly DbHelper _dbHelper;

    public RequestService(DbHelper dbHelper){
        _dbHelper = dbHelper;
    }

    //GET by Id
    public Request? GetById(int requestId){
        Request? data =null;
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand("SELECT * FROM Requests WHERE RequestId = @requestid", conn);
        cmd.Parameters.AddWithValue("@requestid", requestId);
        using var reader = cmd.ExecuteReader();
        if(reader.Read()){
            data = new Request{
                    RequestId = reader.GetInt32("RequestId"),
                    LearnerId = reader.GetInt32("LearnerId"),
                    SkillName = reader.GetString("SkillName"),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description")
            };
        }
        return data;


    }

    //GET all request by learnerId
    public List<Request> GetByLearnerId(int learnerId){
        var list = new List<Request>();
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand("SELECT * FROM Requests WHERE LearnerId = @learnerId", conn);
        cmd.Parameters.AddWithValue("@learnerId",learnerId);
        using var reader = cmd.ExecuteReader();
        while(reader.Read()){
            list.Add(
                new Request
                {
                    RequestId = reader.GetInt32("RequestId"),
                    LearnerId = reader.GetInt32("LearnerId"),
                    SkillName = reader.GetString("SkillName"),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description")
                }
            );
        }
        return list;
    }

    //GET all request
    public List<Request> GetAllRequests(){
        var list = new List<Request>();
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        using var cmd = new MySqlCommand("SELECT * FROM Requests", conn);
        using var reader = cmd.ExecuteReader();
        while(reader.Read()){
            list.Add(
                new Request
                {
                    RequestId = reader.GetInt32("RequestId"),
                    LearnerId = reader.GetInt32("LearnerId"),
                    SkillName = reader.GetString("SkillName"),
                    Topic = reader.IsDBNull(reader.GetOrdinal("Topic")) ? null : reader.GetString("Topic"),
                    Status = reader.GetString("Status"),
                    CreatedAt = reader.GetDateTime("CreatedAt"),
                    Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? null : reader.GetString("Description")
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
        var cmd = new MySqlCommand("INSERT INTO Requests (LearnerId, SkillName, Topic, Description) VALUES (@learnerId, @skillName, @topic, @description)", conn);
        cmd.Parameters.AddWithValue("@learnerId", req.LearnerId);
        cmd.Parameters.AddWithValue("@skillName", req.SkillName);
        cmd.Parameters.AddWithValue("@topic", req.Topic);
        cmd.Parameters.AddWithValue("@description", req.Description);
        cmd.ExecuteNonQuery();
    }

     // PATCH update status
    public void UpdateStatus(int requestId, string status)
    {
        using var conn = _dbHelper.GetConnection();
        conn.Open();
        var cmd = new MySqlCommand(
            "UPDATE Requests SET Status=@status WHERE RequestId=@id", conn);
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

}