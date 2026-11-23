package com.cococarbon.app.data

import android.content.Context
import androidx.credentials.CredentialManager
import androidx.credentials.CustomCredential
import androidx.credentials.GetCredentialRequest
import com.cococarbon.app.BuildConfig
import com.google.android.libraries.identity.googleid.GetGoogleIdOption
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential
import com.google.firebase.FirebaseApp
import com.google.firebase.auth.FirebaseAuth
import com.google.firebase.auth.GoogleAuthProvider
import kotlinx.coroutines.tasks.await
import okhttp3.Interceptor
import okhttp3.OkHttpClient
import okhttp3.Response
import okhttp3.logging.HttpLoggingInterceptor
import retrofit2.Retrofit
import retrofit2.converter.gson.GsonConverterFactory
import java.util.concurrent.TimeUnit

class TokenStore(context: Context) {
    private val prefs = context.getSharedPreferences("cococarbon_auth", Context.MODE_PRIVATE)

    var cachedToken: String?
        get() = prefs.getString("id_token", null)
        set(value) = prefs.edit().putString("id_token", value).apply()

    var authMode: String
        get() = prefs.getString("auth_mode", "none") ?: "none"
        set(value) = prefs.edit().putString("auth_mode", value).apply()

    fun clear() {
        prefs.edit().clear().apply()
        try {
            if (FirebaseApp.getApps(appContext).isNotEmpty()) {
                FirebaseAuth.getInstance().signOut()
            }
        } catch (_: Exception) {
        }
    }

    private val appContext = context.applicationContext
}

class AuthInterceptor(private val tokenStore: TokenStore) : Interceptor {
    override fun intercept(chain: Interceptor.Chain): Response {
        val original = chain.request()
        val token = tokenStore.cachedToken
        val req = if (!token.isNullOrBlank()) {
            original.newBuilder()
                .header("Authorization", "Bearer $token")
                .build()
        } else {
            original
        }
        return chain.proceed(req)
    }
}

object ApiClientFactory {
    fun create(tokenStore: TokenStore): CocoCarbonApiService {
        val logging = HttpLoggingInterceptor().apply {
            level = HttpLoggingInterceptor.Level.BASIC
        }
        val client = OkHttpClient.Builder()
            .addInterceptor(AuthInterceptor(tokenStore))
            .addInterceptor(logging)
            .connectTimeout(15, TimeUnit.SECONDS)
            .readTimeout(35, TimeUnit.SECONDS)
            .build()

        return Retrofit.Builder()
            .baseUrl(BuildConfig.API_BASE_URL)
            .client(client)
            .addConverterFactory(GsonConverterFactory.create())
            .build()
            .create(CocoCarbonApiService::class.java)
    }
}

object FirebaseGoogleAuthHelper {
    fun isFirebaseConfigured(context: Context): Boolean {
        return try {
            FirebaseApp.getApps(context).isNotEmpty() && BuildConfig.FIREBASE_WEB_CLIENT_ID.isNotBlank()
        } catch (_: Exception) {
            false
        }
    }

    suspend fun signInWithGoogle(context: Context): String {
        val webClientId = BuildConfig.FIREBASE_WEB_CLIENT_ID
        require(webClientId.isNotBlank()) {
            "Chưa cấu hình FIREBASE_WEB_CLIENT_ID và google-services.json."
        }
        val credentialManager = CredentialManager.create(context)
        val googleIdOption = GetGoogleIdOption.Builder()
            .setFilterByAuthorizedAccounts(false)
            .setServerClientId(webClientId)
            .build()

        val request = GetCredentialRequest.Builder()
            .addCredentialOption(googleIdOption)
            .build()

        val result = credentialManager.getCredential(context, request)
        val credential = result.credential
        if (credential is CustomCredential &&
            credential.type == GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL
        ) {
            val googleCred = GoogleIdTokenCredential.createFrom(credential.data)
            val firebaseCred = GoogleAuthProvider.getCredential(googleCred.idToken, null)
            val authResult = FirebaseAuth.getInstance().signInWithCredential(firebaseCred).await()
            val tokenResult = authResult.user?.getIdToken(false)?.await()
            return tokenResult?.token ?: error("Không lấy được Firebase ID Token.")
        }
        error("Credential không phải Google ID Token.")
    }
}
