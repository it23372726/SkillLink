using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using NUnit.Framework;
using Testcontainers.MsSql;

namespace SkillLink.Tests.Db
{
    /// <summary>
    /// Provides a ready-to-use connection string to skilllink_test.
    /// - If SKILLLINK_TEST_MSSQL is set, uses that DB.
    /// - Else, spins a Testcontainers MSSQL, creates DB and applies schema.
    /// Also ensures TrustServerCertificate/Encrypt flags for local dev.
    /// </summary>
    public static class TestDbUtil
    {
        private static MsSqlContainer? _container;
        private static bool _ownsContainer;
        private static string? _cachedConnStrToTestDb;

        public static async Task<string> EnsureTestDbAsync()
        {
            if (!string.IsNullOrWhiteSpace(_cachedConnStrToTestDb))
                return _cachedConnStrToTestDb!;

            // Prefer external DB for speed
            var external = Environment.GetEnvironmentVariable("SKILLLINK_TEST_MSSQL");
            if (!string.IsNullOrWhiteSpace(external))
            {
                var ext = EnsureTrustFlags(external);
                var csb = new SqlConnectionStringBuilder(ext);
                if (string.IsNullOrWhiteSpace(csb.InitialCatalog))
                    csb.InitialCatalog = "skilllink_test";

                await EnsureDbExistsAndSchemaAsync(csb.ConnectionString);
                _cachedConnStrToTestDb = csb.ConnectionString;
                return _cachedConnStrToTestDb!;
            }

            // Fall back to Testcontainers
            try
            {
                _container = new MsSqlBuilder()
                    .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                    .WithPassword("Your_password123")
                    .Build();

                await _container.StartAsync();
                _ownsContainer = true;
            }
            catch (Exception ex)
            {
                Assert.Ignore($"Docker MSSQL container could not start. Skipping DB tests. Details: {ex.Message}");
                throw;
            }

            var baseConn = EnsureTrustFlags(_container!.GetConnectionString());
            var csb2 = new SqlConnectionStringBuilder(baseConn)
            {
                InitialCatalog = "skilllink_test",
            };

            await EnsureDbExistsAndSchemaAsync(csb2.ConnectionString);

            _cachedConnStrToTestDb = csb2.ConnectionString;
            return _cachedConnStrToTestDb!;
        }

        public static async Task DisposeAsync()
        {
            if (_ownsContainer && _container is not null)
            {
                await _container.DisposeAsync();
                _container = null;
                _ownsContainer = false;
            }
            _cachedConnStrToTestDb = null;
        }

        private static string EnsureTrustFlags(string connStr)
        {
            var csb = new SqlConnectionStringBuilder(connStr);
            if (!csb.ContainsKey("TrustServerCertificate"))
                csb.TrustServerCertificate = true;
            if (!csb.ContainsKey("Encrypt"))
                csb.Encrypt = false;
            return csb.ConnectionString;
        }

        private static async Task EnsureDbExistsAndSchemaAsync(string connStrToTestDb)
        {
            // 1) Ensure DB exists
            var masterCsb = new SqlConnectionStringBuilder(connStrToTestDb) { InitialCatalog = "master" };
            await using (var conn = new SqlConnection(masterCsb.ConnectionString))
            {
                await conn.OpenAsync();
                var ensureDb = "IF DB_ID('skilllink_test') IS NULL CREATE DATABASE skilllink_test;";
                await using var cmd = new SqlCommand(ensureDb, conn);
                await cmd.ExecuteNonQueryAsync();
            }

            // 2) Apply schema
            var csb = new SqlConnectionStringBuilder(connStrToTestDb) { InitialCatalog = "skilllink_test" };
            var scriptPath = FindSchemaFile();
            if (scriptPath == null)
                Assert.Fail("Cannot find 'db_test/init/1_schema.sql'. Make sure it exists in test project.");

            var batches = SplitSqlBatches(File.ReadAllText(scriptPath!));

            await using (var conn = new SqlConnection(csb.ConnectionString))
            {
                await conn.OpenAsync();
                foreach (var batch in batches)
                {
                    if (string.IsNullOrWhiteSpace(batch)) continue;
                    await using var cmd = new SqlCommand(batch, conn) { CommandTimeout = 120 };
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        private static string? FindSchemaFile()
        {
            var baseDir = TestContext.CurrentContext.TestDirectory;

            string[] candidates =
            {
                Path.Combine(baseDir, "db_test", "init", "1_schema.sql"),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "db_test", "init", "1_schema.sql")),
                Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "db_test", "init", "1_schema.sql")),
            };

            foreach (var p in candidates)
                if (File.Exists(p)) return p;

            return null;
        }

        private static IEnumerable<string> SplitSqlBatches(string script)
        {
            var regex = new Regex(@"^\s*GO\s*;$|^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
            return regex.Split(script);
        }
    }
}
