from datetime import datetime, timezone
from typing import Any, Literal, Optional
from pydantic import BaseModel, Field


class ExtractedField(BaseModel):
    field: str
    value: Optional[str] = None
    confidence: float = Field(ge=0.0, le=1.0)
    provenance: str = "AI_EXTRACTED"


class AuditFinding(BaseModel):
    code: str
    severity: Literal["info", "warning", "error"]
    category: str
    message: str
    evidenceRef: Optional[str] = None


class DuplicateFinding(BaseModel):
    duplicateType: Literal["exact_duplicate", "possible_duplicate"]
    matchedId: Optional[str] = None
    reason: str


class EvidenceItem(BaseModel):
    fileId: str
    pageNumber: Optional[int] = None
    snippet: str
    extractedValue: Optional[str] = None
    confidence: float = Field(ge=0.0, le=1.0)
    reason: str


class ReadinessCriterionItem(BaseModel):
    criterion: str
    status: Literal["Đủ", "Thiếu", "Chưa kiểm tra", "Không áp dụng"]
    notes: str


class ReadinessRubric(BaseModel):
    label: str = "Mức độ đầy đủ hồ sơ / Project Readiness"
    score: float = Field(ge=0.0, le=100.0)
    disclaimer: str = (
        "Điểm này chỉ phản ánh mức độ đầy đủ tài liệu theo rubric nội bộ; "
        "KHÔNG phải điểm hợp lệ tín chỉ, chứng nhận hay xác suất phát hành tín chỉ."
    )
    items: list[ReadinessCriterionItem] = Field(default_factory=list)


class FraudSignal(BaseModel):
    code: str
    severity: Literal["info", "warning", "high"]
    evidenceSnippet: str
    explanation: str


class DocumentAnalysisResponse(BaseModel):
    analysisId: str
    fileId: str
    status: str
    isFallback: bool = False
    extractedFields: list[ExtractedField] = Field(default_factory=list)
    findings: list[AuditFinding] = Field(default_factory=list)
    missingFields: list[str] = Field(default_factory=list)
    duplicates: list[DuplicateFinding] = Field(default_factory=list)
    evidence: list[EvidenceItem] = Field(default_factory=list)
    confidenceNotes: list[str] = Field(default_factory=list)
    readinessRubric: Optional[ReadinessRubric] = None
    projectReadinessScore: float = Field(default=0.0, ge=0.0, le=100.0)
    documentCompletenessScore: float = Field(default=0.0, ge=0.0, le=100.0)
    carbonIntegrityRiskScore: float = Field(default=0.0, ge=0.0, le=100.0)
    fraudRiskScore: float = Field(default=0.0, ge=0.0, le=100.0)
    fraudRiskLevel: Literal["Low", "Medium", "High", "Unknown"] = "Low"
    fraudSignals: list[FraudSignal] = Field(default_factory=list)
    reviewerRequired: bool = True
    provider: str
    modelName: str
    promptVersion: str
    createdAt: datetime = Field(default_factory=lambda: datetime.now(timezone.utc))


class AssistantQueryRequest(BaseModel):
    role: str = "Farmer"
    question: str
    context_data: dict[str, Any] = Field(default_factory=dict)


class CitationItem(BaseModel):
    sourceCode: str
    title: str
    url: str
    excerpt: str


class AssistantQueryResponse(BaseModel):
    answer: str
    hasEnoughEvidence: bool
    citations: list[CitationItem] = Field(default_factory=list)
    disclaimer: str = (
        "Thông tin chỉ mang tính hỗ trợ từ kho tài liệu khoa học được kiểm chứng; "
        "không phải tư vấn pháp lý, chứng nhận carbon hay xác minh VVB độc lập."
    )
    modelName: str
    isFallback: bool = False
