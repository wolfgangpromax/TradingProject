using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapInteractionServiceTests
{
    private readonly FairValueGapInteractionService _service = new();

    [Fact]
    public void Evaluate_BullishCandidateFutureCandleEntersGap_ReturnsInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var candle =
            CreateCandle(
                minute: 20,
                high: 125m,
                low: 115m);

        var result =
            _service.Evaluate(
                candidate,
                candle);

        Assert.True(result.HasInteracted);

        Assert.Equal(
            candle.CloseTime,
            result.FirstInteractionTime);

        Assert.Equal(
            115m,
            result.InteractionPrice);
    }

    [Fact]
    public void Evaluate_BearishCandidateFutureCandleEntersGap_ReturnsInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bearish);

        var candle =
            CreateCandle(
                minute: 20,
                high: 105m,
                low: 95m);

        var result =
            _service.Evaluate(
                candidate,
                candle);

        Assert.True(result.HasInteracted);

        Assert.Equal(
            candle.CloseTime,
            result.FirstInteractionTime);

        Assert.Equal(
            105m,
            result.InteractionPrice);
    }

    [Fact]
    public void Evaluate_BullishCandidatePriceStaysAboveGap_ReturnsNoInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var candle =
            CreateCandle(
                minute: 20,
                high: 140m,
                low: 125m);

        var result =
            _service.Evaluate(
                candidate,
                candle);

        Assert.False(result.HasInteracted);
        Assert.Null(result.FirstInteractionTime);
        Assert.Null(result.InteractionPrice);
    }

    [Fact]
    public void Evaluate_ConfirmationCandle_ReturnsNoInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var gap =
            candidate.State.Gap;

        var candle =
            new Candle
            {
                OpenTime =
                    gap.ConfirmedAt.AddMinutes(-5),

                CloseTime =
                    gap.ConfirmedAt,

                Open = 120m,
                High = 125m,
                Low = 115m,
                Close = 122m,
                Volume = 100m,
                IsClosed = true
            };

        var result =
            _service.Evaluate(
                candidate,
                candle);

        Assert.False(result.HasInteracted);
        Assert.Null(result.FirstInteractionTime);
        Assert.Null(result.InteractionPrice);
    }

    [Fact]
    public void Evaluate_UnclosedCandle_ReturnsNoInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var candle =
            CreateCandle(
                minute: 20,
                high: 125m,
                low: 115m,
                isClosed: false);

        var result =
            _service.Evaluate(
                candidate,
                candle);

        Assert.False(result.HasInteracted);
    }

    [Fact]
    public void Evaluate_NonCandidate_ReturnsNoInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish,
                isCandidate: false);

        var candle =
            CreateCandle(
                minute: 20,
                high: 125m,
                low: 115m);

        var result =
            _service.Evaluate(
                candidate,
                candle);

        Assert.False(result.HasInteracted);
        Assert.Null(result.FirstInteractionTime);
        Assert.Null(result.InteractionPrice);
    }

    private static FairValueGapCandidate CreateCandidate(
        FairValueGapType type,
        bool isCandidate = true)
    {
        var gap =
            new FairValueGap
            {
                Type = type,

                StartTime =
                    new DateTime(
                        2026,
                        9,
                        9,
                        10,
                        0,
                        0,
                        DateTimeKind.Utc),

                ConfirmedAt =
                    new DateTime(
                        2026,
                        9,
                        9,
                        10,
                        15,
                        0,
                        DateTimeKind.Utc),

                LowerPrice = 100m,
                UpperPrice = 120m,
                GapSize = 20m
            };

        var state =
            new FairValueGapState(gap);

        var qualification =
            new FairValueGapQualification
            {
                State = state,
                IsQualified = isCandidate,
                RejectionReasons =
                    isCandidate
                        ? Array.Empty<string>()
                        : new[]
                        {
                            "FVG is not qualified."
                        }
            };

        var trend =
            type == FairValueGapType.Bullish
                ? MarketTrend.Bullish
                : MarketTrend.Bearish;

        var alignment =
            new FairValueGapAlignment
            {
                State = state,
                MarketTrend = trend,
                IsAligned = isCandidate,
                Reason =
                    isCandidate
                        ? "FVG is aligned with market structure."
                        : "FVG direction is not aligned with market structure."
            };

        return new FairValueGapCandidate
        {
            State = state,
            Qualification = qualification,
            Alignment = alignment,
            IsCandidate = isCandidate,
            RejectionReasons =
                isCandidate
                    ? Array.Empty<string>()
                    : new[]
                    {
                        "FVG is not a candidate."
                    }
        };
    }

    private static Candle CreateCandle(
        int minute,
        decimal high,
        decimal low,
        bool isClosed = true)
    {
        var openTime =
            new DateTime(
                2026,
                9,
                9,
                10,
                0,
                0,
                DateTimeKind.Utc)
            .AddMinutes(minute);

        return new Candle
        {
            OpenTime = openTime,
            CloseTime = openTime.AddMinutes(5),
            Open = low,
            High = high,
            Low = low,
            Close = high,
            Volume = 100m,
            IsClosed = isClosed
        };
    }

    [Fact]
    public void Update_FirstInteraction_RecordsInteractionState()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var state =
            new FairValueGapInteractionState(
                candidate);

        var candle =
            CreateCandle(
                minute: 20,
                high: 125m,
                low: 115m);

        _service.Update(
            state,
            candle);

        Assert.True(
            state.HasInteracted);

        Assert.Equal(
            candle.CloseTime,
            state.FirstInteractionTime);

        Assert.Equal(
            115m,
            state.FirstInteractionPrice);
    }

    [Fact]
    public void Update_SubsequentInteraction_PreservesFirstInteraction()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var state =
            new FairValueGapInteractionState(
                candidate);

        var firstCandle =
            CreateCandle(
                minute: 20,
                high: 125m,
                low: 115m);

        var secondCandle =
            CreateCandle(
                minute: 25,
                high: 120m,
                low: 105m);

        _service.Update(
            state,
            firstCandle);

        var firstInteractionTime =
            state.FirstInteractionTime;

        var firstInteractionPrice =
            state.FirstInteractionPrice;

        _service.Update(
            state,
            secondCandle);

        Assert.True(
            state.HasInteracted);

        Assert.Equal(
            firstInteractionTime,
            state.FirstInteractionTime);

        Assert.Equal(
            firstInteractionPrice,
            state.FirstInteractionPrice);

        Assert.Equal(
            firstCandle.CloseTime,
            state.FirstInteractionTime);

        Assert.Equal(
            115m,
            state.FirstInteractionPrice);
    }
    [Fact]
    public void CreateSnapshot_LaterInteractionDoesNotMutateEarlierSnapshot()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var state =
            new FairValueGapInteractionState(
                candidate);

        // Snapshot while the FVG is still waiting.
        var waitingSnapshot =
            state.CreateSnapshot();

        var interactionCandle =
            CreateCandle(
                minute: 20,
                high: 125m,
                low: 115m);

        _service.Update(
            state,
            interactionCandle);

        // Snapshot after the first interaction.
        var interactedSnapshot =
            state.CreateSnapshot();

        // Current state changed.
        Assert.True(
            state.HasInteracted);

        // Earlier historical snapshot must remain unchanged.
        Assert.False(
            waitingSnapshot.HasInteracted);

        Assert.Null(
            waitingSnapshot.FirstInteractionTime);

        Assert.Null(
            waitingSnapshot.FirstInteractionPrice);

        // New snapshot contains the interaction.
        Assert.True(
            interactedSnapshot.HasInteracted);

        Assert.Equal(
            interactionCandle.CloseTime,
            interactedSnapshot.FirstInteractionTime);

        Assert.Equal(
            115m,
            interactedSnapshot.FirstInteractionPrice);
    }
}