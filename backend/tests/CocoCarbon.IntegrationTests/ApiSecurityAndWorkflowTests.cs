using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.AiClient;
using CocoCarbon.Infrastructure.Auth;
using CocoCarbon.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace CocoCarbon.IntegrationTests;

public sealed class StubFirebaseTokenVerifier : IFirebaseTokenVerifier
{
    public Task<VerifiedFirebaseIdentity> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default)
    {
        return idToken switch
        {
            "valid-farmer-a" => Task.FromResult(new VerifiedFirebaseIdentity("uid-farmer-a", "farmer.a@test.local", "Farmer A", true, false)),
            "valid-farmer-b" => Task.FromResult(new VerifiedFirebaseIdentity("uid-farmer-b", "farmer.b@test.local", "Farmer B", true, false)),
            "valid-admin" => Task.FromResult(new VerifiedFirebaseIdentity("demo-admin-uid", "admin.demo@cococarbon.local", "Admin", true, false)),
            "valid-coop" => Task.FromResult(new VerifiedFirebaseIdentity("demo-coop-uid", "coop.demo@cococarbon.local", "CoopMgr", true, false)),
            "valid-reviewer" => Task.FromResult(new VerifiedFirebaseIdentity("demo-reviewer-uid", "reviewer.demo@cococarbon.local", "Reviewer", true, false)),
            "valid-developer" => Task.FromResult(new VerifiedFirebaseIdentity("demo-dev-uid", "developer.demo@cococarbon.local", "Developer", true, false)),
            "valid-buyer" => Task.FromResult(new VerifiedFirebaseIdentity("demo-buyer-uid", "buyer.demo@cococarbon.local", "Buyer", true, false)),
            "expired-token" => throw new TokenVerificationException("Expired token", "EXPIRED_TOKEN"),
            _ => throw new TokenVerificationException("Invalid token", "INVALID_TOKEN")
        };
    }
}

public sealed class StubPythonAiServiceClient : IPythonAiServiceClient
{
    public bool SimulateOutage { get; set; }

    public Task<AiDocumentAnalysisResponse> AnalyzeDocumentAsync(AiDocumentAnalysisRequest request, CancellationToken cancellationToken = default)
    {
        if (SimulateOutage)
        {
            return Task.FromResult(new AiDocumentAnalysisResponse(
                AnalysisId: Guid.NewGuid().ToString(),
                FileId: request.FileId.ToString(),
                Status: "Fallback_AiUnavailable",
                IsFallback: true,
                ExtractedFields: [],
                Findings: [new AiFindingDto("AI_SERVICE_UNAVAILABLE", "warning", "system_fallback", "Simulated AI outage", null)],
                MissingFields: ["Manual review required"],
                Duplicates: [],
                Evidence: [],
                ConfidenceNotes: ["Chưa xác minh độc lập"],
                ReadinessRubric: null,
                ReviewerRequired: true,
                Provider: "fallback",
                ModelName: "none",
                PromptVersion: "fallback-v1",
                CreatedAt: DateTimeOffset.UtcNow));
        }

        return Task.FromResult(new AiDocumentAnalysisResponse(
            AnalysisId: Guid.NewGuid().ToString(),
            FileId: request.FileId.ToString(),
            Status: "Completed",
            IsFallback: false,
            ExtractedFields: [new AiExtractedFieldDto("areaHa", "1.0", 0.92, "AI_EXTRACTED")],
            Findings: [new AiFindingDto("INFO_MATCH", "info", "consistency", "Diện tích khớp hồ sơ khai báo.", "page:1")],
            MissingFields: ["monitoring_plan"],
            Duplicates: [],
            Evidence: [new AiEvidenceDto(request.FileId.ToString(), 1, "Diện tích: 1.0 ha", "1.0", 0.92, "Trích xuất trang 1")],
            ConfidenceNotes: ["Chưa xác minh độc lập; cần reviewer duyệt."],
            ReadinessRubric: new AiReadinessRubricDto("Mức độ đầy đủ hồ sơ / Project Readiness", 75.0, "Không phải chứng nhận tín chỉ.", []),
            ReviewerRequired: true,
            Provider: "gemini",
            ModelName: "gemini-4-hydrogen",
            PromptVersion: "coco-auditor-v1.0",
            CreatedAt: DateTimeOffset.UtcNow));
    }

