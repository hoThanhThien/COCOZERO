# CocoCarbon AI — API Reference & Request/Response Samples

OpenAPI / Swagger UI (khi chạy C# API): `/swagger`

## 1. Xác thực Firebase ID Token (`GET /api/v1/me`)
```bash
curl -H "Authorization: Bearer <FIREBASE_ID_TOKEN>" https://your-domain/api/v1/me
```
Response `200 OK`:
```json
{
  "id": "uuid",
  "firebaseUid": "firebase-uid",
  "email": "farmer@example.com",
  "displayName": "Nguyễn Văn An",
  "roles": [{ "roleCode": "Farmer", "approvalStatus": "Approved" }]
}
```

## 2. Coconut Carbon Calculator (`POST /api/v1/carbon-calculator/estimate`)
Ví dụ chuẩn Dừa Ta 10 năm tuổi, 1.0 ha:
```bash
curl -X POST https://your-domain/api/v1/carbon-calculator/estimate \
  -H "Authorization: Bearer <FIREBASE_ID_TOKEN>" \
  -H "Content-Type: application/json" \
  -d '{"areaHa":1.0,"treeCount":200,"variety":"TallDuaTa","treeAgeYears":10}'
```
Response `200 OK`:
```json
{
  "estimatedCO2EquivalentT": 75.2436,
  "observedTreeDensityPerHa": 200.00,
  "formulaVersion": "CTU-2015-AREA-V1",
  "sourceCitation": "R1: Nguyễn Thị Thanh Trúc & Lê Anh Tuấn (2015) - ..."
}
```
Nếu tuổi khác `4` hoặc `10` (vd: `7`), API trả `422 Unprocessable Entity` với `errorCode: "UNSUPPORTED_AGE_CLASS"` — tuyệt đối không nội suy.

## 3. Upload tài liệu riêng tư & AI Carbon Auditor
- `POST /api/v1/farms/{id}/files` (multipart `file` PDF/JPEG/PNG; kiểm tra MIME + magic bytes + giới hạn dung lượng + SHA-256).
- `GET /api/v1/files/{fileId}/signed-url` -> trả liên kết HMAC tạm thời `/api/v1/files/{fileId}/download?uid=...&expires=...&sig=...`.
- `POST /api/v1/ai/document-analysis` -> C# gọi Python FastAPI nội bộ (`X-Internal-Api-Key`), lưu `AiAnalysis` (`ReviewStatus: PendingReview`), trả `extractedFields`, `findings`, `duplicates`, `evidence`, `readinessRubric`.
- `POST /api/v1/ai/analyses/{id}/review` -> Chỉ `Reviewer` / `Admin` phê duyệt (`Accepted` / `Edited` / `Rejected`).

## 4. Hợp tác xã, Dự án, RFQ & Admin
- `GET /api/v1/cooperatives/{id}/dashboard`
- `GET/POST /api/v1/projects`, `GET/PATCH /api/v1/projects/{id}`, `POST /api/v1/projects/{id}/farms`
- `GET/POST /api/v1/rfqs`, `POST /api/v1/rfqs/{id}/respond`
- `GET /api/v1/admin/users`, `POST /api/v1/admin/users/{id}/roles`, `GET/POST /api/v1/admin/research-factors`, `GET /api/v1/admin/review-queue`, `GET /api/v1/admin/audit-logs`


## 5. AI Financial Assistant, Financial Engine & Carbon Integrity (`/api/ai/*`, `/api/financial/*`, `/api/carbon/*`)
Các endpoint này được C# ASP.NET Core API bảo vệ bằng `[Authorize]` (Firebase Bearer Token / `dev-demo:*`), tự động làm giàu số liệu sổ thu chi thực tế từ `FarmFinancialEntries` khi truyền `?farmId=<uuid>`, và chuyển tiếp nội bộ tới Python Financial & AI Engine:

- **AI Financial Assistant (`POST /api/ai/assistant`)**: phân loại intent song ngữ VI/EN, giữ session context, truy xuất RAG 5 nhóm nguồn (`R1`-`R12`, `AGRI1`, `GF1`), chạy Gemini Interactions Tool Calling (hoặc Heuristic Tool Fallback) để tính bằng Python Financial Engine:
  ```bash
  curl -X POST https://your-domain/api/ai/assistant \
    -H "Authorization: Bearer <FIREBASE_ID_TOKEN>" \
    -H "Content-Type: application/json" \
    -d '{"role":"Farmer","question":"Tính ROI và dòng tiền 5 năm cho vườn dừa 1.5 ha","financialInput":{"farmName":"Vườn Dừa Bến Tre","investment":{"areaHa":1.5,"landPrepAndSeedlingsVnd":12000000,"irrigationAndEquipmentVnd":18000000,"mrvBaselineAndAuditBudgetVnd":5000000,"annualCareAndFertilizerPerHaVnd":9000000,"annualLaborAndHarvestPerHaVnd":7000000,"annualMonitoringAndAdminVnd":2000000,"horizonYears":5},"revenue":{"areaHa":1.5,"annualCoconutYieldUnitsPerHa":9000,"coconutUnitPriceVnd":6500,"annualByProductRevenuePerHaVnd":2500000},"discountRate":0.08}}'
  ```
- **Financial Engine Endpoints**:
  - `POST /api/financial/calculate?farmId=<optional>` -> trả `costBreakdown`, `revenueBreakdown`, `profitAndRoi`, `cashFlowSchedule[]`, `actualAdjustedCashFlowSchedule[]`, `actualVsBudget` (tính lại ROI/NPV theo số liệu thực tế), `actualPeriodAggregations` (`byMonth`, `byQuarter`, `byYear`), `financialAlerts[]`.
  - `POST /api/financial/parse-transaction` -> phân tích câu mô tả tự nhiên VI/EN thành đề xuất ghi Sổ Thu Chi Vườn (`requiresUserConfirmation: true`).
  - `POST /api/financial/scenarios` -> so sánh kịch bản cơ sở với biến động giá/sản lượng/chi phí và kịch bản carbon giả định (`HYPOTHETICAL_SCENARIO`).
  - `POST /api/financial/analyze?farmId=<optional>` -> kết hợp tính toán Python, mô phỏng kịch bản và phân tích AI kèm nguồn RAG.
  - `POST /api/financial/reports?farmId=<optional>` -> báo cáo 9 phần (`executiveSummary`, `investmentOverview`, `revenueAnalysis`, `costAnalysis`, `roiAnalysis`, `cashFlowAnalysis`, `riskAssessment`, `recommendations`, `dataSourcesAndAssumptions`).
- **Carbon Integrity & Proposal Fraud Guard**:
  - `POST /api/carbon/integrity-check` -> tách biệt rõ `projectReadinessScore`, `documentCompletenessScore` và `carbonIntegrityRiskScore` + `explainableAlerts[]` (`IncompleteDocuments_NotFraud` khi chỉ thiếu hồ sơ mà không có dấu hiệu gian lận).
  - `POST /api/carbon/proposal-check` -> kiểm tra đề nghị đầu tư carbon trước khi ký/chuyển tiền (`DO_NOT_SIGN_OR_PAY`, phát hiện yêu cầu đóng phí trước, cam kết lợi nhuận chắc chắn, `tCO2/ha/năm` sai quy chuẩn, áp lực ký gấp).
