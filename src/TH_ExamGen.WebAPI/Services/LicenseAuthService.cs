using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using TH_ExamGen.WebAPI.Database;

namespace TH_ExamGen.WebAPI.Services
{
    public class UserRecord
    {
        public string Id { get; set; } = "";
        public string Phone { get; set; } = "";
        public string PasswordHash { get; set; } = "";
        public string FullName { get; set; } = "";
        public string School { get; set; } = "";
        public string PackageType { get; set; } = "Trial";
        public DateTime? ExpireAt { get; set; }
        public int TotalQuotaRemaining { get; set; } = 1;
        public int DailyQuotaLimit { get; set; } = 1;
        public int DailyGeneratedCount { get; set; } = 0;
        public string LastGeneratedDate { get; set; } = "";
        public DateTime? LastGeneratedAt { get; set; }
        public string CurrentSessionToken { get; set; } = "";
        public string DeviceId { get; set; } = "";
        public bool IsLocked { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LicenseKeyRecord
    {
        public string Id { get; set; } = "";
        public string KeyCode { get; set; } = "";
        public string PackageType { get; set; } = "";
        public int DaysDuration { get; set; }
        public int QuotaAmount { get; set; }
        public int DailyLimit { get; set; }
        public bool IsUsed { get; set; }
        public string? UsedByPhone { get; set; }
        public DateTime? UsedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class ExamLogItem
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Subject { get; set; } = "";
        public int Grade { get; set; }
        public string Period { get; set; } = "";
        public string Title { get; set; } = "";
        public string ExamDataJson { get; set; } = "";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class LicenseAuthService
    {
        private readonly DatabaseService _db;

        public LicenseAuthService(DatabaseService db)
        {
            _db = db;
        }

        private static DateTime GetVietnamNow()
        {
            return DateTime.UtcNow.AddHours(7);
        }

        private static string GetVietnamTodayString()
        {
            return GetVietnamNow().ToString("yyyy-MM-dd");
        }

        public static string HashPassword(string password)
        {
            using var sha = SHA256.Create();
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password + "THEXAM_SALT_2026_@#"));
            return Convert.ToBase64String(bytes);
        }

        public async Task<(bool Success, string Message, string? SessionToken, UserRecord? User)> RegisterAsync(string phone, string password, string fullName, string school, string deviceId)
        {
            phone = phone.Trim().Replace(" ", "");
            if (phone.Length < 9 || phone.Length > 12)
                return (false, "Số điện thoại không hợp lệ.", null, null);
            if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
                return (false, "Mật khẩu phải từ 6 ký tự trở lên.", null, null);

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // 1. Kiểm tra số điện thoại đã tồn tại
            using (var checkCmd = conn.CreateCommand())
            {
                checkCmd.CommandText = "SELECT COUNT(*) FROM users WHERE phone = @phone";
                var p = checkCmd.CreateParameter();
                p.ParameterName = "@phone";
                p.Value = phone;
                checkCmd.Parameters.Add(p);
                long count = Convert.ToInt64(await checkCmd.ExecuteScalarAsync());
                if (count > 0)
                {
                    return (false, "Số điện thoại này đã được đăng ký. Vui lòng bấm Đăng nhập.", null, null);
                }
            }

            // 2. Kiểm tra giới hạn Dùng Thử theo Thiết bị (Device ID - Max 1 tài khoản trial / thiết bị)
            int initialQuota = 1;
            int initialDays = 3;
            if (!string.IsNullOrWhiteSpace(deviceId))
            {
                using var devCmd = conn.CreateCommand();
                devCmd.CommandText = "SELECT COUNT(*) FROM users WHERE device_id = @devId";
                var pDev = devCmd.CreateParameter();
                pDev.ParameterName = "@devId";
                pDev.Value = deviceId;
                devCmd.Parameters.Add(pDev);
                long devCount = Convert.ToInt64(await devCmd.ExecuteScalarAsync());
                if (devCount >= 1)
                {
                    initialQuota = 0; // Đã nhận dùng thử trên máy này
                }
            }

            var userId = Guid.NewGuid().ToString("N");
            var sessionToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            var expireAt = GetVietnamNow().AddDays(initialDays);
            var nowStr = GetVietnamNow().ToString("o");

            using (var insertCmd = conn.CreateCommand())
            {
                insertCmd.CommandText = @"
                    INSERT INTO users (id, phone, password_hash, full_name, school, package_type, expire_at, total_quota_remaining, daily_quota_limit, daily_generated_count, last_generated_date, current_session_token, device_id, is_locked, created_at)
                    VALUES (@id, @phone, @pwd, @name, @school, 'Trial', @expire, @quota, 1, 0, @today, @token, @devId, @isLocked, @created)";

                AddParam(insertCmd, "@id", userId);
                AddParam(insertCmd, "@phone", phone);
                AddParam(insertCmd, "@pwd", HashPassword(password));
                AddParam(insertCmd, "@name", string.IsNullOrWhiteSpace(fullName) ? "Thầy/Cô" : fullName.Trim());
                AddParam(insertCmd, "@school", string.IsNullOrWhiteSpace(school) ? "" : school.Trim());
                AddParam(insertCmd, "@expire", expireAt);
                AddParam(insertCmd, "@quota", initialQuota);
                AddParam(insertCmd, "@today", GetVietnamTodayString());
                AddParam(insertCmd, "@token", sessionToken);
                AddParam(insertCmd, "@devId", deviceId ?? "");
                AddParam(insertCmd, "@isLocked", false);
                AddParam(insertCmd, "@created", DateTime.UtcNow);

                await insertCmd.ExecuteNonQueryAsync();
            }

            var user = await GetUserByPhoneAsync(phone);
            return (true, "Đăng ký tài khoản thành công! Thầy/Cô nhận được 1 đề thi dùng thử miễn phí.", sessionToken, user);
        }

        public async Task<(bool Success, string Message, string? SessionToken, UserRecord? User)> LoginAsync(string phone, string password, string deviceId)
        {
            phone = phone.Trim().Replace(" ", "");
            var user = await GetUserByPhoneAsync(phone);
            if (user == null)
            {
                return (false, "Số điện thoại chưa được đăng ký trên hệ thống.", null, null);
            }

            if (user.IsLocked)
            {
                return (false, "Tài khoản của bạn tạm thời bị khóa. Vui lòng liên hệ Admin để được hỗ trợ.", null, null);
            }

            if (user.PasswordHash != HashPassword(password))
            {
                return (false, "Mật khẩu không chính xác.", null, null);
            }

            // Cập nhật phiên đăng nhập mới (Tầng 4: Đá văng phiên cũ)
            var newSessionToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET current_session_token = @token WHERE id = @id";
            AddParam(cmd, "@token", newSessionToken);
            AddParam(cmd, "@id", user.Id);
            await cmd.ExecuteNonQueryAsync();

            user.CurrentSessionToken = newSessionToken;
            return (true, "Đăng nhập thành công!", newSessionToken, user);
        }

        public async Task<UserRecord?> GetUserByTokenAsync(string sessionToken)
        {
            if (string.IsNullOrWhiteSpace(sessionToken)) return null;

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM users WHERE current_session_token = @token LIMIT 1";
            AddParam(cmd, "@token", sessionToken);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        public async Task<UserRecord?> GetUserByPhoneAsync(string phone)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM users WHERE phone = @phone LIMIT 1";
            AddParam(cmd, "@phone", phone);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return MapUser(reader);
            }
            return null;
        }

