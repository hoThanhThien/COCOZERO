import React from "react";
import { AlertTriangle, CheckCircle2, Info, ShieldAlert } from "lucide-react";

export function OriginBadge({ tag }: { tag: string }) {
  const map: Record<string, { label: string; cls: string }> = {
    Demo: { label: "DEMO / Dữ liệu mẫu", cls: "bg-amber-100 text-amber-900 border-amber-300" },
    Synthetic: { label: "SYNTHETIC / Giả lập", cls: "bg-amber-100 text-amber-900 border-amber-300" },
    UserDeclared: { label: "USER-DECLARED / Tự khai báo", cls: "bg-sky-100 text-sky-900 border-sky-300" },
    AiExtracted: { label: "AI-EXTRACTED / AI trích xuất", cls: "bg-purple-100 text-purple-900 border-purple-300" },
    AI_EXTRACTED: { label: "AI-EXTRACTED / AI trích xuất", cls: "bg-purple-100 text-purple-900 border-purple-300" },
    Verified: { label: "VERIFIED / Có chứng từ duyệt", cls: "bg-emerald-100 text-emerald-900 border-emerald-300" },
  };
  const cfg = map[tag] || { label: tag, cls: "bg-gray-100 text-gray-800 border-gray-300" };
  return (
    <span className={`inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-semibold ${cfg.cls}`}>
      <Info className="h-3 w-3" />
      {cfg.label}
    </span>
  );
}

export function VerificationNoticeBadge({ status }: { status?: string }) {
  const isVerified = status && status.toLowerCase().includes("xác nhận chính thức");
  return (
    <span
      className={`inline-flex items-center gap-1 rounded-full border px-2.5 py-0.5 text-xs font-medium ${
        isVerified ? "bg-emerald-100 text-emerald-900 border-emerald-300" : "bg-stone-100 text-stone-800 border-stone-300"
      }`}
    >
      {isVerified ? <CheckCircle2 className="h-3.5 w-3.5" /> : <ShieldAlert className="h-3.5 w-3.5 text-amber-700" />}
      {status || "Chưa xác minh độc lập"}
    </span>
  );
}

export function ScientificDisclaimerBanner({ text }: { text?: string }) {
  return (
    <div className="rounded-xl border border-amber-300 bg-amber-50/90 p-4 text-sm text-amber-950">
      <div className="flex items-start gap-3">
        <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-700" />
        <div>
          <p className="font-semibold">Tuyên bố khoa học & ranh giới pháp lý bắt buộc</p>
          <p className="mt-1 leading-relaxed">
            {text ||
              "Đây là ước tính tham khảo theo nghiên cứu sinh khối cây dừa tại Giồng Trôm, Bến Tre (2015), áp dụng theo cấp tuổi và nhóm giống khảo sát. Kết quả không đại diện cho phép đo độc lập tại vườn của bạn, không phải lượng hấp thụ CO2 hằng năm, không phải tín chỉ carbon đã được phát hành và không phải doanh thu được đảm bảo."}
          </p>
        </div>
      </div>
    </div>
  );
}
