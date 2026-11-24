using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Entities;

public enum FinancialEntryType
{
    CareExpense = 1,
    MrvPreparationExpense = 2,
    CoconutHarvestRevenue = 3,
    ByProductRevenue = 4,
    GreenSupportGrantOrLoan = 5
}

/// <summary>
/// User-declared farm cashflow & green-practice budgeting record (NOT carbon credit trading or payout).
/// </summary>
public class FarmFinancialEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid FarmId { get; set; }
    public Farm? Farm { get; set; }
    public Guid RecordedByUserId { get; set; }
    public FinancialEntryType EntryType { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal AmountVnd { get; set; }
    public DateTimeOffset EntryDate { get; set; } = DateTimeOffset.UtcNow;
    public string Notes { get; set; } = string.Empty;
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class DataDeletionRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public User? User { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Requested";
    public DateTimeOffset RequestedAt { get; set; } = DateTimeOffset.UtcNow;
}
