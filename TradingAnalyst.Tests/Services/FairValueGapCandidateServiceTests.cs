using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapCandidateServiceTests
{
    private readonly FairValueGapCandidateService _service = new();

    [Fact]
    public void Evaluate_QualifiedAndAlignedActiveFvg_ReturnsCandidate()
    {
        var state = CreateState();

        var qualification = CreateQualification(
            state,
            isQualified: true);

        var alignment = CreateAlignment(
            state,
            MarketTrend.Bullish,
            isAligned: true,
            reason: "Bullish FVG is aligned with bullish market structure.");

        var result =
            _service.Evaluate(
                qualification,
                alignment);

        Assert.True(result.IsCandidate);
        Assert.Empty(result.RejectionReasons);
        Assert.Same(state, result.State);
        Assert.Same(qualification, result.Qualification);
        Assert.Same(alignment, result.Alignment);
    }

    [Fact]
    public void Evaluate_UnqualifiedFvg_ReturnsRejectedCandidate()
    {
        var state = CreateState();

        var qualification = CreateQualification(
            state,
            isQualified: false,
            "FVG is too small relative to ATR.");

        var alignment = CreateAlignment(
            state,
            MarketTrend.Bullish,
            isAligned: true,
            reason: "Bullish FVG is aligned with bullish market structure.");

        var result =
            _service.Evaluate(
                qualification,
                alignment);

        Assert.False(result.IsCandidate);

        Assert.Contains(
            "FVG is too small relative to ATR.",
            result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_MisalignedFvg_ReturnsRejectedCandidate()
    {
        var state = CreateState();

        var qualification = CreateQualification(
            state,
            isQualified: true);

        var alignment = CreateAlignment(
            state,
            MarketTrend.Bearish,
            isAligned: false,
            reason: "FVG direction is not aligned with market structure.");

        var result =
            _service.Evaluate(
                qualification,
                alignment);

        Assert.False(result.IsCandidate);

        Assert.Contains(
            "FVG direction is not aligned with market structure.",
            result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_MitigatedFvg_ReturnsRejectedCandidate()
    {
        var state = CreateState();

        state.UpdateFill(
            100m,
            new DateTime(
                2026,
                9,
                9,
                10,
                20,
                0,
                DateTimeKind.Utc));

        var qualification = CreateQualification(
            state,
            isQualified: false,
            "FVG is fully mitigated.");

        var alignment = CreateAlignment(
            state,
            MarketTrend.Bullish,
            isAligned: true,
            reason: "Bullish FVG is aligned with bullish market structure.");

        var result =
            _service.Evaluate(
                qualification,
                alignment);

        Assert.False(result.IsCandidate);

        Assert.Contains(
            "FVG is fully mitigated.",
            result.RejectionReasons);

        Assert.Single(
            result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_QualificationAndAlignmentForDifferentFvgs_Throws()
    {
        var qualificationState =
            CreateState(
                startTimeMinute: 0);

        var alignmentState =
            CreateState(
                startTimeMinute: 5);

        var qualification =
            CreateQualification(
                qualificationState,
                isQualified: true);

        var alignment =
            CreateAlignment(
                alignmentState,
                MarketTrend.Bullish,
                isAligned: true,
                reason: "Bullish FVG is aligned with bullish market structure.");

        Assert.Throws<ArgumentException>(
            () => _service.Evaluate(
                qualification,
                alignment));
    }

    private static FairValueGapState CreateState(
        int startTimeMinute = 0)
    {
        var startTime =
            new DateTime(
                2026,
                9,
                9,
                10,
                startTimeMinute,
                0,
                DateTimeKind.Utc);

        var confirmedAt =
            startTime.AddMinutes(15);

        var gap =
            new FairValueGap
            {
                Type = FairValueGapType.Bullish,
                StartTime = startTime,
                ConfirmedAt = confirmedAt,
                LowerPrice = 100m,
                UpperPrice = 120m,
                GapSize = 20m
            };

        return new FairValueGapState(gap);
    }

    private static FairValueGapQualification CreateQualification(
        FairValueGapState state,
        bool isQualified,
        params string[] rejectionReasons)
    {
        return new FairValueGapQualification
        {
            State = state,
            IsQualified = isQualified,
            RejectionReasons = rejectionReasons
        };
    }

    private static FairValueGapAlignment CreateAlignment(
        FairValueGapState state,
        MarketTrend marketTrend,
        bool isAligned,
        string reason)
    {
        return new FairValueGapAlignment
        {
            State = state,
            MarketTrend = marketTrend,
            IsAligned = isAligned,
            Reason = reason
        };
    }
}