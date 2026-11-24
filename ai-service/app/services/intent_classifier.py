from dataclasses import dataclass
from typing import Literal


IntentType = Literal[
    "financial_calculation",
    "scenario_simulation",
    "investment_analysis",
    "financial_report",
    "financial_qa",
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


def classify_intent(question: str, has_financial_payload: bool = False) -> IntentResult:
    q = question.lower().strip()
    is_vi = any(ch in q for ch in "ăâđêôơưáàảãạấầẩẫậắằẳẵặéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ")
    lang: Literal["vi", "en"] = "vi" if is_vi else "en"

    signals: list[str] = []

    if any(p in q for p in ("sao hỏa", "mars", "bitcoin", "crypto", "xổ số", "lottery")):
        return IntentResult("unknown_or_out_of_scope", lang, 0.95, ["out_of_scope_keyword"])

    if any(p in q for p in ("báo cáo tài chính", "financial report", "executive summary", "xuất báo cáo")):
        signals.append("report_keyword")
        return IntentResult("financial_report", lang, 0.92, signals)

    if any(p in q for p in ("kịch bản", "mô phỏng", "so sánh phương án", "scenario", "sensitivity", "độ nhạy", "nếu giá")):
        signals.append("scenario_keyword")
        return IntentResult("scenario_simulation", lang, 0.90, signals)

    if any(p in q for p in ("gian lận", "lừa đảo", "toàn vẹn", "integrity", "fraud", "anomaly", "trùng lặp", "mâu thuẫn")):
        signals.append("integrity_keyword")
        return IntentResult("carbon_integrity", lang, 0.90, signals)

    if any(p in q for p in ("tính roi", "calculate roi", "dòng tiền", "cash flow", "payback", "hoàn vốn", "npv", "lợi nhuận", "profit", "chi phí đầu tư", "investment cost", "tính doanh thu", "estimate revenue")):
        signals.append("financial_calc_keyword")
        if has_financial_payload:
            return IntentResult("financial_calculation", lang, 0.94, signals)
        return IntentResult("investment_analysis", lang, 0.88, signals)

    if any(p in q for p in ("chi phí", "tài chính", "ngân sách", "cost", "budget", "finance", "financial", "vốn", "phân bón", "thu hoạch", "phụ phẩm", "carbon revenue", "doanh thu carbon", "bán carbon")):
        signals.append("financial_qa_keyword")
        return IntentResult("financial_qa", lang, 0.86, signals)

    if any(p in q for p in ("ctu", "2015", "ipcc", "verra", "vm0042", "gold standard", "dừa ta", "xiêm xanh", "mật độ", "160", "240", "sinh khối", "biomass", "tco2", "nghị định 29")):
        signals.append("carbon_methodology_keyword")
        return IntentResult("carbon_methodology_qa", lang, 0.88, signals)

    if has_financial_payload:
        return IntentResult("investment_analysis", lang, 0.78, ["financial_payload_present"])

    return IntentResult("unknown_or_out_of_scope", lang, 0.50, ["no_domain_match"])
