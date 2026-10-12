using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class PositionSizingServiceTests
{
    private readonly PositionSizingService _service;

    public PositionSizingServiceTests()
    {
        _service = new PositionSizingService(
            Options.Create(new RiskManagementOptions
            {
                RiskPerTradePercentage = 1.00m
            }));
    }

    [Fact]
    public void Calculate_ValidSetup_CalculatesPositionSize()
    {
        var setup = CreateTradeSetup();

        var result = _service.Calculate(setup, 10_000m);

        Assert.Equal(10_000m, result.AccountEquity);
        Assert.Equal(1.00m, result.RiskPercentage);
        Assert.Equal(100m, result.AccountRiskAmount);
        Assert.Equal(200m, result.RiskPerUnit);
        Assert.Equal(0.5m, result.Quantity);
        Assert.Equal(39_000m, result.PositionValue);
    }

    [Fact]
    public void Calculate_ZeroAccountEquity_Throws()
    {
        var setup = CreateTradeSetup();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.Calculate(setup, 0m));
    }

    [Fact]
    public void Calculate_NegativeAccountEquity_Throws()
    {
        var setup = CreateTradeSetup();

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.Calculate(setup, -1m));
    }

    [Fact]
    public void Calculate_ZeroRiskPerUnit_Throws()
    {
        var setup = CreateTradeSetup(riskPerUnit: 0m);

        Assert.Throws<InvalidOperationException>(
            () => _service.Calculate(setup, 10_000m));
    }

    [Fact]
    public void Calculate_ZeroRiskPercentage_Throws()
    {
        var service = new PositionSizingService(
            Options.Create(new RiskManagementOptions
            {
                RiskPerTradePercentage = 0m
            }));

        var setup = CreateTradeSetup();

        Assert.Throws<InvalidOperationException>(
            () => service.Calculate(setup, 10_000m));
    }

    [Fact]
    public void Calculate_RiskPercentageAboveOneHundred_Throws()
    {
        var service = new PositionSizingService(
            Options.Create(new RiskManagementOptions
            {
                RiskPerTradePercentage = 101m
            }));

        var setup = CreateTradeSetup();

        Assert.Throws<InvalidOperationException>(
            () => service.Calculate(setup, 10_000m));
    }

    private static TradeSetup CreateTradeSetup(decimal riskPerUnit = 200m)
    {
        return new TradeSetup
        {
            EntryConfirmation = null!,
            Direction = TradeDirection.Long,
            EntryPrice = 78_000m,
            StopLossPrice = 77_800m,
            TakeProfitPrice = 78_400m,
            RiskPerUnit = riskPerUnit,
            RewardPerUnit = 400m,
            RiskRewardRatio = 2m,
            CreatedAt = new DateTime(
                2026,
                9,
                10,
                10,
                0,
                0,
                DateTimeKind.Utc),
            Reasons = Array.Empty<string>()
        };
    }
}