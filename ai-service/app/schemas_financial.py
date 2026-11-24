from __future__ import annotations

from typing import Literal, Optional
from pydantic import BaseModel, Field, model_validator


DataCategory = Literal["ACTUAL_DATA", "USER_ASSUMPTION", "MODEL_ESTIMATE", "HYPOTHETICAL_SCENARIO"]


class TaggedValue(BaseModel):
    label: str
    value: float
    unit: str = "VND"
    dataCategory: DataCategory
    note: str = ""


class InvestmentCostInput(BaseModel):
    areaHa: float = Field(gt=0, description="Diện tích vườn dừa (ha)")
    landPrepAndSeedlingsVnd: float = Field(ge=0, default=0.0)
    irrigationAndEquipmentVnd: float = Field(ge=0, default=0.0)
    mrvBaselineAndAuditBudgetVnd: float = Field(ge=0, default=0.0)
    annualCareAndFertilizerPerHaVnd: float = Field(ge=0, default=0.0)
    annualLaborAndHarvestPerHaVnd: float = Field(ge=0, default=0.0)
    annualMonitoringAndAdminVnd: float = Field(ge=0, default=0.0)
    horizonYears: int = Field(ge=1, le=30, default=5)
    isActualData: bool = Field(
        default=False,
        description="True nếu là số liệu sổ sách thực tế của nông hộ; False nếu là giả định lập kế hoạch.",
    )


class RevenueInput(BaseModel):
    areaHa: float = Field(gt=0)
    annualCoconutYieldUnitsPerHa: float = Field(ge=0, default=0.0, description="Sản lượng quả/kg mỗi ha mỗi năm")
    coconutUnitPriceVnd: float = Field(ge=0, default=0.0, description="Đơn giá bán nông sản dừa (VND/đơn vị)")
    annualByProductRevenuePerHaVnd: float = Field(ge=0, default=0.0, description="Doanh thu phụ phẩm gáo/xơ/mụn (VND/ha/năm)")
    annualGreenSupportGrantVnd: float = Field(ge=0, default=0.0, description="Hỗ trợ/tín dụng ưu đãi xanh thực nhận hoặc dự kiến")
    # Optional hypothetical carbon scenario — ONLY used if BOTH referenceTonnes and assumedPricePerTonneVnd > 0
    assumedEligibleCarbonTonnesTotal: float = Field(ge=0, default=0.0)
    assumedCarbonPricePerTonneVnd: float = Field(ge=0, default=0.0)
    carbonRealizationProbability: float = Field(ge=0.0, le=1.0, default=0.0)


class FinancialCalculationRequest(BaseModel):
    farmName: str = "Vườn dừa CocoCarbon"
    investment: InvestmentCostInput
    revenue: RevenueInput
    discountRate: float = Field(ge=0.0, le=0.5, default=0.08)
    actualRecordedExpenseVnd: Optional[float] = Field(default=None, ge=0)
    actualRecordedRevenueVnd: Optional[float] = Field(default=None, ge=0)


class InvestmentCostBreakdown(BaseModel):
    initialCapexVnd: float
    annualOpexVnd: float
    totalOpexOverHorizonVnd: float
    totalInvestmentCostVnd: float
    capexPerHaVnd: float
    annualOpexPerHaVnd: float
    mrvPrepSharePercent: float
    dataCategory: DataCategory


class RevenueBreakdown(BaseModel):
    annualCoconutHarvestRevenueVnd: float
    annualByProductRevenueVnd: float
    annualGreenSupportVnd: float
    annualTotalAgriRevenueVnd: float
    totalAgriRevenueOverHorizonVnd: float
    hypotheticalCarbonRevenueTotalVnd: float
    carbonScenarioIncluded: bool
    carbonScenarioDisclaimer: str
    dataCategory: DataCategory


