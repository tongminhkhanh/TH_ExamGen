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
        private string? _connectionString;
        private bool _isPostgres;
        private bool _isConnected;
        private string? _lastError;

        public bool IsPostgres => _isPostgres;
        public bool IsConnected => _isConnected;
        public string? LastError => _lastError;

        public DatabaseService()
        {
            // 1. Kiểm tra Connection String từ biến môi trường (Render / Supabase)
            var pgConn = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING")
                         ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                         ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

            if (!string.IsNullOrWhiteSpace(pgConn))
            {
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
            if (_isPostgres && !string.IsNullOrWhiteSpace(_connectionString))
            {
                return new NpgsqlConnection(_connectionString);
            }
            string dbPath = Path.Combine(AppContext.BaseDirectory, "thexam_local.db");
            return new SqliteConnection(_connectionString ?? $"Data Source={dbPath}");
        }

        public async Task InitializeAsync()
        {
            const string pgSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id VARCHAR(50) PRIMARY KEY,
                    phone VARCHAR(20) UNIQUE NOT NULL,
                    password_hash VARCHAR(255) NOT NULL,
                    full_name VARCHAR(100),
                    school VARCHAR(150),
                    package_type VARCHAR(50) DEFAULT 'Trial',
                    expire_at TIMESTAMPTZ,
                    total_quota_remaining INT DEFAULT 1,
                    daily_quota_limit INT DEFAULT 1,
                    daily_generated_count INT DEFAULT 0,
                    last_generated_date VARCHAR(20),
                    last_generated_at TIMESTAMPTZ,
                    current_session_token VARCHAR(255),
                    device_id VARCHAR(100),
                    is_locked INT DEFAULT 0,
                    created_at TIMESTAMPTZ DEFAULT NOW()
                );

                CREATE TABLE IF NOT EXISTS license_keys (
                    id VARCHAR(50) PRIMARY KEY,
                    key_code VARCHAR(50) UNIQUE NOT NULL,
                    package_type VARCHAR(50) NOT NULL,
                    days_duration INT NOT NULL,
                    quota_amount INT NOT NULL,
                    daily_limit INT NOT NULL,
                    is_used INT DEFAULT 0,
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

            const string sqliteSql = @"
                CREATE TABLE IF NOT EXISTS users (
                    id TEXT PRIMARY KEY,
                    phone TEXT UNIQUE NOT NULL,
                    password_hash TEXT NOT NULL,
                    full_name TEXT,
                    school TEXT,
                    package_type TEXT DEFAULT 'Trial',
                    expire_at TEXT,
                    total_quota_remaining INTEGER DEFAULT 1,
                    daily_quota_limit INTEGER DEFAULT 1,
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

            if (_isPostgres && !string.IsNullOrWhiteSpace(_connectionString))
            {
                try
                {
                    Console.WriteLine(">>> [DATABASE] Đang thử kết nối PostgreSQL / Supabase...");
                    using var conn = new NpgsqlConnection(_connectionString);
                    await conn.OpenAsync();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = pgSql;
                    await cmd.ExecuteNonQueryAsync();
                    Console.WriteLine(">>> [DATABASE SUCCESS] Kết nối thành công và đã đồng bộ bảng trên Supabase!");
                    _isConnected = true;
                    return;
                }
                catch (Exception ex)
                {
                    _lastError = ex.Message;
                    Console.WriteLine($">>> [DATABASE ERROR] Không thể khởi tạo Supabase: {ex.Message}");
                    Console.WriteLine(">>> [DATABASE FALLBACK] Tự động chuyển về SQLite cục bộ để web luôn hoạt động an toàn!");
                    _isPostgres = false;
                    _isConnected = false;
                    string dbPath = Path.Combine(AppContext.BaseDirectory, "thexam_local.db");
                    _connectionString = $"Data Source={dbPath}";
                }
            }

            // Fallback SQLite cục bộ
            try
            {
                string dbPath = Path.Combine(AppContext.BaseDirectory, "thexam_local.db");
                _connectionString = $"Data Source={dbPath}";
                using var sqliteConn = new SqliteConnection(_connectionString);
                await sqliteConn.OpenAsync();
                using var sqliteCmd = sqliteConn.CreateCommand();
                sqliteCmd.CommandText = sqliteSql;
                await sqliteCmd.ExecuteNonQueryAsync();
                _isConnected = true;
                Console.WriteLine(">>> [DATABASE] Đã sẵn sàng hoạt động với SQLite cục bộ.");
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                Console.WriteLine($">>> [DATABASE FATAL] Lỗi khởi tạo SQLite: {ex.Message}");
            }
        }

        public object GetStatus()
        {
            var envVal = Environment.GetEnvironmentVariable("SUPABASE_CONNECTION_STRING")
                         ?? Environment.GetEnvironmentVariable("DATABASE_URL")
                         ?? Environment.GetEnvironmentVariable("POSTGRES_CONNECTION_STRING");

            return new
            {
                isPostgres = _isPostgres,
                isConnected = _isConnected,
                provider = _isPostgres ? "Supabase (PostgreSQL)" : "SQLite (Cục bộ)",
                lastError = _lastError,
                hasEnv = !string.IsNullOrWhiteSpace(envVal)
            };
        }

        private static string ConvertPostgresUrlToConnectionString(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return raw;

            string s = raw.Trim();

            // 1. Loại bỏ các ký tự bao bọc nếu có (dấu ngoặc kép, nháy đơn)
            if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
            {
                s = s.Substring(1, s.Length - 2).Trim();
            }

            // 2. Loại bỏ tiền tố psql nếu người dùng copy cả lệnh psql
            if (s.StartsWith("psql ", StringComparison.OrdinalIgnoreCase))
            {
                s = s.Substring(5).Trim();
                if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
                {
                    s = s.Substring(1, s.Length - 2).Trim();
                }
            }

            // 3. Nếu là định dạng key=value chuẩn ADO.NET (như Host=...;Username=...)
            if (s.Contains("Host=", StringComparison.OrdinalIgnoreCase) ||
                s.Contains("Server=", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var testBuilder = new NpgsqlConnectionStringBuilder(s);

                    if (!s.Contains("SSL Mode", StringComparison.OrdinalIgnoreCase))
                    {
                        testBuilder.SslMode = SslMode.Require;
                    }
                    testBuilder["No Reset On Close"] = true;
                    return testBuilder.ConnectionString;
                }
                catch
                {
                    // Tiếp tục phân tích ở dưới nếu parser chuỗi bị lỗi
                }
            }

            // 4. Phân tích định dạng URI (postgresql:// hoặc postgres:// hoặc user:pass@host...)
            try
            {
                string work = s;
                if (work.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
                    work = work.Substring(13);
                else if (work.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
                    work = work.Substring(11);

                // Tách query string nếu có (?sslmode=...)
                int qIdx = work.IndexOf('?');
                if (qIdx >= 0)
                {
                    work = work.Substring(0, qIdx);
                }

                // Tách database (sau dấu gạch chéo đầu tiên)
                string db = "postgres";
                int slashIdx = work.IndexOf('/');
                if (slashIdx >= 0)
                {
                    db = work.Substring(slashIdx + 1);
                    work = work.Substring(0, slashIdx);
                }

                // Tách user:pass và host:port (dùng LastIndexOf('@') phòng trường hợp mật khẩu có chứa ký tự @)
                string userInfo = "";
                string hostPort = work;
                int atIdx = work.LastIndexOf('@');
                if (atIdx >= 0)
                {
                    userInfo = work.Substring(0, atIdx);
                    hostPort = work.Substring(atIdx + 1);
                }

                string username = "postgres";
                string password = "";
                if (!string.IsNullOrEmpty(userInfo))
                {
                    int colonIdx = userInfo.IndexOf(':');
                    if (colonIdx >= 0)
                    {
                        username = Uri.UnescapeDataString(userInfo.Substring(0, colonIdx));
                        password = Uri.UnescapeDataString(userInfo.Substring(colonIdx + 1));
                    }
                    else
                    {
                        username = Uri.UnescapeDataString(userInfo);
                    }
                }

                // Xử lý trường hợp người dùng để nguyên ngoặc vuông trong mật khẩu [mypassword]
                if (password.StartsWith("[") && password.EndsWith("]") && password.Length > 2)
                {
                    password = password.Substring(1, password.Length - 2);
                }

                string host = "localhost";
                int port = 5432;
                if (!string.IsNullOrEmpty(hostPort))
                {
                    int colonIdx = hostPort.LastIndexOf(':');
                    if (colonIdx >= 0)
                    {
                        host = hostPort.Substring(0, colonIdx);
                        if (int.TryParse(hostPort.Substring(colonIdx + 1), out int p)) port = p;
                    }
                    else
                    {
                        host = hostPort;
                    }
                }

                var builder = new NpgsqlConnectionStringBuilder
                {
                    Host = host,
                    Port = port,
                    Database = string.IsNullOrWhiteSpace(db) ? "postgres" : db,
                    Username = username,
                    Password = password,
                    SslMode = SslMode.Require,
                    Timeout = 15,
                    CommandTimeout = 30
                };
                builder["No Reset On Close"] = true;

                return builder.ConnectionString;
            }
            catch (Exception ex)
            {
                Console.WriteLine($">>> [ConvertPostgresUrl Error] {ex.Message}");
                return s;
            }
        }
    }
}