    public Task<AiAssistantResponse> QueryAssistantAsync(AiAssistantRequest request, CancellationToken cancellationToken = default) =>
        Task.FromResult(new AiAssistantResponse("OK", true, [], "Disclaimer", "gemini-4-hydrogen", false));
}

public sealed class CocoCarbonApiFactory : WebApplicationFactory<Program>, IDisposable
{
    private readonly string _baseTemp = Path.Combine(AppContext.BaseDirectory, $"test_run_{Guid.NewGuid():N}");
    private readonly string _dbPath;
    private readonly string _storagePath;
    public StubPythonAiServiceClient StubAi { get; } = new();

    public CocoCarbonApiFactory()
    {
        Directory.CreateDirectory(_baseTemp);
        _dbPath = Path.Combine(_baseTemp, "test.db");
        _storagePath = Path.Combine(_baseTemp, "storage");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("DB_PROVIDER", "sqlite");
        builder.UseSetting("ConnectionStrings:DefaultConnection", $"Data Source={_dbPath}");
        builder.UseSetting("LOCAL_STORAGE_ROOT", _storagePath);
        builder.UseSetting("FIREBASE_AUTH_DEV_ALLOW_MOCK", "false");
        builder.UseSetting("SEED_DEMO_DATA", "true");
        builder.UseSetting("MAX_UPLOAD_BYTES", "524288");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IFirebaseTokenVerifier>();
            services.AddSingleton<IFirebaseTokenVerifier, StubFirebaseTokenVerifier>();
            services.RemoveAll<IPythonAiServiceClient>();
            services.AddSingleton<IPythonAiServiceClient>(StubAi);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            try { if (Directory.Exists(_baseTemp)) Directory.Delete(_baseTemp, true); } catch { }
        }
    }
}

public class ApiSecurityAndWorkflowTests : IClassFixture<CocoCarbonApiFactory>
{
    private readonly CocoCarbonApiFactory _factory;

    public ApiSecurityAndWorkflowTests(CocoCarbonApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient ClientWithToken(string? token = null)
    {
        var client = _factory.CreateClient();
        if (token is not null)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task Auth_MissingExpiredInvalidTokens_Return401()
    {
        var anon = ClientWithToken();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anon.GetAsync("/api/v1/me")).StatusCode);

        var invalid = ClientWithToken("not-a-valid-token");
        Assert.Equal(HttpStatusCode.Unauthorized, (await invalid.GetAsync("/api/v1/me")).StatusCode);

        var expired = ClientWithToken("expired-token");
        var expiredResp = await expired.GetAsync("/api/v1/me");
        Assert.Equal(HttpStatusCode.Unauthorized, expiredResp.StatusCode);
        var body = await expiredResp.Content.ReadAsStringAsync();
        Assert.Contains("EXPIRED_TOKEN", body);
    }

    [Fact]
    public async Task Auth_UnapprovedAdminRequest_CannotAccessAdminEndpoints()
    {
        var farmerClient = ClientWithToken("valid-farmer-a");
        var meResp = await farmerClient.GetAsync("/api/v1/me");
        meResp.EnsureSuccessStatusCode();

        // Farmer requests Admin role -> stays PendingApproval
        var reqResp = await farmerClient.PostAsJsonAsync("/api/v1/me/role-requests", new
        {
            requestedRole = "Admin",
            justification = "Attempt self escalation"
        });
        Assert.Equal(HttpStatusCode.Accepted, reqResp.StatusCode);

        // Unapproved admin MUST get 403 Forbidden
        var adminResp = await farmerClient.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, adminResp.StatusCode);

