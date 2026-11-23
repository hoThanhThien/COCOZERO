using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CocoCarbon.Infrastructure.AiClient;

public sealed record AiDocumentAnalysisRequest(
    Guid FileId,
    string FileName,
    string MimeType,
    string Sha256Hash,
    byte[] FileBytes,
    object? FarmContext,
    object? ProjectContext,
    IReadOnlyCollection<string> KnownHashes);

public sealed record AiExtractedFieldDto(
    [property: JsonPropertyName("field")] string Field,
    [property: JsonPropertyName("value")] string? Value,
    [property: JsonPropertyName("confidence")] double Confidence,
    [property: JsonPropertyName("provenance")] string Provenance);

public sealed record AiFindingDto(
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("evidenceRef")] string? EvidenceRef);

public sealed record AiDuplicateDto(
    [property: JsonPropertyName("duplicateType")] string DuplicateType,
    [property: JsonPropertyName("matchedId")] string? MatchedId,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record AiEvidenceDto(
    [property: JsonPropertyName("fileId")] string FileId,
    [property: JsonPropertyName("pageNumber")] int? PageNumber,
    [property: JsonPropertyName("snippet")] string Snippet,
    [property: JsonPropertyName("extractedValue")] string? ExtractedValue,
    [property: JsonPropertyName("confidence")] double Confidence,
    [property: JsonPropertyName("reason")] string Reason);

public sealed record AiReadinessRubricDto(
    [property: JsonPropertyName("label")] string Label,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("disclaimer")] string Disclaimer,
    [property: JsonPropertyName("items")] List<AiReadinessItemDto> Items);

public sealed record AiReadinessItemDto(
    [property: JsonPropertyName("criterion")] string Criterion,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("notes")] string Notes);

public sealed record AiDocumentAnalysisResponse(
    [property: JsonPropertyName("analysisId")] string AnalysisId,
    [property: JsonPropertyName("fileId")] string FileId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("isFallback")] bool IsFallback,
    [property: JsonPropertyName("extractedFields")] List<AiExtractedFieldDto> ExtractedFields,
    [property: JsonPropertyName("findings")] List<AiFindingDto> Findings,
    [property: JsonPropertyName("missingFields")] List<string> MissingFields,
    [property: JsonPropertyName("duplicates")] List<AiDuplicateDto> Duplicates,
    [property: JsonPropertyName("evidence")] List<AiEvidenceDto> Evidence,
    [property: JsonPropertyName("confidenceNotes")] List<string> ConfidenceNotes,
    [property: JsonPropertyName("readinessRubric")] AiReadinessRubricDto? ReadinessRubric,
    [property: JsonPropertyName("reviewerRequired")] bool ReviewerRequired,
    [property: JsonPropertyName("provider")] string Provider,
    [property: JsonPropertyName("modelName")] string ModelName,
    [property: JsonPropertyName("promptVersion")] string PromptVersion,
    [property: JsonPropertyName("createdAt")] DateTimeOffset CreatedAt);

public sealed record AiAssistantRequest(
    string Role,
    string Question,
    object? ContextData);

public sealed record AiCitationDto(
    [property: JsonPropertyName("sourceCode")] string SourceCode,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("url")] string Url,
    [property: JsonPropertyName("excerpt")] string Excerpt);

public sealed record AiAssistantResponse(
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("hasEnoughEvidence")] bool HasEnoughEvidence,
    [property: JsonPropertyName("citations")] List<AiCitationDto> Citations,
    [property: JsonPropertyName("disclaimer")] string Disclaimer,
    [property: JsonPropertyName("modelName")] string ModelName,
    [property: JsonPropertyName("isFallback")] bool IsFallback);

public interface IPythonAiServiceClient
{
    Task<AiDocumentAnalysisResponse> AnalyzeDocumentAsync(
        AiDocumentAnalysisRequest request,
        CancellationToken cancellationToken = default);

