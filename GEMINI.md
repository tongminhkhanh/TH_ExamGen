# [TH_EXAMGEN] - Gemini Project Instructions & Context

## 1. Tổng quan Dự án (Project Overview)
**TH_ExamGen** là hệ thống sinh đề thi và ma trận kiểm tra định kỳ tiểu học tự động chuẩn GDPT 2018 (Bộ sách Kết nối tri thức với cuộc sống), bám sát Thông tư 27/2020/TT-BGDĐT cho 2 môn:
- **Tiếng Việt** (Lớp 4 & 5): Đọc hiểu văn bản (ngữ liệu bám chủ điểm hoặc tự nhập), trắc nghiệm & tự luận, bài tập chính tả, luyện từ và câu, hướng dẫn chấm chi tiết.
- **Toán** (Lớp 4 & 5): Số & phép tính, Hình học & đo lường, Một số yếu tố thống kê & xác suất; tự động dựng hình vẽ trực quan bằng TikZ / SVG.

### Đối tượng sử dụng
- Giáo viên tiểu học lớp 4, lớp 5 trên toàn quốc.
- Tổ chuyên môn và Ban Giám hiệu các trường tiểu học.

### Kiến trúc Kỹ thuật
- **Backend WebAPI:** ASP.NET Core 8.0 Minimal APIs (`src/TH_ExamGen.WebAPI`).
- **Frontend Web:** Vanilla HTML5, CSS3, JavaScript ES6 (`src/TH_ExamGen.WebAPI/wwwroot`), deploy trên Render tại `rade.thtohieu.com`.
- **Desktop Application:** WPF (.NET 8.0, XAML) xuất bản file Word `.docx` tích hợp MiKTeX/TikZ offline (`src/TH_ExamGen.UI`).
- **Dịch vụ cốt lõi & Nghiệp vụ:** `src/TH_ExamGen.Core` và `src/TH_ExamGen.Services`.
- **Cơ sở dữ liệu:** Supabase PostgreSQL (AWS Tokyo pooler connection).
- **AI Engine:** Google Gemini API (`gemini-2.5-flash`, `gemini-3.8-flash`) & DeepSeek API (`deepseek-chat`).

---

## 2. Quy tắc Điều hướng Bắt buộc (Navigation Directive)
> **BẮT BUỘC:** Mỗi khi bắt đầu một phiên làm việc mới hoặc tiếp nhận một nhiệm vụ mới, Gemini **PHẢI** đọc tệp [MAP.md](file:///c:/Users/Administrator/Desktop/D/MAP.md) trước để định vị chính xác vị trí các module, tệp nguồn và tài liệu liên quan.

---

## 3. Quy tắc Ghi chép & Lưu trữ (Documentation & Context)
- Mọi quyết định kiến trúc quan trọng (Architecture Decision Records - ADR), quy chuẩn thiết kế, hoặc thay đổi logic hệ thống **PHẢI** được ghi nhận và cập nhật vào [context/decisions.md](file:///c:/Users/Administrator/Desktop/D/context/decisions.md).
- Tất cả sản phẩm xuất bản, tài liệu bàn giao, tài liệu kiểm thử cuối cùng được lưu vào thư mục `deliverables/`.

---

## 4. Kỷ luật Phạm vi & Quy chuẩn Phát triển (Scope Discipline & Standards)
- **Prime Directive (Rule 0):** Thực hiện **chính xác** những gì người dùng yêu cầu trong phạm vi được giao. Tuyệt đối không tự ý refactor ngoài phạm vi, không thêm tính năng phỏng đoán, không sửa "tiện tay".
- **Không Model-Credit Trailers (Rule 5 & B4):** Tuyệt đối không chèn bất kỳ dòng ghi công mô hình nào (`Co-Authored-By:`, `Claude-Session:`, URL session, hoặc `🤖 Generated with...`) vào commit message, tag hoặc PR. Lịch sử commit chỉ ghi nhận trách nhiệm của con người.
- **Bảo mật API Key & Dữ liệu:**
  - Khóa Google Gemini API Key là khóa riêng của từng giáo viên, được lưu trữ trên `localStorage` trình duyệt của giáo viên, gửi qua payload request khi sinh đề. Máy chủ không lưu khóa cố định trong biến môi trường.
  - Mã PIN Admin Panel được bảo vệ qua biến môi trường `ADMIN_PIN`. Mật khẩu người dùng băm SHA-256 kèm salt bí mật.
- **Native Logic Flow:** Ưu tiên luồng xử lý tự nhiên của ASP.NET Core và Vanilla JS, code tường minh, dễ đọc, không lạm dụng abstraction hoặc wrapper dư thừa.
