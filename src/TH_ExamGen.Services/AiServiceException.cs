using System;

namespace TH_ExamGen.Services
{
    /// <summary>
    /// Lỗi gọi dịch vụ AI (Gemini/DeepSeek) đã được dịch sang thông báo tiếng Việt
    /// thân thiện cho giáo viên. Chi tiết kỹ thuật gốc được giữ ở <see cref="Detail"/>
    /// để ghi log, không hiển thị trực tiếp cho người dùng.
    /// </summary>
    public class AiServiceException : Exception
    {
        public string Detail { get; }

        public AiServiceException(string friendlyMessage, string? detail = null)
            : base(friendlyMessage)
        {
            Detail = detail ?? string.Empty;
        }
    }
}
