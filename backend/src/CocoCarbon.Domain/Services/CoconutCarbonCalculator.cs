using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;

namespace CocoCarbon.Domain.Services;

public sealed record CalculatorInput(
    decimal AreaHa,
    int TreeCount,
    CoconutVariety Variety,
    int TreeAgeYears,
    bool IncludeArithmeticPerTreeAllocation = false);

public sealed record CalculatorOutcome(
    bool IsSupported,
    string? ErrorCode,
    string? ErrorMessage,
    CarbonResearchFactor? FactorUsed,
    decimal EstimatedCO2EquivalentT,
    decimal ObservedTreeDensityPerHa,
    bool IsDensityWithinBenTre2024SurveyRange,
    string DensityContextNote,
    decimal? OptionalArithmeticAllocationKgPerTree,
    string? ArithmeticAllocationWarning,
    string FormulaVersion,
    string Disclaimer);

/// <summary>
/// Authoritative C# domain calculator using exclusively approved CTU 2015 reference factors.
/// Does NOT interpolate ages, does NOT apply double carbon conversions, and does NOT output annual rates or credits.
/// </summary>
public sealed class CoconutCarbonCalculator
{
    public CalculatorOutcome Calculate(
        CalculatorInput input,
        IReadOnlyCollection<CarbonResearchFactor> approvedFactors)
    {
        if (input.AreaHa <= 0m)
        {
            return Unsupported(
                "INVALID_AREA",
                "Diện tích vườn (AreaHa) phải lớn hơn 0 ha.");
        }

        if (input.TreeCount <= 0)
        {
            return Unsupported(
                "INVALID_TREE_COUNT",
                "Số lượng cây dừa (TreeCount) phải là số nguyên dương lớn hơn 0.");
        }

        if (input.Variety is not (CoconutVariety.TallDuaTa or CoconutVariety.ShortXiemXanh))
        {
            return Unsupported(
                "UNSUPPORTED_VARIETY",
                ScientificConstants.UnsupportedFactorMessage);
        }

        if (!ScientificConstants.SupportedAgeClasses.Contains(input.TreeAgeYears))
        {
            return Unsupported(
                "UNSUPPORTED_AGE_CLASS",
                ScientificConstants.UnsupportedFactorMessage);
        }

        var factor = approvedFactors
            .Where(f => f.IsActive && f.Variety == input.Variety && f.TreeAgeYears == input.TreeAgeYears)
            .OrderByDescending(f => f.ValidFrom)
            .ThenByDescending(f => f.Version)
            .FirstOrDefault();

        if (factor is null)
        {
            return Unsupported(
                "FACTOR_NOT_FOUND",
                ScientificConstants.UnsupportedFactorMessage);
        }

        // Direct area-based reference estimate using CTU 2015 factor (already reported as tCO2/ha).
        // Do NOT multiply by 0.47-0.50 or 44/12 again, and do NOT mix with IPCC 196 t/ha dry biomass.
        var estimatedCo2T = decimal.Round(
            input.AreaHa * factor.ValueTCO2PerHa,
            4,
            MidpointRounding.AwayFromZero);

        var observedDensity = decimal.Round(
            input.TreeCount / input.AreaHa,
            2,
            MidpointRounding.AwayFromZero);

        var withinSurveyRange =
            observedDensity >= ScientificConstants.BenTre2024SurveyMinDensity &&
            observedDensity <= ScientificConstants.BenTre2024SurveyMaxDensity;

        var densityContextNote = withinSurveyRange
            ? $"Mật độ quan sát ({observedDensity} cây/ha) nằm trong khoảng khảo sát canh tác Bến Tre 2024 " +
              $"({ScientificConstants.BenTre2024SurveyMinDensity:0}-{ScientificConstants.BenTre2024SurveyMaxDensity:0} cây/ha, mẫu {ScientificConstants.BenTre2024SurveyHouseholdCount} hộ; chỉ mang tính bối cảnh nông học)."
            : $"Mật độ quan sát ({observedDensity} cây/ha) ngoài khoảng quan sát của khảo sát tham khảo Bến Tre 2024 " +
              $"({ScientificConstants.BenTre2024SurveyMinDensity:0}-{ScientificConstants.BenTre2024SurveyMaxDensity:0} cây/ha, mẫu {ScientificConstants.BenTre2024SurveyHouseholdCount} hộ). " +
              "Thông tin này không làm thay đổi hệ số hay kết luận vườn sai/không đạt.";

        decimal? perTreeKg = null;
        string? perTreeLabel = null;
        if (input.IncludeArithmeticPerTreeAllocation)
        {
            perTreeKg = decimal.Round(
                (estimatedCo2T * 1000m) / input.TreeCount,
                4,
                MidpointRounding.AwayFromZero);
            perTreeLabel = ScientificConstants.ArithmeticAllocationLabel;
        }

        return new CalculatorOutcome(
            IsSupported: true,
            ErrorCode: null,
            ErrorMessage: null,
            FactorUsed: factor,
            EstimatedCO2EquivalentT: estimatedCo2T,
            ObservedTreeDensityPerHa: observedDensity,
            IsDensityWithinBenTre2024SurveyRange: withinSurveyRange,
            DensityContextNote: densityContextNote,
            OptionalArithmeticAllocationKgPerTree: perTreeKg,
            ArithmeticAllocationWarning: perTreeLabel,
            FormulaVersion: ScientificConstants.FormulaVersion,
            Disclaimer: ScientificConstants.MandatoryCalculatorDisclaimer);
    }

    private static CalculatorOutcome Unsupported(string errorCode, string message) =>
        new(
            IsSupported: false,
            ErrorCode: errorCode,
            ErrorMessage: message,
            FactorUsed: null,
            EstimatedCO2EquivalentT: 0m,
            ObservedTreeDensityPerHa: 0m,
            IsDensityWithinBenTre2024SurveyRange: false,
            DensityContextNote: string.Empty,
            OptionalArithmeticAllocationKgPerTree: null,
            ArithmeticAllocationWarning: null,
            FormulaVersion: ScientificConstants.FormulaVersion,
            Disclaimer: ScientificConstants.MandatoryCalculatorDisclaimer);
}
