"use client";

import { useState } from "react";
import Link from "next/link";
import { Bot, ExternalLink, Send } from "lucide-react";
import { AppShell } from "@/components/AppShell";
import { apiFetch, getStoredToken } from "@/lib/api";

interface AssistantResult {
  answer: string;
  hasEnoughEvidence: boolean;
  citations: { sourceCode: string; title: string; url: string; excerpt: string }[];
  disclaimer: string;
  modelName: string;
}

const SAMPLE_QUESTIONS = [
  "Tín chỉ là gì và khác gì với ước tính sinh khối CTU 2015?",
  "Hệ số CTU 2015 cho Dừa Ta 10 năm tuổi là bao nhiêu và có giới hạn gì?",
  "Chi phí chăm sóc vườn dừa và chuẩn bị hồ sơ MRV nên theo dõi như thế nào?",
  "Có thể tính doanh thu bán tín chỉ carbon năm thứ 10 từ CTU 2015 không?",
  "Vì sao không được lấy hệ số 196 tấn/ha của IPCC 2006 trộn vào CTU 2015?",
  "Verra VM0042 v2.2 có tự động áp dụng cho sinh khối thân dừa không?",
];

export default function AssistantPage() {
  const [role, setRole] = useState("Farmer");
  const [question, setQuestion] = useState(SAMPLE_QUESTIONS[0]);
  const [res, setRes] = useState<AssistantResult | null>(null);
  const [busy, setBusy] = useState(false);
  const [err, setErr] = useState<string | null>(null);

  async function ask(e: React.FormEvent) {
    e.preventDefault();
    setBusy(true);
    setErr(null);
    try {
      const data = await apiFetch<AssistantResult>("/api/v1/ai/assistant/query", {
        method: "POST",
        body: JSON.stringify({ role, question }),
      });
      setRes(data);
    } catch (e2) {
      setErr(e2 instanceof Error ? e2.message : "Vui lòng đăng nhập để sử dụng trợ lý AI.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <AppShell
      title="Trợ lý Khoa học & Hồ sơ (Curated RAG)"
      subtitle="Chỉ truy xuất từ nguồn đã kiểm chứng [R1]-[R12], [AGRI1], [GF1]; tự động từ chối khi thiếu chứng cứ để tránh hallucination"
    >
      <div className="space-y-6">
        <div className="flex flex-wrap items-center justify-between gap-2 rounded-xl border border-[#cfe0d3] bg-[#f6fbf7] px-4 py-3 text-xs">
          <span>
            Muốn trợ lý <strong>hiểu nhu cầu &amp; hành động thay bạn</strong> (tính ROI, ghi sổ thu chi tự nhiên, soạn RFQ bán hàng, lập checklist cá nhân)?
          </span>
          <Link href="/financial-assistant" className="rounded-lg bg-[#15803d] px-3 py-1.5 font-semibold text-white hover:bg-[#123c29]">
            Mở Trợ lý Tài chính AI &amp; 5 Tác vụ
          </Link>
        </div>
        <form onSubmit={ask} className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-4">
          <div className="flex flex-wrap gap-2">
            {SAMPLE_QUESTIONS.map((q) => (
              <button
                key={q}
                type="button"
                onClick={() => setQuestion(q)}
                className="rounded-full border border-[#d8e2da] bg-[#f7faf7] px-3 py-1 text-xs text-[#123c29] hover:bg-[#e8f5ec]"
              >
                {q}
              </button>
            ))}
          </div>

          <div className="grid gap-3 sm:grid-cols-4">
            <select value={role} onChange={(e) => setRole(e.target.value)} className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm">
              <option value="Farmer">Vai trò: Nông hộ</option>
              <option value="Buyer">Vai trò: Doanh nghiệp Buyer</option>
              <option value="ProjectDeveloper">Vai trò: Project Developer</option>
            </select>
            <input
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              required
              placeholder="Đặt câu hỏi về hệ số, phương pháp luận hoặc quy trình..."
              className="rounded-lg border border-[#d8e2da] px-3 py-2 text-sm sm:col-span-3"
            />
          </div>

          {err && <div className="rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-900">{err}</div>}

          <button
            type="submit"
            disabled={busy || !getStoredToken()}
            className="inline-flex items-center gap-2 rounded-lg bg-[#123c29] px-4 py-2 text-sm font-semibold text-white hover:bg-[#15803d] disabled:opacity-50"
          >
            <Send className="h-4 w-4" /> {busy ? "Đang truy xuất..." : "Hỏi Trợ lý Khoa học"}
          </button>
        </form>

        {res && (
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5 space-y-4">
            <div className="flex items-center justify-between">
              <h2 className="flex items-center gap-2 text-sm font-bold text-[#123c29]">
                <Bot className="h-4 w-4 text-[#15803d]" /> Kết quả tổng hợp ({res.modelName})
              </h2>
              <span className={`rounded-full px-2.5 py-0.5 text-xs font-semibold ${res.hasEnoughEvidence ? "bg-emerald-100 text-emerald-900" : "bg-amber-100 text-amber-900"}`}>
                {res.hasEnoughEvidence ? "Có nguồn đối chiếu" : "Chưa đủ dữ liệu chứng minh"}
              </span>
            </div>
            <p className="whitespace-pre-line text-sm leading-relaxed text-[#1b241f]">{res.answer}</p>

            {res.citations.length > 0 && (
              <div className="space-y-2 border-t border-[#eef3ef] pt-3">
                <p className="text-xs font-bold text-[#526158]">Nguồn trích dẫn:</p>
                {res.citations.map((c) => (
                  <div key={c.sourceCode} className="rounded-lg border border-[#e2eae3] bg-[#fafcfa] p-3 text-xs">
                    <div className="flex items-center justify-between">
                      <span className="font-bold text-[#123c29]">[{c.sourceCode}] {c.title}</span>
                      <a href={c.url} target="_blank" rel="noopener noreferrer" className="inline-flex items-center gap-1 text-[#15803d] hover:underline">
                        Mở nguồn <ExternalLink className="h-3 w-3" />
                      </a>
                    </div>
                    <p className="mt-1 text-[#526158]">{c.excerpt}</p>
                  </div>
                ))}
              </div>
            )}

            <p className="text-xs text-amber-900">{res.disclaimer}</p>
          </div>
        )}
      </div>
    </AppShell>
  );
}
