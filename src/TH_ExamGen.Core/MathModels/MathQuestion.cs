using System.Collections.Generic;

namespace TH_ExamGen.Core.MathModels
{
    public class MathStepSolution
    {
        public int Buoc { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public double Diem { get; set; } = 0.25;
    }

    public class MathQuestion
    {
        public int SoThuTu { get; set; }
        public string MaMucChuongTrinh { get; set; } = string.Empty;
        public string ChuDe { get; set; } = string.Empty;
        public string YeuCauCanDat { get; set; } = string.Empty;
        public string MucDo { get; set; } = "M1"; // M1, M2, M3
        public string LoaiCauHoi { get; set; } = "TN_4LuaChon"; // TN_4LuaChon, TN_DungSai, TN_TraLoiNgan, TL_TinhToan, TL_LoiVan
        public double Diem { get; set; } = 1.0;
        public string NoiDung { get; set; } = string.Empty;
        public string DapAnA { get; set; } = string.Empty;
        public string DapAnB { get; set; } = string.Empty;
        public string DapAnC { get; set; } = string.Empty;
        public string DapAnD { get; set; } = string.Empty;
        public string DapAnDung { get; set; } = string.Empty;
        public bool CoHinhTikz { get; set; } = false;
        public string MaTikz { get; set; } = string.Empty;

        /// <summary>Nguồn sinh mã TikZ: "Template" (mẫu viết tay), "AI" (AI vẽ), "Fallback" (hình mẫu chung).</summary>
        public string TikzSource { get; set; } = string.Empty;
        public string? AnhTikzPath { get; set; }
        public int SoDongOly { get; set; } = 0; // 0 for TN, 4 for Câu 8, 8 for Câu 9, 10
        public List<MathStepSolution> CacBuocGiai { get; set; } = new();
    }
}
