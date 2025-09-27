using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Services;

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
        private SkillService _sut = null!;
        private AuthService _auth = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // Allow external DB via env var to avoid Docker on some machines
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _externalConnStr = external;
            }

            // Detect Docker availability
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

            // Create schema
            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS Users (
                      UserId INT AUTO_INCREMENT PRIMARY KEY,
                      FullName VARCHAR(255) NOT NULL,
                      Email VARCHAR(255) NOT NULL UNIQUE,
                      PasswordHash VARCHAR(255) NULL,
                      Role VARCHAR(50) NOT NULL DEFAULT 'Learner',
                      CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                      Bio TEXT NULL,
                      Location VARCHAR(255) NULL,
                      ProfilePicture VARCHAR(512) NULL,
                      ReadyToTeach TINYINT(1) NOT NULL DEFAULT 0,
                      IsActive TINYINT(1) NOT NULL DEFAULT 1,
                      EmailVerified TINYINT(1) NOT NULL DEFAULT 1
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
                      UNIQUE KEY uk_user_skill (UserId, SkillId),
                      FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
                      FOREIGN KEY (SkillId) REFERENCES Skills(SkillId) ON DELETE CASCADE
                    );
                ";
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Minimal config for AuthService constructor
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connStr },
                    // Jwt settings not needed for GetUserById, but AuthService ctor uses IConfiguration
                    { "Jwt:Key", "x".PadLeft(32,'x') }, 
                    { "Jwt:Issuer", "SkillLink" },
                    { "Jwt:Audience", "SkillLink" },
                    { "Jwt:ExpireMinutes", "60" }
                })
                .Build();

            _dbHelper = new DbHelper(_config);

            // EmailService not needed for these tests, but AuthService requires it
            var dummyEmail = new EmailService(_config);
            _auth = new AuthService(_dbHelper, _config, dummyEmail);

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
            // Clean tables
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var sql = @"
                DELETE FROM UserSkills;
                DELETE FROM Skills;
                DELETE FROM Users;
                ALTER TABLE UserSkills AUTO_INCREMENT = 1;
                ALTER TABLE Skills AUTO_INCREMENT = 1;
                ALTER TABLE Users AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();

            // Seed a user
            await using var ins = new MySqlCommand("INSERT INTO Users(FullName, Email) VALUES ('Alice','alice@example.com')", conn);
            await ins.ExecuteNonQueryAsync();
        }

        private async Task<int> GetUserIdByEmail(string email)
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("SELECT UserId FROM Users WHERE Email=@e", conn);
            cmd.Parameters.AddWithValue("@e", email);
            var obj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(obj);
        }

        [Test]
        public async Task AddSkill_ShouldInsertSkill_AndMapUser_UpsertLevel()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            // First add
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "React", Level = "Beginner" });

            // Second add (same skill), should upsert Level
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "React", Level = "Advanced" });

            var list = _sut.GetUserSkills(uid);
            list.Should().HaveCount(1);
            list[0].Skill!.Name.Should().Be("React");
            list[0].Level.Should().Be("Advanced");
        }

        [Test]
        public async Task DeleteUserSkill_ShouldRemoveMapping()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "C#", Level = "Intermediate" });

            var skills = _sut.GetUserSkills(uid);
            skills.Should().HaveCount(1);
            var skillId = skills[0].Skill!.SkillId;

            _sut.DeleteUserSkill(uid, skillId);

            _sut.GetUserSkills(uid).Should().BeEmpty();
        }

        [Test]
        public async Task GetUserSkills_ShouldReturnSkillWithLevel()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "MySQL", Level = "Beginner" });

            var list = _sut.GetUserSkills(uid);
            list.Should().HaveCount(1);
            list[0].Skill!.Name.Should().Be("MySQL");
            list[0].Level.Should().Be("Beginner");
        }

        [Test]
        public async Task SuggestSkills_ShouldReturnPrefixMatches()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "React", Level = "Intermediate" });
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "Redux", Level = "Intermediate" });
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "Node.js", Level = "Intermediate" });

            var res = _sut.SuggestSkills("Re");
            res.Should().HaveCount(2);
            res.Should().Contain(s => s.Name == "React");
            res.Should().Contain(s => s.Name == "Redux");
        }

        [Test]
        public async Task GetUsersBySkill_ShouldReturnUsersHavingThatSkill()
        {
            var uid = await GetUserIdByEmail("alice@example.com");
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "C#", Level = "Advanced" });

            var users = _sut.GetUsersBySkill("C#");
            users.Should().HaveCount(1);
            users[0].UserId.Should().Be(uid);
            users[0].FullName.Should().Be("Alice");
            users[0].Email.Should().Be("alice@example.com");
        }
    }
}
