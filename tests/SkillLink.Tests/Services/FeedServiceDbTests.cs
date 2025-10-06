using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SkillLink.API.Services;
using SkillLink.API.Data;
using SkillLink.API.Repositories;
using SkillLink.Tests.Db;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class FeedServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private FeedService _feed = null!;
        private ReactionService _reactions = null!;
        private CommentService _comments = null!;
        private string _connStr = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _connStr = await TestDbUtil.EnsureTestDbAsync();

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new[]
                {
                    new KeyValuePair<string,string?>("ConnectionStrings:DefaultConnection", _connStr)
                })
                .Build();

            _dbHelper = new DbHelper(_config);

            var reactionRepo = new ReactionRepository(_dbHelper);
            var commentRepo  = new CommentRepository(_dbHelper);
            var feedRepo     = new FeedRepository(_dbHelper);

            _reactions = new ReactionService(reactionRepo);
            _comments  = new CommentService(commentRepo);
            _feed      = new FeedService(feedRepo, _reactions, _comments);
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            var sql = @"
DELETE FROM dbo.PostComments;
DELETE FROM dbo.PostReactions;
DELETE FROM dbo.Requests;
DELETE FROM dbo.TutorPosts;
DELETE FROM dbo.Users;

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.PostComments'))
    DBCC CHECKIDENT('dbo.PostComments', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.PostReactions'))
    DBCC CHECKIDENT('dbo.PostReactions', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Requests'))
    DBCC CHECKIDENT('dbo.Requests', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.TutorPosts'))
    DBCC CHECKIDENT('dbo.TutorPosts', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUser(SqlConnection conn, string name, string email)
        {
            var cmd = new SqlCommand("INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, CreatedAt, IsActive, EmailVerified) VALUES (@n,@e,'x','Learner',SYSUTCDATETIME(),1,1); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<int> InsertLesson(SqlConnection conn, int tutorId, string title, string desc, DateTime? created = null)
        {
            var cmd = new SqlCommand(@"
INSERT INTO dbo.TutorPosts (TutorId, Title, Description, MaxParticipants, Status, CreatedAt)
VALUES (@t, @ti, @d, 5, 'Open', @c);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@t", tutorId);
            cmd.Parameters.AddWithValue("@ti", title);
            cmd.Parameters.AddWithValue("@d", (object?)desc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", (object?)created ?? DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<int> InsertRequest(SqlConnection conn, int learnerId, string skill, string topic, string desc, DateTime? created = null)
        {
            var cmd = new SqlCommand(@"
INSERT INTO dbo.Requests (LearnerId, SkillName, Topic, Description, Status, CreatedAt)
VALUES (@l, @s, @t, @d, 'OPEN', @c);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@l", learnerId);
            cmd.Parameters.AddWithValue("@s", skill);
            cmd.Parameters.AddWithValue("@t", (object?)topic ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@d", (object?)desc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@c", (object?)created ?? DateTime.UtcNow);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        [Test]
        public async Task GetFeed_ShouldReturnUnion_WithReactions_AndComments_AndSearch()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUser(conn, "Alice Tutor", "alice@ex.com");
            var learner = await InsertUser(conn, "Bob Learner", "bob@ex.com");
            var me = await InsertUser(conn, "Me User", "me@ex.com");

            var lessonId = await InsertLesson(conn, tutor, "C# Advanced", "LINQ", DateTime.UtcNow.AddMinutes(-10));
            var reqId = await InsertRequest(conn, learner, "Java", "Streams", "Collections", DateTime.UtcNow);

            _reactions.UpsertReaction(me, "LESSON", lessonId, "LIKE");
            _reactions.UpsertReaction(me, "REQUEST", reqId, "DISLIKE");

            _comments.Add("LESSON", lessonId, me, "Great!");
            _comments.Add("REQUEST", reqId, me, "I can help");

            var page1 = _feed.GetFeed(me, page: 1, pageSize: 10, q: null);
            page1.Should().HaveCount(2);
            page1[0].PostType.Should().Be("REQUEST");
            page1[1].PostType.Should().Be("LESSON");

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

            var byCsharp = _feed.GetFeed(me, 1, 10, q: "c#");
            byCsharp.Should().ContainSingle(x => x.PostType == "LESSON" && x.PostId == lessonId);

            var byJava = _feed.GetFeed(me, 1, 10, q: "java");
            byJava.Should().ContainSingle(x => x.PostType == "REQUEST" && x.PostId == reqId);
        }

        [Test]
        public async Task ReactionService_ShouldUpsert_AndRemove()
        {
            await using var conn = new SqlConnection(_connStr);
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
            await using var conn = new SqlConnection(_connStr);
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

            _comments.Delete(c1, author, isAdmin: false);
            _comments.Count("LESSON", postId).Should().Be(1);

            var c2 = (int)all[1].GetType().GetProperty("CommentId")!.GetValue(all[1])!;
            _comments.Delete(c2, tutor, isAdmin: false);
            _comments.Count("LESSON", postId).Should().Be(0);
        }
    }
}
