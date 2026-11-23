# Danh sách Thư viện Mã nguồn mở, Tài sản & Công cụ AI đã sử dụng

## 1. Thư viện Mã nguồn mở (Open-Source Libraries & Licenses)

### Backend C# (`backend/`)
- `.NET 8.0 / ASP.NET Core` (MIT)
- `Microsoft.EntityFrameworkCore` 8.0.11 / `Sqlite` / `Design` (MIT)
- `Npgsql.EntityFrameworkCore.PostgreSQL` & `NetTopologySuite` 8.0.11 (PostgreSQL / BSD-3-Clause)
- `FirebaseAdmin` 3.1.0 (Apache-2.0)
- `AWSSDK.S3` 3.7.410.7 (Apache-2.0)
- `Swashbuckle.AspNetCore` 6.9.0 (MIT)
- `xUnit` 2.9.2 (Apache-2.0)

### Python AI Service (`ai-service/`)
- `FastAPI` (MIT), `Uvicorn` (BSD-3-Clause), `Pydantic` / `pydantic-settings` (MIT)
- `PyMuPDF` (`pymupdf`) (AGPL-3.0 — lưu ý kiểm tra giấy phép thương mại trước khi phát hành sản phẩm thương mại đóng nguồn)
- `google-genai` >= 3.2.0 (Apache-2.0)
- `httpx` (BSD-3-Clause), `pytest` (MIT)

### Web Client (`web/`)
- `Next.js` 15.5.7 (MIT), `React` / `React DOM` 19 (MIT), `TypeScript` 5.x (Apache-2.0)
- `Tailwind CSS` 4.x (MIT), `lucide-react` (ISC)
- `firebase` JS SDK 11.x (Apache-2.0)
- `vitest` (MIT)

### Android Native (`android/`)
- `Kotlin` 2.0.21 (Apache-2.0), `Jetpack Compose` / `Material3` / `Navigation Compose` (Apache-2.0)
- `Retrofit` / `OkHttp` (Apache-2.0)
- `Firebase Auth` & `AndroidX Credential Manager` / `Google ID` (Apache-2.0)

## 2. Tài sản có sẵn & Công cụ AI
- **Dữ liệu tham khảo có sẵn**: Bộ 4 hệ số CTU 2015 [R1], thông tin khảo sát Bến Tre 2024 [R2], khung IPCC [R3][R4], Verra VM0042 [R5][R6], Gold Standard SOC [R7], Tanzania Allometric [R8], Nghị định 29/2026/NĐ-CP [R12]. Toàn bộ bản ghi mẫu trong DB gắn nhãn `DEMO`.
- **AI Model & Provider cấu hình**: `gemini-4-hydrogen` (qua `google-genai` Interactions API) với adapter switchable và fallback cục bộ `deterministic-regex-v1`.
- **Công cụ AI hỗ trợ phát triển**: Gemini / Antigravity coding agent để thiết kế kiến trúc, sinh mã nguồn, test và tài liệu theo đặc tả CocoCarbon AI.
