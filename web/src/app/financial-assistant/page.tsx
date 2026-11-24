"use client";

import { useEffect, useState } from "react";
import { BarChart3, Bot, Calculator, FileText, GitCompare, Send, TrendingUp } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { ScientificDisclaimerBanner } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { FarmDto } from "@/lib/types";

interface CashFlowRow {
  year: number;
  capexVnd: number;
  opexVnd: number;
  agriRevenueVnd: number;
  hypotheticalCarbonRevenueVnd: number;
  netCashFlowAgriOnlyVnd: number;
  cumulativeCashFlowAgriOnlyVnd: number;
  netCashFlowWithCarbonScenarioVnd: number;
  cumulativeCashFlowWithCarbonScenarioVnd: number;
}

interface CalcResult {
  farmName: string;
  areaHa: number;
  horizonYears: number;
  costBreakdown: {
    initialCapexVnd: number;
    annualOpexVnd: number;
    totalOpexOverHorizonVnd: number;
    totalInvestmentCostVnd: number;
    mrvPrepSharePercent: number;
    dataCategory: string;
  };
  revenueBreakdown: {
    annualCoconutHarvestRevenueVnd: number;
    annualByProductRevenueVnd: number;
    annualGreenSupportVnd: number;
    annualTotalAgriRevenueVnd: number;
    totalAgriRevenueOverHorizonVnd: number;
    hypotheticalCarbonRevenueTotalVnd: number;
    carbonScenarioIncluded: boolean;
    carbonScenarioDisclaimer: string;
  };
  profitAndRoi: {
    annualNetAgriProfitVnd: number;
    totalNetAgriProfitOverHorizonVnd: number;
    totalNetProfitWithCarbonScenarioVnd: number;
    grossProfitMarginPercent: number;
    roiPercentAgriOnly: number;
    roiPercentWithCarbonScenario?: number | null;
    benefitCostRatio: number;
    paybackPeriodYears?: number | null;
    npvAgriOnlyVnd: number;
  };
  cashFlowSchedule: CashFlowRow[];
  actualVsEstimateNotes: string[];
  assumptions: string[];
}

interface ScenarioItem {
  scenarioName: string;
  totalCostVnd: number;
  totalAgriRevenueVnd: number;
  hypotheticalCarbonRevenueVnd: number;
  netProfitAgriOnlyVnd: number;
  roiPercentAgriOnly: number;
  roiPercentWithCarbon?: number | null;
  paybackYears?: number | null;
  npvAgriOnlyVnd: number;
  assumptionsSummary: string;
}

interface ReportResponse {
  reportId: string;
  generatedAt: string;
  calculations: CalcResult;
  scenarioComparison?: { baseSummary: ScenarioItem; scenarios: ScenarioItem[] } | null;
  report: {
    executiveSummary: string;
    investmentOverview: string;
    revenueAnalysis: string;
    costAnalysis: string;
    roiAnalysis: string;
    cashFlowAnalysis: string;
    riskAssessment: string[];
    recommendations: string[];
    dataSourcesAndAssumptions: string[];
  };
  modelName: string;
  isFallback: boolean;
}

interface ChatMessage {
  role: "user" | "assistant";
  text: string;
  intent?: string;
  tools?: string[];
  citations?: { sourceCode: string; title: string }[];
}

