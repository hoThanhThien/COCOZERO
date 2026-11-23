package com.cococarbon.app.ui

import android.Manifest
import android.annotation.SuppressLint
import android.content.Context
import android.location.LocationManager
import android.net.Uri
import androidx.activity.compose.rememberLauncherForActivityResult
import androidx.activity.result.contract.ActivityResultContracts
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Box
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.foundation.rememberScrollState
import androidx.compose.foundation.verticalScroll
import androidx.compose.material3.Button
import androidx.compose.material3.ButtonDefaults
import androidx.compose.material3.Card
import androidx.compose.material3.CardDefaults
import androidx.compose.material3.Checkbox
import androidx.compose.material3.CircularProgressIndicator
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.Text
import androidx.compose.material3.TopAppBar
import androidx.compose.material3.TopAppBarDefaults
import androidx.compose.runtime.Composable
import androidx.compose.runtime.LaunchedEffect
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Alignment
import androidx.compose.ui.Modifier
import androidx.compose.ui.graphics.Color
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp
import androidx.core.content.FileProvider
import com.cococarbon.app.BuildConfig
import com.cococarbon.app.data.FarmDto
import com.cococarbon.app.ui.theme.EmeraldGreen
import com.cococarbon.app.ui.theme.ForestGreen
import com.cococarbon.app.ui.theme.MintSurface
import java.io.File

@Composable
fun SplashScreen(viewModel: MainViewModel, onNavigateHome: () -> Unit, onNavigateLogin: () -> Unit) {
    LaunchedEffect(Unit) {
        viewModel.checkSessionAndLoad { loggedIn ->
            if (loggedIn) onNavigateHome() else onNavigateLogin()
        }
    }
    Box(modifier = Modifier.fillMaxSize(), contentAlignment = Alignment.Center) {
        Column(horizontalAlignment = Alignment.CenterHorizontally) {
            Text("CocoCarbon AI", style = MaterialTheme.typography.headlineMedium, fontWeight = FontWeight.Bold, color = ForestGreen)
            Spacer(Modifier.height(8.dp))
            Text("Vùng dừa Bến Tre • Tỉnh Vĩnh Long", color = Color.Gray)
            Spacer(Modifier.height(16.dp))
            CircularProgressIndicator(color = EmeraldGreen)
        }
    }
}

