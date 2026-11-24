import math
import re
from typing import Any
import pymupdf


class PdfInspectionResult:
    def __init__(self, page_count: int, pages_text: list[tuple[int, str]], is_malformed: bool, error_message: str | None = None):
        self.page_count = page_count
        self.pages_text = pages_text
        self.is_malformed = is_malformed
        self.error_message = error_message


def inspect_document_bytes(file_bytes: bytes, mime_type: str) -> PdfInspectionResult:
    if mime_type != "application/pdf":
        return PdfInspectionResult(page_count=1, pages_text=[], is_malformed=False)

    if not file_bytes.startswith(b"%PDF-"):
        return PdfInspectionResult(
            page_count=0,
            pages_text=[],
            is_malformed=True,
            error_message="Tệp PDF không hợp lệ (thiếu header %PDF-).",
        )

    try:
        doc = pymupdf.open(stream=file_bytes, filetype="pdf")
    except Exception as exc:
        return PdfInspectionResult(
            page_count=0,
            pages_text=[],
            is_malformed=True,
            error_message=f"Không thể đọc tệp PDF (tệp lỗi hoặc bị hỏng): {type(exc).__name__}",
        )

    pages_text: list[tuple[int, str]] = []
    try:
        for idx, page in enumerate(doc, start=1):
            text = (page.get_text("text") or "").strip()
            if text:
                pages_text.append((idx, text))
        page_count = len(doc)
    finally:
        doc.close()

    return PdfInspectionResult(page_count=page_count, pages_text=pages_text, is_malformed=False)


def _haversine_meters(lat1: float, lon1: float, lat2: float, lon2: float) -> float:
    r = 6371000.0
    p1, p2 = math.radians(lat1), math.radians(lat2)
    dp = math.radians(lat2 - lat1)
    dl = math.radians(lon2 - lon1)
    a = math.sin(dp / 2) ** 2 + math.cos(p1) * math.cos(p2) * math.sin(dl / 2) ** 2
    return 2 * r * math.asin(math.sqrt(a))


