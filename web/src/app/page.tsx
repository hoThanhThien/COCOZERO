import Link from "next/link";
import { ArrowRight, Bot, Calculator, CheckCircle2, FileCheck2, FolderKanban, ShieldCheck, Sprout } from "lucide-react";
import { ScientificDisclaimerBanner } from "@/components/Badges";

export default function LandingPage() {
  return (
    <div className="min-h-screen bg-[#f6f8f5] text-[#1b241f]">
      <header className="border-b border-[#d8e2da] bg-white">
        <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4 sm:px-6">
          <div className="flex items-center gap-2.5">
            <div className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#123c29] text-white">
              <Sprout className="h-5 w-5" />
            </div>
            <div>
              <p className="text-base font-bold text-[#123c29]">CocoCarbon AI</p>
              <p className="text-xs text-[#526158]">Vùng dừa Bến Tre • Tỉnh Vĩnh Long</p>
            </div>
          </div>
          <div className="flex flex-wrap items-center gap-2 sm:gap-3">
            <Link
              href="/projects"
              className="rounded-lg border border-[#d8e2da] px-3 py-2 text-xs font-medium text-[#123c29] hover:bg-[#e8f5ec] sm:px-3.5 sm:text-sm"
            >
              Dự án
            </Link>
            <Link
              href="/login"
              className="rounded-lg bg-[#15803d] px-3.5 py-2 text-xs font-semibold text-white hover:bg-[#123c29] sm:px-4 sm:text-sm"
            >
              Đăng nhập / Trải nghiệm
            </Link>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-6xl space-y-10 px-4 py-10 sm:px-6">
        <section className="rounded-2xl border border-[#cfe0d3] bg-white p-6 shadow-xs sm:p-10">
          <div className="inline-flex items-center gap-2 rounded-full border border-[#c7decb] bg-[#e8f5ec] px-3 py-1 text-xs font-semibold text-[#123c29]">
            HUMAN CENTERED • AI POWERED • SCIENCE FIRST
          </div>
            <h1 className="mt-4 text-2xl font-bold tracking-tight text-[#123c29] sm:text-3xl lg:text-4xl">
            Trợ lý AI hiểu nhu cầu &amp; hành động vì nông hộ — Từ vườn dừa đến giá trị xanh
          </h1>
          <p className="mt-3 max-w-3xl text-base leading-relaxed text-[#435149]">
            CocoCarbon AI giúp cuộc sống và quản trị kinh tế nông nghiệp trở nên <strong>tiện lợi, an toàn và thoải mái hơn</strong> qua hệ sinh thái Trợ lý AI hành động
            (Tài chính • Giao dịch thu chi • Mua sắm vật tư an toàn • Bán hàng/RFQ • Nhắc việc cá nhân), kết hợp lá chắn chống lừa đảo đầu tư carbon trước khi ký kết hay mất tiền.
          </p>
          <div className="mt-6 flex flex-wrap gap-3">
            <Link
              href="/login"
              className="inline-flex items-center gap-2 rounded-xl bg-[#123c29] px-5 py-3 text-sm font-semibold text-white hover:bg-[#15803d]"
            >
              Bắt đầu với Google / Demo <ArrowRight className="h-4 w-4" />
            </Link>
            <Link
              href="/calculator"
              className="inline-flex items-center gap-2 rounded-xl border border-[#c7decb] bg-[#e8f5ec] px-5 py-3 text-sm font-semibold text-[#123c29] hover:bg-[#d8eedf]"
            >
              <Calculator className="h-4 w-4" /> Thử Coconut Carbon Calculator
            </Link>
          </div>
        </section>

        <section className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
          {[
            {
              icon: Sprout,
              title: "1. Số hóa Vườn dừa",
              desc: "Ghi nhận diện tích, số cây, giống (Dừa Ta / Xiêm xanh), cấp tuổi, tọa độ GPS & ảnh có đồng thuận rõ ràng.",
            },
            {
              icon: Calculator,
              title: "2. Calculator Khoa học",
              desc: "Tính ở C# backend với đúng 4 hệ số CTU 2015. Không nội suy tuổi, không thổi phồng thành tín chỉ hay thu nhập.",
            },
            {
              icon: FileCheck2,
              title: "3. AI Carbon Auditor",
              desc: "Trích xuất PDF/ảnh kèm trang chứng cứ, phát hiện trùng lặp SHA-256, lệch số liệu và yêu cầu Reviewer phê duyệt.",
            },
            {
              icon: FolderKanban,
              title: "4. HTX & Directory / RFQ",
              desc: "Tổng hợp vùng nguyên liệu theo ủy quyền và tiếp nhận yêu cầu tìm hiểu (RFQ) từ doanh nghiệp.",
            },
          ].map((card) => {
            const Icon = card.icon;
            return (
              <div key={card.title} className="rounded-xl border border-[#d8e2da] bg-white p-5">
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-[#e8f5ec] text-[#15803d]">
                  <Icon className="h-5 w-5" />
                </div>
                <h2 className="mt-3 text-base font-bold text-[#123c29]">{card.title}</h2>
                <p className="mt-1.5 text-sm leading-relaxed text-[#526158]">{card.desc}</p>
              </div>
            );
          })}
        </section>

        <section className="grid gap-4 md:grid-cols-2">
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <h3 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <ShieldCheck className="h-5 w-5 text-[#15803d]" />
              Ranh giới minh bạch của MVP
            </h3>
            <ul className="mt-3 space-y-2 text-sm text-[#435149]">
              <li className="flex items-start gap-2">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-[#15803d]" />
                <span>Phân biệt rõ Biomass, Carbon stock, CO2e tham khảo, Net removals và Issued/Retired credits.</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-[#15803d]" />
                <span>Không tự phát hành tín chỉ, không tạo serial giả, không giữ tiền hay thanh toán trên nền tảng.</span>
              </li>
              <li className="flex items-start gap-2">
                <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0 text-[#15803d]" />
                <span>Gắn nhãn nguồn dữ liệu: DEMO, USER-DECLARED, AI-EXTRACTED và chỉ VERIFIED khi có bằng chứng hợp lệ.</span>
              </li>
            </ul>
          </div>
          <div className="rounded-xl border border-[#d8e2da] bg-white p-5">
            <h3 className="flex items-center gap-2 text-base font-bold text-[#123c29]">
              <Bot className="h-5 w-5 text-[#15803d]" />
              Lối tắt trải nghiệm nhanh
            </h3>
            <div className="mt-3 grid grid-cols-1 gap-2.5 text-sm sm:grid-cols-2">
              <Link href="/dashboard" className="rounded-lg border border-[#d8e2da] p-3 hover:bg-[#e8f5ec]">
                Bảng điều khiển Nông hộ
              </Link>
              <Link href="/cooperative" className="rounded-lg border border-[#d8e2da] p-3 hover:bg-[#e8f5ec]">
                Dashboard Hợp tác xã
              </Link>
              <Link href="/documents" className="rounded-lg border border-[#d8e2da] p-3 hover:bg-[#e8f5ec]">
                AI Auditor & Review
              </Link>
              <Link href="/assistant" className="rounded-lg border border-[#d8e2da] p-3 hover:bg-[#e8f5ec]">
                Trợ lý Khoa học [R1-R12]
              </Link>
            </div>
          </div>
        </section>

        <ScientificDisclaimerBanner />
      </main>
    </div>
  );
}
