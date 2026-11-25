from __future__ import annotations

from collections import defaultdict
from datetime import datetime, timezone
import re
import unicodedata
import uuid
from app.schemas_financial import (
    ActualLedgerEntryInput,
    ActualVsBudgetSummary,
    CashFlowYearRow,
    FinancialAlertItem,
    FinancialCalculationRequest,
    FinancialCalculationResult,
    FinancialReportResponse,
    FinancialReportSection,
    InvestmentCostBreakdown,
    InvestmentCostInput,
    NaturalLanguageTransactionProposal,
    PeriodAggregations,
    PeriodLedgerSummary,
    ProfitAndRoiMetrics,
    RevenueBreakdown,
    RevenueInput,
    ScenarioComparisonItem,
    ScenarioSimulationRequest,
    ScenarioSimulationResult,
    ScenarioVariation,
)


def calculate_investment_cost(inv: InvestmentCostInput) -> InvestmentCostBreakdown:
    """Deterministic Investment & Operating Cost Calculator."""
    initial_capex = round(
        inv.landPrepAndSeedlingsVnd
        + inv.irrigationAndEquipmentVnd
        + inv.mrvBaselineAndAuditBudgetVnd,
        2,
    )
    annual_opex = round(
        (inv.annualCareAndFertilizerPerHaVnd + inv.annualLaborAndHarvestPerHaVnd) * inv.areaHa
        + inv.annualMonitoringAndAdminVnd,
        2,
    )
    total_opex = round(annual_opex * inv.horizonYears, 2)
    total_cost = round(initial_capex + total_opex, 2)
    mrv_total = inv.mrvBaselineAndAuditBudgetVnd + (inv.annualMonitoringAndAdminVnd * inv.horizonYears)
    mrv_share = round((mrv_total / total_cost) * 100.0, 2) if total_cost > 0 else 0.0

    return InvestmentCostBreakdown(
        initialCapexVnd=initial_capex,
        annualOpexVnd=annual_opex,
        totalOpexOverHorizonVnd=total_opex,
        totalInvestmentCostVnd=total_cost,
        capexPerHaVnd=round(initial_capex / inv.areaHa, 2),
        annualOpexPerHaVnd=round(annual_opex / inv.areaHa, 2),
        mrvPrepSharePercent=mrv_share,
        dataCategory="ACTUAL_DATA" if inv.isActualData else "USER_ASSUMPTION",
    )


def estimate_revenue(rev: RevenueInput, horizon_years: int) -> RevenueBreakdown:
    """Deterministic Agricultural Revenue + Explicit Hypothetical Carbon Scenario Estimator."""
    annual_harvest = round(rev.areaHa * rev.annualCoconutYieldUnitsPerHa * rev.coconutUnitPriceVnd, 2)
    annual_byproduct = round(rev.areaHa * rev.annualByProductRevenuePerHaVnd, 2)
    annual_support = round(rev.annualGreenSupportGrantVnd, 2)
    annual_agri_total = round(annual_harvest + annual_byproduct + annual_support, 2)
    total_agri_horizon = round(annual_agri_total * horizon_years, 2)

    carbon_included = (
        rev.assumedEligibleCarbonTonnesTotal > 0
        and rev.assumedCarbonPricePerTonneVnd > 0
        and rev.carbonRealizationProbability > 0
    )
    hypothetical_carbon_total = (
        round(
            rev.assumedEligibleCarbonTonnesTotal
            * rev.assumedCarbonPricePerTonneVnd
            * rev.carbonRealizationProbability,
            2,
        )
        if carbon_included
        else 0.0
    )

    carbon_disclaimer = (
        f"Kịch bản giả định: {rev.assumedEligibleCarbonTonnesTotal} tCO2e × "
        f"{rev.assumedCarbonPricePerTonneVnd:,.0f} VND/t × xác suất thực hiện {rev.carbonRealizationProbability:.0%}. "
        "Đây là kịch bản minh họa có điều kiện, KHÔNG phải tín chỉ đã phát hành hay doanh thu bảo đảm."
        if carbon_included
        else "Chưa bật kịch bản doanh thu carbon giả định (chỉ tính doanh thu nông sản/phụ phẩm dừa và hỗ trợ xanh)."
    )

    return RevenueBreakdown(
        annualCoconutHarvestRevenueVnd=annual_harvest,
        annualByProductRevenueVnd=annual_byproduct,
        annualGreenSupportVnd=annual_support,
        annualTotalAgriRevenueVnd=annual_agri_total,
        totalAgriRevenueOverHorizonVnd=total_agri_horizon,
        hypotheticalCarbonRevenueTotalVnd=hypothetical_carbon_total,
        carbonScenarioIncluded=carbon_included,
        carbonScenarioDisclaimer=carbon_disclaimer,
        dataCategory="HYPOTHETICAL_SCENARIO" if carbon_included else "MODEL_ESTIMATE",
    )


