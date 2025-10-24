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

[TestFixture]
public class RequestServiceDbTests
{
    private IConfiguration _config = null!;
    private DbHelper _db = null!;
    private RequestService _sut = null!;
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
        var requestRepo = new RequestRepository(_db);
        _sut = new RequestService(requestRepo);
    }

    [OneTimeTearDown]
    public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

    [SetUp]
    public async Task Setup()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        var wipe = @"
DELETE FROM dbo.Requests; 
DELETE FROM dbo.Users;
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Requests'))
    DBCC CHECKIDENT('dbo.Requests', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);";
        await using var cmd = new SqlCommand(wipe, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertUser(SqlConnection conn, string name, string email)
    {
        var cmd = new SqlCommand("INSERT INTO dbo.Users (FullName, Email) VALUES (@n,@e); SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
        cmd.Parameters.AddWithValue("@n", name);
        cmd.Parameters.AddWithValue("@e", email);
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

    private async Task<int> InsertRequest(SqlConnection conn, int learnerId, string skill, string? topic = null, string? desc = null, string status = "OPEN")
    {
        var cmd = new SqlCommand(@"
INSERT INTO dbo.Requests (LearnerId, SkillName, Topic, Description, Status) 
VALUES (@l, @s, @t, @d, @st);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
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
        await using var conn = new SqlConnection(_connStr);
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
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        var u1 = await InsertUser(conn, "Bob", "bob@example.com");
        var u2 = await InsertUser(conn, "Cara", "cara@example.com");

        var older = await InsertRequest(conn, u1, "Math");
        await Task.Delay(50);
        var newer = await InsertRequest(conn, u2, "English");

    var list = _sut.GetAllRequests(null);
        list[0].SkillName.Should().Be("English");
        list[1].SkillName.Should().Be("Math");
    }

    [Test]
    public async Task UpdateRequest_ShouldChangeFields()
    {
        await using var conn = new SqlConnection(_connStr);
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
        await using var conn = new SqlConnection(_connStr);
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
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        var u1 = await InsertUser(conn, "John Doe", "john@example.com");
        var u2 = await InsertUser(conn, "Jane Smith", "jane@example.com");
        await InsertRequest(conn, u1, "Mathematics", "Algebra", "Equations");
        await InsertRequest(conn, u2, "Language", "Grammar", "Punctuation");

    _sut.SearchRequests("math", null).Should().HaveCount(1);
    _sut.SearchRequests("doe", null).Should().HaveCount(1);
    _sut.SearchRequests("punct", null).Should().HaveCount(1);
    }

    [Test]
    public async Task DeleteRequest_ShouldRemove()
    {
        await using var conn = new SqlConnection(_connStr);
        await conn.OpenAsync();
        var u = await InsertUser(conn, "Kate", "kate@example.com");
        var id = await InsertRequest(conn, u, "Science");

        _sut.DeleteRequest(id);
        _sut.GetById(id).Should().BeNull();
    }
}
