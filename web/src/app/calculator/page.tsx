"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { AlertCircle, Calculator, History } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { ScientificDisclaimerBanner } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { CarbonEstimateResponse, FarmDto, ResearchFactorDto } from "@/lib/types";

export default function CalculatorPage() {
  const [factors, setFactors] = useState<ResearchFactorDto[]>([]);
  const [limitations, setLimitations] = useState("");
  const [farms, setFarms] = useState<FarmDto[]>([]);
  const [history, setHistory] = useState<
    { id: string; estimatedCO2EquivalentT: number; observedTreeDensityPerHa: number; factorVersion: string; createdAt: string }[]
  >([]);

  const [farmId, setFarmId] = useState("");
  const [areaHa, setAreaHa] = useState("1.0");
  const [treeCount, setTreeCount] = useState("200");
  const [variety, setVariety] = useState("TallDuaTa");
  const [treeAgeYears, setTreeAgeYears] = useState("10");

  const [result, setResult] = useState<CarbonEstimateResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    apiFetch<{ factors: ResearchFactorDto[]; methodologyLimitations: string }>("/api/v1/research-factors")
      .then((res) => {
        setFactors(res.factors);
        setLimitations(res.methodologyLimitations);
      })
      .catch(() => {});

    if (getStoredToken()) {
      apiFetch<FarmDto[]>("/api/v1/farms").then(setFarms).catch(() => {});
      apiFetch<typeof history>("/api/v1/carbon-calculator/history").then(setHistory).catch(() => {});
    }
  }, []);

  function onSelectFarm(id: string) {
    setFarmId(id);
    const found = farms.find((f) => f.id === id);
    if (found) {
      setAreaHa(String(found.areaHa));
      setTreeCount(String(found.treeCount));
      setVariety(found.variety);
      setTreeAgeYears(String(found.ageYears));
    }
  }

  async function handleEstimate(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    setResult(null);
    try {
      const res = await apiFetch<CarbonEstimateResponse>("/api/v1/carbon-calculator/estimate", {
        method: "POST",
        body: JSON.stringify({
          farmId: farmId || null,
          areaHa: Number(areaHa),
          treeCount: Number(treeCount),
          variety,
          treeAgeYears: Number(treeAgeYears),
          includeArithmeticPerTreeAllocation: false,
        }),
      });
      setResult(res);
      apiFetch<typeof history>("/api/v1/carbon-calculator/history").then(setHistory).catch(() => {});
    } catch (err) {
      setError(err instanceof Error ? err.message : "Không thể tính toán.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell
      title="Coconut Carbon Calculator (CTU 2015)"
      subtitle="Tính toán thực hiện 100% tại C# backend từ đúng 4 hệ số nghiên cứu Giồng Trôm (2015) • Không nội suy tuổi"
    >
      <div className="space-y-6">
        <div className="grid gap-6 lg:grid-cols-2">
          <form onSubmit={handleEstimate} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-4">
            <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <Calculator className="h-5 w-5 text-[#15803d]" /> Nhập thông số vườn dừa
            </h2>

            {!getStoredToken() && (
              <div className="rounded-lg border border-amber-300 bg-amber-50 p-3 text-xs text-amber-950">
                Để lưu lịch sử và gọi API tính toán có xác thực, vui lòng{" "}
                <Link href="/login" className="font-bold underline">đăng nhập</Link> trước.
              </div>
            )}

            {farms.length > 0 && (
              <div>
                <label className="block text-xs font-semibold text-[#526158]">Chọn nhanh từ vườn của bạn (tùy chọn)</label>
                <select value={farmId} onChange={(e) => onSelectFarm(e.target.value)} className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm">
                  <option value="">-- Nhập thủ công --</option>
                  {farms.map((f) => (
                    <option key={f.id} value={f.id}>
                      {f.name} ({f.areaHa} ha, {f.variety}, {f.ageYears} năm)
                    </option>
                  ))}
                </select>
              </div>
            )}

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-[#526158]">Diện tích (ha)</label>
                <input type="number" step="0.01" value={areaHa} onChange={(e) => setAreaHa(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
              </div>
              <div>
                <label className="block text-xs font-semibold text-[#526158]">Số lượng cây (chỉ tính mật độ)</label>
                <input type="number" step="1" value={treeCount} onChange={(e) => setTreeCount(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
              </div>
            </div>

            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="block text-xs font-semibold text-[#526158]">Nhóm giống nghiên cứu</label>
                <select value={variety} onChange={(e) => setVariety(e.target.value)} className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm">
                  <option value="TallDuaTa">Dừa cao - Dừa Ta (Tall)</option>
                  <option value="ShortXiemXanh">Dừa thấp - Xiêm xanh (Short)</option>
                  <option value="OtherUnsupported">Giống khác (test từ chối)</option>
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold text-[#526158]">Cấp tuổi (chỉ nhận 4 hoặc 10)</label>
                <input type="number" value={treeAgeYears} onChange={(e) => setTreeAgeYears(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
              </div>
            </div>

            {error && (
              <div className="flex items-start gap-2 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-900">
                <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
                <span>{error}</span>
              </div>
            )}

            <button
              type="submit"
              disabled={busy}
              className="w-full rounded-xl bg-[#123c29] py-2.5 text-sm font-semibold text-white hover:bg-[#15803d] disabled:opacity-50"
            >
              {busy ? "Đang tính tại C# API..." : "Tính ước tính tham khảo"}
            </button>
          </form>

          <div className="space-y-4">
            {result ? (
              <div className="rounded-xl border-2 border-[#15803d] bg-white p-5 space-y-3">
                <p className="text-xs font-bold uppercase tracking-wider text-[#15803d]">
                  {result.resultLabel}
                </p>
                <p className="text-3xl font-extrabold text-[#123c29]">
                  {result.estimatedCO2EquivalentT} <span className="text-base font-semibold">tCO2 tương đương tham khảo</span>
                </p>
                <p className="text-xs text-[#526158]">{result.unit}</p>

                <div className="rounded-lg border border-[#e2eae3] bg-[#f9fbf9] p-3 text-xs space-y-1">
                  <p><strong>Hệ số áp dụng:</strong> {result.factorUsed.code} — {result.factorUsed.valueTCO2PerHa} tCO2/ha ({result.factorUsed.version})</p>
                  <p><strong>Mật độ quan sát:</strong> {result.observedTreeDensityPerHa} cây/ha</p>
                  <p>{result.densityContextNote}</p>
                  <p><strong>Phiên bản công thức:</strong> {result.formulaVersion}</p>
                  <p><strong>Nguồn:</strong> {result.sourceCitation}</p>
                </div>
              </div>
            ) : (
              <div className="rounded-xl border border-[#d8e2da] bg-white p-5 text-sm text-[#526158]">
                Nhập thông số vườn dừa và bấm <strong>Tính ước tính tham khảo</strong> để xem kết quả từ C# backend.
              </div>
            )}

            <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
              <h3 className="text-sm font-bold text-[#123c29]">Bảng 4 hệ số CTU (2015) được phê duyệt [R1]</h3>
              <div className="mt-3 overflow-x-auto">
                <table className="w-full text-left text-xs">
                  <thead>
                    <tr className="border-b border-[#e2eae3] text-[#526158]">
                      <th className="py-2 pr-3">Nhóm giống</th>
                      <th className="py-2 pr-3">Tuổi</th>
                      <th className="py-2 pr-3">Hệ số (tCO2/ha)</th>
                      <th className="py-2">Phiên bản</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-[#eef3ef]">
                    {factors.map((f) => (
                      <tr key={f.id}>
                        <td className="py-2 pr-3 font-medium">{f.varietyDisplayName}</td>
                        <td className="py-2 pr-3">{f.treeAgeYears} năm</td>
                        <td className="py-2 pr-3 font-bold text-[#123c29]">{f.valueTCO2PerHa}</td>
                        <td className="py-2">{f.version}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
              {limitations && <p className="mt-3 text-xs leading-relaxed text-[#526158]">{limitations}</p>}
            </div>
          </div>
        </div>

        {history.length > 0 && (
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <h3 className="flex items-center gap-2 text-sm font-bold text-[#123c29]">
              <History className="h-4 w-4 text-[#15803d]" /> Lịch sử tính toán bất biến gần đây
            </h3>
            <div className="mt-3 divide-y divide-[#eef3ef] text-xs">
              {history.slice(0, 8).map((h) => (
                <div key={h.id} className="flex flex-wrap items-center justify-between gap-2 py-2">
                  <span><strong>{h.estimatedCO2EquivalentT} tCO2 tham khảo</strong> • Mật độ {h.observedTreeDensityPerHa} cây/ha</span>
                  <span className="text-[#526158]">{h.factorVersion} • {new Date(h.createdAt).toLocaleString("vi-VN")}</span>
                </div>
              ))}
            </div>
          </div>
        )}

        <ScientificDisclaimerBanner />
      </div>
    </AppShell>
  );
}
