using System.Text.Json;
using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.AiClient;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using CocoCarbon.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

[ApiController]
[Route("/api/v1/ai")]
[Authorize]
public sealed class AiAuditController(
    CocoCarbonDbContext db,
    IPrivateFileStorageService storage,
    IPythonAiServiceClient aiClient,
    IAuditService audit) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [HttpPost("document-analysis")]
    public async Task<IActionResult> AnalyzeDocument([FromBody] AnalyzeDocumentRequest req, CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var file = await db.FarmFiles
            .Include(f => f.Farm)
            .FirstOrDefaultAsync(f => f.Id == req.FileId, ct);

        if (file is null)
            return NotFound(new { message = "Tài liệu không tồn tại." });

        var canAccess = file.UploadedByUserId == userId ||
                        file.Farm?.OwnerUserId == userId ||
                        User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer);
        if (!canAccess)
            return Forbid();

        byte[] fileBytes;
        await using (var stream = await storage.OpenReadAsync(file.FileKey, ct))
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, ct);
            fileBytes = ms.ToArray();
        }

        // Send only non-sensitive matching fields + known hashes to internal AI service
        var farmContext = file.Farm is null ? null : new
        {
            farmId = file.Farm.Id,
            farmName = file.Farm.Name,
            province = file.Farm.Province,
            commune = file.Farm.Commune,
            areaHa = file.Farm.AreaHa,
            treeCount = file.Farm.TreeCount,
            variety = file.Farm.Variety.ToString(),
            ageYears = file.Farm.AgeYears
        };

        object? projectContext = null;
        if (req.ProjectId.HasValue)
        {
            var proj = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
            if (proj is not null)
            {
                projectContext = new
                {
                    projectId = proj.Id,
                    proj.Name,
                    proj.IntendedMethodology,
                    proj.BaselineSummary,
                    proj.MonitoringPlanSummary,
                    proj.DeclaredTotalAreaHa,
                    status = proj.ProjectStatus.ToString()
                };
            }
        }

        var knownHashes = await db.FarmFiles
            .Where(f => f.Id != file.Id)
            .Select(f => f.Sha256Hash)
            .Distinct()
            .Take(500)
            .ToListAsync(ct);

        var aiResult = await aiClient.AnalyzeDocumentAsync(
            new AiDocumentAnalysisRequest(
                FileId: file.Id,
                FileName: file.FileName,
                MimeType: file.MimeType,
                Sha256Hash: file.Sha256Hash,
                FileBytes: fileBytes,
                FarmContext: farmContext,
                ProjectContext: projectContext,
                KnownHashes: knownHashes),
            ct);

        var entity = new AiAnalysis
        {
            FileId = file.Id,
            FarmId = req.FarmId ?? file.FarmId,
            ProjectId = req.ProjectId,
            RequestedByUserId = userId,
            Provider = aiResult.Provider,
            ModelName = aiResult.ModelName,
            PromptVersion = aiResult.PromptVersion,
            Status = aiResult.Status,
            IsFallbackResult = aiResult.IsFallback,
            StructuredOutputJson = JsonSerializer.Serialize(new
            {
                aiResult.ExtractedFields,
                aiResult.Findings,
                aiResult.MissingFields,
                aiResult.Duplicates,
                aiResult.ConfidenceNotes
            }, JsonOpts),
            EvidenceJson = JsonSerializer.Serialize(aiResult.Evidence, JsonOpts),
            ReadinessRubricJson = JsonSerializer.Serialize(aiResult.ReadinessRubric, JsonOpts),
            ReviewerRequired = true,
            ReviewStatus = ReviewDecisionStatus.PendingReview,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.AiAnalyses.Add(entity);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "AI_DOCUMENT_ANALYSIS_RUN", "AiAnalysis", entity.Id.ToString(),
            null, new { file.Id, entity.Provider, entity.ModelName, entity.PromptVersion, entity.IsFallbackResult }, null, ct);

        return Ok(new
        {
            analysisId = entity.Id,
            fileId = file.Id,
            status = entity.Status,
            isFallback = entity.IsFallbackResult,
            provenance = "AI_EXTRACTED",
            verificationNotice = "Chưa xác minh độc lập; kết quả AI chỉ mang tính hỗ trợ và bắt buộc có chuyên viên rà soát.",
            extractedFields = aiResult.ExtractedFields,
            findings = aiResult.Findings,
            missingFields = aiResult.MissingFields,
            duplicates = aiResult.Duplicates,
            evidence = aiResult.Evidence,
            confidenceNotes = aiResult.ConfidenceNotes,
            readinessRubric = aiResult.ReadinessRubric,
            reviewerRequired = true,
            reviewStatus = entity.ReviewStatus,
            modelName = entity.ModelName,
            promptVersion = entity.PromptVersion,
            createdAt = entity.CreatedAt
        });
    }

    [HttpGet("analyses")]
    public async Task<IActionResult> ListAnalyses(CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var query = db.AiAnalyses
            .Include(a => a.File)
            .Include(a => a.ReviewerDecisions)
            .AsQueryable();

        if (!User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
        {
            query = query.Where(a => a.RequestedByUserId == userId);
        }

        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Take(100)
            .Select(a => new
            {
                a.Id,
                a.FileId,
                fileName = a.File != null ? a.File.FileName : null,
                a.FarmId,
                a.ProjectId,
                a.Provider,
                a.ModelName,
                a.PromptVersion,
                a.Status,
                a.IsFallbackResult,
                a.ReviewerRequired,
                a.ReviewStatus,
                decisionsCount = a.ReviewerDecisions.Count,
                a.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("analyses/{id:guid}")]
    public async Task<IActionResult> GetAnalysis(Guid id, CancellationToken ct)
    {
        var analysis = await db.AiAnalyses
            .Include(a => a.File)
            .Include(a => a.ReviewerDecisions)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        if (analysis is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (analysis.RequestedByUserId != userId && !User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
            return Forbid();

        return Ok(new
        {
            analysisId = analysis.Id,
            analysis.FileId,
            fileName = analysis.File?.FileName,
            analysis.FarmId,
            analysis.ProjectId,
            analysis.Provider,
            analysis.ModelName,
            analysis.PromptVersion,
            analysis.Status,
            analysis.IsFallbackResult,
            structuredOutput = JsonSerializer.Deserialize<JsonElement>(analysis.StructuredOutputJson),
            evidence = JsonSerializer.Deserialize<JsonElement>(analysis.EvidenceJson),
            readinessRubric = JsonSerializer.Deserialize<JsonElement>(analysis.ReadinessRubricJson),
            analysis.ReviewerRequired,
            analysis.ReviewStatus,
            reviewerDecisions = analysis.ReviewerDecisions.OrderByDescending(d => d.CreatedAt).Select(d => new
            {
                d.Id, d.ReviewerUserId, d.Decision, d.Comment, d.EditedOutputJson, d.CreatedAt
            }),
            analysis.CreatedAt
        });
    }

    [HttpPost("analyses/{id:guid}/review")]
    [Authorize(Roles = "Reviewer,Admin")]
    public async Task<IActionResult> SubmitReviewDecision(
        Guid id,
        [FromBody] SubmitReviewDecisionRequest req,
        CancellationToken ct)
    {
        var analysis = await db.AiAnalyses.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (analysis is null) return NotFound();

        var reviewerId = User.GetInternalUserId();
        var prevStatus = analysis.ReviewStatus;

        var decision = new ReviewerDecision
        {
            AiAnalysisId = analysis.Id,
            ReviewerUserId = reviewerId,
            Decision = req.Decision,
            EditedOutputJson = req.EditedOutputJson,
            Comment = req.Comment.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Preserve original AI output untouched; update review status separately
        analysis.ReviewStatus = req.Decision;

        db.ReviewerDecisions.Add(decision);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(reviewerId, User.GetEmail(), "AI_ANALYSIS_REVIEWED", "AiAnalysis", analysis.Id.ToString(),
            new { prevStatus }, new { analysis.ReviewStatus, decision.Id, req.Comment }, null, ct);

        return Ok(new
        {
            decision.Id,
            analysisId = analysis.Id,
            decision = decision.Decision,
            decision.Comment,
            note = "Kết quả rà soát nội bộ được lưu song song với kết quả AI gốc; đây không phải xác minh VVB độc lập.",
            decision.CreatedAt
        });
    }

    [HttpPost("assistant/query")]
    public async Task<IActionResult> QueryAssistant([FromBody] AssistantQueryRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Question))
            return BadRequest(new { message = "Câu hỏi không được để trống." });

        var userId = User.GetInternalUserId();
        object? context = null;

        if (req.FarmId.HasValue)
        {
            var farm = await db.Farms.AsNoTracking()
                .Include(f => f.Calculations)
                .FirstOrDefaultAsync(f => f.Id == req.FarmId.Value, ct);
            if (farm is not null && (farm.OwnerUserId == userId || User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer)))
            {
                var latestCalc = farm.Calculations.OrderByDescending(c => c.CreatedAt).FirstOrDefault();
                context = new
                {
                    farmName = farm.Name,
                    farm.AreaHa,
                    farm.TreeCount,
                    variety = farm.Variety.ToString(),
                    farm.AgeYears,
                    latestEstimatedCO2EquivalentT = latestCalc?.EstimatedCO2EquivalentT,
                    latestObservedDensity = latestCalc?.ObservedTreeDensityPerHa
                };
            }
        }
        else if (req.ProjectId.HasValue)
        {
            var proj = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.Id == req.ProjectId.Value, ct);
            if (proj is not null)
            {
                context = new
                {
                    proj.Name,
                    proj.Province,
                    proj.HistoricalRegion,
                    proj.IntendedMethodology,
                    proj.ReadinessScore,
                    status = proj.ProjectStatus.ToString(),
                    proj.VerificationStatus
                };
            }
        }

        var response = await aiClient.QueryAssistantAsync(
            new AiAssistantRequest(req.Role, req.Question.Trim(), context), ct);

        return Ok(response);
    }
}
