from types import SimpleNamespace
from unittest.mock import MagicMock
from fastapi.testclient import TestClient
from app.main import app
from app.providers.gemini_provider import GeminiInteractionsProvider
from app.services.intent_classifier import classify_intent
from app.services.rag_knowledge import retrieve_relevant_sources

client = TestClient(app)


def _sample_financial_payload(include_carbon: bool = False):
    return {
        "farmName": "Vườn Dừa Ta Giồng Trôm",
        "discountRate": 0.08,
        "actualRecordedExpenseVnd": 4500000,
        "actualRecordedRevenueVnd": 28000000,
        "investment": {
            "areaHa": 2.0,
            "horizonYears": 5,
            "landPrepAndSeedlingsVnd": 30000000,
            "irrigationAndEquipmentVnd": 15000000,
            "mrvBaselineAndAuditBudgetVnd": 5000000,
            "annualCareAndFertilizerPerHaVnd": 8000000,
            "annualLaborAndHarvestPerHaVnd": 6000000,
            "annualMonitoringAndAdminVnd": 2000000,
            "isActualData": False,
        },
        "revenue": {
            "areaHa": 2.0,
            "annualCoconutYieldUnitsPerHa": 12000,
            "coconutUnitPriceVnd": 5000,
            "annualByProductRevenuePerHaVnd": 3000000,
            "annualGreenSupportGrantVnd": 0,
            "assumedEligibleCarbonTonnesTotal": 150.4872 if include_carbon else 0.0,
            "assumedCarbonPricePerTonneVnd": 250000.0 if include_carbon else 0.0,
            "carbonRealizationProbability": 0.5 if include_carbon else 0.0,
        },
    }


def test_financial_calculate_roi_cashflow_and_boundary_cases():
    resp = client.post("/api/financial/calculate", json=_sample_financial_payload(include_carbon=False))
    assert resp.status_code == 200
    body = resp.json()

    # CAPEX = 30M + 15M + 5M = 50M
    assert body["costBreakdown"]["initialCapexVnd"] == 50_000_000.0
    # Annual OPEX = (8M + 6M) * 2 + 2M = 30M -> 5y = 150M; Total = 200M
    assert body["costBreakdown"]["annualOpexVnd"] == 30_000_000.0
    assert body["costBreakdown"]["totalInvestmentCostVnd"] == 200_000_000.0
    # Annual Rev = (2 * 12000 * 5000) + (2 * 3M) = 120M + 6M = 126M/yr -> 5y = 630M
    assert body["revenueBreakdown"]["annualTotalAgriRevenueVnd"] == 126_000_000.0
    assert body["revenueBreakdown"]["hypotheticalCarbonRevenueTotalVnd"] == 0.0
    assert body["revenueBreakdown"]["carbonScenarioIncluded"] is False
    # Net profit = 630M - 200M = 430M; ROI = 430 / 200 * 100 = 215.0%
    assert body["profitAndRoi"]["totalNetAgriProfitOverHorizonVnd"] == 430_000_000.0
    assert body["profitAndRoi"]["roiPercentAgriOnly"] == 215.0
    assert len(body["cashFlowSchedule"]) == 6  # Year 0..5
    assert body["breakEvenYearAgriOnly"] == 1

    # Invalid negative area must return 422
    bad = _sample_financial_payload()
    bad["investment"]["areaHa"] = -1.0
    assert client.post("/api/financial/calculate", json=bad).status_code == 422


def test_financial_calculate_zero_cost_and_negative_profit_edge_cases():
    zero_payload = {
        "farmName": "Edge Case Zero Cost",
        "investment": {"areaHa": 1.0, "horizonYears": 1},
        "revenue": {"areaHa": 1.0, "annualCoconutYieldUnitsPerHa": 0, "coconutUnitPriceVnd": 0},
    }
    resp = client.post("/api/financial/calculate", json=zero_payload)
    assert resp.status_code == 200
    assert resp.json()["profitAndRoi"]["roiPercentAgriOnly"] == 0.0

    # With explicit carbon assumption enabled
    with_carbon = client.post("/api/financial/calculate", json=_sample_financial_payload(include_carbon=True))
    assert with_carbon.status_code == 200
    c_body = with_carbon.json()
    assert c_body["revenueBreakdown"]["carbonScenarioIncluded"] is True
    # 150.4872 * 250000 * 0.5 = 18,810,900 VND
    assert c_body["revenueBreakdown"]["hypotheticalCarbonRevenueTotalVnd"] == 18_810_900.0
    assert c_body["profitAndRoi"]["roiPercentWithCarbonScenario"] > c_body["profitAndRoi"]["roiPercentAgriOnly"]


