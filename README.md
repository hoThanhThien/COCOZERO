# CocoCarbon AI — Nền tảng Dữ liệu Carbon & Chuỗi Giá trị Dừa

> **Từ vườn dừa đến giá trị xanh — From Coconut Farms to Verifiable Value**
> Khu vực khởi điểm: **Vùng dừa Bến Tre — Tỉnh Vĩnh Long (theo địa giới hành chính hiện hành)**

CocoCarbon AI giúp nông hộ, hợp tác xã, đơn vị phát triển dự án và doanh nghiệp thu thập, chuẩn hóa, kiểm tra bất thường hồ sơ vườn dừa; thực hiện ước tính tham khảo theo nghiên cứu khoa học; tổng hợp vùng nguyên liệu theo ủy quyền; và tiếp nhận yêu cầu tìm hiểu (RFQ).

> [!IMPORTANT]
> **Ranh giới minh bạch của MVP**:
> - CocoCarbon AI **không** phải sàn giao dịch carbon được cấp phép, registry, đơn vị phát hành, tổ chức thẩm định (VVB) hay tổ chức chứng nhận.
> - Kết quả **Coconut Carbon Calculator** sử dụng đúng 4 hệ số CTU 2015 [R1] (`24.5180`, `75.2436`, `20.4583`, `69.9189` tCO2/ha), là **giá trị CO2 tương đương tham khảo theo sinh khối và cấp tuổi nghiên cứu**, **không phải** `tCO2/ha/năm`, **không phải** tín chỉ carbon đã phát hành và **không đảm bảo** doanh thu.
> - Dữ liệu trong database được gắn nhãn nguồn rõ ràng: `Demo`, `Synthetic`, `UserDeclared`, `AiExtracted`, `Verified`.

---

## 1. Kiến trúc & Công nghệ

| Thành phần | Thư mục | Công nghệ | Vai trò |
| :--- | :--- | :--- | :--- |
| **Core Backend** | `backend/` | C# ASP.NET Core 8, EF Core, PostgreSQL + PostGIS (SQLite local dev/test), Firebase Admin `.NET` | Nguồn sự thật nghiệp vụ: xác thực Firebase ID token, RBAC, ownership, Calculator CTU 2015, private storage + HMAC signed URL, Projects/RFQ, AuditLogs, proxy bảo mật tới AI Financial & Integrity Engine (`/api/ai/assistant`, `/api/financial/*`, `/api/carbon/integrity-check`) |
| **Internal AI Service** | `ai-service/` | Python FastAPI, `google-genai` (`gemini-4-hydrogen` Interactions API), PyMuPDF, Python Financial Engine, 5-Category Curated RAG | AI Financial Assistant (Intent + Context + Tool Calling), Python Financial Engine (Cost/Revenue/Profit/ROI/CashFlow/Scenarios/9-part Report), Carbon Integrity 3-score checker & PDF/image anomaly extractor |
| **Web Client** | `web/` | Next.js 15.5.7, React 19, TypeScript, Tailwind CSS, Firebase JS SDK | Giao diện Green Fintech + AgriTech: Dashboard, Farm Registry, Calculator, Financial Assistant (`/financial-assistant`), Document Center + Integrity 3-Score Checker, AI Audit, HTX, Directory/RFQ, Admin |
| **Android App** | `android/` | Kotlin, Jetpack Compose, MVVM, Retrofit/OkHttp, Credential Manager + Firebase Auth | Ứng dụng hiện trường cho nông hộ: GPS, Camera/Gallery upload, Calculator (gọi C# API), AI result |
| **Infrastructure** | `infra/`, `docker-compose.yml` | Docker Compose, Nginx reverse proxy, PostgreSQL/PostGIS, MinIO (S3) | Triển khai Web trước; `postgres` và `ai-service` chỉ chạy ở mạng nội bộ |

---

## 2. Chạy thử nghiệm cục bộ (Local Quickstart)

### Bước 1: Chuẩn bị biến môi trường
```bash
cp .env.example .env
```
*(Điền `GEMINI_API_KEY` nếu muốn gọi Gemini trực tiếp; nếu để trống, AI Service vẫn chạy chế độ `heuristic_fallback` và đánh dấu `isFallback: true`.)*

### Bước 2: Chạy bằng Docker Compose (PostgreSQL + PostGIS + AI + API + Web + Nginx)
```bash
docker compose up -d --build
```
Truy cập Web qua Nginx tại `http://localhost` và Swagger API tại `http://localhost/swagger`.

### Bước 3: Chạy từng dịch vụ trực tiếp (Development Mode)
1. **Python AI Service**:
   ```bash
   cd ai-service
   uv sync --extra dev
   uv run uvicorn app.main:app --host 127.0.0.1 --port 8000
   ```
2. **C# ASP.NET Core API** (dùng `appsettings.Development.json` + SQLite & demo mode mặc định):
   ```bash
   cd backend
   dotnet run --project src/CocoCarbon.Api
   ```
3. **Next.js Web Client**:
   ```bash
   cd web
   npm install
   npm run dev
   ```
4. **Android APK**:
   ```bash
   cd android
   ./gradlew assembleDebug
   ```

---

## 3. Chạy kiểm thử (Tests)

- **C# Unit + Integration Tests** (19 unit tests + 7 integration/security/E2E tests):
  ```bash
  cd backend
  dotnet test CocoCarbon.sln
  ```
- **Python AI Tests** (15 tests):
  ```bash
  cd ai-service
  uv run pytest -v
  ```
- **Web Tests & Build**:
  ```bash
  cd web
  npm test && npm run build
  ```
- **Android Unit Test**:
  ```bash
  cd android
  ./gradlew testDebugUnitTest
  ```

---

## 4. Tài liệu đi kèm (`docs/`)
- [Đối chiếu khuyến nghị Hackathon (MoMo Tech Contest 2026)](docs/hackathon-gap-audit.md)
- [Register nguồn khoa học & quy tắc giới hạn](docs/scientific-sources.md)
- [Kiến trúc hệ thống & luồng bảo mật](docs/architecture.md)
- [Mẫu API & Request/Response](docs/api-examples.md)
- [Hướng dẫn triển khai Production & Firebase/Android](docs/deployment-guide.md)
- [Hướng dẫn sử dụng 1 trang A4](docs/user-guide-1page.md)
- [Dàn ý Slide (10 trang)](docs/pitch-slides-outline.md)
- [Kịch bản Video Demo (<= 3 phút)](docs/demo-video-script.md)
- [Danh sách OSS, Tài sản & Công cụ AI](docs/oss-and-assets.md)
