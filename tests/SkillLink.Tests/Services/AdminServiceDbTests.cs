using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Services;

using SkillLink.API.Models;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class AdminServiceDbTests
    {
        private Testcontainers.MySql.MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;

        private IConfiguration _config = null!;
        private DbHelper _db = null!;
        private AdminService _sut = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // Allow using external MySQL for CI or local dev
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _externalConnStr = external;
            }

            // Docker detection
            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sock1 = "/var/run/docker.sock";
            var sock2 = Path.Combine(home, ".docker/run/docker.sock");
            var dockerSocketExists = File.Exists(sock1) || File.Exists(sock2);

            if (!(_externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost)))
            {
                Assert.Ignore("Docker not available. Skipping AdminService DB integration tests.");
                return;
            }

            if (_externalConnStr == null)
            {
                try
                {
                    _mysql = new Testcontainers.MySql.MySqlBuilder()
                        .WithImage("mysql:8.0")
                        .WithDatabase("skilllink_test_admin")
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

            // Create Users table schema (only fields used by AdminService)
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
                      EmailVerified TINYINT(1) NOT NULL DEFAULT 0,
                      EmailVerificationToken VARCHAR(255) NULL,
                      EmailVerificationExpires DATETIME NULL
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

            _db = new DbHelper(_config);
            _sut = new AdminService(_db);
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
            // Clean Users table before each test
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var sql = @"DELETE FROM Users; ALTER TABLE Users AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, string name, string email, string role = "Learner", bool isActive = true, bool readyToTeach = false)
        {
            var cmd = new MySqlCommand(@"
                INSERT INTO Users (FullName, Email, Role, IsActive, ReadyToTeach, EmailVerified, CreatedAt)
                VALUES (@n, @e, @r, @a, @t, 1, NOW());
                SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@a", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@t", readyToTeach ? 1 : 0);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task GetUsers_ShouldReturn_All_And_FilterBySearch()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await InsertUserAsync(conn, "Alice Test", "alice@example.com", "Learner");
            await InsertUserAsync(conn, "Bob Tutor", "bob@example.com", "Tutor");

            var all = _sut.GetUsers(null);
            all.Should().HaveCount(2);

            var filtered = _sut.GetUsers("bob");
            filtered.Should().HaveCount(1);
            filtered[0].FullName.Should().Be("Bob Tutor");
        }

        [Test]
        public async Task SetUserActive_ShouldUpdate()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var id = await InsertUserAsync(conn, "Cara", "cara@example.com", "Learner", isActive:true);

            var ok = _sut.SetUserActive(id, false);
            ok.Should().BeTrue();

            // verify from DB
            var check = new MySqlCommand("SELECT IsActive FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var isActive = Convert.ToInt32(await check.ExecuteScalarAsync());
            isActive.Should().Be(0);
        }

        [Test]
        public async Task SetUserRole_ShouldUpdate()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var id = await InsertUserAsync(conn, "Drew", "drew@example.com", "Learner");

            var ok = _sut.SetUserRole(id, "Tutor");
            ok.Should().BeTrue();

            var check = new MySqlCommand("SELECT Role FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var role = (string)(await check.ExecuteScalarAsync() ?? "");
            role.Should().Be("Tutor");
        }
    }
}
