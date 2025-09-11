using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Services;
using Testcontainers.MySql;
using Microsoft.Extensions.Configuration;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class SessionServiceDbTests
    {
        private MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private SessionService _sut = null!;

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
                Assert.Ignore("Docker not available. Skipping SessionService DB integration tests.");
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

            // Create schema
            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS Sessions (
                        SessionId INT AUTO_INCREMENT PRIMARY KEY,
                        RequestId INT NOT NULL,
                        TutorId INT NOT NULL,
                        ScheduledAt DATETIME NULL,
                        Status VARCHAR(50) NOT NULL,
                        CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
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
            _sut = new SessionService(_dbHelper);
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
            await using var cmd = new MySqlCommand("DELETE FROM Sessions; ALTER TABLE Sessions AUTO_INCREMENT = 1;", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        [Test]
        public void AddSession_And_GetAll()
        {
            _sut.AddSession(new Session { RequestId = 1, TutorId = 2, ScheduledAt = null, Status = "PENDING" });
            var all = _sut.GetAllSessions();
            all.Should().HaveCount(1);
            all[0].RequestId.Should().Be(1);
            all[0].TutorId.Should().Be(2);
            all[0].Status.Should().Be("PENDING");
        }

        [Test]
        public void GetById_ShouldReturnInserted()
        {
            _sut.AddSession(new Session { RequestId = 10, TutorId = 3, ScheduledAt = DateTime.UtcNow, Status = "SCHEDULED" });
            var item = _sut.GetAllSessions()[0];
            var fetched = _sut.GetById(item.SessionId);
            fetched.Should().NotBeNull();
            fetched!.RequestId.Should().Be(10);
            fetched.TutorId.Should().Be(3);
        }

        [Test]
        public void GetByTutorId_ShouldFilter()
        {
            _sut.AddSession(new Session { RequestId = 1, TutorId = 7, ScheduledAt = null, Status = "PENDING" });
            _sut.AddSession(new Session { RequestId = 2, TutorId = 8, ScheduledAt = null, Status = "PENDING" });
            var list = _sut.GetByTutorId(7)!;
            list.Should().HaveCount(1);
            list[0].TutorId.Should().Be(7);
        }

        [Test]
        public void UpdateStatus_ShouldChange()
        {
            _sut.AddSession(new Session { RequestId = 1, TutorId = 2, ScheduledAt = null, Status = "PENDING" });
            var id = _sut.GetAllSessions()[0].SessionId;
            _sut.UpdateStatus(id, "COMPLETED");
            var after = _sut.GetById(id)!;
            after.Status.Should().Be("COMPLETED");
        }

        [Test]
        public void Delete_ShouldRemove()
        {
            _sut.AddSession(new Session { RequestId = 1, TutorId = 2, ScheduledAt = null, Status = "PENDING" });
            var id = _sut.GetAllSessions()[0].SessionId;
            _sut.Delete(id);
            _sut.GetById(id).Should().BeNull();
        }
    }
}
