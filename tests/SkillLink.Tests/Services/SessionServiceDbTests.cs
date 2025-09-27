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
    public class SessionServiceDbTests
    {
        private Testcontainers.MySql.MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;

        private IConfiguration _config = null!;
        private DbHelper _db = null!;
        private SessionService _sut = null!;

        // Reusable learner for Requests FK
        private int _learnerId;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external)) _externalConnStr = external;

            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sock1 = "/var/run/docker.sock";
            var sock2 = Path.Combine(home, ".docker/run/docker.sock");
            var dockerSocketExists = File.Exists(sock1) || File.Exists(sock2);

            if (!(_externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost)))
            {
                Assert.Ignore("Docker not available. Skipping SessionService DB integration tests.");
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

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connStr }
                })
                .Build();

            _db = new DbHelper(_config);
            _sut = new SessionService(_db);

            // Ensure learner exists to satisfy Requests(LearnerId) -> Users(UserId)
            _learnerId = await EnsureTestLearnerAsync(connStr);
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
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Clean child tables only; keep Users (learner and tutors)
            var sql = @"
                DELETE FROM Sessions;
                ALTER TABLE Sessions AUTO_INCREMENT = 1;
                DELETE FROM Requests;
                ALTER TABLE Requests AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // ---------------------------
        // Helpers: ensure test users
        // ---------------------------

        private static async Task<int> EnsureTestLearnerAsync(string connStr)
        {
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var findSql = "SELECT UserId FROM Users WHERE Email LIKE 'session-test-learner@%'";
            await using (var findCmd = new MySqlCommand(findSql, conn))
            {
                var maybeId = await findCmd.ExecuteScalarAsync();
                if (maybeId != null && maybeId != DBNull.Value)
                    return Convert.ToInt32(maybeId);
            }

            var email = $"session-test-learner@{Guid.NewGuid():N}.local";
            var insertSql = @"
                INSERT INTO Users
                  (FullName, Email, PasswordHash, Role, ReadyToTeach, IsActive, EmailVerified)
                VALUES
                  ('Session Test Learner', @em, 'hash', 'Learner', 0, 1, 1);
                SELECT LAST_INSERT_ID();";

            await using (var cmd = new MySqlCommand(insertSql, conn))
            {
                cmd.Parameters.AddWithValue("@em", email);
                var idObj = await cmd.ExecuteScalarAsync();
                return Convert.ToInt32(idObj);
            }
        }

        /// <summary>
        /// Ensure a Tutor user exists and return its id.
        /// Uses Role='Tutor' so it satisfies any role checks in SessionService.
        /// </summary>
        private int EnsureTestTutor(string key)
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var email = $"session-test-tutor-{key}@local";

            // Try find existing
            using (var find = new MySqlCommand("SELECT UserId FROM Users WHERE Email=@em", conn))
            {
                find.Parameters.AddWithValue("@em", email);
                var idObj = find.ExecuteScalar();
                if (idObj != null && idObj != DBNull.Value)
                    return Convert.ToInt32(idObj);
            }

            // Create tutor
            var sql = @"
                INSERT INTO Users
                  (FullName, Email, PasswordHash, Role, ReadyToTeach, IsActive, EmailVerified)
                VALUES
                  (@name, @em, 'hash', 'Tutor', 1, 1, 1);
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", $"Tutor {key}");
            cmd.Parameters.AddWithValue("@em", email);
            var newId = Convert.ToInt32(cmd.ExecuteScalar());
            return newId;
        }

        /// <summary>
        /// Inserts a Request owned by the reusable learner; returns RequestId.
        /// </summary>
        private int NewRequest(string skill = "TestSkill")
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var sql = @"
                INSERT INTO Requests (LearnerId, SkillName, Topic, Description, Status)
                VALUES (@learner, @skill, NULL, NULL, 'OPEN');
                SELECT LAST_INSERT_ID();";

            using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@learner", _learnerId);
            cmd.Parameters.AddWithValue("@skill", skill);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        // ---------------------------
        // Tests
        // ---------------------------

        [Test]
        public void AddSession_Then_GetById_ShouldReturnInserted()
        {
            var reqId = NewRequest("Physics");
            var tutorId = EnsureTestTutor("add");

            var s = new Session { RequestId = reqId, TutorId = tutorId, Status = "PENDING", ScheduledAt = null };
            _sut.AddSession(s);

            var all = _sut.GetAllSessions();
            all.Should().HaveCount(1);
            var first = all[0];

            var byId = _sut.GetById(first.SessionId);
            byId.Should().NotBeNull();
            byId!.TutorId.Should().Be(tutorId);
            byId.RequestId.Should().Be(reqId);
            byId.Status.Should().Be("PENDING");
        }

        [Test]
        public void GetByTutorId_ShouldReturnOnlyTutorsSessions()
        {
            var r1 = NewRequest("Req1");
            var r2 = NewRequest("Req2");
            var r3 = NewRequest("Req3");

            var tutorA = EnsureTestTutor("A");
            var tutorB = EnsureTestTutor("B");

            _sut.AddSession(new Session { RequestId = r1, TutorId = tutorA, Status = "PENDING" });
            _sut.AddSession(new Session { RequestId = r2, TutorId = tutorB, Status = "PENDING" });
            _sut.AddSession(new Session { RequestId = r3, TutorId = tutorA, Status = "PENDING" });

            var list = _sut.GetByTutorId(tutorA);
            list.Should().HaveCount(2);
            list.Should().OnlyContain(x => x.TutorId == tutorA);
        }

        [Test]
        public void UpdateStatus_ShouldChangeOnlyStatus()
        {
            var r1 = NewRequest("Req1");
            var tutorId = EnsureTestTutor("upd");

            _sut.AddSession(new Session { RequestId = r1, TutorId = tutorId, Status = "PENDING" });
            var first = _sut.GetAllSessions()[0];

            _sut.UpdateStatus(first.SessionId, "SCHEDULED");

            var updated = _sut.GetById(first.SessionId)!;
            updated.Status.Should().Be("SCHEDULED");
            updated.TutorId.Should().Be(tutorId);
            updated.RequestId.Should().Be(r1);
        }

        [Test]
        public void Delete_ShouldRemoveRow()
        {
            var r1 = NewRequest("Req1");
            var tutorId = EnsureTestTutor("del");

            _sut.AddSession(new Session { RequestId = r1, TutorId = tutorId, Status = "PENDING" });
            var first = _sut.GetAllSessions()[0];

            _sut.Delete(first.SessionId);
            _sut.GetById(first.SessionId).Should().BeNull();
        }
    }
}
