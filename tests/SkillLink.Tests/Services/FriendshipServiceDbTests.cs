using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Services;
using SkillLink.API.Models; // adjust if needed

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class FriendshipServiceDbTests
    {
        private Testcontainers.MySql.MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;

        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private FriendshipService _sut = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // Allow external MySQL via env var (optional)
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _externalConnStr = external;
            }

            // If no external DB, try Docker
            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sock1 = "/var/run/docker.sock";
            var sock2 = Path.Combine(home, ".docker/run/docker.sock");
            var dockerSocketExists = File.Exists(sock1) || File.Exists(sock2);

            if (!(_externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost)))
            {
                Assert.Ignore("Docker not available. Skipping FriendshipService DB integration tests.");
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
                      ProfilePicture VARCHAR(255) NULL
                    );

                    CREATE TABLE IF NOT EXISTS Friendships (
                      Id INT AUTO_INCREMENT PRIMARY KEY,
                      FollowerId INT NOT NULL,
                      FollowedId INT NOT NULL,
                      CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                      UNIQUE KEY uq_follow (FollowerId, FollowedId),
                      FOREIGN KEY (FollowerId) REFERENCES Users(UserId) ON DELETE CASCADE,
                      FOREIGN KEY (FollowedId) REFERENCES Users(UserId) ON DELETE CASCADE
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
            _sut = new FriendshipService(_dbHelper);
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
            var connStr = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            // Clean tables
            var sql = @"
                DELETE FROM Friendships;
                DELETE FROM Users;
                ALTER TABLE Friendships AUTO_INCREMENT = 1;
                ALTER TABLE Users AUTO_INCREMENT = 1;
            ";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, string name, string email, string? pic = null)
        {
            var cmd = new MySqlCommand(
                @"INSERT INTO Users (FullName, Email, ProfilePicture) VALUES (@n, @e, @p);
                  SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", (object?)pic ?? DBNull.Value);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task Follow_ShouldInsert_And_PreventDuplicates()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var alice = await InsertUserAsync(conn, "Alice", "alice@example.com");
            var bob = await InsertUserAsync(conn, "Bob", "bob@example.com");

            _sut.Follow(alice, bob);

            // second time should throw our InvalidOperationException
            Action again = () => _sut.Follow(alice, bob);
            again.Should().Throw<InvalidOperationException>()
                 .WithMessage("Already following");

            // check stored
            var countCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            countCmd.Parameters.AddWithValue("@f", alice);
            countCmd.Parameters.AddWithValue("@fd", bob);
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            count.Should().Be(1);
        }

        [Test]
        public async Task Unfollow_ShouldDeleteRow()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var a = await InsertUserAsync(conn, "A", "a@example.com");
            var b = await InsertUserAsync(conn, "B", "b@example.com");

            _sut.Follow(a, b);
            _sut.Unfollow(a, b);

            var countCmd = new MySqlCommand(
                "SELECT COUNT(*) FROM Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            countCmd.Parameters.AddWithValue("@f", a);
            countCmd.Parameters.AddWithValue("@fd", b);
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            count.Should().Be(0);
        }

        [Test]
        public async Task GetMyFriends_ShouldReturnFollowedUsers()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var me = await InsertUserAsync(conn, "Me", "me@example.com");
            var u1 = await InsertUserAsync(conn, "Jane Roe", "jane@example.com", "pic1.jpg");
            var u2 = await InsertUserAsync(conn, "Mark Smith", "mark@example.com", null);

            _sut.Follow(me, u1);
            _sut.Follow(me, u2);

            var list = _sut.GetMyFriends(me);
            list.Should().HaveCount(2);
            list.Should().ContainSingle(x => x.UserId == u1 && x.ProfilePicture == "pic1.jpg");
            list.Should().ContainSingle(x => x.UserId == u2 && x.ProfilePicture == null);
        }

        [Test]
        public async Task GetFollowers_ShouldReturnUsersWhoFollowMe()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var me = await InsertUserAsync(conn, "Me", "me@example.com");
            var a = await InsertUserAsync(conn, "Alpha", "alpha@example.com");
            var b = await InsertUserAsync(conn, "Beta", "beta@example.com");

            _sut.Follow(a, me);
            _sut.Follow(b, me);

            var followers = _sut.GetFollowers(me);
            followers.Should().HaveCount(2);
            followers.Should().Contain(x => x.Email == "alpha@example.com");
            followers.Should().Contain(x => x.Email == "beta@example.com");
        }

        [Test]
        public async Task SearchUsers_ShouldMatchNameOrEmail_ExcludeSelf_AndLimit()
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            await using var conn = new MySqlConnection(connStr);
            await conn.OpenAsync();

            var me = await InsertUserAsync(conn, "Me Myself", "me@example.com");
            await InsertUserAsync(conn, "Jane Goodall", "jane@example.com");
            await InsertUserAsync(conn, "Janet Leigh", "janet@example.com");
            await InsertUserAsync(conn, "Bob Jones", "bob@example.com");

            var res = _sut.SearchUsers("jan", me);
            res.Should().HaveCount(2);
            res.Should().OnlyContain(u => u.FullName.StartsWith("Jan", StringComparison.OrdinalIgnoreCase)
                                       || u.Email.Contains("jan", StringComparison.OrdinalIgnoreCase));
            res.Should().NotContain(u => u.Email == "me@example.com");
        }
    }
}
