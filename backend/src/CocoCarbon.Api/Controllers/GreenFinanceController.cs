using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.Persistence;
using CocoCarbon.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

public sealed record CreateFinanceEntryRequest(
    FinancialEntryType EntryType,
    string Category,
    decimal AmountVnd,
    string? Notes,
    DateTimeOffset? EntryDate);

[ApiController]
[Route("/api/v1")]
[Authorize]
public sealed class GreenFinanceController(CocoCarbonDbContext db, IAuditService audit) : ControllerBase
{
    [HttpGet("farms/{farmId:guid}/finance-ledger")]
    public async Task<IActionResult> GetFarmLedger(Guid farmId, CancellationToken ct)
    {
        var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == farmId, ct);
        if (farm is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (farm.OwnerUserId != userId && !User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
            return Forbid();

        var entries = await db.FarmFinancialEntries
            .Where(e => e.FarmId == farmId)
            .OrderByDescending(e => e.EntryDate)
            .Select(e => new
            {
                e.Id,
                e.FarmId,
                e.EntryType,
                e.Category,
                e.AmountVnd,
                e.EntryDate,
                e.Notes,
                e.DataOriginTag
            })
            .ToListAsync(ct);

        var totalExpense = entries
            .Where(e => e.EntryType is FinancialEntryType.CareExpense or FinancialEntryType.MrvPreparationExpense)
            .Sum(e => e.AmountVnd);
        var totalAgriRevenue = entries
            .Where(e => e.EntryType is FinancialEntryType.CoconutHarvestRevenue or FinancialEntryType.ByProductRevenue or FinancialEntryType.GreenSupportGrantOrLoan)
            .Sum(e => e.AmountVnd);

        bool IsExpense(FinancialEntryType t) =>
            t is FinancialEntryType.CareExpense or FinancialEntryType.MrvPreparationExpense;

        List<object> AggregateBy(Func<DateTimeOffset, string> keySelector) =>
            entries
                .GroupBy(e => keySelector(e.EntryDate))
                .OrderByDescending(g => g.Key)
                .Select(g =>
                {
                    var exp = g.Where(x => IsExpense(x.EntryType)).Sum(x => x.AmountVnd);
                    var rev = g.Where(x => !IsExpense(x.EntryType)).Sum(x => x.AmountVnd);
                    return (object)new
                    {
                        period = g.Key,
                        expenseVnd = exp,
                        revenueVnd = rev,
                        netCashflowVnd = rev - exp,
                        entryCount = g.Count()
                    };
                })
                .ToList();

        var aggregations = new
        {
            byMonth = AggregateBy(d => $"{d.UtcDateTime:yyyy-MM}"),
            byQuarter = AggregateBy(d => $"{d.UtcDateTime.Year}-Q{((d.UtcDateTime.Month - 1) / 3) + 1}"),
            byYear = AggregateBy(d => $"{d.UtcDateTime:yyyy}")
        };

        return Ok(new
        {
            farmId,
            farmName = farm.Name,
            summary = new
            {
                totalDeclaredExpenseVnd = totalExpense,
                totalDeclaredAgriAndSupportVnd = totalAgriRevenue,
                netDeclaredFarmCashflowVnd = totalAgriRevenue - totalExpense,
                scientificBoundaryNotice =
                    "Sổ thu chi phản ánh dòng tiền nông sản/phụ phẩm và chi phí canh tác do người dùng khai báo (USER_DECLARED). " +
                    "Không phải định giá tín chỉ carbon hay doanh thu carbon được bảo đảm."
            },
            aggregations,
            entries
        });
    }

    [HttpPost("farms/{farmId:guid}/finance-ledger")]
    public async Task<IActionResult> AddFinanceEntry(
        Guid farmId,
        [FromBody] CreateFinanceEntryRequest req,
        CancellationToken ct)
    {
        var farm = await db.Farms.FirstOrDefaultAsync(f => f.Id == farmId, ct);
        if (farm is null) return NotFound();

        var userId = User.GetInternalUserId();
        if (farm.OwnerUserId != userId && !User.HasApprovedRole(RoleCode.Admin))
            return Forbid();

        if (string.IsNullOrWhiteSpace(req.Category))
            return BadRequest(new { message = "Hạng mục thu/chi không được để trống." });
        if (req.AmountVnd <= 0m)
            return BadRequest(new { message = "Số tiền (VND) phải lớn hơn 0." });

        var entry = new FarmFinancialEntry
        {
            FarmId = farmId,
            RecordedByUserId = userId,
            EntryType = req.EntryType,
            Category = req.Category.Trim(),
            AmountVnd = decimal.Round(req.AmountVnd, 2, MidpointRounding.AwayFromZero),
            EntryDate = req.EntryDate ?? DateTimeOffset.UtcNow,
            Notes = req.Notes?.Trim() ?? "",
            DataOriginTag = DataOriginTag.UserDeclared
        };

        db.FarmFinancialEntries.Add(entry);
        await db.SaveChangesAsync(ct);

        await audit.RecordAsync(userId, User.GetEmail(), "FARM_FINANCE_ENTRY_CREATED", "FarmFinancialEntry", entry.Id.ToString(),
            null, new { entry.FarmId, entry.EntryType, entry.AmountVnd }, null, ct);

        return CreatedAtAction(nameof(GetFarmLedger), new { farmId }, new
        {
            entry.Id,
            entry.FarmId,
            entry.EntryType,
            entry.Category,
            entry.AmountVnd,
            entry.EntryDate,
            entry.Notes,
            entry.DataOriginTag
        });
    }
}
