from __future__ import annotations

from datetime import datetime, timezone
from typing import Any, Literal, Optional
import uuid
from pydantic import BaseModel, Field

from app.services.document_extractor import (
    inspect_document_bytes,
    run_consistency_and_duplicate_checks,
)
from app.services.readiness_rubric import evaluate_project_readiness


class ExplainableIntegrityAlert(BaseModel):
    code: str
    category: Literal["document_missing", "data_inconsistency", "anomaly_detection", "anti_fraud", "duplicate"]
    severity: Literal["info", "warning", "high"]
    scoreTypeAffected: Literal["project_readiness", "document_completeness", "carbon_integrity_risk"]
    evidenceSnippet: str
    explanation: str
    recommendedAction: str


class CarbonIntegrityCheckRequest(BaseModel):
    farmName: str = ""
    province: str = "Vĩnh Long"
    commune: str = ""
    areaHa: Optional[float] = Field(default=None, gt=0)
    treeCount: Optional[int] = Field(default=None, gt=0)
    variety: Optional[str] = None
    ageYears: Optional[int] = None
    latitude: Optional[float] = None
    longitude: Optional[float] = None
    declaredTextClaims: str = ""
    intendedMethodology: str = ""
    baselineSummary: str = ""
    monitoringPlanSummary: str = ""
    hasLandDocument: bool = False
    hasFieldPhotos: bool = False
    hasOwnerConsent: bool = False
    fileSha256: str = ""
    knownHashes: list[str] = Field(default_factory=list)
    knownCoordinates: list[dict[str, Any]] = Field(default_factory=list)


class CarbonIntegrityCheckResponse(BaseModel):
    checkId: str
    checkedAt: str
    # Strictly separated 3 scores per Section 6 requirement
    projectReadinessScore: float = Field(ge=0.0, le=100.0)
    documentCompletenessScore: float = Field(ge=0.0, le=100.0)
    carbonIntegrityRiskScore: float = Field(ge=0.0, le=100.0)
    riskLevel: Literal["Low", "Medium", "High"]
    integrityStatus: Literal[
        "ReadyForInternalReview",
        "IncompleteDocuments_NotFraud",
        "ReviewRequired_AnomaliesDetected",
        "HighIntegrityRisk_Flagged",
    ]
    missingDocuments: list[str] = Field(default_factory=list)
    explainableAlerts: list[ExplainableIntegrityAlert] = Field(default_factory=list)
    scoreSeparationExplanation: str = (
        "Project Readiness Score, Document Completeness Score và Carbon Integrity Risk Score được tính tách biệt: "
        "thiếu tài liệu chỉ làm giảm Document Completeness Score và KHÔNG bị quy kết là hành vi gian lận."
    )
    reviewerRequired: bool = True
    disclaimer: str = (
        "Kết quả kiểm tra toàn vẹn hỗ trợ rà soát nội bộ; không thay thế thẩm định độc lập (VVB) hay registry chính thức."
    )


