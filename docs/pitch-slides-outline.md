# Dàn ý Slide Thuyết trình CocoCarbon AI (10 Trang — MoMo Tech Contest 2026)

## Slide 1: Tên sản phẩm & Định vị (*Human Centered • AI Powered • Science First*)
- **CocoCarbon AI** — *Từ vườn dừa đến giá trị xanh (From Coconut Farms to Verifiable Value)*
- Thuộc 2 chủ đề MoMo Tech Contest 2026: **Trợ lý AI (Tài chính Nông hộ & HTX)** + **Chống lừa đảo (Đầu tư Carbon & Greenwashing)**.
- Địa bàn khởi điểm: Vùng dừa Bến Tre — Tỉnh Vĩnh Long (bảo mật nền tảng với Firebase Auth, RBAC, IDOR, Rate Limit).

## Slide 2: Bài toán thực tế — Hai rủi ro kép của Nông hộ Vườn dừa
- **Rủi ro mất tiền do lừa đảo dự án carbon**: Môi giới thổi phồng "hấp thụ tCO2/ha/năm cố định", "lợi nhuận chắc chắn" hoặc đòi đóng phí/đặt cọc trước khi ký hợp đồng.
- **Khó khăn quản lý tài chính vườn**: Nông hộ ít theo dõi chi phí phân bón, nhân công, MRV vs doanh thu nông sản/phụ phẩm (gáo, xơ, mụn dừa) theo tháng/quý/năm để biết có thực sự có lãi trước khi mở rộng.

## Slide 3: Câu chuyện Sản phẩm — Trợ lý AI hiểu nhu cầu & hành động thay người dùng
- Sản phẩm giúp cuộc sống nông hộ & HTX trở nên **tiện lợi, an toàn và thoải mái hơn** qua 5 tác vụ Trợ lý hành động (từ cốt lõi Tài chính Xanh & Anti-Fraud):
  - **Trợ lý Tài chính**: đồng hành quản lý thu chi thực tế, ROI/NPV, dòng tiền & báo cáo 9 phần.
  - **Trợ lý Thanh toán & Giao dịch**: phân tích câu tự nhiên, chuẩn bị lệnh ghi Sổ Thu Chi (1 chạm xác nhận) & chặn phí trước đáng ngờ.
  - **Trợ lý Mua sắm**: ước tính ngân sách vật tư (giống, phân, tưới, MRV) & kiểm tra an toàn gói hợp tác carbon.
  - **Trợ lý Bán hàng**: tư vấn phụ phẩm gáo/xơ/mụn dừa & tự soạn thư phản hồi RFQ minh bạch cho Buyer.
  - **Trợ lý Cá nhân**: tổng hợp checklist hồ sơ còn thiếu, nhắc việc và tiến độ MRV hằng tháng.

## Slide 4: Kiến trúc Đa Mô hình AI (Admin API Key) & Gemini Tool Calling
- **Mở rộng linh hoạt cho Admin (`/admin`)**: Hỗ trợ thêm và chuyển đổi API Key ngay trên giao diện Admin cho **Google Gemini (`gemini-4-hydrogen`), OpenAI GPT, OpenRouter, Groq, Anthropic Claude hoặc Custom endpoint** — bảo mật key ở phía server, hỗ trợ kiểm tra kết nối trực tiếp & tự động fallback khi thiếu key.
- **Không để AI tự tính nhẩm**: Toàn bộ phép tính ROI/NPV/Dòng tiền thực tế đều thực thi qua Python Deterministic Tool Calling (`calculate_financial_metrics`, `parse_natural_language_transaction`, `generate_sales_rfq_draft`, `build_personal_action_checklist`).

## Slide 5: Hệ thống Chống Lừa đảo Đề nghị Đầu tư & Hồ sơ Carbon
- **Kiểm tra Đề nghị Đầu tư (`/api/carbon/proposal-check`)**: Bắt yêu cầu đóng phí trước (`FRAUD_UPFRONT_FEE_DEMAND`), cam kết doanh thu chắc chắn, thổi phồng `tCO2/ha/năm`, áp lực ký gấp (`DO_NOT_SIGN_OR_PAY`).
- **Tách biệt công bằng 3 chỉ số**: `Project Readiness Score`, `Document Completeness Score`, `Carbon Integrity Risk Score` — **thiếu hồ sơ KHÔNG bị đánh đồng là gian lận** (`IncompleteDocuments_NotFraud`), luôn qua Reviewer duyệt.

## Slide 6: Nền tảng Khoa học Carbon & RAG 5 Nhóm Nguồn Kiểm chứng
- 4 hệ số CTU 2015 `[R1]` (24.5180 / 75.2436 / 20.4583 / 69.9189 tCO2/ha); từ chối nội suy tuổi (`HTTP 422`), không gán giá tín chỉ khi thiếu căn cứ.
- Kho RAG **10 nguồn / 5 nhóm** (`coconut_research`, `agri_operations`, `financial_management`, `carbon_regulation`, `carbon_mrv`: `R1–R12`, `AGRI1`, `GF1`) được kiểm tra cấu trúc ngay lúc khởi động FastAPI & CI.

## Slide 7: Kiến trúc Kỹ thuật & Bảo mật Sản phẩm
- Web (Next.js 15 + Firebase Google/Email Auth baked via Docker build args) + Android Native (Kotlin/Compose).
- **C# ASP.NET Core 8** làm nguồn sự thật nghiệp vụ (`VerifyIdTokenAsync`, RBAC, IDOR check, Calculator `decimal`, HMAC Signed URL).
- **Python FastAPI** chạy nội bộ qua `X-Internal-Api-Key`; Docker Compose + Nginx TLS reverse proxy.

## Slide 8: Minh chứng Kiểm thử Đa tầng & Chất lượng Thực thi
- CI tự động `.github/workflows/ci.yml` + kiểm thử cục bộ đầy đủ các tầng:
  - Python `pytest`: Unit & API (Financial Engine, RAG JSON validation, Gemini Tool Calling mock/fallback, Proposal & Integrity 3-score).
  - C# `dotnet test`: Unit CTU 2015 Calculator + Integration Security/IDOR/E2E.
  - Web: Vitest guardrails + Next.js production build.
  - Android: Unit test & Kotlin source.

## Slide 9: Tiềm năng Tác động & Tính Khả thi Triển khai
- Phục vụ trực tiếp nông hộ, HTX vùng dừa ĐBSCL (Vĩnh Long / Bến Tre) tăng khả năng kiểm soát chi phí thực tế, tận dụng phụ phẩm tuần hoàn (xơ/mụn/gáo) và phòng tránh lừa đảo xanh.
- Đóng gói gọn nhẹ trên VPS via Docker Compose + APK hiện trường, chi phí vận hành thấp.

## Slide 10: Lộ trình Nâng cấp Tiếp theo
- **Đã hoàn thành**: Core API, Financial Companion thực tế, Natural-Language Ledger, Proposal Fraud Warning, AI 3-Score Integrity, Docker & CI.
- **Giai đoạn sau cuộc thi**: Tích hợp OCR nâng cao cho hóa đơn vật tư nông nghiệp, chuẩn hóa hiệu chuẩn Tier 2/3 cùng viện nghiên cứu và kết nối registry chính thức theo Nghị định 29/2026/NĐ-CP.