def test_scenario_simulation_and_nine_section_financial_report():
    sim_resp = client.post("/api/financial/scenarios", json={"baseInput": _sample_financial_payload(), "scenarios": []})
    assert sim_resp.status_code == 200
    sim = sim_resp.json()
    assert len(sim["scenarios"]) == 2
    assert sim["scenarios"][0]["roiPercentAgriOnly"] < sim["baseSummary"]["roiPercentAgriOnly"]

    rep_resp = client.post(
        "/api/financial/reports",
        json={"calculationInput": _sample_financial_payload(), "includeScenarios": True},
    )
    assert rep_resp.status_code == 200
    rep = rep_resp.json()
    for section in [
        "executiveSummary",
        "investmentOverview",
        "revenueAnalysis",
        "costAnalysis",
        "roiAnalysis",
        "cashFlowAnalysis",
        "riskAssessment",
        "recommendations",
        "dataSourcesAndAssumptions",
    ]:
        assert section in rep["report"] and rep["report"][section]


def test_bilingual_intent_classification_and_multicategory_rag():
    assert classify_intent("Tính ROI và dòng tiền 5 năm cho vườn 2 ha", has_financial_payload=True).intent == "financial_calculation"
    assert classify_intent("Simulate price drop scenario for coconut farm").intent == "scenario_simulation"
    assert classify_intent("Generate executive financial report").intent == "financial_report"
    assert classify_intent("Check fraud and carbon integrity risks").intent == "carbon_integrity"

    fin_docs = retrieve_relevant_sources("ROI cash flow CAPEX OPEX", category="financial_management")
    assert len(fin_docs) >= 1 and fin_docs[0]["relevanceScore"] > 0

    agri_docs = retrieve_relevant_sources("phân bón hữu cơ mụn dừa thu hoạch", category="agri_operations")
    assert len(agri_docs) >= 1


def test_gemini_provider_tool_calling_and_fallback_behavior():
    # Mock Gemini Interactions API two-turn function calling:
    # Turn 1 returns function_call step -> executes calculate_financial_metrics -> Turn 2 returns grounded text
    fc_step = SimpleNamespace(
        type="function_call",
        id="fc_123",
        name="calculate_financial_metrics",
        arguments={"areaHa": 1.0, "horizonYears": 3, "annualCoconutYieldUnitsPerHa": 10000, "coconutUnitPriceVnd": 5000},
        model_dump=lambda: {
            "type": "function_call",
            "id": "fc_123",
            "name": "calculate_financial_metrics",
            "arguments": {"areaHa": 1.0, "horizonYears": 3, "annualCoconutYieldUnitsPerHa": 10000, "coconutUnitPriceVnd": 5000},
        },
    )
    turn1 = SimpleNamespace(steps=[fc_step], output_text=None)
    turn2 = SimpleNamespace(
        steps=[SimpleNamespace(type="model_output", model_dump=lambda: {"type": "model_output"})],
        output_text="[Ước tính] Tổng doanh thu 3 năm = 150,000,000 VND theo Python Financial Engine [GF1].",
    )

    provider = GeminiInteractionsProvider(api_key="dummy-test-key", max_retries=1)
    provider._client = MagicMock()
    provider._client.interactions.create.side_effect = [turn1, turn2]

    ans, traces, is_fb = provider.run_assistant_with_tools(
        role="Farmer",
        question="Tính doanh thu 3 năm cho 1 ha dừa",
        intent="financial_calculation",
        language="vi",
        session_history=[],
        context_data={"areaHa": 1.0},
        retrieved_sources=[],
    )
    assert is_fb is False
    assert len(traces) == 1 and traces[0]["tool"] == "calculate_financial_metrics" and traces[0]["ok"] is True
    assert "150,000,000" in ans

    # Simulate Gemini API exception -> retries then gracefully falls back to HeuristicFallbackProvider
    provider._client.interactions.create.side_effect = TimeoutError("Simulated Gemini outage")
    fb_ans, fb_traces, fb_flag = provider.run_assistant_with_tools(
        role="Farmer",
        question="Tính ROI cho 1 ha",
        intent="financial_calculation",
        language="vi",
        session_history=[],
        context_data={"areaHa": 1.0, "annualCoconutYieldUnitsPerHa": 10000, "coconutUnitPriceVnd": 5000},
        retrieved_sources=[],
    )
    assert fb_flag is True
    assert len(fb_traces) == 1
    assert "Python Financial Engine" in fb_ans


