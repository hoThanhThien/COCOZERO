from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Optional
import uuid
from fastapi import APIRouter, Header, HTTPException
from pydantic import BaseModel, Field

from app.config import settings
from app.providers.base import BaseAiProvider
from app.providers.gemini_provider import GeminiInteractionsProvider
from app.providers.heuristic_provider import HeuristicFallbackProvider
from app.schemas_financial import (
    FinancialAnalysisRequest,
    FinancialCalculationRequest,
    FinancialCalculationResult,
    FinancialReportResponse,
    NaturalLanguageTransactionProposal,
    NaturalLanguageTransactionRequest,
    ScenarioSimulationRequest,
    ScenarioSimulationResult,
)
from app.services.ai_runtime_config import (
    TestAiProviderRequest,
    UpdateAiProviderRequest,
    ai_runtime_config,
)
from app.services.carbon_integrity import (
    CarbonIntegrityCheckRequest,
    CarbonIntegrityCheckResponse,
    ProposalFraudCheckRequest,
    ProposalFraudCheckResponse,
    evaluate_carbon_integrity,
    evaluate_investment_proposal,
)
from app.services.context_manager import session_context_store
from app.services.financial_engine import (
    build_deterministic_financial_report,
    parse_natural_language_transaction,
    run_financial_calculation,
    run_scenario_simulation,
)
from app.services.intent_classifier import classify_intent
from app.services.rag_knowledge import retrieve_relevant_sources
from app.services.tool_registry import execute_tool

router = APIRouter()


def get_provider() -> BaseAiProvider:
    return ai_runtime_config.get_active_provider()


def verify_key_if_configured(x_internal_api_key: str | None) -> None:
    if settings.ai_service_internal_key and x_internal_api_key != settings.ai_service_internal_key:
        raise HTTPException(status_code=403, detail="Unauthorized internal AI call.")


class AiAssistantChatRequest(BaseModel):
    sessionId: Optional[str] = None
    role: str = "Farmer"
    question: str = Field(min_length=1)
    contextData: dict[str, Any] = Field(default_factory=dict)
    financialInput: Optional[FinancialCalculationRequest] = None


class SourceAttributionDto(BaseModel):
    sourceCode: str
    category: str
    title: str
    url: str
    relevanceScore: float
    excerpt: str
    metadata: dict[str, Any] = Field(default_factory=dict)


class AiAssistantChatResponse(BaseModel):
    sessionId: str
    intent: str
    assistantCapability: str = "Trợ lý Tài chính"
    language: str
    answer: str
    hasEnoughEvidence: bool
    toolCallsExecuted: list[dict[str, Any]] = Field(default_factory=list)
    suggestedActions: list[dict[str, Any]] = Field(default_factory=list)
    transactionProposal: Optional[NaturalLanguageTransactionProposal] = None
    financialResult: Optional[FinancialCalculationResult] = None
    scenarioResult: Optional[ScenarioSimulationResult] = None
    citations: list[SourceAttributionDto] = Field(default_factory=list)
    dataDistinction: dict[str, str]
    provider: str
    modelName: str
    isFallback: bool
    disclaimer: str


