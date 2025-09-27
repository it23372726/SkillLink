using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;

[TestFixture]
public class RequestServiceDbTests
{
    private Testcontainers.MySql.MySqlContainer _mysql = null!;
    private bool _ownsContainer = false;
    private string? _externalConnStr = null;

    private IConfiguration _config = null!;
    private DbHelper _db = null!;
    private RequestService _sut = null!;

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
            Assert.Ignore("Docker not available. Skipping RequestService DB tests.");
            return;
        }

        if (_externalConnStr == null)
        {
            try
            {
                _mysql = new Testcontainers.MySql.MySqlBuilder()
                    .WithImage("mysql:8.0")
                    .WithDatabase("skilllink_test_requests")
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

        // Create Users + Requests tables
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
        _sut = new RequestService(_db);
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
        var wipe = @"DELETE FROM Requests; DELETE FROM Users; ALTER TABLE Requests AUTO_INCREMENT=1; ALTER TABLE Users AUTO_INCREMENT=1;";
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

    private async Task<int> InsertRequest(MySqlConnection conn, int learnerId, string skill, string? topic = null, string? desc = null, string status = "OPEN")
    {
        var cmd = new MySqlCommand(@"INSERT INTO Requests (LearnerId, SkillName, Topic, Description, Status) 
                                     VALUES (@l, @s, @t, @d, @st); SELECT LAST_INSERT_ID();", conn);
        cmd.Parameters.AddWithValue("@l", learnerId);
        cmd.Parameters.AddWithValue("@s", skill);
        cmd.Parameters.AddWithValue("@t", (object?)topic ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@d", (object?)desc ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@st", status);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    [Test]
    public async Task AddRequest_Then_GetByLearnerId()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();

        var u = await InsertUser(conn, "Alice", "alice@example.com");
        _sut.AddRequest(new Request { LearnerId = u, SkillName = "React", Topic = "Hooks", Description = "useEffect" });

        var list = _sut.GetByLearnerId(u);
        list.Should().HaveCount(1);
        list[0].FullName.Should().Be("Alice");
        list[0].SkillName.Should().Be("React");
    }

    [Test]
    public async Task GetAllRequests_ShouldOrder_Newest_First()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        var u1 = await InsertUser(conn, "Bob", "bob@example.com");
        var u2 = await InsertUser(conn, "Cara", "cara@example.com");

        var older = await InsertRequest(conn, u1, "Math");
        await Task.Delay(50);
        var newer = await InsertRequest(conn, u2, "English");

        var list = _sut.GetAllRequests();
        list[0].SkillName.Should().Be("English");
        list[1].SkillName.Should().Be("Math");
    }

    [Test]
    public async Task UpdateRequest_ShouldChangeFields()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        var u = await InsertUser(conn, "Don", "don@example.com");
        var id = await InsertRequest(conn, u, "Art", "Sketch", "Basics");

        _sut.UpdateRequest(id, new Request { SkillName = "Art 101", Topic = null, Description = "Intro" });
        var r = _sut.GetById(id)!;
        r.SkillName.Should().Be("Art 101");
        r.Topic.Should().BeNull();
        r.Description.Should().Be("Intro");
    }

    [Test]
    public async Task UpdateStatus_ShouldWork()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        var u = await InsertUser(conn, "Eve", "eve@example.com");
        var id = await InsertRequest(conn, u, "Physics");

        _sut.UpdateStatus(id, "CLOSED");
        var r = _sut.GetById(id)!;
        r.Status.Should().Be("CLOSED");
    }

    [Test]
    public async Task SearchRequests_ShouldMatch_Text_And_Name()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        var u1 = await InsertUser(conn, "John Doe", "john@example.com");
        var u2 = await InsertUser(conn, "Jane Smith", "jane@example.com");
        await InsertRequest(conn, u1, "Mathematics", "Algebra", "Equations");
        await InsertRequest(conn, u2, "Language", "Grammar", "Punctuation");

        _sut.SearchRequests("math").Should().HaveCount(1);
        _sut.SearchRequests("doe").Should().HaveCount(1);
        _sut.SearchRequests("punct").Should().HaveCount(1);
    }

    [Test]
    public async Task DeleteRequest_ShouldRemove()
    {
        await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
        await conn.OpenAsync();
        var u = await InsertUser(conn, "Kate", "kate@example.com");
        var id = await InsertRequest(conn, u, "Science");

        _sut.DeleteRequest(id);
        _sut.GetById(id).Should().BeNull();
    }
}
