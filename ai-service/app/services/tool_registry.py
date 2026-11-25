from __future__ import annotations

from typing import Any
from pydantic import ValidationError

from app.schemas_financial import (
    FinancialCalculationRequest,
    InvestmentCostInput,
    RevenueInput,
    ScenarioSimulationRequest,
)
from app.services.carbon_integrity import (
    CarbonIntegrityCheckRequest,
    ProposalFraudCheckRequest,
    evaluate_carbon_integrity,
    evaluate_investment_proposal,
)
from app.services.financial_engine import (
    build_deterministic_financial_report,
    parse_natural_language_transaction,
    run_financial_calculation,
    run_scenario_simulation,
)
from app.services.rag_knowledge import retrieve_relevant_sources


TOOL_DECLARATIONS: list[dict[str, Any]] = [
    {
        "type": "function",
        "name": "calculate_financial_metrics",
        "description": (
            "Tính toán chính xác chi phí đầu tư (CAPEX/OPEX), doanh thu nông sản/phụ phẩm dừa, lợi nhuận ròng, "
            "ROI, thời gian hoàn vốn, NPV và bảng dòng tiền nhiều năm bằng Python Financial Engine. "
            "Luôn gọi hàm này thay vì tự tính nhẩm."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "farmName": {"type": "string"},
                "areaHa": {"type": "number", "description": "Diện tích vườn dừa (ha), phải > 0"},
                "horizonYears": {"type": "integer", "description": "Số năm phân tích (1-30)"},
                "landPrepAndSeedlingsVnd": {"type": "number"},
                "irrigationAndEquipmentVnd": {"type": "number"},
                "mrvBaselineAndAuditBudgetVnd": {"type": "number"},
                "annualCareAndFertilizerPerHaVnd": {"type": "number"},
                "annualLaborAndHarvestPerHaVnd": {"type": "number"},
                "annualMonitoringAndAdminVnd": {"type": "number"},
                "annualCoconutYieldUnitsPerHa": {"type": "number"},
                "coconutUnitPriceVnd": {"type": "number"},
                "annualByProductRevenuePerHaVnd": {"type": "number"},
                "annualGreenSupportGrantVnd": {"type": "number"},
                "assumedEligibleCarbonTonnesTotal": {"type": "number"},
                "assumedCarbonPricePerTonneVnd": {"type": "number"},
                "carbonRealizationProbability": {"type": "number"},
                "discountRate": {"type": "number"},
            },
            "required": ["areaHa"],
        },
    },
    {
        "type": "function",
        "name": "simulate_financial_scenarios",
        "description": (
            "Chạy mô phỏng so sánh nhiều kịch bản tài chính (Cơ sở, Thận trọng, Tích cực) khi biến động giá bán, "
            "sản lượng và chi phí chăm sóc."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "areaHa": {"type": "number"},
                "horizonYears": {"type": "integer"},
                "annualCareAndFertilizerPerHaVnd": {"type": "number"},
                "annualLaborAndHarvestPerHaVnd": {"type": "number"},
                "annualCoconutYieldUnitsPerHa": {"type": "number"},
                "coconutUnitPriceVnd": {"type": "number"},
                "annualByProductRevenuePerHaVnd": {"type": "number"},
                "landPrepAndSeedlingsVnd": {"type": "number"},
            },
            "required": ["areaHa"],
        },
    },
    {
        "type": "function",
        "name": "search_knowledge_base",
        "description": (
            "Truy xuất tài liệu khoa học về dừa (CTU 2015, khảo sát 2024), hướng dẫn canh tác, "
            "quản lý tài chính xanh, quy định carbon (Nghị định 29/2026) và phương pháp MRV (IPCC, Verra, Gold Standard)."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "query": {"type": "string"},
                "category": {
                    "type": "string",
                    "enum": ["coconut_research", "agri_operations", "financial_management", "carbon_regulation", "carbon_mrv"],
                },
            },
            "required": ["query"],
        },
    },
    {
        "type": "function",
        "name": "evaluate_carbon_integrity",
        "description": (
            "Đánh giá tính toàn vẹn hồ sơ carbon với 3 điểm tách biệt: Project Readiness Score, "
            "Document Completeness Score và Carbon Integrity Risk Score kèm cảnh báo giải thích."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "farmName": {"type": "string"},
                "areaHa": {"type": "number"},
                "treeCount": {"type": "integer"},
                "ageYears": {"type": "integer"},
                "declaredTextClaims": {"type": "string"},
                "hasLandDocument": {"type": "boolean"},
                "hasFieldPhotos": {"type": "boolean"},
                "hasOwnerConsent": {"type": "boolean"},
            },
        },
    },
    {
        "type": "function",
        "name": "evaluate_investment_proposal",
        "description": (
            "Kiểm tra đề nghị đầu tư / hợp đồng carbon trước khi nông hộ ký tên hoặc chuyển tiền: "
            "phát hiện yêu cầu đóng phí trước, cam kết lợi nhuận chắc chắn, tuyên bố sai tCO2/ha/năm và áp lực ký gấp."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "proposerName": {"type": "string"},
                "upfrontFeeRequestedVnd": {"type": "number"},
                "promisedAnnualCarbonRevenueVnd": {"type": "number"},
                "promisedCo2ClaimText": {"type": "string"},
                "contractTermsText": {"type": "string"},
                "knownRegistryId": {"type": "string"},
            },
        },
    },
    {
        "type": "function",
        "name": "parse_natural_language_transaction",
        "description": "Phân tích mô tả chi tiêu/thu hoạch vườn dừa tự nhiên (VI/EN) để đề xuất ghi vào Sổ Thu Chi Vườn (cần user xác nhận).",
        "parameters": {
            "type": "object",
            "properties": {"text": {"type": "string"}},
            "required": ["text"],
        },
    },
    {
        "type": "function",
        "name": "generate_sales_rfq_draft",
        "description": (
            "Soạn thảo thông tin giới thiệu sản phẩm dừa/phụ phẩm hoặc phản hồi yêu cầu tìm hiểu (RFQ) cho nông hộ/HTX "
            "giúp tiết kiệm thời gian giao dịch minh bạch với Buyer (không bịa tín chỉ phát hành)."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "farmName": {"type": "string"},
                "areaHa": {"type": "number"},
                "annualYieldUnitsPerHa": {"type": "number"},
                "byProductsSummary": {"type": "string"},
                "buyerInquiry": {"type": "string"},
            },
        },
    },
    {
        "type": "function",
        "name": "build_personal_action_checklist",
        "description": (
            "Lập danh sách công việc cá nhân, lịch theo dõi chi phí/chứng từ MRV còn thiếu và hành động ưu tiên cho chủ vườn."
        ),
        "parameters": {
            "type": "object",
            "properties": {
                "farmName": {"type": "string"},
                "hasLandDocument": {"type": "boolean"},
                "hasFieldPhotos": {"type": "boolean"},
                "hasOwnerConsent": {"type": "boolean"},
                "hasFinancialEntries": {"type": "boolean"},
            },
        },
    },
]


