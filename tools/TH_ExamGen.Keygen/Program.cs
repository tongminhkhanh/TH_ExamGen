using System;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace TH_ExamGen.Keygen
{
    public class KeygenApp : Form
    {
        public const string SALT = "THEXAM_STUDIO_GDPT2018_2026_@#$*ITONG_SECURE_AUTH";

        private TextBox _txtMachineId = null!;
        private DateTimePicker _dtpExpire = null!;
        private TextBox _txtKey = null!;
        private Label _lblExpiryInfo = null!;
        private Button _btnGenerate = null!;
        private Button _btnCopy = null!;

        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new KeygenApp());
        }

        public KeygenApp()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "CÔNG CỤ CẤP KEY BẢN QUYỀN — TH-EXAM STUDIO";
            this.Size = new Size(590, 480);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            this.BackColor = Color.FromArgb(248, 250, 252);

            try
            {
                string icoFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(icoFile)) this.Icon = new Icon(icoFile);
            }
            catch { }

            // Title
            Label lblTitle = new Label();
            lblTitle.Text = "HỆ THỐNG CẤP KEY BẢN QUYỀN TH-EXAM STUDIO";
            lblTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
            lblTitle.ForeColor = Color.FromArgb(15, 23, 42); // Navy
            lblTitle.Location = new Point(22, 16);
            lblTitle.AutoSize = true;
            this.Controls.Add(lblTitle);

            Label lblSubtitle = new Label();
            lblSubtitle.Text = "Sinh mã kích hoạt chính thức cho giáo viên Trường Tiểu học Ít Ong & các trường";
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblSubtitle.ForeColor = Color.FromArgb(100, 116, 139);
            lblSubtitle.Location = new Point(22, 42);
            lblSubtitle.AutoSize = true;
            this.Controls.Add(lblSubtitle);

            // 1. Machine ID
            Label lblMid = new Label();
            lblMid.Text = "1. Nhập Mã máy của khách hàng (THEX-XXXX-XXXX-XXXX):";
            lblMid.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblMid.ForeColor = Color.FromArgb(30, 41, 59);
            lblMid.Location = new Point(22, 75);
            lblMid.AutoSize = true;
            this.Controls.Add(lblMid);

            _txtMachineId = new TextBox();
            _txtMachineId.Location = new Point(22, 100);
            _txtMachineId.Size = new Size(530, 28);
            _txtMachineId.Font = new Font("Consolas", 11.5F, FontStyle.Bold);
            _txtMachineId.ForeColor = Color.FromArgb(15, 23, 42);
            _txtMachineId.BackColor = Color.White;
            this.Controls.Add(_txtMachineId);

            // 2. Expiry Date & Presets
            Label lblDate = new Label();
            lblDate.Text = "2. Chọn thời hạn bản quyền cấp cho khách:";
            lblDate.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblDate.ForeColor = Color.FromArgb(30, 41, 59);
            lblDate.Location = new Point(22, 140);
            lblDate.AutoSize = true;
            this.Controls.Add(lblDate);

            _dtpExpire = new DateTimePicker();
            _dtpExpire.Format = DateTimePickerFormat.Custom;
            _dtpExpire.CustomFormat = "dd/MM/yyyy";
            _dtpExpire.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _dtpExpire.Location = new Point(22, 166);
            _dtpExpire.Size = new Size(130, 28);

            int defaultYear = DateTime.Today.Month >= 6 ? DateTime.Today.Year + 1 : DateTime.Today.Year;
            _dtpExpire.Value = new DateTime(defaultYear, 5, 31);
            this.Controls.Add(_dtpExpire);

            // Preset Buttons
            Button btn1Month = CreatePresetButton("+1 Tháng", 160, 165, 70, () => _dtpExpire.Value = DateTime.Today.AddMonths(1));
            Button btnHk1 = CreatePresetButton("HK1 (31/12)", 235, 165, 90, () => _dtpExpire.Value = new DateTime(DateTime.Today.Year, 12, 31));
            Button btnSchoolYear = CreatePresetButton("Hết năm học (31/5)", 330, 165, 125, () => {
                int y = DateTime.Today.Month >= 6 ? DateTime.Today.Year + 1 : DateTime.Today.Year;
                _dtpExpire.Value = new DateTime(y, 5, 31);
            });
            Button btn1Year = CreatePresetButton("+1 Năm", 460, 165, 62, () => _dtpExpire.Value = DateTime.Today.AddYears(1));
            Button btnForever = CreatePresetButton("2099", 527, 165, 50, () => _dtpExpire.Value = new DateTime(2099, 12, 31));

            this.Controls.Add(btn1Month);
            this.Controls.Add(btnHk1);
            this.Controls.Add(btnSchoolYear);
            this.Controls.Add(btn1Year);
            this.Controls.Add(btnForever);

            // 3. Generate Button
            _btnGenerate = new Button();
            _btnGenerate.Text = "🔑 TẠO MÃ KÍCH HOẠT CHO KHÁCH";
            _btnGenerate.Location = new Point(22, 212);
            _btnGenerate.Size = new Size(530, 44);
            _btnGenerate.BackColor = Color.FromArgb(79, 70, 229); // Indigo 600
            _btnGenerate.ForeColor = Color.White;
            _btnGenerate.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            _btnGenerate.FlatStyle = FlatStyle.Flat;
            _btnGenerate.FlatAppearance.BorderSize = 0;
            _btnGenerate.Cursor = Cursors.Hand;
            _btnGenerate.Click += (s, e) => {
                string mid = _txtMachineId.Text.Trim();
                if (string.IsNullOrEmpty(mid)) {
                    MessageBox.Show("Vui lòng nhập Mã máy của khách hàng (dạng THEX-XXXX-XXXX-XXXX)!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtMachineId.Focus();
                    return;
                }
                DateTime exp = _dtpExpire.Value.Date;
                string key = GenerateKey(mid, exp);
                _txtKey.Text = key;
                _lblExpiryInfo.Text = string.Format("✓ Đã tạo thành công! Thời hạn bản quyền đến hết ngày: {0:dd/MM/yyyy}", exp);
                _lblExpiryInfo.ForeColor = Color.FromArgb(22, 101, 52); // Dark Green
            };
            this.Controls.Add(_btnGenerate);

            // 4. Result Key Display
            Label lblKey = new Label();
            lblKey.Text = "3. Mã kích hoạt cấp cho khách hàng (License Key):";
            lblKey.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            lblKey.ForeColor = Color.FromArgb(30, 41, 59);
            lblKey.Location = new Point(22, 270);
            lblKey.AutoSize = true;
            this.Controls.Add(lblKey);

            _txtKey = new TextBox();
            _txtKey.Location = new Point(22, 296);
            _txtKey.Size = new Size(385, 28);
            _txtKey.ReadOnly = true;
            _txtKey.Font = new Font("Consolas", 11.5F, FontStyle.Bold);
            _txtKey.ForeColor = Color.FromArgb(22, 101, 52);
            _txtKey.BackColor = Color.FromArgb(254, 252, 232); // Pale yellow
            this.Controls.Add(_txtKey);

            _btnCopy = new Button();
            _btnCopy.Text = "📋 Sao chép Key";
            _btnCopy.Location = new Point(415, 294);
            _btnCopy.Size = new Size(137, 34);
            _btnCopy.BackColor = Color.FromArgb(22, 163, 74); // Green 600
            _btnCopy.ForeColor = Color.White;
            _btnCopy.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
            _btnCopy.FlatStyle = FlatStyle.Flat;
            _btnCopy.FlatAppearance.BorderSize = 0;
            _btnCopy.Cursor = Cursors.Hand;
            _btnCopy.Click += (s, e) => {
                if (!string.IsNullOrEmpty(_txtKey.Text)) {
                    DateTime exp = _dtpExpire.Value.Date;
                    string zaloMsg = string.Format(
                        "Kính gửi Quý Thầy/Cô,\n\n" +
                        "Dưới đây là Mã kích hoạt bản quyền phần mềm TH-Exam Studio:\n" +
                        "{0}\n\n" +
                        "• Thời hạn bản quyền: Đến hết ngày {1:dd/MM/yyyy}\n" +
                        "• Hướng dẫn: Mở phần mềm TH-Exam Studio -> Bấm mục [Bản Quyền] -> Dán mã trên và bấm [Kích hoạt].\n\n" +
                        "Chúc Thầy/Cô công tác tốt!",
                        _txtKey.Text, exp);

                    Clipboard.SetText(zaloMsg);
                    MessageBox.Show("Đã sao chép nội dung kích hoạt!\nBạn có thể mở Zalo dán (Ctrl+V) gửi cho giáo viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            this.Controls.Add(_btnCopy);

            // 5. Expiry info label
            _lblExpiryInfo = new Label();
            _lblExpiryInfo.Text = "Thời hạn: Chọn ngày và bấm 'TẠO MÃ KÍCH HOẠT'";
            _lblExpiryInfo.Font = new Font("Segoe UI", 9.5F, FontStyle.Italic);
            _lblExpiryInfo.ForeColor = Color.Gray;
            _lblExpiryInfo.Location = new Point(22, 335);
            _lblExpiryInfo.Size = new Size(530, 24);
            this.Controls.Add(_lblExpiryInfo);

            // 6. Security Note
            Label lblNote = new Label();
            lblNote.Text = "Ghi chú bảo mật: Mã kích hoạt được gắn chặt theo Mã máy và thời hạn đã chọn.\nKhách hàng dán mã này vào cửa sổ [Bản Quyền] trong TH-Exam Studio để mở khóa đầy đủ tính năng.";
            lblNote.ForeColor = Color.FromArgb(100, 116, 139);
            lblNote.Font = new Font("Segoe UI", 8.5F);
            lblNote.Location = new Point(22, 368);
            lblNote.Size = new Size(530, 45);
            this.Controls.Add(lblNote);
        }

        private Button CreatePresetButton(string text, int x, int y, int w, Action onClick)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.Location = new Point(x, y);
            btn.Size = new Size(w, 29);
            btn.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            btn.BackColor = Color.FromArgb(241, 245, 249);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            btn.Cursor = Cursors.Hand;
            btn.Click += (s, e) => onClick();
            return btn;
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
    }
}
