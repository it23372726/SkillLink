using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using FluentAssertions;
using MySql.Data.MySqlClient;
using NUnit.Framework;
using SkillLink.API.Models;
using SkillLink.API.Services;
using Testcontainers.MySql;
using Microsoft.Extensions.Configuration;

namespace SkillLink.Tests.Services
{
    [TestFixture]
    public class AuthServiceDbTests
    {
        private MySqlContainer _mysql = null!;
    private bool _ownsContainer = false;
    private string? _externalConnStr = null;
        private IConfiguration _config = null!;
        private DbHelper _dbHelper = null!;
        private EmailService _emailService = null!;
        private AuthService _sut = null!;

        [OneTimeSetUp]
        public async Task OneTimeSetup()
        {
            // If external connection string provided, use it; else try Docker; else skip
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MYSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                _externalConnStr = external;
            }

            // Auto-skip integration tests if Docker isn't available (and no external DB)
            var dockerHost = Environment.GetEnvironmentVariable("DOCKER_HOST");
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var sock1 = "/var/run/docker.sock";
            var sock2 = Path.Combine(home, ".docker/run/docker.sock");
            var dockerSocketExists = File.Exists(sock1) || File.Exists(sock2);
            var shouldRun = _externalConnStr != null || dockerSocketExists || !string.IsNullOrEmpty(dockerHost);

            if (!shouldRun)
            {
                Assert.Ignore("Docker not available. Skipping AuthService DB integration tests.");
                return;
            }

            if (_externalConnStr == null)
            {
                try
                {
                    _mysql = new MySqlBuilder()
                        .WithImage("mysql:8.0")
                        .WithDatabase("skilllink_test")
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

            // Create schema
            await using (var conn = new MySqlConnection(connStr))
            {
                await conn.OpenAsync();
                var sql = @"CREATE TABLE IF NOT EXISTS Users (
                    UserId INT AUTO_INCREMENT PRIMARY KEY,
                    FullName VARCHAR(255) NOT NULL,
                    Email VARCHAR(255) NOT NULL UNIQUE,
                    PasswordHash VARCHAR(255) NOT NULL,
                    Role VARCHAR(50) NOT NULL,
                    CreatedAt DATETIME NOT NULL,
                    Bio TEXT NULL,
                    Location TEXT NULL,
                    ProfilePicture TEXT NULL,
                    ReadyToTeach TINYINT(1) NOT NULL DEFAULT 0,
                    IsActive TINYINT(1) NOT NULL DEFAULT 1,
                    EmailVerified TINYINT(1) NOT NULL DEFAULT 0,
                    EmailVerificationToken VARCHAR(255) NULL,
                    EmailVerificationExpires DATETIME NULL
                );";
                await using var cmd = new MySqlCommand(sql, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // Build config
            _config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", connStr },
                    { "Jwt:Key", "super_secret_key_123456_super_secret_key" },
                    { "Jwt:Issuer", "SkillLinkAPI" },
                    { "Jwt:Audience", "SkillLinkClient" },
                    { "Jwt:ExpireMinutes", "120" },
                    { "Api:BaseUrl", "http://localhost:5159" },
                    { "Smtp:Host", "127.0.0.1" },
                    { "Smtp:Port", "2525" },
                    { "Smtp:User", "" },
                    { "Smtp:Pass", "" },
                    { "Smtp:From", "noreply@skilllink.test" },
                    { "Smtp:UseSSL", "false" }
                })
                .Build();

            _dbHelper = new DbHelper(_config);
            _emailService = new EmailService(_config);
            _sut = new AuthService(_dbHelper, _config, _emailService);
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
            await using var cmd = new MySqlCommand("DELETE FROM Users; ALTER TABLE Users AUTO_INCREMENT = 1;", conn);
            await cmd.ExecuteNonQueryAsync();
        }

        private static string Sha256Base64(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToBase64String(bytes);
        }

