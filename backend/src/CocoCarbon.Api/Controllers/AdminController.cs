using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

[ApiController]
[Route("/api/v1/admin")]
[Authorize(Roles = "Admin")]
public sealed class AdminController(CocoCarbonDbContext db, IAuditService audit) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken ct)
    {
        var users = await db.Users
            .Include(u => u.UserRoles)
            .OrderByDescending(u => u.CreatedAt)
            .Select(u => new
            {
                u.Id,
                u.FirebaseUid,
                u.Email,
                u.DisplayName,
                u.Status,
                u.CreatedAt,
                roles = u.UserRoles.Select(r => new { r.Id, roleCode = r.RoleCode.ToString(), r.ApprovalStatus, r.ScopeType })
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpPost("users/{id:guid}/roles")]
    public async Task<IActionResult> AssignOrApproveRole(Guid id, [FromBody] AssignUserRoleRequest req, CancellationToken ct)
    {
        var user = await db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user is null) return NotFound();

        var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == req.RoleCode, ct);
        if (role is null) return BadRequest(new { message = "Role không tồn tại." });

        var scopeType = string.IsNullOrWhiteSpace(req.ScopeType) ? "Global" : req.ScopeType.Trim();
        var existing = user.UserRoles.FirstOrDefault(ur => ur.RoleCode == req.RoleCode && ur.ScopeType == scopeType && ur.ScopeId == req.ScopeId);
        var adminId = User.GetInternalUserId();

        if (existing is null)
        {
            existing = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                RoleCode = req.RoleCode,
                ScopeType = scopeType,
                ScopeId = req.ScopeId,
                ApprovalStatus = string.IsNullOrWhiteSpace(req.ApprovalStatus) ? "Approved" : req.ApprovalStatus,
                ApprovedByUserId = adminId
            };
            db.UserRoles.Add(existing);
        }
        else
        {
            existing.ApprovalStatus = string.IsNullOrWhiteSpace(req.ApprovalStatus) ? "Approved" : req.ApprovalStatus;
            existing.ApprovedByUserId = adminId;
        }

        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(adminId, User.GetEmail(), "ADMIN_ROLE_UPDATED", "UserRole", existing.Id.ToString(),
            null, new { TargetUserId = id, req.RoleCode, existing.ApprovalStatus }, null, ct);

        return Ok(new { existing.Id, existing.UserId, roleCode = existing.RoleCode.ToString(), existing.ApprovalStatus });
    }

    [HttpGet("research-factors")]
    public async Task<IActionResult> ListAllFactors(CancellationToken ct)
    {
        var factors = await db.CarbonResearchFactors
            .Include(f => f.Source)
            .OrderBy(f => f.Code)
            .ThenByDescending(f => f.ValidFrom)
            .ToListAsync(ct);
        return Ok(factors);
    }

    [HttpPost("research-factors")]
    public async Task<IActionResult> CreateFactorVersion([FromBody] CreateResearchFactorVersionRequest req, CancellationToken ct)
    {
        if (req.ValueTCO2PerHa <= 0m)
            return BadRequest(new { message = "Giá trị hệ số phải > 0." });
        var sourceExists = await db.ResearchSources.AnyAsync(s => s.Id == req.SourceId, ct);
        if (!sourceExists) return BadRequest(new { message = "ResearchSource không tồn tại." });

        // Deactivate older active version of same (Variety, TreeAgeYears) WITHOUT modifying historical CarbonCalculations
        var previousActive = await db.CarbonResearchFactors
            .Where(f => f.Variety == req.Variety && f.TreeAgeYears == req.TreeAgeYears && f.IsActive)
            .ToListAsync(ct);

        foreach (var prev in previousActive)
        {
            prev.IsActive = false;
        }

        var adminEmail = User.GetEmail();
        var factor = new CarbonResearchFactor
        {
            Code = req.Code.Trim().ToUpperInvariant(),
            Variety = req.Variety,
            VarietyDisplayName = req.VarietyDisplayName.Trim(),
            TreeAgeYears = req.TreeAgeYears,
            ValueTCO2PerHa = req.ValueTCO2PerHa,
            Unit = req.Unit.Trim(),
            SourceId = req.SourceId,
            Version = req.Version.Trim(),
            ValidFrom = req.ValidFrom,
            ApprovedBy = adminEmail,
            ApplicabilityNotes = req.ApplicabilityNotes.Trim(),
            IsActive = true
        };

        db.CarbonResearchFactors.Add(factor);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(User.GetInternalUserId(), adminEmail, "RESEARCH_FACTOR_VERSION_CREATED",
            "CarbonResearchFactor", factor.Id.ToString(),
            previousActive.Select(p => new { p.Id, p.Version, p.ValueTCO2PerHa }),
            new { factor.Id, factor.Code, factor.Version, factor.ValueTCO2PerHa, factor.ValidFrom },
            null, ct);

        return CreatedAtAction(nameof(ListAllFactors), null, factor);
    }

    [HttpGet("review-queue")]
    public async Task<IActionResult> GetReviewQueue(CancellationToken ct)
    {
        var pendingAnalyses = await db.AiAnalyses
            .Include(a => a.File)
            .Where(a => a.ReviewStatus == ReviewDecisionStatus.PendingReview)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new
            {
                a.Id,
                a.FileId,
                fileName = a.File != null ? a.File.FileName : null,
                a.FarmId,
                a.ProjectId,
                a.ModelName,
                a.Status,
                a.IsFallbackResult,
                a.CreatedAt
            })
            .ToListAsync(ct);

        var pendingFarms = await db.Farms
            .Where(f => f.Status == FarmStatus.ReadyForReview || f.Status == FarmStatus.Submitted)
            .OrderByDescending(f => f.UpdatedAt)
            .Select(f => new { f.Id, f.Name, f.Province, f.Commune, f.AreaHa, f.Status, f.DataOriginTag })
            .ToListAsync(ct);

        return Ok(new { pendingAnalyses, pendingFarms });
    }

    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs([FromQuery] int limit = 100, CancellationToken ct = default)
    {
        var safeLimit = Math.Clamp(limit, 1, 500);
        var logs = await db.AuditLogs
            .OrderByDescending(l => l.CreatedAt)
            .Take(safeLimit)
            .ToListAsync(ct);
        return Ok(logs);
    }
}
