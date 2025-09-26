using MySql.Data.MySqlClient;
using SkillLink.API.Models;

namespace SkillLink.API.Services
{
    public class SkillService
    {
        private readonly DbHelper _dbHelper;
        private readonly AuthService _authService;

        public SkillService(DbHelper dbHelper, AuthService authService)
        {
            _dbHelper = dbHelper;
            _authService = authService;
        }

        // Add Skill to a User
        public void AddSkill(AddSkillRequest req)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            // 1. Check if skill already exists
            int skillId;
            using (var checkCmd = new MySqlCommand("SELECT SkillId FROM Skills WHERE Name=@name", conn))
            {
                checkCmd.Parameters.AddWithValue("@name", req.SkillName);
                var result = checkCmd.ExecuteScalar();
                if (result != null)
                {
                    skillId = Convert.ToInt32(result);
                }
                else
                {
                    // Insert new skill
                    using var insertCmd = new MySqlCommand("INSERT INTO Skills (Name, IsPredefined) VALUES (@name, 0)", conn);
                    insertCmd.Parameters.AddWithValue("@name", req.SkillName);
                    insertCmd.ExecuteNonQuery();
                    skillId = (int)insertCmd.LastInsertedId;
                }
            }

            // 2. Add mapping into UserSkills
            using var mapCmd = new MySqlCommand(
                "INSERT INTO UserSkills (UserId, SkillId, Level) VALUES (@uid, @sid, @level) ON DUPLICATE KEY UPDATE Level=@level",
                conn
            );
            mapCmd.Parameters.AddWithValue("@uid", req.UserId);
            mapCmd.Parameters.AddWithValue("@sid", skillId);
            mapCmd.Parameters.AddWithValue("@level", req.Level);
            mapCmd.ExecuteNonQuery();
        }

        // Delete Skill from a User
        public void DeleteUserSkill(int userId, int skillId)
        {
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand("DELETE FROM UserSkills WHERE UserId=@uid AND SkillId=@sid", conn);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@sid", skillId);
            cmd.ExecuteNonQuery();
        }

        // Get all skills of a user
        public List<UserSkill> GetUserSkills(int userId)
        {
            var list = new List<UserSkill>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand(@"
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
                    UserSkillId = reader.GetInt32("UserSkillId"),
                    SkillId = reader.GetInt32("SkillId"),
                    UserId = userId,
                    Level = reader.GetString("Level"),
                    Skill = new Skill
                    {
                        SkillId = reader.GetInt32("SkillId"),
                        Name = reader.GetString("Name"),
                        IsPredefined = reader.GetBoolean("IsPredefined")
                    }
                });
            }
            return list;
        }

        // Suggest skills (autocomplete)
        public List<Skill> SuggestSkills(string query)
        {
            var list = new List<Skill>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand("SELECT * FROM Skills WHERE Name LIKE @q LIMIT 10", conn);
            cmd.Parameters.AddWithValue("@q", query + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Skill
                {
                    SkillId = reader.GetInt32("SkillId"),
                    Name = reader.GetString("Name"),
                    IsPredefined = reader.GetBoolean("IsPredefined")
                });
            }
            return list;
        }

        // Filter users by skill
        public List<User> GetUsersBySkill(string query)
        {
            var users = new List<User>();
            using var conn = _dbHelper.GetConnection();
            conn.Open();

            using var cmd = new MySqlCommand("SELECT us.UserId FROM UserSkills us JOIN Skills s ON us.SkillId = s.SkillId WHERE s.Name LIKE @name LIMIT 10", conn);
            cmd.Parameters.AddWithValue("@name", query + "%");

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add( _authService.GetUserById(reader.GetInt32("UserId")));
                
            }
            return users;
        }
    }
}