def test_api_ai_assistant_session_context_and_analyze_endpoint():
    chat1 = client.post(
        "/api/ai/assistant",
        json={
            "sessionId": "sess-e2e-1",
            "role": "Farmer",
            "question": "Tính ROI và dòng tiền cho vườn mẫu 2 ha",
            "financialInput": _sample_financial_payload(),
        },
    )
    assert chat1.status_code == 200
    b1 = chat1.json()
    assert b1["intent"] == "financial_calculation"
    assert b1["financialResult"]["profitAndRoi"]["roiPercentAgriOnly"] == 215.0

    # Second turn reuses session financial input for scenario simulation
    chat2 = client.post(
        "/api/ai/assistant",
        json={
            "sessionId": "sess-e2e-1",
            "role": "Farmer",
            "question": "Hãy mô phỏng kịch bản nếu giá dừa giảm",
        },
    )
    assert chat2.status_code == 200
    assert chat2.json()["intent"] == "scenario_simulation"
    assert chat2.json()["scenarioResult"] is not None

    an_resp = client.post(
        "/api/financial/analyze",
        json={"calculationInput": _sample_financial_payload(), "includeScenarios": True},
    )
    assert an_resp.status_code == 200
    assert "aiNarrative" in an_resp.json()


def test_carbon_integrity_separates_missing_docs_from_fraud_risk():
    # Case 1: Incomplete documents ONLY (no fraud signals) -> low carbonIntegrityRiskScore, low documentCompletenessScore
    incomplete_resp = client.post(
        "/api/carbon/integrity-check",
        json={
            "farmName": "Vườn Thiếu Giấy Tờ",
            "areaHa": 1.0,
            "treeCount": 200,
            "ageYears": 10,
            "hasLandDocument": False,
            "hasFieldPhotos": False,
            "hasOwnerConsent": False,
        },
    )
    assert incomplete_resp.status_code == 200
    inc = incomplete_resp.json()
    assert inc["documentCompletenessScore"] == 0.0
    assert inc["carbonIntegrityRiskScore"] == 0.0
    assert inc["integrityStatus"] == "IncompleteDocuments_NotFraud"

    # Case 2: Fraudulent greenwashing claims + density outlier + duplicate GPS -> HighIntegrityRisk_Flagged
    fraud_resp = client.post(
        "/api/carbon/integrity-check",
        json={
            "farmName": "Vườn Gian Lận",
            "areaHa": 1.0,
            "treeCount": 420,
            "ageYears": 10,
            "latitude": 10.1652,
            "longitude": 106.4621,
            "knownCoordinates": [{"farmId": "other-1", "latitude": 10.16521, "longitude": 106.46211}],
            "declaredTextClaims": "Hấp thụ 75 tCO2/ha/năm và cam kết lợi nhuận chắc chắn, đã được Verra phát hành.",
            "hasLandDocument": True,
            "hasFieldPhotos": True,
            "hasOwnerConsent": True,
        },
    )
    assert fraud_resp.status_code == 200
    fr = fraud_resp.json()
    assert fr["carbonIntegrityRiskScore"] >= 60.0
    assert fr["riskLevel"] == "High"
    assert fr["integrityStatus"] == "HighIntegrityRisk_Flagged"