        // Real admin CAN access
        var realAdmin = ClientWithToken("valid-admin");
        var okAdminResp = await realAdmin.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, okAdminResp.StatusCode);
    }

    [Fact]
    public async Task Security_IdorAndUploadValidation_EnforcedStrictly()
    {
        var farmerA = ClientWithToken("valid-farmer-a");
        var farmerB = ClientWithToken("valid-farmer-b");

        // Farmer A creates farm
        var createFarm = await farmerA.PostAsJsonAsync("/api/v1/farms", new
        {
            name = "Vườn Farmer A",
            province = "Vĩnh Long",
            commune = "Xã Châu Hưng",
            areaHa = 1.2m,
            treeCount = 240,
            variety = "TallDuaTa",
            ageYears = 10,
            hasLocationConsent = true,
            hasPhotoConsent = true
        });
        createFarm.EnsureSuccessStatusCode();
        var farmJson = await createFarm.Content.ReadFromJsonAsync<JsonElement>();
        var farmId = farmJson.GetProperty("id").GetGuid();

        // Farmer B cannot read Farmer A's farm (IDOR)
        var idorResp = await farmerB.GetAsync($"/api/v1/farms/{farmId}");
        Assert.Equal(HttpStatusCode.Forbidden, idorResp.StatusCode);

        // Spoofed PDF (HTML payload masquerading as PDF) must fail magic-bytes check
        using var spoofContent = new MultipartFormDataContent();
        var fakePdf = new ByteArrayContent(Encoding.UTF8.GetBytes("<html><script>alert(1)</script></html>"));
        fakePdf.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        spoofContent.Add(fakePdf, "file", "evil.pdf");
        var spoofResp = await farmerA.PostAsync($"/api/v1/farms/{farmId}/files", spoofContent);
        Assert.Equal(HttpStatusCode.BadRequest, spoofResp.StatusCode);
        Assert.Contains("MIME_SIGNATURE_MISMATCH", await spoofResp.Content.ReadAsStringAsync());

        // Oversized valid PDF (> 512 KB configured limit in test) must be rejected
        var bigBytes = new byte[600 * 1024];
        Encoding.ASCII.GetBytes("%PDF-1.4\n").CopyTo(bigBytes, 0);
        using var bigContent = new MultipartFormDataContent();
        var bigFile = new ByteArrayContent(bigBytes);
        bigFile.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        bigContent.Add(bigFile, "file", "big.pdf");
        var bigResp = await farmerA.PostAsync($"/api/v1/farms/{farmId}/files", bigContent);
        Assert.Equal(HttpStatusCode.BadRequest, bigResp.StatusCode);
        Assert.Contains("FILE_TOO_LARGE", await bigResp.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task EndToEnd_FarmCalculatorUploadAiReviewProjectRfq_AndAiFallback()
    {
        var farmerA = ClientWithToken("valid-farmer-a");
        var reviewer = ClientWithToken("valid-reviewer");
        var developer = ClientWithToken("valid-developer");
        var buyer = ClientWithToken("valid-buyer");

        // 1. Record consent & create farm
        var consentResp = await farmerA.PostAsJsonAsync("/api/v1/me/consents", new
        {
            consentType = "AiThirdPartyProcessing",
            granted = true,
            policyVersion = "v1.0",
            textShown = "Đồng ý xử lý dữ liệu"
        });
        consentResp.EnsureSuccessStatusCode();

        var farmResp = await farmerA.PostAsJsonAsync("/api/v1/farms", new
        {
            name = "Vườn E2E Giồng Trôm",
            province = "Vĩnh Long",
            commune = "Giồng Trôm",
            areaHa = 1.0m,
            treeCount = 200,
            variety = "TallDuaTa",
            ageYears = 10,
            hasLocationConsent = true,
            hasPhotoConsent = true
        });
        farmResp.EnsureSuccessStatusCode();
        var farmId = (await farmResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // 2. Calculator via C# backend
        var calcResp = await farmerA.PostAsJsonAsync("/api/v1/carbon-calculator/estimate", new
        {
            farmId,
            areaHa = 1.0m,
            treeCount = 200,
            variety = "TallDuaTa",
            treeAgeYears = 10
        });
        calcResp.EnsureSuccessStatusCode();
        var calcJson = await calcResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(75.2436m, calcJson.GetProperty("estimatedCO2EquivalentT").GetDecimal());

        // Unsupported age rejected with 422
        var badCalcResp = await farmerA.PostAsJsonAsync("/api/v1/carbon-calculator/estimate", new
        {
            farmId,
            areaHa = 1.0m,
            treeCount = 200,
            variety = "TallDuaTa",
            treeAgeYears = 7
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badCalcResp.StatusCode);

        // 3. Upload valid PDF & get signed download URL
        var pdfBytes = Encoding.ASCII.GetBytes("%PDF-1.4\n1 0 obj\n<< /Type /Catalog >>\nendobj\n%%EOF");
        using var uploadForm = new MultipartFormDataContent();
        var pdfContent = new ByteArrayContent(pdfBytes);
        pdfContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/pdf");
        uploadForm.Add(pdfContent, "file", "so_do_vuon.pdf");
        var uploadResp = await farmerA.PostAsync($"/api/v1/farms/{farmId}/files", uploadForm);
        Assert.Equal(HttpStatusCode.Created, uploadResp.StatusCode);
        var fileId = (await uploadResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var signedResp = await farmerA.GetAsync($"/api/v1/files/{fileId}/signed-url");
        signedResp.EnsureSuccessStatusCode();
        var downloadUrl = (await signedResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("downloadUrl").GetString()!;

        var anonClient = ClientWithToken();
        var dlOk = await anonClient.GetAsync(downloadUrl);
        Assert.Equal(HttpStatusCode.OK, dlOk.StatusCode);
        var dlTampered = await anonClient.GetAsync(downloadUrl + "00");
        Assert.Equal(HttpStatusCode.Unauthorized, dlTampered.StatusCode);

        // 4. AI analysis normal & outage fallback
        _factory.StubAi.SimulateOutage = false;
        var aiResp = await farmerA.PostAsJsonAsync("/api/v1/ai/document-analysis", new { fileId, farmId });
        aiResp.EnsureSuccessStatusCode();
        var aiJson = await aiResp.Content.ReadFromJsonAsync<JsonElement>();
        var analysisId = aiJson.GetProperty("analysisId").GetGuid();
        Assert.False(aiJson.GetProperty("isFallback").GetBoolean());

        // Reviewer submits decision
        var reviewResp = await reviewer.PostAsJsonAsync($"/api/v1/ai/analyses/{analysisId}/review", new
        {
            decision = "Accepted",
            comment = "Đã đối chiếu hồ sơ tham chiếu."
        });
        reviewResp.EnsureSuccessStatusCode();

        // Simulate AI outage -> graceful fallback + calculator still works
        _factory.StubAi.SimulateOutage = true;
        var fallbackResp = await farmerA.PostAsJsonAsync("/api/v1/ai/document-analysis", new { fileId, farmId });
        fallbackResp.EnsureSuccessStatusCode();
        Assert.True((await fallbackResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("isFallback").GetBoolean());
        _factory.StubAi.SimulateOutage = false;

        // 5. Developer creates project & Buyer submits RFQ
        var projResp = await developer.PostAsJsonAsync("/api/v1/projects", new
        {
            name = "Dự án E2E Vùng Dừa Vĩnh Long",
            code = $"PRJ-E2E-{Guid.NewGuid():N}"[..16],
            projectType = "Coconut Soil & Biomass",
            province = "Vĩnh Long",
            declaredTotalAreaHa = 1.0m,
            intendedMethodology = "Đánh giá bước đầu"
        });
        projResp.EnsureSuccessStatusCode();
        var projId = (await projResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var rfqResp = await buyer.PostAsJsonAsync("/api/v1/rfqs", new
        {
            projectId = projId,
            buyerOrganization = "Buyer E2E Corp",
            inquiryScope = "Hợp tác tìm hiểu thông tin",
            message = "Xin lịch sử dữ liệu vườn dừa."
        });
        Assert.Equal(HttpStatusCode.Created, rfqResp.StatusCode);
    }

    [Fact]
    public async Task CooperativeDashboard_EnforcesManagerScope_AndExcludesUnauthorizedFarms()
    {
        var coopClient = ClientWithToken("valid-coop");
        var farmerA = ClientWithToken("valid-farmer-a");

        // Create a new cooperative managed by demo-coop-uid
        var coopResp = await coopClient.PostAsJsonAsync("/api/v1/cooperatives", new
        {
            name = "HTX Test Scope",
            code = $"HTX-T-{Guid.NewGuid():N}"[..12],
            province = "Vĩnh Long",
            commune = "Giồng Trôm"
        });
        coopResp.EnsureSuccessStatusCode();
        var coopId = (await coopResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        // Farmer A cannot view Cooperative dashboard
        var unauthDash = await farmerA.GetAsync($"/api/v1/cooperatives/{coopId}/dashboard");
        Assert.Equal(HttpStatusCode.Forbidden, unauthDash.StatusCode);

        // Farmer A creates farm (3.0 ha) & Coop links it WITHOUT authorization -> excluded from summary
        var fResp = await farmerA.PostAsJsonAsync("/api/v1/farms", new
        {
            name = "Vườn chưa ủy quyền",
            province = "Vĩnh Long",
            commune = "Giồng Trôm",
            areaHa = 3.0m,
            treeCount = 600,
            variety = "TallDuaTa",
            ageYears = 10,
            hasLocationConsent = false,
            hasPhotoConsent = false
        });
        fResp.EnsureSuccessStatusCode();
        var fId = (await fResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var linkResp = await coopClient.PostAsJsonAsync($"/api/v1/cooperatives/{coopId}/farms", new
        {
            farmId = fId,
            isDataAuthorized = false
        });
        linkResp.EnsureSuccessStatusCode();

        var dashResp = await coopClient.GetAsync($"/api/v1/cooperatives/{coopId}/dashboard");
        dashResp.EnsureSuccessStatusCode();
        var dashJson = await dashResp.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, dashJson.GetProperty("summary").GetProperty("authorizedFarmCount").GetInt32());
        Assert.Equal(0m, dashJson.GetProperty("summary").GetProperty("totalDeclaredAreaHa").GetDecimal());
    }

    [Fact]
    public async Task FactorVersioning_PreservesImmutableCalculationHistory()
    {
        using var isolatedFactory = new CocoCarbonApiFactory();
        var farmerA = isolatedFactory.CreateClient();
        farmerA.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-farmer-a");
        var admin = isolatedFactory.CreateClient();
        admin.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "valid-admin");

        // 1. Run calculation using v1.0 factor (75.2436)
        var calc1 = await farmerA.PostAsJsonAsync("/api/v1/carbon-calculator/estimate", new
        {
            areaHa = 1.0m,
            treeCount = 200,
            variety = "TallDuaTa",
            treeAgeYears = 10
        });
        calc1.EnsureSuccessStatusCode();
        var calc1Id = (await calc1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("calculationId").GetGuid();

        // 2. Admin creates v2.0 factor for TallDuaTa 10y (e.g., 80.0000)
        var sourcesResp = await admin.GetAsync("/api/v1/research-sources");
        var sourcesJson = await sourcesResp.Content.ReadFromJsonAsync<JsonElement>();
        var r1Id = sourcesJson.EnumerateArray().First(s => s.GetProperty("code").GetString() == "R1").GetProperty("id").GetGuid();

        var createFactorResp = await admin.PostAsJsonAsync("/api/v1/admin/research-factors", new
        {
            code = "CTU2015-TALL-10Y",
            variety = "TallDuaTa",
            varietyDisplayName = "Dừa cao - Dừa Ta (Tall)",
            treeAgeYears = 10,
            valueTCO2PerHa = 80.0000m,
            unit = "tCO2/ha",
            sourceId = r1Id,
            version = $"TEST-v2-{Guid.NewGuid():N}"[..16],
            validFrom = DateTimeOffset.UtcNow,
            applicabilityNotes = "Test version immutability"
        });
        createFactorResp.EnsureSuccessStatusCode();

        // 3. Verify historic calculation STILL retains 75.2436 and CTU-2015-v1.0
        var historyResp = await farmerA.GetAsync("/api/v1/carbon-calculator/history");
        historyResp.EnsureSuccessStatusCode();
        var histJson = await historyResp.Content.ReadFromJsonAsync<JsonElement>();
        var historicItem = histJson.EnumerateArray().First(x => x.GetProperty("id").GetGuid() == calc1Id);
        Assert.Equal(75.2436m, historicItem.GetProperty("estimatedCO2EquivalentT").GetDecimal());
        Assert.Equal("CTU-2015-v1.0", historicItem.GetProperty("factorVersion").GetString());
    }
}
