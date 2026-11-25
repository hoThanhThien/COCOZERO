# Kịch bản Video Demo Sản phẩm (Tối đa 3 phút — MoMo Tech Contest 2026)

> Chủ đề cuộc thi: **Imagine the Future Workspace (Human Centered. AI Powered)**
> Định vị sản phẩm: **AI Financial Assistant & Anti-Fraud Shield** chuyên biệt cho nông hộ trồng dừa và Hợp tác xã vùng Bến Tre – tỉnh Vĩnh Long (nền tảng bảo mật Firebase Auth, RBAC, ownership, CORS và rate limiting làm lớp bảo vệ sản phẩm).

- **0:00 – 0:25 | Mở đầu & Câu chuyện Nông hộ**: Giới thiệu tình huống thực tế: một nông hộ trồng dừa nhận được lời mời tham gia dự án carbon hứa hẹn lợi nhuận cố định và yêu cầu đóng phí trước. CocoCarbon AI giúp nông hộ **phát hiện dấu hiệu lừa đảo trước khi mất tiền** và **quản lý thu chi/ROI thực tế** dựa trên 2 trụ cột AI cốt lõi:
  1. **Trợ lý Tài chính AI cho nông hộ & HTX** (Gemini Tool Calling + Python Financial Engine + Sổ Thu Chi thực tế).
  2. **Chống lừa đảo đầu tư carbon & greenwashing** (kiểm tra đề nghị đầu tư, bóc tách bằng chứng PDF/ảnh, tách biệt 3 điểm).
- **0:25 – 0:55 | Đăng nhập an toàn & Chống lừa đảo trước khi ký (`/login` → `/documents`)**: Đăng nhập qua Firebase Auth (Google Sign-In hoặc tài khoản Email dùng thử trên bản Production). Vào `/documents`, dán thử đề nghị *"cam kết lợi nhuận 80 triệu/ha/năm, hấp thụ 75 tCO2/ha/năm, chuyển trước 5 triệu hôm nay"* → `POST /api/carbon/proposal-check` chấm rủi ro cao, gắn khuyến nghị **`DO_NOT_SIGN_OR_PAY`**, bóc tách cờ `FRAUD_UPFRONT_FEE_DEMAND`, `FRAUD_GUARANTEED_CARBON_PROFIT`, kèm 3 chỉ số tách biệt (`Project Readiness`, `Document Completeness`, `Carbon Integrity Risk` — thiếu giấy tờ không bị quy kết là gian lận).
- **0:55 – 1:45 | Trợ lý Quản lý Tài chính Vườn dừa (`/financial-assistant`)**:
  1. Nhập câu tự nhiên *"Hôm nay chi 3.5 triệu mua phân hữu cơ mụn dừa cho vườn"* → AI phân loại (`CareExpense`, `3,500,000 đ`) và yêu cầu **người dùng bấm xác nhận** trước khi lưu vào Sổ Thu Chi Vườn (`FarmFinancialEntries`).
  2. Bấm phân tích gắn với vườn thực tế (`?farmId=...`): Python Financial Engine tự tổng hợp sổ theo **Tháng/Quý/Năm**, đối chiếu **Chi thực tế vs Ngân sách**, phát cảnh báo vượt ngân sách/dòng tiền âm nếu có, và **tính lại ROI/NPV/Dòng tiền hiệu chỉnh** kèm bảng so sánh kịch bản & báo cáo 9 phần.
- **1:45 – 2:25 | Calculator Khoa học CTU 2015 & AI Audit PDF (`/calculator` → `/farms/{id}`)**: Chạy Calculator tại C# API (`1.0 ha Dừa Ta 10 năm → 75.2436 tCO2 tương đương tham khảo`), thử nhập tuổi `7` để chứng minh hệ thống từ chối nội suy (`HTTP 422`); xem quy trình upload PDF kiểm tra magic bytes + SHA-256 + chồng lấn GPS cho Reviewer duyệt.
- **2:25 – 2:55 | HTX, Danh mục Dự án, RFQ & Android Hiện trường (`/cooperative`, `/projects`, Android APK)**: Tổng hợp diện tích vùng nguyên liệu theo ủy quyền HTX, xem danh mục dự án công khai minh bạch và ứng dụng Android Kotlin Compose thu thập dữ liệu tại vườn.
- **2:55 – 3:00 | Kết thúc**: Tóm tắt thông điệp *Bảo vệ nông hộ trước cạm bẫy tín chỉ carbon giả – Đồng hành quản lý dòng tiền bền vững từ nông sản & phụ phẩm dừa*.
