using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

[ApiController]
[Route("/api/v1")]
public sealed class ProjectsAndRfqsController(CocoCarbonDbContext db, IAuditService audit) : ControllerBase
{
    [HttpGet("projects")]
    [AllowAnonymous]
    public async Task<IActionResult> ListProjects(
        [FromQuery] string? province,
        [FromQuery] ProjectStatus? status,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = db.Projects
            .Include(p => p.Cooperative)
            .Include(p => p.Listings)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(province))
            query = query.Where(p => p.Province.Contains(province.Trim()));

        if (status.HasValue)
            query = query.Where(p => p.ProjectStatus == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Code.ToLower().Contains(term) ||
                p.IntendedMethodology.ToLower().Contains(term));
        }

        var projects = await query
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new
            {
                p.Id,
                p.Code,
                p.Name,
                p.ProjectType,
                p.Province,
                p.HistoricalRegion,
                p.DeclaredTotalAreaHa,
                p.IntendedMethodology,
                p.ReadinessScore,
                p.ReadinessLabel,
                p.VerificationStatus,
                p.ProjectStatus,
                p.DataOriginTag,
                p.Disclaimer,
                cooperativeName = p.Cooperative != null ? p.Cooperative.Name : null,
                listingsCount = p.Listings.Count(l => l.IsPublic),
                p.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(new
        {
            directoryNotice = "Đây là danh mục thông tin dự án và tiếp nhận yêu cầu tìm hiểu (Directory/RFQ), KHÔNG phải sàn giao dịch carbon được cấp phép hay registry phát hành tín chỉ.",
            projects
        });
    }

    [HttpGet("projects/{id:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetProject(Guid id, CancellationToken ct)
    {
        var project = await db.Projects
            .Include(p => p.Cooperative)
            .Include(p => p.ProjectFarms)
            .Include(p => p.CreditLots)
            .Include(p => p.Listings)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

        if (project is null) return NotFound();

        return Ok(new
        {
            project.Id,
            project.Code,
            project.Name,
            project.DeveloperId,
            project.CooperativeId,
            cooperativeName = project.Cooperative?.Name,
            project.ProjectType,
            project.Province,
            project.HistoricalRegion,
            project.DeclaredTotalAreaHa,
            participatingFarmCount = project.ProjectFarms.Count(pf => pf.ParticipationStatus == ParticipationStatus.Active),
            project.IntendedMethodology,
            project.MethodologyNotes,
            project.BaselineSummary,
            project.MonitoringPlanSummary,
            project.ReadinessScore,
            project.ReadinessLabel,
            project.VerificationStatus,
            project.ProjectStatus,
            project.DataOriginTag,
            project.Disclaimer,
            creditLots = project.CreditLots.Select(cl => new
            {
                cl.Id,
                cl.RegistryName,
                cl.SerialRange,
                cl.VintageYear,
                cl.QuantityTCO2e,
                cl.IssuanceStatus,
                cl.HasOfficialRegistryEvidence,
                cl.VerificationEvidenceRef,
                cl.DataOriginTag
            }),
            listings = project.Listings.Where(l => l.IsPublic).Select(l => new
            {
                l.Id, l.Title, l.Summary, l.StageNote, l.DataOriginTag, l.CreatedAt
            }),
            project.CreatedAt,
            project.UpdatedAt
        });
    }

    [HttpPost("projects")]
    [Authorize(Roles = "ProjectDeveloper,Admin")]
    public async Task<IActionResult> CreateProject([FromBody] CreateProjectRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new { message = "Tên và mã dự án không được để trống." });
        if (req.DeclaredTotalAreaHa < 0m)
            return BadRequest(new { message = "Diện tích khai báo không hợp lệ." });

        var userId = User.GetInternalUserId();
        var project = new Project
        {
            DeveloperId = userId,
            CooperativeId = req.CooperativeId,
            Name = req.Name.Trim(),
            Code = req.Code.Trim().ToUpperInvariant(),
            ProjectType = string.IsNullOrWhiteSpace(req.ProjectType) ? "Coconut Agroforestry & Soil Management" : req.ProjectType.Trim(),
            Province = string.IsNullOrWhiteSpace(req.Province) ? ScientificConstants.CurrentProvinceDefault : req.Province.Trim(),
            HistoricalRegion = string.IsNullOrWhiteSpace(req.HistoricalRegion) ? ScientificConstants.HistoricalRegionBenTre : req.HistoricalRegion.Trim(),
            DeclaredTotalAreaHa = req.DeclaredTotalAreaHa,
            IntendedMethodology = string.IsNullOrWhiteSpace(req.IntendedMethodology)
                ? "Đang chuẩn bị phương pháp luận (chưa phê duyệt)"
                : req.IntendedMethodology.Trim(),
            MethodologyNotes = req.MethodologyNotes?.Trim() ?? "",
            BaselineSummary = req.BaselineSummary?.Trim() ?? "",
            MonitoringPlanSummary = req.MonitoringPlanSummary?.Trim() ?? "",
            ProjectStatus = ProjectStatus.DataCollection,
            VerificationStatus = "Chưa xác minh độc lập",
            DataOriginTag = DataOriginTag.UserDeclared
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync(ct);

        // Automatically create an informational directory listing
        db.Listings.Add(new Listing
        {
            ProjectId = project.Id,
            SellerUserId = userId,
            Title = project.Name,
            Summary = $"Hồ sơ giai đoạn {project.ProjectStatus}; sẵn sàng tiếp nhận trao đổi RFQ (không giao dịch tài chính trên nền tảng).",
            DataOriginTag = DataOriginTag.UserDeclared
        });
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "PROJECT_CREATED", "Project", project.Id.ToString(),
            null, new { project.Code, project.Name, project.ProjectStatus }, null, ct);

        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, new
        {
            project.Id,
            project.Code,
            project.Name,
            project.ProjectType,
            project.Province,
            project.HistoricalRegion,
            project.DeclaredTotalAreaHa,
            project.IntendedMethodology,
            project.ReadinessScore,
            project.ReadinessLabel,
            project.VerificationStatus,
            project.ProjectStatus,
            project.DataOriginTag,
            project.Disclaimer,
            project.CreatedAt
        });
    }

    [HttpPatch("projects/{id:guid}")]
    [Authorize(Roles = "ProjectDeveloper,Admin")]
    public async Task<IActionResult> UpdateProject(Guid id, [FromBody] UpdateProjectRequest req, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (project.DeveloperId != userId && !User.HasApprovedRole(RoleCode.Admin))
            return Forbid();

        if (req.ProjectStatus == ProjectStatus.RegisteredExternalEvidenceOnly &&
            string.IsNullOrWhiteSpace(req.OfficialRegistryEvidenceRef))
        {
            return BadRequest(new
            {
                message = "Không thể đặt trạng thái đã đăng ký nếu thiếu bằng chứng registry chính thức (OfficialRegistryEvidenceRef)."
            });
        }

        if (!string.IsNullOrWhiteSpace(req.Name)) project.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.IntendedMethodology)) project.IntendedMethodology = req.IntendedMethodology.Trim();
        if (req.MethodologyNotes is not null) project.MethodologyNotes = req.MethodologyNotes.Trim();
        if (req.BaselineSummary is not null) project.BaselineSummary = req.BaselineSummary.Trim();
        if (req.MonitoringPlanSummary is not null) project.MonitoringPlanSummary = req.MonitoringPlanSummary.Trim();
        if (req.ReadinessScore.HasValue) project.ReadinessScore = Math.Clamp(req.ReadinessScore.Value, 0m, 100m);
        if (req.ProjectStatus.HasValue)
        {
            project.ProjectStatus = req.ProjectStatus.Value;
            if (req.ProjectStatus.Value == ProjectStatus.RegisteredExternalEvidenceOnly &&
                !string.IsNullOrWhiteSpace(req.OfficialRegistryEvidenceRef))
            {
                project.VerificationStatus = $"Có tham chiếu bằng chứng bên ngoài: {req.OfficialRegistryEvidenceRef.Trim()}";
            }
        }
        project.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "PROJECT_UPDATED", "Project", project.Id.ToString(),
            null, new { project.ProjectStatus, project.ReadinessScore, project.VerificationStatus }, null, ct);

        return Ok(new
        {
            project.Id,
            project.Code,
            project.Name,
            project.IntendedMethodology,
            project.MethodologyNotes,
            project.BaselineSummary,
            project.MonitoringPlanSummary,
            project.ReadinessScore,
            project.VerificationStatus,
            project.ProjectStatus,
            project.UpdatedAt
        });
    }

    [HttpPost("projects/{id:guid}/farms")]
    [Authorize(Roles = "ProjectDeveloper,Admin")]
    public async Task<IActionResult> LinkFarmToProject(Guid id, [FromBody] AddProjectFarmRequest req, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (project is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (project.DeveloperId != userId && !User.HasApprovedRole(RoleCode.Admin))
            return Forbid();

        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == req.FarmId, ct);
        if (farm is null) return NotFound(new { message = "Vườn dừa không tồn tại." });

        var existing = await db.ProjectFarms.FirstOrDefaultAsync(pf => pf.ProjectId == id && pf.FarmId == req.FarmId, ct);
        if (existing is null)
        {
            existing = new ProjectFarm
            {
                ProjectId = id,
                FarmId = req.FarmId,
                HasFarmerConsent = req.HasFarmerConsent,
                ParticipationStatus = ParticipationStatus.Active
            };
            db.ProjectFarms.Add(existing);
            await db.SaveChangesAsync(ct);
        }

        // Recompute declared project area from participating farms
        project.DeclaredTotalAreaHa = await db.ProjectFarms
            .Where(pf => pf.ProjectId == id && pf.ParticipationStatus == ParticipationStatus.Active && pf.Farm != null)
            .SumAsync(pf => pf.Farm!.AreaHa, ct);
        await db.SaveChangesAsync(ct);

        return Ok(new { existing.Id, project.DeclaredTotalAreaHa });
    }

    [HttpPost("rfqs")]
    [Authorize]
    public async Task<IActionResult> CreateRfq([FromBody] CreateRfqRequest req, CancellationToken ct)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == req.ProjectId, ct);
        if (project is null) return NotFound(new { message = "Dự án không tồn tại." });
        if (string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { message = "Nội dung yêu cầu trao đổi (RFQ) không được để trống." });

        var buyerId = User.GetInternalUserId();
        var rfq = new Rfq
        {
            ProjectId = project.Id,
            ListingId = req.ListingId,
            BuyerUserId = buyerId,
            SellerUserId = project.DeveloperId,
            BuyerOrganization = string.IsNullOrWhiteSpace(req.BuyerOrganization) ? User.GetEmail() : req.BuyerOrganization.Trim(),
            InquiryScope = req.InquiryScope.Trim(),
            Message = req.Message.Trim(),
            Status = RfqStatus.Submitted,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        db.Rfqs.Add(rfq);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(buyerId, User.GetEmail(), "RFQ_CREATED", "Rfq", rfq.Id.ToString(),
            null, new { rfq.ProjectId, rfq.BuyerOrganization, rfq.Status }, null, ct);

        return CreatedAtAction(nameof(ListRfqs), null, new
        {
            rfq.Id,
            rfq.ProjectId,
            rfq.BuyerOrganization,
            rfq.InquiryScope,
            rfq.Message,
            rfq.Status,
            notice = "RFQ chỉ là kênh trao đổi thông tin ban đầu, không phải hợp đồng mua bán hay thanh toán tín chỉ trong MVP.",
            rfq.CreatedAt
        });
    }

    [HttpGet("rfqs")]
    [Authorize]
    public async Task<IActionResult> ListRfqs(CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var query = db.Rfqs.Include(r => r.Project).AsQueryable();

        if (!User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
        {
            query = query.Where(r => r.BuyerUserId == userId || r.SellerUserId == userId);
        }

        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.ProjectId,
                projectName = r.Project != null ? r.Project.Name : "",
                r.BuyerUserId,
                r.SellerUserId,
                r.BuyerOrganization,
                r.InquiryScope,
                r.Message,
                r.Status,
                r.SellerResponse,
                r.CreatedAt,
                r.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("rfqs/{id:guid}/respond")]
    [Authorize]
    public async Task<IActionResult> RespondRfq(Guid id, [FromBody] RespondRfqRequest req, CancellationToken ct)
    {
        var rfq = await db.Rfqs.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (rfq is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (rfq.SellerUserId != userId && !User.HasApprovedRole(RoleCode.Admin))
            return Forbid();

        rfq.Status = req.Status;
        rfq.SellerResponse = req.SellerResponse.Trim();
        rfq.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "RFQ_RESPONDED", "Rfq", rfq.Id.ToString(),
            null, new { rfq.Status }, null, ct);

        return Ok(new
        {
            rfq.Id,
            rfq.ProjectId,
            rfq.Status,
            rfq.SellerResponse,
            rfq.UpdatedAt
        });
    }
}