        // ================= KIỂM SOÁT HẠN MỨC 5 TẦNG =================

        public (bool Allowed, string Reason) VerifyExamGenerationQuota(UserRecord user, string sessionToken)
        {
            // Tầng 4: Kiểm tra phiên đăng nhập hợp lệ
            if (user.CurrentSessionToken != sessionToken)
            {
                return (false, "Tài khoản đã đăng nhập ở một thiết bị khác. Vui lòng tải lại trang và đăng nhập lại.");
            }

            if (user.IsLocked)
            {
                return (false, "Tài khoản tạm thời bị khóa. Vui lòng liên hệ Admin qua Zalo để mở khóa.");
            }

            var now = GetVietnamNow();

            // Tầng 1: Cooldown (60 giây giữa 2 lần bấm)
            if (user.LastGeneratedAt.HasValue)
            {
                var secondsSinceLast = (now - user.LastGeneratedAt.Value).TotalSeconds;
                if (secondsSinceLast < 60)
                {
                    int waitSeconds = 60 - (int)secondsSinceLast;
                    return (false, $"Hệ thống đang bảo vệ tài nguyên AI. Vui lòng chờ {waitSeconds} giây để duyệt đề vừa tạo trước khi bấm tạo đề tiếp theo!");
                }
            }

            // Tầng 3: Kiểm tra hạn sử dụng gói & Tổng số lượt còn lại
            if (user.ExpireAt.HasValue && user.ExpireAt.Value < now)
            {
                return (false, "Gói bản quyền của bạn đã hết hạn. Vui lòng bấm 'Nạp Key / Nâng cấp' để tiếp tục sử dụng.");
            }

            if (user.TotalQuotaRemaining <= 0)
            {
                return (false, "Bạn đã sử dụng hết số lượt tạo đề thi của gói hiện tại. Vui lòng bấm 'Nạp Key / Nâng cấp' để bổ sung lượt!");
            }

            // Tầng 2: Giới hạn số đề theo ngày (Daily Cap)
            string todayStr = GetVietnamTodayString();
            int currentDailyCount = (user.LastGeneratedDate == todayStr) ? user.DailyGeneratedCount : 0;
            if (currentDailyCount >= user.DailyQuotaLimit)
            {
                return (false, $"Bạn đã đạt giới hạn tối đa {user.DailyQuotaLimit} đề/ngày của gói {user.PackageType}. Quy định này nhằm chống chia sẻ tài khoản tập thể. Vui lòng quay lại vào ngày mai hoặc liên hệ Admin!");
            }

            return (true, "Hợp lệ");
        }

