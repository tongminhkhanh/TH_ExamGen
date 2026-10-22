# Nhật Ký Quyết Định Kiến Trúc (Architecture Decision Records - ADR)

Tài liệu này ghi lại các quyết định thiết kế và kiến trúc quan trọng của hệ thống `TH_ExamGen`.

---

## ADR-001: Quản lý Gemini API Key riêng biệt trên trình duyệt người dùng (Client-Side Storage)
- **Ngày:** 2026-10-20
- **Trạng thái:** Đã áp dụng
- **Bối cảnh:** Trước đây máy chủ Render lưu một biến môi trường `GEMINI_API_KEY` dùng chung. Điều này dẫn đến việc người dùng chưa có tài khoản hoặc chưa nhập key vẫn gọi AI qua key của server, gây tốn kém chi phí và dễ bị cạn hạn mức (quota) cho toàn hệ thống.
- **Quyết định:**
  1. Gỡ bỏ hoàn toàn `GEMINI_API_KEY` khỏi biến môi trường máy chủ Render.
  2. Mỗi giáo viên tự tạo khóa miễn phí trên Google AI Studio và nhập vào mục "⚙ Cấu hình".
  3. Khóa được lưu trữ an toàn trong `localStorage` trên trình duyệt của giáo viên (`thexam_user_gemini_key`) và gửi kèm payload `settings.geminiApiKey` trong mỗi request gọi AI.
  4. Backend kiểm tra: nếu request không có `GeminiApiKey`, lập tức trả về lỗi hướng dẫn người dùng nhập key.

---

## ADR-002: Khóa đợt kiểm tra theo Gói bản quyền (Period Locking)
- **Ngày:** 2026-10-20
- **Trạng thái:** Đã áp dụng
- **Bối cảnh:** Cần phân chia gói sản phẩm theo học kỳ để giảm giá thành cho giáo viên chỉ có nhu cầu kiểm tra theo kỳ thay vì cả năm. Cần lựa chọn giữa khóa theo ngày tháng lịch thực tế hay khóa theo đợt thi.
- **Quyết định:**
  1. **Không khóa theo ngày tháng lịch:** Vì giáo viên thường phải ra đề thi trước từ 2-3 tuần để nộp tổ chuyên môn duyệt, hoặc lịch thi tại các địa phương có thể xê dịch. Khóa theo ngày tháng sẽ gây ức chế cho giáo viên.
  2. **Khóa theo Đợt kiểm tra chuẩn GDPT 2018:**
     - `Term1`: Chỉ cho phép tạo đề *Giữa Học kỳ I* và *Cuối Học kỳ I*.
     - `Term2` / `Term`: Chỉ cho phép tạo đề *Giữa Học kỳ II* và *Cuối Học kỳ II*.
     - `Year` / `School` / `Trial`: Mở khóa toàn bộ cả 4 đợt kiểm tra trong năm học.
  3. Áp dụng kiểm soát đồng bộ cả ở Bước 1 (Gợi ý, Rà soát, Tải Word Ma trận) và Bước 3 (Sinh đề thi bằng AI).

---

## ADR-003: Bảo vệ 2 lớp (Dual-Layer Enforcement)
- **Ngày:** 2026-10-20
- **Trạng thái:** Đã áp dụng
- **Bối cảnh:** Nếu chỉ kiểm tra ở frontend, người dùng am hiểu kỹ thuật có thể gọi API trực tiếp. Nếu chỉ kiểm tra ở backend, trải nghiệm người dùng kém (bấm nút mới báo lỗi).
- **Quyết định:**
  - **Tầng 1 (Frontend `app.js`):** Tự động khóa `disabled` các đợt thi ngoài gói, thêm biểu tượng `🔒`, tự động chuyển về đợt thi hợp lệ; kiểm tra token đăng nhập và API key trước khi gửi request.
  - **Tầng 2 (Backend `Program.cs` + `LicenseAuthService.cs`):** Kiểm tra `X-Session-Token`, hạn mức gói, đợt kiểm tra `IsPeriodAllowedForPackage` và `GeminiApiKey` ngay tại controller/minimal API; trả về mã lỗi HTTP chuẩn (401 Unauthorized, 400 Bad Request) nếu không hợp lệ.

---

## ADR-004: Bảo mật Quản trị viên (Admin PIN) & Dữ liệu Người dùng
- **Ngày:** 2026-10-20
- **Trạng thái:** Đã áp dụng
- **Bối cảnh:** Trang `admin.html` trước đây hiển thị mã PIN gợi ý `(Mặc định: 888888)` và API danh sách người dùng trả về cả chuỗi băm mật khẩu.
- **Quyết định:**
  1. Xóa hoàn toàn gợi ý mã PIN mặc định trên giao diện `admin.html`.
  2. Cấu hình `ADMIN_PIN` riêng biệt trong biến môi trường bí mật trên Render (`srv-dbau5am7bikc73b0br70`).
  3. Trong API `AdminGetAllUsersAsync`, làm sạch chuỗi băm mật khẩu (`u.PasswordHash = ""`) trước khi trả về frontend.

---

## ADR-005: Cơ chế Trừ lượt An toàn (Safe Quota Deduction) & Chống Lạm dụng
- **Ngày:** 2026-10-18
- **Trạng thái:** Đã áp dụng
- **Bối cảnh:** Người dùng tạo đề có thể gặp lỗi mạng hoặc AI phản hồi chậm, nếu trừ lượt trước khi AI sinh xong sẽ gây mất quyền lợi người dùng.
- **Quyết định:**
  1. **Chỉ trừ lượt khi AI đã sinh đề thành công 100%:** Hàm `CommitSuccessfulExamAsync` chỉ được gọi sau khi `GeminiService` đã parse và validate đề thi hợp lệ.
  2. **Cooldown 60 giây:** Bắt buộc chờ 60 giây giữa 2 lần bấm sinh đề liên tiếp để giáo viên xem xét đề và bảo vệ tài nguyên AI.
  3. **Giới hạn số đề theo ngày (Daily Quota Limit):** Ngăn chặn việc một tài khoản bị chia sẻ cho nhiều giáo viên cùng dùng chung trong một ngày.
