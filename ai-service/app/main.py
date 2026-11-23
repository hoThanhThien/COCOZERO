import json
import uuid
from fastapi import FastAPI, File, Form, Header, HTTPException, UploadFile
from app.config import settings
from app.providers.base import BaseAiProvider
from app.providers.gemini_provider import GeminiInteractionsProvider
from app.providers.heuristic_provider import HeuristicFallbackProvider
from app.schemas import (
    AssistantQueryRequest,
    AssistantQueryResponse,
    AuditFinding,
    CitationItem,
    DocumentAnalysisResponse,
    DuplicateFinding,
    EvidenceItem,
    ExtractedField,
)
from app.services.document_extractor import (
    inspect_document_bytes,
    run_consistency_and_duplicate_checks,
)
from app.services.rag_knowledge import retrieve_relevant_sources
from app.services.readiness_rubric import evaluate_project_readiness

app = FastAPI(
    title="CocoCarbon Internal AI Service",
    version="1.0.0",
    docs_url=None,
    redoc_url=None,
)


def get_provider() -> BaseAiProvider:
    if settings.ai_provider.lower() == "gemini" and settings.gemini_api_key:
        return GeminiInteractionsProvider(
            api_key=settings.gemini_api_key,
            model_name=settings.gemini_model,
            timeout_seconds=settings.ai_request_timeout_seconds,
            max_retries=settings.ai_max_retries,
        )
    return HeuristicFallbackProvider("GEMINI_API_KEY chưa cấu hình hoặc đang chạy ở chế độ fallback")


def verify_internal_key(x_internal_api_key: str | None) -> None:
    if settings.ai_service_internal_key and x_internal_api_key != settings.ai_service_internal_key:
        raise HTTPException(status_code=403, detail="Unauthorized internal AI call.")


@app.get("/health")
def health() -> dict[str, str]:
    return {
        "status": "healthy",
        "provider": settings.ai_provider,
        "model": settings.gemini_model,
        "promptVersion": settings.prompt_version,
    }


