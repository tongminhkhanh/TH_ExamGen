using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace TH_ExamGen.WebAPI.Database
{
    public class DatabaseService
    {
        private readonly string? _connectionString;
        private readonly bool _isPostgres;

        public bool IsPostgres => _isPostgres;

        public DatabaseService()
        {
            // 1. Kiểm tra Connection String từ biến môi trường (Render / Supabase)
            var pgConn = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING")
                         ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                         ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

            if (!string.IsNullOrWhiteSpace(pgConn))
            {
                // Hỗ trợ cả định dạng postgres://... URL lẫn Host=... connection string
                _connectionString = ConvertPostgresUrlToConnectionString(pgConn);
                _isPostgres = true;
            }
            else
            {
                // Fallback SQLite cục bộ nếu chưa gắn Supabase
                string dbPath = Path.Combine(AppContext.BaseDirectory, "thexam_local.db");
                _connectionString = $"Data Source={dbPath}";
                _isPostgres = false;
            }
        }

        public DbConnection CreateConnection()
        {
            if (_isPostgres)
            {
                return new NpgsqlConnection(_connectionString);
            }
            return new SqliteConnection(_connectionString);
        }

        public async Task InitializeAsync()
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            string sql;
            if (_isPostgres)
            {
                sql = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id VARCHAR(50) PRIMARY KEY,
                        phone VARCHAR(20) UNIQUE NOT NULL,
                        password_hash VARCHAR(255) NOT NULL,
                        full_name VARCHAR(100),
                        school VARCHAR(150),
                        package_type VARCHAR(50) DEFAULT 'Trial',
                        expire_at TIMESTAMPTZ,
                        total_quota_remaining INT DEFAULT 3,
                        daily_quota_limit INT DEFAULT 5,
                        daily_generated_count INT DEFAULT 0,
                        last_generated_date VARCHAR(20),
                        last_generated_at TIMESTAMPTZ,
                        current_session_token VARCHAR(255),
                        device_id VARCHAR(100),
                        is_locked BOOLEAN DEFAULT FALSE,
                        created_at TIMESTAMPTZ DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS license_keys (
                        id VARCHAR(50) PRIMARY KEY,
                        key_code VARCHAR(50) UNIQUE NOT NULL,
                        package_type VARCHAR(50) NOT NULL,
                        days_duration INT NOT NULL,
                        quota_amount INT NOT NULL,
                        daily_limit INT NOT NULL,
                        is_used BOOLEAN DEFAULT FALSE,
                        used_by_phone VARCHAR(20),
                        used_at TIMESTAMPTZ,
                        created_at TIMESTAMPTZ DEFAULT NOW()
                    );

                    CREATE TABLE IF NOT EXISTS exam_logs (
                        id VARCHAR(50) PRIMARY KEY,
                        user_id VARCHAR(50) NOT NULL,
                        subject VARCHAR(20) NOT NULL,
                        grade INT NOT NULL,
                        period VARCHAR(50) NOT NULL,
                        title VARCHAR(255),
                        exam_data_json TEXT NOT NULL,
                        created_at TIMESTAMPTZ DEFAULT NOW()
                    );
                ";
            }
            else
            {
                sql = @"
                    CREATE TABLE IF NOT EXISTS users (
                        id TEXT PRIMARY KEY,
                        phone TEXT UNIQUE NOT NULL,
                        password_hash TEXT NOT NULL,
                        full_name TEXT,
                        school TEXT,
                        package_type TEXT DEFAULT 'Trial',
                        expire_at TEXT,
                        total_quota_remaining INTEGER DEFAULT 3,
                        daily_quota_limit INTEGER DEFAULT 5,
                        daily_generated_count INTEGER DEFAULT 0,
                        last_generated_date TEXT,
                        last_generated_at TEXT,
                        current_session_token TEXT,
                        device_id TEXT,
                        is_locked INTEGER DEFAULT 0,
                        created_at TEXT
                    );

                    CREATE TABLE IF NOT EXISTS license_keys (
                        id TEXT PRIMARY KEY,
                        key_code TEXT UNIQUE NOT NULL,
                        package_type TEXT NOT NULL,
                        days_duration INTEGER NOT NULL,
                        quota_amount INTEGER NOT NULL,
                        daily_limit INTEGER NOT NULL,
                        is_used INTEGER DEFAULT 0,
                        used_by_phone TEXT,
                        used_at TEXT,
                        created_at TEXT
                    );

                    CREATE TABLE IF NOT EXISTS exam_logs (
                        id TEXT PRIMARY KEY,
                        user_id TEXT NOT NULL,
                        subject TEXT NOT NULL,
                        grade INTEGER NOT NULL,
                        period TEXT NOT NULL,
                        title TEXT,
                        exam_data_json TEXT NOT NULL,
                        created_at TEXT
                    );
                ";
            }

            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            await cmd.ExecuteNonQueryAsync();
        }

        private static string ConvertPostgresUrlToConnectionString(string url)
        {
            if (!url.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
                !url.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            try
            {
                var uri = new Uri(url);
                var userInfo = uri.UserInfo.Split(':');
                var user = userInfo[0];
                var pass = userInfo.Length > 1 ? userInfo[1] : "";
                var host = uri.Host;
                var port = uri.Port > 0 ? uri.Port : 5432;
                var db = uri.AbsolutePath.TrimStart('/');

                return $"Host={host};Port={port};Database={db};Username={user};Password={pass};SSL Mode=Require;Trust Server Certificate=true;";
            }
            catch
            {
                return url;
            }
        }
    }
}