@Composable
fun LoginOnboardingScreen(
    state: CocoUiState,
    viewModel: MainViewModel,
    onLoggedIn: () -> Unit
) {
    val context = LocalContext.current
    var consentAccepted by remember { mutableStateOf(true) }

    Column(
        modifier = Modifier
            .fillMaxSize()
            .verticalScroll(rememberScrollState())
            .padding(20.dp),
        verticalArrangement = Arrangement.Center
    ) {
        Text("CocoCarbon AI Mobile", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.Bold, color = ForestGreen)
        Text("Số hóa vườn dừa hiện trường, GPS, ảnh và Calculator CTU 2015", color = Color.DarkGray)
        Spacer(Modifier.height(16.dp))

        Card(colors = CardDefaults.cardColors(containerColor = MintSurface)) {
            Column(Modifier.padding(14.dp)) {
                Text("Điều khoản & Đồng thuận dữ liệu", fontWeight = FontWeight.Bold, color = ForestGreen)
                Text("Ứng dụng chỉ xin quyền GPS và Camera khi bạn tạo vườn hoặc chụp ảnh minh chứng, và gửi tài liệu tới AI theo đồng thuận.", style = MaterialTheme.typography.bodySmall)
                Row(verticalAlignment = Alignment.CenterVertically) {
                    Checkbox(checked = consentAccepted, onCheckedChange = { consentAccepted = it })
                    Text("Tôi đồng ý điều khoản và thu thập dữ liệu có kiểm soát", style = MaterialTheme.typography.bodySmall)
                }
            }
        }

        Spacer(Modifier.height(16.dp))
        state.error?.let {
            Text(it, color = Color.Red, style = MaterialTheme.typography.bodySmall)
            Spacer(Modifier.height(8.dp))
        }

        Button(
            onClick = {
                viewModel.signInWithGoogle(context) {
                    if (consentAccepted) viewModel.recordOnboardingConsents()
                    onLoggedIn()
                }
            },
            enabled = !state.isLoading && consentAccepted,
            modifier = Modifier.fillMaxWidth(),
            colors = ButtonDefaults.buttonColors(containerColor = ForestGreen)
        ) {
            Text("Đăng nhập Google (Firebase Auth)")
        }

        if (BuildConfig.DEMO_MODE_ENABLED) {
            Spacer(Modifier.height(10.dp))
            OutlinedButton(
                onClick = {
                    viewModel.signInDemo {
                        if (consentAccepted) viewModel.recordOnboardingConsents()
                        onLoggedIn()
                    }
                },
                enabled = !state.isLoading && consentAccepted,
                modifier = Modifier.fillMaxWidth()
            ) {
                Text("Trải nghiệm nhanh tài khoản Nông hộ [DEMO]")
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FarmerHomeScreen(
    state: CocoUiState,
    onOpenFarms: () -> Unit,
    onOpenCreateFarm: () -> Unit,
    onOpenCalculator: () -> Unit,
    onOpenAiResults: () -> Unit,
    onOpenProfile: () -> Unit
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Trang chủ Nông hộ", color = Color.White) },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen)
            )
        }
    ) { padding ->
        Column(
            modifier = Modifier
                .padding(padding)
                .fillMaxSize()
                .verticalScroll(rememberScrollState())
                .padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp)
        ) {
            Card(colors = CardDefaults.cardColors(containerColor = MintSurface), modifier = Modifier.fillMaxWidth()) {
                Column(Modifier.padding(16.dp)) {
                    Text(state.profile?.displayName ?: "Nông hộ CocoCarbon", fontWeight = FontWeight.Bold, color = ForestGreen)
                    Text("Số vườn hiện có: ${state.farms.size} • Hồ sơ AI: ${state.analyses.size}", style = MaterialTheme.typography.bodySmall)
                }
            }

            Button(onClick = onOpenCreateFarm, modifier = Modifier.fillMaxWidth(), colors = ButtonDefaults.buttonColors(containerColor = EmeraldGreen)) {
                Text("+ Khai báo Vườn dừa & GPS")
            }
            OutlinedButton(onClick = onOpenFarms, modifier = Modifier.fillMaxWidth()) {
                Text("Danh sách Vườn & Upload ảnh/PDF (${state.farms.size})")
            }
            OutlinedButton(onClick = onOpenCalculator, modifier = Modifier.fillMaxWidth()) {
                Text("Coconut Carbon Calculator (API C#)")
            }
            OutlinedButton(onClick = onOpenAiResults, modifier = Modifier.fillMaxWidth()) {
                Text("Kết quả AI Auditor & Trạng thái tài liệu")
            }
            OutlinedButton(onClick = onOpenProfile, modifier = Modifier.fillMaxWidth()) {
                Text("Hồ sơ cá nhân & Đăng xuất")
            }

            Text(
                "Lưu ý khoa học: Kết quả calculator là ước tính tham khảo theo nghiên cứu CTU (2015) tại Giồng Trôm; không phải tín chỉ đã phát hành hay lượng hấp thụ hằng năm.",
                style = MaterialTheme.typography.bodySmall,
                color = Color.DarkGray
            )
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FarmListScreen(
    state: CocoUiState,
    onBack: () -> Unit,
    onSelectFarm: (FarmDto) -> Unit,
    onCreateNew: () -> Unit
) {
    Scaffold(
        topBar = {
            TopAppBar(
                title = { Text("Danh sách Vườn dừa", color = Color.White) },
                colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen),
                actions = {
                    Button(onClick = onCreateNew) { Text("Mới") }
                }
            )
        }
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize().padding(16.dp)) {
            OutlinedButton(onClick = onBack) { Text("← Quay lại") }
            Spacer(Modifier.height(8.dp))
            LazyColumn(verticalArrangement = Arrangement.spacedBy(10.dp)) {
                items(state.farms) { farm ->
                    Card(onClick = { onSelectFarm(farm) }, modifier = Modifier.fillMaxWidth()) {
                        Column(Modifier.padding(14.dp)) {
                            Text(farm.name, fontWeight = FontWeight.Bold, color = ForestGreen)
                            Text("${farm.commune}, ${farm.province} • ${farm.areaHa} ha • ${farm.treeCount} cây", style = MaterialTheme.typography.bodySmall)
                            Text("Giống: ${farm.variety} • Tuổi: ${farm.ageYears} năm • [${farm.dataOriginTag}]", style = MaterialTheme.typography.bodySmall, color = EmeraldGreen)
                        }
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CreateFarmScreen(
    state: CocoUiState,
    viewModel: MainViewModel,
    onBack: () -> Unit
) {
    val context = LocalContext.current
    var name by remember { mutableStateOf("") }
    var commune by remember { mutableStateOf("Xã Lương Quới (Giồng Trôm)") }
    var areaHa by remember { mutableStateOf("1.0") }
    var treeCount by remember { mutableStateOf("200") }
    var variety by remember { mutableStateOf("TallDuaTa") }
    var ageYears by remember { mutableStateOf("10") }
    var latText by remember { mutableStateOf("10.1652") }
    var lngText by remember { mutableStateOf("106.4621") }
    var locConsent by remember { mutableStateOf(true) }
    var photoConsent by remember { mutableStateOf(true) }

    val gpsPermissionLauncher = rememberLauncherForActivityResult(
        ActivityResultContracts.RequestMultiplePermissions()
    ) { perms ->
        if (perms[Manifest.permission.ACCESS_FINE_LOCATION] == true ||
            perms[Manifest.permission.ACCESS_COARSE_LOCATION] == true
        ) {
            fetchLastKnownLocation(context)?.let { (la, ln) ->
                latText = la.toString()
                lngText = ln.toString()
            }
        }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text("Tạo Vườn dừa mới", color = Color.White) }, colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen)) }
    ) { padding ->
        Column(
            Modifier.padding(padding).fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp),
            verticalArrangement = Arrangement.spacedBy(10.dp)
        ) {
            OutlinedButton(onClick = onBack) { Text("← Hủy / Quay lại") }
            OutlinedTextField(value = name, onValueChange = { name = it }, label = { Text("Tên vườn dừa") }, modifier = Modifier.fillMaxWidth())
            OutlinedTextField(value = commune, onValueChange = { commune = it }, label = { Text("Xã/Khu vực (Vĩnh Long)") }, modifier = Modifier.fillMaxWidth())
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(value = areaHa, onValueChange = { areaHa = it }, label = { Text("Diện tích (ha)") }, modifier = Modifier.weight(1f))
                OutlinedTextField(value = treeCount, onValueChange = { treeCount = it }, label = { Text("Số cây") }, modifier = Modifier.weight(1f))
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedButton(
                    onClick = { variety = if (variety == "TallDuaTa") "ShortXiemXanh" else "TallDuaTa" },
                    modifier = Modifier.weight(1f)
                ) { Text("Giống: $variety") }
                OutlinedTextField(value = ageYears, onValueChange = { ageYears = it }, label = { Text("Tuổi cây") }, modifier = Modifier.weight(1f))
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(value = latText, onValueChange = { latText = it }, label = { Text("Lat") }, modifier = Modifier.weight(1f))
                OutlinedTextField(value = lngText, onValueChange = { lngText = it }, label = { Text("Lng") }, modifier = Modifier.weight(1f))
            }
            OutlinedButton(onClick = {
                gpsPermissionLauncher.launch(arrayOf(Manifest.permission.ACCESS_FINE_LOCATION, Manifest.permission.ACCESS_COARSE_LOCATION))
            }) { Text("Lấy tọa độ GPS hiện tại") }

            Row(verticalAlignment = Alignment.CenterVertically) {
                Checkbox(checked = locConsent, onCheckedChange = { locConsent = it })
                Text("Đồng thuận lưu vị trí GPS", style = MaterialTheme.typography.bodySmall)
            }
            Row(verticalAlignment = Alignment.CenterVertically) {
                Checkbox(checked = photoConsent, onCheckedChange = { photoConsent = it })
                Text("Đồng thuận lưu ảnh & tài liệu", style = MaterialTheme.typography.bodySmall)
            }

            state.error?.let { Text(it, color = Color.Red, style = MaterialTheme.typography.bodySmall) }

            Button(
                onClick = {
                    viewModel.createFarm(
                        name = name.ifBlank { "Vườn dừa hiện trường" },
                        commune = commune,
                        areaHa = areaHa.toDoubleOrNull() ?: 1.0,
                        treeCount = treeCount.toIntOrNull() ?: 200,
                        variety = variety,
                        ageYears = ageYears.toIntOrNull() ?: 10,
                        lat = latText.toDoubleOrNull(),
                        lng = lngText.toDoubleOrNull(),
                        locationConsent = locConsent,
                        photoConsent = photoConsent,
                        onCreated = onBack
                    )
                },
                enabled = !state.isLoading,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = ForestGreen)
            ) { Text("Lưu hồ sơ vườn") }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun FarmDetailAndUploadScreen(
    farm: FarmDto,
    state: CocoUiState,
    viewModel: MainViewModel,
    onBack: () -> Unit,
    onOpenAiResult: () -> Unit
) {
    val context = LocalContext.current
    var tempPhotoUri by remember { mutableStateOf<Uri?>(null) }

    LaunchedEffect(farm.id) { viewModel.loadFarmFiles(farm.id) }

    val galleryLauncher = rememberLauncherForActivityResult(ActivityResultContracts.GetContent()) { uri ->
        if (uri != null) {
            val bytes = context.contentResolver.openInputStream(uri)?.readBytes() ?: return@rememberLauncherForActivityResult
            val mime = context.contentResolver.getType(uri) ?: "image/jpeg"
            val ext = if (mime == "application/pdf") "pdf" else if (mime == "image/png") "png" else "jpg"
            viewModel.uploadAndOptionallyAnalyze(farm.id, "upload_${System.currentTimeMillis()}.$ext", mime, bytes, "LandDocument", true)
        }
    }

    val cameraLauncher = rememberLauncherForActivityResult(ActivityResultContracts.TakePicture()) { ok ->
        val uri = tempPhotoUri
        if (ok && uri != null) {
            val bytes = context.contentResolver.openInputStream(uri)?.readBytes() ?: return@rememberLauncherForActivityResult
            viewModel.uploadAndOptionallyAnalyze(farm.id, "camera_${System.currentTimeMillis()}.jpg", "image/jpeg", bytes, "FarmPhoto", true)
        }
    }

    val cameraPermLauncher = rememberLauncherForActivityResult(ActivityResultContracts.RequestPermission()) { granted ->
        if (granted) {
            val file = File(context.cacheDir, "photo_${System.currentTimeMillis()}.jpg")
            val uri = FileProvider.getUriForFile(context, "${context.packageName}.fileprovider", file)
            tempPhotoUri = uri
            cameraLauncher.launch(uri)
        }
    }

    Scaffold(
        topBar = { TopAppBar(title = { Text(farm.name, color = Color.White) }, colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen)) }
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            OutlinedButton(onClick = onBack) { Text("← Danh sách vườn") }
            Card(Modifier.fillMaxWidth()) {
                Column(Modifier.padding(14.dp)) {
                    Text("${farm.commune}, ${farm.province} (${farm.historicalRegion})")
                    Text("Diện tích: ${farm.areaHa} ha • Số cây: ${farm.treeCount} • Giống: ${farm.variety} (${farm.ageYears} năm)")
                    Text("Trạng thái: ${farm.status} • Nhãn: ${farm.dataOriginTag}")
                }
            }

            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                Button(onClick = { cameraPermLauncher.launch(Manifest.permission.CAMERA) }, modifier = Modifier.weight(1f), colors = ButtonDefaults.buttonColors(containerColor = EmeraldGreen)) {
                    Text("Chụp ảnh & AI")
                }
                OutlinedButton(onClick = { galleryLauncher.launch("*/*") }, modifier = Modifier.weight(1f)) {
                    Text("Chọn PDF/Ảnh")
                }
            }

            state.infoMessage?.let { Text(it, color = EmeraldGreen, style = MaterialTheme.typography.bodySmall) }
            state.error?.let { Text(it, color = Color.Red, style = MaterialTheme.typography.bodySmall) }

            if (state.latestAiResult != null) {
                Button(onClick = onOpenAiResult, modifier = Modifier.fillMaxWidth(), colors = ButtonDefaults.buttonColors(containerColor = ForestGreen)) {
                    Text("Xem báo cáo AI Auditor mới nhất")
                }
            }

            Text("Tệp đã tải lên (${state.selectedFarmFiles.size})", fontWeight = FontWeight.Bold)
            state.selectedFarmFiles.forEach { f ->
                Card(Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(10.dp)) {
                        Text("${f.fileName} (v${f.version})", fontWeight = FontWeight.SemiBold)
                        Text("${f.mimeType} • SHA256: ${f.sha256Hash.take(12)}...", style = MaterialTheme.typography.bodySmall)
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun CalculatorScreen(state: CocoUiState, viewModel: MainViewModel, onBack: () -> Unit) {
    var areaHa by remember { mutableStateOf("1.0") }
    var treeCount by remember { mutableStateOf("200") }
    var variety by remember { mutableStateOf("TallDuaTa") }
    var ageYears by remember { mutableStateOf("10") }

    Scaffold(
        topBar = { TopAppBar(title = { Text("Carbon Calculator (C# API)", color = Color.White) }, colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen)) }
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            OutlinedButton(onClick = onBack) { Text("← Trang chủ") }
            Text("Gọi trực tiếp API C# backend với 4 hệ số CTU 2015 (không tính lại trên Android).", style = MaterialTheme.typography.bodySmall)
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedTextField(value = areaHa, onValueChange = { areaHa = it }, label = { Text("Diện tích (ha)") }, modifier = Modifier.weight(1f))
                OutlinedTextField(value = treeCount, onValueChange = { treeCount = it }, label = { Text("Số cây") }, modifier = Modifier.weight(1f))
            }
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedButton(onClick = { variety = if (variety == "TallDuaTa") "ShortXiemXanh" else "TallDuaTa" }, modifier = Modifier.weight(1f)) {
                    Text("Giống: $variety")
                }
                OutlinedTextField(value = ageYears, onValueChange = { ageYears = it }, label = { Text("Tuổi (4/10)") }, modifier = Modifier.weight(1f))
            }
            Button(
                onClick = {
                    viewModel.runCalculator(
                        farmId = null,
                        areaHa = areaHa.toDoubleOrNull() ?: 0.0,
                        treeCount = treeCount.toIntOrNull() ?: 0,
                        variety = variety,
                        ageYears = ageYears.toIntOrNull() ?: 0
                    )
                },
                enabled = !state.isLoading,
                modifier = Modifier.fillMaxWidth(),
                colors = ButtonDefaults.buttonColors(containerColor = ForestGreen)
            ) { Text("Tính ước tính tham khảo") }

            state.error?.let { Text(it, color = Color.Red, style = MaterialTheme.typography.bodySmall) }

            state.estimateResult?.let { res ->
                Card(colors = CardDefaults.cardColors(containerColor = MintSurface), modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(14.dp), verticalArrangement = Arrangement.spacedBy(6.dp)) {
                        Text(res.resultLabel, fontWeight = FontWeight.Bold, color = ForestGreen)
                        Text("${res.estimatedCO2EquivalentT} tCO2 tương đương tham khảo", style = MaterialTheme.typography.headlineSmall, fontWeight = FontWeight.ExtraBold, color = EmeraldGreen)
                        Text("Hệ số: ${res.factorUsed.code} (${res.factorUsed.valueTCO2PerHa} tCO2/ha)")
                        Text("Mật độ quan sát: ${res.observedTreeDensityPerHa} cây/ha • ${res.densityContextNote}", style = MaterialTheme.typography.bodySmall)
                        Text("Nguồn: ${res.sourceCitation}", style = MaterialTheme.typography.bodySmall)
                        Text("Tuyên bố bắt buộc: ${res.disclaimer}", style = MaterialTheme.typography.bodySmall, color = Color.DarkGray)
                    }
                }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun AiResultDocumentScreen(state: CocoUiState, onBack: () -> Unit) {
    val latest = state.latestAiResult ?: state.analyses.firstOrNull()
    Scaffold(
        topBar = { TopAppBar(title = { Text("Kết quả AI Auditor", color = Color.White) }, colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen)) }
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize().verticalScroll(rememberScrollState()).padding(16.dp), verticalArrangement = Arrangement.spacedBy(10.dp)) {
            OutlinedButton(onClick = onBack) { Text("← Quay lại") }
            if (latest == null) {
                Text("Chưa có báo cáo phân tích AI nào. Vui lòng chọn một vườn và upload tài liệu/ảnh.")
            } else {
                Card(colors = CardDefaults.cardColors(containerColor = MintSurface), modifier = Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(14.dp)) {
                        Text("Trạng thái: ${latest.status} • Review: ${latest.reviewStatus}", fontWeight = FontWeight.Bold, color = ForestGreen)
                        Text("Model: ${latest.modelName}", style = MaterialTheme.typography.bodySmall)
                        latest.readinessRubric?.let {
                            Text("${it.label}: ${it.score}/100", fontWeight = FontWeight.Bold, color = EmeraldGreen)
                            Text(it.disclaimer, style = MaterialTheme.typography.bodySmall)
                        }
                    }
                }
                Text("Cảnh báo & Phát hiện:", fontWeight = FontWeight.Bold)
                latest.findings?.forEach { Text("• [${it.code}] ${it.message}", style = MaterialTheme.typography.bodySmall) }
                Text("Bằng chứng trích dẫn:", fontWeight = FontWeight.Bold)
                latest.evidence?.forEach { Text("• Trang ${it.pageNumber ?: "?"}: ${it.snippet}", style = MaterialTheme.typography.bodySmall) }
            }
        }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
fun ProfileScreen(state: CocoUiState, viewModel: MainViewModel, onBack: () -> Unit, onSignedOut: () -> Unit) {
    Scaffold(
        topBar = { TopAppBar(title = { Text("Hồ sơ & Đăng xuất", color = Color.White) }, colors = TopAppBarDefaults.topAppBarColors(containerColor = ForestGreen)) }
    ) { padding ->
        Column(Modifier.padding(padding).fillMaxSize().padding(16.dp), verticalArrangement = Arrangement.spacedBy(12.dp)) {
            OutlinedButton(onClick = onBack) { Text("← Quay lại") }
            Card(Modifier.fillMaxWidth()) {
                Column(Modifier.padding(14.dp)) {
                    Text(state.profile?.displayName ?: "Người dùng", fontWeight = FontWeight.Bold)
                    Text(state.profile?.email ?: "", style = MaterialTheme.typography.bodySmall)
                    Text("Vai trò: ${state.profile?.roles?.joinToString { "${it.roleCode}(${it.approvalStatus})" } ?: "N/A"}", style = MaterialTheme.typography.bodySmall)
                }
            }
            Button(
                onClick = { viewModel.signOut(onSignedOut) },
                colors = ButtonDefaults.buttonColors(containerColor = Color(0xFFB91C1C)),
                modifier = Modifier.fillMaxWidth()
            ) { Text("Đăng xuất") }
        }
    }
}

@SuppressLint("MissingPermission")
private fun fetchLastKnownLocation(context: Context): Pair<Double, Double>? {
    return try {
        val lm = context.getSystemService(Context.LOCATION_SERVICE) as LocationManager
        val loc = lm.getLastKnownLocation(LocationManager.GPS_PROVIDER)
            ?: lm.getLastKnownLocation(LocationManager.NETWORK_PROVIDER)
        if (loc != null) loc.latitude to loc.longitude else null
    } catch (_: Exception) {
        null
    }
}