    Task<AiAssistantResponse> QueryAssistantAsync(
        AiAssistantRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PythonAiServiceClient : IPythonAiServiceClient
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<PythonAiServiceClient> _logger;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public PythonAiServiceClient(HttpClient http, IConfiguration config, ILogger<PythonAiServiceClient> logger)
    {
        _http = http;
        _config = config;
        _logger = logger;

        var baseUrl = config["AI_SERVICE_URL"] ?? "http://localhost:8000";
        _http.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/");
        var timeoutSec = int.TryParse(config["AI_SERVICE_TIMEOUT_SECONDS"], out var s) && s > 0 ? s : 30;
        _http.Timeout = TimeSpan.FromSeconds(timeoutSec);
    }

    public async Task<AiDocumentAnalysisResponse> AnalyzeDocumentAsync(
        AiDocumentAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(request.FileBytes);
            fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse(request.MimeType);
            form.Add(fileContent, "file", request.FileName);

            form.Add(new StringContent(request.FileId.ToString()), "file_id");
            form.Add(new StringContent(request.Sha256Hash), "sha256_hash");
            form.Add(new StringContent(JsonSerializer.Serialize(request.FarmContext ?? new { }, JsonOpts)), "farm_context_json");
            form.Add(new StringContent(JsonSerializer.Serialize(request.ProjectContext ?? new { }, JsonOpts)), "project_context_json");
            form.Add(new StringContent(JsonSerializer.Serialize(request.KnownHashes, JsonOpts)), "known_hashes_json");

            using var msg = new HttpRequestMessage(HttpMethod.Post, "internal/v1/analyze-document")
            {
                Content = form
            };
            var internalKey = _config["AI_SERVICE_INTERNAL_KEY"];
            if (!string.IsNullOrWhiteSpace(internalKey))
            {
                msg.Headers.Add("X-Internal-Api-Key", internalKey);
            }

            using var resp = await _http.SendAsync(msg, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("AI service returned HTTP {StatusCode}: {Body}", resp.StatusCode, body);
                return BuildFallbackAnalysis(request.FileId, $"AI service HTTP {(int)resp.StatusCode}");
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            var parsed = JsonSerializer.Deserialize<AiDocumentAnalysisResponse>(json, JsonOpts);
            return parsed ?? BuildFallbackAnalysis(request.FileId, "Invalid JSON payload from AI service");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "AI service unavailable or timed out for file {FileId}. Returning graceful fallback.", request.FileId);
            return BuildFallbackAnalysis(request.FileId, "Dịch vụ AI tạm thời không khả dụng hoặc hết thời gian chờ.");
        }
    }

    public async Task<AiAssistantResponse> QueryAssistantAsync(
        AiAssistantRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, "internal/v1/assistant/query")
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    role = request.Role,
                    question = request.Question,
                    context_data = request.ContextData ?? new { }
                }, JsonOpts), Encoding.UTF8, "application/json")
            };

            var internalKey = _config["AI_SERVICE_INTERNAL_KEY"];
            if (!string.IsNullOrWhiteSpace(internalKey))
            {
                msg.Headers.Add("X-Internal-Api-Key", internalKey);
            }

            using var resp = await _http.SendAsync(msg, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                return BuildFallbackAssistant();
            }

            var json = await resp.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<AiAssistantResponse>(json, JsonOpts) ?? BuildFallbackAssistant();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "AI assistant request failed; returning safe fallback.");
            return BuildFallbackAssistant();
        }
    }

    private static AiDocumentAnalysisResponse BuildFallbackAnalysis(Guid fileId, string reason) =>
        new(
            AnalysisId: Guid.NewGuid().ToString(),
            FileId: fileId.ToString(),
            Status: "Fallback_AiUnavailable",
            IsFallback: true,
            ExtractedFields: [],
            Findings:
            [
                new AiFindingDto(
                    Code: "AI_SERVICE_UNAVAILABLE",
                    Severity: "warning",
                    Category: "system_fallback",
                    Message: $"{reason} Hệ thống lõi C# (đăng ký vườn, calculator, upload) vẫn hoạt động bình thường; hồ sơ cần reviewer kiểm tra thủ công.",
                    EvidenceRef: null)
            ],
            MissingFields: ["Cần chuyên viên rà soát thủ công do AI tạm thời không khả dụng"],
            Duplicates: [],
            Evidence: [],
            ConfidenceNotes: ["Chưa xác minh độc lập; không có trích xuất AI hoàn chỉnh."],
            ReadinessRubric: null,
            ReviewerRequired: true,
            Provider: "fallback",
            ModelName: "none",
            PromptVersion: "fallback-v1",
            CreatedAt: DateTimeOffset.UtcNow);

    private static AiAssistantResponse BuildFallbackAssistant() =>
        new(
            Answer: "Trợ lý AI hiện tạm thời ngoại tuyến. Bạn vẫn có thể sử dụng đầy đủ Farm Registry, Coconut Carbon Calculator (C#) và Project Directory. Vui lòng tham khảo Register nguồn khoa học [R1]-[R12].",
            HasEnoughEvidence: false,
            Citations: [],
            Disclaimer: "Chưa xác minh độc lập. AI chỉ hỗ trợ tổng hợp tài liệu, không thay thế tổ chức thẩm định hoặc cơ quan quản lý.",
            ModelName: "fallback",
            IsFallback: true);
}
