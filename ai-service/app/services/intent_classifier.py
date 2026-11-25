from dataclasses import dataclass
from typing import Literal
import unicodedata


IntentType = Literal[
    "financial_calculation",
    "scenario_simulation",
    "investment_analysis",
    "financial_report",
    "financial_qa",
    "payment_transaction_assist",
    "shopping_procurement_assist",
    "sales_rfq_assist",
    "personal_workflow_assist",
    "carbon_integrity",
    "carbon_methodology_qa",
    "unknown_or_out_of_scope",
]


@dataclass(frozen=True)
class IntentResult:
    intent: IntentType
    language: Literal["vi", "en"]
    confidence: float
    matchedSignals: list[str]
    assistantCapability: str = "Trợ lý Tài chính"


def _fold(text: str) -> str:
    nfc = unicodedata.normalize("NFC", text or "").lower().strip().replace("đ", "d")
    nfd = unicodedata.normalize("NFD", nfc)
    return "".join(ch for ch in nfd if unicodedata.category(ch) != "Mn")


def classify_intent(question: str, has_financial_payload: bool = False) -> IntentResult:
    q = unicodedata.normalize("NFC", question or "").lower().strip()
    qf = _fold(question)
    is_vi = any(ch in q for ch in "ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ") or any(
        tok in qf for tok in ("la gi", "trieu", "vuon", "dua", "chi phi", "loi nhuan", "tin chi", "bao cao", "mua sam", "thanh toan", "ban hang", "ca nhan")
    )
    lang: Literal["vi", "en"] = "vi" if is_vi else "en"
    signals: list[str] = []

    if any(p in qf for p in ("sao hoa", "mars", "bitcoin", "crypto", "xo so", "lottery")):
        return IntentResult("unknown_or_out_of_scope", lang, 0.95, ["out_of_scope_keyword"], "Ngoài phạm vi")

    if any(p in qf for p in ("bao cao tai chinh", "financial report", "executive summary", "xuat bao cao")):
        signals.append("report_keyword")
        return IntentResult("financial_report", lang, 0.92, signals, "Trợ lý Tài chính")

    if any(p in qf for p in ("kich ban", "mo phong", "so sanh phuong an", "scenario", "sensitivity", "do nhay", "neu gia")):
        signals.append("scenario_keyword")
        return IntentResult("scenario_simulation", lang, 0.90, signals, "Trợ lý Tài chính")

    if any(p in qf for p in ("gian lan", "lua dao", "toan ven", "integrity", "fraud", "anomaly", "trung lap", "mau thuan", "phi truoc", "coc")):
        signals.append("integrity_keyword")
        return IntentResult("carbon_integrity", lang, 0.91, signals, "Chống lừa đảo & Kiểm tra An toàn")

    if any(p in qf for p in ("ghi so", "thanh toan", "chi tieu", "hom nay chi", "hom nay thu", "payment", "record transaction", "log expense", "tra tien", "giao dich")):
        signals.append("payment_tx_keyword")
        return IntentResult("payment_transaction_assist", lang, 0.91, signals, "Trợ lý Thanh toán & Giao dịch")

    if any(p in qf for p in ("mua sam", "mua giong", "mua phan", "vat tu", "shopping", "procurement", "equipment", "he thong tuoi")):
        signals.append("shopping_keyword")
        return IntentResult("shopping_procurement_assist", lang, 0.89, signals, "Trợ lý Mua sắm")

    if any(p in qf for p in ("ban hang", "chao gia", "rfq", "buyer", "khach hang", "sales", "phu pham", "xuat ban", "hop tac xa")):
        signals.append("sales_rfq_keyword")
        return IntentResult("sales_rfq_assist", lang, 0.89, signals, "Trợ lý Bán hàng")

    if any(p in qf for p in ("viec can lam", "nhac viec", "lich trinh", "checklist", "ca nhan", "ho so con thieu", "todo", "personal assistant", "task")):
        signals.append("personal_task_keyword")
        return IntentResult("personal_workflow_assist", lang, 0.89, signals, "Trợ lý Cá nhân")

    if any(p in qf for p in ("tinh roi", "calculate roi", "dong tien", "cash flow", "payback", "hoan von", "npv", "loi nhuan", "profit", "chi phi dau tu", "investment cost", "tinh doanh thu", "estimate revenue")):
        signals.append("financial_calc_keyword")
        if has_financial_payload:
            return IntentResult("financial_calculation", lang, 0.94, signals, "Trợ lý Tài chính")
        return IntentResult("investment_analysis", lang, 0.88, signals, "Trợ lý Tài chính")

    if any(p in qf for p in ("chi phi", "tai chinh", "ngan sach", "cost", "budget", "finance", "financial", "von", "phan bon", "thu hoach", "carbon revenue", "doanh thu carbon", "ban carbon")):
        signals.append("financial_qa_keyword")
        return IntentResult("financial_qa", lang, 0.86, signals, "Trợ lý Tài chính")

    if any(p in qf for p in ("tin chi", "carbon credit", "ctu", "2015", "ipcc", "verra", "vm0042", "gold standard", "dua ta", "xiem xanh", "mat do", "160", "240", "sinh khoi", "biomass", "tco2", "nghi dinh 29")):
        signals.append("carbon_methodology_keyword")
        return IntentResult("carbon_methodology_qa", lang, 0.88, signals, "Trợ lý Tài chính & Khoa học")

    if has_financial_payload:
        return IntentResult("investment_analysis", lang, 0.78, ["financial_payload_present"], "Trợ lý Tài chính")

    return IntentResult("unknown_or_out_of_scope", lang, 0.50, ["no_domain_match"], "Trợ lý AI")
