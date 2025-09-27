// File: SkillLink.API/Seeding/DbSeeder.cs
using System;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SkillLink.API.Services; // DbHelper

namespace SkillLink.API.Seeding
{
    public static class DbSeeder
    {
        // Adjust if your schema uses different names/casing
        private const string USERS_TABLE = "users";
        private const string COL_EMAIL   = "Email";
        private const string COL_PASS    = "PasswordHash";
        private const string COL_ROLE    = "Role";
        private const string COL_ACTIVE  = "IsActive";
        private const string COL_NAME    = "FullName";
        private const string COL_CREATED = "CreatedAt";
        private const string COL_VERIFIED = "EmailVerified";    // <-- new

        public static void Seed(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<DbHelper>();

            using var conn = db.GetConnection();   // DbHelper must expose GetConnection()
            conn.Open();

            EnsureUser(conn, "admin@skilllink.local",   "Admin@123",   "Admin",   "SkillLink Admin");
            EnsureUser(conn, "learner@skilllink.local", "Learner@123", "Learner", "Learner One");
            EnsureUser(conn, "tutor@skilllink.local",   "Tutor@123",   "Tutor",   "Tutor One");
        }

        /// <summary>
        /// SHA256 -> Base64 to match AuthService.HashPassword.
        /// </summary>
        private static string Sha256Base64(string password)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Insert user if missing; if exists but hash differs, update to the expected SHA256 Base64 hash.
        /// Also refresh role/name/active/emailVerified to keep data consistent.
        /// </summary>
        private static void EnsureUser(IDbConnection conn, string email, string plainPassword, string role, string fullName)
        {
            var desiredHash = Sha256Base64(plainPassword);

            // 1) Read existing
            using var getCmd = conn.CreateCommand();
            getCmd.CommandText = $@"SELECT {COL_PASS} FROM {USERS_TABLE} WHERE {COL_EMAIL} = @email LIMIT 1;";
            var pEmail = getCmd.CreateParameter();
            pEmail.ParameterName = "@email";
            pEmail.Value = email;
            getCmd.Parameters.Add(pEmail);

            var existingHashObj = getCmd.ExecuteScalar();
            var existingHash = existingHashObj as string;

            if (existingHash != null)
            {
                // 2) Update if hash differs or to force metadata consistency (role/active/name/verified)
                using var upd = conn.CreateCommand();
                upd.CommandText = $@"
UPDATE {USERS_TABLE}
SET {COL_PASS} = @hash,
    {COL_ROLE} = @role,
    {COL_ACTIVE} = @active,
    {COL_NAME} = @name,
    {COL_VERIFIED} = @verified
WHERE {COL_EMAIL} = @email;";

                var pHash = upd.CreateParameter(); pHash.ParameterName = "@hash"; pHash.Value = desiredHash;
                var pRole = upd.CreateParameter(); pRole.ParameterName = "@role"; pRole.Value = role;
                var pAct  = upd.CreateParameter(); pAct.ParameterName  = "@active"; pAct.Value = true;
                var pName = upd.CreateParameter(); pName.ParameterName = "@name"; pName.Value = fullName;
                var pVer  = upd.CreateParameter(); pVer.ParameterName  = "@verified"; pVer.Value = true;

                upd.Parameters.Add(pEmail);
                upd.Parameters.Add(pHash);
                upd.Parameters.Add(pRole);
                upd.Parameters.Add(pAct);
                upd.Parameters.Add(pName);
                upd.Parameters.Add(pVer);

                // If the hash already matches, this still ensures EmailVerified=1 & metadata sync
                upd.ExecuteNonQuery();
                return;
            }

            // 3) Insert
            using var ins = conn.CreateCommand();
            ins.CommandText = $@"
INSERT INTO {USERS_TABLE}
    ({COL_EMAIL}, {COL_PASS}, {COL_ROLE}, {COL_ACTIVE}, {COL_NAME}, {COL_CREATED}, {COL_VERIFIED})
VALUES
    (@email, @hash, @role, @active, @name, @createdAt, @verified);";

            var ipHash = ins.CreateParameter(); ipHash.ParameterName = "@hash"; ipHash.Value = desiredHash;
            var ipRole = ins.CreateParameter(); ipRole.ParameterName = "@role"; ipRole.Value = role;
            var ipAct  = ins.CreateParameter(); ipAct.ParameterName  = "@active"; ipAct.Value = true;
            var ipName = ins.CreateParameter(); ipName.ParameterName = "@name"; ipName.Value = fullName;
            var ipAt   = ins.CreateParameter(); ipAt.ParameterName   = "@createdAt"; ipAt.Value = DateTime.UtcNow;
            var ipVer  = ins.CreateParameter(); ipVer.ParameterName  = "@verified"; ipVer.Value = true;

            ins.Parameters.Add(pEmail);
            ins.Parameters.Add(ipHash);
            ins.Parameters.Add(ipRole);
            ins.Parameters.Add(ipAct);
            ins.Parameters.Add(ipName);
            ins.Parameters.Add(ipAt);
            ins.Parameters.Add(ipVer);

            ins.ExecuteNonQuery();
        }
    }
}
