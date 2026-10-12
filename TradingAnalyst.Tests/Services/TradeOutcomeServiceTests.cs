using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class TradeOutcomeServiceTests
{
    private readonly TradeOutcomeService _service = new();

    [Fact]
    public void Evaluate_LongTakeProfitHit_ReturnsWon()
    {
        var setup = CreateSetup(TradeDirection.Long, 100m, 90m, 120m);
        var candle = CreateCandle(105m, 121m, 100m, 120m, setup.CreatedAt.AddMinutes(5));

        var result = _service.Evaluate(setup, candle);

        Assert.Equal(TradeOutcomeStatus.Won, result.Status);
        Assert.Equal(120m, result.ExitPrice);
        Assert.Equal(2m, result.RealizedRiskRewardRatio);
    }

    [Fact]
    public void Evaluate_LongStopLossHit_ReturnsLost()
    {
        var setup = CreateSetup(TradeDirection.Long, 100m, 90m, 120m);
        var candle = CreateCandle(100m, 105m, 89m, 92m, setup.CreatedAt.AddMinutes(5));

        var result = _service.Evaluate(setup, candle);

        Assert.Equal(TradeOutcomeStatus.Lost, result.Status);
        Assert.Equal(90m, result.ExitPrice);
        Assert.Equal(-1m, result.RealizedRiskRewardRatio);
    }

    [Fact]
    public void Evaluate_ShortTakeProfitHit_ReturnsWon()
    {
        var setup = CreateSetup(TradeDirection.Short, 100m, 110m, 80m);
        var candle = CreateCandle(95m, 100m, 79m, 82m, setup.CreatedAt.AddMinutes(5));

        var result = _service.Evaluate(setup, candle);

        Assert.Equal(TradeOutcomeStatus.Won, result.Status);
        Assert.Equal(80m, result.ExitPrice);
        Assert.Equal(2m, result.RealizedRiskRewardRatio);
    }

    [Fact]
    public void Evaluate_ShortStopLossHit_ReturnsLost()
    {
        var setup = CreateSetup(TradeDirection.Short, 100m, 110m, 80m);
        var candle = CreateCandle(100m, 111m, 95m, 108m, setup.CreatedAt.AddMinutes(5));

        var result = _service.Evaluate(setup, candle);

        Assert.Equal(TradeOutcomeStatus.Lost, result.Status);
        Assert.Equal(110m, result.ExitPrice);
        Assert.Equal(-1m, result.RealizedRiskRewardRatio);
    }

    [Fact]
    public void Evaluate_BothStopLossAndTakeProfitHit_ReturnsLost()
    {
        var setup = CreateSetup(TradeDirection.Long, 100m, 90m, 120m);
        var candle = CreateCandle(100m, 121m, 89m, 110m, setup.CreatedAt.AddMinutes(5));

        var result = _service.Evaluate(setup, candle);

        Assert.Equal(TradeOutcomeStatus.Lost, result.Status);
        Assert.Equal(90m, result.ExitPrice);
        Assert.Contains("Both stop loss and take profit", result.Reasons.Single());
    }

    [Fact]
    public void Evaluate_SetupCreationCandle_ReturnsActive()
    {
        var setup = CreateSetup(TradeDirection.Long, 100m, 90m, 120m);
        var candle = CreateCandle(100m, 130m, 80m, 110m, setup.CreatedAt);

        var result = _service.Evaluate(setup, candle);

        Assert.Equal(TradeOutcomeStatus.Active, result.Status);
        Assert.Null(result.ClosedAt);
        Assert.Null(result.ExitPrice);
        Assert.Null(result.RealizedRiskRewardRatio);
    }

    [Fact]
    public void Update_WonTrade_CannotLaterBecomeLost()
    {
        var setup = CreateSetup(TradeDirection.Long, 100m, 90m, 120m);
        var state = new TradeOutcomeState(setup);

        _service.Update(state, CreateCandle(100m, 121m, 95m, 120m, setup.CreatedAt.AddMinutes(5)));
        _service.Update(state, CreateCandle(100m, 105m, 80m, 85m, setup.CreatedAt.AddMinutes(10)));

        Assert.Equal(TradeOutcomeStatus.Won, state.Status);
        Assert.Equal(120m, state.ExitPrice);
        Assert.Equal(2m, state.RealizedRiskRewardRatio);
    }

    private static TradeSetup CreateSetup(
        TradeDirection direction,
        decimal entryPrice,
        decimal stopLossPrice,
        decimal takeProfitPrice)
    {
        var confirmation = CreateEntryConfirmation(
            direction == TradeDirection.Long
                ? FairValueGapType.Bullish
                : FairValueGapType.Bearish,
            entryPrice);

        var risk = direction == TradeDirection.Long
            ? entryPrice - stopLossPrice
            : stopLossPrice - entryPrice;

        var reward = direction == TradeDirection.Long
            ? takeProfitPrice - entryPrice
            : entryPrice - takeProfitPrice;

        return new TradeSetup
        {
            EntryConfirmation = confirmation,
            Direction = direction,
            EntryPrice = entryPrice,
            StopLossPrice = stopLossPrice,
            TakeProfitPrice = takeProfitPrice,
            RiskPerUnit = risk,
            RewardPerUnit = reward,
            RiskRewardRatio = reward / risk,
            CreatedAt = confirmation.ConfirmedAt!.Value,
            Reasons = new[] { "Test setup." }
        };
    }

    private static FairValueGapEntryConfirmation CreateEntryConfirmation(
        FairValueGapType type,
        decimal confirmationPrice)
    {
        var gap = new FairValueGap
        {
            Type = type,
            StartTime = new DateTime(2026, 1, 1, 10, 0, 0),
            ConfirmedAt = new DateTime(2026, 1, 1, 10, 15, 0),
            LowerPrice = 95m,
            UpperPrice = 105m,
            GapSize = 10m
        };

        var gapState = new FairValueGapState(gap);

        var qualification = new FairValueGapQualification
        {
            State = gapState,
            IsQualified = true,
            RejectionReasons = Array.Empty<string>()
        };

        var alignment = new FairValueGapAlignment
        {
            State = gapState,
            MarketTrend = type == FairValueGapType.Bullish ? MarketTrend.Bullish : MarketTrend.Bearish,
            IsAligned = true,
            Reason = "Test alignment."
        };

        var candidate = new FairValueGapCandidate
        {
            State = gapState,
            Qualification = qualification,
            Alignment = alignment,
            IsCandidate = true,
            RejectionReasons = Array.Empty<string>()
        };

        var interaction = new FairValueGapInteractionState(candidate);
        interaction.RecordInteraction(new DateTime(2026, 1, 1, 10, 20, 0), 100m);

        return new FairValueGapEntryConfirmation
        {
            Interaction = interaction,
            Status = EntryConfirmationStatus.Confirmed,
            ConfirmedAt = new DateTime(2026, 1, 1, 10, 25, 0),
            ConfirmationPrice = confirmationPrice,
            Reasons = new[] { "Test confirmation." }
        };
    }

    private static Candle CreateCandle(
        decimal open,
        decimal high,
        decimal low,
        decimal close,
        DateTime closeTime)
    {
        return new Candle
        {
            Open = open,
            High = high,
            Low = low,
            Close = close,
            OpenTime = closeTime.AddMinutes(-5),
            CloseTime = closeTime,
            IsClosed = true
        };
    }
}