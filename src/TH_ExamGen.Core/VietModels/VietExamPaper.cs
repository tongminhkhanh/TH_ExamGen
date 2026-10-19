using System.Collections.Generic;

namespace TH_ExamGen.Core.VietModels
{
    public class VietExamPaper
    {
        public string MonHoc { get; set; } = "Tiếng Việt";
        public int KhoiLop { get; set; } = 4;
        public int ThoiGianPhut { get; set; } = 35; // Thời gian làm bài đọc hiểu & kiến thức tiếng Việt
        public string Period { get; set; } = "Cuối Học kỳ II";
        public string TieuDe { get; set; } = "BÀI KIỂM TRA ĐỊNH KỲ CUỐI HỌC KỲ II";
        public string HuongDanDocThanhTieng { get; set; } = 
            "1. Giáo viên cho học sinh bốc thăm đọc một đoạn văn khoảng 105 - 110 tiếng/phút trong các bài tập đọc đã học.\n" +
            "2. Học sinh trả lời 01 câu hỏi về nội dung đoạn vừa đọc do giáo viên nêu.\n" +
            "- Điểm đọc thành tiếng: 1.0 điểm (đọc đúng tiếng, từ, ngắt nghỉ đúng dấu câu, giọng đọc diễn cảm).\n" +
            "- Điểm trả lời câu hỏi: 1.0 điểm (trả lời đúng, rõ ràng, đủ ý).";

        public ReadingPassage BaiDoc { get; set; } = new();
        public List<VietQuestion> DanhSachCauHoi { get; set; } = new();
        public List<VietMatrixItem> DanhSachMaTran { get; set; } = new();

        /// <summary>
        /// Cảnh báo sau khi sinh đề (ví dụ: AI chưa khớp ma trận sau nhiều lần thử) — hiển thị cho
        /// giáo viên rà soát nhưng KHÔNG chặn xem trước / xuất Word. Được lưu kèm bản nháp.
        /// </summary>
        public List<string> CanhBaoKiemTra { get; set; } = new();

        public static List<VietMatrixItem> GetDefaultGrade4Matrix()
        {
            return VietCurriculumBank.GetGrade4FinalTerm2Matrix();
        }
    }
}
