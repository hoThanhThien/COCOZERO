using CocoCarbon.Domain.Constants;
using CocoCarbon.Domain.Entities;
using CocoCarbon.Domain.Enums;
using CocoCarbon.Domain.Services;
using Xunit;

namespace CocoCarbon.UnitTests;

public class CoconutCarbonCalculatorTests
{
    private readonly CoconutCarbonCalculator _calculator = new();

    private static List<CarbonResearchFactor> SeedCtuFactors() =>
    [
        new CarbonResearchFactor
        {
            Id = Guid.NewGuid(),
            Code = "CTU2015-TALL-4Y",
            Variety = CoconutVariety.TallDuaTa,
            VarietyDisplayName = "Dừa cao - Dừa Ta (Tall)",
            TreeAgeYears = 4,
            ValueTCO2PerHa = 24.5180m,
            Version = "CTU-2015-v1.0",
            ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsActive = true
        },
        new CarbonResearchFactor
        {
            Id = Guid.NewGuid(),
            Code = "CTU2015-TALL-10Y",
            Variety = CoconutVariety.TallDuaTa,
            VarietyDisplayName = "Dừa cao - Dừa Ta (Tall)",
            TreeAgeYears = 10,
            ValueTCO2PerHa = 75.2436m,
            Version = "CTU-2015-v1.0",
            ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsActive = true
        },
        new CarbonResearchFactor
        {
            Id = Guid.NewGuid(),
            Code = "CTU2015-SHORT-4Y",
            Variety = CoconutVariety.ShortXiemXanh,
            VarietyDisplayName = "Dừa thấp - Xiêm xanh (Short)",
            TreeAgeYears = 4,
            ValueTCO2PerHa = 20.4583m,
            Version = "CTU-2015-v1.0",
            ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsActive = true
        },
        new CarbonResearchFactor
        {
            Id = Guid.NewGuid(),
            Code = "CTU2015-SHORT-10Y",
            Variety = CoconutVariety.ShortXiemXanh,
            VarietyDisplayName = "Dừa thấp - Xiêm xanh (Short)",
            TreeAgeYears = 10,
            ValueTCO2PerHa = 69.9189m,
            Version = "CTU-2015-v1.0",
            ValidFrom = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsActive = true
        }
    ];

