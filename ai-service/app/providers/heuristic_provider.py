import re
from typing import Any
from app.providers.base import BaseAiProvider, ModelExtractionOutput
from app.services.tool_registry import execute_tool


class HeuristicFallbackProvider(BaseAiProvider):
    """Deterministic local extractor & tool-backed assistant when Gemini API key is unconfigured or unreachable."""

    def __init__(self, reason: str = "Local deterministic fallback (Gemini API unconfigured or offline)") -> None:
        self.provider_name = "heuristic_fallback"
        self.model_name = "deterministic-engine-v2"
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
                "reason": "Phát hiện diện tích theo đơn vị ha trong nội dung văn bản (fallback cục bộ).",
            })
        else:
            missing.append("areaHa (chưa tìm thấy diện tích ha rõ ràng trong tài liệu)")

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
                "reason": "Phát hiện số lượng cây khai báo trong tài liệu (fallback cục bộ).",
            })
        else:
            missing.append("treeCount (chưa thấy số lượng cây trong tài liệu)")

        if re.search(r"dừa\s*ta|tall", joined, re.IGNORECASE):
            extracted.append({"field": "variety", "value": "TallDuaTa", "confidence": 0.74, "provenance": "AI_EXTRACTED"})
        elif re.search(r"xiêm\s*xanh|short", joined, re.IGNORECASE):
            extracted.append({"field": "variety", "value": "ShortXiemXanh", "confidence": 0.74, "provenance": "AI_EXTRACTED"})
        else:
            missing.append("variety (chưa thấy giống Dừa Ta / Xiêm xanh trong tài liệu)")

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
            "Chưa xác minh độc lập; dữ liệu từ tài liệu chỉ là AI_EXTRACTED và bắt buộc có Reviewer kiểm tra.",
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

    def run_assistant_with_tools(
        self,
        *,
        role: str,
        question: str,
        intent: str,
        language: str,
        session_history: list[dict[str, Any]],
        context_data: dict[str, Any],
        retrieved_sources: list[dict[str, Any]],
        precomputed_tool_outputs: dict[str, Any] | None = None,
    ) -> tuple[str, list[dict[str, Any]], bool]:
        tool_traces: list[dict[str, Any]] = []
        lines: list[str] = []

        if intent == "unknown_or_out_of_scope" and not retrieved_sources and not precomputed_tool_outputs:
            msg = (
                "Insufficient verified evidence in the curated CocoCarbon knowledge base to answer this request without fabrication."
                if language == "en"
                else "Chưa có đủ dữ liệu trong kho nguồn kiểm chứng ([R1]-[R12], [AGRI1], [GF1]) hoặc thiếu thông số đầu vào từ người dùng để trả lời câu hỏi này mà không bịa số liệu."
            )
            return msg, tool_traces, True

        # Execute deterministic tool if financial context has areaHa and intent requires calculation/scenario
        if precomputed_tool_outputs:
            for tname, tout in precomputed_tool_outputs.items():
                tool_traces.append({"tool": tname, "source": "python_financial_engine", "ok": True})
                if tname == "calculate_financial_metrics" and "calculation" in tout:
                    c = tout["calculation"]["costBreakdown"]
                    r = tout["calculation"]["revenueBreakdown"]
                    p = tout["calculation"]["profitAndRoi"]
                    lines.append(
                        f"[Kết quả Python Financial Engine — Dữ liệu/Giả định tường minh]\n"
                        f"• Tổng chi phí ({tout['calculation']['horizonYears']} năm): {c['totalInvestmentCostVnd']:,.0f} VND "
                        f"(CAPEX {c['initialCapexVnd']:,.0f} + OPEX {c['totalOpexOverHorizonVnd']:,.0f})\n"
                        f"• Doanh thu nông sản & phụ phẩm: {r['totalAgriRevenueOverHorizonVnd']:,.0f} VND\n"
                        f"• Lợi nhuận ròng nông nghiệp: {p['totalNetAgriProfitOverHorizonVnd']:,.0f} VND | "
                        f"ROI thuần nông nghiệp: {p['roiPercentAgriOnly']:.2f}% | NPV: {p['npvAgriOnlyVnd']:,.0f} VND"
                    )
                    if r["carbonScenarioIncluded"]:
                        lines.append(f"• Kịch bản carbon giả định (chưa phát hành tín chỉ): +{r['hypotheticalCarbonRevenueTotalVnd']:,.0f} VND")
                    if tout["calculation"].get("actualVsBudget"):
                        avb = tout["calculation"]["actualVsBudget"]
                        lines.append(
                            f"• Đối chiếu Sổ Thu Chi thực tế: Chi={avb['actualRecordedExpenseVnd']:,.0f} VND, "
                            f"Thu={avb['actualRecordedRevenueVnd']:,.0f} VND -> ROI hiệu chỉnh={avb['adjustedRoiPercentAgriOnly']:.2f}%."
                        )
                    for al in tout["calculation"].get("financialAlerts", []):
                        lines.append(f"• [CẢNH BÁO {al['code']}] {al['message']}")
                elif tname == "simulate_financial_scenarios" and "simulation" in tout:
                    sim = tout["simulation"]
                    lines.append("[So sánh Kịch bản Tài chính — Python Scenario Simulator]")
                    lines.append(f"• {sim['baseSummary']['scenarioName']}: ROI={sim['baseSummary']['roiPercentAgriOnly']:.1f}%, Lợi nhuận={sim['baseSummary']['netProfitAgriOnlyVnd']:,.0f} VND")
                    for sc in sim.get("scenarios", []):
                        lines.append(f"• {sc['scenarioName']}: ROI={sc['roiPercentAgriOnly']:.1f}%, Lợi nhuận={sc['netProfitAgriOnlyVnd']:,.0f} VND")
        elif intent in ("financial_calculation", "investment_analysis", "scenario_simulation") and isinstance(context_data, dict) and context_data.get("areaHa"):
            calc_args = {
                "areaHa": context_data.get("areaHa", 1.0),
                "horizonYears": context_data.get("horizonYears", 5),
                "landPrepAndSeedlingsVnd": context_data.get("landPrepAndSeedlingsVnd", 0.0),
                "annualCareAndFertilizerPerHaVnd": context_data.get("annualCareAndFertilizerPerHaVnd", 0.0),
                "annualLaborAndHarvestPerHaVnd": context_data.get("annualLaborAndHarvestPerHaVnd", 0.0),
                "annualCoconutYieldUnitsPerHa": context_data.get("annualCoconutYieldUnitsPerHa", 0.0),
                "coconutUnitPriceVnd": context_data.get("coconutUnitPriceVnd", 0.0),
                "annualByProductRevenuePerHaVnd": context_data.get("annualByProductRevenuePerHaVnd", 0.0),
            }
            tool_name = "simulate_financial_scenarios" if intent == "scenario_simulation" else "calculate_financial_metrics"
            out = execute_tool(tool_name, calc_args)
            tool_traces.append({"tool": tool_name, "arguments": calc_args, "ok": out.get("ok", False)})
            if out.get("ok") and "calculation" in out:
                c = out["calculation"]["costBreakdown"]
                p = out["calculation"]["profitAndRoi"]
                lines.append(
                    f"[Python Financial Engine] Tổng chi phí: {c['totalInvestmentCostVnd']:,.0f} VND | "
                    f"Lợi nhuận ròng: {p['totalNetAgriProfitOverHorizonVnd']:,.0f} VND | ROI: {p['roiPercentAgriOnly']:.2f}%."
                )
            elif out.get("ok") and "simulation" in out:
                sim = out["simulation"]
                lines.append(f"[Python Scenario Simulator] Kịch bản cơ sở ROI={sim['baseSummary']['roiPercentAgriOnly']:.2f}%.")

        if retrieved_sources:
            header = (
                f"Grounded synthesis for {role} (intent={intent}, offline/fallback mode):"
                if language == "en"
                else f"Tổng hợp có dẫn chứng cho {role} (intent={intent}, chế độ fallback cục bộ):"
            )
            lines.append(header)
            for src in retrieved_sources:
                lines.append(f"- [{src['sourceCode']}] {src['title']}: {src['excerpt']}")

        if not lines:
            lines.append(
                "Để tính ROI, lợi nhuận hoặc mô phỏng dòng tiền chính xác, vui lòng cung cấp chi phí đầu tư, "
                "chi phí chăm sóc, sản lượng và giá bán nông sản trong biểu mẫu Tài chính."
            )

        lines.append(
            "Lưu ý bắt buộc: Kết quả tách biệt giữa Dữ liệu thực tế, Giả định người dùng và Ước tính mô hình; "
            "trữ lượng carbon tham khảo CTU 2015 không phải tín chỉ đã phát hành."
        )
        return "\n".join(lines), tool_traces, True
