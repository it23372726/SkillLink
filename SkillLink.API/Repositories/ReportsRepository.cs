using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models.Reports;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class ReportsRepository : IReportsRepository
    {
        private readonly DbHelper _db;

        public ReportsRepository(DbHelper db) => _db = db;

        public List<SkillDemandRow> GetTopRequestedSkills(DateTime? from, DateTime? to, int limit)
        {
            var list = new List<SkillDemandRow>();
            using var conn = _db.GetConnection();
            conn.Open();

            var sql = @"
                SELECT TOP (@limit)
                       LTRIM(RTRIM(SkillName)) AS SkillName,
                       COUNT(*) AS TotalRequests,
                       SUM(CASE WHEN Status = 'SCHEDULED' THEN 1 ELSE 0 END) AS Scheduled,
                       SUM(CASE WHEN Status = 'COMPLETED' THEN 1 ELSE 0 END) AS Completed,
                       MIN(CreatedAt) AS FirstRequestAt,
                       MAX(CreatedAt) AS LastRequestAt
                FROM Requests
                WHERE SkillName IS NOT NULL AND LTRIM(RTRIM(SkillName)) <> ''
                  AND (@from IS NULL OR CreatedAt >= @from)
                  AND (@to   IS NULL OR CreatedAt <  @to)
                GROUP BY LTRIM(RTRIM(SkillName))
                ORDER BY TotalRequests DESC, SkillName ASC;";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@limit", limit);
            cmd.Parameters.AddWithValue("@from", (object?)from ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@to", (object?)to ?? DBNull.Value);

            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                list.Add(new SkillDemandRow
                {
                    SkillName = rd.GetString(rd.GetOrdinal("SkillName")),
                    TotalRequests = rd.GetInt32(rd.GetOrdinal("TotalRequests")),
                    Scheduled = rd.GetInt32(rd.GetOrdinal("Scheduled")),
                    Completed = rd.GetInt32(rd.GetOrdinal("Completed")),
                    FirstRequestAt = rd.IsDBNull(rd.GetOrdinal("FirstRequestAt")) ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("FirstRequestAt")),
                    LastRequestAt  = rd.IsDBNull(rd.GetOrdinal("LastRequestAt"))  ? (DateTime?)null : rd.GetDateTime(rd.GetOrdinal("LastRequestAt")),
                });
            }

            return list;
        }
    }
}
