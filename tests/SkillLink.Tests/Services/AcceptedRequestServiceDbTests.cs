using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SkillLink.API.Data;
using SkillLink.API.Repositories;
using SkillLink.API.Services;
using SkillLink.Tests.Db;

[TestFixture]
public class AcceptedRequestServiceDbTests
{
    private IConfiguration _config = null!;
    private DbHelper _db = null!;
    private AcceptedRequestService _sut = null!;
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

        _db = new DbHelper(_config);
        _sut = new AcceptedRequestService(new AcceptedRequestRepository(_db));
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        var wipe = @"
DELETE FROM dbo.AcceptedRequests; 
DELETE FROM dbo.Requests; 
DELETE FROM dbo.Users;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.AcceptedRequests'))
    DBCC CHECKIDENT('dbo.AcceptedRequests', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Requests'))
    DBCC CHECKIDENT('dbo.Requests', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);";
        await using var cmd = new SqlCommand(wipe, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertUser(SqlConnection conn, string name, string email)
    {
        var cmd = new SqlCommand(@"
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, CreatedAt, IsActive, EmailVerified)
VALUES (@n,@e,'x','Learner',SYSUTCDATETIME(),1,1);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@e", email);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> InsertRequest(SqlConnection conn, int learnerId, string skill, string? topic = null, string? desc = null)
    {
        var cmd = new SqlCommand(@"
INSERT INTO dbo.Requests (LearnerId, SkillName, Topic, Description)
VALUES (@l, @s, @t, @d);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        cmd.Parameters.AddWithValue("@l", learnerId);
        cmd.Parameters.AddWithValue("@s", skill);
        cmd.Parameters.AddWithValue("@t", (object?)topic ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)desc ?? DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Test]
    public async Task AcceptRequest_ShouldInsert_And_PreventDuplicates()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        var learner = await InsertUser(conn, "Learner", "learner@example.com");
        var acceptor = await InsertUser(conn, "Tutor", "tutor@example.com");
        var reqId = await InsertRequest(conn, learner, "Math", "Algebra");

        _sut.AcceptRequest(reqId, acceptor);

        Action again = () => _sut.AcceptRequest(reqId, acceptor);
        again.Should().Throw<Exception>().WithMessage("*already*");
    }

    [Test]
    public async Task GetAcceptedRequestsByUser_ShouldReturnJoinedDetails()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        var me = await InsertUser(conn, "Me Tutor", "me@example.com");
        var l1 = await InsertUser(conn, "Alice", "alice@example.com");
        var r1 = await InsertRequest(conn, l1, "React", "Hooks", "useEffect");

        _sut.AcceptRequest(r1, me);

        var list = _sut.GetAcceptedRequestsByUser(me);
        list.Should().HaveCount(1);
        list[0].SkillName.Should().Be("React");
        list[0].RequesterName.Should().Be("Alice");
        list[0].RequesterEmail.Should().Be("alice@example.com");
    }

    [Test]
    public async Task HasUserAcceptedRequest_ShouldReflect()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        var me = await InsertUser(conn, "Me", "me@example.com");
        var l = await InsertUser(conn, "Lee", "lee@example.com");
        var req = await InsertRequest(conn, l, "Node");

        _sut.HasUserAcceptedRequest(me, req).Should().BeFalse();
        _sut.AcceptRequest(req, me);
        _sut.HasUserAcceptedRequest(me, req).Should().BeTrue();
    }

    [Test]
    public void UpdateAcceptanceStatus_ShouldUpdate()
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();

        var l = InsertUser(conn, "Z", "z@example.com").GetAwaiter().GetResult();
        var a = InsertUser(conn, "A", "a@example.com").GetAwaiter().GetResult();
        var r = InsertRequest(conn, l, "C#").GetAwaiter().GetResult();

        var ins = new SqlCommand(@"
INSERT INTO dbo.AcceptedRequests (RequestId, AcceptorId) 
VALUES (@r,@a);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        ins.Parameters.AddWithValue("@r", r);
        ins.Parameters.AddWithValue("@a", a);
        var arId = Convert.ToInt32(ins.ExecuteScalar());

        _sut.UpdateAcceptanceStatus(arId, "COMPLETED");

        var check = new SqlCommand("SELECT Status FROM dbo.AcceptedRequests WHERE AcceptedRequestId=@id", conn);
        check.Parameters.AddWithValue("@id", arId);
        (check.ExecuteScalar() as string).Should().Be("COMPLETED");
    }

    [Test]
    public void ScheduleMeeting_ShouldSetFields_AndStatus()
    {
        using var conn = new SqlConnection(_connStr);
        conn.Open();

        var l = InsertUser(conn, "L", "l@example.com").GetAwaiter().GetResult();
        var a = InsertUser(conn, "A", "a@example.com").GetAwaiter().GetResult();
        var r = InsertRequest(conn, l, "Python").GetAwaiter().GetResult();

        var ins = new SqlCommand(@"
INSERT INTO dbo.AcceptedRequests (RequestId, AcceptorId) 
VALUES (@r,@a);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        ins.Parameters.AddWithValue("@r", r);
        ins.Parameters.AddWithValue("@a", a);
        var arId = Convert.ToInt32(ins.ExecuteScalar());

        var when = DateTime.UtcNow.AddDays(2);
        _sut.ScheduleMeeting(arId, when, "Zoom", "https://zoom.us/abc");

        var check = new SqlCommand(@"
SELECT Status, MeetingType, MeetingLink 
FROM dbo.AcceptedRequests WHERE AcceptedRequestId=@id", conn);
        check.Parameters.AddWithValue("@id", arId);
        using var rd = check.ExecuteReader();
        rd.Read().Should().BeTrue();
        rd.GetString(rd.GetOrdinal("Status")).Should().Be("SCHEDULED");
        rd.GetString(rd.GetOrdinal("MeetingType")).Should().Be("Zoom");
        rd.GetString(rd.GetOrdinal("MeetingLink")).Should().Be("https://zoom.us/abc");
    }

    [Test]
    public async Task GetRequestsIAskedFor_ShouldReturn_Acceptor_Details()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();

        var me = await InsertUser(conn, "Requester", "req@example.com");
        var tutor = await InsertUser(conn, "Tutor", "tutor@example.com");
        var req = await InsertRequest(conn, me, "Java");

        _sut.AcceptRequest(req, tutor);

        var list = _sut.GetRequestsIAskedFor(me);
        list.Should().HaveCount(1);
        list[0].RequesterName.Should().Be("Tutor"); // acceptor
        list[0].RequesterEmail.Should().Be("tutor@example.com");
    }
}
