package com.cococarbon.app.data

import okhttp3.MultipartBody
import okhttp3.RequestBody
import retrofit2.http.Body
import retrofit2.http.GET
import retrofit2.http.Multipart
import retrofit2.http.POST
import retrofit2.http.Part
import retrofit2.http.Path

interface CocoCarbonApiService {
    @GET("api/v1/me")
    suspend fun getMe(): UserProfileDto

    @POST("api/v1/me/consents")
    suspend fun recordConsent(@Body body: RecordConsentRequest)

    @GET("api/v1/farms")
    suspend fun getFarms(): List<FarmDto>

    @POST("api/v1/farms")
    suspend fun createFarm(@Body body: CreateFarmRequest): FarmDto

    @GET("api/v1/farms/{id}/files")
    suspend fun getFarmFiles(@Path("id") farmId: String): List<UploadedFileDto>

    @Multipart
    @POST("api/v1/farms/{id}/files")
    suspend fun uploadFarmFile(
        @Path("id") farmId: String,
        @Part file: MultipartBody.Part,
        @Part("category") category: RequestBody
    ): UploadedFileDto

    @POST("api/v1/carbon-calculator/estimate")
    suspend fun estimateCarbon(@Body body: CarbonEstimateRequest): CarbonEstimateResponse

    @POST("api/v1/ai/document-analysis")
    suspend fun analyzeDocument(@Body body: AnalyzeDocumentRequest): AiAnalysisResponse

    @GET("api/v1/ai/analyses")
    suspend fun getAnalyses(): List<AiAnalysisResponse>
}
