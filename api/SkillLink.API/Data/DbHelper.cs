using MySql.Data.MySqlClient;

public class DbHelper
{
    private readonly IConfiguration _config;
    private readonly string _connectionString;

    public DbHelper(IConfiguration config)
    {
        _config = config;
        _connectionString = _config.GetConnectionString("DefaultConnection");
    }

    public MySqlConnection GetConnection()
    {
        return new MySqlConnection(_connectionString);
    }
}
