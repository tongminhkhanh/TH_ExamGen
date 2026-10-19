namespace TH_ExamGen.Core.Curriculum
{
    /// <summary>Shared scope metadata for a lesson/skill in a subject curriculum catalog.</summary>
    public interface ICurriculumTopicItem
    {
        string MaMuc { get; set; }
        string MaPhienBan { get; set; }
        int KhoiLop { get; set; }
        string HocKy { get; set; }
        int TuanBatDau { get; set; }
        int TuanKetThuc { get; set; }
        string NguonThamKhao { get; set; }
        string CanCuTuan { get; set; }
        bool DaXacNhan { get; set; }
        string MachKienThuc { get; set; }
        string ChuDe { get; set; }
        string NoiDung { get; set; }
        string Tuan { get; set; }
    }
}
