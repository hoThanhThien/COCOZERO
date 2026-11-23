package com.cococarbon.app.data

import okhttp3.MediaType.Companion.toMediaType
import okhttp3.MultipartBody
import okhttp3.RequestBody.Companion.toRequestBody

class CocoCarbonRepository(
    private val api: CocoCarbonApiService,
    private val tokenStore: TokenStore
) {
    fun hasSession(): Boolean = !tokenStore.cachedToken.isNullOrBlank()

    fun setDemoToken(token: String = "dev-demo:farmer") {
        tokenStore.cachedToken = token
        tokenStore.authMode = "demo"
    }

    fun setFirebaseToken(token: String) {
        tokenStore.cachedToken = token
        tokenStore.authMode = "firebase"
    }

    fun signOut() = tokenStore.clear()

    suspend fun getMe(): UserProfileDto = api.getMe()

    suspend fun recordConsent(type: String, granted: Boolean, text: String) {
        api.recordConsent(RecordConsentRequest(consentType = type, granted = granted, textShown = text))
    }

    suspend fun getFarms(): List<FarmDto> = api.getFarms()

    suspend fun createFarm(req: CreateFarmRequest): FarmDto = api.createFarm(req)

    suspend fun getFarmFiles(farmId: String): List<UploadedFileDto> = api.getFarmFiles(farmId)

    suspend fun uploadFile(
        farmId: String,
        fileName: String,
        mimeType: String,
        bytes: ByteArray,
        category: String = "FarmPhoto"
    ): UploadedFileDto {
        val body = bytes.toRequestBody(mimeType.toMediaType())
        val part = MultipartBody.Part.createFormData("file", fileName, body)
        val catBody = category.toRequestBody("text/plain".toMediaType())
        return api.uploadFarmFile(farmId, part, catBody)
    }

    suspend fun estimateCarbon(req: CarbonEstimateRequest): CarbonEstimateResponse =
        api.estimateCarbon(req)

    suspend fun analyzeDocument(fileId: String, farmId: String?): AiAnalysisResponse =
        api.analyzeDocument(AnalyzeDocumentRequest(fileId, farmId))

    suspend fun getAnalyses(): List<AiAnalysisResponse> = api.getAnalyses()
}
