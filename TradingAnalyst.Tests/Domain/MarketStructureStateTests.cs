using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Tests.Domain;

public sealed class MarketStructureStateTests
{
    [Fact]
    public void NewState_ShouldStartAsUnknown()
    {
        var state = new MarketStructureState();

        Assert.Equal(
            MarketTrend.Unknown,
            state.Trend);

        Assert.Null(state.ProtectedHigh);
        Assert.Null(state.ProtectedLow);
        Assert.Empty(state.ConsumedLevels);
    }

    [Fact]
    public void EnterTransition_ShouldSetTrendToTransition()
    {
        var state = new MarketStructureState();

        state.SetTrend(MarketTrend.Bullish);

        state.EnterTransition(BreakDirection.Bearish);

        Assert.Equal(MarketTrend.Transition, state.Trend);
        Assert.Equal(BreakDirection.Bearish, state.TransitionDirection);
    }

    [Fact]
    public void ConsumeLevel_ShouldMarkStructurePointAsConsumed()
    {
        var state = new MarketStructureState();

        var structurePoint =
            new MarketStructurePoint
            {
                OpenTime =
                    new DateTime(
                        2026, 9, 6,
                        10, 0, 0,
                        DateTimeKind.Utc),

                ConfirmedAt =
                    new DateTime(
                        2026, 9, 6,
                        10, 10, 0,
                        DateTimeKind.Utc),

                Price = 80000m,

                SwingType = SwingType.High,

                StructureType = MarketStructureType.HigherHigh,

                CandleIndex = 10
            };

        state.ConsumeLevel(structurePoint);

        Assert.True(state.IsConsumed(structurePoint));
        Assert.Single(state.ConsumedLevels);

    }

    [Fact]
    public void SetProtectedLevels_ShouldStoreHighAndLow()
    {
        var state = new MarketStructureState();

        var high = new MarketStructurePoint
        {
            OpenTime = new DateTime(
                2026, 9, 6, 10, 0, 0,
                DateTimeKind.Utc),

            ConfirmedAt = new DateTime(
                2026, 9, 6, 10, 10, 0,
                DateTimeKind.Utc),

            Price = 80100m,
            SwingType = SwingType.High,
            StructureType = MarketStructureType.HigherHigh,
            CandleIndex = 10
        };

        var low = new MarketStructurePoint
        {
            OpenTime = new DateTime(
                2026, 9, 6, 10, 5, 0,
                DateTimeKind.Utc),

            ConfirmedAt = new DateTime(
                2026, 9, 6, 10, 15, 0,
                DateTimeKind.Utc),

            Price = 79700m,
            SwingType = SwingType.Low,
            StructureType = MarketStructureType.HigherLow,
            CandleIndex = 11
        };

        state.SetProtectedLevels(high, low);

        Assert.Same(high, state.ProtectedHigh);
        Assert.Same(low, state.ProtectedLow);
    }

    [Fact]
    public void SetTrend_AfterTransition_ShouldClearTransitionDirection()
    {
        var state = new MarketStructureState();

        state.SetTrend(MarketTrend.Bearish);
        state.EnterTransition(BreakDirection.Bullish);

        Assert.Equal(MarketTrend.Transition, state.Trend);
        Assert.Equal(BreakDirection.Bullish, state.TransitionDirection);

        state.SetTrend(MarketTrend.Bullish);

        Assert.Equal(MarketTrend.Bullish, state.Trend);
        Assert.Null(state.TransitionDirection);
    }

    [Fact]
    public void TryConfirmTransition_MatchingTrend_ShouldConfirmTransition()
    {
        var state = new MarketStructureState();

        state.SetTrend(MarketTrend.Bearish);
        state.EnterTransition(BreakDirection.Bullish);

        var confirmed =state.TryConfirmTransition(MarketTrend.Bullish);

        Assert.True(confirmed);
        Assert.Equal(MarketTrend.Bullish, state.Trend);
        Assert.Null(state.TransitionDirection);
    }

    [Fact]
    public void TryConfirmTransition_OppositeTrend_ShouldRemainInTransition()
    {
        var state = new MarketStructureState();

        state.SetTrend(MarketTrend.Bearish);
        state.EnterTransition(BreakDirection.Bullish);

        var confirmed =state.TryConfirmTransition( MarketTrend.Bearish);

        Assert.False(confirmed);
        Assert.Equal(MarketTrend.Transition, state.Trend);
        Assert.Equal(BreakDirection.Bullish, state.TransitionDirection);
    }
}