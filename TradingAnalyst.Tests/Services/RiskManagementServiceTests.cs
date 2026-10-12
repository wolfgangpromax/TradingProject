using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class RiskManagementServiceTests
{
    private readonly RiskManagementService _service;

    public RiskManagementServiceTests()
    {
        _service = new RiskManagementService(
            Options.Create(new RiskManagementOptions
            {
                RiskPerTradePercentage = 1.00m,
                MaximumDailyLossR = 3.00m,
                MaximumTradesPerDay = 5,
                MaximumActiveTrades = 1
            }));
    }

    [Fact]
    public void Evaluate_WithinRiskLimits_ApprovesTrade()
    {
        var context = CreateContext(
            tradesToday: 2,
            activeTrades: 0,
            dailyRealizedR: -2m);

        var result = _service.Evaluate(context);

        Assert.True(result.IsApproved);
        Assert.Empty(result.RejectionReasons);
        Assert.Equal(2, result.TradesToday);
        Assert.Equal(0, result.ActiveTrades);
        Assert.Equal(-2m, result.DailyRealizedR);
    }

    [Fact]
    public void Evaluate_MaximumTradesReached_RejectsTrade()
    {
        var context = CreateContext(
            tradesToday: 5,
            activeTrades: 0,
            dailyRealizedR: 0m);

        var result = _service.Evaluate(context);

        Assert.False(result.IsApproved);
        Assert.Single(result.RejectionReasons);
        Assert.Contains(
            "Maximum trades per day of 5 has been reached.",
            result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_MaximumDailyLossReached_RejectsTrade()
    {
        var context = CreateContext(
            tradesToday: 3,
            activeTrades: 0,
            dailyRealizedR: -3m);

        var result = _service.Evaluate(context);

        Assert.False(result.IsApproved);
        Assert.Single(result.RejectionReasons);
        Assert.Contains(
            "Maximum daily loss of 3.00R has been reached.",
            result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_DailyLossBeyondLimit_RejectsTrade()
    {
        var context = CreateContext(
            tradesToday: 3,
            activeTrades: 0,
            dailyRealizedR: -4m);

        var result = _service.Evaluate(context);

        Assert.False(result.IsApproved);
        Assert.Single(result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_MaximumActiveTradesReached_RejectsTrade()
    {
        var context = CreateContext(
            tradesToday: 1,
            activeTrades: 1,
            dailyRealizedR: 0m);

        var result = _service.Evaluate(context);

        Assert.False(result.IsApproved);
        Assert.Single(result.RejectionReasons);
        Assert.Contains(
            "Maximum active trades of 1 has been reached.",
            result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_MultipleLimitsReached_ReturnsAllRejectionReasons()
    {
        var context = CreateContext(
            tradesToday: 5,
            activeTrades: 1,
            dailyRealizedR: -3m);

        var result = _service.Evaluate(context);

        Assert.False(result.IsApproved);
        Assert.Equal(3, result.RejectionReasons.Count);
    }

    [Fact]
    public void Evaluate_NegativeTradesToday_Throws()
    {
        var context = CreateContext(
            tradesToday: -1,
            activeTrades: 0,
            dailyRealizedR: 0m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.Evaluate(context));
    }

    [Fact]
    public void Evaluate_NegativeActiveTrades_Throws()
    {
        var context = CreateContext(
            tradesToday: 0,
            activeTrades: -1,
            dailyRealizedR: 0m);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => _service.Evaluate(context));
    }

    [Fact]
    public void Evaluate_JustAboveDailyLossLimit_ApprovesTrade()
    {
        var context = CreateContext(
            tradesToday: 2,
            activeTrades: 0,
            dailyRealizedR: -2.99m);

        var result = _service.Evaluate(context);

        Assert.True(result.IsApproved);
        Assert.Empty(result.RejectionReasons);
    }

    private static TradeRiskContext CreateContext(
        int tradesToday,
        int activeTrades,
        decimal dailyRealizedR)
    {
        return new TradeRiskContext
        {
            PositionSize = CreatePositionSize(),
            TradesToday = tradesToday,
            ActiveTrades = activeTrades,
            DailyRealizedR = dailyRealizedR
        };
    }

    private static PositionSize CreatePositionSize()
    {
        var setup = new TradeSetup
        {
            EntryConfirmation = null!,
            Direction = TradeDirection.Long,
            EntryPrice = 78_000m,
            StopLossPrice = 77_800m,
            TakeProfitPrice = 78_400m,
            RiskPerUnit = 200m,
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

        return new PositionSize
        {
            Setup = setup,
            AccountEquity = 10_000m,
            RiskPercentage = 1m,
            AccountRiskAmount = 100m,
            RiskPerUnit = 200m,
            Quantity = 0.5m,
            PositionValue = 39_000m,
            Reasons = Array.Empty<string>()
        };
    }
}