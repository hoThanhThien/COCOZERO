package com.cococarbon.app.ui

import android.app.Application
import android.content.Context
import androidx.lifecycle.AndroidViewModel
import androidx.lifecycle.viewModelScope
import com.cococarbon.app.data.AiAnalysisResponse
import com.cococarbon.app.data.ApiClientFactory
import com.cococarbon.app.data.CarbonEstimateRequest
import com.cococarbon.app.data.CarbonEstimateResponse
import com.cococarbon.app.data.CocoCarbonRepository
import com.cococarbon.app.data.CreateFarmRequest
import com.cococarbon.app.data.FarmDto
import com.cococarbon.app.data.FirebaseGoogleAuthHelper
import com.cococarbon.app.data.TokenStore
import com.cococarbon.app.data.UploadedFileDto
import com.cococarbon.app.data.UserProfileDto
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import kotlinx.coroutines.launch

data class CocoUiState(
    val isLoading: Boolean = false,
    val isLoggedIn: Boolean = false,
    val error: String? = null,
    val infoMessage: String? = null,
    val profile: UserProfileDto? = null,
    val farms: List<FarmDto> = emptyList(),
    val selectedFarmFiles: List<UploadedFileDto> = emptyList(),
    val estimateResult: CarbonEstimateResponse? = null,
    val latestAiResult: AiAnalysisResponse? = null,
    val analyses: List<AiAnalysisResponse> = emptyList()
)

class MainViewModel(application: Application) : AndroidViewModel(application) {
    private val tokenStore = TokenStore(application)
    private val repo = CocoCarbonRepository(ApiClientFactory.create(tokenStore), tokenStore)

    private val _state = MutableStateFlow(CocoUiState(isLoggedIn = repo.hasSession()))
    val state: StateFlow<CocoUiState> = _state.asStateFlow()

    fun clearBanner() {
        _state.value = _state.value.copy(error = null, infoMessage = null)
    }

    fun checkSessionAndLoad(onDone: (Boolean) -> Unit) {
        viewModelScope.launch {
            if (!repo.hasSession()) {
                _state.value = _state.value.copy(isLoggedIn = false)
                onDone(false)
                return@launch
            }
            try {
                val me = repo.getMe()
                val farms = repo.getFarms()
                val analyses = repo.getAnalyses()
                _state.value = _state.value.copy(
                    isLoggedIn = true,
                    profile = me,
                    farms = farms,
                    analyses = analyses
                )
                onDone(true)
            } catch (_: Exception) {
                _state.value = _state.value.copy(isLoggedIn = false)
                onDone(false)
            }
        }
    }