def build_cash_flow_schedule(
    cost: InvestmentCostBreakdown,
    revenue: RevenueBreakdown,
    horizon_years: int,
    discount_rate: float,
) -> tuple[list[CashFlowYearRow], float, int | None, float | None]:
    """Build multi-year cash flow schedule, NPV (agri only), break-even year, and payback years."""
    rows: list[CashFlowYearRow] = []
    cum_agri = -cost.initialCapexVnd
    cum_with_carbon = -cost.initialCapexVnd
    npv_agri = -cost.initialCapexVnd
    break_even_year: int | None = None
    payback_years: float | None = None

    # Year 0 initial investment
    rows.append(
        CashFlowYearRow(
            year=0,
            capexVnd=cost.initialCapexVnd,
            opexVnd=0.0,
            agriRevenueVnd=0.0,
            hypotheticalCarbonRevenueVnd=0.0,
            netCashFlowAgriOnlyVnd=-cost.initialCapexVnd,
            cumulativeCashFlowAgriOnlyVnd=round(cum_agri, 2),
            netCashFlowWithCarbonScenarioVnd=-cost.initialCapexVnd,
            cumulativeCashFlowWithCarbonScenarioVnd=round(cum_with_carbon, 2),
        )
    )

    # Distribute optional hypothetical carbon scenario evenly only from year 2 onward (never year 0/1)
    carbon_years = max(1, horizon_years - 1)
    annual_hyp_carbon = (
        round(revenue.hypotheticalCarbonRevenueTotalVnd / carbon_years, 2)
        if revenue.carbonScenarioIncluded
        else 0.0
    )

    for yr in range(1, horizon_years + 1):
        opex = cost.annualOpexVnd
        agri_rev = revenue.annualTotalAgriRevenueVnd
        carbon_rev = annual_hyp_carbon if (revenue.carbonScenarioIncluded and (horizon_years == 1 or yr >= 2)) else 0.0

        net_agri = round(agri_rev - opex, 2)
        net_with_carbon = round(net_agri + carbon_rev, 2)

        prev_cum = cum_agri
        cum_agri = round(cum_agri + net_agri, 2)
        cum_with_carbon = round(cum_with_carbon + net_with_carbon, 2)

        if break_even_year is None and cum_agri >= 0:
            break_even_year = yr
            if net_agri > 0 and prev_cum < 0:
                payback_years = round((yr - 1) + abs(prev_cum) / net_agri, 2)
            else:
                payback_years = float(yr)

        npv_agri += net_agri / ((1.0 + discount_rate) ** yr)

        rows.append(
            CashFlowYearRow(
                year=yr,
                capexVnd=0.0,
                opexVnd=opex,
                agriRevenueVnd=agri_rev,
                hypotheticalCarbonRevenueVnd=carbon_rev,
                netCashFlowAgriOnlyVnd=net_agri,
                cumulativeCashFlowAgriOnlyVnd=cum_agri,
                netCashFlowWithCarbonScenarioVnd=net_with_carbon,
                cumulativeCashFlowWithCarbonScenarioVnd=cum_with_carbon,
            )
        )

    return rows, round(npv_agri, 2), break_even_year, payback_years


