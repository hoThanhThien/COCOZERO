using System.Text.Json;
using System.Text.Json.Nodes;
using CocoCarbon.Api.Contracts;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Infrastructure.AiClient;
using CocoCarbon.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CocoCarbon.Api.Controllers;

[ApiController]
[Authorize]
public sealed class FinancialAndIntegrityController(
    CocoCarbonDbContext db,
    IPythonAiServiceClient aiClient) : ControllerBase
{
    [HttpPost("/api/ai/assistant")]
    public async Task<IActionResult> AiAssistant([FromBody] JsonElement body, CancellationToken ct) =>
        await ForwardAsync("api/ai/assistant", body, ct);

    [HttpPost("/api/financial/calculate")]
    public async Task<IActionResult> Calculate([FromBody] JsonElement body, [FromQuery] Guid? farmId, CancellationToken ct) =>
        await ForwardWithActualsAsync("api/financial/calculate", body, farmId, isNestedCalculationInput: false, ct);

    [HttpPost("/api/financial/scenarios")]
    public async Task<IActionResult> Scenarios([FromBody] JsonElement body, CancellationToken ct) =>
        await ForwardAsync("api/financial/scenarios", body, ct);

    [HttpPost("/api/financial/analyze")]
    public async Task<IActionResult> Analyze([FromBody] JsonElement body, [FromQuery] Guid? farmId, CancellationToken ct) =>
        await ForwardWithActualsAsync("api/financial/analyze", body, farmId, isNestedCalculationInput: true, ct);

    [HttpPost("/api/financial/reports")]
    public async Task<IActionResult> Reports([FromBody] JsonElement body, [FromQuery] Guid? farmId, CancellationToken ct) =>
        await ForwardWithActualsAsync("api/financial/reports", body, farmId, isNestedCalculationInput: true, ct);

    [HttpPost("/api/financial/parse-transaction")]
    public async Task<IActionResult> ParseTransaction([FromBody] JsonElement body, CancellationToken ct) =>
        await ForwardAsync("api/financial/parse-transaction", body, ct);

    [HttpPost("/api/carbon/integrity-check")]
    public async Task<IActionResult> IntegrityCheck([FromBody] JsonElement body, CancellationToken ct) =>
        await ForwardAsync("api/carbon/integrity-check", body, ct);

    [HttpPost("/api/carbon/proposal-check")]
    public async Task<IActionResult> ProposalCheck([FromBody] JsonElement body, CancellationToken ct) =>
        await ForwardAsync("api/carbon/proposal-check", body, ct);

    private async Task<IActionResult> ForwardAsync(string path, JsonElement body, CancellationToken ct)
    {
        var (status, respBody) = await aiClient.ForwardJsonAsync(path, body.GetRawText(), ct);
        return new ContentResult
        {
            StatusCode = status,
            ContentType = "application/json",
            Content = respBody
        };
    }

    private async Task<IActionResult> ForwardWithActualsAsync(
        string path,
        JsonElement body,
        Guid? farmId,
        bool isNestedCalculationInput,
        CancellationToken ct)
    {
        var rawJson = body.GetRawText();
        if (farmId.HasValue)
        {
            var userId = User.GetInternalUserId();
            var farm = await db.Farms.AsNoTracking().FirstOrDefaultAsync(f => f.Id == farmId.Value, ct);
            if (farm is null) return NotFound(new { message = "Vườn dừa không tồn tại." });
            if (farm.OwnerUserId != userId && !User.HasApprovedRole(RoleCode.Admin, RoleCode.Reviewer))
                return Forbid();

            var entries = await db.FarmFinancialEntries
                .AsNoTracking()
                .Where(e => e.FarmId == farmId.Value)
                .ToListAsync(ct);

            var actualExp = entries
                .Where(e => e.EntryType is FinancialEntryType.CareExpense or FinancialEntryType.MrvPreparationExpense)
                .Sum(e => e.AmountVnd);
            var actualRev = entries
                .Where(e => e.EntryType is FinancialEntryType.CoconutHarvestRevenue or FinancialEntryType.ByProductRevenue or FinancialEntryType.GreenSupportGrantOrLoan)
                .Sum(e => e.AmountVnd);

            var root = JsonNode.Parse(rawJson)?.AsObject();
            if (root is not null)
            {
                var target = isNestedCalculationInput ? root["calculationInput"]?.AsObject() : root;
                if (target is not null)
                {
                    target["actualRecordedExpenseVnd"] ??= actualExp;
                    target["actualRecordedRevenueVnd"] ??= actualRev;
                    var serializedEntries = new JsonArray();
                    foreach (var e in entries.OrderByDescending(x => x.EntryDate))
                    {
                        serializedEntries.Add(new JsonObject
                        {
                            ["entryType"] = e.EntryType.ToString(),
                            ["category"] = e.Category,
                            ["amountVnd"] = e.AmountVnd,
                            ["entryDate"] = e.EntryDate.ToString("O"),
                        });
                    }
                    target["actualEntries"] ??= serializedEntries;
                }
                rawJson = root.ToJsonString();
            }
        }

        var (status, respBody) = await aiClient.ForwardJsonAsync(path, rawJson, ct);
        return new ContentResult
        {
            StatusCode = status,
            ContentType = "application/json",
            Content = respBody
        };
    }
}
