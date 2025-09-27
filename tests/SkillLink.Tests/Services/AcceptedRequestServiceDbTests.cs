using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;

[TestFixture]
public class AcceptedRequestServiceDbTests
{
    private Testcontainers.MySql.MySqlContainer _mysql = null!;
    private bool _ownsContainer = false;
    private string? _externalConnStr = null;

    private IConfiguration _config = null!;
    private DbHelper _db = null!;
    private AcceptedRequestService _sut = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
        if (!string.IsNullOrWhiteSpace(external)) _externalConnStr = external;

        var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var sock1 = "/var/run/docker.sock";
        var sock2 = Path.Combine(home, ".docker/run/docker.sock");
        var hasDocker = File.Exists(sock1) || File.Exists(sock2) || !string.IsNullOrEmpty(dockerHost);
        if (!(_externalConnStr != null || hasDocker))
        {
            Assert.Ignore("Docker not available. Skipping AcceptedRequestService DB tests.");
            return;
        }

        if (_externalConnStr == null)
        {
            try
            {
                _mysql = new Testcontainers.MySql.MySqlBuilder()
                    .WithImage("mysql:8.0")
                    .WithDatabase("skilllink_test_acc")
                    .WithUsername("testuser")
                    .WithPassword("testpass")
                    .Build();
                await _mysql.StartAsync();
                _ownsContainer = true;
            }
            catch (Exception ex)
            {
                Assert.Ignore($"Docker not available. Skipping DB tests. Details: {ex.Message}");
                return;
            }
        }

        var connStr = _externalConnStr ?? _mysql.GetConnectionString();

