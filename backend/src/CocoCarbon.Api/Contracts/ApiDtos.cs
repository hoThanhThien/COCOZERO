using System.Security.Claims;
using CocoCarbon.Api.Authentication;
using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Api.Contracts;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetInternalUserId(this ClaimsPrincipal principal)
    {
        var raw = principal.FindFirstValue(CocoClaimTypes.InternalUserId)
                  ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var id) ? id : Guid.Empty;
    }

    public static string GetEmail(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(ClaimTypes.Email) ?? "";

    public static bool HasApprovedRole(this ClaimsPrincipal principal, params RoleCode[] roles) =>
        roles.Any(r => principal.IsInRole(r.ToString()));
}

public sealed record UpdateProfileRequest(string DisplayName, string? Phone);

public sealed record RecordConsentRequest(ConsentType ConsentType, bool Granted, string PolicyVersion, string TextShown);

public sealed record RequestRoleUpgradeRequest(RoleCode RequestedRole, string? Justification);

public sealed record CreateFarmRequest(
    string Name,
    string Province,
    string Commune,
    string? HistoricalRegion,
    decimal AreaHa,
    int TreeCount,
    CoconutVariety Variety,
    int AgeYears,
    double? Latitude,
    double? Longitude,
    string? LocationGeoJson,
    string? CareHistoryJson,
    Guid? CooperativeId,
    bool HasLocationConsent,
    bool HasPhotoConsent,
    string? BoundaryPolygonGeoJson);

public sealed record UpdateFarmRequest(
    string? Name,
    string? Commune,
    decimal? AreaHa,
    int? TreeCount,
    CoconutVariety? Variety,
    int? AgeYears,
    double? Latitude,
    double? Longitude,
    string? CareHistoryJson,
    FarmStatus? Status);

public sealed record CreateObservationRequest(
    int TreeAgeYears,
    decimal? DbhCm,
    decimal? HeightM,
    string HealthStatus,
    string? PracticeDataJson);

public sealed record CarbonEstimateRequest(
    Guid? FarmId,
    decimal AreaHa,
    int TreeCount,
    CoconutVariety Variety,
    int TreeAgeYears,
    bool IncludeArithmeticPerTreeAllocation = false);

public sealed record AnalyzeDocumentRequest(
    Guid FileId,
    Guid? FarmId,
    Guid? ProjectId);

public sealed record SubmitReviewDecisionRequest(
    ReviewDecisionStatus Decision,
    string Comment,
    string? EditedOutputJson);

public sealed record AssistantQueryRequest(
    string Role,
    string Question,
    Guid? FarmId,
    Guid? ProjectId);

public sealed record CreateCooperativeRequest(
    string Name,
    string Code,
    string Province,
    string Commune,
    string? HistoricalRegion);

public sealed record AddCooperativeFarmRequest(Guid FarmId, bool IsDataAuthorized);

public sealed record CreateProjectRequest(
    string Name,
    string Code,
    string ProjectType,
    string Province,
    string? HistoricalRegion,
    decimal DeclaredTotalAreaHa,
    string IntendedMethodology,
    string? MethodologyNotes,
    string? BaselineSummary,
    string? MonitoringPlanSummary,
    Guid? CooperativeId);

public sealed record UpdateProjectRequest(
    string? Name,
    string? IntendedMethodology,
    string? MethodologyNotes,
    string? BaselineSummary,
    string? MonitoringPlanSummary,
    ProjectStatus? ProjectStatus,
    decimal? ReadinessScore,
    string? OfficialRegistryEvidenceRef);

public sealed record AddProjectFarmRequest(Guid FarmId, bool HasFarmerConsent);

public sealed record CreateRfqRequest(
    Guid ProjectId,
    Guid? ListingId,
    string BuyerOrganization,
    string InquiryScope,
    string Message);

public sealed record RespondRfqRequest(RfqStatus Status, string SellerResponse);

public sealed record AssignUserRoleRequest(
    RoleCode RoleCode,
    string ApprovalStatus,
    string? ScopeType,
    Guid? ScopeId);

public sealed record CreateResearchFactorVersionRequest(
    string Code,
    CoconutVariety Variety,
    string VarietyDisplayName,
    int TreeAgeYears,
    decimal ValueTCO2PerHa,
    string Unit,
    Guid SourceId,
    string Version,
    DateTimeOffset ValidFrom,
    string ApplicabilityNotes);
