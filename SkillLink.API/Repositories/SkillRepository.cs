using Microsoft.Data.SqlClient;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories.Abstractions;

namespace SkillLink.API.Repositories
{
    public class SkillRepository : ISkillRepository
    {
        private readonly DbHelper _db;
        public SkillRepository(DbHelper db) => _db = db;

        public int? GetSkillIdByName(string name)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT SkillId FROM Skills WHERE Name=@name", conn);
            cmd.Parameters.AddWithValue("@name", name);
            var res = cmd.ExecuteScalar();
            return res == null ? (int?)null : Convert.ToInt32(res);
        }

        public int InsertSkill(string name, bool isPredefined = false)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(
                @"INSERT INTO Skills (Name, IsPredefined) VALUES (@name, @p);
                  SELECT CAST(SCOPE_IDENTITY() as int);", conn);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@p", isPredefined ? 1 : 0);
            var id = cmd.ExecuteScalar();
            return Convert.ToInt32(id);
        }

        public void UpsertUserSkill(int userId, int skillId, string level)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            // UPDATE first, then INSERT if not exists
            var sql = @"
                UPDATE UserSkills SET Level=@level 
                WHERE UserId=@uid AND SkillId=@sid;

                IF @@ROWCOUNT = 0
                BEGIN
                    INSERT INTO UserSkills (UserId, SkillId, Level) VALUES (@uid, @sid, @level);
                END";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@sid", skillId);
            cmd.Parameters.AddWithValue("@level", level);
            cmd.ExecuteNonQuery();
        }

        public void DeleteUserSkill(int userId, int skillId)
        {
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("DELETE FROM UserSkills WHERE UserId=@uid AND SkillId=@sid", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@sid", skillId);
            cmd.ExecuteNonQuery();
        }

        public List<UserSkill> GetUserSkillsWithSkill(int userId)
        {
            var list = new List<UserSkill>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"
                SELECT us.UserSkillId, us.Level, s.SkillId, s.Name, s.IsPredefined
                FROM UserSkills us
                JOIN Skills s ON us.SkillId = s.SkillId
                WHERE us.UserId=@uid", conn);
            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new UserSkill
                {
                    UserSkillId = reader.GetInt32(reader.GetOrdinal("UserSkillId")),
                    SkillId = reader.GetInt32(reader.GetOrdinal("SkillId")),
                    UserId = userId,
                    Level = reader.GetString(reader.GetOrdinal("Level")),
                    Skill = new Skill
                    {
                        SkillId = reader.GetInt32(reader.GetOrdinal("SkillId")),
                        Name = reader.GetString(reader.GetOrdinal("Name")),
                        IsPredefined = reader.GetBoolean(reader.GetOrdinal("IsPredefined"))
                    }
                });
            }
            return list;
        }

        public List<Skill> SuggestSkillsByPrefix(string prefix)
        {
            var list = new List<Skill>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand("SELECT TOP 10 * FROM Skills WHERE Name LIKE @q", conn);
            cmd.Parameters.AddWithValue("@q", prefix + "%");
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Skill
                {
                    SkillId = reader.GetInt32(reader.GetOrdinal("SkillId")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    IsPredefined = reader.GetBoolean(reader.GetOrdinal("IsPredefined"))
                });
            }
            return list;
        }

        public List<int> GetUserIdsBySkillPrefix(string prefix)
        {
            var list = new List<int>();
            using var conn = _db.GetConnection();
            conn.Open();
            using var cmd = new SqlCommand(@"
                SELECT DISTINCT TOP 10 us.UserId 
                FROM UserSkills us 
                JOIN Skills s ON us.SkillId = s.SkillId 
                WHERE s.Name LIKE @name", conn);
            cmd.Parameters.AddWithValue("@name", prefix + "%");
            using var r = cmd.ExecuteReader();
            while (r.Read()) list.Add(r.GetInt32(r.GetOrdinal("UserId")));
            return list;
        }
    }
}
