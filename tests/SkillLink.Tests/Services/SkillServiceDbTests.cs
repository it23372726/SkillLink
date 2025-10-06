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
    public class SkillServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private SkillService _sut = null!;
        private AuthService _auth = null!;
        private string _connStr = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _connStr = await TestDbUtil.EnsureTestDbAsync();

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", _connStr },
                    { "Jwt:Key", "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx" }, // 32+ chars
                    { "Jwt:Issuer", "SkillLink" },
                    { "Jwt:Audience", "SkillLink" },
                    { "Jwt:ExpireMinutes", "60" }
                })
                .Build();

            _dbHelper = new DbHelper(_config);

            var email = new EmailService(_config);
            var authRepo = new AuthRepository(_dbHelper);
            _auth = new AuthService(_config, email, authRepo);

            var skillRepo = new SkillRepository(_dbHelper);
            _sut = new SkillService(skillRepo, _auth);
        }

        [OneTimeTearDown]
        public async Task OneTimeTeardown() => await TestDbUtil.DisposeAsync();

        [SetUp]
        public async Task Setup()
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            var sql = @"
DELETE FROM dbo.UserSkills;
DELETE FROM dbo.Skills;
DELETE FROM dbo.Users;

IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.UserSkills'))
    DBCC CHECKIDENT('dbo.UserSkills', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Skills'))
    DBCC CHECKIDENT('dbo.Skills', RESEED, 0);
IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE [object_id] = OBJECT_ID('dbo.Users'))
    DBCC CHECKIDENT('dbo.Users', RESEED, 0);

INSERT INTO dbo.Users (FullName, Email) VALUES ('Alice','alice@example.com');";
            await using var cmd = new SqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private async Task<int> GetUserIdByEmail(string email)
        {
            await using var conn = new SqlConnection(_connStr);
            await conn.OpenAsync();
            var cmd = new SqlCommand("SELECT UserId FROM dbo.Users WHERE Email=@e", conn);
            cmd.Parameters.AddWithValue("@e", email);
            return Convert.ToInt32(await cmd.ExecuteScalarAsync());
        }

        [Test]
        public async Task AddSkill_ShouldInsertSkill_AndMapUser_UpsertLevel()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "React", Level = "Beginner" });
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "React", Level = "Advanced" });

            var list = _sut.GetUserSkills(uid);
            list.Should().HaveCount(1);
            list[0].Skill!.Name.Should().Be("React");
            list[0].Level.Should().Be("Advanced");
        }

        [Test]
        public async Task DeleteUserSkill_ShouldRemoveMapping()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "C#", Level = "Intermediate" });

            var skills = _sut.GetUserSkills(uid);
            skills.Should().HaveCount(1);
            var skillId = skills[0].Skill!.SkillId;

            _sut.DeleteUserSkill(uid, skillId);
            _sut.GetUserSkills(uid).Should().BeEmpty();
        }

        [Test]
        public async Task GetUserSkills_ShouldReturnSkillWithLevel()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "MySQL", Level = "Beginner" });

            var list = _sut.GetUserSkills(uid);
            list.Should().HaveCount(1);
            list[0].Skill!.Name.Should().Be("MySQL");
            list[0].Level.Should().Be("Beginner");
        }

        [Test]
        public async Task SuggestSkills_ShouldReturnPrefixMatches()
        {
            var uid = await GetUserIdByEmail("alice@example.com");

            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "React", Level = "Intermediate" });
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "Redux", Level = "Intermediate" });
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "Node.js", Level = "Intermediate" });

            var res = _sut.SuggestSkills("Re");
            res.Should().HaveCount(2);
            res.Should().Contain(s => s.Name == "React");
            res.Should().Contain(s => s.Name == "Redux");
        }

        [Test]
        public async Task GetUsersBySkill_ShouldReturnUsersHavingThatSkill()
        {
            var uid = await GetUserIdByEmail("alice@example.com");
            _sut.AddSkill(new AddSkillRequest { UserId = uid, SkillName = "C#", Level = "Advanced" });

            var users = _sut.GetUsersBySkill("C#");
            users.Should().HaveCount(1);
            users[0].UserId.Should().Be(uid);
            users[0].FullName.Should().Be("Alice");
            users[0].Email.Should().Be("alice@example.com");
        }
    }
}
