using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Tests.Domain;

public sealed class FairValueGapStateTests
{
    [Fact]
    public void NewState_ShouldBeUnmitigated()
    {
        var state = new FairValueGapState(
            CreateGap());

        Assert.False(state.IsMitigated);
        Assert.Null(state.MitigatedAt);
        Assert.Equal(0m, state.FillPercentage);
    }

    [Fact]
    public void UpdateFill_PartialFill_ShouldRemainUnmitigated()
    {
        var state = new FairValueGapState(
            CreateGap());

        var time =
            new DateTime(2026, 9, 7, 11, 0, 0);

        state.UpdateFill(
            40m,
            time);

        Assert.Equal(40m, state.FillPercentage);
        Assert.False(state.IsMitigated);
        Assert.Null(state.MitigatedAt);
    }

    [Fact]
    public void UpdateFill_FullFill_ShouldMarkMitigated()
    {
        var state = new FairValueGapState(
            CreateGap());

        var time =
            new DateTime(2026, 9, 7, 11, 0, 0);

        state.UpdateFill(
            100m,
            time);

        Assert.Equal(100m, state.FillPercentage);
        Assert.True(state.IsMitigated);
        Assert.Equal(
            time,
            state.MitigatedAt);
    }

    private static FairValueGap CreateGap()
    {
        return new FairValueGap
        {
            Type = FairValueGapType.Bullish,
            StartTime =
                new DateTime(2026, 9, 7, 10, 0, 0),
            ConfirmedAt =
                new DateTime(2026, 9, 7, 10, 15, 0),
            LowerPrice = 100m,
            UpperPrice = 105m,
            GapSize = 5m
        };
    }

    [Fact]
    public void UpdateFill_LowerPercentage_ShouldNotReduceExistingFill()
    {
        var state = new FairValueGapState(
            CreateGap());

        state.UpdateFill(
            60m,
            new DateTime(2026, 9, 7, 11, 0, 0));

        state.UpdateFill(
            30m,
            new DateTime(2026, 9, 7, 11, 5, 0));

        Assert.Equal(
            60m,
            state.FillPercentage);

        Assert.False(
            state.IsMitigated);

        Assert.Null(
            state.MitigatedAt);
    }

    [Fact]
    public void CreateSnapshot_ShouldNotChangeWhenOriginalStateChanges()
    {
        var state = new FairValueGapState(
            CreateGap());

        state.UpdateFill(
            40m,
            new DateTime(2026, 9, 7, 11, 0, 0));

        var snapshot =
            state.CreateSnapshot();

        state.UpdateFill(
            100m,
            new DateTime(2026, 9, 7, 11, 5, 0));

        Assert.Equal(
            40m,
            snapshot.FillPercentage);

        Assert.False(
            snapshot.IsMitigated);

        Assert.Null(
            snapshot.MitigatedAt);

        Assert.Equal(
            100m,
            state.FillPercentage);

        Assert.True(
            state.IsMitigated);
    }
}