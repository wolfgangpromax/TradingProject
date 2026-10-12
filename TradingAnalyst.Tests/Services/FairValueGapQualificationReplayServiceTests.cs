using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapQualificationReplayServiceTests
{
    private readonly FairValueGapQualificationReplayService _service;

    public FairValueGapQualificationReplayServiceTests()
    {
        var fvgDetectionService =
            new FairValueGapDetectionService();

        var fvgMitigationService =
            new FairValueGapMitigationService();

        var fvgReplayService =
            new FairValueGapReplayService(
                fvgDetectionService,
                fvgMitigationService);

        var atrService =
            new AverageTrueRangeService();

        var options =
            Options.Create(
                new FairValueGapQualificationOptions
                {
                    MinimumGapAtrRatio = 0.10m
                });

        var qualificationService =
            new FairValueGapQualificationService(
                options);

        // Market structure dependencies
        var swingDetectionService =
            new SwingDetectionService();

        var marketStructureService =
            new MarketStructureService();

        var marketTrendService =
            new MarketTrendService();

        var structureBreakDetectionService =
            new StructureBreakDetectionService();

        var fvgAlignmentService = 
            new FairValueGapAlignmentService();

        var fvgCandidateService =
            new FairValueGapCandidateService();

        var marketStructureReplayService =
            new MarketStructureReplayService(
                swingDetectionService,
                marketStructureService,
                marketTrendService,
                structureBreakDetectionService);

        var fvgInteractionService =
            new FairValueGapInteractionService();

        var tradeSetupService = new TradeSetupService(
                                    Options.Create(new TradeSetupOptions
                                    {
                                        StopLossAtrBufferMultiplier = 0.10m,
                                        MinimumRiskRewardRatio = 2.00m
                                    }));

        var tradeOutcomeService = new TradeOutcomeService();

        var riskManagementOptions = Options.Create(
            new RiskManagementOptions
            {
                InitialAccountEquity = 10_000m,
                RiskPerTradePercentage = 1.00m,
                MaximumDailyLossR = 3.00m,
                MaximumTradesPerDay = 5,
                MaximumActiveTrades = 1
            });

        var positionSizingService = new PositionSizingService(riskManagementOptions);
        var riskManagementService = new RiskManagementService(riskManagementOptions);
        var tradeExecutionService = new TradeExecutionService();

        _service = new FairValueGapQualificationReplayService(
            fvgReplayService,
            atrService,
            qualificationService,
            marketStructureReplayService,
            fvgAlignmentService,
            fvgCandidateService,
            fvgInteractionService,
            new FairValueGapEntryConfirmationService(),
            tradeSetupService,
            tradeOutcomeService,
            positionSizingService,
            riskManagementService,
            riskManagementOptions,
            tradeExecutionService);
    }

    [Fact]
    public void Replay_FvgUsesAtrAvailableAtSnapshotTime()
    {
        var candles = new List<Candle>
        {
            CreateCandle(
                minute: 0,
                open: 100m,
                high: 110m,
                low: 100m,
                close: 105m),

            CreateCandle(
                minute: 5,
                open: 105m,
                high: 112m,
                low: 103m,
                close: 108m),

            // Bullish FVG:
            // Candle 3 Low = 120
            // Candle 1 High = 110
            // Gap = 10
            CreateCandle(
                minute: 10,
                open: 120m,
                high: 125m,
                low: 120m,
                close: 123m),

            // Large future volatility.
            // This must NOT affect the qualification
            // of the FVG at minute 10.
            CreateCandle(
                minute: 15,
                open: 123m,
                high: 300m,
                low: 50m,
                close: 200m)
        };

        var snapshots =
            _service
                .Replay(
                    candles,
                    atrPeriod: 2)
                .OrderBy(x => x.Time)
                .ToList();

        var fvgSnapshot =
            snapshots.Single(x =>
                x.Time ==
                candles[2].CloseTime);

        var qualification =
            Assert.Single(
                fvgSnapshot.Qualifications);

        Assert.True(
            qualification.IsQualified);

        Assert.NotNull(
            fvgSnapshot.AverageTrueRange);

        // ATR at this point must only use candles
        // available through candle 3.
        //
        // TR candle 2:
        // max(9, |112-105|, |103-105|)
        // = 9
        //
        // TR candle 3:
        // max(5, |125-108|, |120-108|)
        // = 17
        //
        // ATR = (9 + 17) / 2
        //     = 13
        Assert.Equal(
            13m,
            fvgSnapshot.AverageTrueRange.Value);
    }

    [Fact]
    public void Replay_FutureVolatilityDoesNotChangeEarlierSnapshotAtr()
    {
        var candlesWithoutFuture =
            new List<Candle>
            {
                CreateCandle(
                    minute: 0,
                    open: 100m,
                    high: 110m,
                    low: 100m,
                    close: 105m),

                CreateCandle(
                    minute: 5,
                    open: 105m,
                    high: 112m,
                    low: 103m,
                    close: 108m),

                CreateCandle(
                    minute: 10,
                    open: 120m,
                    high: 125m,
                    low: 120m,
                    close: 123m)
            };

        var candlesWithFuture =
            new List<Candle>(
                candlesWithoutFuture)
            {
                CreateCandle(
                    minute: 15,
                    open: 123m,
                    high: 500m,
                    low: 1m,
                    close: 300m)
            };

        var withoutFuture =
            _service
                .Replay(
                    candlesWithoutFuture,
                    atrPeriod: 2)
                .Single();

        var withFuture =
            _service
                .Replay(
                    candlesWithFuture,
                    atrPeriod: 2)
                .Single(x =>
                    x.Time ==
                    candlesWithoutFuture[2].CloseTime);

        Assert.Equal(
            withoutFuture.AverageTrueRange,
            withFuture.AverageTrueRange);

        Assert.Equal(
            13m,
            withFuture.AverageTrueRange);
    }

    [Fact]
    public void Replay_InsufficientCandlesForAtr_ReturnsNullAtr()
    {
        var candles = new List<Candle>
        {
            CreateCandle(
                minute: 0,
                open: 100m,
                high: 110m,
                low: 100m,
                close: 105m),

            CreateCandle(
                minute: 5,
                open: 105m,
                high: 112m,
                low: 103m,
                close: 108m),

            CreateCandle(
                minute: 10,
                open: 120m,
                high: 125m,
                low: 120m,
                close: 123m)
        };

        var snapshot =
            Assert.Single(
                _service.Replay(
                    candles,
                    atrPeriod: 14));

        Assert.Null(
            snapshot.AverageTrueRange);

        // Our current qualification rule does not
        // reject an otherwise valid FVG merely
        // because ATR is unavailable.
        var qualification =
            Assert.Single(
                snapshot.Qualifications);

        Assert.True(
            qualification.IsQualified);
    }

    [Fact]
    public void Replay_InvalidAtrPeriod_Throws()
    {
        var candles =
            new List<Candle>();

        Action action = () =>
        {
            _service.Replay(
                candles,
                atrPeriod: 0);
        };

        Assert.Throws<ArgumentOutOfRangeException>(
            action);
    }

    private static Candle CreateCandle(
        int minute,
        decimal open,
        decimal high,
        decimal low,
        decimal close)
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

            IsClosed = true
        };
    }

    [Fact]
    public void Replay_FutureMarketTrendDoesNotChangeEarlierSnapshotTrend()
    {
        var candlesWithoutFuture =
            new List<Candle>
            {
            CreateCandle(
                minute: 0,
                open: 100m,
                high: 110m,
                low: 100m,
                close: 105m),

            CreateCandle(
                minute: 5,
                open: 105m,
                high: 112m,
                low: 103m,
                close: 108m),

            // Creates a bullish FVG:
            // C3.Low = 120 > C1.High = 110
            CreateCandle(
                minute: 10,
                open: 120m,
                high: 125m,
                low: 120m,
                close: 123m)
            };

        var candlesWithFuture =
            new List<Candle>(candlesWithoutFuture)
            {
            // Future candles deliberately create
            // substantial price movement.
            CreateCandle(
                minute: 15,
                open: 123m,
                high: 140m,
                low: 118m,
                close: 135m),

            CreateCandle(
                minute: 20,
                open: 135m,
                high: 150m,
                low: 130m,
                close: 145m),

            CreateCandle(
                minute: 25,
                open: 145m,
                high: 155m,
                low: 125m,
                close: 130m)
            };

        var withoutFuture =
            _service
                .Replay(
                    candlesWithoutFuture,
                    atrPeriod: 2)
                .Single();

        var withFuture =
            _service
                .Replay(
                    candlesWithFuture,
                    atrPeriod: 2)
                .Single(x =>
                    x.Time ==
                    candlesWithoutFuture[2].CloseTime);

        Assert.Equal(
            withoutFuture.Time,
            withFuture.Time);

        Assert.Equal(
            withoutFuture.MarketTrend,
            withFuture.MarketTrend);

        Assert.Equal(
            withoutFuture.AverageTrueRange,
            withFuture.AverageTrueRange);
    }

    [Fact]
    public void Replay_FvgContainsQualificationAndAlignmentAtSameSnapshot()
    {
        var candles = new List<Candle>
    {
        CreateCandle(
            minute: 0,
            open: 100m,
            high: 110m,
            low: 100m,
            close: 105m),

        CreateCandle(
            minute: 5,
            open: 105m,
            high: 112m,
            low: 103m,
            close: 108m),

        // Bullish FVG:
        // C3.Low = 120
        // C1.High = 110
        // Gap = 10
        CreateCandle(
            minute: 10,
            open: 120m,
            high: 125m,
            low: 120m,
            close: 123m)
    };

        var snapshot =
            Assert.Single(
                _service.Replay(
                    candles,
                    atrPeriod: 2));

        var qualification =
            Assert.Single(
                snapshot.Qualifications);

        var alignment =
            Assert.Single(
                snapshot.Alignments);

        // Both results must refer to the same FVG.
        Assert.Equal(
            qualification.State.Gap.Key,
            alignment.State.Gap.Key);

        Assert.Equal(
            snapshot.MarketTrend,
            alignment.MarketTrend);

        // Gap = 10
        // ATR = 13
        // Gap / ATR ~= 0.769
        // Minimum = 0.10
        Assert.True(
            qualification.IsQualified);

        // With only three candles there is not enough
        // confirmed market structure to establish
        // directional trend.
        Assert.Equal(
            MarketTrend.Unknown,
            snapshot.MarketTrend);

        Assert.False(
            alignment.IsAligned);

        Assert.Equal(
            "Market structure direction is unknown.",
            alignment.Reason);
    }

    [Fact]
    public void Replay_FutureCandlesDoNotChangeEarlierCandidateDecision()
    {
        var initialCandles = new List<Candle>
    {
        CreateCandle(
            minute: 0,
            open: 100m,
            high: 110m,
            low: 100m,
            close: 105m),

        CreateCandle(
            minute: 5,
            open: 105m,
            high: 112m,
            low: 103m,
            close: 108m),

        // Creates bullish FVG:
        // C1.High = 110
        // C3.Low  = 120
        CreateCandle(
            minute: 10,
            open: 120m,
            high: 125m,
            low: 120m,
            close: 123m)
    };

        var candlesWithFutureData =
            initialCandles
                .Concat(
                [
                    CreateCandle(
                    minute: 15,
                    open: 123m,
                    high: 180m,
                    low: 90m,
                    close: 170m),

                CreateCandle(
                    minute: 20,
                    open: 170m,
                    high: 200m,
                    low: 80m,
                    close: 85m),

                CreateCandle(
                    minute: 25,
                    open: 85m,
                    high: 210m,
                    low: 70m,
                    close: 205m)
                ])
                .ToList();

        var initialReplay =
            _service.Replay(
                initialCandles,
                atrPeriod: 2);

        var replayWithFutureData =
            _service.Replay(
                candlesWithFutureData,
                atrPeriod: 2);

        var snapshotTime =
            initialCandles[2].CloseTime;

        var initialSnapshot =
            initialReplay.Single(
                x => x.Time == snapshotTime);

        var futureReplaySnapshot =
            replayWithFutureData.Single(
                x => x.Time == snapshotTime);

        var initialCandidate =
            Assert.Single(
                initialSnapshot.Candidates);

        var futureReplayCandidate =
            Assert.Single(
                futureReplaySnapshot.Candidates);

        Assert.Equal(
            initialCandidate.State.Gap.Key,
            futureReplayCandidate.State.Gap.Key);

        Assert.Equal(
            initialCandidate.IsCandidate,
            futureReplayCandidate.IsCandidate);

        Assert.Equal(
            initialCandidate.RejectionReasons,
            futureReplayCandidate.RejectionReasons);

        Assert.Equal(
            initialCandidate.Qualification.IsQualified,
            futureReplayCandidate.Qualification.IsQualified);

        Assert.Equal(
            initialCandidate.Alignment.IsAligned,
            futureReplayCandidate.Alignment.IsAligned);

        Assert.Equal(
            initialCandidate.Alignment.MarketTrend,
            futureReplayCandidate.Alignment.MarketTrend);

        Assert.Equal(
            initialSnapshot.AverageTrueRange,
            futureReplaySnapshot.AverageTrueRange);

        Assert.Equal(
            initialSnapshot.MarketTrend,
            futureReplaySnapshot.MarketTrend);
    }

    [Fact]
    public void Replay_NoConfirmedEntry_DoesNotCreateTradeSetup()
    {
        var candles = new List<Candle>
    {
        CreateCandle(
            minute: 0,
            open: 100m,
            high: 110m,
            low: 100m,
            close: 105m),

        CreateCandle(
            minute: 5,
            open: 105m,
            high: 112m,
            low: 103m,
            close: 108m),

        // Creates bullish FVG:
        // Lower = 110
        // Upper = 120
        CreateCandle(
            minute: 10,
            open: 120m,
            high: 125m,
            low: 120m,
            close: 123m)
    };

        var snapshot = Assert.Single(_service.Replay(candles, atrPeriod: 2));

        Assert.Empty(snapshot.EntryConfirmations);
        Assert.Empty(snapshot.TradeSetups);
    }

    [Fact]
    public void Replay_NoConfirmedEntry_DoesNotCreateRiskAssessmentOrPositionSize()
    {
        var candles = new List<Candle>
    {
        CreateCandle(
            minute: 0,
            open: 100m,
            high: 110m,
            low: 100m,
            close: 105m),

        CreateCandle(
            minute: 5,
            open: 105m,
            high: 112m,
            low: 103m,
            close: 108m),

        // Creates bullish FVG:
        // Lower = 110
        // Upper = 120
        CreateCandle(
            minute: 10,
            open: 120m,
            high: 125m,
            low: 120m,
            close: 123m)
    };

        var snapshot = Assert.Single(
            _service.Replay(
                candles,
                atrPeriod: 2));

        Assert.Empty(snapshot.EntryConfirmations);
        Assert.Empty(snapshot.TradeSetups);
        Assert.Empty(snapshot.PositionSizes);
        Assert.Empty(snapshot.TradeRiskAssessments);
        Assert.Empty(snapshot.TradeOutcomes);
    }

}