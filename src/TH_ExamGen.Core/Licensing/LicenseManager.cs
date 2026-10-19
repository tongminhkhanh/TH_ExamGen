using System;
using System.IO;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

namespace TH_ExamGen.Core.Licensing
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public static class LicenseManager
    {
        public const string SALT = "THEXAM_STUDIO_GDPT2018_2026_@#$*ITONG_SECURE_AUTH";
        public const string REG_KEY = @"Software\THExamStudio";
        public const int TRIAL_DAYS = 14;
        public const int TRIAL_MAX_COUNT = 10;
        public const string CONTACT_HOTLINE = "0326439774";

        private static string GetAppDataBackupPath()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string dir = Path.Combine(appData, "THExamStudio");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                return Path.Combine(dir, "license_cache.dat");
            }
            catch
            {
                return "";
            }
        }

        public static string GetMachineId()
        {
            try
            {
                string cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
                string mbSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
                string diskSerial = GetDiskSerial();

                string raw = (cpuId + "_" + mbSerial + "_" + diskSerial + "_" + Environment.MachineName).Trim();
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                    string hex = BitConverter.ToString(hash).Replace("-", "");
                    return string.Format("THEX-{0}-{1}-{2}", hex.Substring(0, 4), hex.Substring(4, 4), hex.Substring(8, 4));
                }
            }
            catch
            {
                string fallback = Environment.MachineName + "_" + Environment.UserName;
                using (SHA256 sha = SHA256.Create())
                {
                    byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fallback));
                    string hex = BitConverter.ToString(hash).Replace("-", "");
                    return string.Format("THEX-{0}-{1}-{2}", hex.Substring(0, 4), hex.Substring(4, 4), hex.Substring(8, 4));
                }
            }
        }

        private static string GetWmiProperty(string table, string prop)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT " + prop + " FROM " + table))
                {
                    foreach (ManagementObject mo in searcher.Get())
                    {
                        object val = mo[prop];
                        if (val != null && !string.IsNullOrEmpty(val.ToString()?.Trim()))
                            return val.ToString()!.Trim();
                    }
                }
            }
            catch { }
            return "DEFAULT";
        }

        private static string GetDiskSerial()
        {
            try
            {
                DriveInfo d = new DriveInfo("C");
                return d.TotalSize.ToString();
            }
            catch
            {
                return "12345678";
            }
        }

        public static string GenerateKey(string machineId, DateTime expireDate)
        {
            if (string.IsNullOrEmpty(machineId)) return "";
            machineId = machineId.Trim().ToUpper();
            string dateStr = expireDate.ToString("yyyyMMdd");
            string data = machineId + "_" + dateStr + "_" + SALT;
            using (SHA256 sha = SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(data));
                string hex = BitConverter.ToString(hash).Replace("-", "");
                return string.Format("{0}-{1}-{2}-{3}", 
                    dateStr, hex.Substring(0, 4), hex.Substring(4, 4), hex.Substring(8, 4));
            }
        }

        public static bool ValidateKey(string machineId, string key, out DateTime expireDate)
        {
            expireDate = DateTime.MinValue;
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(machineId)) return false;
            key = key.Trim().ToUpper().Replace(" ", "");
            machineId = machineId.Trim().ToUpper();

            string cleanKey = key.Replace("-", "");
            if (cleanKey.Length >= 20)
            {
                string datePart = cleanKey.Substring(0, 8);
                if (DateTime.TryParseExact(datePart, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateTime parsedDate))
                {
                    string expected = GenerateKey(machineId, parsedDate).Replace("-", "");
                    if (string.Equals(expected, cleanKey, StringComparison.OrdinalIgnoreCase))
                    {
                        expireDate = parsedDate;
                        // Kiểm tra chống chỉnh lùi ngày hệ thống
                        if (CheckClockTampering()) return false;
                        RecordLastRunTime();
                        return DateTime.Now.Date <= parsedDate.Date;
                    }
                }
            }

            return false;
        }

        public static bool SaveKey(string key)
        {
            bool regSuccess = false;
            try
            {
                using (RegistryKey? rk = Registry.CurrentUser.CreateSubKey(REG_KEY))
                {
                    if (rk != null)
                    {
                        rk.SetValue("LicenseKey", key.Trim().ToUpper());
                        regSuccess = true;
                    }
                }
            }
            catch { }

            try
            {
                string backupPath = GetAppDataBackupPath();
                if (!string.IsNullOrEmpty(backupPath))
                {
                    string toSave = key.Trim().ToUpper() + "#" + SALT;
                    string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(toSave));
                    File.WriteAllText(backupPath, b64);
                }
            }
            catch { }

            return regSuccess;
        }

        public static string GetSavedKey()
        {
            try
            {
                using (RegistryKey? rk = Registry.CurrentUser.OpenSubKey(REG_KEY))
                {
                    if (rk != null)
                    {
                        object? val = rk.GetValue("LicenseKey");
                        if (val != null && !string.IsNullOrWhiteSpace(val.ToString()))
                            return val.ToString()!.Trim();
                    }
                }
            }
            catch { }

            try
            {
                string backupPath = GetAppDataBackupPath();
                if (!string.IsNullOrEmpty(backupPath) && File.Exists(backupPath))
                {
                    string b64 = File.ReadAllText(backupPath).Trim();
                    string raw = Encoding.UTF8.GetString(Convert.FromBase64String(b64));
                    string[] parts = raw.Split('#');
                    if (parts.Length >= 2 && parts[1] == SALT)
                    {
                        return parts[0].Trim();
                    }
                }
            }
            catch { }

            return "";
        }

        private static void RecordLastRunTime()
        {
            try
            {
                string nowStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "#" + SALT;
                string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(nowStr));
                using (RegistryKey? rk = Registry.CurrentUser.CreateSubKey(REG_KEY))
                {
                    rk?.SetValue("LastRun", b64);
                }
            }
            catch { }
        }

        private static bool CheckClockTampering()
        {
            try
            {
                using (RegistryKey? rk = Registry.CurrentUser.OpenSubKey(REG_KEY))
                {
                    if (rk != null)
                    {
                        object? val = rk.GetValue("LastRun");
                        if (val != null)
                        {
                            string raw = Encoding.UTF8.GetString(Convert.FromBase64String(val.ToString()!));
                            string[] parts = raw.Split('#');
                            if (parts.Length >= 2 && DateTime.TryParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", 
                                System.Globalization.CultureInfo.InvariantCulture, 
                                System.Globalization.DateTimeStyles.None, out DateTime lastRun))
                            {
                                if (DateTime.Now < lastRun - TimeSpan.FromHours(1))
                                {
                                    return true; // Phát hiện giờ hệ thống bị chỉnh lùi về quá khứ
                                }
                            }
                        }
                    }
                }
            }
            catch { }
            return false;
        }

        private static DateTime GetOrCreateTrialStartDate()
        {
            try
            {
                using (RegistryKey? rk = Registry.CurrentUser.CreateSubKey(REG_KEY))
                {
                    if (rk != null)
                    {
                        object? val = rk.GetValue("TrialStart");
                        if (val != null)
                        {
                            try
                            {
                                string raw = Encoding.UTF8.GetString(Convert.FromBase64String(val.ToString()!));
                                string[] parts = raw.Split('#');
                                if (parts.Length >= 2 && DateTime.TryParseExact(parts[0], "yyyy-MM-dd HH:mm:ss", 
                                    System.Globalization.CultureInfo.InvariantCulture, 
                                    System.Globalization.DateTimeStyles.None, out DateTime dt))
                                {
                                    return dt;
                                }
                            }
                            catch { }
                        }

                        DateTime now = DateTime.Now;
                        string toSave = now.ToString("yyyy-MM-dd HH:mm:ss") + "#" + SALT;
                        string b64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(toSave));
                        rk.SetValue("TrialStart", b64);
                        return now;
                    }
                }
            }
            catch { }
            return DateTime.Now;
        }

        public static int GetTrialCount()
        {
            try
            {
                using (RegistryKey? rk = Registry.CurrentUser.OpenSubKey(REG_KEY))
                {
                    if (rk != null)
                    {
                        int c1 = 0, c2 = 0;
                        object? val1 = rk.GetValue("TrialCount");
                        if (val1 != null && int.TryParse(val1.ToString(), out int v1)) c1 = v1;

                        object? val2 = rk.GetValue("SystemTrialCount");
                        if (val2 != null && int.TryParse(val2.ToString(), out int v2)) c2 = v2;

                        return Math.Max(c1, c2);
                    }
                }
            }
            catch { }
            return 0;
        }

        public static void IncrementTrialCount()
        {
            try
            {
                int current = GetTrialCount();
                using (RegistryKey? rk = Registry.CurrentUser.CreateSubKey(REG_KEY))
                {
                    rk?.SetValue("TrialCount", current + 1);
                    rk?.SetValue("SystemTrialCount", current + 1);
                }
            }
            catch { }
        }

        public static bool IsActivated()
        {
            string mid = GetMachineId();
            string savedKey = GetSavedKey();
            return ValidateKey(mid, savedKey, out _);
        }

        public static bool GetLicenseStatus(out string statusMessage, out bool isOfficial, out int trialDaysLeft, out int remainingCount)
        {
            string mid = GetMachineId();
            string savedKey = GetSavedKey();
            if (ValidateKey(mid, savedKey, out DateTime expireDate))
            {
                statusMessage = string.Format("Bản quyền chính thức (Hạn đến {0:dd/MM/yyyy})", expireDate);
                isOfficial = true;
                trialDaysLeft = 0;
                remainingCount = 9999;
                return true;
            }

            isOfficial = false;
            DateTime start = GetOrCreateTrialStartDate();
            int elapsed = (int)(DateTime.Now.Date - start.Date).TotalDays;
            trialDaysLeft = Math.Max(0, TRIAL_DAYS - elapsed);
            int countUsed = GetTrialCount();
            remainingCount = Math.Max(0, TRIAL_MAX_COUNT - countUsed);

            if (CheckClockTampering())
            {
                statusMessage = "Cảnh báo: Thời gian hệ thống bị chỉnh sửa không chính xác. Vui lòng đặt lại ngày giờ chuẩn!";
                return false;
            }

            if (elapsed < 0 || trialDaysLeft <= 0)
            {
                statusMessage = $"Đã hết hạn {TRIAL_DAYS} ngày dùng thử. Vui lòng kích hoạt bản quyền!";
                return false;
            }

            if (remainingCount <= 0)
            {
                statusMessage = $"Đã dùng hết {TRIAL_MAX_COUNT} lượt tạo đề thử miễn phí. Vui lòng kích hoạt bản quyền!";
                return false;
            }

            statusMessage = $"Bản dùng thử (Còn {remainingCount} lượt / {trialDaysLeft} ngày)";
            return true;
        }

        public static bool CanUseSoftware(out string errorMessage)
        {
            errorMessage = "";
            bool canUse = GetLicenseStatus(out string statusMessage, out bool isOfficial, out _, out _);
            if (isOfficial) return true;

            if (!canUse)
            {
                errorMessage = string.Format(
                    "{0}\n\n" +
                    "Để tiếp tục tạo đề thi và xuất file Word không giới hạn, quý thầy cô vui lòng kích hoạt BẢN QUYỀN CHÍNH THỨC.\n\n" +
                    "Bấm vào mục [Bản Quyền] hoặc liên hệ Hotline/Zalo: {1} để nhận mã kích hoạt bản quyền.",
                    statusMessage, CONTACT_HOTLINE);
                return false;
            }

            return true;
        }
    }
}
