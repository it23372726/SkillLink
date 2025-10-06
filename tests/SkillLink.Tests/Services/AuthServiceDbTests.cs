using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Services;
using SkillLink.API.Data;
using SkillLink.API.Repositories;
using SkillLink.Tests.Db;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class AuthServiceDbTests
    {
        private IConfiguration _config = null!;
        private DbHelper _db = null!;
        private AuthService _sut = null!;
        private string _connStr = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            _connStr = await TestDbUtil.EnsureTestDbAsync();

            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", _connStr },
                    { "Jwt:Key", "THIS_IS_A_LONG_TEST_KEY_FOR_HMAC_256__CHANGE_ME" },
                    { "Jwt:Issuer", "SkillLink.Tests" },
                    { "Jwt:Audience", "SkillLink.Tests" },
                    { "Jwt:ExpireMinutes", "30" },
                    { "Api:BaseUrl", "http://localhost:5159" },
                    { "Smtp:Host", "localhost" },
                    { "Smtp:Port", "25" },
                    { "Smtp:User", "" },
                    { "Smtp:Pass", "" },
                    { "Smtp:From", "noreply@skilllink.local" },
                    { "Smtp:UseSSL", "false" }
                })
                .Build();

            _db = new DbHelper(_config);
            var repo  = new AuthRepository(_db);
            var email = new EmailService(_config);
            _sut = new AuthService(_config, email, repo);
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

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private async Task<int> InsertUserAsync(SqlConnection conn, string fullName, string email, string password, string role = "Learner", bool verified = true, bool isActive = true, bool readyToTeach = false)
        {
            var cmd = new SqlCommand(@"
INSERT INTO dbo.Users (FullName, Email, PasswordHash, Role, CreatedAt, Bio, Location, ProfilePicture,
                       ReadyToTeach, IsActive, EmailVerified)
VALUES (@n, @e, @p, @r, SYSUTCDATETIME(), NULL, NULL, NULL, @t, @a, @v);
SELECT CAST(SCOPE_IDENTITY() AS INT);", conn);
            cmd.Parameters.AddWithValue("@n", fullName);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", HashPassword(password));
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@t", readyToTeach);
            cmd.Parameters.AddWithValue("@a", isActive);
            cmd.Parameters.AddWithValue("@v", verified);
            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public void Register_ShouldInsert_Unverified_WithToken()
        {
            _sut.Register(new RegisterRequest
            {
                FullName = "Alice",
                Email = "alice@example.com",
                Password = "P@ssw0rd",
                Role = "Learner"
            });

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var get = new SqlCommand("SELECT EmailVerified, EmailVerificationToken FROM dbo.Users WHERE Email=@e", conn);
            get.Parameters.AddWithValue("@e", "alice@example.com");
            using var r = get.ExecuteReader();
            r.Read().Should().BeTrue();

            Convert.ToBoolean(r["EmailVerified"]).Should().BeFalse();

            var token = r.IsDBNull(r.GetOrdinal("EmailVerificationToken")) ? null : r.GetString(r.GetOrdinal("EmailVerificationToken"));
            token.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void VerifyEmailByToken_ShouldSetVerified_And_ClearToken()
        {
            _sut.Register(new RegisterRequest
            {
                FullName = "Bob",
                Email = "bob@example.com",
                Password = "P@ssw0rd"
            });

            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var getToken = new SqlCommand("SELECT EmailVerificationToken FROM dbo.Users WHERE Email=@e", conn);
            getToken.Parameters.AddWithValue("@e", "bob@example.com");
            var token = (string)getToken.ExecuteScalar()!;
            token.Should().NotBeNullOrEmpty();

            var ok = _sut.VerifyEmailByToken(token);
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT EmailVerified, EmailVerificationToken FROM dbo.Users WHERE Email=@e", conn);
            check.Parameters.AddWithValue("@e", "bob@example.com");
            using var rr = check.ExecuteReader();
            rr.Read().Should().BeTrue();

            Convert.ToBoolean(rr["EmailVerified"]).Should().BeTrue();
            rr.IsDBNull(rr.GetOrdinal("EmailVerificationToken")).Should().BeTrue();
        }

        [Test]
        public void Login_ShouldReturnNull_WhenNotVerified()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            InsertUserAsync(conn, "Cara", "cara@example.com", "secret", verified: false, isActive: true).GetAwaiter().GetResult();

            var token = _sut.Login(new LoginRequest { Email = "cara@example.com", Password = "secret" });
            token.Should().BeNull();
        }

        [Test]
        public void Login_ShouldReturnToken_WhenVerified_AndPasswordCorrect()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            InsertUserAsync(conn, "Drew", "drew@example.com", "secret", verified: true, isActive: true).GetAwaiter().GetResult();

            var token = _sut.Login(new LoginRequest { Email = "drew@example.com", Password = "secret" });
            token.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void SetActive_ShouldUpdate()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var id = InsertUserAsync(conn, "Elle", "elle@example.com", "pw", verified: true, isActive: true).GetAwaiter().GetResult();

            var ok = _sut.SetActive(id, false);
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT IsActive FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var active = Convert.ToInt32((bool)(check.ExecuteScalar() ?? false));
            active.Should().Be(0);
        }

        [Test]
        public void UpdateTeachMode_ShouldToggleRole_AndFlag()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var id = InsertUserAsync(conn, "Finn", "finn@example.com", "pw", verified: true, isActive: true, readyToTeach: false).GetAwaiter().GetResult();

            var ok = _sut.UpdateTeachMode(id, true);
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT ReadyToTeach, Role FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            using var r = check.ExecuteReader();
            r.Read().Should().BeTrue();

            Convert.ToBoolean(r["ReadyToTeach"]).Should().BeTrue();
            r.GetString(r.GetOrdinal("Role")).Should().Be("Tutor");
        }

        [Test]
        public void UpdateUserProfile_ShouldPersist()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var id = InsertUserAsync(conn, "Gail", "gail@example.com", "pw", verified: true, isActive: true).GetAwaiter().GetResult();

            var ok = _sut.UpdateUserProfile(id, new UpdateProfileRequest
            {
                FullName = "Gail Updated",
                Bio = "Hello",
                Location = "Colombo"
            });
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT FullName, Bio, Location FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            using var r = check.ExecuteReader();
            r.Read().Should().BeTrue();
            r.GetString(r.GetOrdinal("FullName")).Should().Be("Gail Updated");
            r.GetString(r.GetOrdinal("Bio")).Should().Be("Hello");
            r.GetString(r.GetOrdinal("Location")).Should().Be("Colombo");
        }

        [Test]
        public void UpdateProfilePicture_ShouldPersist()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var id = InsertUserAsync(conn, "Hank", "hank@example.com", "pw").GetAwaiter().GetResult();

            var ok = _sut.UpdateProfilePicture(id, "/uploads/profiles/img.png");
            ok.Should().BeTrue();

            var check = new SqlCommand("SELECT ProfilePicture FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            (check.ExecuteScalar() as string).Should().Be("/uploads/profiles/img.png");
        }

        [Test]
        public void DeleteUserFromDB_ShouldDeleteNonAdmin()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();
            var id = InsertUserAsync(conn, "Ivy", "ivy@example.com", "pw", role: "Learner").GetAwaiter().GetResult();

            _sut.DeleteUserFromDB(id);

            var check = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            Convert.ToInt32(check.ExecuteScalar()).Should().Be(0);
        }

        [Test]
        public void DeleteUserFromDB_ShouldPreventDeletingLastAdmin()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var adminId = InsertUserAsync(conn, "Admin One", "admin1@example.com", "pw", role: "Admin").GetAwaiter().GetResult();

            Action act = () => _sut.DeleteUserFromDB(adminId);
            act.Should().Throw<InvalidOperationException>().WithMessage("*last admin*");
        }

        [Test]
        public void DeleteUserFromDB_ShouldAllow_IfMoreThanOneAdmin()
        {
            using var conn = new SqlConnection(_connStr);
            conn.Open();

            var admin1 = InsertUserAsync(conn, "Admin One", "admin1@example.com", "pw", role: "Admin").GetAwaiter().GetResult();
            var admin2 = InsertUserAsync(conn, "Admin Two", "admin2@example.com", "pw", role: "Admin").GetAwaiter().GetResult();

            _sut.DeleteUserFromDB(admin1);

            var check = new SqlCommand("SELECT COUNT(*) FROM dbo.Users WHERE Role='Admin'", conn);
            Convert.ToInt32(check.ExecuteScalar()).Should().Be(1);
        }
    }
}
