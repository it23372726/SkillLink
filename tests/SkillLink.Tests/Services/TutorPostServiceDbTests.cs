using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SkillLink.API.Data;
using SkillLink.API.Models;
using SkillLink.API.Repositories;
using SkillLink.API.Services;
using SkillLink.Tests.Db;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class TutorPostServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private TutorPostService _sut = null!;
        private string _connStr = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _connStr = await TestDbUtil.EnsureTestDbAsync();

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", _connStr }
                })
                .Build();

            _dbHelper = new DbHelper(_config);

            var repo = new TutorPostRepository(_dbHelper);
            _sut = new TutorPostService(repo);
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
DELETE FROM dbo.TutorPostParticipants;
DELETE FROM dbo.TutorPosts;
DELETE FROM dbo.Users;

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.TutorPostParticipants'))
    DBCC CHECKIDENT('dbo.TutorPostParticipants', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.TutorPosts'))
    DBCC CHECKIDENT('dbo.TutorPosts', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(SqlConnection conn, string name, string email)
        {
            var cmd = new SqlCommand(@"
INSERT INTO dbo.Users (FullName, Email) VALUES (@n, @e);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        private async Task<int> InsertPostAsync(SqlConnection conn, int tutorId, string title, int max = 2, string status = "Open")
        {
            var cmd = new SqlCommand(@"
INSERT INTO dbo.TutorPosts (TutorId, Title, Description, MaxParticipants, Status)
VALUES (@t, @ti, '', @m, @st);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@t", tutorId);
            cmd.Parameters.AddWithValue("@ti", title);
            cmd.Parameters.AddWithValue("@m", max);
            cmd.Parameters.AddWithValue("@st", status);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        [Test]
        public async Task CreatePost_Then_GetById_ShouldReturnInserted()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutorId = await InsertUserAsync(conn, "Tutor X", "tx@example.com");
            var id = _sut.CreatePost(new TutorPost
            {
                TutorId = tutorId,
                Title = "C# Basics",
                Description = "Intro",
                MaxParticipants = 3,
            });

            id.Should().BeGreaterThan(0);

            var found = _sut.GetById(id);
            found.Should().NotBeNull();
            found!.Title.Should().Be("C# Basics");
            found.TutorId.Should().Be(tutorId);
            found.Status.Should().Be("Open");
            found.CurrentParticipants.Should().Be(0);
        }

        [Test]
        public async Task GetPosts_ShouldInclude_CurrentParticipants_AndTutor()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var t1 = await InsertUserAsync(conn, "Tutor A", "a@example.com");
            var u2 = await InsertUserAsync(conn, "User B", "b@example.com");

            var p1 = await InsertPostAsync(conn, t1, "Post A", max: 2, status: "Open");
            var ins = new SqlCommand("INSERT INTO dbo.TutorPostParticipants (PostId, UserId) VALUES (@p,@u)", conn);
            ins.Parameters.AddWithValue("@p", p1);
            ins.Parameters.AddWithValue("@u", u2);
            await ins.ExecuteNonQueryAsync();

            var list = _sut.GetPosts();
            list.Should().HaveCount(1);
            list[0].Title.Should().Be("Post A");
            list[0].TutorName.Should().Be("Tutor A");
            list[0].CurrentParticipants.Should().Be(1);
        }

        [Test]
        public async Task AcceptPost_ShouldInsert_And_CloseWhenFull()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUserAsync(conn, "Tutor", "t@example.com");
            var u1 = await InsertUserAsync(conn, "U1", "u1@example.com");
            var u2 = await InsertUserAsync(conn, "U2", "u2@example.com");
            var postId = await InsertPostAsync(conn, tutor, "Small class", max: 2);

            _sut.AcceptPost(postId, u1);
            _sut.AcceptPost(postId, u2); // fills → closes

            var refreshed = _sut.GetById(postId)!;
            refreshed.CurrentParticipants.Should().Be(2);
            refreshed.Status.Should().Be("Closed");
        }

        [Test]
        public async Task AcceptPost_ShouldReject_Duplicate_Self_Full_Closed_Scheduled()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUserAsync(conn, "Tutor", "t@example.com");
            var u1 = await InsertUserAsync(conn, "U1", "u1@example.com");
            var u2 = await InsertUserAsync(conn, "U2", "u2@example.com");

            // duplicate
            var pidDup = await InsertPostAsync(conn, tutor, "dup", max: 2, status: "Open");
            _sut.AcceptPost(pidDup, u1);
            FluentActions.Invoking(() => _sut.AcceptPost(pidDup, u1))
                .Should().Throw<InvalidOperationException>().WithMessage("*already*");

            // self
            var pidSelf = await InsertPostAsync(conn, tutor, "self", max: 2, status: "Open");
            FluentActions.Invoking(() => _sut.AcceptPost(pidSelf, tutor))
                .Should().Throw<InvalidOperationException>().WithMessage("*cannot accept your own*");

            // full
            var pidFull = await InsertPostAsync(conn, tutor, "full", max: 1, status: "Open");
            _sut.AcceptPost(pidFull, u1);
            FluentActions.Invoking(() => _sut.AcceptPost(pidFull, u2))
                .Should().Throw<InvalidOperationException>().WithMessage("*full*");

            // closed
            var pidClosed = await InsertPostAsync(conn, tutor, "closed", max: 3, status: "Closed");
            FluentActions.Invoking(() => _sut.AcceptPost(pidClosed, u1))
                .Should().Throw<InvalidOperationException>().WithMessage("*already closed*");

            // scheduled
            var pidSched = await InsertPostAsync(conn, tutor, "sched", max: 3, status: "Scheduled");
            FluentActions.Invoking(() => _sut.AcceptPost(pidSched, u1))
                .Should().Throw<InvalidOperationException>().WithMessage("*scheduled*");
        }

        [Test]
        public async Task Schedule_ShouldUpdate_Status_AndDate()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUserAsync(conn, "Tutor", "t@example.com");
            var pid = await InsertPostAsync(conn, tutor, "S1", max: 5, status: "Open");

            var when = DateTime.UtcNow.AddDays(2);
            _sut.Schedule(pid, when);

            var post = _sut.GetById(pid)!;
            post.Status.Should().Be("Scheduled");
            post.ScheduledAt.Should().BeCloseTo(when, TimeSpan.FromSeconds(2));
        }

        [Test]
        public async Task UpdatePost_ShouldEnforceOwner_And_MaxParticipantsConstraint()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUserAsync(conn, "Tutor", "t@example.com");
            var other = await InsertUserAsync(conn, "Other", "o@example.com");
            var learner = await InsertUserAsync(conn, "L", "l@example.com");
            var pid = await InsertPostAsync(conn, tutor, "Up", max: 2, status: "Open");

            // Not owner
            var dto = new UpdateTutorPostDto { Title = "New", Description = "D", MaxParticipants = 2 };
            FluentActions.Invoking(() => _sut.UpdatePost(pid, other, dto))
                .Should().Throw<UnauthorizedAccessException>();

            // participants > new max → invalid
            var accept = new SqlCommand("INSERT INTO dbo.TutorPostParticipants (PostId, UserId) VALUES (@p,@u)", conn);
            accept.Parameters.AddWithValue("@p", pid);
            accept.Parameters.AddWithValue("@u", learner);
            await accept.ExecuteNonQueryAsync();

            var dtoTooSmall = new UpdateTutorPostDto { Title = "New2", MaxParticipants = 0, Description = "x" };
            FluentActions.Invoking(() => _sut.UpdatePost(pid, tutor, dtoTooSmall))
                .Should().Throw<InvalidOperationException>();

            // Valid update
            var dtoOk = new UpdateTutorPostDto { Title = "New3", MaxParticipants = 3, Description = "updated", Status = "Open" };
            _sut.UpdatePost(pid, tutor, dtoOk);
            var updated = _sut.GetById(pid)!;
            updated.Title.Should().Be("New3");
            updated.MaxParticipants.Should().Be(3);
        }

        [Test]
        public async Task DeletePost_ShouldEnforceOwnerOnly()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUserAsync(conn, "Tutor", "t@example.com");
            var other = await InsertUserAsync(conn, "Other", "o@example.com");
            var pid = await InsertPostAsync(conn, tutor, "Del", max: 1, status: "Open");

            FluentActions.Invoking(() => _sut.DeletePost(pid, other))
                .Should().Throw<UnauthorizedAccessException>();

            _sut.DeletePost(pid, tutor);
            _sut.GetById(pid).Should().BeNull();
        }

        [Test]
        public async Task SetImageUrl_ShouldPersistValue()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var tutor = await InsertUserAsync(conn, "Tutor", "t@example.com");
            var pid = await InsertPostAsync(conn, tutor, "Img", max: 2, status: "Open");

            _sut.SetImageUrl(pid, "/uploads/lessons/x.png");
            var post = _sut.GetById(pid)!;
            post.ImageUrl.Should().Be("/uploads/lessons/x.png");
        }
    }
}
