using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using Testcontainers.MySql;
using Microsoft.Extensions.Configuration;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class AcceptedRequestServiceDbTests
    {
        private MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private AcceptedRequestService _sut = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
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
            var shouldRun = _externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost);

            if (!shouldRun)
            {
                Assert.Ignore("Docker not available. Skipping AcceptedRequestService DB integration tests.");
                return;
            }

            if (_externalConnStr == null)
            {
                try
                {
                    _mysql = new MySqlBuilder()
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

            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS Users (
                        UserId INT AUTO_INCREMENT PRIMARY KEY,
                        FullName VARCHAR(255) NOT NULL,
                        Email VARCHAR(255) NOT NULL,
                        Role VARCHAR(50) NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS Requests (
                        RequestId INT AUTO_INCREMENT PRIMARY KEY,
                        LearnerId INT NOT NULL,
                        SkillName VARCHAR(255) NOT NULL,
                        Topic VARCHAR(255) NULL,
                        Description TEXT NULL,
                        Status VARCHAR(50) NOT NULL DEFAULT 'OPEN',
                        CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
                    );
                    CREATE TABLE IF NOT EXISTS AcceptedRequests (
                        AcceptedRequestId INT AUTO_INCREMENT PRIMARY KEY,
                        RequestId INT NOT NULL,
                        AcceptorId INT NOT NULL,
                        AcceptedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                        Status VARCHAR(50) NOT NULL DEFAULT 'ACCEPTED',
                        ScheduleDate DATETIME NULL,
                        MeetingType VARCHAR(50) NULL,
                        MeetingLink VARCHAR(255) NULL
                    );";
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
            _sut = new AcceptedRequestService(_dbHelper);
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
            if (_config == null) return;
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await using var cmd = new MySqlCommand("DELETE FROM AcceptedRequests; ALTER TABLE AcceptedRequests AUTO_INCREMENT = 1; DELETE FROM Requests; ALTER TABLE Requests AUTO_INCREMENT = 1; DELETE FROM Users; ALTER TABLE Users AUTO_INCREMENT = 1;", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, string name, string email, string role)
        {
            var sql = "INSERT INTO Users (FullName, Email, Role) VALUES (@n, @e, @r); SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@r", role);
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        private async Task<int> InsertRequestAsync(MySqlConnection conn, int learnerId, string skill, string? topic)
        {
            var sql = "INSERT INTO Requests (LearnerId, SkillName, Topic) VALUES (@l, @s, @t); SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@l", learnerId);
            cmd.Parameters.AddWithValue("@s", skill);
            cmd.Parameters.AddWithValue("@t", (object?)topic ?? DBNull.Value);
            var id = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(id);
        }

        [Test]
        public async Task AcceptRequest_ShouldInsert_WhenNotExisting()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var learner = await InsertUserAsync(conn, "Alice", "alice@x.com", "Learner");
            var tutor = await InsertUserAsync(conn, "Bob", "bob@x.com", "Tutor");
            var req = await InsertRequestAsync(conn, learner, "CSharp", null);

            _sut.AcceptRequest(req, tutor);

            await using var check = new MySqlCommand("SELECT COUNT(*) FROM AcceptedRequests", conn);
            var count = Convert.ToInt32(await check.ExecuteScalarAsync());
            count.Should().Be(1);
        }

        [Test]
        public async Task AcceptRequest_ShouldThrow_WhenDuplicate()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var learner = await InsertUserAsync(conn, "Alice", "alice@x.com", "Learner");
            var tutor = await InsertUserAsync(conn, "Bob", "bob@x.com", "Tutor");
            var req = await InsertRequestAsync(conn, learner, "CSharp", null);
            _sut.AcceptRequest(req, tutor);

            Action act = () => _sut.AcceptRequest(req, tutor);
            act.Should().Throw<Exception>().WithMessage("*already accepted*");
        }

        [Test]
        public async Task UpdateAcceptanceStatus_ShouldChange()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var learner = await InsertUserAsync(conn, "Alice", "alice@x.com", "Learner");
            var tutor = await InsertUserAsync(conn, "Bob", "bob@x.com", "Tutor");
            var req = await InsertRequestAsync(conn, learner, "CSharp", null);
            _sut.AcceptRequest(req, tutor);

            await using var getId = new MySqlCommand("SELECT AcceptedRequestId FROM AcceptedRequests LIMIT 1", conn);
            var id = Convert.ToInt32(await getId.ExecuteScalarAsync());
            _sut.UpdateAcceptanceStatus(id, "REJECTED");

            await using var check = new MySqlCommand("SELECT Status FROM AcceptedRequests WHERE AcceptedRequestId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var status = (string?)await check.ExecuteScalarAsync();
            status.Should().Be("REJECTED");
        }

        [Test]
        public async Task HasUserAcceptedRequest_ShouldReflectState()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var learner = await InsertUserAsync(conn, "Alice", "alice@x.com", "Learner");
            var tutor = await InsertUserAsync(conn, "Bob", "bob@x.com", "Tutor");
            var req = await InsertRequestAsync(conn, learner, "CSharp", null);

            _sut.HasUserAcceptedRequest(tutor, req).Should().BeFalse();
            _sut.AcceptRequest(req, tutor);
            _sut.HasUserAcceptedRequest(tutor, req).Should().BeTrue();
        }

        [Test]
        public async Task ScheduleMeeting_ShouldSetFields()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var learner = await InsertUserAsync(conn, "Alice", "alice@x.com", "Learner");
            var tutor = await InsertUserAsync(conn, "Bob", "bob@x.com", "Tutor");
            var req = await InsertRequestAsync(conn, learner, "CSharp", null);
            _sut.AcceptRequest(req, tutor);

            await using var getId = new MySqlCommand("SELECT AcceptedRequestId FROM AcceptedRequests LIMIT 1", conn);
            var id = Convert.ToInt32(await getId.ExecuteScalarAsync());
            var when = DateTime.UtcNow.AddDays(1);
            _sut.ScheduleMeeting(id, when, "ZOOM", "http://zoom");

            await using var check = new MySqlCommand("SELECT Status, MeetingType FROM AcceptedRequests WHERE AcceptedRequestId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            await using var reader = await check.ExecuteReaderAsync();
            await reader.ReadAsync();
            ((string)reader["Status"]).Should().Be("SCHEDULED");
            ((string)reader["MeetingType"]).Should().Be("ZOOM");
        }
    }
}
