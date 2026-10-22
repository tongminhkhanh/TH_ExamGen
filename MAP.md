# [TH_EXAMGEN] - Bản Đồ Điều Hướng Dự Án (Project Map)

Tệp này cung cấp cấu trúc thư mục thực tế của dự án `TH_ExamGen` cùng vai trò cụ thể của từng tệp và thư mục.

```
TH_ExamGen/
├── .gitignore                                  # Cấu hình bỏ qua các tệp tạm, secrets và build artifacts
├── .env.example                                # Bản mẫu biến môi trường cấu hình (Render / CSDL)
├── GEMINI.md                                   # Chỉ dẫn cốt lõi và nguyên tắc hành vi của Gemini cho dự án
├── MAP.md                                      # Bản đồ điều hướng toàn bộ cấu trúc dự án (tệp này)
├── SPEC.md                                     # Đặc tả yêu cầu kỹ thuật, nghiệp vụ và tiêu chuẩn chấp thuận
├── PLAN.md                                     # Kế hoạch hành động, roadmap và danh sách việc cần làm
├── README.md                                   # Hướng dẫn cài đặt và chạy ứng dụng
├── Dockerfile                                  # Dockerfile đóng gói WebAPI chạy trên Render
├── render.yaml                                 # Cấu hình triển khai dịch vụ Web Service lên Render
├── TH_ExamGen.sln                              # Solution Visual Studio kết nối 4 project .NET
│
├── context/                                    # Thư mục lưu trữ ngữ cảnh kiến trúc & quyết định kỹ thuật
│   └── decisions.md                            # Nhật ký quyết định kiến trúc (ADR)
│
├── deliverables/                               # Thư mục lưu trữ sản phẩm đầu ra hoàn thiện
│
├── src/                                        # Mã nguồn các project .NET 8.0
│   ├── TH_ExamGen.Core/                        # Thư viện lõi chứa models, cấu hình và ngân hàng chương trình
│   │   ├── Config/                             # Cấu hình ứng dụng (AppSettings.cs)
│   │   ├── Curriculum/                         # Dữ liệu phân phối chương trình GDPT 2018 (Toán & Tiếng Việt)
│   │   ├── MathModels/                         # Mô hình dữ liệu đề thi, ma trận và câu hỏi môn Toán
│   │   └── VietModels/                         # Mô hình dữ liệu đề thi, ngữ liệu đọc hiểu môn Tiếng Việt
│   │
│   ├── TH_ExamGen.Services/                    # Tầng dịch vụ nghiệp vụ và tích hợp AI
│   │   ├── DatabaseService.cs                  # Kết nối và khởi tạo CSDL PostgreSQL (Supabase)
│   │   ├── GeminiService.cs                    # Giao tiếp API Google Gemini & DeepSeek, retry, parsing
│   │   ├── PromptBuilderService.cs             # Xây dựng prompt sinh đề thi và ma trận chuẩn sư phạm
│   │   ├── LicenseAuthService.cs               # Quản lý tài khoản, mã kích hoạt, hạn mức và phân quyền kỳ
│   │   ├── WordExportMathService.cs            # Kết xuất đề thi và ma trận Toán sang tệp Word (.docx)
│   │   ├── WordExportVietService.cs            # Kết xuất đề thi và ma trận Tiếng Việt sang tệp Word (.docx)
│   │   └── TikzSpecialistService.cs            # Xử lý hình vẽ hình học Toán bằng mã TikZ / SVG
│   │
│   ├── TH_ExamGen.WebAPI/                      # Web API ASP.NET Core Minimal API và Frontend Web
│   │   ├── Program.cs                          # Khởi tạo WebAPI, định tuyến endpoints AI, Auth, Export
│   │   └── wwwroot/                            # Tài nguyên tĩnh giao diện web
│   │       ├── index.html                      # Giao diện chính của ứng dụng web
│   │       ├── app.js                          # Toàn bộ logic frontend tương tác WebAPI, lưu localStorage
│   │       ├── styles.css                      # Phong cách giao diện ứng dụng web
│   │       └── admin.html                      # Trang quản trị tạo mã bản quyền, quản lý tài khoản người dùng
│   │
│   └── TH_ExamGen.UI/                          # Ứng dụng Desktop WPF (.NET 8.0 Windows)
│       ├── App.xaml / App.xaml.cs              # Điểm khởi chạy ứng dụng WPF
│       ├── MainWindow.xaml / .cs               # Cửa sổ chính của phần mềm desktop
│       └── Views/                              # Các màn hình điều khiển môn Toán, Tiếng Việt, Cài đặt
│
└── tools/                                      # Công cụ hỗ trợ
    └── TikzOffline/                            # Công cụ hỗ trợ render hình vẽ Toán offline bằng MiKTeX
```
