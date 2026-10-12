using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapAlignmentServiceTests
{
    private readonly FairValueGapAlignmentService _service = new();

    [Fact]
    public void Evaluate_BullishFvgAndBullishTrend_ReturnsAligned()
    {
        var state = CreateState(FairValueGapType.Bullish);

        var result =
            _service.Evaluate(
                state,
                MarketTrend.Bullish);

        Assert.True(result.IsAligned);
        Assert.Equal(MarketTrend.Bullish, result.MarketTrend);
        Assert.Same(state, result.State);

        Assert.Equal(
            "Bullish FVG is aligned with bullish market structure.",
            result.Reason);
    }

    [Fact]
    public void Evaluate_BearishFvgAndBearishTrend_ReturnsAligned()
    {
        var state = CreateState(FairValueGapType.Bearish);

        var result =
            _service.Evaluate(
                state,
                MarketTrend.Bearish);

        Assert.True(result.IsAligned);
        Assert.Equal(MarketTrend.Bearish, result.MarketTrend);

        Assert.Equal(
            "Bearish FVG is aligned with bearish market structure.",
            result.Reason);
    }

    [Fact]
    public void Evaluate_BullishFvgAndBearishTrend_ReturnsNotAligned()
    {
        var state = CreateState(FairValueGapType.Bullish);

        var result =
            _service.Evaluate(
                state,
                MarketTrend.Bearish);

        Assert.False(result.IsAligned);

        Assert.Equal(
            "FVG direction is not aligned with market structure.",
            result.Reason);
    }

    [Fact]
    public void Evaluate_BearishFvgAndBullishTrend_ReturnsNotAligned()
    {
        var state = CreateState(FairValueGapType.Bearish);

        var result =
            _service.Evaluate(
                state,
                MarketTrend.Bullish);

        Assert.False(result.IsAligned);

        Assert.Equal(
            "FVG direction is not aligned with market structure.",
            result.Reason);
    }

    [Fact]
    public void Evaluate_TransitionTrend_ReturnsNotAligned()
    {
        var state = CreateState(FairValueGapType.Bullish);

        var result =
            _service.Evaluate(
                state,
                MarketTrend.Transition);

        Assert.False(result.IsAligned);

        Assert.Equal(
            "Market structure is in transition.",
            result.Reason);
    }

    [Fact]
    public void Evaluate_UnknownTrend_ReturnsNotAligned()
    {
        var state = CreateState(FairValueGapType.Bullish);

        var result =
            _service.Evaluate(
                state,
                MarketTrend.Unknown);

        Assert.False(result.IsAligned);

        Assert.Equal(
            "Market structure direction is unknown.",
            result.Reason);
    }

    private static FairValueGapState CreateState(
        FairValueGapType type)
    {
        var confirmedAt =
            new DateTime(
                2026,
                9,
                9,
                10,
                15,
                0,
                DateTimeKind.Utc);

        var gap =
            new FairValueGap
            {
                Type = type,

                StartTime =
                    confirmedAt.AddMinutes(-10),

                ConfirmedAt =
                    confirmedAt,

                LowerPrice = 100m,
                UpperPrice = 120m,
                GapSize = 20m
            };

        return new FairValueGapState(gap);
    }
}