        // Tầng 5: Trừ lượt an toàn - CHỈ GỌI KHI AI ĐÃ TẠO ĐỀ THÀNH CÔNG 100%
        public async Task CommitSuccessfulExamAsync(UserRecord user, string subject, int grade, string period, string title, string examDataJson)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            var now = GetVietnamNow();
            string todayStr = GetVietnamTodayString();
            int newDailyCount = (user.LastGeneratedDate == todayStr) ? (user.DailyGeneratedCount + 1) : 1;
            int newTotalQuota = Math.Max(0, user.TotalQuotaRemaining - 1);

            // 1. Cập nhật lượt người dùng
            using (var updateCmd = conn.CreateCommand())
            {
                updateCmd.CommandText = @"
                    UPDATE users 
                    SET total_quota_remaining = @quota,
                        daily_generated_count = @daily,
                        last_generated_date = @today,
                        last_generated_at = @now
                    WHERE id = @id";

                AddParam(updateCmd, "@quota", newTotalQuota);
                AddParam(updateCmd, "@daily", newDailyCount);
                AddParam(updateCmd, "@today", todayStr);
                AddParam(updateCmd, "@now", now);
                AddParam(updateCmd, "@id", user.Id);
                await updateCmd.ExecuteNonQueryAsync();
            }

            // 2. Lưu vào kho lịch sử đề thi (Kho lưu trữ đề cũ miễn phí)
            using (var logCmd = conn.CreateCommand())
            {
                logCmd.CommandText = @"
                    INSERT INTO exam_logs (id, user_id, subject, grade, period, title, exam_data_json, created_at)
                    VALUES (@logId, @userId, @sub, @grade, @period, @title, @json, @created)";

                AddParam(logCmd, "@logId", Guid.NewGuid().ToString("N"));
                AddParam(logCmd, "@userId", user.Id);
                AddParam(logCmd, "@sub", subject);
                AddParam(logCmd, "@grade", grade);
                AddParam(logCmd, "@period", period);
                AddParam(logCmd, "@title", title);
                AddParam(logCmd, "@json", examDataJson);
                AddParam(logCmd, "@created", DateTime.UtcNow);
                await logCmd.ExecuteNonQueryAsync();
            }
        }

