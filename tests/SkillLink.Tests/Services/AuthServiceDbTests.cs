using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Services;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class AuthServiceDbTests
    {
        private Testcontainers.MySql.MySqlContainer _mysql = null!;
        private bool _ownsContainer = false;
        private string? _externalConnStr = null;

        private IConfiguration _config = null!;
        private DbHelper _db = null!;
        private EmailService _email = null!;
        private AuthService _sut = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // External or Docker
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _externalConnStr = external;
            }

            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sock1 = "/var/run/docker.sock";
            var sock2 = Path.Combine(home, ".docker/run/docker.sock");
            var dockerSocketExists = File.Exists(sock1) || File.Exists(sock2);

            if (!(_externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost)))
            {
                Assert.Ignore("Docker not available. Skipping AuthService DB integration tests.");
                return;
            }

            if (_externalConnStr == null)
            {
                try
                {
                    _mysql = new Testcontainers.MySql.MySqlBuilder()
                        .WithImage("mysql:8.0")
                        .WithDatabase("skilllink_test_auth")
                        .WithUsername("testuser")
                        .WithPassword("testpass")
                        .Build();

                    await _mysql.StartAsync();
                    _ownsContainer = true;
                }
                catch (Exception ex)
                {
                    Assert.Ignore($"Docker not available or failed to start MySQL container. Skipping DB tests. Details: {ex.Message}");
                    return;
                }
            }

            var connStr = _externalConnStr ?? _mysql.GetConnectionString();

            // Create Users table with full set of fields used by AuthService
            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"
                    CREATE TABLE IF NOT EXISTS Users (
                      UserId INT AUTO_INCREMENT PRIMARY KEY,
                      FullName VARCHAR(255) NOT NULL,
                      Email VARCHAR(255) NOT NULL UNIQUE,
                      PasswordHash VARCHAR(255) NOT NULL,
                      Role VARCHAR(50) NOT NULL DEFAULT 'Learner',
                      CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                      Bio TEXT NULL,
                      Location VARCHAR(255) NULL,
                      ProfilePicture VARCHAR(512) NULL,
                      ReadyToTeach TINYINT(1) NOT NULL DEFAULT 0,
                      IsActive TINYINT(1) NOT NULL DEFAULT 1,
                      EmailVerified TINYINT(1) NOT NULL DEFAULT 0,
                      EmailVerificationToken VARCHAR(255) NULL,
                      EmailVerificationExpires DATETIME NULL
                    );
                ";
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // In-memory config (JWT + API + SMTP)
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connStr },
                    { "Jwt:Key", "THIS_IS_A_LONG_TEST_KEY_FOR_HMAC_256__CHANGE_ME" },
                    { "Jwt:Issuer", "SkillLink.Tests" },
                    { "Jwt:Audience", "SkillLink.Tests" },
                    { "Jwt:ExpireMinutes", "30" },
                    { "Api:BaseUrl", "http://localhost:5159" },
                    // Dummy SMTP - will fail in send, but Register swallows exceptions
                    { "Smtp:Host", "localhost" },
                    { "Smtp:Port", "25" },
                    { "Smtp:User", "" },
                    { "Smtp:Pass", "" },
                    { "Smtp:From", "noreply@skilllink.local" },
                    { "Smtp:UseSSL", "false" }
                })
                .Build();

            _db = new DbHelper(_config);
            _email = new EmailService(_config);
            _sut = new AuthService(_db, _config, _email);
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
            var sql = @"DELETE FROM Users; ALTER TABLE Users AUTO_INCREMENT = 1;";
            await using var cmd = new MySqlCommand(sql, conn);
            await cmd.ExecuteNonQueryAsync();
        }

        // helper: same hashing as service (SHA256 -> Base64)
        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, string fullName, string email, string password, string role = "Learner", bool verified = true, bool isActive = true, bool readyToTeach = false)
        {
            var cmd = new MySqlCommand(@"
                INSERT INTO Users (FullName, Email, PasswordHash, Role, CreatedAt, Bio, Location, ProfilePicture,
                                   ReadyToTeach, IsActive, EmailVerified)
                VALUES (@n, @e, @p, @r, NOW(), NULL, NULL, NULL, @t, @a, @v);
                SELECT LAST_INSERT_ID();", conn);
            cmd.Parameters.AddWithValue("@n", fullName);
            cmd.Parameters.AddWithValue("@e", email);
            cmd.Parameters.AddWithValue("@p", HashPassword(password));
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@t", readyToTeach ? 1 : 0);
            cmd.Parameters.AddWithValue("@a", isActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@v", verified ? 1 : 0);
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

            // Validate from DB
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var get = new MySqlCommand("SELECT EmailVerified, EmailVerificationToken FROM Users WHERE Email=@e", conn);
            get.Parameters.AddWithValue("@e", "alice@example.com");
            using var r = get.ExecuteReader();
            r.Read().Should().BeTrue();
            var verified = r.GetInt32("EmailVerified");
            var token = r.IsDBNull(r.GetOrdinal("EmailVerificationToken")) ? null : r.GetString("EmailVerificationToken");

            verified.Should().Be(0);
            token.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void VerifyEmailByToken_ShouldSetVerified_And_ClearToken()
        {
            // First Register to create token
            _sut.Register(new RegisterRequest
            {
                FullName = "Bob",
                Email = "bob@example.com",
                Password = "P@ssw0rd"
            });

            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var getToken = new MySqlCommand("SELECT EmailVerificationToken FROM Users WHERE Email=@e", conn);
            getToken.Parameters.AddWithValue("@e", "bob@example.com");
            var token = (string)getToken.ExecuteScalar()!;
            token.Should().NotBeNullOrEmpty();

            var ok = _sut.VerifyEmailByToken(token);
            ok.Should().BeTrue();

            var check = new MySqlCommand("SELECT EmailVerified, EmailVerificationToken FROM Users WHERE Email=@e", conn);
            check.Parameters.AddWithValue("@e", "bob@example.com");
            using var rr = check.ExecuteReader();
            rr.Read().Should().BeTrue();
            rr.GetInt32("EmailVerified").Should().Be(1);
            rr.IsDBNull(rr.GetOrdinal("EmailVerificationToken")).Should().BeTrue();
        }

        [Test]
        public void Login_ShouldReturnNull_WhenNotVerified()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            InsertUserAsync(conn, "Cara", "cara@example.com", "secret", verified:false, isActive:true).GetAwaiter().GetResult();

            var token = _sut.Login(new LoginRequest { Email = "cara@example.com", Password = "secret" });
            token.Should().BeNull();
        }

        [Test]
        public void Login_ShouldReturnToken_WhenVerified_AndPasswordCorrect()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            InsertUserAsync(conn, "Drew", "drew@example.com", "secret", verified:true, isActive:true).GetAwaiter().GetResult();

            var token = _sut.Login(new LoginRequest { Email = "drew@example.com", Password = "secret" });
            token.Should().NotBeNullOrEmpty();
        }

        [Test]
        public void SetActive_ShouldUpdate()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var id = InsertUserAsync(conn, "Elle", "elle@example.com", "pw", verified:true, isActive:true).GetAwaiter().GetResult();

            var ok = _sut.SetActive(id, false);
            ok.Should().BeTrue();

            var check = new MySqlCommand("SELECT IsActive FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            var active = Convert.ToInt32(check.ExecuteScalar());
            active.Should().Be(0);
        }

        [Test]
        public void UpdateTeachMode_ShouldToggleRole_AndFlag()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var id = InsertUserAsync(conn, "Finn", "finn@example.com", "pw", verified:true, isActive:true, readyToTeach:false).GetAwaiter().GetResult();

            var ok = _sut.UpdateTeachMode(id, true);
            ok.Should().BeTrue();

            var check = new MySqlCommand("SELECT ReadyToTeach, Role FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            using var r = check.ExecuteReader();
            r.Read().Should().BeTrue();
            r.GetInt32("ReadyToTeach").Should().Be(1);
            r.GetString("Role").Should().Be("Tutor");
        }

        [Test]
        public void UpdateUserProfile_ShouldPersist()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var id = InsertUserAsync(conn, "Gail", "gail@example.com", "pw", verified:true, isActive:true).GetAwaiter().GetResult();

            var ok = _sut.UpdateUserProfile(id, new UpdateProfileRequest
            {
                FullName = "Gail Updated",
                Bio = "Hello",
                Location = "Colombo"
            });
            ok.Should().BeTrue();

            var check = new MySqlCommand("SELECT FullName, Bio, Location FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            using var r = check.ExecuteReader();
            r.Read().Should().BeTrue();
            r.GetString("FullName").Should().Be("Gail Updated");
            r.GetString("Bio").Should().Be("Hello");
            r.GetString("Location").Should().Be("Colombo");
        }

        [Test]
        public void UpdateProfilePicture_ShouldPersist()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var id = InsertUserAsync(conn, "Hank", "hank@example.com", "pw").GetAwaiter().GetResult();

            var ok = _sut.UpdateProfilePicture(id, "/uploads/profiles/img.png");
            ok.Should().BeTrue();

            var check = new MySqlCommand("SELECT ProfilePicture FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            (check.ExecuteScalar() as string).Should().Be("/uploads/profiles/img.png");
        }

        [Test]
        public void DeleteUserFromDB_ShouldDeleteNonAdmin()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var id = InsertUserAsync(conn, "Ivy", "ivy@example.com", "pw", role:"Learner").GetAwaiter().GetResult();

            _sut.DeleteUserFromDB(id);

            var check = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE UserId=@id", conn);
            check.Parameters.AddWithValue("@id", id);
            Convert.ToInt32(check.ExecuteScalar()).Should().Be(0);
        }

        [Test]
        public void DeleteUserFromDB_ShouldPreventDeletingLastAdmin()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Create single Admin
            var adminId = InsertUserAsync(conn, "Admin One", "admin1@example.com", "pw", role:"Admin").GetAwaiter().GetResult();

            Action act = () => _sut.DeleteUserFromDB(adminId);
            act.Should().Throw<InvalidOperationException>().WithMessage("*last admin*");
        }

        [Test]
        public void DeleteUserFromDB_ShouldAllow_IfMoreThanOneAdmin()
        {
            using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var admin1 = InsertUserAsync(conn, "Admin One", "admin1@example.com", "pw", role:"Admin").GetAwaiter().GetResult();
            var admin2 = InsertUserAsync(conn, "Admin Two", "admin2@example.com", "pw", role:"Admin").GetAwaiter().GetResult();

            // Deleting one should succeed
            _sut.DeleteUserFromDB(admin1);

            var check = new MySqlCommand("SELECT COUNT(*) FROM Users WHERE Role='Admin'", conn);
            Convert.ToInt32(check.ExecuteScalar()).Should().Be(1);
        }
    }
}