    fun signInDemo(onSuccess: () -> Unit) {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                repo.setDemoToken()
                val me = repo.getMe()
                val farms = repo.getFarms()
                val analyses = repo.getAnalyses()
                _state.value = _state.value.copy(
                    isLoading = false,
                    isLoggedIn = true,
                    profile = me,
                    farms = farms,
                    analyses = analyses,
                    infoMessage = "Đăng nhập chế độ DEMO thành công."
                )
                onSuccess()
            } catch (e: Exception) {
                _state.value = _state.value.copy(isLoading = false, error = "Lỗi kết nối API: ${e.message}")
            }
        }
    }

    fun signInWithGoogle(activityContext: Context, onSuccess: () -> Unit) {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                val token = FirebaseGoogleAuthHelper.signInWithGoogle(activityContext)
                repo.setFirebaseToken(token)
                val me = repo.getMe()
                val farms = repo.getFarms()
                val analyses = repo.getAnalyses()
                _state.value = _state.value.copy(
                    isLoading = false,
                    isLoggedIn = true,
                    profile = me,
                    farms = farms,
                    analyses = analyses
                )
                onSuccess()
            } catch (e: Exception) {
                _state.value = _state.value.copy(
                    isLoading = false,
                    error = "Google Sign-In chưa sẵn sàng hoặc bị hủy: ${e.message}"
                )
            }
        }
    }

    fun recordOnboardingConsents() {
        viewModelScope.launch {
            try {
                repo.recordConsent("GpsLocation", true, "Đồng thuận thu thập vị trí vườn dừa trên Android")
                repo.recordConsent("FieldPhoto", true, "Đồng thuận chụp ảnh hiện trường vườn dừa")
                repo.recordConsent("AiThirdPartyProcessing", true, "Đồng thuận xử lý hồ sơ qua AI nội bộ")
                _state.value = _state.value.copy(infoMessage = "Đã lưu đồng thuận (ConsentRecord) trên máy chủ.")
            } catch (e: Exception) {
                _state.value = _state.value.copy(error = e.message)
            }
        }
    }

    fun refreshFarms() {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                val farms = repo.getFarms()
                _state.value = _state.value.copy(isLoading = false, farms = farms)
            } catch (e: Exception) {
                _state.value = _state.value.copy(isLoading = false, error = e.message)
            }
        }
    }

    fun createFarm(
        name: String,
        commune: String,
        areaHa: Double,
        treeCount: Int,
        variety: String,
        ageYears: Int,
        lat: Double?,
        lng: Double?,
        locationConsent: Boolean,
        photoConsent: Boolean,
        onCreated: () -> Unit
    ) {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                repo.createFarm(
                    CreateFarmRequest(
                        name = name,
                        commune = commune,
                        areaHa = areaHa,
                        treeCount = treeCount,
                        variety = variety,
                        ageYears = ageYears,
                        latitude = if (locationConsent) lat else null,
                        longitude = if (locationConsent) lng else null,
                        hasLocationConsent = locationConsent,
                        hasPhotoConsent = photoConsent
                    )
                )
                val farms = repo.getFarms()
                _state.value = _state.value.copy(
                    isLoading = false,
                    farms = farms,
                    infoMessage = "Đã tạo vườn dừa mới (USER-DECLARED)."
                )
                onCreated()
            } catch (e: Exception) {
                _state.value = _state.value.copy(isLoading = false, error = "Lỗi tạo vườn: ${e.message}")
            }
        }
    }

    fun loadFarmFiles(farmId: String) {
        viewModelScope.launch {
            try {
                val files = repo.getFarmFiles(farmId)
                _state.value = _state.value.copy(selectedFarmFiles = files)
            } catch (e: Exception) {
                _state.value = _state.value.copy(error = e.message)
            }
        }
    }

    fun uploadAndOptionallyAnalyze(
        farmId: String,
        fileName: String,
        mimeType: String,
        bytes: ByteArray,
        category: String,
        runAi: Boolean
    ) {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                val uploaded = repo.uploadFile(farmId, fileName, mimeType, bytes, category)
                val files = repo.getFarmFiles(farmId)
                var aiRes: AiAnalysisResponse? = _state.value.latestAiResult
                if (runAi) {
                    aiRes = repo.analyzeDocument(uploaded.id, farmId)
                }
                val analyses = repo.getAnalyses()
                _state.value = _state.value.copy(
                    isLoading = false,
                    selectedFarmFiles = files,
                    latestAiResult = aiRes,
                    analyses = analyses,
                    infoMessage = if (runAi) "Đã upload và chạy AI Auditor!" else "Đã tải tệp lên kho riêng tư!"
                )
            } catch (e: Exception) {
                _state.value = _state.value.copy(isLoading = false, error = "Upload/AI thất bại: ${e.message}")
            }
        }
    }

    fun runCalculator(farmId: String?, areaHa: Double, treeCount: Int, variety: String, ageYears: Int) {
        viewModelScope.launch {
            _state.value = _state.value.copy(isLoading = true, error = null)
            try {
                // Always delegates calculation to authoritative C# ASP.NET Core API
                val res = repo.estimateCarbon(
                    CarbonEstimateRequest(
                        farmId = farmId,
                        areaHa = areaHa,
                        treeCount = treeCount,
                        variety = variety,
                        treeAgeYears = ageYears
                    )
                )
                _state.value = _state.value.copy(isLoading = false, estimateResult = res)
            } catch (e: Exception) {
                _state.value = _state.value.copy(
                    isLoading = false,
                    estimateResult = null,
                    error = "Không tính được (lưu ý chỉ nhận Dừa Ta/Xiêm xanh tuổi 4 hoặc 10): ${e.message}"
                )
            }
        }
    }

    fun signOut(onDone: () -> Unit) {
        repo.signOut()
        _state.value = CocoUiState(isLoggedIn = false)
        onDone()
    }
}
