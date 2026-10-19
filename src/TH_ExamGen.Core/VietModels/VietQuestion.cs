namespace TH_ExamGen.Core.VietModels
{
    public class VietQuestion
    {
        public int SoThuTu { get; set; }
        public string MaMucChuongTrinh { get; set; } = string.Empty;
        public string MachKienThuc { get; set; } = "Đọc hiểu văn bản"; // Đọc hiểu văn bản, Kiến thức Tiếng Việt
        public string YeuCauCanDat { get; set; } = string.Empty;
        public string MucDo { get; set; } = "M1"; // M1, M2, M3
        public string LoaiCauHoi { get; set; } = "TN_4LuaChon"; // TN_4LuaChon, TL
        public double Diem { get; set; } = 0.5;
        public string NoiDung { get; set; } = string.Empty;
        public string DapAnA { get; set; } = string.Empty;
        public string DapAnB { get; set; } = string.Empty;
        public string DapAnC { get; set; } = string.Empty;
        public string DapAnD { get; set; } = string.Empty;
        public string DapAnDung { get; set; } = string.Empty;
        public string HuongDanCham { get; set; } = string.Empty;
        public int SoDongOly { get; set; } = 0; // 0 for TN, 3-4 for TL (câu 5, 6, 7, 10, 11)
    }
}
