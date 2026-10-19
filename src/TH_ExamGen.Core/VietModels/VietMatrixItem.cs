using System.ComponentModel;
using System.Text.Json.Serialization;

namespace TH_ExamGen.Core.VietModels
{
    public class VietMatrixItem : INotifyPropertyChanged
    {
        public int SoThuTu { get; set; }
        public string MaMucChuongTrinh { get; set; } = string.Empty;
        public string MachKienThuc { get; set; } = "Đọc hiểu văn bản"; // Đọc hiểu văn bản, Kiến thức Tiếng Việt
        public string NoiDungKienThuc { get; set; } = string.Empty;
        public string YeuCauCanDat { get; set; } = string.Empty;
        public string MucDo { get; set; } = "M1"; // M1, M2, M3
        public string HinhThuc { get; set; } = "TN"; // TN, TL
        public double Diem { get; set; } = 0.5;

        private bool _isAiNew;

        /// <summary>
        /// Dòng do AI vừa tạo trong phiên làm việc này và giáo viên chưa rà soát (tô vàng trên bảng).
        /// Không lưu vào bản nháp - chỉ có ý nghĩa trong phiên hiện tại.
        /// </summary>
        [JsonIgnore]
        public bool IsAiNew
        {
            get => _isAiNew;
            set
            {
                if (_isAiNew == value) return;
                _isAiNew = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsAiNew)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}