        // ================= KÍCH HOẠT MÃ BẢN QUYỀN (LICENSE KEY) =================

        public async Task<(bool Success, string Message)> ActivateLicenseKeyAsync(UserRecord user, string keyCode)
        {
            keyCode = keyCode.Trim().ToUpper();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            // 1. Tìm mã key
            LicenseKeyRecord? key = null;
            using (var findCmd = conn.CreateCommand())
            {
                findCmd.CommandText = "SELECT * FROM license_keys WHERE key_code = @code LIMIT 1";
                AddParam(findCmd, "@code", keyCode);
                using var reader = await findCmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    key = new LicenseKeyRecord
                    {
                        Id = reader["id"].ToString() ?? "",
                        KeyCode = reader["key_code"].ToString() ?? "",
                        PackageType = reader["package_type"].ToString() ?? "",
                        DaysDuration = Convert.ToInt32(reader["days_duration"]),
                        QuotaAmount = Convert.ToInt32(reader["quota_amount"]),
                        DailyLimit = Convert.ToInt32(reader["daily_limit"]),
                        IsUsed = Convert.ToBoolean(reader["is_used"])
                    };
                }
            }

            if (key == null)
            {
                return (false, "Mã kích hoạt không tồn tại. Vui lòng kiểm tra lại ký tự hoặc liên hệ Admin.");
            }

            if (key.IsUsed)
            {
                return (false, "Mã kích hoạt này đã được sử dụng trước đó.");
            }

            // 2. Tính ngày hết hạn mới (gia hạn thêm)
            var now = GetVietnamNow();
            DateTime newExpire = (user.ExpireAt.HasValue && user.ExpireAt.Value > now)
                ? user.ExpireAt.Value.AddDays(key.DaysDuration)
                : now.AddDays(key.DaysDuration);

            int newQuota = user.TotalQuotaRemaining + key.QuotaAmount;

            // 3. Cập nhật User
            using (var updateUserCmd = conn.CreateCommand())
            {
                updateUserCmd.CommandText = @"
                    UPDATE users
                    SET package_type = @pkg,
                        expire_at = @expire,
                        total_quota_remaining = @quota,
                        daily_quota_limit = @dailyLimit
                    WHERE id = @id";

                AddParam(updateUserCmd, "@pkg", key.PackageType);
                AddParam(updateUserCmd, "@expire", newExpire);
                AddParam(updateUserCmd, "@quota", newQuota);
                AddParam(updateUserCmd, "@dailyLimit", key.DailyLimit);
                AddParam(updateUserCmd, "@id", user.Id);
                await updateUserCmd.ExecuteNonQueryAsync();
            }

            // 4. Đánh dấu Key đã dùng
            using (var updateKeyCmd = conn.CreateCommand())
            {
                updateKeyCmd.CommandText = @"
                    UPDATE license_keys
                    SET is_used = @isUsed,
                        used_by_phone = @phone,
                        used_at = @usedAt
                    WHERE id = @keyId";

                AddParam(updateKeyCmd, "@isUsed", true);
                AddParam(updateKeyCmd, "@phone", user.Phone);
                AddParam(updateKeyCmd, "@usedAt", now);
                AddParam(updateKeyCmd, "@keyId", key.Id);
                await updateKeyCmd.ExecuteNonQueryAsync();
            }