def evaluate_carbon_integrity(
    req: CarbonIntegrityCheckRequest,
    *,
    file_bytes: bytes | None = None,
    mime_type: str | None = None,
    extracted_fields: list[dict[str, Any]] | None = None,
) -> CarbonIntegrityCheckResponse:
    """Compute separated 3-score Carbon Integrity assessment with explainable alerts."""
    alerts: list[ExplainableIntegrityAlert] = []
    missing_docs: list[str] = []

    # 1. Document Completeness Score (0-100) — missing items do NOT add fraud risk
    doc_checks = [
        ("Giấy tờ quyền sử dụng đất / chứng từ vườn", req.hasLandDocument),
        ("Ảnh hiện trường vườn dừa có tọa độ/ngày chụp", req.hasFieldPhotos),
        ("Bản ghi đồng thuận (Consent) của chủ hộ", req.hasOwnerConsent),
        ("Tài liệu mô tả Baseline & Phương pháp dự kiến", bool(req.baselineSummary.strip() and req.intendedMethodology.strip())),
        ("Kế hoạch giám sát định kỳ (Monitoring Plan)", bool(req.monitoringPlanSummary.strip())),
    ]
    present_docs = 0
    for label, ok in doc_checks:
        if ok:
            present_docs += 1
        else:
            missing_docs.append(label)
            alerts.append(
                ExplainableIntegrityAlert(
                    code="MISSING_SUPPORTING_DOCUMENT",
                    category="document_missing",
                    severity="info",
                    scoreTypeAffected="document_completeness",
                    evidenceSnippet=f"Thiếu: {label}",
                    explanation=f"Hồ sơ hiện chưa bổ sung '{label}'. Đây là thiếu sót tài liệu, KHÔNG phải bằng chứng gian lận.",
                    recommendedAction=f"Nhắc nông hộ / đơn vị dự án tải bổ sung '{label}'.",
                )
            )

    has_malformed_pdf = False
    pages_text: list[tuple[int, str]] = []
    if file_bytes and mime_type:
        pdf_info = inspect_document_bytes(file_bytes, mime_type)
        has_malformed_pdf = pdf_info.is_malformed
        pages_text = pdf_info.pages_text
        if has_malformed_pdf:
            alerts.append(
                ExplainableIntegrityAlert(
                    code="MALFORMED_DOCUMENT_FILE",
                    category="document_missing",
                    severity="warning",
                    scoreTypeAffected="document_completeness",
                    evidenceSnippet=pdf_info.error_message or "Corrupted PDF",
                    explanation="Tệp PDF không thể mở/đọc bằng PyMuPDF; cần tải lại bản rõ nét.",
                    recommendedAction="Yêu cầu người dùng upload lại tệp PDF hợp lệ.",
                )
            )
    if req.declaredTextClaims.strip():
        pages_text.append((1, req.declaredTextClaims.strip()))

    doc_completeness = round((present_docs / len(doc_checks)) * 100.0, 2)
    if has_malformed_pdf:
        doc_completeness = max(0.0, round(doc_completeness - 20.0, 2))

    # 2. Project Readiness Score (0-100)
    farm_ctx = {
        "farmName": req.farmName,
        "commune": req.commune,
        "areaHa": req.areaHa,
        "treeCount": req.treeCount,
        "latitude": req.latitude,
        "longitude": req.longitude,
    }
    proj_ctx = {
        "name": req.farmName,
        "intendedMethodology": req.intendedMethodology,
        "baselineSummary": req.baselineSummary,
        "monitoringPlanSummary": req.monitoringPlanSummary,
    }
    readiness = evaluate_project_readiness(
        farm_context=farm_ctx,
        project_context=proj_ctx,
        extracted_fields=extracted_fields or ([] if not req.hasLandDocument else [{"field": "areaHa", "value": str(req.areaHa)}]),
        has_malformed_pdf=has_malformed_pdf,
    )

    # 3. Carbon Integrity Risk Score (0-100) — consistency, anomalies, duplicates & greenwashing ONLY
    _, duplicates, fraud_score, _, fraud_signals = run_consistency_and_duplicate_checks(
        file_id="integrity-check",
        sha256_hash=req.fileSha256,
        known_hashes=req.knownHashes,
        known_coordinates=req.knownCoordinates,
        pages_text=pages_text,
        extracted_fields=extracted_fields or [],
        farm_context=farm_ctx,
    )

    for sig in fraud_signals:
        alerts.append(
            ExplainableIntegrityAlert(
                code=sig["code"],
                category="duplicate" if "DUPLICATE" in sig["code"] or "OVERLAP" in sig["code"] else "anti_fraud",
                severity="high" if sig["severity"] == "high" else "warning",
                scoreTypeAffected="carbon_integrity_risk",
                evidenceSnippet=sig["evidenceSnippet"],
                explanation=sig["explanation"],
                recommendedAction="Reviewer đối chiếu chứng từ gốc/tọa độ thực địa trước khi phê duyệt.",
            )
        )

    # Additional agronomic/scientific anomaly checks on declared farm parameters
    if req.areaHa and req.treeCount:
        density = req.treeCount / req.areaHa
        if density > 350 or density < 80:
            if not any(a.code == "FRAUD_DENSITY_OUTLIER" for a in alerts):
                fraud_score = min(100.0, fraud_score + 20.0)
                alerts.append(
                    ExplainableIntegrityAlert(
                        code="ANOMALY_TREE_DENSITY",
                        category="anomaly_detection",
                        severity="warning",
                        scoreTypeAffected="carbon_integrity_risk",
                        evidenceSnippet=f"{req.treeCount} cây / {req.areaHa} ha = {density:.1f} cây/ha",
                        explanation="Mật độ khai báo lệch xa khoảng khảo sát Bến Tre 2024 (160–240 cây/ha); cần kiểm tra lại diện tích hoặc số cây.",
                        recommendedAction="Xác minh lại số cây đếm thực tế hoặc ranh giới thửa đất.",
                    )
                )

    if req.ageYears is not None and req.ageYears not in (4, 10):
        alerts.append(
            ExplainableIntegrityAlert(
                code="CTU_FACTOR_OUT_OF_AGE_DOMAIN",
                category="data_inconsistency",
                severity="info",
                scoreTypeAffected="project_readiness",
                evidenceSnippet=f"ageYears={req.ageYears}",
                explanation="Tuổi cây khác 4 và 10 năm chưa có hệ số CTU 2015 phù hợp (không nội suy). Cần thu thập dữ liệu đo DBH/chiều cao nếu định nâng cấp Tier 2/3.",
                recommendedAction="Ghi nhận quan trắc thực địa; không áp dụng hệ số CTU 2015 cho cấp tuổi không phù hợp.",
            )
        )

    risk_score = min(100.0, round(fraud_score, 1))
    risk_level: Literal["Low", "Medium", "High"] = (
        "High" if risk_score >= 60.0 else ("Medium" if risk_score >= 25.0 else "Low")
    )

    if risk_level == "High":
        status = "HighIntegrityRisk_Flagged"
    elif risk_level == "Medium":
        status = "ReviewRequired_AnomaliesDetected"
    elif doc_completeness < 80.0:
        status = "IncompleteDocuments_NotFraud"
    else:
        status = "ReadyForInternalReview"

    return CarbonIntegrityCheckResponse(
        checkId=str(uuid.uuid4()),
        checkedAt=datetime.now(timezone.utc).isoformat(),
        projectReadinessScore=readiness.score,
        documentCompletenessScore=doc_completeness,
        carbonIntegrityRiskScore=risk_score,
        riskLevel=risk_level,
        integrityStatus=status,
        missingDocuments=missing_docs,
        explainableAlerts=alerts,
    )