class ProfitAndRoiMetrics(BaseModel):
    annualNetAgriProfitVnd: float
    totalNetAgriProfitOverHorizonVnd: float
    totalNetProfitWithCarbonScenarioVnd: float
    grossProfitMarginPercent: float
    roiPercentAgriOnly: float
    roiPercentWithCarbonScenario: Optional[float] = None
    benefitCostRatio: float
    paybackPeriodYears: Optional[float] = None
    npvAgriOnlyVnd: float


class CashFlowYearRow(BaseModel):
    year: int
    capexVnd: float
    opexVnd: float
    agriRevenueVnd: float
    hypotheticalCarbonRevenueVnd: float
    netCashFlowAgriOnlyVnd: float
    cumulativeCashFlowAgriOnlyVnd: float
    netCashFlowWithCarbonScenarioVnd: float
    cumulativeCashFlowWithCarbonScenarioVnd: float


class FinancialCalculationResult(BaseModel):
    farmName: str
    areaHa: float
    horizonYears: int
    discountRate: float
    costBreakdown: InvestmentCostBreakdown
    revenueBreakdown: RevenueBreakdown
    profitAndRoi: ProfitAndRoiMetrics
    cashFlowSchedule: list[CashFlowYearRow]
    breakEvenYearAgriOnly: Optional[int] = None
    actualVsEstimateNotes: list[str] = Field(default_factory=list)
    assumptions: list[str] = Field(default_factory=list)
    scientificAndLegalBoundary: str = (
        "Các phép tính tài chính được thực hiện bởi Python Financial Engine dựa trên dữ liệu người dùng nhập "
        "và giả định kịch bản được gắn nhãn rõ. Nếu không nhập đơn giá carbon giả định, kịch bản carbon bằng 0. "
        "Ước tính carbon CTU 2015 KHÔNG phải tín chỉ đã phát hành và KHÔNG phải doanh thu được bảo đảm."
    )


class ScenarioVariation(BaseModel):
    name: str
    priceMultiplier: float = Field(gt=0, le=5.0, default=1.0)
    yieldMultiplier: float = Field(gt=0, le=5.0, default=1.0)
    costMultiplier: float = Field(gt=0, le=5.0, default=1.0)
    assumedCarbonPricePerTonneVnd: Optional[float] = Field(default=None, ge=0)
    carbonRealizationProbability: Optional[float] = Field(default=None, ge=0.0, le=1.0)


class ScenarioSimulationRequest(BaseModel):
    baseInput: FinancialCalculationRequest
    scenarios: list[ScenarioVariation] = Field(default_factory=list)


class ScenarioComparisonItem(BaseModel):
    scenarioName: str
    totalCostVnd: float
    totalAgriRevenueVnd: float
    hypotheticalCarbonRevenueVnd: float
    netProfitAgriOnlyVnd: float
    netProfitWithCarbonVnd: float
    roiPercentAgriOnly: float
    roiPercentWithCarbon: Optional[float]
    paybackYears: Optional[float]
    npvAgriOnlyVnd: float
    assumptionsSummary: str


class ScenarioSimulationResult(BaseModel):
    baseSummary: ScenarioComparisonItem
    scenarios: list[ScenarioComparisonItem]
    sensitivityNotes: list[str]
    disclaimer: str


class FinancialAnalysisRequest(BaseModel):
    calculationInput: FinancialCalculationRequest
    includeScenarios: bool = True
    includeCarbonSensitivity: bool = False
    question: Optional[str] = None


class FinancialReportSection(BaseModel):
    executiveSummary: str
    investmentOverview: str
    revenueAnalysis: str
    costAnalysis: str
    roiAnalysis: str
    cashFlowAnalysis: str
    riskAssessment: list[str]
    recommendations: list[str]
    dataSourcesAndAssumptions: list[str]


class FinancialReportResponse(BaseModel):
    reportId: str
    generatedAt: str
    farmName: str
    calculations: FinancialCalculationResult
    scenarioComparison: Optional[ScenarioSimulationResult] = None
    report: FinancialReportSection
    citations: list[dict] = Field(default_factory=list)
    provider: str
    modelName: str
    isFallback: bool = False
    disclaimer: str