@app.post("/internal/v1/analyze-document", response_model=DocumentAnalysisResponse)
async def analyze_document(
    file: UploadFile = File(...),
    file_id: str = Form(...),
    sha256_hash: str = Form(""),
    farm_context_json: str = Form("{}"),
    project_context_json: str = Form("{}"),
    known_hashes_json: str = Form("[]"),
    x_internal_api_key: str | None = Header(default=None),
) -> DocumentAnalysisResponse:
    verify_internal_key(x_internal_api_key)

    file_bytes = await file.read()
    if len(file_bytes) == 0:
        raise HTTPException(status_code=400, detail="Empty file.")
    if len(file_bytes) > settings.ai_max_file_bytes:
        raise HTTPException(status_code=413, detail="File exceeds AI max byte limit.")

    mime_type = file.content_type or "application/octet-stream"
    if mime_type not in ("application/pdf", "image/jpeg", "image/png"):
        raise HTTPException(status_code=400, detail="Unsupported MIME type for AI analysis.")

    try:
        farm_context = json.loads(farm_context_json) if farm_context_json else {}
        project_context = json.loads(project_context_json) if project_context_json else {}
        known_hashes = json.loads(known_hashes_json) if known_hashes_json else []
    except json.JSONDecodeError as exc:
        raise HTTPException(status_code=400, detail=f"Invalid context JSON: {exc}") from exc

    pdf_info = inspect_document_bytes(file_bytes, mime_type)

    findings: list[AuditFinding] = []
    if pdf_info.is_malformed:
        findings.append(AuditFinding(
            code="MALFORMED_PDF",
            severity="error",
            category="document_integrity",
            message=pdf_info.error_message or "PDF bị hỏng hoặc không hợp lệ.",
            evidenceRef=f"file:{file_id}",
        ))
        rubric = evaluate_project_readiness(
            farm_context=farm_context,
            project_context=project_context,
            extracted_fields=[],
            has_malformed_pdf=True,
        )
        return DocumentAnalysisResponse(
            analysisId=str(uuid.uuid4()),
            fileId=file_id,
            status="MalformedDocument",
            isFallback=True,
            extractedFields=[],
            findings=findings,
            missingFields=["Tệp PDF lỗi cần nông dân/HTX tải lại bản hợp lệ"],
            duplicates=[],
            evidence=[],
            confidenceNotes=["Không thể đọc tài liệu; chưa xác minh độc lập."],
            readinessRubric=rubric,
            reviewerRequired=True,
            provider="pymupdf",
            modelName="none",
            promptVersion=settings.prompt_version,
        )

    provider = get_provider()
    extraction, is_fallback = provider.extract_document(
        file_id=file_id,
        filename=file.filename or "document",
        mime_type=mime_type,
        file_bytes=file_bytes,
        extracted_text_pages=pdf_info.pages_text,
        farm_context=farm_context,
        project_context=project_context,
    )

    rule_findings, duplicates = run_consistency_and_duplicate_checks(
        file_id=file_id,
        sha256_hash=sha256_hash,
        known_hashes=known_hashes,
        extracted_fields=extraction.extractedFields,
        farm_context=farm_context,
    )

    all_findings = findings + [AuditFinding(**f) for f in rule_findings]
    evidence_items = [EvidenceItem(**e) for e in extraction.evidence]

    if not evidence_items:
        all_findings.append(AuditFinding(
            code="NO_DIRECT_EVIDENCE",
            severity="warning",
            category="evidence",
            message="Không tìm thấy trích dẫn/bằng chứng trực tiếp rõ ràng trong tài liệu — giữ trạng thái Chưa xác minh độc lập.",
            evidenceRef=f"file:{file_id}",
        ))

    rubric = evaluate_project_readiness(
        farm_context=farm_context,
        project_context=project_context,
        extracted_fields=extraction.extractedFields,
        has_malformed_pdf=False,
    )

    confidence_notes = list(extraction.confidenceNotes)
    if not any("Chưa xác minh độc lập" in n for n in confidence_notes):
        confidence_notes.append("Chưa xác minh độc lập; kết quả AI chỉ phục vụ rà soát nội bộ.")

    return DocumentAnalysisResponse(
        analysisId=str(uuid.uuid4()),
        fileId=file_id,
        status="Fallback_Heuristic" if is_fallback else "Completed",
        isFallback=is_fallback,
        extractedFields=[ExtractedField(**f) for f in extraction.extractedFields],
        findings=all_findings,
        missingFields=extraction.missingFields,
        duplicates=[DuplicateFinding(**d) for d in duplicates],
        evidence=evidence_items,
        confidenceNotes=confidence_notes,
        readinessRubric=rubric,
        reviewerRequired=True,
        provider=provider.provider_name if not is_fallback else "heuristic_fallback",
        modelName=provider.model_name,
        promptVersion=settings.prompt_version,
    )


@app.post("/internal/v1/assistant/query", response_model=AssistantQueryResponse)
def assistant_query(
    req: AssistantQueryRequest,
    x_internal_api_key: str | None = Header(default=None),
) -> AssistantQueryResponse:
    verify_internal_key(x_internal_api_key)

    retrieved = retrieve_relevant_sources(req.question)
    if not retrieved:
        return AssistantQueryResponse(
            answer=(
                "Chưa có đủ dữ liệu trong kho nguồn khoa học và pháp lý được kiểm chứng ([R1]-[R12]) "
                "để trả lời câu hỏi này. CocoCarbon AI không tự bịa số liệu, hệ số hay quy chuẩn."
            ),
            hasEnoughEvidence=False,
            citations=[],
            modelName="curated-rag-guard",
            isFallback=False,
        )

    provider = get_provider()
    answer_text, is_fallback = provider.synthesize_rag_answer(
        role=req.role,
        question=req.question,
        context_data=req.context_data,
        retrieved_sources=retrieved,
    )
    citations = [
        CitationItem(
            sourceCode=s["sourceCode"],
            title=s["title"],
            url=s["url"],
            excerpt=s["excerpt"],
        )
        for s in retrieved
    ]

    return AssistantQueryResponse(
        answer=answer_text,
        hasEnoughEvidence=True,
        citations=citations,
        modelName=provider.model_name if not is_fallback else "curated-rag-fallback",
        isFallback=is_fallback,
    )
