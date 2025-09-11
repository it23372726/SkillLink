using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;

// RequestService and models are in the global namespace in API

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class RequestServiceDbTests
    {
    private Testcontainers.MySql.MySqlContainer _mysql = null!;
    private bool _ownsContainer = false;
    private string? _externalConnStr = null;
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private RequestService _sut = null!;

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
                Assert.Ignore("Docker not available. Skipping RequestService DB integration tests.");
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

            // Create schema for Users and Requests
            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS Users (
                      UserId INT AUTO_INCREMENT PRIMARY KEY,
                      FullName VARCHAR(255) NOT NULL,
                      Email VARCHAR(255) NOT NULL UNIQUE
                    );
                    CREATE TABLE IF NOT EXISTS Requests (
                      RequestId INT AUTO_INCREMENT PRIMARY KEY,
                      LearnerId INT NOT NULL,
                      SkillName VARCHAR(255) NOT NULL,
                      Topic TEXT NULL,
                      Status VARCHAR(50) NOT NULL DEFAULT 'OPEN',
                      CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                      Description TEXT NULL,
                      FOREIGN KEY (LearnerId) REFERENCES Users(UserId)
                    );
                ";
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connStr }
                })
                .Build();

            _dbHelper = new DbHelper(_config);
            _sut = new RequestService(_dbHelper);
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
            var sql = "DELETE FROM Requests; DELETE FROM Users; ALTER TABLE Requests AUTO_INCREMENT = 1; ALTER TABLE Users AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, string fullName, string email)
        {
            var cmd = new MySqlCommand("INSERT INTO Users (FullName, Email) VALUES (@n, @e); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", fullName);
            cmd.Parameters.AddWithValue("@e", email);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        private async Task<int> InsertRequestAsync(MySqlConnection conn, int learnerId, string skill, string? topic, string? description, string status = "OPEN", DateTime? createdAt = null)
        {
            var cmd = new MySqlCommand(@"INSERT INTO Requests (LearnerId, SkillName, Topic, Description, Status, CreatedAt)
                                         VALUES (@l, @s, @t, @d, @st, @c);
                                         SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@l", learnerId);
            cmd.Parameters.AddWithValue("@s", skill);
            cmd.Parameters.AddWithValue("@t", (object?)topic ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@d", (object?)description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@st", status);
            cmd.Parameters.AddWithValue("@c", createdAt ?? DateTime.UtcNow);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task AddRequest_ShouldInsertRowWithDefaults()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Alice", "alice@example.com");

            _sut.AddRequest(new Request {
                LearnerId = userId,
                SkillName = "Math",
                Topic = "Algebra",
                Description = "Need help with equations"
            });

            var list = _sut.GetByLearnerId(userId);
            list.Should().HaveCount(1);
            list[0].SkillName.Should().Be("Math");
            list[0].Status.Should().Be("OPEN");
            list[0].FullName.Should().Be("Alice");
            list[0].Email.Should().Be("alice@example.com");
        }

        [Test]
        public async Task GetById_ShouldReturnRequestWithUser()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Bob", "bob@example.com");
            var reqId = await InsertRequestAsync(conn, userId, "English", "Grammar", "Punctuation", "OPEN", DateTime.UtcNow.AddMinutes(-10));

            var r = _sut.GetById(reqId);
            r.Should().NotBeNull();
            r!.LearnerId.Should().Be(userId);
            r.FullName.Should().Be("Bob");
            r.Email.Should().Be("bob@example.com");
        }

        [Test]
        public async Task GetByLearnerId_ShouldReturnAllForLearner()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Cara", "cara@example.com");
            await InsertRequestAsync(conn, userId, "Science", "Biology", "Cells");
            await InsertRequestAsync(conn, userId, "Science", "Chemistry", "Atoms");

            var list = _sut.GetByLearnerId(userId);
            list.Should().HaveCount(2);
            list.Should().OnlyContain(x => x.LearnerId == userId);
        }

        [Test]
        public async Task GetAllRequests_ShouldReturnOrderedByCreatedAtDesc()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var u1 = await InsertUserAsync(conn, "Drew", "drew@example.com");
            var u2 = await InsertUserAsync(conn, "Elle", "elle@example.com");
            var older = await InsertRequestAsync(conn, u1, "History", "WW2", "Essay", "OPEN", DateTime.UtcNow.AddHours(-2));
            var newer = await InsertRequestAsync(conn, u2, "Music", "Piano", "Scales", "OPEN", DateTime.UtcNow);

            var list = _sut.GetAllRequests();
            list.Should().HaveCount(2);
            list[0].RequestId.Should().Be(newer);
            list[1].RequestId.Should().Be(older);
        }

        [Test]
        public async Task UpdateRequest_ShouldModifyFields()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Finn", "finn@example.com");
            var reqId = await InsertRequestAsync(conn, userId, "Art", "Drawing", "Shading");

            _sut.UpdateRequest(reqId, new Request {
                SkillName = "Art Basics",
                Topic = null,
                Description = "Intro"
            });

            var r = _sut.GetById(reqId)!;
            r.SkillName.Should().Be("Art Basics");
            r.Topic.Should().BeNull();
            r.Description.Should().Be("Intro");
        }

        [Test]
        public async Task UpdateStatus_ShouldChangeOnlyStatus()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Gail", "gail@example.com");
            var reqId = await InsertRequestAsync(conn, userId, "Coding", "C#", "LINQ");

            _sut.UpdateStatus(reqId, "CLOSED");

            var r = _sut.GetById(reqId)!;
            r.Status.Should().Be("CLOSED");
            r.SkillName.Should().Be("Coding");
        }

        [Test]
        public async Task DeleteRequest_ShouldRemoveRow()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var userId = await InsertUserAsync(conn, "Hank", "hank@example.com");
            var reqId = await InsertRequestAsync(conn, userId, "Sports", "Tennis", "Serve");

            _sut.DeleteRequest(reqId);
            var r = _sut.GetById(reqId);
            r.Should().BeNull();
        }

        [Test]
        public async Task SearchRequests_ShouldMatch_Skill_Topic_Description_And_FullName()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var u1 = await InsertUserAsync(conn, "Ivy Mathers", "ivy@example.com");
            var u2 = await InsertUserAsync(conn, "John Doe", "john@example.com");

            await InsertRequestAsync(conn, u1, "Mathematics", "Algebra", "Equations");
            await InsertRequestAsync(conn, u2, "Language", "Grammar", "Punctuation");

            var bySkill = _sut.SearchRequests("math");
            bySkill.Should().HaveCount(1);
            bySkill[0].LearnerId.Should().Be(u1);

            var byTopic = _sut.SearchRequests("gram");
            byTopic.Should().HaveCount(1);
            byTopic[0].LearnerId.Should().Be(u2);

            var byDesc = _sut.SearchRequests("equat");
            byDesc.Should().HaveCount(1);
            byDesc[0].LearnerId.Should().Be(u1);

            var byName = _sut.SearchRequests("doe");
            byName.Should().HaveCount(1);
            byName[0].LearnerId.Should().Be(u2);
        }
    }
}
