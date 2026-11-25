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
    "the", "and", "for", "with", "what", "how", "are", "this", "that", "from",
}

_OUT_OF_SCOPE_PATTERNS = (
    "sao hỏa", "mars", "mặt trăng", "moon", "bitcoin", "crypto", "xổ số", "lottery",
    "giá sàn tín chỉ carbon tại sao hỏa",
)


_REQUIRED_SOURCE_KEYS = ("sourceCode", "category", "title", "url", "metadata", "keywords", "excerpt")


def validate_curated_sources() -> list[dict[str, Any]]:
    """Load and validate curated_sources.json strictly so startup/CI fails fast on malformed RAG data."""
    with open(_SOURCES_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)

    if not isinstance(data, list) or len(data) == 0:
        raise ValueError("curated_sources.json must be a non-empty JSON array.")

    seen_codes: set[str] = set()
    for idx, src in enumerate(data):
        if not isinstance(src, dict):
            raise ValueError(f"curated_sources[{idx}] must be a JSON object.")
        for key in _REQUIRED_SOURCE_KEYS:
            if key not in src:
                raise ValueError(f"curated_sources[{idx}] missing required key '{key}'.")
        if not isinstance(src["metadata"], dict):
            raise ValueError(f"curated_sources[{idx}] ('{src.get('sourceCode')}') metadata must be an object.")
        if not isinstance(src["keywords"], list):
            raise ValueError(f"curated_sources[{idx}] ('{src.get('sourceCode')}') keywords must be a list.")
        code = str(src["sourceCode"]).strip()
        if not code or code in seen_codes:
            raise ValueError(f"Duplicate or empty sourceCode '{code}' in curated_sources[{idx}].")
        seen_codes.add(code)
    return data


def load_curated_sources() -> list[dict[str, Any]]:
    return validate_curated_sources()


def retrieve_relevant_sources(
    question: str,
    *,
    category: str | None = None,
    top_k: int = 4,
) -> list[dict[str, Any]]:
    q = question.lower()

    if any(term in q for term in _OUT_OF_SCOPE_PATTERNS):
        return []

    tokens = [
        tok for tok in re.findall(r"[\w\d]+", q)
        if len(tok) >= 3 and tok not in _STOPWORDS
    ]
    sources = load_curated_sources()
    scored: list[tuple[int, dict[str, Any]]] = []

    for src in sources:
        if category and src.get("category") != category:
            continue
        score = 0
        for kw in src.get("keywords", []):
            if kw.lower() in q:
                score += 3
        haystack = f"{src['title']} {src['excerpt']} {src.get('category', '')}".lower()
        for token in tokens:
            if token in haystack:
                score += 1
        if score >= 3:
            enriched = dict(src)
            enriched["relevanceScore"] = round(min(1.0, score / 12.0), 3)
            scored.append((score, enriched))

    scored.sort(key=lambda pair: pair[0], reverse=True)
    return [item[1] for item in scored[:top_k]]
