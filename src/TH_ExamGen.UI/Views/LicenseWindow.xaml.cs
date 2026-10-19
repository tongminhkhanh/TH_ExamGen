using System;
using System.Windows;
using System.Windows.Media;
using TH_ExamGen.Core.Licensing;

namespace TH_ExamGen.UI.Views
{
    public partial class LicenseWindow : Window
    {
        public bool IsSuccessfullyActivated { get; private set; } = false;

        public LicenseWindow()
        {
            InitializeComponent();
            LoadLicenseInfo();
        }

        private void LoadLicenseInfo()
        {
            txtMachineId.Text = LicenseManager.GetMachineId();
            txtLicenseKey.Text = "";
            UpdateStatusDisplay();
        }

        private void UpdateStatusDisplay()
        {
            bool ok = LicenseManager.GetLicenseStatus(out string msg, out bool isOfficial, out int daysLeft, out int remainingCount);
            lblLicenseStatus.Text = msg;

            if (isOfficial)
            {
                lblLicenseStatus.Foreground = new SolidColorBrush(Color.FromRgb(22, 163, 74)); // Green
            }
            else if (ok)
            {
                lblLicenseStatus.Foreground = new SolidColorBrush(Color.FromRgb(217, 119, 6)); // Orange/Amber
            }
            else
            {
                lblLicenseStatus.Foreground = new SolidColorBrush(Color.FromRgb(220, 38, 38)); // Red
            }
        }

        private void BtnCopyMachineId_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(txtMachineId.Text);
            MessageBox.Show(
                $"Đã sao chép Mã máy vào bộ nhớ tạm!\n\n" +
                $"Mã máy: {txtMachineId.Text}\n\n" +
                $"Quý Thầy/Cô vui lòng dán (Ctrl+V) gửi qua Zalo tác giả: {LicenseManager.CONTACT_HOTLINE} để nhận mã kích hoạt bản quyền.",
                "Đã sao chép", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void BtnActivate_Click(object sender, RoutedEventArgs e)
        {
            string key = txtLicenseKey.Text.Trim();
            if (string.IsNullOrWhiteSpace(key))
            {
                MessageBox.Show("Vui lòng nhập mã kích hoạt (License Key) được cấp!", "Cảnh báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtLicenseKey.Focus();
                return;
            }

            string mid = txtMachineId.Text.Trim();
            if (LicenseManager.ValidateKey(mid, key, out DateTime expireDate))
            {
                LicenseManager.SaveKey(key);
                IsSuccessfullyActivated = true;
                txtLicenseKey.Text = "";
                UpdateStatusDisplay();
                MessageBox.Show(
                    $"Chúc mừng bạn đã kích hoạt bản quyền TH-Exam Studio thành công!\n\n" +
                    $"• Thời hạn bản quyền: Đến hết ngày {expireDate:dd/MM/yyyy}\n" +
                    $"• Toàn bộ tính năng tạo đề Toán, Tiếng Việt và xuất Word đã được mở khóa đầy đủ.",
                    "Kích hoạt thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            else
            {
                MessageBox.Show(
                    $"Mã kích hoạt không đúng hoặc đã hết hạn cho máy tính này!\n\n" +
                    $"Vui lòng kiểm tra lại hoặc liên hệ Hotline/Zalo: {LicenseManager.CONTACT_HOTLINE} để được cấp mã chính xác.",
                    "Lỗi kích hoạt", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