@router.post("/api/ai/assistant", response_model=AiAssistantChatResponse)
def ai_financial_assistant(
    req: AiAssistantChatRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> AiAssistantChatResponse:
    verify_key_if_configured(x_internal_api_key)

    sid = req.sessionId or str(uuid.uuid4())
    session_ctx = session_context_store.get_context(sid)
    intent_res = classify_intent(req.question, has_financial_payload=req.financialInput is not None)

    merged_context = dict(session_ctx.get("farmContext") or {})
    merged_context.update(req.contextData or {})

    retrieved = retrieve_relevant_sources(req.question, top_k=4)

    precomputed: dict[str, Any] = {}
    fin_result: FinancialCalculationResult | None = None
    sim_result: ScenarioSimulationResult | None = None
    tx_proposal: NaturalLanguageTransactionProposal | None = None
    suggested_actions: list[dict[str, Any]] = []

    if intent_res.intent == "payment_transaction_assist":
        tx_proposal = parse_natural_language_transaction(req.question)
        precomputed["parse_natural_language_transaction"] = {"ok": True, "transactionProposal": tx_proposal.model_dump()}
        suggested_actions.append({
            "type": "confirm_ledger_entry",
            "title": "Xác nhận ghi Sổ Thu Chi Vườn",
            "detail": f"{tx_proposal.category}: {tx_proposal.amountVnd:,.0f} đ",
        })
    elif intent_res.intent == "sales_rfq_assist":
        sales_out = execute_tool("generate_sales_rfq_draft", {
            "farmName": merged_context.get("farmName") or (req.financialInput.farmName if req.financialInput else "Vườn dừa CocoCarbon"),
            "areaHa": merged_context.get("areaHa") or (req.financialInput.investment.areaHa if req.financialInput else 1.5),
            "annualYieldUnitsPerHa": (req.financialInput.revenue.annualCoconutYieldUnitsPerHa if req.financialInput else 9000),
            "buyerInquiry": req.question,
        })
        precomputed["generate_sales_rfq_draft"] = sales_out
        suggested_actions.append({"type": "open_rfq", "title": "Mở Không gian Dự án / RFQ", "href": "/projects"})
    elif intent_res.intent == "personal_workflow_assist":
        chk_out = execute_tool("build_personal_action_checklist", {
            "farmName": merged_context.get("farmName") or "Vườn dừa CocoCarbon",
            "hasLandDocument": bool(merged_context.get("hasLandDocument", False)),
            "hasFieldPhotos": bool(merged_context.get("hasFieldPhotos", True)),
            "hasOwnerConsent": bool(merged_context.get("hasOwnerConsent", True)),
            "hasFinancialEntries": bool(merged_context.get("farmId")),
        })
        precomputed["build_personal_action_checklist"] = chk_out
        suggested_actions.append({"type": "open_documents", "title": "Kiểm tra Hồ sơ & Chứng từ", "href": "/documents"})

    active_fin_input = req.financialInput
    if active_fin_input is None and session_ctx.get("latestFinancialInput") and intent_res.intent in ("financial_calculation", "scenario_simulation", "financial_report"):
        try:
            active_fin_input = FinancialCalculationRequest.model_validate(session_ctx["latestFinancialInput"])
        except Exception:
            active_fin_input = None

    if active_fin_input is not None:
        if intent_res.intent == "scenario_simulation":
            sim_result = run_scenario_simulation(ScenarioSimulationRequest(baseInput=active_fin_input))
            precomputed["simulate_financial_scenarios"] = {"ok": True, "simulation": sim_result.model_dump()}
        else:
            fin_result = run_financial_calculation(active_fin_input)
            precomputed["calculate_financial_metrics"] = {"ok": True, "calculation": fin_result.model_dump()}
            if intent_res.intent == "shopping_procurement_assist":
                suggested_actions.append({
                    "type": "open_proposal_guard",
                    "title": "Kiểm tra độ an toàn đề nghị vật tư / carbon trước khi thanh toán",
                    "href": "/documents",
                })

    provider = get_provider()
    answer_text, tool_traces, is_fallback = provider.run_assistant_with_tools(
        role=req.role,
        question=req.question,
        intent=intent_res.intent,
        language=intent_res.language,
        session_history=session_ctx.get("turns", []),
        context_data=merged_context,
        retrieved_sources=retrieved,
        precomputed_tool_outputs=precomputed or None,
    )

    # Also capture knowledge sources if Gemini called search_knowledge_base dynamically
    citations = [
        SourceAttributionDto(
            sourceCode=s["sourceCode"],
            category=s.get("category", "general"),
            title=s["title"],
            url=s["url"],
            relevanceScore=float(s.get("relevanceScore", 0.5)),
            excerpt=s["excerpt"],
            metadata=s.get("metadata", {}),
        )
        for s in retrieved
    ]

    has_evidence = bool(citations or fin_result or sim_result or tx_proposal or tool_traces or precomputed)
    if intent_res.intent == "unknown_or_out_of_scope" and not has_evidence:
        has_evidence = False

    session_context_store.update_context(
        sid,
        user_question=req.question,
        intent=intent_res.intent,
        assistant_answer=answer_text,
        financial_input=active_fin_input.model_dump() if active_fin_input else None,
        financial_result=fin_result.model_dump() if fin_result else None,
        farm_context=merged_context,
    )

    return AiAssistantChatResponse(
        sessionId=sid,
        intent=intent_res.intent,
        assistantCapability=intent_res.assistantCapability,
        language=intent_res.language,
        answer=answer_text,
        hasEnoughEvidence=has_evidence,
        toolCallsExecuted=tool_traces,
        suggestedActions=suggested_actions,
        transactionProposal=tx_proposal,
        financialResult=fin_result,
        scenarioResult=sim_result,
        citations=citations,
        dataDistinction={
            "actualData": "Số liệu sổ thu chi nông hộ nhập trực tiếp (USER_DECLARED / ACTUAL_DATA)",
            "assumptions": "Đơn giá bán, sản lượng, tỷ lệ chiết khấu, giá carbon giả định (USER_ASSUMPTION)",
            "estimates": "ROI, NPV, dòng tiền nhiều năm tính bởi Python Financial Engine (MODEL_ESTIMATE)",
        },
        provider=provider.provider_name if not is_fallback else "heuristic_fallback",
        modelName=provider.model_name,
        isFallback=is_fallback,
        disclaimer=(
            "Các phép tính được xử lý bằng Python Financial Engine; không tự bịa số liệu hoặc giá tín chỉ. "
            "Ước tính carbon CTU 2015 không phải tín chỉ carbon đã phát hành."
        ),
    )


@router.post("/api/financial/calculate", response_model=FinancialCalculationResult)
def financial_calculate(
    req: FinancialCalculationRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> FinancialCalculationResult:
    verify_key_if_configured(x_internal_api_key)
    return run_financial_calculation(req)


@router.post("/api/financial/scenarios", response_model=ScenarioSimulationResult)
def financial_scenarios(
    req: ScenarioSimulationRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> ScenarioSimulationResult:
    verify_key_if_configured(x_internal_api_key)
    return run_scenario_simulation(req)


@router.post("/api/financial/analyze")
def financial_analyze(
    req: FinancialAnalysisRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> dict[str, Any]:
    verify_key_if_configured(x_internal_api_key)
    calc = run_financial_calculation(req.calculationInput)
    sim = run_scenario_simulation(ScenarioSimulationRequest(baseInput=req.calculationInput)) if req.includeScenarios else None
    retrieved = retrieve_relevant_sources(
        req.question or "phân tích tài chính chi phí chăm sóc roi dòng tiền vườn dừa mrv",
        top_k=3,
    )
    provider = get_provider()
    narrative, tool_traces, is_fallback = provider.run_assistant_with_tools(
        role="Farmer",
        question=req.question or "Hãy tóm tắt phân tích tài chính đầu tư vườn dừa này.",
        intent="investment_analysis",
        language="vi",
        session_history=[],
        context_data={"farmName": req.calculationInput.farmName, "areaHa": req.calculationInput.investment.areaHa},
        retrieved_sources=retrieved,
        precomputed_tool_outputs={
            "calculate_financial_metrics": {"ok": True, "calculation": calc.model_dump()},
            **({"simulate_financial_scenarios": {"ok": True, "simulation": sim.model_dump()}} if sim else {}),
        },
    )
    return {
        "calculation": calc,
        "scenarioComparison": sim,
        "aiNarrative": narrative,
        "toolCallsExecuted": tool_traces,
        "citations": retrieved,
        "isFallback": is_fallback,
        "modelName": provider.model_name,
    }


@router.post("/api/financial/reports", response_model=FinancialReportResponse)
def financial_reports(
    req: FinancialAnalysisRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> FinancialReportResponse:
    verify_key_if_configured(x_internal_api_key)
    calc = run_financial_calculation(req.calculationInput)
    sim = run_scenario_simulation(ScenarioSimulationRequest(baseInput=req.calculationInput)) if req.includeScenarios else None
    retrieved = retrieve_relevant_sources("quản lý tài chính vườn dừa roi dòng tiền mrv ctU 2015", top_k=4)
    report_sections = build_deterministic_financial_report(calc, sim, retrieved)

    provider = get_provider()
    is_fallback_mode = provider.provider_name == "heuristic"
    return FinancialReportResponse(
        reportId=str(uuid.uuid4()),
        generatedAt=datetime.now(timezone.utc).isoformat(),
        farmName=calc.farmName,
        calculations=calc,
        scenarioComparison=sim,
        report=report_sections,
        citations=retrieved,
        provider=provider.provider_name if not is_fallback_mode else "heuristic_fallback",
        modelName=provider.model_name,
        isFallback=is_fallback_mode,
        disclaimer=calc.scientificAndLegalBoundary,
    )


@router.post("/api/financial/parse-transaction", response_model=NaturalLanguageTransactionProposal)
def financial_parse_transaction(
    req: NaturalLanguageTransactionRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> NaturalLanguageTransactionProposal:
    verify_key_if_configured(x_internal_api_key)
    return parse_natural_language_transaction(req.text)


@router.post("/api/carbon/integrity-check", response_model=CarbonIntegrityCheckResponse)
def carbon_integrity_check(
    req: CarbonIntegrityCheckRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> CarbonIntegrityCheckResponse:
    verify_key_if_configured(x_internal_api_key)
    return evaluate_carbon_integrity(req)


@router.post("/api/carbon/proposal-check", response_model=ProposalFraudCheckResponse)
def carbon_proposal_check(
    req: ProposalFraudCheckRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> ProposalFraudCheckResponse:
    verify_key_if_configured(x_internal_api_key)
    return evaluate_investment_proposal(req)


@router.get("/internal/v1/admin/ai-provider")
def get_admin_ai_provider(x_internal_api_key: str | None = Header(default=None)) -> dict[str, Any]:
    verify_key_if_configured(x_internal_api_key)
    return ai_runtime_config.get_public_status()


@router.post("/internal/v1/admin/ai-provider")
def update_admin_ai_provider(
    req: UpdateAiProviderRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> dict[str, Any]:
    verify_key_if_configured(x_internal_api_key)
    return ai_runtime_config.update(req)


@router.post("/internal/v1/admin/ai-provider/test")
def test_admin_ai_provider(
    req: TestAiProviderRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> dict[str, Any]:
    verify_key_if_configured(x_internal_api_key)
    return ai_runtime_config.test_connection(req)
