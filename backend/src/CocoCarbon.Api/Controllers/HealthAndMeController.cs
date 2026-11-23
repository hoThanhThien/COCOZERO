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
public sealed class HealthAndMeController(CocoCarbonDbContext db, IAuditService audit) : ControllerBase
{
    [HttpGet("/health")]
    [HttpGet("/api/v1/health")]
    [AllowAnonymous]
    public async Task<IActionResult> Health(CancellationToken ct)
    {
        var canConnect = await db.Database.CanConnectAsync(ct);
        var factorCount = canConnect ? await db.CarbonResearchFactors.CountAsync(f => f.IsActive, ct) : 0;
        return Ok(new
        {
            status = canConnect ? "healthy" : "degraded",
            service = "CocoCarbon.Api",
            activeResearchFactors = factorCount,
            timestamp = DateTimeOffset.UtcNow
        });
    }

    [HttpGet("/api/v1/me")]
    [Authorize]
    public async Task<IActionResult> GetMe(CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var user = await db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ConsentRecords)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null) return NotFound();

        return Ok(new
        {
            id = user.Id,
            firebaseUid = user.FirebaseUid,
            email = user.Email,
            displayName = user.DisplayName,
            phone = user.Phone,
            status = user.Status,
            roles = user.UserRoles.Select(r => new
            {
                roleCode = r.RoleCode.ToString(),
                approvalStatus = r.ApprovalStatus,
                scopeType = r.ScopeType,
                scopeId = r.ScopeId
            }),
            consents = user.ConsentRecords.Select(c => new
            {
                c.ConsentType,
                c.Granted,
                c.PolicyVersion,
                c.RecordedAt
            }),
            createdAt = user.CreatedAt
        });
    }

    [HttpPut("/api/v1/me/profile")]
    [Authorize]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.DisplayName))
        {
            return BadRequest(new { message = "DisplayName không được để trống." });
        }

        var userId = User.GetInternalUserId();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return NotFound();

        var before = new { user.DisplayName, user.Phone };
        user.DisplayName = req.DisplayName.Trim();
        user.Phone = req.Phone?.Trim();
        user.UpdatedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(user.Id, user.Email, "USER_PROFILE_UPDATED", "User", user.Id.ToString(),
            before, new { user.DisplayName, user.Phone }, null, ct);

        return Ok(new { user.Id, user.DisplayName, user.Phone, user.UpdatedAt });
    }

    [HttpPost("/api/v1/me/consents")]
    [Authorize]
    public async Task<IActionResult> RecordConsent([FromBody] RecordConsentRequest req, CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var record = new ConsentRecord
        {
            UserId = userId,
            ConsentType = req.ConsentType,
            Granted = req.Granted,
            PolicyVersion = string.IsNullOrWhiteSpace(req.PolicyVersion) ? "v1.0-2026-11" : req.PolicyVersion.Trim(),
            TextShown = req.TextShown.Trim(),
            RecordedAt = DateTimeOffset.UtcNow
        };
        db.ConsentRecords.Add(record);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "CONSENT_RECORDED", "ConsentRecord", record.Id.ToString(),
            null, new { record.ConsentType, record.Granted, record.PolicyVersion }, null, ct);

        return CreatedAtAction(nameof(GetMe), null, new
        {
            record.Id,
            record.ConsentType,
            record.Granted,
            record.PolicyVersion,
            record.RecordedAt
        });
    }

    [HttpPost("/api/v1/me/role-requests")]
    [Authorize]
    public async Task<IActionResult> RequestRoleUpgrade([FromBody] RequestRoleUpgradeRequest req, CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Code == req.RequestedRole, ct);
        if (role is null) return BadRequest(new { message = "Vai trò không tồn tại." });

        var existing = await db.UserRoles.FirstOrDefaultAsync(
            ur => ur.UserId == userId && ur.RoleCode == req.RequestedRole && ur.ScopeType == "Global", ct);
        if (existing is not null)
        {
            return Ok(new
            {
                roleCode = existing.RoleCode.ToString(),
                approvalStatus = existing.ApprovalStatus,
                message = "Yêu cầu vai trò đã tồn tại; vai trò nâng cao cần Admin phê duyệt."
            });
        }

        // Security rule: NEVER auto-approve role elevations from client input.
        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = role.Id,
            RoleCode = req.RequestedRole,
            ScopeType = "Global",
            ApprovalStatus = "PendingApproval"
        };
        db.UserRoles.Add(userRole);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "ROLE_UPGRADE_REQUESTED", "UserRole", userRole.Id.ToString(),
            null, new { req.RequestedRole, ApprovalStatus = "PendingApproval", req.Justification }, null, ct);

        return Accepted(new
        {
            roleCode = userRole.RoleCode.ToString(),
            approvalStatus = userRole.ApprovalStatus,
            message = "Đã ghi nhận yêu cầu nâng quyền ở trạng thái PendingApproval. Chỉ Admin được phê duyệt."
        });
    }
}
