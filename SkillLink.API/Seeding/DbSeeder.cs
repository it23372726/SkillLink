// File: SkillLink.API/Seeding/DbSeeder.cs
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SkillLink.API.Data; // DbHelper

namespace SkillLink.API.Seeding
{
    public static class DbSeeder
    {
        // MSSQL-safe, schema-qualified, bracketed identifiers
        private const string USERS_TABLE   = "[dbo].[Users]";
        private const string COL_EMAIL     = "[Email]";
        private const string COL_PASS      = "[PasswordHash]";
        private const string COL_ROLE      = "[Role]";
        private const string COL_ACTIVE    = "[IsActive]";
        private const string COL_NAME      = "[FullName]";
        private const string COL_CREATED   = "[CreatedAt]";
        private const string COL_VERIFIED  = "[EmailVerified]";

        public static void Seed(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbHelper>();

            using var conn = db.GetConnection();
            conn.Open();

            EnsureUser(conn, "admin@skilllink.local",   "Admin@123",   "Admin",   "SkillLink Admin");
            EnsureUser(conn, "learner@skilllink.local", "Learner@123", "Learner", "Learner One");
            EnsureUser(conn, "tutor@skilllink.local",   "Tutor@123",   "Tutor",   "Tutor One");
        }

        /// <summary>
        /// Helper to add a new parameter to a command (never reuse parameter instances).
        /// </summary>
        private static void AddParam(IDbCommand cmd, string name, object value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        /// <summary>
        /// SHA256 -> Base64 to match AuthService.HashPassword (if same algorithm).
        /// </summary>
        private static string Sha256Base64(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Upsert-like: if email exists → update hash/role/active/name/verified,
        /// else insert a new user. MSSQL version (uses COUNT(*) for existence).
        /// </summary>
        private static void EnsureUser(IDbConnection conn, string email, string plainPassword, string role, string fullName)
        {
            var desiredHash = Sha256Base64(plainPassword);

            // 1) Check existence safely (new command & new parameters)
            using (var exists = conn.CreateCommand())
            {
                exists.CommandText = $@"SELECT COUNT(1) FROM {USERS_TABLE} WHERE {COL_EMAIL} = @email;";
                AddParam(exists, "@email", email);

                var cntObj = exists.ExecuteScalar();
                var count = Convert.ToInt32(cntObj);

                if (count > 0)
                {
                    // 2) Update metadata + hash (new command & new parameters)
                    using var upd = conn.CreateCommand();
                    upd.CommandText = $@"
UPDATE {USERS_TABLE}
SET {COL_PASS}     = @hash,
    {COL_ROLE}     = @role,
    {COL_ACTIVE}   = @active,
    {COL_NAME}     = @name,
    {COL_VERIFIED} = @verified
WHERE {COL_EMAIL}  = @email;";

                    AddParam(upd, "@hash",     desiredHash);
                    AddParam(upd, "@role",     role);
                    AddParam(upd, "@active",   true);            // BIT
                    AddParam(upd, "@name",     fullName);
                    AddParam(upd, "@verified", true);            // BIT
                    AddParam(upd, "@email",    email);

                    upd.ExecuteNonQuery();
                    return;
                }
            }

            // 3) Insert new (new command & new parameters)
            using (var ins = conn.CreateCommand())
            {
                ins.CommandText = $@"
INSERT INTO {USERS_TABLE}
    ({COL_EMAIL}, {COL_PASS}, {COL_ROLE}, {COL_ACTIVE}, {COL_NAME}, {COL_CREATED}, {COL_VERIFIED})
VALUES
    (@email, @hash, @role, @active, @name, @createdAt, @verified);";

                AddParam(ins, "@email",     email);
                AddParam(ins, "@hash",      desiredHash);
                AddParam(ins, "@role",      role);
                AddParam(ins, "@active",    true);               // BIT
                AddParam(ins, "@name",      fullName);
                AddParam(ins, "@createdAt", DateTime.UtcNow);
                AddParam(ins, "@verified",  true);               // BIT

                ins.ExecuteNonQuery();
            }
        }
    }
}
