"use client";

import Link from "next/link";
import { useEffect, useState } from "react";
import { MapPin, Plus, Trees } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { OriginBadge } from "@/components/Badges";
import { apiFetch, getStoredToken } from "@/lib/api";
import type { FarmDto } from "@/lib/types";

export default function FarmsPage() {
  const [farms, setFarms] = useState<FarmDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);

  const [name, setName] = useState("");
  const [province, setProvince] = useState("Vĩnh Long");
  const [commune, setCommune] = useState("Xã Lương Quới (Giồng Trôm)");
  const [areaHa, setAreaHa] = useState("1.0");
  const [treeCount, setTreeCount] = useState("200");
  const [variety, setVariety] = useState("TallDuaTa");
  const [ageYears, setAgeYears] = useState("10");
  const [latitude, setLatitude] = useState("10.1652");
  const [longitude, setLongitude] = useState("106.4621");
  const [hasLocationConsent, setHasLocationConsent] = useState(true);
  const [hasPhotoConsent, setHasPhotoConsent] = useState(true);

  async function loadFarms() {
    if (!getStoredToken()) {
      setLoading(false);
      return;
    }
    try {
      const list = await apiFetch<FarmDto[]>("/api/v1/farms");
      setFarms(list);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Không thể tải danh sách vườn.");
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    loadFarms();
  }, []);

  async function handleCreate(e: React.FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    setError(null);
    setSuccess(null);
    try {
      await apiFetch("/api/v1/farms", {
        method: "POST",
        body: JSON.stringify({
          name,
          province,
          commune,
          historicalRegion: "Vùng dừa Bến Tre",
          areaHa: Number(areaHa),
          treeCount: Number(treeCount),
          variety,
          ageYears: Number(ageYears),
          latitude: hasLocationConsent && latitude ? Number(latitude) : null,
          longitude: hasLocationConsent && longitude ? Number(longitude) : null,
          hasLocationConsent,
          hasPhotoConsent,
        }),
      });
      setName("");
      setSuccess("Đã tạo hồ sơ vườn dừa (gắn nhãn USER-DECLARED).");
      await loadFarms();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Lỗi khi tạo vườn.");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <AppShell title="Sổ đăng ký Vườn dừa (Farm Registry)" subtitle="Địa giới hành chính hiện hành: Tỉnh Vĩnh Long • Ngữ cảnh vùng dừa Bến Tre">
      {error && <div className="mb-4 rounded-xl border border-red-300 bg-red-50 p-3 text-sm text-red-900">{error}</div>}
      {success && <div className="mb-4 rounded-xl border border-emerald-300 bg-emerald-50 p-3 text-sm text-emerald-900">{success}</div>}

      <div className="grid gap-6 lg:grid-cols-3">
        <form onSubmit={handleCreate} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-3 lg:col-span-1">
          <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
            <Plus className="h-4 w-4 text-[#15803d]" /> Tạo hồ sơ vườn mới
          </h2>
          <div>
            <label className="block text-xs font-semibold text-[#526158]">Tên vườn</label>
            <input value={name} onChange={(e) => setName(e.target.value)} required placeholder="VD: Vườn Dừa Ta Hộ Ông Năm" className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Tỉnh/TP hiện hành</label>
              <input value={province} onChange={(e) => setProvince(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Xã/Khu vực</label>
              <input value={commune} onChange={(e) => setCommune(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Diện tích (ha)</label>
              <input type="number" step="0.01" min="0.01" value={areaHa} onChange={(e) => setAreaHa(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Số lượng cây</label>
              <input type="number" min="1" step="1" value={treeCount} onChange={(e) => setTreeCount(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Nhóm giống</label>
              <select value={variety} onChange={(e) => setVariety(e.target.value)} className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm">
                <option value="TallDuaTa">Dừa cao - Dừa Ta</option>
                <option value="ShortXiemXanh">Dừa thấp - Xiêm xanh</option>
                <option value="OtherUnsupported">Giống khác (chưa có hệ số)</option>
              </select>
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Tuổi cây (năm)</label>
              <input type="number" min="1" max="100" value={ageYears} onChange={(e) => setAgeYears(e.target.value)} required className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
          </div>
          <div className="grid grid-cols-2 gap-2">
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Vĩ độ (Lat)</label>
              <input value={latitude} onChange={(e) => setLatitude(e.target.value)} className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
            <div>
              <label className="block text-xs font-semibold text-[#526158]">Kinh độ (Lng)</label>
              <input value={longitude} onChange={(e) => setLongitude(e.target.value)} className="mt-1 w-full rounded-lg border border-[#d8e2da] px-3 py-2 text-sm" />
            </div>
          </div>
          <label className="flex items-center gap-2 text-xs text-[#435149]">
            <input type="checkbox" checked={hasLocationConsent} onChange={(e) => setHasLocationConsent(e.target.checked)} />
            Đồng thuận lưu tọa độ GPS vị trí vườn
          </label>
          <label className="flex items-center gap-2 text-xs text-[#435149]">
            <input type="checkbox" checked={hasPhotoConsent} onChange={(e) => setHasPhotoConsent(e.target.checked)} />
            Đồng thuận lưu ảnh & tài liệu phục vụ rà soát
          </label>
          <button
            type="submit"
            disabled={submitting || !getStoredToken()}
            className="w-full rounded-xl bg-[#123c29] py-2.5 text-sm font-semibold text-white hover:bg-[#15803d] disabled:opacity-50"
          >
            {submitting ? "Đang lưu..." : "Lưu hồ sơ vườn"}
          </button>
        </form>

        <div className="rounded-xl border border-[#d8e2da] bg-white p-5 lg:col-span-2">
          <h2 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
            <Trees className="h-5 w-5 text-[#15803d]" /> Danh sách vườn dừa ({farms.length})
          </h2>
          {loading ? (
            <p className="mt-4 text-sm text-[#526158]">Đang tải...</p>
          ) : farms.length === 0 ? (
            <p className="mt-4 text-sm text-[#526158]">Chưa có vườn dừa nào hoặc bạn chưa đăng nhập.</p>
          ) : (
            <div className="mt-4 space-y-3">
              {farms.map((farm) => (
                <div key={farm.id} className="rounded-xl border border-[#e2eae3] p-4 transition hover:border-[#15803d]">
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex flex-wrap items-center gap-2">
                      <Link href={`/farms/${farm.id}`} className="text-base font-bold text-[#123c29] hover:underline">
                        {farm.name}
                      </Link>
                      <OriginBadge tag={farm.dataOriginTag} />
                    </div>
                    <span className="rounded-full bg-[#e8f5ec] px-2.5 py-0.5 text-xs font-semibold text-[#15803d]">
                      {farm.status}
                    </span>
                  </div>
                  <div className="mt-2 grid gap-2 text-xs text-[#526158] sm:grid-cols-4">
                    <div>Khu vực: {farm.commune}, {farm.province}</div>
                    <div>Diện tích: <strong className="text-[#1b241f]">{farm.areaHa} ha</strong></div>
                    <div>Mật độ quan sát: <strong className="text-[#1b241f]">{(farm.treeCount / farm.areaHa).toFixed(1)} cây/ha</strong></div>
                    <div>Giống / Tuổi: {farm.variety} • {farm.ageYears} năm</div>
                  </div>
                  <div className="mt-3 flex flex-wrap items-center justify-between gap-2">
                    <span className="inline-flex items-center gap-1 text-xs text-[#526158]">
                      <MapPin className="h-3.5 w-3.5" />
                      {farm.latitude && farm.longitude ? `${farm.latitude.toFixed(4)}, ${farm.longitude.toFixed(4)}` : "Chưa có tọa độ"}
                    </span>
                    <Link
                      href={`/farms/${farm.id}`}
                      className="rounded-lg bg-[#e8f5ec] px-3 py-1.5 text-xs font-semibold text-[#123c29] hover:bg-[#d1ebd9]"
                    >
                      Mở chi tiết, Tài liệu & Calculator →
                    </Link>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </AppShell>
  );
}
