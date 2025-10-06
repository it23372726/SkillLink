using Microsoft.Data.SqlClient;

namespace SkillLink.API.Data
{
    public class DbHelper
    {
        private readonly string _connectionString;

        public DbHelper(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection")!;
        }

        public SqlConnection GetConnection() => new SqlConnection(_connectionString);
    }
}
