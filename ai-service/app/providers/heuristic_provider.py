import re
from typing import Any
from app.providers.base import BaseAiProvider, ModelExtractionOutput


class HeuristicFallbackProvider(BaseAiProvider):
    """Deterministic local extractor/fallback when Gemini API key is unconfigured or unreachable."""

    def __init__(self, reason: str = "Local deterministic fallback (Gemini API unconfigured or offline)") -> None:
        self.provider_name = "heuristic_fallback"
        self.model_name = "deterministic-regex-v1"
        self._reason = reason

    def extract_document(
        self,
        *,
        file_id: str,
        filename: str,
        mime_type: str,
        file_bytes: bytes,
        extracted_text_pages: list[tuple[int, str]],
        farm_context: dict[str, Any],
        project_context: dict[str, Any],
    ) -> tuple[ModelExtractionOutput, bool]:
        extracted: list[dict[str, Any]] = []
        evidence: list[dict[str, Any]] = []
        missing: list[str] = []

        joined = "\n".join(text for _, text in extracted_text_pages)

        # Area pattern e.g. "1.5 ha" or "1,5 ha" or "diện tích: 2.0 ha"
        area_match = re.search(r"(\d+(?:[.,]\d+)?)\s*(?:ha|hecta)", joined, re.IGNORECASE)
        if area_match:
            val = area_match.group(1).replace(",", ".")
            page_no = next((p for p, t in extracted_text_pages if area_match.group(0) in t), 1)
            extracted.append({"field": "areaHa", "value": val, "confidence": 0.72, "provenance": "AI_EXTRACTED"})
            evidence.append({
                "fileId": file_id,
                "pageNumber": page_no,
                "snippet": area_match.group(0),
                "extractedValue": val,
                "confidence": 0.72,
                "reason": "Phát hiện diện tích theo đơn vị ha trong nội dung văn bản (fallback cục bộ)."
            })
        else:
            missing.append("areaHa (chưa tìm thấy diện tích ha rõ ràng trong tài liệu)")

        # Tree count pattern
        tree_match = re.search(r"(\d{2,6})\s*(?:cây|gốc)", joined, re.IGNORECASE)
        if tree_match:
            val = tree_match.group(1)
            page_no = next((p for p, t in extracted_text_pages if tree_match.group(0) in t), 1)
            extracted.append({"field": "treeCount", "value": val, "confidence": 0.70, "provenance": "AI_EXTRACTED"})
            evidence.append({
                "fileId": file_id,
                "pageNumber": page_no,
                "snippet": tree_match.group(0),
                "extractedValue": val,
                "confidence": 0.70,
                "reason": "Phát hiện số lượng cây khai báo trong tài liệu (fallback cục bộ)."
            })
        else:
            missing.append("treeCount (chưa thấy số lượng cây trong tài liệu)")

        # Variety detection
        if re.search(r"dừa\s*ta|tall", joined, re.IGNORECASE):
            extracted.append({"field": "variety", "value": "TallDuaTa", "confidence": 0.74, "provenance": "AI_EXTRACTED"})
        elif re.search(r"xiêm\s*xanh|short", joined, re.IGNORECASE):
            extracted.append({"field": "variety", "value": "ShortXiemXanh", "confidence": 0.74, "provenance": "AI_EXTRACTED"})
        else:
            missing.append("variety (chưa thấy giống Dừa Ta / Xiêm xanh trong tài liệu)")

        # Commune / Province
        if re.search(r"vĩnh\s*long|bến\s*tre|giồng\s*trôm|ba\s*tri|châu\s*thành|thạnh\s*phú|bình\s*đại", joined, re.IGNORECASE):
            loc_match = re.search(r"(?:tỉnh|huyện|xã|vùng)?\s*(?:vĩnh\s*long|bến\s*tre|giồng\s*trôm|ba\s*tri|châu\s*thành|thạnh\s*phú|bình\s*đại)", joined, re.IGNORECASE)
            if loc_match:
                extracted.append({"field": "regionMention", "value": loc_match.group(0).strip(), "confidence": 0.75, "provenance": "AI_EXTRACTED"})
        else:
            missing.append("location_reference")

        if mime_type.startswith("image/") and not extracted_text_pages:
            missing.append("Ảnh hiện trường cần OCR/thị giác đa phương thức hoặc kiểm tra thủ công bởi Reviewer")

        notes = [
            f"Chế độ xử lý: {self._reason}.",
            "Chưa xác minh độc lập; dữ liệu từ tài liệu chỉ là AI_EXTRACTED và bắt buộc có Reviewer kiểm tra."
        ]

        return ModelExtractionOutput(
            extractedFields=extracted,
            documentClaims=[],
            missingFields=missing,
            evidence=evidence,
            confidenceNotes=notes,
        ), True

    def synthesize_rag_answer(
        self,
        *,
        role: str,
        question: str,
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
    ) -> tuple[str, bool]:
        if not retrieved_sources:
            return (
                "Chưa có đủ dữ liệu trong kho nguồn khoa học được kiểm chứng để trả lời câu hỏi này. "
                "Hệ thống không tự suy diễn hệ số, giá tín chỉ hay trạng thái chứng nhận.",
                True,
            )

        lines = [
            f"[Tổng hợp từ nguồn khoa học đã kiểm chứng cho vai trò {role} — chế độ ngoại tuyến/fallback]",
        ]
        if context_data:
            lines.append(f"Ngữ cảnh hồ sơ hiện tại: {context_data}")
        for src in retrieved_sources:
            lines.append(f"- [{src['sourceCode']}] {src['title']}: {src['excerpt']}")
        lines.append("Lưu ý: Đây là thông tin tham khảo từ tài liệu gốc, không phải xác minh VVB hay cam kết tín chỉ.")
        return "\n".join(lines), True
