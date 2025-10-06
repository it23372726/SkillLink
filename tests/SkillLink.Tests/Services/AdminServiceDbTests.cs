using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SkillLink.API.Data;
using SkillLink.API.Repositories;
using SkillLink.API.Services;
using SkillLink.Tests.Db;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class AdminServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _db = null!;
        private AdminService _sut = null!;
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
            _sut = new AdminService(new AdminRepository(_db));
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            var sql = @"
DELETE FROM dbo.Users; 
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(SqlConnection conn, string name, string email, string role = "Learner", bool isActive = true, bool readyToTeach = false)
        {
            var cmd = new SqlCommand(@"
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, CreatedAt, IsActive, ReadyToTeach, EmailVerified)
VALUES (@n, @e, 'x', @r, SYSUTCDATETIME(), @a, @t, 1);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@a", isActive);
            cmd.Parameters.AddWithValue("@t", readyToTeach);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task GetUsers_ShouldReturn_All_And_FilterBySearch()
        {
            await using var conn = new SqlConnection(_connStr);
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
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var id = await InsertUserAsync(conn, "Cara", "cara@example.com", "Learner", isActive: true);

            var ok = _sut.SetUserActive(id, false);
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT IsActive FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var isActive = Convert.ToInt32((bool)(await check.ExecuteScalarAsync() ?? false));
            isActive.Should().Be(0);
        }

        [Test]
        public async Task SetUserRole_ShouldUpdate()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var id = await InsertUserAsync(conn, "Drew", "drew@example.com", "Learner");

            var ok = _sut.SetUserRole(id, "Tutor");
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT Role FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var role = (string)(await check.ExecuteScalarAsync() ?? "");
            role.Should().Be("Tutor");
        }
    }
}
