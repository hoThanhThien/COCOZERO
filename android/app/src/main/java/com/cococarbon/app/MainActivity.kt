package com.cococarbon.app

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.runtime.collectAsState
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.lifecycle.viewmodel.compose.viewModel
import androidx.navigation.compose.NavHost
import androidx.navigation.compose.composable
import androidx.navigation.compose.rememberNavController
import com.cococarbon.app.data.FarmDto
import com.cococarbon.app.ui.AiResultDocumentScreen
import com.cococarbon.app.ui.CalculatorScreen
import com.cococarbon.app.ui.CreateFarmScreen
import com.cococarbon.app.ui.FarmDetailAndUploadScreen
import com.cococarbon.app.ui.FarmListScreen
import com.cococarbon.app.ui.FarmerHomeScreen
import com.cococarbon.app.ui.LoginOnboardingScreen
import com.cococarbon.app.ui.MainViewModel
import com.cococarbon.app.ui.ProfileScreen
import com.cococarbon.app.ui.SplashScreen
import com.cococarbon.app.ui.theme.CocoCarbonTheme

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent {
            CocoCarbonTheme {
                val nav = rememberNavController()
                val vm: MainViewModel = viewModel()
                val state by vm.state.collectAsState()
                var selectedFarm by remember { mutableStateOf<FarmDto?>(null) }

                NavHost(navController = nav, startDestination = "splash") {
                    composable("splash") {
                        SplashScreen(
                            viewModel = vm,
                            onNavigateHome = { nav.navigate("home") { popUpTo("splash") { inclusive = true } } },
                            onNavigateLogin = { nav.navigate("login") { popUpTo("splash") { inclusive = true } } }
                        )
                    }
                    composable("login") {
                        LoginOnboardingScreen(
                            state = state,
                            viewModel = vm,
                            onLoggedIn = { nav.navigate("home") { popUpTo("login") { inclusive = true } } }
                        )
                    }
                    composable("home") {
                        FarmerHomeScreen(
                            state = state,
                            onOpenFarms = { nav.navigate("farms") },
                            onOpenCreateFarm = { nav.navigate("create_farm") },
                            onOpenCalculator = { nav.navigate("calculator") },
                            onOpenAiResults = { nav.navigate("ai_results") },
                            onOpenProfile = { nav.navigate("profile") }
                        )
                    }
                    composable("farms") {
                        FarmListScreen(
                            state = state,
                            onBack = { nav.popBackStack() },
                            onSelectFarm = { farm ->
                                selectedFarm = farm
                                nav.navigate("farm_detail")
                            },
                            onCreateNew = { nav.navigate("create_farm") }
                        )
                    }
                    composable("create_farm") {
                        CreateFarmScreen(state = state, viewModel = vm, onBack = { nav.popBackStack() })
                    }
                    composable("farm_detail") {
                        val farm = selectedFarm ?: state.farms.firstOrNull()
                        if (farm != null) {
                            FarmDetailAndUploadScreen(
                                farm = farm,
                                state = state,
                                viewModel = vm,
                                onBack = { nav.popBackStack() },
                                onOpenAiResult = { nav.navigate("ai_results") }
                            )
                        }
                    }
                    composable("calculator") {
                        CalculatorScreen(state = state, viewModel = vm, onBack = { nav.popBackStack() })
                    }
                    composable("ai_results") {
                        AiResultDocumentScreen(state = state, onBack = { nav.popBackStack() })
                    }
                    composable("profile") {
                        ProfileScreen(
                            state = state,
                            viewModel = vm,
                            onBack = { nav.popBackStack() },
                            onSignedOut = { nav.navigate("login") { popUpTo(0) { inclusive = true } } }
                        )
                    }
                }
            }
        }
    }
}