export default function FinancialAssistantPage() {
  const [farms, setFarms] = useState<FarmDto[]>([]);
  const [farmId, setFarmId] = useState("");
  const [farmName, setFarmName] = useState("Vườn Dừa Ta Giồng Trôm");
  const [areaHa, setAreaHa] = useState("2.0");
  const [horizonYears, setHorizonYears] = useState("5");
  const [capexPrep, setCapexPrep] = useState("25000000");
  const [capexEquip, setCapexEquip] = useState("12000000");
  const [mrvBudget, setMrvBudget] = useState("5000000");
  const [carePerHa, setCarePerHa] = useState("7000000");
  const [laborPerHa, setLaborPerHa] = useState("5000000");
  const [yieldPerHa, setYieldPerHa] = useState("11000");
  const [unitPrice, setUnitPrice] = useState("5000");
  const [byProductPerHa, setByProductPerHa] = useState("3000000");
  const [includeCarbonHyp, setIncludeCarbonHyp] = useState(false);
  const [hypCarbonTonnes, setHypCarbonTonnes] = useState("150.4872");
  const [hypCarbonPrice, setHypCarbonPrice] = useState("250000");
  const [hypCarbonProb, setHypCarbonProb] = useState("0.5");

  const [calcResult, setCalcResult] = useState<CalcResult | null>(null);
  const [scenarios, setScenarios] = useState<{ baseSummary: ScenarioItem; scenarios: ScenarioItem[] } | null>(null);
  const [report, setReport] = useState<ReportResponse | null>(null);

  const [sessionId] = useState(() => (typeof crypto !== "undefined" ? crypto.randomUUID() : "web-session"));
  const [chatInput, setChatInput] = useState("Tính ROI, lợi nhuận và đề xuất tối ưu chi phí cho vườn dừa này?");
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  useEffect(() => {
    if (!getStoredToken()) return;
    apiFetch<FarmDto[]>("/api/v1/farms")
      .then((list) => {
        setFarms(list);
        if (list[0]) {
          setFarmId(list[0].id);
          setFarmName(list[0].name);
          setAreaHa(String(list[0].areaHa));
        }
      })
      .catch(() => {});
  }, []);

  function buildPayload() {
    const a = Number(areaHa) || 1.0;
    return {
      farmName,
      discountRate: 0.08,
      investment: {
        areaHa: a,
        horizonYears: Number(horizonYears) || 5,
        landPrepAndSeedlingsVnd: Number(capexPrep) || 0,
        irrigationAndEquipmentVnd: Number(capexEquip) || 0,
        mrvBaselineAndAuditBudgetVnd: Number(mrvBudget) || 0,
        annualCareAndFertilizerPerHaVnd: Number(carePerHa) || 0,
        annualLaborAndHarvestPerHaVnd: Number(laborPerHa) || 0,
        annualMonitoringAndAdminVnd: 1500000,
        isActualData: false,
      },
      revenue: {
        areaHa: a,
        annualCoconutYieldUnitsPerHa: Number(yieldPerHa) || 0,
        coconutUnitPriceVnd: Number(unitPrice) || 0,
        annualByProductRevenuePerHaVnd: Number(byProductPerHa) || 0,
        annualGreenSupportGrantVnd: 0,
        assumedEligibleCarbonTonnesTotal: includeCarbonHyp ? Number(hypCarbonTonnes) || 0 : 0,
        assumedCarbonPricePerTonneVnd: includeCarbonHyp ? Number(hypCarbonPrice) || 0 : 0,
        carbonRealizationProbability: includeCarbonHyp ? Number(hypCarbonProb) || 0 : 0,
      },
    };
  }

  async function runFinancialSuite() {
    setBusy(true);
    setErr(null);
    try {
      const payload = buildPayload();
      const qs = farmId ? `?farmId=${farmId}` : "";
      const [calc, sim, rep] = await Promise.all([
        apiFetch<CalcResult>(`/api/financial/calculate${qs}`, { method: "POST", body: JSON.stringify(payload) }),
        apiFetch<{ baseSummary: ScenarioItem; scenarios: ScenarioItem[] }>("/api/financial/scenarios", {
          method: "POST",
          body: JSON.stringify({ baseInput: payload, scenarios: [] }),
        }),
        apiFetch<ReportResponse>(`/api/financial/reports${qs}`, {
          method: "POST",
          body: JSON.stringify({ calculationInput: payload, includeScenarios: true }),
        }),
      ]);
      setCalcResult(calc);
      setScenarios(sim);
      setReport(rep);
    } catch (e) {
      setErr(e instanceof Error ? e.message : "Vui lòng đăng nhập trước khi sử dụng AI Financial Assistant.");
    } finally {
      setBusy(false);
    }
  }

  async function sendChat(e: React.FormEvent) {
    e.preventDefault();
    if (!chatInput.trim()) return;
    const q = chatInput.trim();
    setMessages((prev) => [...prev, { role: "user", text: q }]);
    setChatInput("");
    setBusy(true);
    setErr(null);
    try {
      const res = await apiFetch<{
        intent: string;
        answer: string;
        toolCallsExecuted: { tool: string }[];
        citations: { sourceCode: string; title: string }[];
        financialResult?: CalcResult | null;
        scenarioResult?: { baseSummary: ScenarioItem; scenarios: ScenarioItem[] } | null;
      }>("/api/ai/assistant", {
        method: "POST",
        body: JSON.stringify({
          sessionId,
          role: "Farmer",
          question: q,
          contextData: { farmId, farmName, areaHa: Number(areaHa) || 1 },
          financialInput: buildPayload(),
        }),
      });
      if (res.financialResult) setCalcResult(res.financialResult);
      if (res.scenarioResult) setScenarios(res.scenarioResult);
      setMessages((prev) => [
        ...prev,
        {
          role: "assistant",
          text: res.answer,
          intent: res.intent,
          tools: res.toolCallsExecuted.map((t) => t.tool),
          citations: res.citations,
        },
      ]);
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Lỗi gọi Trợ lý Tài chính AI.");
    } finally {
      setBusy(false);
    }
  }

  const maxAbsCash = calcResult
    ? Math.max(1, ...calcResult.cashFlowSchedule.map((r) => Math.max(Math.abs(r.cumulativeCashFlowAgriOnlyVnd), Math.abs(r.cumulativeCashFlowWithCarbonScenarioVnd))))
    : 1;

  return (
    <AppShell
      title="AI Financial Assistant & Phân tích Tài chính Xanh"
      subtitle="Gemini Tool Calling + Python Financial Engine + RAG đa nguồn • Tách bạch Dữ liệu thực tế, Giả định và Ước tính"
    >
      {err && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}

      <div className="space-y-6">
        {/* Input Form + Chat Split */}
        <div className="grid gap-6 lg:grid-cols-2">
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
            <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <Calculator className="h-5 w-5 text-[#15803d]" /> Tham số Đầu tư & Doanh thu (Python Engine)
            </h2>
            {farms.length > 0 && (
              <select
                value={farmId}
                onChange={(e) => {
                  const id = e.target.value;
                  setFarmId(id);
                  const f = farms.find((x) => x.id === id);
                  if (f) {
                    setFarmName(f.name);
                    setAreaHa(String(f.areaHa));
                  }
                }}
                className="w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-xs"
              >
                <option value="">-- Nhập tham số độc lập --</option>
                {farms.map((f) => (
                  <option key={f.id} value={f.id}>
                    {f.name} ({f.areaHa} ha — tự nạp số liệu thực tế từ Sổ Thu Chi)
                  </option>
                ))}
              </select>
            )}

            <div className="grid grid-cols-3 gap-2 text-xs">
              <div>
                <label className="font-semibold text-[#526158]">Diện tích (ha)</label>
                <input type="number" step="0.1" value={areaHa} onChange={(e) => setAreaHa(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">Chu kỳ (năm)</label>
                <input type="number" min="1" max="20" value={horizonYears} onChange={(e) => setHorizonYears(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">CAPEX Giống/Đất</label>
                <input type="number" value={capexPrep} onChange={(e) => setCapexPrep(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">CAPEX Tưới/Thiết bị</label>
                <input type="number" value={capexEquip} onChange={(e) => setCapexEquip(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">Ngân sách MRV</label>
                <input type="number" value={mrvBudget} onChange={(e) => setMrvBudget(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">Chăm sóc/ha/năm</label>
                <input type="number" value={carePerHa} onChange={(e) => setCarePerHa(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">Nhân công/ha/năm</label>
                <input type="number" value={laborPerHa} onChange={(e) => setLaborPerHa(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">Sản lượng/ha/năm</label>
                <input type="number" value={yieldPerHa} onChange={(e) => setYieldPerHa(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
              <div>
                <label className="font-semibold text-[#526158]">Giá dừa (VND/đv)</label>
                <input type="number" value={unitPrice} onChange={(e) => setUnitPrice(e.target.value)} className="mt-1 w-full rounded border border-[#d8e2da] px-2 py-1.5" />
              </div>
            </div>

            <div className="rounded-lg border border-amber-200 bg-amber-50/70 p-3 text-xs space-y-2">
              <label className="flex items-center gap-2 font-semibold text-amber-950">
                <input type="checkbox" checked={includeCarbonHyp} onChange={(e) => setIncludeCarbonHyp(e.target.checked)} />
                Mô phỏng thêm Kịch bản Carbon Giả định (tùy chọn — không phải tín chỉ phát hành)
              </label>
              {includeCarbonHyp && (
                <div className="grid grid-cols-3 gap-2">
                  <input type="number" value={hypCarbonTonnes} onChange={(e) => setHypCarbonTonnes(e.target.value)} placeholder="Tổng tCO2e giả định" className="rounded border bg-white px-2 py-1" />
                  <input type="number" value={hypCarbonPrice} onChange={(e) => setHypCarbonPrice(e.target.value)} placeholder="Giá giả định VND/t" className="rounded border bg-white px-2 py-1" />
                  <input type="number" step="0.1" value={hypCarbonProb} onChange={(e) => setHypCarbonProb(e.target.value)} placeholder="Xác suất (0-1)" className="rounded border bg-white px-2 py-1" />
                </div>
              )}
            </div>

            <button
              type="button"
              disabled={busy}
              onClick={runFinancialSuite}
              className="w-full rounded-xl bg-[#123c29] py-2.5 text-sm font-semibold text-white hover:bg-[#15803d] disabled:opacity-50"
            >
              {busy ? "Đang chạy Python Financial Engine..." : "Phân tích Tài chính, Kịch bản & Xuất Báo cáo"}
            </button>
          </div>

          {/* Chat Interface */}
          <div className="flex flex-col justify-between rounded-xl border border-[#d8e2da] bg-white p-5">
            <div>
              <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
                <Bot className="h-5 w-5 text-[#15803d]" /> Hội thoại Trợ lý Tài chính AI (Bilingual VI/EN)
              </h2>
              <div className="mt-2 flex flex-wrap gap-1.5">
                {[
                  "Tính ROI và dòng tiền cho vườn dừa hiện tại",
                  "Simulate scenario if coconut price drops 15%",
                  "Ước tính chi phí MRV chiếm bao nhiêu % tổng đầu tư?",
                ].map((q) => (
                  <button
                    key={q}
                    type="button"
                    onClick={() => setChatInput(q)}
                    className="rounded-full border border-[#d8e2da] bg-[#f7faf7] px-2.5 py-1 text-[11px] text-[#123c29] hover:bg-[#e8f5ec]"
                  >
                    {q}
                  </button>
                ))}
              </div>
              <div className="mt-3 max-h-72 space-y-2.5 overflow-y-auto pr-1">
                {messages.length === 0 ? (
                  <p className="text-xs text-[#526158]">
                    Đặt câu hỏi bằng Tiếng Việt hoặc Tiếng Anh — Gemini sẽ phân loại intent và gọi Python Financial Engine để tính số liệu chính xác.
                  </p>
                ) : (
                  messages.map((m, idx) => (
                    <div key={idx} className={`rounded-lg p-3 text-xs ${m.role === "user" ? "ml-6 bg-[#e8f5ec] text-[#123c29]" : "mr-4 border border-[#e2eae3] bg-[#fafcfa]"}`}>
                      {m.intent && (
                        <div className="mb-1 flex flex-wrap gap-1.5 text-[10px]">
                          <span className="rounded bg-[#123c29] px-1.5 py-0.5 text-white">intent: {m.intent}</span>
                          {m.tools?.map((t) => (
                            <span key={t} className="rounded bg-emerald-100 px-1.5 py-0.5 font-semibold text-emerald-900">tool: {t}</span>
                          ))}
                        </div>
                      )}
                      <p className="whitespace-pre-line leading-relaxed">{m.text}</p>
                      {m.citations && m.citations.length > 0 && (
                        <p className="mt-1 text-[10px] text-[#526158]">
                          Nguồn: {m.citations.map((c) => `[${c.sourceCode}]`).join(", ")}
                        </p>
                      )}
                    </div>
                  ))
                )}
              </div>
            </div>

            <form onSubmit={sendChat} className="mt-3 flex gap-2 border-t border-[#eef3ef] pt-3">
              <input
                value={chatInput}
                onChange={(e) => setChatInput(e.target.value)}
                placeholder="Hỏi về ROI, chi phí, kịch bản hoặc phương pháp carbon..."
                className="flex-1 rounded-lg border border-[#d8e2da] px-3 py-2 text-xs"
              />
              <button type="submit" disabled={busy} className="inline-flex items-center gap-1 rounded-lg bg-[#15803d] px-3.5 py-2 text-xs font-semibold text-white hover:bg-[#123c29]">
                <Send className="h-3.5 w-3.5" /> Gửi
              </button>
            </form>
          </div>
        </div>

        {/* Financial Dashboard, Cost Breakdown, ROI & Cash Flow Chart */}
        {calcResult && (
          <div className="space-y-6">
            <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
              <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
                <p className="text-xs text-[#526158]">Tổng đầu tư ({calcResult.horizonYears} năm)</p>
                <p className="mt-1 text-xl font-bold text-[#123c29]">{calcResult.costBreakdown.totalInvestmentCostVnd.toLocaleString("vi-VN")} đ</p>
                <p className="text-[11px] text-[#526158]">MRV: {calcResult.costBreakdown.mrvPrepSharePercent}% tổng chi phí</p>
              </div>
              <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
                <p className="text-xs text-[#526158]">Doanh thu nông sản & phụ phẩm</p>
                <p className="mt-1 text-xl font-bold text-[#15803d]">{calcResult.revenueBreakdown.totalAgriRevenueOverHorizonVnd.toLocaleString("vi-VN")} đ</p>
                <p className="text-[11px] text-[#526158]">Carbon giả định: {calcResult.revenueBreakdown.hypotheticalCarbonRevenueTotalVnd.toLocaleString("vi-VN")} đ</p>
              </div>
              <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
                <p className="text-xs text-[#526158]">ROI Thuần Nông nghiệp / BCR</p>
                <p className="mt-1 text-xl font-bold text-[#123c29]">
                  {calcResult.profitAndRoi.roiPercentAgriOnly.toFixed(1)}% <span className="text-xs font-normal">(BCR {calcResult.profitAndRoi.benefitCostRatio.toFixed(2)})</span>
                </p>
                <p className="text-[11px] text-[#526158]">
                  Hoàn vốn: {calcResult.profitAndRoi.paybackPeriodYears ? `${calcResult.profitAndRoi.paybackPeriodYears} năm` : "Chưa hoàn vốn"}
                </p>
              </div>
              <div className="rounded-xl border border-[#d8e2da] bg-white p-4">
                <p className="text-xs text-[#526158]">NPV (chiết khấu 8%)</p>
                <p className="mt-1 text-xl font-bold text-[#123c29]">{calcResult.profitAndRoi.npvAgriOnlyVnd.toLocaleString("vi-VN")} đ</p>
                <p className="text-[11px] text-[#526158]">Biên lợi nhuận: {calcResult.profitAndRoi.grossProfitMarginPercent.toFixed(1)}%</p>
              </div>
            </div>

            {/* Cost Breakdown + Cash Flow Visualization */}
            <div className="grid gap-6 lg:grid-cols-2">
              <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
                <h3 className="flex items-center gap-2 text-sm font-bold text-[#123c29]">
                  <BarChart3 className="h-4 w-4 text-[#15803d]" /> Cơ cấu Chi phí & ROI Visualization
                </h3>
                {[
                  { label: "CAPEX ban đầu", val: calcResult.costBreakdown.initialCapexVnd, color: "bg-[#123c29]" },
                  { label: "OPEX vận hành nhiều năm", val: calcResult.costBreakdown.totalOpexOverHorizonVnd, color: "bg-[#15803d]" },
                  { label: "Lợi nhuận ròng nông nghiệp", val: Math.max(0, calcResult.profitAndRoi.totalNetAgriProfitOverHorizonVnd), color: "bg-emerald-500" },
                ].map((bar) => {
                  const maxVal = Math.max(1, calcResult.revenueBreakdown.totalAgriRevenueOverHorizonVnd, calcResult.costBreakdown.totalInvestmentCostVnd);
                  const pct = Math.min(100, Math.round((bar.val / maxVal) * 100));
                  return (
                    <div key={bar.label} className="text-xs">
                      <div className="flex justify-between">
                        <span>{bar.label}</span>
                        <strong>{bar.val.toLocaleString("vi-VN")} đ</strong>
                      </div>
                      <div className="mt-1 h-2.5 w-full rounded-full bg-[#eef3ef]">
                        <div className={`h-2.5 rounded-full ${bar.color}`} style={{ width: `${pct}%` }} />
                      </div>
                    </div>
                  );
                })}
                <div className="pt-2 text-xs text-[#526158] space-y-1">
                  {calcResult.actualVsEstimateNotes.map((n, i) => <p key={i}>• {n}</p>)}
                </div>
              </div>

              <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
                <h3 className="flex items-center gap-2 text-sm font-bold text-[#123c29]">
                  <TrendingUp className="h-4 w-4 text-[#15803d]" /> Biểu đồ Dòng tiền Lũy kế (Năm 0 → {calcResult.horizonYears})
                </h3>
                <div className="mt-4 flex h-44 items-end gap-2 border-b border-[#d8e2da] pb-2">
                  {calcResult.cashFlowSchedule.map((row) => {
                    const val = row.cumulativeCashFlowAgriOnlyVnd;
                    const heightPct = Math.max(8, Math.round((Math.abs(val) / maxAbsCash) * 100));
                    return (
                      <div key={row.year} className="flex flex-1 flex-col items-center justify-end h-full">
                        <span className="mb-1 text-[9px] text-[#526158]">{(val / 1_000_000).toFixed(0)}M</span>
                        <div
                          className={`w-full max-w-[34px] rounded-t ${val >= 0 ? "bg-[#15803d]" : "bg-amber-600"}`}
                          style={{ height: `${heightPct}%` }}
                          title={`Năm ${row.year}: ${val.toLocaleString("vi-VN")} đ`}
                        />
                        <span className="mt-1 text-[10px] font-semibold">N{row.year}</span>
                      </div>
                    );
                  })}
                </div>
                <p className="mt-2 text-[11px] text-[#526158]">Đơn vị nhãn cột: triệu VND (dòng tiền lũy kế thuần nông nghiệp).</p>
              </div>
            </div>
          </div>
        )}

        {/* Scenario Comparison */}
        {scenarios && (
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <h3 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <GitCompare className="h-5 w-5 text-[#15803d]" /> So sánh Kịch bản Tài chính (Scenario Simulator)
            </h3>
            <div className="mt-3 overflow-x-auto">
              <table className="w-full text-left text-xs">
                <thead>
                  <tr className="border-b border-[#e2eae3] text-[#526158]">
                    <th className="py-2 pr-3">Kịch bản</th>
                    <th className="py-2 pr-3">Tổng Chi phí</th>
                    <th className="py-2 pr-3">Doanh thu Nông nghiệp</th>
                    <th className="py-2 pr-3">Lợi nhuận Ròng</th>
                    <th className="py-2 pr-3">ROI (%)</th>
                    <th className="py-2">Giả định áp dụng</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-[#eef3ef]">
                  {[scenarios.baseSummary, ...scenarios.scenarios].map((s) => (
                    <tr key={s.scenarioName}>
                      <td className="py-2 pr-3 font-bold text-[#123c29]">{s.scenarioName}</td>
                      <td className="py-2 pr-3">{s.totalCostVnd.toLocaleString("vi-VN")} đ</td>
                      <td className="py-2 pr-3">{s.totalAgriRevenueVnd.toLocaleString("vi-VN")} đ</td>
                      <td className="py-2 pr-3 font-semibold text-[#15803d]">{s.netProfitAgriOnlyVnd.toLocaleString("vi-VN")} đ</td>
                      <td className="py-2 pr-3 font-bold">{s.roiPercentAgriOnly.toFixed(1)}%</td>
                      <td className="py-2 text-[#526158]">{s.assumptionsSummary}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

        {/* 9-Section AI Financial Report */}
        {report && (
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
                <FileText className="h-5 w-5 text-[#15803d]" /> Báo cáo Tài chính Tổng hợp (9 Phần Chuẩn)
              </h3>
              <span className="text-xs text-[#526158]">{report.modelName}</span>
            </div>
            <div className="grid gap-3 text-xs md:grid-cols-2">
              <div className="rounded-lg border border-[#e2eae3] p-3"><strong>1. Executive Summary:</strong> {report.report.executiveSummary}</div>
              <div className="rounded-lg border border-[#e2eae3] p-3"><strong>2. Investment Overview:</strong> {report.report.investmentOverview}</div>
              <div className="rounded-lg border border-[#e2eae3] p-3"><strong>3. Revenue Analysis:</strong> {report.report.revenueAnalysis}</div>
              <div className="rounded-lg border border-[#e2eae3] p-3"><strong>4. Cost Analysis:</strong> {report.report.costAnalysis}</div>
              <div className="rounded-lg border border-[#e2eae3] p-3"><strong>5. ROI Analysis:</strong> {report.report.roiAnalysis}</div>
              <div className="rounded-lg border border-[#e2eae3] p-3"><strong>6. Cash Flow Analysis:</strong> {report.report.cashFlowAnalysis}</div>
            </div>
            <div className="grid gap-3 text-xs md:grid-cols-3">
              <div className="rounded-lg border border-amber-200 bg-amber-50/50 p-3">
                <strong>7. Risk Assessment:</strong>
                <ul className="mt-1 list-disc pl-4 space-y-1">{report.report.riskAssessment.map((x, i) => <li key={i}>{x}</li>)}</ul>
              </div>
              <div className="rounded-lg border border-emerald-200 bg-emerald-50/50 p-3">
                <strong>8. Recommendations:</strong>
                <ul className="mt-1 list-disc pl-4 space-y-1">{report.report.recommendations.map((x, i) => <li key={i}>{x}</li>)}</ul>
              </div>
              <div className="rounded-lg border border-[#e2eae3] p-3">
                <strong>9. Data Sources & Assumptions:</strong>
                <ul className="mt-1 list-disc pl-4 space-y-1">{report.report.dataSourcesAndAssumptions.map((x, i) => <li key={i}>{x}</li>)}</ul>
              </div>
            </div>
          </div>
        )}

        <ScientificDisclaimerBanner />
      </div>
    </AppShell>
  );
}
