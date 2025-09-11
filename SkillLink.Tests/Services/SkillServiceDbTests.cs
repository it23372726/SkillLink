using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Services;
using SkillLink.API.Models;

// SkillService/AuthService/EmailService/DbHelper and Models are in the API project

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class SkillServiceDbTests
    {
    private Testcontainers.MySql.MySqlContainer _mysql = null!;
    private bool _ownsContainer = false;
    private string? _externalConnStr = null;
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private EmailService _email = null!;
        private AuthService _auth = null!;
        private SkillService _sut = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // If external connection string provided, use it; else try Docker; else skip
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _externalConnStr = external;
            }

            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sock1 = "/var/run/docker.sock";
            var sock2 = Path.Combine(home, ".docker/run/docker.sock");
            var dockerSocketExists = File.Exists(sock1) || File.Exists(sock2);
            if (!(_externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost)))
            {
                Assert.Ignore("Docker not available. Skipping SkillService DB integration tests.");
                return;
            }

            if (_externalConnStr == null)
            {
                try
                {
                    _mysql = new Testcontainers.MySql.MySqlBuilder()
                        .WithImage("mysql:8.0")
                        .WithDatabase("skilllink_test")
                        .WithUsername("testuser")
                        .WithPassword("testpass")
                        .Build();
                    await _mysql.StartAsync();
                    _ownsContainer = true;
                }
                catch (Exception ex)
                {
                    Assert.Ignore($"Docker not available or failed to start MySQL container. Skipping DB tests. Details: {ex.Message}");
                    return;
                }
            }

            var connStr = _externalConnStr ?? _mysql.GetConnectionString();

            // Create schema for Users, Skills, and UserSkills
            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS Users (
                      UserId INT AUTO_INCREMENT PRIMARY KEY,
                      FullName VARCHAR(255) NOT NULL,
                      Email VARCHAR(255) NOT NULL UNIQUE,
                      PasswordHash VARCHAR(255) NOT NULL,
                      Role VARCHAR(50) NOT NULL,
                      ProfilePicture TEXT NULL,
                      CreatedAt DATETIME NOT NULL,
                      Bio TEXT NULL,
                      Location VARCHAR(255) NULL,
                      ReadyToTeach TINYINT(1) NOT NULL DEFAULT 0,
                      IsActive TINYINT(1) NOT NULL DEFAULT 1,
                      EmailVerified TINYINT(1) NOT NULL DEFAULT 0,
                      EmailVerificationToken VARCHAR(255) NULL,
                      EmailVerificationExpires DATETIME NULL
                    );
                    CREATE TABLE IF NOT EXISTS Skills (
                      SkillId INT AUTO_INCREMENT PRIMARY KEY,
                      Name VARCHAR(255) NOT NULL UNIQUE,
                      IsPredefined TINYINT(1) NOT NULL DEFAULT 0
                    );
                    CREATE TABLE IF NOT EXISTS UserSkills (
                      UserSkillId INT AUTO_INCREMENT PRIMARY KEY,
                      UserId INT NOT NULL,
                      SkillId INT NOT NULL,
                      Level VARCHAR(50) NOT NULL,
                      UNIQUE KEY uq_user_skill (UserId, SkillId)
                    );
                ";
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connStr },
                    // Dummy SMTP/JWT to satisfy constructors if ever used
                    { "Smtp:Host", "localhost" },
                    { "Smtp:Port", "2525" },
                    { "Smtp:User", "user" },
                    { "Smtp:Pass", "pass" },
                    { "Smtp:From", "noreply@example.com" },
                    { "Jwt:Key", "testkeytestkeytestkeytestkey" },
                    { "Jwt:Issuer", "test" },
                    { "Jwt:Audience", "test" },
                    { "Jwt:ExpireMinutes", "60" }
                })
                .Build();

            _dbHelper = new DbHelper(_config);
            _email = new EmailService(_config);
            _auth = new AuthService(_dbHelper, _config, _email);
            _sut = new SkillService(_dbHelper, _auth);
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown()
        {
            if (_ownsContainer && _mysql != null)
            {
                await _mysql.DisposeAsync();
            }
        }

        [SetUp]
        public async Task Setup()
        {
            // Clean tables between tests
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var sql = string.Join(" ", new[]
            {
                "DELETE FROM UserSkills;",
                "DELETE FROM Skills;",
                "DELETE FROM Users;",
                "ALTER TABLE UserSkills AUTO_INCREMENT = 1;",
                "ALTER TABLE Skills AUTO_INCREMENT = 1;",
                "ALTER TABLE Users AUTO_INCREMENT = 1;"
            });
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, string fullName, string email, string role = "Learner")
        {
            var cmd = new MySqlCommand(@"INSERT INTO Users
                (FullName, Email, PasswordHash, Role, ProfilePicture, CreatedAt, Bio, Location, ReadyToTeach, IsActive, EmailVerified)
                VALUES (@n, @e, @ph, @r, NULL, @c, NULL, NULL, 0, 1, 1);
                SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", fullName);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@ph", Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("pass")));
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@c", DateTime.UtcNow);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        private async Task<int> InsertSkillAsync(MySqlConnection conn, string name, bool predefined = false)
        {
            var cmd = new MySqlCommand("INSERT INTO Skills (Name, IsPredefined) VALUES (@n, @p); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@p", predefined ? 1 : 0);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        private async Task<int> InsertUserSkillAsync(MySqlConnection conn, int userId, int skillId, string level)
        {
            var cmd = new MySqlCommand("INSERT INTO UserSkills (UserId, SkillId, Level) VALUES (@u, @s, @l); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@u", userId);
            cmd.Parameters.AddWithValue("@s", skillId);
            cmd.Parameters.AddWithValue("@l", level);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task AddSkill_ShouldInsertNewSkill_AndMapping_AndUpsertLevel()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Alice", "alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = userId, SkillName = "CSharp", Level = "Intermediate" });

            // call again with new level to exercise ON DUPLICATE KEY UPDATE
            _sut.AddSkill(new AddSkillRequest { UserId = userId, SkillName = "CSharp", Level = "Advanced" });

            // Verify: one Skill row, one mapping with updated level
            var check = new MySqlCommand("SELECT COUNT(*) FROM Skills WHERE Name='CSharp'", conn);
            (Convert.ToInt32(await check.ExecuteScalarAsync())).Should().Be(1);

            var levelCmd = new MySqlCommand(@"SELECT us.Level FROM UserSkills us JOIN Skills s ON us.SkillId=s.SkillId WHERE us.UserId=@u AND s.Name='CSharp'", conn);
            levelCmd.Parameters.AddWithValue("@u", userId);
            var level = (string)(await levelCmd.ExecuteScalarAsync())!;
            level.Should().Be("Advanced");
        }

        [Test]
        public async Task DeleteUserSkill_ShouldRemoveMapping()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Bob", "bob@example.com");
            var skillId = await InsertSkillAsync(conn, "Python");
            var usId = await InsertUserSkillAsync(conn, userId, skillId, "Beginner");

            _sut.DeleteUserSkill(userId, skillId);

            var left = new MySqlCommand("SELECT COUNT(*) FROM UserSkills WHERE UserId=@u AND SkillId=@s", conn);
            left.Parameters.AddWithValue("@u", userId);
            left.Parameters.AddWithValue("@s", skillId);
            (Convert.ToInt32(await left.ExecuteScalarAsync())).Should().Be(0);
        }

        [Test]
        public async Task GetUserSkills_ShouldReturnJoinedSkills()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Cara", "cara@example.com");
            var s1 = await InsertSkillAsync(conn, "SQL", true);
            var s2 = await InsertSkillAsync(conn, "Docker", false);
            await InsertUserSkillAsync(conn, userId, s1, "Advanced");
            await InsertUserSkillAsync(conn, userId, s2, "Intermediate");

            var list = _sut.GetUserSkills(userId);
            list.Should().HaveCount(2);
            list.Select(x => x.Skill!.Name).Should().BeEquivalentTo(new[] { "SQL", "Docker" });
            list.First(x => x.Skill!.Name == "SQL").Skill!.IsPredefined.Should().BeTrue();
        }

        [Test]
        public async Task SuggestSkills_ShouldRespectPrefixAndLimit()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            // Insert 12 matching + 2 non-matching
            for (int i = 0; i < 12; i++)
            {
                await InsertSkillAsync(conn, $"Data{i}");
            }
            await InsertSkillAsync(conn, "Other1");
            await InsertSkillAsync(conn, "Other2");

            var result = _sut.SuggestSkills("Data");
            result.Count.Should().BeLessThanOrEqualTo(10);
            result.Should().OnlyContain(s => s.Name.StartsWith("Data", StringComparison.OrdinalIgnoreCase));
        }

        [Test]
        public async Task GetUsersBySkill_ShouldReturnUsersMatchingSkillPrefix()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var u1 = await InsertUserAsync(conn, "Drew", "drew@example.com");
            var u2 = await InsertUserAsync(conn, "Elle", "elle@example.com");
            var u3 = await InsertUserAsync(conn, "Finn", "finn@example.com");
            var sCSharp = await InsertSkillAsync(conn, "CSharp");
            var sJava = await InsertSkillAsync(conn, "Java");
            await InsertUserSkillAsync(conn, u1, sCSharp, "Intermediate");
            await InsertUserSkillAsync(conn, u2, sCSharp, "Beginner");
            await InsertUserSkillAsync(conn, u3, sJava, "Advanced");

            var users = _sut.GetUsersBySkill("CSh");
            users.Select(u => u.Email).Should().BeEquivalentTo(new[] { "drew@example.com", "elle@example.com" });
            users.Should().OnlyContain(u => !string.IsNullOrWhiteSpace(u.FullName) && !string.IsNullOrWhiteSpace(u.Email));
        }
    }
}