            return (true, $"Kích hoạt thành công gói {key.PackageType}! Tài khoản được cộng thêm {key.QuotaAmount} lượt tạo đề và gia hạn đến ngày {newExpire:dd/MM/yyyy}.");
        }

        // ================= KHO ĐỀ THI CỦA TÔI =================

        public async Task<List<ExamLogItem>> GetUserExamsAsync(string userId)
        {
            var list = new List<ExamLogItem>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT id, user_id, subject, grade, period, title, created_at FROM exam_logs WHERE user_id = @uid ORDER BY created_at DESC LIMIT 50";
            AddParam(cmd, "@uid", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new ExamLogItem
                {
                    Id = reader["id"].ToString() ?? "",
                    UserId = reader["user_id"].ToString() ?? "",
                    Subject = reader["subject"].ToString() ?? "",
                    Grade = Convert.ToInt32(reader["grade"]),
                    Period = reader["period"].ToString() ?? "",
                    Title = reader["title"].ToString() ?? "",
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                });
            }
            return list;
        }

        public async Task<ExamLogItem?> GetExamByIdAsync(string userId, string examId)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM exam_logs WHERE id = @eid AND user_id = @uid LIMIT 1";
            AddParam(cmd, "@eid", examId);
            AddParam(cmd, "@uid", userId);

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new ExamLogItem
                {
                    Id = reader["id"].ToString() ?? "",
                    UserId = reader["user_id"].ToString() ?? "",
                    Subject = reader["subject"].ToString() ?? "",
                    Grade = Convert.ToInt32(reader["grade"]),
                    Period = reader["period"].ToString() ?? "",
                    Title = reader["title"].ToString() ?? "",
                    ExamDataJson = reader["exam_data_json"].ToString() ?? "",
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                };
            }
            return null;
        }

        // ================= ADMIN MANAGEMENT =================

        public bool VerifyAdminPin(string pin)
        {
            var envPin = Environment.GetEnvironmentVariable("ADMIN_PIN") ?? "888888";
            return pin.Trim() == envPin.Trim();
        }

        public async Task<List<UserRecord>> AdminGetAllUsersAsync()
        {
            var list = new List<UserRecord>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM users ORDER BY created_at DESC LIMIT 200";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(MapUser(reader));
            }
            return list;
        }

        public async Task<bool> AdminActivateUserAsync(string phone, string packageType)
        {
            phone = phone.Trim();
            var user = await GetUserByPhoneAsync(phone);
            if (user == null) return false;

            int days = packageType == "Year" ? 365 : (packageType == "School" ? 365 : (packageType == "Bonus5" ? 0 : 150));
            int quota = packageType == "Year" ? 30 : (packageType == "School" ? 100 : (packageType == "Bonus5" ? 5 : 15));
            int dailyLimit = packageType == "Year" ? 4 : (packageType == "School" ? 10 : (packageType == "Bonus5" ? user.DailyQuotaLimit : 2));

            var now = GetVietnamNow();
            DateTime newExpire = (packageType == "Bonus5")
                ? (user.ExpireAt ?? now.AddDays(30))
                : ((user.ExpireAt.HasValue && user.ExpireAt.Value > now)
                    ? user.ExpireAt.Value.AddDays(days)
                    : now.AddDays(days));
            string finalPkg = (packageType == "Bonus5") ? user.PackageType : packageType;

            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                UPDATE users 
                SET package_type = @pkg,
                    expire_at = @expire,
                    total_quota_remaining = total_quota_remaining + @quota,
                    daily_quota_limit = @dailyLimit,
                    is_locked = @isLocked
                WHERE phone = @phone";

            AddParam(cmd, "@pkg", finalPkg);
            AddParam(cmd, "@expire", newExpire);
            AddParam(cmd, "@quota", quota);
            AddParam(cmd, "@dailyLimit", dailyLimit);
            AddParam(cmd, "@isLocked", false);
            AddParam(cmd, "@phone", phone);

            int affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<bool> AdminToggleLockUserAsync(string userId)
        {
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "UPDATE users SET is_locked = NOT is_locked WHERE id = @uid";
            AddParam(cmd, "@uid", userId);
            int affected = await cmd.ExecuteNonQueryAsync();
            return affected > 0;
        }

        public async Task<List<string>> AdminGenerateKeysAsync(string packageType, int count)
        {
            int days = packageType == "Year" ? 365 : (packageType == "SchoolItem" ? 365 : 150);
            int quota = packageType == "Year" ? 30 : (packageType == "SchoolItem" ? 20 : 15);
            int dailyLimit = packageType == "Year" ? 4 : (packageType == "SchoolItem" ? 3 : 2);
            string prefix = packageType == "Year" ? "NAM" : (packageType == "SchoolItem" ? "TRUONG" : "KY");

            var generatedCodes = new List<string>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            for (int i = 0; i < count; i++)
            {
                var rand = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper();
                string keyCode = $"THEX-2026-{prefix}-{rand.Substring(0, 4)}-{rand.Substring(4, 4)}";

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO license_keys (id, key_code, package_type, days_duration, quota_amount, daily_limit, is_used, created_at)
                    VALUES (@id, @code, @pkg, @days, @quota, @daily, @isUsed, @created)";

                AddParam(cmd, "@id", Guid.NewGuid().ToString("N"));
                AddParam(cmd, "@code", keyCode);
                AddParam(cmd, "@pkg", packageType);
                AddParam(cmd, "@days", days);
                AddParam(cmd, "@quota", quota);
                AddParam(cmd, "@daily", dailyLimit);
                AddParam(cmd, "@isUsed", false);
                AddParam(cmd, "@created", DateTime.UtcNow);

                await cmd.ExecuteNonQueryAsync();
                generatedCodes.Add(keyCode);
            }

            return generatedCodes;
        }

        public async Task<List<LicenseKeyRecord>> AdminGetAllKeysAsync()
        {
            var list = new List<LicenseKeyRecord>();
            using var conn = _db.CreateConnection();
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT * FROM license_keys ORDER BY created_at DESC LIMIT 200";

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                list.Add(new LicenseKeyRecord
                {
                    Id = reader["id"].ToString() ?? "",
                    KeyCode = reader["key_code"].ToString() ?? "",
                    PackageType = reader["package_type"].ToString() ?? "",
                    DaysDuration = Convert.ToInt32(reader["days_duration"]),
                    QuotaAmount = Convert.ToInt32(reader["quota_amount"]),
                    DailyLimit = Convert.ToInt32(reader["daily_limit"]),
                    IsUsed = Convert.ToBoolean(reader["is_used"]),
                    UsedByPhone = reader["used_by_phone"]?.ToString(),
                    UsedAt = reader["used_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["used_at"]),
                    CreatedAt = Convert.ToDateTime(reader["created_at"])
                });
            }
            return list;
        }

        // ================= HELPERS =================

        private static void AddParam(DbCommand cmd, string name, object? value)
        {
            var p = cmd.CreateParameter();
            p.ParameterName = name;
            p.Value = value ?? DBNull.Value;
            cmd.Parameters.Add(p);
        }

        private static UserRecord MapUser(DbDataReader reader)
        {
            return new UserRecord
            {
                Id = reader["id"].ToString() ?? "",
                Phone = reader["phone"].ToString() ?? "",
                PasswordHash = reader["password_hash"].ToString() ?? "",
                FullName = reader["full_name"]?.ToString() ?? "",
                School = reader["school"]?.ToString() ?? "",
                PackageType = reader["package_type"]?.ToString() ?? "Trial",
                ExpireAt = reader["expire_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["expire_at"]),
                TotalQuotaRemaining = Convert.ToInt32(reader["total_quota_remaining"]),
                DailyQuotaLimit = Convert.ToInt32(reader["daily_quota_limit"]),
                DailyGeneratedCount = Convert.ToInt32(reader["daily_generated_count"]),
                LastGeneratedDate = reader["last_generated_date"]?.ToString() ?? "",
                LastGeneratedAt = reader["last_generated_at"] == DBNull.Value ? null : Convert.ToDateTime(reader["last_generated_at"]),
                CurrentSessionToken = reader["current_session_token"]?.ToString() ?? "",
                DeviceId = reader["device_id"]?.ToString() ?? "",
                IsLocked = Convert.ToBoolean(reader["is_locked"]),
                CreatedAt = Convert.ToDateTime(reader["created_at"])
            };
        }
    }
}
