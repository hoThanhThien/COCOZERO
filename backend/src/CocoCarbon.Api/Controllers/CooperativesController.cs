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
[Route("/api/v1/cooperatives")]
[Authorize]
public sealed class CooperativesController(CocoCarbonDbContext db, IAuditService audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListCooperatives(CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        IQueryable<Cooperative> query = db.Cooperatives;

        if (!User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
        {
            var memberCoopIds = await db.CooperativeMembers
                .Where(m => m.UserId == userId)
                .Select(m => m.CooperativeId)
                .ToListAsync(ct);

            query = query.Where(c => c.ManagerUserId == userId || memberCoopIds.Contains(c.Id));
        }

        var list = await query
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Code,
                c.Province,
                c.Commune,
                c.HistoricalRegion,
                c.ManagerUserId,
                c.Status,
                c.DataOriginTag,
                c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(list);
    }

    [HttpPost]
    [Authorize(Roles = "CooperativeManager,Admin")]
    public async Task<IActionResult> CreateCooperative([FromBody] CreateCooperativeRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Code))
            return BadRequest(new { message = "Tên và mã hợp tác xã là bắt buộc." });

        var userId = User.GetInternalUserId();
        var coop = new Cooperative
        {
            Name = req.Name.Trim(),
            Code = req.Code.Trim().ToUpperInvariant(),
            Province = string.IsNullOrWhiteSpace(req.Province) ? ScientificConstants.CurrentProvinceDefault : req.Province.Trim(),
            Commune = req.Commune.Trim(),
            HistoricalRegion = string.IsNullOrWhiteSpace(req.HistoricalRegion) ? ScientificConstants.HistoricalRegionBenTre : req.HistoricalRegion.Trim(),
            ManagerUserId = userId,
            DataOriginTag = DataOriginTag.UserDeclared
        };

        db.Cooperatives.Add(coop);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "COOPERATIVE_CREATED", "Cooperative", coop.Id.ToString(),
            null, new { coop.Name, coop.Code }, null, ct);

        return CreatedAtAction(nameof(GetDashboard), new { id = coop.Id }, coop);
    }

    [HttpPost("{id:guid}/farms")]
    [Authorize(Roles = "CooperativeManager,Admin")]
    public async Task<IActionResult> AddFarmToCooperative(Guid id, [FromBody] AddCooperativeFarmRequest req, CancellationToken ct)
    {
        var coop = await db.Cooperatives.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (coop is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (coop.ManagerUserId != userId && !User.HasApprovedRole(RoleCode.Admin))
            return Forbid();

        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == req.FarmId, ct);
        if (farm is null) return NotFound(new { message = "Không tìm thấy vườn dừa." });

        var existing = await db.CooperativeMembers
            .FirstOrDefaultAsync(m => m.CooperativeId == id && m.FarmId == req.FarmId, ct);
        if (existing is null)
        {
            existing = new CooperativeMember
            {
                CooperativeId = id,
                UserId = farm.OwnerUserId,
                FarmId = farm.Id,
                IsDataAuthorized = req.IsDataAuthorized,
                ParticipationStatus = ParticipationStatus.Active
            };
            db.CooperativeMembers.Add(existing);
        }
        else
        {
            existing.IsDataAuthorized = req.IsDataAuthorized;
        }

        farm.CooperativeId = id;
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "COOPERATIVE_FARM_LINKED", "CooperativeMember", existing.Id.ToString(),
            null, new { id, req.FarmId, req.IsDataAuthorized }, null, ct);

        return Ok(new { existing.Id, existing.CooperativeId, existing.FarmId, existing.IsDataAuthorized });
    }

    [HttpGet("{id:guid}/dashboard")]
    public async Task<IActionResult> GetDashboard(Guid id, CancellationToken ct)
    {
        var coop = await db.Cooperatives.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id, ct);
        if (coop is null) return NotFound();

        var userId = User.GetInternalUserId();
        var isAuthorizedManager = coop.ManagerUserId == userId || User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer);
        if (!isAuthorizedManager) return Forbid();

        // Only include farms with explicit IsDataAuthorized == true
        var authorizedMembers = await db.CooperativeMembers
            .Include(m => m.Farm)
            .Include(m => m.User)
            .Where(m => m.CooperativeId == id && m.IsDataAuthorized && m.Farm != null)
            .ToListAsync(ct);

        var farms = authorizedMembers.Select(m => m.Farm!).DistinctBy(f => f.Id).ToList();
        var totalAreaHa = farms.Sum(f => f.AreaHa);
        var totalTrees = farms.Sum(f => f.TreeCount);

        return Ok(new
        {
            cooperative = new
            {
                coop.Id,
                coop.Name,
                coop.Code,
                coop.Province,
                coop.Commune,
                coop.HistoricalRegion,
                coop.DataOriginTag
            },
            summary = new
            {
                authorizedMemberCount = authorizedMembers.Select(m => m.UserId).Distinct().Count(),
                authorizedFarmCount = farms.Count,
                totalDeclaredAreaHa = totalAreaHa,
                totalDeclaredTrees = totalTrees,
                farmsReadyForReview = farms.Count(f => f.Status == FarmStatus.ReadyForReview),
                farmsNeedingRevision = farms.Count(f => f.Status == FarmStatus.NeedsRevision),
                scientificNotice = "Tổng diện tích là dữ liệu khai báo do hộ thành viên ủy quyền; KHÔNG tự động coi là dự án carbon đủ điều kiện hoặc tín chỉ đã phát hành."
            },
            farms = farms.Select(f => new
            {
                f.Id,
                f.Name,
                f.Commune,
                f.AreaHa,
                f.TreeCount,
                f.Variety,
                f.AgeYears,
                f.Latitude,
                f.Longitude,
                f.Status,
                f.DataOriginTag
            })
        });
    }
}
