# Dàn ý Slide Thuyết trình CocoCarbon AI (10 Trang — MoMo Tech Contest 2026)

## Slide 1: Tên sản phẩm & Định vị (*Human Centered • AI Powered • Science First*)
- **CocoCarbon AI** — *Từ vườn dừa đến giá trị xanh (From Coconut Farms to Verifiable Value)*
- Thuộc 2 chủ đề MoMo Tech Contest 2026: **Trợ lý AI (Tài chính Nông hộ & HTX)** + **Chống lừa đảo (Đầu tư Carbon & Greenwashing)**.
- Địa bàn khởi điểm: Vùng dừa Bến Tre — Tỉnh Vĩnh Long (bảo mật nền tảng với Firebase Auth, RBAC, IDOR, Rate Limit).

## Slide 2: Bài toán thực tế — Hai rủi ro kép của Nông hộ Vườn dừa
- **Rủi ro mất tiền do lừa đảo dự án carbon**: Môi giới thổi phồng "hấp thụ tCO2/ha/năm cố định", "lợi nhuận chắc chắn" hoặc đòi đóng phí/đặt cọc trước khi ký hợp đồng.
- **Khó khăn quản lý tài chính vườn**: Nông hộ ít theo dõi chi phí phân bón, nhân công, MRV vs doanh thu nông sản/phụ phẩm (gáo, xơ, mụn dừa) theo tháng/quý/năm để biết có thực sự có lãi trước khi mở rộng.

## Slide 3: Câu chuyện Sản phẩm & Giải pháp Khép kín
- Khi nhận đề nghị đầu tư carbon: kiểm tra ngay dấu hiệu lừa đảo trước khi ký/chuyển tiền (`/documents`).
- Hằng tháng/vụ mùa: ghi thu chi bằng tiếng Việt/Anh tự nhiên (xác nhận trước khi lưu) và dùng **AI Financial Assistant** tính ROI/NPV/dòng tiền dựa trên **dữ liệu thực tế + giả định minh bạch** (`/financial-assistant`).
- Luồng đầy đủ: Số hóa vườn → Sổ Thu Chi & Đối chiếu Ngân sách → Chống lừa đảo & AI Auditor → CTU 2015 Calculator → HTX & RFQ.

## Slide 4: Trợ lý Tài chính AI (Gemini 4 + Python Engine + Sổ Thu Chi Thực tế)
- **Không để AI tự tính nhẩm**: `GeminiInteractionsProvider` (`gemini-4-hydrogen` Interactions API, `tools`, `store=False`) gọi hàm Python deterministic (`financial_engine.py`), kèm fallback ngoại tuyến an toàn.
- **Gắn kết số liệu thật (`FarmFinancialEntries`)**: Tổng hợp **Tháng/Quý/Năm**, đối chiếu thực tế vs ngân sách, cảnh báo `OVER_BUDGET_OPEX` / `ACTUAL_NEGATIVE_CASHFLOW`, tính lại ROI hiệu chỉnh, mô phỏng kịch bản và xuất báo cáo 9 phần.

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