def _aggregate_ledger_periods(entries: list[ActualLedgerEntryInput]) -> PeriodAggregations:
    """Aggregate declared farm finance entries by month, quarter, and year."""
    if not entries:
        return PeriodAggregations()

    expense_types = {"CareExpense", "MrvPreparationExpense"}
    buckets: dict[str, dict[str, list[float]]] = {
        "month": defaultdict(lambda: [0.0, 0.0, 0.0]),
        "quarter": defaultdict(lambda: [0.0, 0.0, 0.0]),
        "year": defaultdict(lambda: [0.0, 0.0, 0.0]),
    }

    for entry in entries:
        dt_str = (entry.entryDate or "").strip()
        try:
            dt = datetime.fromisoformat(dt_str.replace("Z", "+00:00")) if dt_str else datetime.now(timezone.utc)
        except ValueError:
            dt = datetime.now(timezone.utc)

        m_key = dt.strftime("%Y-%m")
        q_num = ((dt.month - 1) // 3) + 1
        q_key = f"{dt.year}-Q{q_num}"
        y_key = f"{dt.year}"

        is_exp = entry.entryType in expense_types
        for scope, key in (("month", m_key), ("quarter", q_key), ("year", y_key)):
            buckets[scope][key][0 if is_exp else 1] += float(entry.amountVnd)
            buckets[scope][key][2] += 1.0

    def to_list(data: dict[str, list[float]]) -> list[PeriodLedgerSummary]:
        out: list[PeriodLedgerSummary] = []
        for k in sorted(data.keys(), reverse=True):
            exp, rev, cnt = data[k]
            out.append(
                PeriodLedgerSummary(
                    period=k,
                    expenseVnd=round(exp, 2),
                    revenueVnd=round(rev, 2),
                    netCashflowVnd=round(rev - exp, 2),
                    entryCount=int(cnt),
                )
            )
        return out

    return PeriodAggregations(
        byMonth=to_list(buckets["month"]),
        byQuarter=to_list(buckets["quarter"]),
        byYear=to_list(buckets["year"]),
    )


def _compute_actual_adjusted_financials(
    req: FinancialCalculationRequest,
    cost: InvestmentCostBreakdown,
    rev: RevenueBreakdown,
    annual_net_agri: float,
    total_net_agri: float,
    break_even_year: int | None,
) -> tuple[ActualVsBudgetSummary | None, list[CashFlowYearRow], PeriodAggregations, list[FinancialAlertItem]]:
    """Recalculate ROI & Cash Flow incorporating actual ledger records and generate budget/cashflow alerts."""
    period_agg = _aggregate_ledger_periods(req.actualEntries)

    exp_from_entries = sum(
        e.amountVnd for e in req.actualEntries if e.entryType in ("CareExpense", "MrvPreparationExpense")
    )
    rev_from_entries = sum(
        e.amountVnd for e in req.actualEntries if e.entryType not in ("CareExpense", "MrvPreparationExpense")
    )

    has_actuals = (
        req.actualRecordedExpenseVnd is not None
        or req.actualRecordedRevenueVnd is not None
        or bool(req.actualEntries)
    )
    alerts: list[FinancialAlertItem] = []

    if annual_net_agri < 0 or total_net_agri < 0 or break_even_year is None:
        alerts.append(
            FinancialAlertItem(
                code="PROJECTED_NEGATIVE_CASHFLOW_RISK",
                severity="high" if total_net_agri < 0 else "warning",
                title="Cảnh báo dòng tiền dự kiến âm hoặc chậm hoàn vốn",
                message=(
                    f"Lợi nhuận ròng nông nghiệp {req.investment.horizonYears} năm dự kiến là {total_net_agri:,.0f} VND; "
                    + ("chưa đạt điểm hòa vốn trong chu kỳ." if break_even_year is None else f"năm hòa vốn: Năm {break_even_year}.")
                ),
                recommendedAction="Rà soát chi phí phân bón/nhân công hoặc bổ sung nguồn thu phụ phẩm (gáo, xơ, mụn dừa) trước khi vay/đầu tư.",
            )
        )

    if total_net_agri < 0 and rev.hypotheticalCarbonRevenueTotalVnd > 0:
        alerts.append(
            FinancialAlertItem(
                code="CARBON_DEPENDENCY_WARNING",
                severity="warning",
                title="Rủi ro phụ thuộc doanh thu carbon giả định",
                message="Kịch bản chỉ có lãi khi cộng thêm doanh thu carbon giả định; nông sản nền đang lỗ.",
                recommendedAction="Không ký hợp đồng cam kết hay chi phí trước nếu dòng tiền nông sản cơ bản chưa tự chủ.",
            )
        )

    if not has_actuals:
        return None, [], period_agg, alerts

    actual_exp = float(req.actualRecordedExpenseVnd) if req.actualRecordedExpenseVnd is not None else exp_from_entries
    actual_rev = float(req.actualRecordedRevenueVnd) if req.actualRecordedRevenueVnd is not None else rev_from_entries
    actual_net = round(actual_rev - actual_exp, 2)

    planned_yr1_opex = round(cost.annualOpexVnd, 2)
    expense_variance = round(actual_exp - planned_yr1_opex, 2)
    utilization_pct = round((actual_exp / planned_yr1_opex) * 100.0, 2) if planned_yr1_opex > 0 else 0.0
    is_over_budget = planned_yr1_opex > 0 and actual_exp > planned_yr1_opex

    if is_over_budget:
        alerts.append(
            FinancialAlertItem(
                code="OVER_BUDGET_OPEX",
                severity="high" if utilization_pct >= 120.0 else "warning",
                title="Chi phí thực tế vượt ngân sách vận hành dự kiến",
                message=(
                    f"Chi thực tế ({actual_exp:,.0f} VND) vượt ngân sách vận hành năm ({planned_yr1_opex:,.0f} VND) "
                    f"+{expense_variance:,.0f} VND ({utilization_pct:.1f}%)."
                ),
                recommendedAction="Kiểm tra lại các khoản chi chăm sóc/MRV thực tế trong Sổ Thu Chi Vườn và điều chỉnh định mức phân bón/nhân công.",
            )
        )

    if actual_net < 0:
        alerts.append(
            FinancialAlertItem(
                code="ACTUAL_NEGATIVE_CASHFLOW",
                severity="high",
                title="Dòng tiền thực tế ghi nhận đang âm",
                message=f"Thu thực tế ({actual_rev:,.0f} VND) thấp hơn chi thực tế ({actual_exp:,.0f} VND), âm {abs(actual_net):,.0f} VND.",
                recommendedAction="Tạm hoãn chi đầu tư mở rộng không cấp thiết và ưu tiên thu hoạch/tiêu thụ phụ phẩm.",
            )
        )

    # Re-baseline cash flow schedule: Year 0 = initial CAPEX, Year 1 = ACTUAL recorded ledger totals, Year 2..N = remaining forecast
    adj_rows: list[CashFlowYearRow] = []
    cum_agri = -cost.initialCapexVnd
    cum_with_carbon = -cost.initialCapexVnd
    npv_agri = -cost.initialCapexVnd
    adj_be_year: int | None = None
    adj_payback: float | None = None

    adj_rows.append(
        CashFlowYearRow(
            year=0,
            capexVnd=cost.initialCapexVnd,
            opexVnd=0.0,
            agriRevenueVnd=0.0,
            hypotheticalCarbonRevenueVnd=0.0,
            netCashFlowAgriOnlyVnd=-cost.initialCapexVnd,
            cumulativeCashFlowAgriOnlyVnd=round(cum_agri, 2),
            netCashFlowWithCarbonScenarioVnd=-cost.initialCapexVnd,
            cumulativeCashFlowWithCarbonScenarioVnd=round(cum_with_carbon, 2),
        )
    )

    carbon_years = max(1, req.investment.horizonYears - 1)
    annual_hyp_carbon = (
        round(rev.hypotheticalCarbonRevenueTotalVnd / carbon_years, 2)
        if rev.carbonScenarioIncluded
        else 0.0
    )

    adj_total_opex = actual_exp
    adj_total_rev = actual_rev
    for yr in range(1, req.investment.horizonYears + 1):
        opex = actual_exp if yr == 1 else cost.annualOpexVnd
        agri_rev = actual_rev if yr == 1 else rev.annualTotalAgriRevenueVnd
        if yr >= 2:
            adj_total_opex += opex
            adj_total_rev += agri_rev

        carbon_rev = annual_hyp_carbon if (rev.carbonScenarioIncluded and (req.investment.horizonYears == 1 or yr >= 2)) else 0.0
        net_agri = round(agri_rev - opex, 2)
        net_with_carbon = round(net_agri + carbon_rev, 2)
        prev_cum = cum_agri
        cum_agri = round(cum_agri + net_agri, 2)
        cum_with_carbon = round(cum_with_carbon + net_with_carbon, 2)

        if adj_be_year is None and cum_agri >= 0:
            adj_be_year = yr
            if net_agri > 0 and prev_cum < 0:
                adj_payback = round((yr - 1) + abs(prev_cum) / net_agri, 2)
            else:
                adj_payback = float(yr)

        npv_agri += net_agri / ((1.0 + req.discountRate) ** yr)
        adj_rows.append(
            CashFlowYearRow(
                year=yr,
                capexVnd=0.0,
                opexVnd=round(opex, 2),
                agriRevenueVnd=round(agri_rev, 2),
                hypotheticalCarbonRevenueVnd=carbon_rev,
                netCashFlowAgriOnlyVnd=net_agri,
                cumulativeCashFlowAgriOnlyVnd=cum_agri,
                netCashFlowWithCarbonScenarioVnd=net_with_carbon,
                cumulativeCashFlowWithCarbonScenarioVnd=cum_with_carbon,
            )
        )

    adj_total_cost = round(cost.initialCapexVnd + adj_total_opex, 2)
    adj_total_rev = round(adj_total_rev, 2)
    adj_net_profit = round(adj_total_rev - adj_total_cost, 2)
    adj_roi = round((adj_net_profit / adj_total_cost) * 100.0, 2) if adj_total_cost > 0 else 0.0

    summary_txt = (
        f"Năm 1 sử dụng số liệu thực tế Sổ Thu Chi (Chi={actual_exp:,.0f} đ vs ngân sách năm {planned_yr1_opex:,.0f} đ, "
        f"Thu={actual_rev:,.0f} đ), cập nhật ROI hiệu chỉnh đạt {adj_roi:.2f}%."
    )

    actual_vs_budget = ActualVsBudgetSummary(
        hasActualData=True,
        actualRecordedExpenseVnd=round(actual_exp, 2),
        actualRecordedRevenueVnd=round(actual_rev, 2),
        actualNetCashflowVnd=actual_net,
        plannedYear1BudgetVnd=planned_yr1_opex,
        expenseVarianceVnd=expense_variance,
        expenseUtilizationPercent=utilization_pct,
        isOverBudget=is_over_budget,
        adjustedTotalCostVnd=adj_total_cost,
        adjustedTotalAgriRevenueVnd=adj_total_rev,
        adjustedNetProfitAgriOnlyVnd=adj_net_profit,
        adjustedRoiPercentAgriOnly=adj_roi,
        adjustedPaybackPeriodYears=adj_payback,
        adjustedNpvAgriOnlyVnd=round(npv_agri, 2),
        comparisonSummary=summary_txt,
    )
    return actual_vs_budget, adj_rows, period_agg, alerts


def parse_natural_language_transaction(text: str) -> NaturalLanguageTransactionProposal:
    """Parse Vietnamese/English financial note into a structured FarmFinancialEntry proposal requiring user confirmation."""
    raw = unicodedata.normalize("NFC", text or "").strip()
    lowered = raw.lower()

    # Extract amount with VND unit multipliers (triệu/tr/trieu, nghìn/nghin/ngàn/k)
    amount = 0.0
    m = re.search(
        r"(\d+(?:[.,]\d+)?(?:\s*\d{3})*)\s*(triệu|trieu|tr|nghìn|nghin|ngàn|ngan|k|vnđ|vnd|đồng|dong|đ)?",
        lowered,
    )
    if m:
        num_str = m.group(1).replace(" ", "")
        unit = (m.group(2) or "").strip()
        if unit in ("triệu", "trieu", "tr"):
            num_norm = num_str.replace(",", ".")
            try:
                amount = float(num_norm) * 1_000_000.0
            except ValueError:
                amount = 0.0
        elif unit in ("nghìn", "nghin", "ngàn", "ngan", "k"):
            num_norm = num_str.replace(",", ".")
            try:
                amount = float(num_norm) * 1_000.0
            except ValueError:
                amount = 0.0
        else:
            # Plain VND figure like 3.500.000 or 3,500,000 or 2500000
            cleaned = num_str.replace(".", "").replace(",", "")
            try:
                amount = float(cleaned)
            except ValueError:
                amount = 0.0

    # Determine direction & entry type
    is_mrv = any(k in lowered for k in ("mrv", "đo đạc", "hồ sơ carbon", "gps", "kiểm kê", "thẩm định", "baseline"))
    is_byproduct = any(k in lowered for k in ("gáo", "xơ dừa", "mụn dừa", "phụ phẩm", "by-product", "byproduct"))
    is_grant = any(k in lowered for k in ("hỗ trợ", "tài trợ", "vay ưu đãi", "tín dụng xanh", "grant", "loan"))
    is_revenue = any(k in lowered for k in ("bán", "thu tiền", "doanh thu", "tiền dừa", "xuất bán", "sold", "revenue", "harvest", "income"))

    if is_mrv and not is_revenue:
        entry_type = "MrvPreparationExpense"
        direction = "expense"
        category = "Chi phí chuẩn bị hồ sơ & đo đạc MRV"
    elif is_grant:
        entry_type = "GreenSupportGrantOrLoan"
        direction = "revenue"
        category = "Hỗ trợ / Tín dụng ưu đãi xanh"
    elif is_byproduct and (is_revenue or not any(k in lowered for k in ("chi", "mua", "trả", "spent", "paid"))):
        entry_type = "ByProductRevenue"
        direction = "revenue"
        category = "Doanh thu phụ phẩm dừa (xơ/mụn/gáo)"
    elif is_revenue:
        entry_type = "CoconutHarvestRevenue"
        direction = "revenue"
        category = "Doanh thu thu hoạch dừa"
    else:
        entry_type = "CareExpense"
        direction = "expense"
        if any(k in lowered for k in ("phân", "fertilizer", "hữu cơ")):
            category = "Phân bón & chăm sóc hữu cơ"
        elif any(k in lowered for k in ("công", "thu hoạch", "labor", "nạo vét")):
            category = "Nhân công chăm sóc & thu hoạch"
        else:
            category = "Chi phí chăm sóc vườn dừa"

    clarification = None
    conf = 0.88
    if amount <= 0:
        clarification = "Chưa nhận diện được số tiền rõ ràng (VND); vui lòng nhập số tiền trước khi lưu."
        conf = 0.45

    return NaturalLanguageTransactionProposal(
        suggestedEntryType=entry_type,  # type: ignore[arg-type]
        category=category,
        amountVnd=round(amount, 2),
        entryDateIso=datetime.now(timezone.utc).isoformat(),
        notes=raw[:240],
        direction=direction,  # type: ignore[arg-type]
        confidence=conf,
        requiresUserConfirmation=True,
        clarificationNeeded=clarification,
        explanation=(
            f"Phân loại từ mô tả tự nhiên -> {entry_type} ({category}), số tiền {amount:,.0f} VND. "
            "Chỉ lưu vào Sổ Thu Chi Vườn khi người dùng bấm xác nhận."
        ),
    )


def run_financial_calculation(req: FinancialCalculationRequest) -> FinancialCalculationResult:
    """Master deterministic Financial Engine entry point."""
    cost = calculate_investment_cost(req.investment)
    rev = estimate_revenue(req.revenue, req.investment.horizonYears)
    rows, npv_agri, break_even_year, payback_years = build_cash_flow_schedule(
        cost, rev, req.investment.horizonYears, req.discountRate
    )

    annual_net_agri = round(rev.annualTotalAgriRevenueVnd - cost.annualOpexVnd, 2)
    total_net_agri = round(rev.totalAgriRevenueOverHorizonVnd - cost.totalInvestmentCostVnd, 2)
    total_net_with_carbon = round(total_net_agri + rev.hypotheticalCarbonRevenueTotalVnd, 2)

    margin = (
        round((total_net_agri / rev.totalAgriRevenueOverHorizonVnd) * 100.0, 2)
        if rev.totalAgriRevenueOverHorizonVnd > 0
        else 0.0
    )
    roi_agri = (
        round((total_net_agri / cost.totalInvestmentCostVnd) * 100.0, 2)
        if cost.totalInvestmentCostVnd > 0
        else 0.0
    )
    roi_with_carbon = (
        round((total_net_with_carbon / cost.totalInvestmentCostVnd) * 100.0, 2)
        if (rev.carbonScenarioIncluded and cost.totalInvestmentCostVnd > 0)
        else None
    )
    bcr = (
        round(rev.totalAgriRevenueOverHorizonVnd / cost.totalInvestmentCostVnd, 4)
        if cost.totalInvestmentCostVnd > 0
        else 0.0
    )

    profit_roi = ProfitAndRoiMetrics(
        annualNetAgriProfitVnd=annual_net_agri,
        totalNetAgriProfitOverHorizonVnd=total_net_agri,
        totalNetProfitWithCarbonScenarioVnd=total_net_with_carbon,
        grossProfitMarginPercent=margin,
        roiPercentAgriOnly=roi_agri,
        roiPercentWithCarbonScenario=roi_with_carbon,
        benefitCostRatio=bcr,
        paybackPeriodYears=payback_years,
        npvAgriOnlyVnd=npv_agri,
    )

    actual_vs_budget, adj_rows, period_agg, fin_alerts = _compute_actual_adjusted_financials(
        req, cost, rev, annual_net_agri, total_net_agri, break_even_year
    )

    actual_notes: list[str] = []
    if actual_vs_budget is not None:
        actual_notes.append(
            f"[DỮ LIỆU THỰC TẾ / ACTUAL_DATA] Sổ thu chi ghi nhận: Chi={actual_vs_budget.actualRecordedExpenseVnd:,.0f} VND, "
            f"Thu={actual_vs_budget.actualRecordedRevenueVnd:,.0f} VND, "
            f"Ròng thực tế={actual_vs_budget.actualNetCashflowVnd:,.0f} VND."
        )
        actual_notes.append(f"[SO SÁNH THỰC TẾ VS KẾ HOẠCH] {actual_vs_budget.comparisonSummary}")
    else:
        actual_notes.append(
            "[DỮ LIỆU THỰC TẾ] Chưa truyền số liệu sổ thu chi thực tế từ vườn; phép tính dùng dữ liệu đầu vào người dùng khai báo."
        )

    assumptions = [
        f"[GIẢ ĐỊNH] Diện tích={req.investment.areaHa} ha; Chu kỳ phân tích={req.investment.horizonYears} năm; Tỷ lệ chiết khấu={req.discountRate:.1%}/năm.",
        f"[GIẢ ĐỊNH] Sản lượng dừa={req.revenue.annualCoconutYieldUnitsPerHa:,.1f} đơn vị/ha/năm; Đơn giá={req.revenue.coconutUnitPriceVnd:,.0f} VND/đơn vị.",
        f"[GIẢ ĐỊNH] Phụ phẩm dừa={req.revenue.annualByProductRevenuePerHaVnd:,.0f} VND/ha/năm; Hỗ trợ xanh={req.revenue.annualGreenSupportGrantVnd:,.0f} VND/năm.",
    ]
    if rev.carbonScenarioIncluded:
        assumptions.append(f"[KỊCH BẢN GIẢ ĐỊNH CARBON] {rev.carbonScenarioDisclaimer}")
    else:
        assumptions.append("[RANH GIỚI CARBON] Không gán giá hoặc doanh thu tín chỉ carbon khi thiếu đơn giá và giả định xác suất thực hiện.")

    return FinancialCalculationResult(
        farmName=req.farmName,
        areaHa=req.investment.areaHa,
        horizonYears=req.investment.horizonYears,
        discountRate=req.discountRate,
        costBreakdown=cost,
        revenueBreakdown=rev,
        profitAndRoi=profit_roi,
        cashFlowSchedule=rows,
        actualAdjustedCashFlowSchedule=adj_rows,
        actualVsBudget=actual_vs_budget,
        actualPeriodAggregations=period_agg,
        financialAlerts=fin_alerts,
        breakEvenYearAgriOnly=break_even_year,
        actualVsEstimateNotes=actual_notes,
        assumptions=assumptions,
    )


def _to_comparison_item(name: str, res: FinancialCalculationResult, summary: str) -> ScenarioComparisonItem:
    return ScenarioComparisonItem(
        scenarioName=name,
        totalCostVnd=res.costBreakdown.totalInvestmentCostVnd,
        totalAgriRevenueVnd=res.revenueBreakdown.totalAgriRevenueOverHorizonVnd,
        hypotheticalCarbonRevenueVnd=res.revenueBreakdown.hypotheticalCarbonRevenueTotalVnd,
        netProfitAgriOnlyVnd=res.profitAndRoi.totalNetAgriProfitOverHorizonVnd,
        netProfitWithCarbonVnd=res.profitAndRoi.totalNetProfitWithCarbonScenarioVnd,
        roiPercentAgriOnly=res.profitAndRoi.roiPercentAgriOnly,
        roiPercentWithCarbon=res.profitAndRoi.roiPercentWithCarbonScenario,
        paybackYears=res.profitAndRoi.paybackPeriodYears,
        npvAgriOnlyVnd=res.profitAndRoi.npvAgriOnlyVnd,
        assumptionsSummary=summary,
    )


def run_scenario_simulation(req: ScenarioSimulationRequest) -> ScenarioSimulationResult:
    """Compare Base vs Conservative/Optimistic/Custom financial scenarios deterministically."""
    base_res = run_financial_calculation(req.baseInput)
    base_item = _to_comparison_item("Kịch bản Cơ sở (Base)", base_res, "100% giá bán, 100% sản lượng, 100% chi phí")

    variations = req.scenarios or [
        ScenarioVariation(
            name="Kịch bản Thận trọng (Giá -15%, Chi phí +10%)",
            priceMultiplier=0.85,
            yieldMultiplier=0.95,
            costMultiplier=1.10,
        ),
        ScenarioVariation(
            name="Kịch bản Tích cực (Sản lượng +15%, Phụ phẩm +15%)",
            priceMultiplier=1.05,
            yieldMultiplier=1.15,
            costMultiplier=1.00,
        ),
    ]

    results: list[ScenarioComparisonItem] = []
    for var in variations:
        cloned = req.baseInput.model_copy(deep=True)
        cloned.investment.annualCareAndFertilizerPerHaVnd *= var.costMultiplier
        cloned.investment.annualLaborAndHarvestPerHaVnd *= var.costMultiplier
        cloned.investment.annualMonitoringAndAdminVnd *= var.costMultiplier
        cloned.revenue.coconutUnitPriceVnd *= var.priceMultiplier
        cloned.revenue.annualCoconutYieldUnitsPerHa *= var.yieldMultiplier
        cloned.revenue.annualByProductRevenuePerHaVnd *= var.yieldMultiplier
        if var.assumedCarbonPricePerTonneVnd is not None:
            cloned.revenue.assumedCarbonPricePerTonneVnd = var.assumedCarbonPricePerTonneVnd
        if var.carbonRealizationProbability is not None:
            cloned.revenue.carbonRealizationProbability = var.carbonRealizationProbability

        sim_res = run_financial_calculation(cloned)
        summary = (
            f"Giá x{var.priceMultiplier:.2f}, Sản lượng x{var.yieldMultiplier:.2f}, Chi phí x{var.costMultiplier:.2f}"
            + (
                f", Carbon giả định {cloned.revenue.assumedCarbonPricePerTonneVnd:,.0f} VND/t"
                if sim_res.revenueBreakdown.carbonScenarioIncluded
                else ""
            )
        )
        results.append(_to_comparison_item(var.name, sim_res, summary))

    notes = [
        "So sánh kịch bản cho thấy mức độ nhạy cảm của ROI và NPV theo biến động giá dừa, sản lượng và chi phí chăm sóc.",
        "Hiệu quả kinh tế bền vững phải dựa trên dòng tiền nông sản/phụ phẩm dừa trước khi tính tới kịch bản carbon.",
    ]

    return ScenarioSimulationResult(
        baseSummary=base_item,
        scenarios=results,
        sensitivityNotes=notes,
        disclaimer=base_res.scientificAndLegalBoundary,
    )


def build_deterministic_financial_report(
    calc: FinancialCalculationResult,
    sim: ScenarioSimulationResult | None = None,
    citations: list[dict] | None = None,
) -> FinancialReportSection:
    """Build complete 9-section structured Financial Report strictly from Engine outputs."""
    c = calc.costBreakdown
    r = calc.revenueBreakdown
    p = calc.profitAndRoi

    payback_txt = f"{p.paybackPeriodYears} năm" if p.paybackPeriodYears is not None else "Chưa hoàn vốn trong chu kỳ khảo sát"
    exec_sum = (
        f"Báo cáo tài chính cho '{calc.farmName}' ({calc.areaHa} ha, chu kỳ {calc.horizonYears} năm): "
        f"Tổng chi phí đầu tư & vận hành ước tính {c.totalInvestmentCostVnd:,.0f} VND; "
        f"Doanh thu nông sản & phụ phẩm dừa {r.totalAgriRevenueOverHorizonVnd:,.0f} VND; "
        f"Lợi nhuận ròng nông nghiệp {p.totalNetAgriProfitOverHorizonVnd:,.0f} VND (ROI = {p.roiPercentAgriOnly:.1f}%, hoàn vốn: {payback_txt})."
    )
    if r.carbonScenarioIncluded:
        exec_sum += (
            f" Nếu xét thêm kịch bản carbon giả định ({r.hypotheticalCarbonRevenueTotalVnd:,.0f} VND, chưa phải tín chỉ phát hành), "
            f"ROI kịch bản đạt {p.roiPercentWithCarbonScenario:.1f}%."
        )
    if calc.actualVsBudget:
        avb = calc.actualVsBudget
        exec_sum += (
            f" Khi đưa số liệu thực tế từ Sổ Thu Chi vào tính lại (Chi thực tế {avb.actualRecordedExpenseVnd:,.0f} VND, "
            f"Thu thực tế {avb.actualRecordedRevenueVnd:,.0f} VND), ROI hiệu chỉnh đạt {avb.adjustedRoiPercentAgriOnly:.2f}%."
        )

    inv_overview = (
        f"Tổng vốn đầu tư ban đầu (CAPEX) là {c.initialCapexVnd:,.0f} VND ({c.capexPerHaVnd:,.0f} VND/ha); "
        f"chi phí vận hành định kỳ (OPEX) là {c.annualOpexVnd:,.0f} VND/năm ({c.totalOpexOverHorizonVnd:,.0f} VND/{calc.horizonYears} năm). "
        f"Ngân sách chuẩn bị hồ sơ/MRV chiếm {c.mrvPrepSharePercent:.1f}% tổng chi phí."
    )
    rev_analysis = (
        f"Doanh thu dừa chính đạt {r.annualCoconutHarvestRevenueVnd:,.0f} VND/năm, phụ phẩm (gáo/xơ/mụn) đạt "
        f"{r.annualByProductRevenueVnd:,.0f} VND/năm, hỗ trợ xanh {r.annualGreenSupportVnd:,.0f} VND/năm. "
        f"{r.carbonScenarioDisclaimer}"
    )
    cost_analysis = (
        f"Chi phí phân loại nguồn: {c.dataCategory}. OPEX bình quân {c.annualOpexPerHaVnd:,.0f} VND/ha/năm. "
        "Tách riêng chi phí canh tác cốt lõi và chi phí đo đạc/lập hồ sơ MRV giúp HTX chủ động cân đối dòng tiền."
    )
    roi_analysis = (
        f"ROI thuần nông nghiệp = {p.roiPercentAgriOnly:.2f}%, Tỷ lệ Lợi ích/Chi phí (BCR) = {p.benefitCostRatio:.2f}, "
        f"NPV (@{calc.discountRate:.1%}) = {p.npvAgriOnlyVnd:,.0f} VND, Biên lợi nhuận = {p.grossProfitMarginPercent:.1f}%."
    )
    cf_analysis = (
        f"Năm hòa vốn dòng tiền lũy kế (không tính carbon): "
        + (f"Năm thứ {calc.breakEvenYearAgriOnly}." if calc.breakEvenYearAgriOnly is not None else "Chưa đạt điểm hòa vốn trong chu kỳ đã chọn.")
    )

    risks = [
        "Rủi ro biến động giá dừa tươi/khô và năng suất theo thời tiết/xâm nhập mặn vùng ĐBSCL.",
        "Rủi ro chi phí MRV vượt ngân sách nếu diện tích vườn nhỏ lẻ không gom theo Hợp tác xã.",
        "Rủi ro pháp lý/khoa học nếu nhầm lẫn trữ lượng tham khảo CTU 2015 thành tín chỉ carbon bán được.",
    ]
    if p.totalNetAgriProfitOverHorizonVnd < 0:
        risks.insert(0, "CẢNH BÁO TÀI CHÍNH: Dòng tiền thuần nông nghiệp đang âm — không nên đầu tư dựa trên kỳ vọng thu từ carbon.")
    for alert in calc.financialAlerts:
        risks.insert(0, f"[{alert.code} - {alert.severity.upper()}] {alert.message}")

    recs = [
        "Ghi nhận định kỳ chi phí phân hữu cơ/mụn dừa và doanh thu nông sản thực tế trên Sổ Tài chính Xanh (USER_DECLARED).",
        "Ưu tiên tăng thu từ phụ phẩm dừa (xơ, mụn, gáo) để cải thiện biên lợi nhuận nền trước khi tham gia dự án carbon.",
        "Tham gia Hợp tác xã vùng dừa Bến Tre - Vĩnh Long để chia sẻ chi phí đo đạc/baseline/thẩm định (VVB).",
    ]

    sources_assumptions = calc.actualVsEstimateNotes + calc.assumptions
    if sim:
        sources_assumptions.extend(sim.sensitivityNotes)
    for cit in citations or []:
        sources_assumptions.append(f"[{cit.get('sourceCode', 'SRC')}] {cit.get('title', '')}")

    return FinancialReportSection(
        executiveSummary=exec_sum,
        investmentOverview=inv_overview,
        revenueAnalysis=rev_analysis,
        costAnalysis=cost_analysis,
        roiAnalysis=roi_analysis,
        cashFlowAnalysis=cf_analysis,
        riskAssessment=risks,
        recommendations=recs,
        dataSourcesAndAssumptions=sources_assumptions,
    )