def run_consistency_and_duplicate_checks(
    *,
    file_id: str,
    sha256_hash: str,
    known_hashes: list[str],
    known_coordinates: list[dict[str, Any]] | None,
    pages_text: list[tuple[int, str]],
    extracted_fields: list[dict[str, Any]],
    farm_context: dict[str, Any],
) -> tuple[list[dict[str, Any]], list[dict[str, Any]], float, str, list[dict[str, Any]]]:
    findings: list[dict[str, Any]] = []
    duplicates: list[dict[str, Any]] = []
    fraud_signals: list[dict[str, Any]] = []
    risk_score = 0.0

    # 1. Exact SHA-256 duplicate detection
    if sha256_hash and sha256_hash in known_hashes:
        duplicates.append({
            "duplicateType": "exact_duplicate",
            "matchedId": sha256_hash[:16],
            "reason": "Mã băm SHA-256 của tệp trùng khớp chính xác với một tài liệu đã tải lên trước đó trong hệ thống.",
        })
        findings.append({
            "code": "EXACT_DUPLICATE_HASH",
            "severity": "warning",
            "category": "duplicate_detection",
            "message": "Phát hiện tệp trùng lặp chính xác qua SHA-256; cần kiểm tra tránh nộp trùng hồ sơ giữa các vườn/dự án.",
            "evidenceRef": f"sha256:{sha256_hash[:12]}",
        })
        fraud_signals.append({
            "code": "EXACT_DUPLICATE_HASH",
            "severity": "high",
            "evidenceSnippet": f"SHA-256={sha256_hash[:16]}...",
            "explanation": "Tài liệu giống hệt một tệp đã tồn tại; có nguy cơ nộp trùng hồ sơ vườn.",
        })
        risk_score += 40.0

    # 2. GPS coordinate overlap anti-fraud check (<= 50m from another registered farm)
    if known_coordinates and farm_context.get("latitude") is not None and farm_context.get("longitude") is not None:
        try:
            lat = float(farm_context["latitude"])
            lng = float(farm_context["longitude"])
            for item in known_coordinates:
                dist = _haversine_meters(lat, lng, float(item["latitude"]), float(item["longitude"]))
                if dist <= 50.0:
                    duplicates.append({
                        "duplicateType": "possible_duplicate",
                        "matchedId": str(item.get("farmId", "")),
                        "reason": f"Tọa độ GPS cách một vườn khác chỉ {dist:.1f}m — nghi ngờ trùng ranh giới.",
                    })
                    fraud_signals.append({
                        "code": "FRAUD_COORDINATE_OVERLAP",
                        "severity": "high",
                        "evidenceSnippet": f"({lat:.5f}, {lng:.5f}) cách {dist:.1f}m so với farm {str(item.get('farmId', ''))[:8]}",
                        "explanation": "Cần kiểm tra thực địa/polygon để loại trừ đăng ký chồng lấn diện tích.",
                    })
                    findings.append({
                        "code": "FRAUD_COORDINATE_OVERLAP",
                        "severity": "warning",
                        "category": "anti_fraud",
                        "message": f"Cảnh báo chống trùng lặp: tọa độ vườn gần vườn khác ({dist:.1f}m).",
                        "evidenceRef": f"farm:{item.get('farmId', '')}",
                    })
                    risk_score += 35.0
                    break
        except (ValueError, TypeError, KeyError):
            pass

    # 3. Cross-check extracted fields vs declared farm fields
    extracted_map = {str(item.get("field", "")): item.get("value") for item in extracted_fields}
    if farm_context and "areaHa" in farm_context and "areaHa" in extracted_map and extracted_map["areaHa"] is not None:
        try:
            declared_area = float(farm_context["areaHa"])
            extracted_area = float(str(extracted_map["areaHa"]).replace(",", "."))
            if abs(declared_area - extracted_area) > 0.15:
                findings.append({
                    "code": "AREA_MISMATCH",
                    "severity": "warning",
                    "category": "consistency",
                    "message": (
                        f"Diện tích trích xuất từ tài liệu ({extracted_area} ha) khác với diện tích vườn khai báo "
                        f"({declared_area} ha). Cần Reviewer đối chiếu lại giấy tờ đất."
                    ),
                    "evidenceRef": f"file:{file_id}",
                })
                fraud_signals.append({
                    "code": "AREA_MISMATCH",
                    "severity": "warning",
                    "evidenceSnippet": f"Tài liệu: {extracted_area} ha vs Khai báo: {declared_area} ha",
                    "explanation": "Sai lệch diện tích giữa chứng từ và hồ sơ khai báo.",
                })
                risk_score += 20.0
            else:
                findings.append({
                    "code": "AREA_CONSISTENT",
                    "severity": "info",
                    "category": "consistency",
                    "message": f"Diện tích trích xuất ({extracted_area} ha) tương thích với diện tích khai báo ({declared_area} ha).",
                    "evidenceRef": f"file:{file_id}",
                })
        except ValueError:
            pass

    if farm_context and "treeCount" in farm_context and "treeCount" in extracted_map and extracted_map["treeCount"] is not None:
        try:
            declared_trees = int(farm_context["treeCount"])
            extracted_trees = int(float(str(extracted_map["treeCount"])))
            if abs(declared_trees - extracted_trees) > max(10, int(declared_trees * 0.1)):
                findings.append({
                    "code": "TREE_COUNT_MISMATCH",
                    "severity": "warning",
                    "category": "consistency",
                    "message": (
                        f"Số cây trích xuất ({extracted_trees}) chênh lệch đáng kể so với số cây khai báo ({declared_trees})."
                    ),
                    "evidenceRef": f"file:{file_id}",
                })
                fraud_signals.append({
                    "code": "TREE_COUNT_MISMATCH",
                    "severity": "warning",
                    "evidenceSnippet": f"Tài liệu: {extracted_trees} cây vs Khai báo: {declared_trees} cây",
                    "explanation": "Sai lệch số lượng cây khai báo so với chứng từ.",
                })
                risk_score += 20.0
        except ValueError:
            pass

    # 4. Semantic Anti-Fraud & Greenwashing Detector on extracted document text
    full_text = "\n".join(text for _, text in pages_text)
    if full_text:
        annual_match = re.search(
            r"(t\s*co2\s*/\s*ha\s*/\s*n[aă]m|h[aấ]p\s*th[uụ]\s*h[aằ]ng\s*n[aă]m|h[aấ]p\s*th[uụ]\s*h[aà]ng\s*n[aă]m|annual\s*sequestration)",
            full_text,
            re.IGNORECASE,
        )
        if annual_match:
            fraud_signals.append({
                "code": "FRAUD_ANNUAL_RATE_CLAIM",
                "severity": "high",
                "evidenceSnippet": annual_match.group(0),
                "explanation": "Tài liệu mô tả sai quy chuẩn khoa học khi gán trữ lượng CTU 2015 thành mức hấp thụ hằng năm (tCO2/ha/năm).",
            })
            findings.append({
                "code": "FRAUD_ANNUAL_RATE_CLAIM",
                "severity": "error",
                "category": "anti_fraud",
                "message": "Phát hiện tuyên bố sai khoa học (tCO2/ha/năm / hấp thụ hằng năm) trong tài liệu tải lên.",
                "evidenceRef": f"file:{file_id}",
            })
            risk_score += 30.0

        rev_match = re.search(
            r"(doanh\s*thu\s*ch[aắ]c\s*ch[aắ]n|thu\s*nh[aậ]p\s*c[oố]\s*[dđ][iị]nh|cam\s*k[eế]t\s*l[oợ]i\s*nhu[aậ]n|b[aả]o\s*[dđ][aả]m\s*b[aá]n\s*t[ií]n\s*ch[iỉ]|guaranteed\s*revenue)",
            full_text,
            re.IGNORECASE,
        )
        if rev_match:
            fraud_signals.append({
                "code": "FRAUD_GUARANTEED_REVENUE_CLAIM",
                "severity": "high",
                "evidenceSnippet": rev_match.group(0),
                "explanation": "Phát hiện dấu hiệu quảng cáo/cam kết lợi nhuận carbon chắc chắn không có cơ sở pháp lý.",
            })
            findings.append({
                "code": "FRAUD_GUARANTEED_REVENUE_CLAIM",
                "severity": "error",
                "category": "anti_fraud",
                "message": "Cảnh báo chống lừa đảo: Tài liệu chứa cam kết doanh thu/lợi nhuận tín chỉ carbon bất thường.",
                "evidenceRef": f"file:{file_id}",
            })
            risk_score += 35.0

        cert_match = re.search(
            r"([dđ][aã]\s*[dđ][uươ][oợ]c\s*(?:verra|gold\s*standard)\s*(?:ch[uứ]ng\s*nh[aậ]n|ph[aá]t\s*h[aà]nh)|issuable\s*credits\s*=|serial\s*t[ií]n\s*ch[iỉ]\s*[dđ][aã]\s*c[aấ]p)",
            full_text,
            re.IGNORECASE,
        )
        if cert_match:
            fraud_signals.append({
                "code": "FRAUD_UNVERIFIED_CERT_CLAIM",
                "severity": "high",
                "evidenceSnippet": cert_match.group(0),
                "explanation": "Tài liệu tự nhận đã chứng nhận/phát hành tín chỉ nhưng chưa có đối soát registry chính thức.",
            })
            findings.append({
                "code": "FRAUD_UNVERIFIED_CERT_CLAIM",
                "severity": "warning",
                "category": "anti_fraud",
                "message": "Tuyên bố đã chứng nhận/phát hành tín chỉ trong tài liệu chưa được xác minh độc lập.",
                "evidenceRef": f"file:{file_id}",
            })
            risk_score += 30.0

        density_match = re.search(r"(\d{3,4})\s*c[aâ]y\s*/\s*ha", full_text, re.IGNORECASE)
        if density_match:
            d_val = int(density_match.group(1))
            if d_val > 350 or d_val < 80:
                fraud_signals.append({
                    "code": "FRAUD_DENSITY_OUTLIER",
                    "severity": "warning",
                    "evidenceSnippet": density_match.group(0),
                    "explanation": f"Mật độ {d_val} cây/ha lệch lớn so với khoảng khảo sát Bến Tre 2024 (160–240 cây/ha); nghi ngờ khai khống mật độ.",
                })
                findings.append({
                    "code": "FRAUD_DENSITY_OUTLIER",
                    "severity": "warning",
                    "category": "anti_fraud",
                    "message": f"Mật độ nêu trong tài liệu ({d_val} cây/ha) bất thường so với thực tế canh tác Bến Tre (160–240 cây/ha).",
                    "evidenceRef": f"file:{file_id}",
                })
                risk_score += 20.0

    capped_score = min(100.0, round(risk_score, 1))
    risk_level = "High" if capped_score >= 60.0 else ("Medium" if capped_score >= 25.0 else "Low")
    return findings, duplicates, capped_score, risk_level, fraud_signals
