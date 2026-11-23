import json
import re
from pathlib import Path
from typing import Any

_SOURCES_PATH = Path(__file__).resolve().parent.parent / "data" / "curated_sources.json"

_STOPWORDS = {
    "tại", "năm", "của", "cho", "bao", "nhiêu", "làm", "sao", "nào", "với",
    "trong", "này", "khi", "hay", "hoặc", "được", "không", "cần", "như",
    "thế", "một", "các", "những", "để", "từ", "trên", "dưới", "theo", "về",
    "bằng", "nhưng", "nếu", "thì", "đang", "sẽ", "đã", "là", "có", "gì",
}

_UNSUPPORTED_FINANCIAL_TERMS = (
    "giá sàn", "giá bán", "giá tín chỉ", "bao nhiêu tiền", "lợi nhuận",
    "doanh thu", "trả bao nhiêu", "price", "payout", "revenue",
)


def load_curated_sources() -> list[dict[str, Any]]:
    with open(_SOURCES_PATH, "r", encoding="utf-8") as f:
        return json.load(f)


def retrieve_relevant_sources(question: str, top_k: int = 3) -> list[dict[str, Any]]:
    q = question.lower()

    # Do not hallucinate financial prices, payouts, or out-of-scope planetary queries
    if any(term in q for term in _UNSUPPORTED_FINANCIAL_TERMS) or "sao hỏa" in q or "mars" in q:
        return []

    tokens = [
        tok for tok in re.findall(r"[\w\d]+", q)
        if len(tok) >= 3 and tok not in _STOPWORDS
    ]
    sources = load_curated_sources()
    scored: list[tuple[int, dict[str, Any]]] = []

    for src in sources:
        score = 0
        for kw in src.get("keywords", []):
            if kw.lower() in q:
                score += 3
        haystack = f"{src['title']} {src['excerpt']}".lower()
        for token in tokens:
            if token in haystack:
                score += 1
        if score >= 3:
            scored.append((score, src))

    scored.sort(key=lambda pair: pair[0], reverse=True)
    return [item[1] for item in scored[:top_k]]
