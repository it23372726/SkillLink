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
    public class SessionServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _db = null!;
        private SessionService _sut = null!;
        private string _connStr = null!;
        private int _learnerId; // reusable learner for Request FK

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

            _db = new DbHelper(_config);
            _sut = new SessionService(new SessionRepository(_db));

            // Ensure a reusable learner user
            _learnerId = await EnsureLearnerAsync();
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var sql = @"
DELETE FROM dbo.Sessions;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Sessions'))
    DBCC CHECKIDENT('dbo.Sessions', RESEED, 0);

DELETE FROM dbo.Requests;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Requests'))
    DBCC CHECKIDENT('dbo.Requests', RESEED, 0);";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> EnsureLearnerAsync()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var find = new SqlCommand("SELECT TOP 1 UserId FROM dbo.Users WHERE Email LIKE 'session-test-learner@%'", conn);
            var idObj = await find.ExecuteScalarAsync();
            if (idObj != null && idObj != DBNull.Value)
                return Convert.ToInt32(idObj);

            var email = $"session-test-learner@{Guid.NewGuid():N}.local";
            var ins = new SqlCommand(@"
INSERT INTO dbo.Users (FullName, Email, Role, IsActive, EmailVerified, CreatedAt)
VALUES ('Session Test Learner', @em, 'Learner', 1, 1, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            ins.Parameters.AddWithValue("@em", email);
            return Convert.ToInt32(await ins.ExecuteScalarAsync());
        }

        private int EnsureTutor(string key)
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var email = $"session-test-tutor-{key}@local";

            using (var find = new SqlCommand("SELECT TOP 1 UserId FROM dbo.Users WHERE Email=@em", conn))
            {
                find.Parameters.AddWithValue("@em", email);
                var idObj = find.ExecuteScalar();
                if (idObj != null && idObj != DBNull.Value)
                    return Convert.ToInt32(idObj);
            }

            var sql = @"
INSERT INTO dbo.Users (FullName, Email, Role, ReadyToTeach, IsActive, EmailVerified, CreatedAt)
VALUES (@name, @em, 'Tutor', 1, 1, 1, SYSUTCDATETIME());
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@name", $"Tutor {key}");
            cmd.Parameters.AddWithValue("@em", email);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private int NewRequest(string skill = "TestSkill")
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var sql = @"
INSERT INTO dbo.Requests (LearnerId, SkillName, Topic, Description, Status)
VALUES (@learner, @skill, NULL, NULL, 'OPEN');
SELECT CAST(SCOPE_IDENTITY() AS INT);";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@learner", _learnerId);
            cmd.Parameters.AddWithValue("@skill", skill);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        [Test]
        public void AddSession_Then_GetById_ShouldReturnInserted()
        {
            var reqId = NewRequest("Physics");
            var tutorId = EnsureTutor("add");

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

            var tutorA = EnsureTutor("A");
            var tutorB = EnsureTutor("B");

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
            var tutorId = EnsureTutor("upd");

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
            var tutorId = EnsureTutor("del");

            _sut.AddSession(new Session { RequestId = r1, TutorId = tutorId, Status = "PENDING" });
            var first = _sut.GetAllSessions()[0];

            _sut.Delete(first.SessionId);
            _sut.GetById(first.SessionId).Should().BeNull();
        }
    }
}
