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
    public class FeedServiceDbTests
    {
        private Testcontainers.MySql.MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private FeedService _feed = null!;
        private ReactionService _reactions = null!;
        private CommentService _comments = null!;

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
                Assert.Ignore("Docker not available. Skipping FeedService DB integration tests.");
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

            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                  UserId INT AUTO_INCREMENT PRIMARY KEY,
                  FullName VARCHAR(255) NOT NULL,
                  Email VARCHAR(255) NOT NULL UNIQUE,
                  ProfilePicture VARCHAR(1024) NULL
                );

                CREATE TABLE IF NOT EXISTS TutorPosts (
                  PostId INT AUTO_INCREMENT PRIMARY KEY,
                  TutorId INT NOT NULL,
                  Title VARCHAR(255) NOT NULL,
                  Description TEXT NULL,
                  MaxParticipants INT NOT NULL DEFAULT 1,
                  Status VARCHAR(50) NOT NULL DEFAULT 'Open',
                  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  ScheduledAt DATETIME NULL,
                  ImageUrl VARCHAR(1024) NULL,
                  FOREIGN KEY (TutorId) REFERENCES Users(UserId)
                );

                CREATE TABLE IF NOT EXISTS Requests (
                  RequestId INT AUTO_INCREMENT PRIMARY KEY,
                  LearnerId INT NOT NULL,
                  SkillName VARCHAR(255) NOT NULL,
                  Topic TEXT NULL,
                  Description TEXT NULL,
                  Status VARCHAR(50) NOT NULL DEFAULT 'OPEN',
                  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  FOREIGN KEY (LearnerId) REFERENCES Users(UserId)
                );

                CREATE TABLE IF NOT EXISTS PostReactions (
                  Id INT AUTO_INCREMENT PRIMARY KEY,
                  PostType VARCHAR(20) NOT NULL,
                  PostId INT NOT NULL,
                  UserId INT NOT NULL,
                  Reaction VARCHAR(10) NOT NULL,
                  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  UNIQUE KEY uq_react (PostType, PostId, UserId)
                );

                CREATE TABLE IF NOT EXISTS PostComments (
                  CommentId INT AUTO_INCREMENT PRIMARY KEY,
                  PostType VARCHAR(20) NOT NULL,
                  PostId INT NOT NULL,
                  UserId INT NOT NULL,
                  Content TEXT NOT NULL,
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
            _reactions = new ReactionService(_dbHelper);
            _comments = new CommentService(_dbHelper);
            _feed = new FeedService(_dbHelper, _reactions, _comments);
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
            var sql = @"
                DELETE FROM PostComments;
                DELETE FROM PostReactions;
                DELETE FROM Requests;
                DELETE FROM TutorPosts;
                DELETE FROM Users;
                ALTER TABLE PostComments AUTO_INCREMENT = 1;
                ALTER TABLE PostReactions AUTO_INCREMENT = 1;
                ALTER TABLE Requests AUTO_INCREMENT = 1;
                ALTER TABLE TutorPosts AUTO_INCREMENT = 1;
                ALTER TABLE Users AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUser(MySqlConnection conn, string name, string email)
        {
            var cmd = new MySqlCommand("INSERT INTO Users (FullName, Email) VALUES (@n, @e); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<int> InsertLesson(MySqlConnection conn, int tutorId, string title, string desc, DateTime? created = null)
        {
            var cmd = new MySqlCommand(@"
                INSERT INTO TutorPosts (TutorId, Title, Description, MaxParticipants, Status, CreatedAt)
                VALUES (@t, @ti, @d, 5, 'Open', @c); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@t", tutorId);
            cmd.Parameters.AddWithValue("@ti", title);
            cmd.Parameters.AddWithValue("@d", desc);
            cmd.Parameters.AddWithValue("@c", created ?? DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<int> InsertRequest(MySqlConnection conn, int learnerId, string skill, string topic, string desc, DateTime? created = null)
        {
            var cmd = new MySqlCommand(@"
                INSERT INTO Requests (LearnerId, SkillName, Topic, Description, Status, CreatedAt)
                VALUES (@l, @s, @t, @d, 'OPEN', @c); SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@l", learnerId);
            cmd.Parameters.AddWithValue("@s", skill);
            cmd.Parameters.AddWithValue("@t", topic);
            cmd.Parameters.AddWithValue("@d", desc);
            cmd.Parameters.AddWithValue("@c", created ?? DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        [Test]
        public async Task GetFeed_ShouldReturnUnion_WithReactions_AndComments_AndSearch()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var tutor = await InsertUser(conn, "Alice Tutor", "alice@ex.com");
            var learner = await InsertUser(conn, "Bob Learner", "bob@ex.com");
            var me = await InsertUser(conn, "Me User", "me@ex.com");

            var lessonId = await InsertLesson(conn, tutor, "C# Advanced", "LINQ", DateTime.UtcNow.AddMinutes(-10));
            var reqId = await InsertRequest(conn, learner, "Java", "Streams", "Collections", DateTime.UtcNow);

            // reactions
            _reactions.UpsertReaction(me, "LESSON", lessonId, "LIKE");
            _reactions.UpsertReaction(me, "REQUEST", reqId, "DISLIKE");

            // comments
            _comments.Add("LESSON", lessonId, me, "Great!");
            _comments.Add("REQUEST", reqId, me, "I can help");

            // page 1
            var page1 = _feed.GetFeed(me, page: 1, pageSize: 10, q: null);
            page1.Should().HaveCount(2);
            page1[0].PostType.Should().Be("REQUEST"); // created later (newest first)
            page1[1].PostType.Should().Be("LESSON");

            // verify augmentation
            var req = page1[0];
            req.Likes.Should().Be(0);
            req.Dislikes.Should().Be(1);
            req.MyReaction.Should().Be("DISLIKE");
            req.CommentCount.Should().Be(1);

            var les = page1[1];
            les.Likes.Should().Be(1);
            les.Dislikes.Should().Be(0);
            les.MyReaction.Should().Be("LIKE");
            les.CommentCount.Should().Be(1);

            // search: "c#" should match lesson title/desc; "java" should match request skill
            var byCsharp = _feed.GetFeed(me, 1, 10, q: "c#");
            byCsharp.Should().ContainSingle(x => x.PostType == "LESSON" && x.PostId == lessonId);

            var byJava = _feed.GetFeed(me, 1, 10, q: "java");
            byJava.Should().ContainSingle(x => x.PostType == "REQUEST" && x.PostId == reqId);
        }

        [Test]
        public async Task ReactionService_ShouldUpsert_AndRemove()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var u1 = await InsertUser(conn, "U1", "u1@ex.com");
            var u2 = await InsertUser(conn, "U2", "u2@ex.com");
            var t = await InsertUser(conn, "T", "t@ex.com");
            var postId = await InsertLesson(conn, t, "X", "D");

            _reactions.UpsertReaction(u1, "LESSON", postId, "LIKE");
            _reactions.UpsertReaction(u2, "LESSON", postId, "DISLIKE");

            var s1 = _reactions.GetReactionSummary(u1, "LESSON", postId);
            s1.likes.Should().Be(1);
            s1.dislikes.Should().Be(1);
            s1.my.Should().Be("LIKE");

            _reactions.UpsertReaction(u1, "LESSON", postId, "DISLIKE");
            var s2 = _reactions.GetReactionSummary(u1, "LESSON", postId);
            s2.likes.Should().Be(0);
            s2.dislikes.Should().Be(2);
            s2.my.Should().Be("DISLIKE");

            _reactions.RemoveReaction(u1, "LESSON", postId);
            var s3 = _reactions.GetReactionSummary(u1, "LESSON", postId);
            s3.likes.Should().Be(0);
            s3.dislikes.Should().Be(1);
            s3.my.Should().BeNull();
        }

        [Test]
        public async Task CommentService_ShouldAdd_Get_Count_Delete_WithOwnerOrAdmin()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var tutor = await InsertUser(conn, "Tutor", "t@ex.com");
            var author = await InsertUser(conn, "Author", "a@ex.com");
            var admin = await InsertUser(conn, "Admin", "adm@ex.com");
            var postId = await InsertLesson(conn, tutor, "L1", "desc");

            _comments.Add("LESSON", postId, author, "c1");
            _comments.Add("LESSON", postId, author, "c2");

            _comments.Count("LESSON", postId).Should().Be(2);
            var all = _comments.GetComments("LESSON", postId);
            all.Should().HaveCount(2);
            var c1 = (int)all[0].GetType().GetProperty("CommentId")!.GetValue(all[0])!;

            // delete by comment owner
            _comments.Delete(c1, author, isAdmin: false);
            _comments.Count("LESSON", postId).Should().Be(1);

            // delete by post owner (tutor)
            var c2 = (int)all[1].GetType().GetProperty("CommentId")!.GetValue(all[1])!;
            _comments.Delete(c2, tutor, isAdmin: false);
            _comments.Count("LESSON", postId).Should().Be(0);
        }
    }
}

