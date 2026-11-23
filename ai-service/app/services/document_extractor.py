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


def run_consistency_and_duplicate_checks(
    *,
    file_id: str,
    sha256_hash: str,
    known_hashes: list[str],
    extracted_fields: list[dict[str, Any]],
    farm_context: dict[str, Any],
) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    findings: list[dict[str, Any]] = []
    duplicates: list[dict[str, Any]] = []

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

    # Cross-check extracted area vs declared farm area
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
        except ValueError:
            pass

    return findings, duplicates
