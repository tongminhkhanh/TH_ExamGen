namespace TH_ExamGen.Core.VietModels
{
    public class ReadingPassage
    {
        public string TieuDe { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string TheLoai { get; set; } = "Văn bản văn học"; // Văn bản văn học, Văn bản thông tin
        public string ChuDe { get; set; } = "Bảo vệ môi trường";
        public string MaMucChuongTrinh { get; set; } = string.Empty;
        public string NguonGoc { get; set; } = "teacher-authored"; // AI-authored, teacher-authored, external
        public string NguonChiTiet { get; set; } = string.Empty;
    }
}