        // Create schema: Users, Requests, AcceptedRequests
        await using (var conn = new MySqlConnection(connStr))
        {
            await conn.OpenAsync();
            var sql = @"
                CREATE TABLE IF NOT EXISTS Users (
                  UserId INT AUTO_INCREMENT PRIMARY KEY,
                  FullName VARCHAR(255) NOT NULL,
                  Email VARCHAR(255) NOT NULL UNIQUE
                );
                CREATE TABLE IF NOT EXISTS Requests (
                  RequestId INT AUTO_INCREMENT PRIMARY KEY,
                  LearnerId INT NOT NULL,
                  SkillName VARCHAR(255) NOT NULL,
                  Topic TEXT NULL,
                  Status VARCHAR(50) NOT NULL DEFAULT 'OPEN',
                  CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  Description TEXT NULL,
                  FOREIGN KEY (LearnerId) REFERENCES Users(UserId)
                );
                CREATE TABLE IF NOT EXISTS AcceptedRequests (
                  AcceptedRequestId INT AUTO_INCREMENT PRIMARY KEY,
                  RequestId INT NOT NULL,
                  AcceptorId INT NOT NULL,
                  AcceptedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                  Status VARCHAR(50) NOT NULL DEFAULT 'PENDING',
                  ScheduleDate DATETIME NULL,
                  MeetingType VARCHAR(50) NULL,
                  MeetingLink VARCHAR(512) NULL,
                  FOREIGN KEY (RequestId) REFERENCES Requests(RequestId),
                  FOREIGN KEY (AcceptorId) REFERENCES Users(UserId)
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
        _sut = new AcceptedRequestService(_db);
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown()
    {
        if (_ownsContainer && _mysql != null)
            await _mysql.DisposeAsync();
    }

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        var wipe = @"DELETE FROM AcceptedRequests; DELETE FROM Requests; DELETE FROM Users;
                     ALTER TABLE AcceptedRequests AUTO_INCREMENT=1; 
                     ALTER TABLE Requests AUTO_INCREMENT=1; 
                     ALTER TABLE Users AUTO_INCREMENT=1;";
        await using var cmd = new MySqlCommand(wipe, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertUser(MySqlConnection conn, string name, string email)
    {
        var cmd = new MySqlCommand("INSERT INTO Users (FullName, Email) VALUES (@n,@e); SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@e", email);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> InsertRequest(MySqlConnection conn, int learnerId, string skill, string? topic=null, string? desc=null)
    {
        var cmd = new MySqlCommand(@"INSERT INTO Requests (LearnerId, SkillName, Topic, Description)
                                     VALUES (@l,@s,@t,@d); SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@l", learnerId);
        cmd.Parameters.AddWithValue("@s", skill);
        cmd.Parameters.AddWithValue("@t", (object?)topic ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)desc ?? DBNull.Value);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Test]
    public async Task AcceptRequest_ShouldInsert_And_PreventDuplicates()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        var learner = await InsertUser(conn, "Learner", "learner@example.com");
        var acceptor = await InsertUser(conn, "Tutor", "tutor@example.com");
        var reqId = await InsertRequest(conn, learner, "Math", "Algebra");

        _sut.AcceptRequest(reqId, acceptor);

        // 2nd accept should throw
        Action again = () => _sut.AcceptRequest(reqId, acceptor);
        again.Should().Throw<Exception>().WithMessage("*already*");
    }

    [Test]
    public async Task GetAcceptedRequestsByUser_ShouldReturnJoinedDetails()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
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
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
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
        using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        conn.Open();

        // seed
        var l = InsertUser(conn, "Z", "z@example.com").GetAwaiter().GetResult();
        var a = InsertUser(conn, "A", "a@example.com").GetAwaiter().GetResult();
        var r = InsertRequest(conn, l, "C#").GetAwaiter().GetResult();

        // accept manually
        var ins = new MySqlCommand("INSERT INTO AcceptedRequests (RequestId, AcceptorId) VALUES (@r,@a); SELECT LAST_INSERT_ID();", conn);
        ins.Parameters.AddWithValue("@r", r);
        ins.Parameters.AddWithValue("@a", a);
        var arId = Convert.ToInt32(ins.ExecuteScalar());

        _sut.UpdateAcceptanceStatus(arId, "COMPLETED");

        var check = new MySqlCommand("SELECT Status FROM AcceptedRequests WHERE AcceptedRequestId=@id", conn);
        check.Parameters.AddWithValue("@id", arId);
        (check.ExecuteScalar() as string).Should().Be("COMPLETED");
    }

    [Test]
    public void ScheduleMeeting_ShouldSetFields_AndStatus()
    {
        using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        conn.Open();

        var l = InsertUser(conn, "L", "l@example.com").GetAwaiter().GetResult();
        var a = InsertUser(conn, "A", "a@example.com").GetAwaiter().GetResult();
        var r = InsertRequest(conn, l, "Python").GetAwaiter().GetResult();

        var ins = new MySqlCommand("INSERT INTO AcceptedRequests (RequestId, AcceptorId) VALUES (@r,@a); SELECT LAST_INSERT_ID();", conn);
        ins.Parameters.AddWithValue("@r", r);
        ins.Parameters.AddWithValue("@a", a);
        var arId = Convert.ToInt32(ins.ExecuteScalar());

        var when = DateTime.UtcNow.AddDays(2);
        _sut.ScheduleMeeting(arId, when, "Zoom", "https://zoom.us/abc");

        var check = new MySqlCommand("SELECT Status, MeetingType, MeetingLink FROM AcceptedRequests WHERE AcceptedRequestId=@id", conn);
        check.Parameters.AddWithValue("@id", arId);
        using var rd = check.ExecuteReader();
        rd.Read().Should().BeTrue();
        rd.GetString("Status").Should().Be("SCHEDULED");
        rd.GetString("MeetingType").Should().Be("Zoom");
        rd.GetString("MeetingLink").Should().Be("https://zoom.us/abc");
    }

    [Test]
    public async Task GetRequestsIAskedFor_ShouldReturn_Acceptor_Details()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        var me = await InsertUser(conn, "Requester", "req@example.com");
        var tutor = await InsertUser(conn, "Tutor", "tutor@example.com");
        var req = await InsertRequest(conn, me, "Java");

        _sut.AcceptRequest(req, tutor);

        var list = _sut.GetRequestsIAskedFor(me);
        list.Should().HaveCount(1);
        list[0].RequesterName.Should().Be("Tutor"); // (mapped from acceptor)
        list[0].RequesterEmail.Should().Be("tutor@example.com");
    }
}