def execute_tool(name: str, arguments: dict[str, Any]) -> dict[str, Any]:
    """Execute a registered deterministic Python tool safely."""
    try:
        if name == "calculate_financial_metrics":
            req = FinancialCalculationRequest(
                farmName=str(arguments.get("farmName") or "Vườn dừa CocoCarbon"),
                discountRate=float(arguments.get("discountRate", 0.08)),
                actualRecordedExpenseVnd=(
                    float(arguments["actualRecordedExpenseVnd"])
                    if arguments.get("actualRecordedExpenseVnd") is not None
                    else None
                ),
                actualRecordedRevenueVnd=(
                    float(arguments["actualRecordedRevenueVnd"])
                    if arguments.get("actualRecordedRevenueVnd") is not None
                    else None
                ),
                investment=InvestmentCostInput(
                    areaHa=float(arguments.get("areaHa", 1.0)),
                    horizonYears=int(arguments.get("horizonYears", 5)),
                    landPrepAndSeedlingsVnd=float(arguments.get("landPrepAndSeedlingsVnd", 0.0)),
                    irrigationAndEquipmentVnd=float(arguments.get("irrigationAndEquipmentVnd", 0.0)),
                    mrvBaselineAndAuditBudgetVnd=float(arguments.get("mrvBaselineAndAuditBudgetVnd", 0.0)),
                    annualCareAndFertilizerPerHaVnd=float(arguments.get("annualCareAndFertilizerPerHaVnd", 0.0)),
                    annualLaborAndHarvestPerHaVnd=float(arguments.get("annualLaborAndHarvestPerHaVnd", 0.0)),
                    annualMonitoringAndAdminVnd=float(arguments.get("annualMonitoringAndAdminVnd", 0.0)),
                ),
                revenue=RevenueInput(
                    areaHa=float(arguments.get("areaHa", 1.0)),
                    annualCoconutYieldUnitsPerHa=float(arguments.get("annualCoconutYieldUnitsPerHa", 0.0)),
                    coconutUnitPriceVnd=float(arguments.get("coconutUnitPriceVnd", 0.0)),
                    annualByProductRevenuePerHaVnd=float(arguments.get("annualByProductRevenuePerHaVnd", 0.0)),
                    annualGreenSupportGrantVnd=float(arguments.get("annualGreenSupportGrantVnd", 0.0)),
                    assumedEligibleCarbonTonnesTotal=float(arguments.get("assumedEligibleCarbonTonnesTotal", 0.0)),
                    assumedCarbonPricePerTonneVnd=float(arguments.get("assumedCarbonPricePerTonneVnd", 0.0)),
                    carbonRealizationProbability=float(arguments.get("carbonRealizationProbability", 0.0)),
                ),
            )
            calc = run_financial_calculation(req)
            report = build_deterministic_financial_report(calc)
            return {
                "ok": True,
                "calculation": calc.model_dump(),
                "reportSummary": report.executiveSummary,
            }

        if name == "simulate_financial_scenarios":
            base_req = FinancialCalculationRequest(
                farmName=str(arguments.get("farmName") or "Vườn dừa CocoCarbon"),
                investment=InvestmentCostInput(
                    areaHa=float(arguments.get("areaHa", 1.0)),
                    horizonYears=int(arguments.get("horizonYears", 5)),
                    landPrepAndSeedlingsVnd=float(arguments.get("landPrepAndSeedlingsVnd", 0.0)),
                    annualCareAndFertilizerPerHaVnd=float(arguments.get("annualCareAndFertilizerPerHaVnd", 0.0)),
                    annualLaborAndHarvestPerHaVnd=float(arguments.get("annualLaborAndHarvestPerHaVnd", 0.0)),
                ),
                revenue=RevenueInput(
                    areaHa=float(arguments.get("areaHa", 1.0)),
                    annualCoconutYieldUnitsPerHa=float(arguments.get("annualCoconutYieldUnitsPerHa", 0.0)),
                    coconutUnitPriceVnd=float(arguments.get("coconutUnitPriceVnd", 0.0)),
                    annualByProductRevenuePerHaVnd=float(arguments.get("annualByProductRevenuePerHaVnd", 0.0)),
                ),
            )
            sim = run_scenario_simulation(ScenarioSimulationRequest(baseInput=base_req))
            return {"ok": True, "simulation": sim.model_dump()}

        if name == "search_knowledge_base":
            sources = retrieve_relevant_sources(
                str(arguments.get("query", "")),
                category=arguments.get("category"),
                top_k=4,
            )
            return {"ok": True, "sources": sources}

        if name == "evaluate_carbon_integrity":
            check_req = CarbonIntegrityCheckRequest(
                farmName=str(arguments.get("farmName") or ""),
                areaHa=arguments.get("areaHa"),
                treeCount=arguments.get("treeCount"),
                ageYears=arguments.get("ageYears"),
                declaredTextClaims=str(arguments.get("declaredTextClaims") or ""),
                hasLandDocument=bool(arguments.get("hasLandDocument", False)),
                hasFieldPhotos=bool(arguments.get("hasFieldPhotos", False)),
                hasOwnerConsent=bool(arguments.get("hasOwnerConsent", False)),
            )
            res = evaluate_carbon_integrity(check_req)
            return {"ok": True, "integrity": res.model_dump()}

        if name == "evaluate_investment_proposal":
            prop_req = ProposalFraudCheckRequest(
                proposerName=str(arguments.get("proposerName") or ""),
                upfrontFeeRequestedVnd=float(arguments.get("upfrontFeeRequestedVnd") or 0.0),
                promisedAnnualCarbonRevenueVnd=float(arguments.get("promisedAnnualCarbonRevenueVnd") or 0.0),
                promisedCo2ClaimText=str(arguments.get("promisedCo2ClaimText") or ""),
                contractTermsText=str(arguments.get("contractTermsText") or ""),
                knownRegistryId=str(arguments.get("knownRegistryId") or ""),
            )
            res = evaluate_investment_proposal(prop_req)
            return {"ok": True, "proposalCheck": res.model_dump()}

        if name == "parse_natural_language_transaction":
            proposal = parse_natural_language_transaction(str(arguments.get("text") or ""))
            return {"ok": True, "transactionProposal": proposal.model_dump()}

        if name == "generate_sales_rfq_draft":
            farm_name = str(arguments.get("farmName") or "Vườn dừa CocoCarbon")
            area_ha = float(arguments.get("areaHa") or 1.0)
            yield_ha = float(arguments.get("annualYieldUnitsPerHa") or 0.0)
            byprod = str(arguments.get("byProductsSummary") or "dừa khô, gáo dừa, xơ và mụn dừa hữu cơ")
            draft = (
                f"Thư phản hồi RFQ minh bạch — {farm_name} ({area_ha:.1f} ha, vùng dừa Bến Tre - Vĩnh Long):\n"
                f"1. Năng lực cung ứng nông sản & tuần hoàn: ước tính {yield_ha * area_ha:,.0f} đơn vị dừa/năm cùng phụ phẩm {byprod}.\n"
                "2. Trạng thái dữ liệu carbon: đang chuẩn hóa hồ sơ, GPS & Sổ Thu Chi xanh trên CocoCarbon AI; "
                "mọi chỉ số CTU 2015 chỉ mang tính tham khảo kỹ thuật, chưa phát hành tín chỉ thương mại."
            )
            return {"ok": True, "salesRfqDraft": draft}

        if name == "build_personal_action_checklist":
            farm_name = str(arguments.get("farmName") or "Vườn dừa")
            tasks = []
            if not arguments.get("hasLandDocument"):
                tasks.append("Tải lên bản chụp/PDF giấy tờ quyền sử dụng đất (ưu tiên cao — tăng Document Completeness).")
            if not arguments.get("hasFieldPhotos"):
                tasks.append("Chụp và tải 2–3 ảnh hiện trường vườn dừa có tọa độ GPS.")
            if not arguments.get("hasOwnerConsent"):
                tasks.append("Hoàn tất xác nhận đồng thuận (Consent) về vị trí GPS & xử lý AI.")
            if not arguments.get("hasFinancialEntries"):
                tasks.append("Ghi nhận khoản chi chăm sóc/phân bón hoặc doanh thu thu hoạch tuần này vào Sổ Thu Chi.")
            tasks.append("Kiểm tra cảnh báo rủi ro nếu có lời mời ký hợp đồng carbon yêu cầu đóng phí trước.")
            return {"ok": True, "farmName": farm_name, "actionChecklist": tasks}

        return {"ok": False, "error": f"Unknown tool: {name}"}
    except (ValidationError, ValueError, TypeError) as exc:
        return {"ok": False, "error": f"Invalid tool parameters: {exc}"}
