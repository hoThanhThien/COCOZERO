using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CocoCarbon.Infrastructure.Auth;

public sealed record VerifiedFirebaseIdentity(
    string FirebaseUid,
    string Email,
    string DisplayName,
    bool EmailVerified,
    bool IsDevDemoToken);

public sealed class TokenVerificationException(string message, string code = "INVALID_TOKEN") : Exception(message)
{
    public string ErrorCode { get; } = code;
}

public interface IFirebaseTokenVerifier
{
    Task<VerifiedFirebaseIdentity> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}

public sealed class FirebaseAdminTokenVerifier : IFirebaseTokenVerifier
{
    private static readonly object AppInitLock = new();
    private readonly IConfiguration _config;
    private readonly ILogger<FirebaseAdminTokenVerifier> _logger;
    private readonly bool _allowDevDemoTokens;

    private static readonly Dictionary<string, VerifiedFirebaseIdentity> DemoIdentities = new(StringComparer.Ordinal)
    {
        ["dev-demo:farmer"] = new("demo-farmer-uid", "farmer.demo@cococarbon.local", "Nguyễn Văn An [DEMO Nông hộ]", true, true),
        ["dev-demo:coop"] = new("demo-coop-uid", "coop.demo@cococarbon.local", "Trần Thị Bình [DEMO Quản lý HTX]", true, true),
        ["dev-demo:developer"] = new("demo-dev-uid", "developer.demo@cococarbon.local", "Mekong AgriCarbon Dev [DEMO]", true, true),
        ["dev-demo:buyer"] = new("demo-buyer-uid", "buyer.demo@cococarbon.local", "Saigon Green ESG Buyer [DEMO]", true, true),
        ["dev-demo:reviewer"] = new("demo-reviewer-uid", "reviewer.demo@cococarbon.local", "Lê Minh Khoa [DEMO Reviewer]", true, true),
        ["dev-demo:admin"] = new("demo-admin-uid", "admin.demo@cococarbon.local", "Quản trị CocoCarbon [DEMO Admin]", true, true),
    };

    public FirebaseAdminTokenVerifier(
        IConfiguration config,
        ILogger<FirebaseAdminTokenVerifier> logger)
    {
        _config = config;
        _logger = logger;

        var envName = _config["ASPNETCORE_ENVIRONMENT"] ?? _config["DOTNET_ENVIRONMENT"] ?? "Production";
        var flag = _config["FIREBASE_AUTH_DEV_ALLOW_MOCK"];
        _allowDevDemoTokens =
            string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase) &&
            string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);

        EnsureFirebaseInitialized();
    }

    public async Task<VerifiedFirebaseIdentity> VerifyIdTokenAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            throw new TokenVerificationException("Thiếu Firebase ID token.", "MISSING_TOKEN");
        }

        if (idToken.StartsWith("dev-demo:", StringComparison.Ordinal))
        {
            if (!_allowDevDemoTokens)
            {
                throw new TokenVerificationException(
                    "Chế độ token demo bị tắt trong môi trường hiện tại.",
                    "DEV_TOKEN_DISABLED");
            }

            if (DemoIdentities.TryGetValue(idToken, out var demo))
            {
                return demo;
            }

            throw new TokenVerificationException("Dev demo token không hợp lệ.", "INVALID_DEV_TOKEN");
        }

        if (FirebaseApp.DefaultInstance is null)
        {
            throw new TokenVerificationException(
                "Firebase Admin SDK chưa được cấu hình trên máy chủ (thiếu FIREBASE_SERVICE_ACCOUNT_PATH hoặc FIREBASE_PROJECT_ID).",
                "FIREBASE_NOT_CONFIGURED");
        }

        try
        {
            var decoded = await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken, cancellationToken);
            var uid = decoded.Uid;
            var email = decoded.Claims.TryGetValue("email", out var emailObj) ? emailObj?.ToString() ?? "" : "";
            var name = decoded.Claims.TryGetValue("name", out var nameObj) ? nameObj?.ToString() ?? email : email;
            var emailVerified = decoded.Claims.TryGetValue("email_verified", out var evObj) &&
                                bool.TryParse(evObj?.ToString(), out var ev) && ev;

            return new VerifiedFirebaseIdentity(
                FirebaseUid: uid,
                Email: string.IsNullOrWhiteSpace(email) ? $"{uid}@firebase.user" : email,
                DisplayName: string.IsNullOrWhiteSpace(name) ? uid : name,
                EmailVerified: emailVerified,
                IsDevDemoToken: false);
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.ExpiredIdToken)
        {
            throw new TokenVerificationException("Firebase ID token đã hết hạn.", "EXPIRED_TOKEN");
        }
        catch (FirebaseAuthException ex) when (ex.AuthErrorCode == AuthErrorCode.RevokedIdToken)
        {
            throw new TokenVerificationException("Firebase ID token đã bị thu hồi.", "REVOKED_TOKEN");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Firebase ID token verification failed.");
            throw new TokenVerificationException("Firebase ID token không hợp lệ.", "INVALID_TOKEN");
        }
    }

    private void EnsureFirebaseInitialized()
    {
        if (FirebaseApp.DefaultInstance is not null) return;

        lock (AppInitLock)
        {
            if (FirebaseApp.DefaultInstance is not null) return;

            var saPath = _config["FIREBASE_SERVICE_ACCOUNT_PATH"];
            var saJson = _config["FIREBASE_SERVICE_ACCOUNT_JSON"];
            var projectId = _config["FIREBASE_PROJECT_ID"];

            try
            {
                if (!string.IsNullOrWhiteSpace(saPath) && File.Exists(saPath))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromFile(saPath),
                        ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId
                    });
                    _logger.LogInformation("Initialized FirebaseApp using service account file.");
                }
                else if (!string.IsNullOrWhiteSpace(saJson))
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        Credential = GoogleCredential.FromJson(saJson),
                        ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId
                    });
                    _logger.LogInformation("Initialized FirebaseApp using inline JSON secret.");
                }
                else if (!string.IsNullOrWhiteSpace(projectId) && !_allowDevDemoTokens)
                {
                    FirebaseApp.Create(new AppOptions
                    {
                        ProjectId = projectId
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FirebaseApp could not be initialized at startup; real Firebase token verification unavailable until configured.");
            }
        }
    }
}
