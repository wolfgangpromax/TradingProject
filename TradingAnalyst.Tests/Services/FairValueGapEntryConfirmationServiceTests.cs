using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapEntryConfirmationServiceTests
{
    private readonly FairValueGapEntryConfirmationService _service = new();

    [Fact]
    public void Evaluate_BullishFutureRejectionCandle_ReturnsConfirmed()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bullish);

        var candle =
            CreateCandle(
                minute: 25,
                open: 115m,
                high: 126m,
                low: 110m,
                close: 124m);

        var result =
            _service.Evaluate(
                interaction,
                candle);

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            result.Status);

        Assert.Equal(
            candle.CloseTime,
            result.ConfirmedAt);

        Assert.Equal(
            124m,
            result.ConfirmationPrice);

        Assert.Contains(
            "Bullish rejection confirmed above the FVG.",
            result.Reasons);
    }

    [Fact]
    public void Evaluate_BearishFutureRejectionCandle_ReturnsConfirmed()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bearish);

        var candle =
            CreateCandle(
                minute: 25,
                open: 105m,
                high: 110m,
                low: 94m,
                close: 96m);

        var result =
            _service.Evaluate(
                interaction,
                candle);

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            result.Status);

        Assert.Equal(
            candle.CloseTime,
            result.ConfirmedAt);

        Assert.Equal(
            96m,
            result.ConfirmationPrice);

        Assert.Contains(
            "Bearish rejection confirmed below the FVG.",
            result.Reasons);
    }

    [Fact]
    public void Evaluate_InteractionCandleCannotConfirm_ReturnsWaiting()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bullish);

        var interactionTime =
            interaction.FirstInteractionTime!.Value;

        var candle =
            new Candle
            {
                OpenTime =
                    interactionTime.AddMinutes(-5),

                CloseTime =
                    interactionTime,

                Open = 115m,
                High = 126m,
                Low = 110m,
                Close = 124m,
                Volume = 100m,
                IsClosed = true
            };

        var result =
            _service.Evaluate(
                interaction,
                candle);

        Assert.Equal(
            EntryConfirmationStatus.Waiting,
            result.Status);

        Assert.Null(
            result.ConfirmedAt);

        Assert.Null(
            result.ConfirmationPrice);
    }

    [Fact]
    public void Evaluate_UnclosedConfirmationCandle_ReturnsWaiting()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bullish);

        var candle =
            CreateCandle(
                minute: 25,
                open: 115m,
                high: 126m,
                low: 110m,
                close: 124m,
                isClosed: false);

        var result =
            _service.Evaluate(
                interaction,
                candle);

        Assert.Equal(
            EntryConfirmationStatus.Waiting,
            result.Status);

        Assert.Null(
            result.ConfirmedAt);
    }

    [Fact]
    public void Evaluate_BullishCandleClosesInsideGap_ReturnsWaiting()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bullish);

        var candle =
            CreateCandle(
                minute: 25,
                open: 110m,
                high: 119m,
                low: 105m,
                close: 118m);

        var result =
            _service.Evaluate(
                interaction,
                candle);

        Assert.Equal(
            EntryConfirmationStatus.Waiting,
            result.Status);

        Assert.Null(
            result.ConfirmedAt);

        Assert.Null(
            result.ConfirmationPrice);

        Assert.Contains(
            "Waiting for bullish rejection confirmation above the FVG.",
            result.Reasons);
    }

    [Fact]
    public void Evaluate_NoInteractionYet_ReturnsWaiting()
    {
        var candidate =
            CreateCandidate(
                FairValueGapType.Bullish);

        var interaction =
            new FairValueGapInteractionState(
                candidate);

        var candle =
            CreateCandle(
                minute: 25,
                open: 115m,
                high: 126m,
                low: 110m,
                close: 124m);

        var result =
            _service.Evaluate(
                interaction,
                candle);

        Assert.Equal(
            EntryConfirmationStatus.Waiting,
            result.Status);

        Assert.Null(
            result.ConfirmedAt);

        Assert.Contains(
            "FVG has not been interacted with yet.",
            result.Reasons);
    }

    private static FairValueGapInteractionState CreateInteractionState(
        FairValueGapType type)
    {
        var candidate =
            CreateCandidate(type);

        var interaction =
            new FairValueGapInteractionState(
                candidate);

        interaction.RecordInteraction(
            new DateTime(
                2026,
                9,
                9,
                10,
                20,
                0,
                DateTimeKind.Utc),

            type == FairValueGapType.Bullish
                ? 115m
                : 105m);

        return interaction;
    }

    private static FairValueGapCandidate CreateCandidate(
        FairValueGapType type)
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
                IsQualified = true,
                RejectionReasons =
                    Array.Empty<string>()
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
                IsAligned = true,
                Reason =
                    "FVG is aligned with market structure."
            };

        return new FairValueGapCandidate
        {
            State = state,
            Qualification = qualification,
            Alignment = alignment,
            IsCandidate = true,
            RejectionReasons =
                Array.Empty<string>()
        };
    }

    private static Candle CreateCandle(
        int minute,
        decimal open,
        decimal high,
        decimal low,
        decimal close,
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
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 100m,
            IsClosed = isClosed
        };
    }

    [Fact]
    public void Update_ValidConfirmation_StoresFirstConfirmation()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bullish);

        var state =
            new FairValueGapEntryConfirmationState(
                interaction);

        var candle =
            CreateCandle(
                minute: 25,
                open: 115m,
                high: 126m,
                low: 110m,
                close: 124m);

        _service.Update(
            state,
            candle);

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            state.Status);

        Assert.Equal(
            candle.CloseTime,
            state.ConfirmedAt);

        Assert.Equal(
            124m,
            state.ConfirmationPrice);

        Assert.Contains(
            "Bullish rejection confirmed above the FVG.",
            state.Reasons);
    }

    [Fact]
    public void Update_WhenAlreadyConfirmed_DoesNotOverwriteFirstConfirmation()
    {
        var interaction =
            CreateInteractionState(
                FairValueGapType.Bullish);

        var state =
            new FairValueGapEntryConfirmationState(
                interaction);

        var firstCandle =
            CreateCandle(
                minute: 25,
                open: 115m,
                high: 126m,
                low: 110m,
                close: 124m);

        var laterCandle =
            CreateCandle(
                minute: 30,
                open: 125m,
                high: 135m,
                low: 123m,
                close: 132m);

        _service.Update(
            state,
            firstCandle);

        _service.Update(
            state,
            laterCandle);

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            state.Status);

        Assert.Equal(
            firstCandle.CloseTime,
            state.ConfirmedAt);

        Assert.Equal(
            124m,
            state.ConfirmationPrice);

        Assert.NotEqual(
            laterCandle.CloseTime,
            state.ConfirmedAt);

        Assert.NotEqual(
            132m,
            state.ConfirmationPrice);
    }

    [Fact]
    public void Evaluate_BullishCloseBelowLowerBoundary_ReturnsInvalidated()
    {
        var interaction = CreateInteractionState(FairValueGapType.Bullish);

        var candle = CreateCandle(
            minute: 25,
            open: 105m,
            high: 110m,
            low: 90m,
            close: 95m);

        var result = _service.Evaluate(interaction, candle);

        Assert.Equal(EntryConfirmationStatus.Invalidated, result.Status);
        Assert.Null(result.ConfirmedAt);
        Assert.Null(result.ConfirmationPrice);

        Assert.Contains(
            "Bullish FVG entry setup invalidated by a close below the lower boundary.",
            result.Reasons);
    }

    [Fact]
    public void Evaluate_BearishCloseAboveUpperBoundary_ReturnsInvalidated()
    {
        var interaction = CreateInteractionState(FairValueGapType.Bearish);

        var candle = CreateCandle(
            minute: 25,
            open: 115m,
            high: 130m,
            low: 110m,
            close: 125m);

        var result = _service.Evaluate(interaction, candle);

        Assert.Equal(EntryConfirmationStatus.Invalidated, result.Status);
        Assert.Null(result.ConfirmedAt);
        Assert.Null(result.ConfirmationPrice);

        Assert.Contains(
            "Bearish FVG entry setup invalidated by a close above the upper boundary.",
            result.Reasons);
    }

    [Fact]
    public void Update_BullishCloseBelowLowerBoundary_PersistsInvalidatedState()
    {
        var interaction = CreateInteractionState(FairValueGapType.Bullish);
        var state = new FairValueGapEntryConfirmationState(interaction);

        var candle = CreateCandle(
            minute: 25,
            open: 105m,
            high: 110m,
            low: 90m,
            close: 95m);

        _service.Update(state, candle);

        Assert.Equal(EntryConfirmationStatus.Invalidated, state.Status);
        Assert.Null(state.ConfirmedAt);
        Assert.Null(state.ConfirmationPrice);

        Assert.Contains(
            "Bullish FVG entry setup invalidated by a close below the lower boundary.",
            state.Reasons);
    }

    [Fact]
    public void Update_InvalidatedState_CannotBeConfirmedByLaterCandle()
    {
        var interaction = CreateInteractionState(FairValueGapType.Bullish);
        var state = new FairValueGapEntryConfirmationState(interaction);

        var invalidationCandle = CreateCandle(
            minute: 25,
            open: 105m,
            high: 110m,
            low: 90m,
            close: 95m);

        _service.Update(state, invalidationCandle);

        var confirmationCandle = CreateCandle(
            minute: 30,
            open: 115m,
            high: 130m,
            low: 110m,
            close: 125m);

        _service.Update(state, confirmationCandle);

        Assert.Equal(EntryConfirmationStatus.Invalidated, state.Status);
        Assert.Null(state.ConfirmedAt);
        Assert.Null(state.ConfirmationPrice);

        Assert.Contains(
            "Bullish FVG entry setup invalidated by a close below the lower boundary.",
            state.Reasons);
    }
}