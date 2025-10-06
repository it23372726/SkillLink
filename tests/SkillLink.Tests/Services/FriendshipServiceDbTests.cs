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
    public class FriendshipServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private FriendshipService _sut = null!;
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
            var repo = new FriendshipRepository(_dbHelper);
            _sut = new FriendshipService(repo);
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            var sql = @"
DELETE FROM dbo.Friendships;
DELETE FROM dbo.Users;

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Friendships'))
    DBCC CHECKIDENT('dbo.Friendships', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> InsertUserAsync(SqlConnection conn, string name, string email, string? pic = null)
        {
            var cmd = new SqlCommand(
                @"INSERT INTO dbo.Users (FullName, Email, ProfilePicture) VALUES (@n, @e, @p);
                  SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@n", name);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", (object?)pic ?? DBNull.Value);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task Follow_ShouldInsert_And_PreventDuplicates()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var alice = await InsertUserAsync(conn, "Alice", "alice@example.com");
            var bob = await InsertUserAsync(conn, "Bob", "bob@example.com");

            _sut.Follow(alice, bob);

            Action again = () => _sut.Follow(alice, bob);
            again.Should().Throw<InvalidOperationException>()
                 .WithMessage("Already following");

            var countCmd = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            countCmd.Parameters.AddWithValue("@f", alice);
            countCmd.Parameters.AddWithValue("@fd", bob);
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            count.Should().Be(1);
        }

        [Test]
        public async Task Unfollow_ShouldDeleteRow()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();

            var a = await InsertUserAsync(conn, "A", "a@example.com");
            var b = await InsertUserAsync(conn, "B", "b@example.com");

            _sut.Follow(a, b);
            _sut.Unfollow(a, b);

            var countCmd = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Friendships WHERE FollowerId=@f AND FollowedId=@fd", conn);
            countCmd.Parameters.AddWithValue("@f", a);
            countCmd.Parameters.AddWithValue("@fd", b);
            var count = Convert.ToInt32(await countCmd.ExecuteScalarAsync());
            count.Should().Be(0);
        }

        [Test]
        public async Task GetMyFriends_ShouldReturnFollowedUsers()
        {
            await using var conn = new SqlConnection(_connStr);
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
            await using var conn = new SqlConnection(_connStr);
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
            await using var conn = new SqlConnection(_connStr);
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