        private async Task<int> InsertUserAsync(MySqlConnection conn, User user)
        {
            var sql = @"INSERT INTO Users
                (FullName, Email, PasswordHash, Role, CreatedAt, Bio, Location, ProfilePicture,
                 ReadyToTeach, IsActive, EmailVerified, EmailVerificationToken, EmailVerificationExpires)
                VALUES (@FullName, @Email, @PasswordHash, @Role, @CreatedAt, @Bio, @Location, @ProfilePicture,
                        @ReadyToTeach, @IsActive, @EmailVerified, @Token, @Expires); SELECT LAST_INSERT_ID();";
            await using var cmd = new MySqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@FullName", user.FullName);
            cmd.Parameters.AddWithValue("@Email", user.Email);
            cmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
            cmd.Parameters.AddWithValue("@Role", user.Role);
            cmd.Parameters.AddWithValue("@CreatedAt", user.CreatedAt);
            cmd.Parameters.AddWithValue("@Bio", (object?)user.Bio ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Location", (object?)user.Location ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfilePicture", (object?)user.ProfilePicture ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ReadyToTeach", user.ReadyToTeach ? 1 : 0);
            cmd.Parameters.AddWithValue("@IsActive", user.IsActive ? 1 : 0);
            cmd.Parameters.AddWithValue("@EmailVerified", user.EmailVerified ? 1 : 0);
            cmd.Parameters.AddWithValue("@Token", (object?)user.EmailVerificationToken ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@Expires", (object?)user.EmailVerificationExpires ?? DBNull.Value);

            var idObj = await cmd.ExecuteScalarAsync();
            return Convert.ToInt32(idObj);
        }

        [Test]
        public async Task GetUserById_ShouldReturnInsertedUser()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var u = new User
            {
                FullName = "Alice",
                Email = "alice@example.com",
                PasswordHash = Sha256Base64("Pass123"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                Bio = "Bio",
                Location = "Earth",
                ProfilePicture = null,
                ReadyToTeach = false,
                IsActive = true,
                EmailVerified = true
            };
            var id = await InsertUserAsync(conn, u);

            var fetched = _sut.GetUserById(id);
            fetched.Should().NotBeNull();
            fetched!.FullName.Should().Be("Alice");
            fetched.Email.Should().Be("alice@example.com");
        }

        [Test]
        public async Task CurrentUser_ShouldReturnLimitedUser_WhenClaimsValid()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var id = await InsertUserAsync(conn, new User
            {
                FullName = "Bob",
                Email = "bob@example.com",
                PasswordHash = Sha256Base64("Pass123"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = true
            });

            var identity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, "Learner"),
                new Claim(JwtRegisteredClaimNames.Email, "bob@example.com")
            }, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            var me = _sut.CurrentUser(principal);
            me.Should().NotBeNull();
            me!.UserId.Should().Be(id);
            me.Email.Should().Be("bob@example.com");
            me.Role.Should().Be("Learner");
            me.PasswordHash.Should().BeNullOrEmpty();
        }

        [Test]
        public async Task Login_ShouldReturnToken_ForVerifiedActive_WithCorrectPassword()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await InsertUserAsync(conn, new User
            {
                FullName = "Carol",
                Email = "carol@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = true
            });

