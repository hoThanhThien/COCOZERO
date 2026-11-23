using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Entities;

public class ResearchSource
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Authors { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Publication { get; set; } = string.Empty;
    public string ReferenceUrl { get; set; } = string.Empty;
    public string StudyLocation { get; set; } = string.Empty;
    public string SampleDesignSummary { get; set; } = string.Empty;
    public string Limitations { get; set; } = string.Empty;
}

public class CarbonResearchFactor
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public CoconutVariety Variety { get; set; }
    public string VarietyDisplayName { get; set; } = string.Empty;
    public int TreeAgeYears { get; set; }
    public decimal ValueTCO2PerHa { get; set; }
    public string Unit { get; set; } = "tCO2/ha (reference stock at studied age class)";
    public Guid SourceId { get; set; }
    public ResearchSource? Source { get; set; }
    public string Version { get; set; } = ScientificConstants.DefaultFactorVersion;
    public DateTimeOffset ValidFrom { get; set; } = DateTimeOffset.UtcNow;
    public string ApprovedBy { get; set; } = "CocoCarbon Scientific Committee";
    public string ApplicabilityNotes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CarbonCalculation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? FarmId { get; set; }
    public Farm? Farm { get; set; }
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public string InputSnapshotJson { get; set; } = "{}";
    public Guid FactorId { get; set; }
    public CarbonResearchFactor? Factor { get; set; }
    public string FactorVersion { get; set; } = string.Empty;
    public string FormulaVersion { get; set; } = ScientificConstants.FormulaVersion;

    public decimal EstimatedCO2EquivalentT { get; set; }
    public decimal ObservedTreeDensityPerHa { get; set; }
    public string DensityContextNote { get; set; } = string.Empty;

    public decimal? OptionalArithmeticAllocationKgPerTree { get; set; }
    public string? ArithmeticAllocationLabel { get; set; }

    public string SourceCitation { get; set; } = string.Empty;
    public string Disclaimer { get; set; } = ScientificConstants.MandatoryCalculatorDisclaimer;
    public DataOriginTag DataOriginTag { get; set; } = DataOriginTag.UserDeclared;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