    [Theory]
    [InlineData(CoconutVariety.TallDuaTa, 4, 1.0, 24.5180)]
    [InlineData(CoconutVariety.TallDuaTa, 10, 1.0, 75.2436)]
    [InlineData(CoconutVariety.TallDuaTa, 10, 2.0, 150.4872)]
    [InlineData(CoconutVariety.ShortXiemXanh, 4, 1.0, 20.4583)]
    [InlineData(CoconutVariety.ShortXiemXanh, 10, 1.0, 69.9189)]
    public void Calculate_AllFourCtuFactors_ReturnsExactReferenceValues(
        CoconutVariety variety, int age, decimal areaHa, decimal expectedCo2)
    {
        var result = _calculator.Calculate(
            new CalculatorInput(areaHa, (int)(200 * areaHa), variety, age),
            SeedCtuFactors());

        Assert.True(result.IsSupported);
        Assert.Equal(expectedCo2, result.EstimatedCO2EquivalentT);
        Assert.Equal(ScientificConstants.FormulaVersion, result.FormulaVersion);
        Assert.Null(result.OptionalArithmeticAllocationKgPerTree);
        Assert.Contains("không phải lượng hấp thụ CO2 hằng năm", result.Disclaimer);
        Assert.Contains("không phải tín chỉ carbon", result.Disclaimer);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.5)]
    public void Calculate_ZeroOrNegativeArea_IsRejected(decimal areaHa)
    {
        var result = _calculator.Calculate(
            new CalculatorInput(areaHa, 200, CoconutVariety.TallDuaTa, 10),
            SeedCtuFactors());

        Assert.False(result.IsSupported);
        Assert.Equal("INVALID_AREA", result.ErrorCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Calculate_ZeroOrNegativeTreeCount_IsRejected(int treeCount)
    {
        var result = _calculator.Calculate(
            new CalculatorInput(1.0m, treeCount, CoconutVariety.TallDuaTa, 10),
            SeedCtuFactors());

        Assert.False(result.IsSupported);
        Assert.Equal("INVALID_TREE_COUNT", result.ErrorCode);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(15)]
    public void Calculate_UnsupportedAge_RejectsWithoutInterpolation(int ageYears)
    {
        var result = _calculator.Calculate(
            new CalculatorInput(1.0m, 200, CoconutVariety.TallDuaTa, ageYears),
            SeedCtuFactors());

        Assert.False(result.IsSupported);
        Assert.Equal("UNSUPPORTED_AGE_CLASS", result.ErrorCode);
        Assert.Equal(0m, result.EstimatedCO2EquivalentT);
    }

    [Fact]
    public void Calculate_UnsupportedVariety_IsRejected()
    {
        var result = _calculator.Calculate(
            new CalculatorInput(1.0m, 200, CoconutVariety.OtherUnsupported, 10),
            SeedCtuFactors());

        Assert.False(result.IsSupported);
        Assert.Equal("UNSUPPORTED_VARIETY", result.ErrorCode);
    }

    [Fact]
    public void Calculate_PrecisionAndRounding_IsDeterministicFourDecimals()
    {
        var result = _calculator.Calculate(
            new CalculatorInput(0.3333m, 67, CoconutVariety.TallDuaTa, 10),
            SeedCtuFactors());

        Assert.True(result.IsSupported);
        // 0.3333 * 75.2436 = 25.07869188 -> 25.0787
        Assert.Equal(25.0787m, result.EstimatedCO2EquivalentT);
        Assert.Equal(201.02m, result.ObservedTreeDensityPerHa);
        Assert.True(result.IsDensityWithinBenTre2024SurveyRange);
    }

    [Fact]
    public void Calculate_DensityOutside2024SurveyRange_DoesNotRejectOrAlterFactor()
    {
        var result = _calculator.Calculate(
            new CalculatorInput(1.0m, 300, CoconutVariety.TallDuaTa, 10),
            SeedCtuFactors());

        Assert.True(result.IsSupported);
        Assert.Equal(75.2436m, result.EstimatedCO2EquivalentT);
        Assert.Equal(300.00m, result.ObservedTreeDensityPerHa);
        Assert.False(result.IsDensityWithinBenTre2024SurveyRange);
        Assert.Contains("ngoài khoảng quan sát", result.DensityContextNote);
    }

    [Fact]
    public void Calculate_OptionalPerTreeAllocation_HasExplicitWarningWhenEnabled()
    {
        var result = _calculator.Calculate(
            new CalculatorInput(1.0m, 200, CoconutVariety.TallDuaTa, 10, IncludeArithmeticPerTreeAllocation: true),
            SeedCtuFactors());

        Assert.True(result.IsSupported);
        Assert.Equal(376.2180m, result.OptionalArithmeticAllocationKgPerTree);
        Assert.Equal(ScientificConstants.ArithmeticAllocationLabel, result.ArithmeticAllocationWarning);
    }

    [Fact]
    public void Calculate_FactorVersioning_UsesLatestActiveVersionOnly()
    {
        var factors = SeedCtuFactors();
        factors.First(f => f.Code == "CTU2015-TALL-10Y").IsActive = false;
        factors.Add(new CarbonResearchFactor
        {
            Id = Guid.NewGuid(),
            Code = "CTU2015-TALL-10Y",
            Variety = CoconutVariety.TallDuaTa,
            VarietyDisplayName = "Dừa cao - Dừa Ta (Tall)",
            TreeAgeYears = 10,
            ValueTCO2PerHa = 78.0000m,
            Version = "CTU-2015-v2.0",
            ValidFrom = new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero),
            IsActive = true
        });

        var result = _calculator.Calculate(
            new CalculatorInput(1.0m, 200, CoconutVariety.TallDuaTa, 10),
            factors);

        Assert.True(result.IsSupported);
        Assert.Equal("CTU-2015-v2.0", result.FactorUsed!.Version);
        Assert.Equal(78.0000m, result.EstimatedCO2EquivalentT);
    }
}