            var token = _sut.Login(new LoginRequest { Email = "carol@example.com", Password = "Secret!1" });
            token.Should().NotBeNullOrEmpty();

            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token!);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == "carol@example.com");
        }

        [Test]
        public async Task Login_ShouldReturnNull_WhenNotVerified()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await InsertUserAsync(conn, new User
            {
                FullName = "Dan",
                Email = "dan@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = false
            });

            var token = _sut.Login(new LoginRequest { Email = "dan@example.com", Password = "Secret!1" });
            token.Should().BeNull();
        }

        [Test]
        public async Task Login_ShouldReturnNull_WhenInactive()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await InsertUserAsync(conn, new User
            {
                FullName = "Eve",
                Email = "eve@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = false,
                EmailVerified = true
            });

            var token = _sut.Login(new LoginRequest { Email = "eve@example.com", Password = "Secret!1" });
            token.Should().BeNull();
        }

        [Test]
        public async Task Login_ShouldReturnNull_OnWrongPassword()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await InsertUserAsync(conn, new User
            {
                FullName = "Finn",
                Email = "finn@example.com",
                PasswordHash = Sha256Base64("Correct123"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = true
            });

            var token = _sut.Login(new LoginRequest { Email = "finn@example.com", Password = "Wrong!" });
            token.Should().BeNull();
        }

        [Test]
        public async Task GetUserProfile_ShouldReturnProfile()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var id = await InsertUserAsync(conn, new User
            {
                FullName = "Gail",
                Email = "gail@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                Bio = "Teacher",
                Location = "USA",
                ProfilePicture = null,
                ReadyToTeach = true,
                IsActive = true,
                EmailVerified = true
            });

            var profile = _sut.GetUserProfile(id);
            profile.Should().NotBeNull();
            profile!.ReadyToTeach.Should().BeTrue();
            profile.Bio.Should().Be("Teacher");
        }

        [Test]
        public async Task UpdateUserProfile_ShouldPersistChanges()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var id = await InsertUserAsync(conn, new User
            {
                FullName = "Hank",
                Email = "hank@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = true
            });

            var ok = _sut.UpdateUserProfile(id, new UpdateProfileRequest
            {
                FullName = "Henry",
                Bio = "Bio here",
                Location = "EU"
            });
            ok.Should().BeTrue();

            var profile = _sut.GetUserProfile(id)!;
            profile.FullName.Should().Be("Henry");
            profile.Bio.Should().Be("Bio here");
            profile.Location.Should().Be("EU");
        }

        [Test]
        public async Task UpdateTeachMode_ShouldToggleRoleAndFlag()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var id = await InsertUserAsync(conn, new User
            {
                FullName = "Ivy",
                Email = "ivy@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                ReadyToTeach = false,
                IsActive = true,
                EmailVerified = true
            });

            var ok1 = _sut.UpdateTeachMode(id, true);
            ok1.Should().BeTrue();
            var p1 = _sut.GetUserProfile(id)!;
            p1.ReadyToTeach.Should().BeTrue();
            p1.Role.Should().Be("Tutor");

            var ok2 = _sut.UpdateTeachMode(id, false);
            ok2.Should().BeTrue();
            var p2 = _sut.GetUserProfile(id)!;
            p2.ReadyToTeach.Should().BeFalse();
            p2.Role.Should().Be("Learner");
        }

        [Test]
        public async Task SetActive_ShouldUpdateStatus()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var id = await InsertUserAsync(conn, new User
            {
                FullName = "Jill",
                Email = "jill@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = true
            });

            var ok = _sut.SetActive(id, false);
            ok.Should().BeTrue();
            var p = _sut.GetUserProfile(id)!;
            p.IsActive.Should().BeFalse();
        }

        [Test]
        public async Task VerifyEmailByToken_ShouldVerify_WhenValid()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var token = "validtoken123";
            var id = await InsertUserAsync(conn, new User
            {
                FullName = "Kim",
                Email = "kim@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = false,
                EmailVerificationToken = token,
                EmailVerificationExpires = DateTime.UtcNow.AddHours(1)
            });

            var ok = _sut.VerifyEmailByToken(token);
            ok.Should().BeTrue();

            var p = _sut.GetUserProfile(id)!;
            p.EmailVerified.Should().BeTrue();
        }

        [Test]
        public async Task VerifyEmailByToken_ShouldReturnFalse_WhenExpired()
        {
            await using var conn = new MySqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            var token = "expiredtoken123";
            await InsertUserAsync(conn, new User
            {
                FullName = "Liam",
                Email = "liam@example.com",
                PasswordHash = Sha256Base64("Secret!1"),
                Role = "Learner",
                CreatedAt = DateTime.UtcNow,
                IsActive = true,
                EmailVerified = false,
                EmailVerificationToken = token,
                EmailVerificationExpires = DateTime.UtcNow.AddHours(-1)
            });

            var ok = _sut.VerifyEmailByToken(token);
            ok.Should().BeFalse();
        }

        [Test]
        public void VerifyEmailByToken_ShouldReturnFalse_WhenNotFound()
        {
            var ok = _sut.VerifyEmailByToken("doesnotexist");
            ok.Should().BeFalse();
        }
    }
}
