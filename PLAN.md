# [TH_EXAMGEN] - Kế Hoạch Hành Động & Tiến Độ (Action Plan & Roadmap)

Tài liệu này theo dõi tiến độ các hạng mục đã hoàn thành và định hướng phát triển các giai đoạn tiếp theo.

---

## 1. Các Hạng Mục Đã Hoàn Thành (Completed Milestones)

- [x] **Kiến trúc cốt lõi:**
  - [x] Thiết lập Solution gồm 4 project .NET 8.0: `Core`, `Services`, `WebAPI`, `UI` (WPF).
  - [x] Thiết lập ngân hàng phân phối chương trình GDPT 2018 Kết nối tri thức cho Tiếng Việt và Toán (Lớp 4 & 5).
  - [x] Đóng gói Dockerfile và triển khai tự động lên Render qua `render.yaml`.
- [x] **Tích hợp Trí tuệ Nhân tạo (AI Engine):**
  - [x] Tích hợp Google Gemini (`gemini-2.5-flash`, `gemini-3.8-flash`) và DeepSeek API với cơ chế tự động thử lại (Retry) khi gặp 429/5xx.
  - [x] Xây dựng bộ prompt chuẩn sư phạm có kiểm soát độ dài ngữ liệu đọc hiểu và cấu trúc điểm.
  - [x] Hỗ trợ gợi ý và rà soát ma trận tự động bám sát chương trình.
- [x] **Cơ sở dữ liệu & Bản quyền:**
  - [x] Tích hợp Supabase PostgreSQL qua Transaction Pooler port 5432.
  - [x] Hệ thống kiểm soát hạn mức 5 tầng (Phiên đăng nhập, Cooldown 60s, Hạn dùng gói, Daily Cap, Trừ lượt sau khi sinh đề thành công).
  - [x] Phân chia gói bản quyền theo Học kỳ (`Term1`, `Term2`, `Year`, `School`) với mức giảm giá hơn 50%.
- [x] **Bảo mật & Trải nghiệm Người dùng:**
  - [x] Thiết lập mã PIN bảo mật Admin Panel (`ADMIN_PIN`) trên Render.
  - [x] Ẩn chuỗi băm mật khẩu khi hiển thị danh sách người dùng trong trang quản trị.
  - [x] Khóa toàn bộ tính năng AI và xuất Word ở Bước 1 và Bước 3 khi chưa đăng nhập.
  - [x] Gỡ bỏ hard key API mặc định của server; chuyển đổi sang lưu trữ khóa riêng biệt của từng giáo viên trên `localStorage` trình duyệt.

---

## 2. Kế Hoạch Sắp Tới (Upcoming Roadmap)

- [ ] **Giai đoạn 1: Nâng cao Trải nghiệm Người dùng & Kiểm định Chất lượng:**
  - [ ] Bổ sung bộ kiểm thử tự động (Unit Test / Integration Test) cho luồng kiểm tra bản quyền `LicenseAuthService` và phân quyền học kỳ `IsPeriodAllowedForPackage`.
  - [ ] Kiểm thử toàn diện thời gian phản hồi (latency) của Gemini API và cơ chế thông báo trạng thái trực quan hơn cho giáo viên.
- [ ] **Giai đoạn 2: Tối ưu Hóa Ngân hàng Đề & Hình vẽ Toán:**
  - [ ] Mở rộng thư viện mẫu hình vẽ Toán Lớp 5 (hình tròn, diện tích hình thang, hình hộp chữ nhật).
  - [ ] Tối ưu hóa file Word xuất bản để đảm bảo tương thích 100% khi mở trên cả Microsoft Word 2010 - 2024 và WPS Office.
- [ ] **Giai đoạn 3: Giám sát & Quản trị Vận hành:**
  - [ ] Xây dựng bảng theo dõi số lượng đề đã sinh theo ngày trên `admin.html`.
  - [ ] Bổ sung tính năng sao lưu tự động kho đề thi và xuất danh sách giáo viên ra file Excel.
