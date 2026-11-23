using System.Text.Json;
using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Domain.Services;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

[ApiController]
[Route("/api/v1")]
public sealed class CarbonCalculatorController(
    CocoCarbonDbContext db,
    CoconutCarbonCalculator calculator,
    IAuditService audit) : ControllerBase
{
    [HttpGet("research-sources")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResearchSources(CancellationToken ct)
    {
        var sources = await db.ResearchSources
            .OrderBy(s => s.Code)
            .ToListAsync(ct);
        return Ok(sources);
    }

    [HttpGet("research-factors")]
    [AllowAnonymous]
    public async Task<IActionResult> GetResearchFactors(CancellationToken ct)
    {
        var factors = await db.CarbonResearchFactors
            .Include(f => f.Source)
            .Where(f => f.IsActive)
            .OrderBy(f => f.Variety)
            .ThenBy(f => f.TreeAgeYears)
            .Select(f => new
            {
                f.Id,
                f.Code,
                f.Variety,
                f.VarietyDisplayName,
                f.TreeAgeYears,
                f.ValueTCO2PerHa,
                f.Unit,
                f.Version,
                f.ValidFrom,
                f.ApprovedBy,
                f.ApplicabilityNotes,
                source = f.Source == null ? null : new
                {
                    f.Source.Code,
                    f.Source.Title,
                    f.Source.Authors,
                    f.Source.Year,
                    f.Source.Publication,
                    f.Source.Limitations
                }
            })
            .ToListAsync(ct);

        return Ok(new
        {
            formulaVersion = ScientificConstants.FormulaVersion,
            methodologyLimitations = ScientificConstants.MethodologyLimitationsSummary,
            mandatoryDisclaimer = ScientificConstants.MandatoryCalculatorDisclaimer,
            factors
        });
    }

    [HttpPost("carbon-calculator/estimate")]
    [Authorize]
    public async Task<IActionResult> Estimate([FromBody] CarbonEstimateRequest req, CancellationToken ct)
    {
        var userId = User.GetInternalUserId();

        if (req.FarmId.HasValue)
        {
            var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == req.FarmId.Value, ct);
            if (farm is null) return NotFound(new { message = "Vườn dừa không tồn tại." });
            if (farm.OwnerUserId != userId && !User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
                return Forbid();
        }

        var approvedFactors = await db.CarbonResearchFactors
            .Include(f => f.Source)
            .Where(f => f.IsActive)
            .ToListAsync(ct);

        var outcome = calculator.Calculate(
            new CalculatorInput(
                req.AreaHa,
                req.TreeCount,
                req.Variety,
                req.TreeAgeYears,
                req.IncludeArithmeticPerTreeAllocation),
            approvedFactors);

        if (!outcome.IsSupported || outcome.FactorUsed is null)
        {
            return UnprocessableEntity(new
            {
                errorCode = outcome.ErrorCode,
                message = outcome.ErrorMessage,
                supportedVarieties = new[] { "TallDuaTa", "ShortXiemXanh" },
                supportedAges = ScientificConstants.SupportedAgeClasses,
                disclaimer = ScientificConstants.MandatoryCalculatorDisclaimer
            });
        }

        var sourceCitation = outcome.FactorUsed.Source is not null
            ? $"{outcome.FactorUsed.Source.Code}: {outcome.FactorUsed.Source.Authors} ({outcome.FactorUsed.Source.Year}) - {outcome.FactorUsed.Source.Title}"
            : "CTU 2015 [R1]";

        var snapshot = JsonSerializer.Serialize(new
        {
            req.FarmId,
            req.AreaHa,
            req.TreeCount,
            variety = req.Variety.ToString(),
            req.TreeAgeYears,
            req.IncludeArithmeticPerTreeAllocation
        });

        var record = new CarbonCalculation
        {
            FarmId = req.FarmId,
            UserId = userId,
            FactorId = outcome.FactorUsed.Id,
            FactorVersion = outcome.FactorUsed.Version,
            FormulaVersion = outcome.FormulaVersion,
            InputSnapshotJson = snapshot,
            EstimatedCO2EquivalentT = outcome.EstimatedCO2EquivalentT,
            ObservedTreeDensityPerHa = outcome.ObservedTreeDensityPerHa,
            DensityContextNote = outcome.DensityContextNote,
            OptionalArithmeticAllocationKgPerTree = outcome.OptionalArithmeticAllocationKgPerTree,
            ArithmeticAllocationLabel = outcome.ArithmeticAllocationWarning,
            SourceCitation = sourceCitation,
            Disclaimer = outcome.Disclaimer,
            DataOriginTag = DataOriginTag.UserDeclared,
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.CarbonCalculations.Add(record);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "CARBON_ESTIMATE_CREATED", "CarbonCalculation", record.Id.ToString(),
            null, new { record.EstimatedCO2EquivalentT, record.FactorVersion, record.FormulaVersion, record.FarmId }, null, ct);

        return Ok(new
        {
            calculationId = record.Id,
            farmId = record.FarmId,
            resultLabel = "Giá trị CO2 tương đương tham khảo theo sinh khối và cấp tuổi trong nghiên cứu Giồng Trôm (2015)",
            estimatedCO2EquivalentT = record.EstimatedCO2EquivalentT,
            unit = "tCO2 tương đương tham khảo (tổng theo diện tích khai báo, KHÔNG phải tCO2/năm, KHÔNG phải tín chỉ)",
            factorUsed = new
            {
                outcome.FactorUsed.Id,
                outcome.FactorUsed.Code,
                outcome.FactorUsed.VarietyDisplayName,
                outcome.FactorUsed.TreeAgeYears,
                outcome.FactorUsed.ValueTCO2PerHa,
                outcome.FactorUsed.Version
            },
            observedTreeDensityPerHa = record.ObservedTreeDensityPerHa,
            isDensityWithinBenTre2024SurveyRange = outcome.IsDensityWithinBenTre2024SurveyRange,
            densityContextNote = record.DensityContextNote,
            optionalArithmeticAllocationKgPerTree = record.OptionalArithmeticAllocationKgPerTree,
            arithmeticAllocationWarning = record.ArithmeticAllocationLabel,
            formulaVersion = record.FormulaVersion,
            sourceCitation = record.SourceCitation,
            limitations = ScientificConstants.MethodologyLimitationsSummary,
            disclaimer = record.Disclaimer,
            createdAt = record.CreatedAt
        });
    }

    [HttpGet("carbon-calculator/history")]
    [Authorize]
    public async Task<IActionResult> History([FromQuery] Guid? farmId, CancellationToken ct)
    {
        var userId = User.GetInternalUserId();
        var query = db.CarbonCalculations
            .Include(c => c.Factor)
            .AsQueryable();

        if (!User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
        {
            query = query.Where(c => c.UserId == userId);
        }

        if (farmId.HasValue)
        {
            query = query.Where(c => c.FarmId == farmId.Value);
        }

        var list = await query
            .OrderByDescending(c => c.CreatedAt)
            .Take(100)
            .Select(c => new
            {
                c.Id,
                c.FarmId,
                c.EstimatedCO2EquivalentT,
                c.ObservedTreeDensityPerHa,
                c.DensityContextNote,
                c.FactorVersion,
                factorCode = c.Factor != null ? c.Factor.Code : "",
                c.FormulaVersion,
                c.SourceCitation,
                c.Disclaimer,
                c.InputSnapshotJson,
                c.DataOriginTag,
                c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(list);
    }
}
