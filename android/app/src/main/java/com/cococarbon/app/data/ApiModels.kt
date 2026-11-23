package com.cococarbon.app.data

data class UserRoleDto(
    val roleCode: String,
    val approvalStatus: String
)

data class UserProfileDto(
    val id: String,
    val firebaseUid: String,
    val email: String,
    val displayName: String,
    val phone: String?,
    val roles: List<UserRoleDto>
)

data class RecordConsentRequest(
    val consentType: String,
    val granted: Boolean,
    val policyVersion: String = "v1.0-2026-11",
    val textShown: String
)

data class FarmDto(
    val id: String,
    val name: String,
    val province: String,
    val commune: String,
    val historicalRegion: String,
    val areaHa: Double,
    val treeCount: Int,
    val variety: String,
    val ageYears: Int,
    val latitude: Double?,
    val longitude: Double?,
    val status: String,
    val dataOriginTag: String
)

data class CreateFarmRequest(
    val name: String,
    val province: String = "Vĩnh Long",
    val commune: String,
    val historicalRegion: String = "Vùng dừa Bến Tre",
    val areaHa: Double,
    val treeCount: Int,
    val variety: String,
    val ageYears: Int,
    val latitude: Double?,
    val longitude: Double?,
    val hasLocationConsent: Boolean,
    val hasPhotoConsent: Boolean
)

data class CarbonEstimateRequest(
    val farmId: String?,
    val areaHa: Double,
    val treeCount: Int,
    val variety: String,
    val treeAgeYears: Int,
    val includeArithmeticPerTreeAllocation: Boolean = false
)

data class FactorUsedDto(
    val id: String,
    val code: String,
    val varietyDisplayName: String,
    val treeAgeYears: Int,
    val valueTCO2PerHa: Double,
    val version: String
)

data class CarbonEstimateResponse(
    val calculationId: String,
    val resultLabel: String,
    val estimatedCO2EquivalentT: Double,
    val unit: String,
    val factorUsed: FactorUsedDto,
    val observedTreeDensityPerHa: Double,
    val densityContextNote: String,
    val formulaVersion: String,
    val sourceCitation: String,
    val limitations: String,
    val disclaimer: String
)

data class UploadedFileDto(
    val id: String,
    val farmId: String,
    val fileName: String,
    val mimeType: String,
    val sizeBytes: Long,
    val sha256Hash: String,
    val version: Int
)

data class AnalyzeDocumentRequest(
    val fileId: String,
    val farmId: String?
)

data class AiExtractedFieldDto(
    val field: String,
    val value: String?,
    val confidence: Double,
    val provenance: String
)

data class AiFindingDto(
    val code: String,
    val severity: String,
    val message: String
)

data class AiEvidenceDto(
    val fileId: String,
    val pageNumber: Int?,
    val snippet: String,
    val extractedValue: String?,
    val reason: String
)

data class AiReadinessRubricDto(
    val label: String,
    val score: Double,
    val disclaimer: String
)

data class AiAnalysisResponse(
    val analysisId: String,
    val fileId: String?,
    val status: String,
    val isFallback: Boolean,
    val verificationNotice: String?,
    val extractedFields: List<AiExtractedFieldDto>?,
    val findings: List<AiFindingDto>?,
    val missingFields: List<String>?,
    val evidence: List<AiEvidenceDto>?,
    val readinessRubric: AiReadinessRubricDto?,
    val modelName: String,
    val reviewStatus: String
)
