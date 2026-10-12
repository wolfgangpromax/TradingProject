using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class TradeSetupServiceTests
{
    private readonly TradeSetupService _service;

    public TradeSetupServiceTests()
    {
        var options = Options.Create(new TradeSetupOptions
        {
            StopLossAtrBufferMultiplier = 0.10m,
            MinimumRiskRewardRatio = 2.00m
        });

        _service = new TradeSetupService(options);
    }

    [Fact]
    public void Create_BullishConfirmedEntry_CreatesLongTradeSetup()
    {
        var confirmation = CreateEntryConfirmation(FairValueGapType.Bullish, 120m);

        var context = new TradeSetupContext
        {
            EntryConfirmation = confirmation,
            AverageTrueRange = 50m
        };

        var result = _service.Create(context);

        Assert.NotNull(result);
        Assert.Equal(TradeDirection.Long, result.Direction);
        Assert.Equal(120m, result.EntryPrice);
        Assert.Equal(95m, result.StopLossPrice);
        Assert.Equal(170m, result.TakeProfitPrice);
        Assert.Equal(25m, result.RiskPerUnit);
        Assert.Equal(50m, result.RewardPerUnit);
        Assert.Equal(2m, result.RiskRewardRatio);
    }

    [Fact]
    public void Create_BearishConfirmedEntry_CreatesShortTradeSetup()
    {
        var confirmation = CreateEntryConfirmation(FairValueGapType.Bearish, 100m);

        var context = new TradeSetupContext
        {
            EntryConfirmation = confirmation,
            AverageTrueRange = 50m
        };

        var result = _service.Create(context);

        Assert.NotNull(result);
        Assert.Equal(TradeDirection.Short, result.Direction);
        Assert.Equal(100m, result.EntryPrice);
        Assert.Equal(125m, result.StopLossPrice);
        Assert.Equal(50m, result.TakeProfitPrice);
        Assert.Equal(25m, result.RiskPerUnit);
        Assert.Equal(50m, result.RewardPerUnit);
        Assert.Equal(2m, result.RiskRewardRatio);
    }

    [Fact]
    public void Create_WaitingEntryConfirmation_ReturnsNull()
    {
        var confirmation = CreateEntryConfirmation(FairValueGapType.Bullish, 120m);

        confirmation = new FairValueGapEntryConfirmation
        {
            Interaction = confirmation.Interaction,
            Status = EntryConfirmationStatus.Waiting,
            ConfirmedAt = null,
            ConfirmationPrice = null,
            Reasons = new[] { "Waiting for confirmation." }
        };

        var context = new TradeSetupContext
        {
            EntryConfirmation = confirmation,
            AverageTrueRange = 50m
        };

        var result = _service.Create(context);

        Assert.Null(result);
    }

    [Fact]
    public void Create_InvalidatedEntryConfirmation_ReturnsNull()
    {
        var confirmation = CreateEntryConfirmation(FairValueGapType.Bullish, 120m);

        confirmation = new FairValueGapEntryConfirmation
        {
            Interaction = confirmation.Interaction,
            Status = EntryConfirmationStatus.Invalidated,
            ConfirmedAt = null,
            ConfirmationPrice = null,
            Reasons = new[] { "Entry setup invalidated." }
        };

        var context = new TradeSetupContext
        {
            EntryConfirmation = confirmation,
            AverageTrueRange = 50m
        };

        var result = _service.Create(context);

        Assert.Null(result);
    }

    [Fact]
    public void Create_MissingAverageTrueRange_ReturnsNull()
    {
        var confirmation = CreateEntryConfirmation(FairValueGapType.Bullish, 120m);

        var context = new TradeSetupContext
        {
            EntryConfirmation = confirmation,
            AverageTrueRange = null
        };

        var result = _service.Create(context);

        Assert.Null(result);
    }

    [Fact]
    public void Create_ZeroAverageTrueRange_ReturnsNull()
    {
        var confirmation = CreateEntryConfirmation(FairValueGapType.Bullish, 120m);

        var context = new TradeSetupContext
        {
            EntryConfirmation = confirmation,
            AverageTrueRange = 0m
        };

        var result = _service.Create(context);

        Assert.Null(result);
    }

    private static FairValueGapEntryConfirmation CreateEntryConfirmation(FairValueGapType type, decimal confirmationPrice)
    {
        var gap = new FairValueGap
        {
            Type = type,
            StartTime = new DateTime(2026, 1, 1, 10, 0, 0),
            ConfirmedAt = new DateTime(2026, 1, 1, 10, 15, 0),
            LowerPrice = 100m,
            UpperPrice = 120m,
            GapSize = 20m
        };

        var state = new FairValueGapState(gap);

        var qualification = new FairValueGapQualification
        {
            State = state,
            IsQualified = true,
            RejectionReasons = Array.Empty<string>()
        };

        var alignment = new FairValueGapAlignment
        {
            State = state,
            MarketTrend = type == FairValueGapType.Bullish
                ? MarketTrend.Bullish
                : MarketTrend.Bearish,
            IsAligned = true,
            Reason = "FVG is aligned with market trend."
        };

        var candidate = new FairValueGapCandidate
        {
            State = state,
            Qualification = qualification,
            Alignment = alignment,
            IsCandidate = true,
            RejectionReasons = Array.Empty<string>()
        };

        var interaction = new FairValueGapInteractionState(candidate);

        interaction.RecordInteraction(
            new DateTime(2026, 1, 1, 10, 20, 0),
            110m);

        return new FairValueGapEntryConfirmation
        {
            Interaction = interaction,
            Status = EntryConfirmationStatus.Confirmed,
            ConfirmedAt = new DateTime(2026, 1, 1, 10, 25, 0),
            ConfirmationPrice = confirmationPrice,
            Reasons = new[] { "Entry confirmed." }
        };
    }
}