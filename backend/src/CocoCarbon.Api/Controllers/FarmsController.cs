using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using CocoCarbon.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

[ApiController]
[Route("/api/v1")]
[Authorize]
public sealed class FarmsController(
    CocoCarbonDbContext db,
    IPrivateFileStorageService storage,
    IAuditService audit) : ControllerBase
{
    [HttpGet("farms")]
    public async Task<IActionResult> ListFarms(CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        IQueryable<Farm> query = db.Farms
            .Include(f => f.OwnerUser)
            .Include(f => f.Cooperative);

        if (!User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
        {
            var managedCoopIds = await db.Cooperatives
                .Where(c => c.ManagerUserId == userId)
                .Select(c => c.Id)
                .ToListAsync(ct);

            var authorizedCoopFarmIds = await db.CooperativeMembers
                .Where(cm => managedCoopIds.Contains(cm.CooperativeId) && cm.IsDataAuthorized)
                .Select(cm => cm.FarmId)
                .Where(id => id != null)
                .Select(id => id!.Value)
                .ToListAsync(ct);

            var developerProjectFarmIds = await db.ProjectFarms
                .Where(pf => pf.HasFarmerConsent && pf.Project != null && pf.Project.DeveloperId == userId)
                .Select(pf => pf.FarmId)
                .ToListAsync(ct);

            query = query.Where(f =>
                f.OwnerUserId == userId ||
                authorizedCoopFarmIds.Contains(f.Id) ||
                developerProjectFarmIds.Contains(f.Id));
        }

        var farms = await query
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.OwnerUserId,
                ownerName = f.OwnerUser != null ? f.OwnerUser.DisplayName : "",
                f.CooperativeId,
                cooperativeName = f.Cooperative != null ? f.Cooperative.Name : null,
                f.Name,
                f.Province,
                f.Commune,
                f.HistoricalRegion,
                f.AreaHa,
                f.TreeCount,
                f.Variety,
                f.AgeYears,
                f.Latitude,
                f.Longitude,
                f.Status,
                f.DataOriginTag,
                f.HasLocationConsent,
                f.HasPhotoConsent,
                f.CreatedAt,
                f.UpdatedAt
            })
            .ToListAsync(ct);

        return Ok(farms);
    }

    [HttpPost("farms")]
    public async Task<IActionResult> CreateFarm([FromBody] CreateFarmRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Commune))
            return BadRequest(new { message = "Tên vườn và xã/phường không được để trống." });
        if (req.AreaHa <= 0m)
            return BadRequest(new { message = "Diện tích (AreaHa) phải lớn hơn 0." });
        if (req.TreeCount <= 0)
            return BadRequest(new { message = "Số cây (TreeCount) phải là số nguyên dương." });
        if (req.AgeYears <= 0 || req.AgeYears > 120)
            return BadRequest(new { message = "Tuổi cây phải nằm trong khoảng 1-120 năm." });

        var userId = User.GetInternalUserId();
        var farm = new Farm
        {
            OwnerUserId = userId,
            CooperativeId = req.CooperativeId,
            Name = req.Name.Trim(),
            Province = string.IsNullOrWhiteSpace(req.Province) ? ScientificConstants.CurrentProvinceDefault : req.Province.Trim(),
            Commune = req.Commune.Trim(),
            HistoricalRegion = string.IsNullOrWhiteSpace(req.HistoricalRegion) ? ScientificConstants.HistoricalRegionBenTre : req.HistoricalRegion.Trim(),
            AreaHa = req.AreaHa,
            TreeCount = req.TreeCount,
            Variety = req.Variety,
            AgeYears = req.AgeYears,
            Latitude = req.HasLocationConsent ? req.Latitude : null,
            Longitude = req.HasLocationConsent ? req.Longitude : null,
            LocationGeoJson = req.HasLocationConsent ? req.LocationGeoJson : null,
            CareHistoryJson = string.IsNullOrWhiteSpace(req.CareHistoryJson) ? "{}" : req.CareHistoryJson,
            HasLocationConsent = req.HasLocationConsent,
            HasPhotoConsent = req.HasPhotoConsent,
            Status = FarmStatus.Submitted,
            DataOriginTag = DataOriginTag.UserDeclared
        };

        if (!string.IsNullOrWhiteSpace(req.BoundaryPolygonGeoJson) && req.HasLocationConsent)
        {
            farm.Plots.Add(new FarmPlot
            {
                PolygonGeoJson = req.BoundaryPolygonGeoJson,
                AreaHa = req.AreaHa,
                BoundarySource = "UserDeclared"
            });
        }

        if (req.CooperativeId.HasValue)
        {
            var coopExists = await db.Cooperatives.AnyAsync(c => c.Id == req.CooperativeId.Value, ct);
            if (!coopExists) return BadRequest(new { message = "Hợp tác xã không tồn tại." });
        }

        db.Farms.Add(farm);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "FARM_CREATED", "Farm", farm.Id.ToString(),
            null, new { farm.Name, farm.AreaHa, farm.TreeCount, farm.Variety, farm.AgeYears }, null, ct);

        return CreatedAtAction(nameof(GetFarm), new { id = farm.Id }, new
        {
            farm.Id,
            farm.Name,
            farm.Province,
            farm.Commune,
            farm.HistoricalRegion,
            farm.AreaHa,
            farm.TreeCount,
            farm.Variety,
            farm.AgeYears,
            farm.Status,
            farm.DataOriginTag,
            farm.CreatedAt
        });
    }

    [HttpGet("farms/{id:guid}")]
    public async Task<IActionResult> GetFarm(Guid id, CancellationToken ct)
    {
        if (!await CanAccessFarmAsync(id, ct))
            return Forbid();

        var farm = await db.Farms
            .Include(f => f.OwnerUser)
            .Include(f => f.Cooperative)
            .Include(f => f.Plots)
            .Include(f => f.Observations)
            .Include(f => f.Files)
            .Include(f => f.Calculations)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (farm is null) return NotFound();

        return Ok(new
        {
            farm.Id,
            farm.OwnerUserId,
            ownerName = farm.OwnerUser?.DisplayName,
            farm.CooperativeId,
            cooperativeName = farm.Cooperative?.Name,
            farm.Name,
            farm.Province,
            farm.Commune,
            farm.HistoricalRegion,
            farm.AreaHa,
            farm.TreeCount,
            farm.Variety,
            farm.AgeYears,
            farm.Latitude,
            farm.Longitude,
            farm.LocationGeoJson,
            farm.CareHistoryJson,
            farm.Status,
            farm.DataOriginTag,
            farm.HasLocationConsent,
            farm.HasPhotoConsent,
            farm.CreatedAt,
            farm.UpdatedAt,
            plots = farm.Plots.Select(p => new { p.Id, p.PolygonGeoJson, p.AreaHa, p.LandUse, p.BoundarySource }),
            observations = farm.Observations.OrderByDescending(o => o.ObservedAt).Select(o => new
            {
                o.Id, o.ObservedAt, o.TreeAgeYears, o.DbhCm, o.HeightM, o.HealthStatus, o.PracticeDataJson, o.DataOriginTag
            }),
            files = farm.Files.OrderByDescending(fi => fi.CreatedAt).Select(fi => new
            {
                fi.Id, fi.Category, fi.FileName, fi.MimeType, fi.SizeBytes, fi.Sha256Hash, fi.Version, fi.Visibility, fi.DataOriginTag, fi.CreatedAt
            }),
            calculations = farm.Calculations.OrderByDescending(c => c.CreatedAt).Select(c => new
            {
                c.Id, c.EstimatedCO2EquivalentT, c.ObservedTreeDensityPerHa, c.FactorVersion, c.FormulaVersion, c.SourceCitation, c.Disclaimer, c.CreatedAt
            })
        });
    }

    [HttpPatch("farms/{id:guid}")]
    public async Task<IActionResult> UpdateFarm(Guid id, [FromBody] UpdateFarmRequest req, CancellationToken ct)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (farm is null) return NotFound();
        if (!CanModifyFarm(farm)) return Forbid();

        var before = new { farm.Name, farm.AreaHa, farm.TreeCount, farm.Variety, farm.AgeYears, farm.Status };

        if (!string.IsNullOrWhiteSpace(req.Name)) farm.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.Commune)) farm.Commune = req.Commune.Trim();
        if (req.AreaHa.HasValue)
        {
            if (req.AreaHa.Value <= 0m) return BadRequest(new { message = "Diện tích phải > 0." });
            farm.AreaHa = req.AreaHa.Value;
        }
        if (req.TreeCount.HasValue)
        {
            if (req.TreeCount.Value <= 0) return BadRequest(new { message = "Số cây phải > 0." });
            farm.TreeCount = req.TreeCount.Value;
        }
        if (req.Variety.HasValue) farm.Variety = req.Variety.Value;
        if (req.AgeYears.HasValue)
        {
            if (req.AgeYears.Value <= 0) return BadRequest(new { message = "Tuổi cây phải > 0." });
            farm.AgeYears = req.AgeYears.Value;
        }
        if (req.Latitude.HasValue && farm.HasLocationConsent) farm.Latitude = req.Latitude;
        if (req.Longitude.HasValue && farm.HasLocationConsent) farm.Longitude = req.Longitude;
        if (!string.IsNullOrWhiteSpace(req.CareHistoryJson)) farm.CareHistoryJson = req.CareHistoryJson;
        if (req.Status.HasValue) farm.Status = req.Status.Value;
        farm.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(User.GetInternalUserId(), User.GetEmail(), "FARM_UPDATED", "Farm", farm.Id.ToString(),
            before, new { farm.Name, farm.AreaHa, farm.TreeCount, farm.Variety, farm.AgeYears, farm.Status }, null, ct);

        return Ok(new { farm.Id, farm.Name, farm.AreaHa, farm.TreeCount, farm.Variety, farm.AgeYears, farm.Status, farm.UpdatedAt });
    }

    [HttpDelete("farms/{id:guid}")]
    public async Task<IActionResult> DeleteFarm(Guid id, CancellationToken ct)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (farm is null) return NotFound();
        if (!CanModifyFarm(farm)) return Forbid();

        db.Farms.Remove(farm);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(User.GetInternalUserId(), User.GetEmail(), "FARM_DELETED", "Farm", id.ToString(),
            new { farm.Name }, null, null, ct);

        return NoContent();
    }

    [HttpPost("farms/{id:guid}/observations")]
    public async Task<IActionResult> AddObservation(Guid id, [FromBody] CreateObservationRequest req, CancellationToken ct)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (farm is null) return NotFound();
        if (!CanModifyFarm(farm)) return Forbid();

        var obs = new FarmObservation
        {
            FarmId = id,
            TreeAgeYears = req.TreeAgeYears,
            DbhCm = req.DbhCm,
            HeightM = req.HeightM,
            HealthStatus = string.IsNullOrWhiteSpace(req.HealthStatus) ? "Healthy" : req.HealthStatus.Trim(),
            PracticeDataJson = string.IsNullOrWhiteSpace(req.PracticeDataJson) ? "{}" : req.PracticeDataJson,
            RecordedByUserId = User.GetInternalUserId(),
            DataOriginTag = DataOriginTag.UserDeclared
        };

        db.FarmObservations.Add(obs);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetFarm), new { id }, new
        {
            obs.Id,
            obs.FarmId,
            obs.ObservedAt,
            obs.TreeAgeYears,
            obs.DbhCm,
            obs.HeightM,
            obs.HealthStatus,
            obs.PracticeDataJson,
            obs.DataOriginTag
        });
    }

    [HttpGet("farms/{id:guid}/files")]
    public async Task<IActionResult> ListFarmFiles(Guid id, CancellationToken ct)
    {
        if (!await CanAccessFarmAsync(id, ct)) return Forbid();

        var files = await db.FarmFiles
            .Where(f => f.FarmId == id)
            .OrderByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id, f.FarmId, f.Category, f.FileName, f.MimeType, f.SizeBytes, f.Sha256Hash, f.Version, f.Visibility, f.DataOriginTag, f.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(files);
    }

    [HttpPost("farms/{id:guid}/files")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> UploadFarmFile(
        Guid id,
        IFormFile? file,
        [FromForm] FileCategory category = FileCategory.LandDocument,
        [FromForm] FileVisibility visibility = FileVisibility.Private,
        CancellationToken ct = default)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == id, ct);
        if (farm is null) return NotFound();
        if (!CanModifyFarm(farm)) return Forbid();

        if (file is null || file.Length == 0)
            return BadRequest(new { message = "Vui lòng chọn tệp hợp lệ (PDF, JPEG, PNG)." });

        var userId = User.GetInternalUserId();
        StoredFileMetadata stored;
        try
        {
            await using var stream = file.OpenReadStream();
            stored = await storage.SaveAsync(stream, file.FileName, file.ContentType, ct);
        }
        catch (FileValidationException ex)
        {
            return BadRequest(new { errorCode = ex.ErrorCode, message = ex.Message });
        }

        var prevVersion = await db.FarmFiles
            .Where(f => f.FarmId == id && f.FileName == stored.SafeFileName)
            .MaxAsync(f => (int?)f.Version, ct) ?? 0;

        var duplicateExists = await db.FarmFiles
            .AnyAsync(f => f.Sha256Hash == stored.Sha256Hash, ct);

        var entity = new FarmFile
        {
            FarmId = id,
            UploadedByUserId = userId,
            Category = category,
            FileKey = stored.FileKey,
            FileName = stored.SafeFileName,
            MimeType = stored.DetectedMimeType,
            SizeBytes = stored.SizeBytes,
            Sha256Hash = stored.Sha256Hash,
            Version = prevVersion + 1,
            Visibility = visibility,
            DataOriginTag = DataOriginTag.UserDeclared
        };

        db.FarmFiles.Add(entity);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "FARM_FILE_UPLOADED", "FarmFile", entity.Id.ToString(),
            null, new { entity.FileName, entity.MimeType, entity.SizeBytes, entity.Sha256Hash, entity.Version, duplicateExists }, null, ct);

        return CreatedAtAction(nameof(ListFarmFiles), new { id }, new
        {
            entity.Id,
            entity.FarmId,
            entity.Category,
            entity.FileName,
            entity.MimeType,
            entity.SizeBytes,
            entity.Sha256Hash,
            entity.Version,
            entity.Visibility,
            duplicateHashWarning = duplicateExists,
            entity.CreatedAt
        });
    }

    [HttpGet("files/{fileId:guid}/signed-url")]
    public async Task<IActionResult> GetSignedDownloadUrl(Guid fileId, CancellationToken ct)
    {
        var file = await db.FarmFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file is null) return NotFound();
        if (!await CanAccessFarmAsync(file.FarmId, ct)) return Forbid();

        var userId = User.GetInternalUserId();
        var expires = DateTimeOffset.UtcNow.AddMinutes(5).ToUnixTimeSeconds();
        var sig = storage.CreateSignedDownloadSignature(file.Id, userId, expires);

        return Ok(new
        {
            fileId = file.Id,
            expires,
            downloadUrl = $"/api/v1/files/{file.Id}/download?uid={userId}&expires={expires}&sig={sig}"
        });
    }

    [HttpGet("files/{fileId:guid}/download")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadPrivateFile(
        Guid fileId,
        [FromQuery] Guid? uid,
        [FromQuery] long? expires,
        [FromQuery] string? sig,
        CancellationToken ct)
    {
        var file = await db.FarmFiles.FirstOrDefaultAsync(f => f.Id == fileId, ct);
        if (file is null) return NotFound();

        var authorized = false;
        if (User.Identity?.IsAuthenticated == true)
        {
            authorized = await CanAccessFarmAsync(file.FarmId, ct);
        }
        else if (uid.HasValue && expires.HasValue && !string.IsNullOrWhiteSpace(sig))
        {
            authorized = storage.ValidateSignedDownloadSignature(fileId, uid.Value, expires.Value, sig);
        }

        if (!authorized) return Unauthorized(new { message = "Liên kết tải xuống không hợp lệ hoặc đã hết hạn." });

        var stream = await storage.OpenReadAsync(file.FileKey, ct);
        return File(stream, file.MimeType, file.FileName);
    }

    private bool CanModifyFarm(Farm farm)
    {
        var userId = User.GetInternalUserId();
        return farm.OwnerUserId == userId || User.HasApprovedRole(RoleCode.Admin);
    }

    private async Task<bool> CanAccessFarmAsync(Guid farmId, CancellationToken ct)
    {
        var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == farmId, ct);
        if (farm is null) return false;

        if (User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
            return true;

        var userId = User.GetInternalUserId();
        if (farm.OwnerUserId == userId)
            return true;

        if (farm.CooperativeId.HasValue)
        {
            var isCoopMgr = await db.Cooperatives.AnyAsync(c => c.Id == farm.CooperativeId && c.ManagerUserId == userId, ct);
            var isAuthorized = await db.CooperativeMembers.AnyAsync(cm =>
                cm.CooperativeId == farm.CooperativeId && cm.FarmId == farmId && cm.IsDataAuthorized, ct);
            if (isCoopMgr && isAuthorized) return true;
        }

        var isDevAuthorized = await db.ProjectFarms.AnyAsync(pf =>
            pf.FarmId == farmId && pf.HasFarmerConsent && pf.Project != null && pf.Project.DeveloperId == userId, ct);
        return isDevAuthorized;
    }
}
