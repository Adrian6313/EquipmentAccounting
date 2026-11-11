using System;
using System.Collections.Generic;
using EquipmentAccounting.Core.Diagnostics;
using Xunit;

namespace EquipmentAccounting.Tests.PracticalWork10_11;

/// <summary>
/// Набор модульных тестов для проверки алгоритмов отладки и обработки исключений (ПР №10-11, Вариант 4).
/// </summary>
public sealed class DiagnosticsTests
{
    private readonly Variant4DiagnosticsService _service = new();

    [Fact]
    public void CalculateVariant4Series_WithSmallN_CalculatesExactValues()
    {
        // N = 1: S = 1/1 = 1.0
        var (steps1, sum1) = _service.CalculateVariant4Series(1);
        Assert.Single(steps1);
        Assert.Equal(1.0, sum1, precision: 6);

        // N = 2: S = 1 - 1/4 = 0.75
        var (steps2, sum2) = _service.CalculateVariant4Series(2);
        Assert.Equal(2, steps2.Count);
        Assert.Equal(0.75, sum2, precision: 6);

        // N = 5: S = 1 - 1/4 + 1/9 - 1/16 + 1/25 = 0.8386111111...
        var (steps5, sum5) = _service.CalculateVariant4Series(5);
        Assert.Equal(5, steps5.Count);
        double expected5 = 1.0 - (1.0 / 4.0) + (1.0 / 9.0) - (1.0 / 16.0) + (1.0 / 25.0);
        Assert.Equal(expected5, sum5, precision: 8);
    }

    [Fact]
    public void CalculateVariant4Series_WithLargeN_ApproachesTheoreticalLimit()
    {
        // Ряд сходится к π² / 12 ≈ 0.822467
        var (_, sum) = _service.CalculateVariant4Series(10_000);
        Assert.True(Math.Abs(sum - Variant4DiagnosticsService.TheoreticalLimit) < 0.0002,
            $"Сумма {sum} недостаточно близка к пределу {Variant4DiagnosticsService.TheoreticalLimit}");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(1_000_001)]
    public void CalculateVariant4Series_WithInvalidN_ThrowsArgumentOutOfRangeException(int invalidN)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.CalculateVariant4Series(invalidN));
    }

    [Fact]
    public void CalculateDepreciation_WithValidParameters_ReturnsCorrectSchedule()
    {
        var steps = _service.CalculateDepreciation(initialCost: 100_000m, years: 5, salvageValue: 10_000m, annualHours: 2000);

        Assert.Equal(5, steps.Count);
        Assert.Equal(1, steps[0].Year);
        Assert.Equal(5, steps[^1].Year);
        Assert.True(steps[^1].EndCost >= 10_000m);
        Assert.All(steps, step => Assert.True(step.CostPerHour >= 0m));
    }

    [Fact]
    public void CalculateDepreciation_WithZeroAnnualHours_ThrowsDivideByZeroException()
    {
        Assert.Throws<DivideByZeroException>(() =>
            _service.CalculateDepreciation(initialCost: 50_000m, years: 3, salvageValue: 5_000m, annualHours: 0));
    }

    [Fact]
    public void CalculateDepreciation_WithSalvageGreaterThanCost_ThrowsCustomException()
    {
        var ex = Assert.Throws<EquipmentDiagnosticsException>(() =>
            _service.CalculateDepreciation(initialCost: 50_000m, years: 3, salvageValue: 70_000m, annualHours: 1000));

        Assert.Equal("ERR_SALVAGE_INVALID", ex.DiagnosticCode);
    }

    [Fact]
    public void SimulateError_ThrowsExactExpectedExceptions()
    {
        Assert.Throws<DivideByZeroException>(() => _service.SimulateError("divide_by_zero"));
        Assert.Throws<FormatException>(() => _service.SimulateError("format"));
        Assert.Throws<ArgumentOutOfRangeException>(() => _service.SimulateError("range"));
        Assert.Throws<OverflowException>(() => _service.SimulateError("overflow"));
        Assert.Throws<EquipmentDiagnosticsException>(() => _service.SimulateError("custom"));
    }

    [Fact]
    public void ExecuteDiagnosticsWorkflow_AlwaysExecutesFinallyBlock()
    {
        var successLog = new List<string>();
        bool successResult = _service.ExecuteDiagnosticsWorkflow("10", "series", successLog);
        Assert.True(successResult);
        Assert.Contains(successLog, line => line.Contains("[finally]"));

        var errorLog = new List<string>();
        bool errorResult = _service.ExecuteDiagnosticsWorkflow("not_a_number", "series", errorLog);
        Assert.False(errorResult);
        Assert.Contains(errorLog, line => line.Contains("[catch FormatException]"));
        Assert.Contains(errorLog, line => line.Contains("[finally]"));
    }
}
