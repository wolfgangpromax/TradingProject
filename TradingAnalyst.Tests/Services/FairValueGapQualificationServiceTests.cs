using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class FairValueGapQualificationServiceTests
{
    private static FairValueGapQualificationService CreateService(
        decimal minimumGapAtrRatio = 0.10m)
    {
        var options =
            Options.Create(
                new FairValueGapQualificationOptions
                {
                    MinimumGapAtrRatio = minimumGapAtrRatio
                });

        return new FairValueGapQualificationService(options);
    }

    [Fact]
    public void Qualify_ActiveConfirmedFvg_ReturnsQualified()
    {
        // Arrange
        var service = CreateService();

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 105m);

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(5)
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.True(result.IsQualified);
        Assert.Empty(result.RejectionReasons);
        Assert.Same(state, result.State);
    }

    [Fact]
    public void Qualify_FullyMitigatedFvg_ReturnsRejected()
    {
        // Arrange
        var service = CreateService();

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 105m);

        var state = new FairValueGapState(gap);

        state.UpdateFill(
            100m,
            confirmedAt.AddMinutes(5));

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(10)
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.False(result.IsQualified);

        Assert.Contains(
            "FVG is fully mitigated.",
            result.RejectionReasons);
    }

    [Fact]
    public void Qualify_ZeroSizeFvg_ReturnsRejected()
    {
        // Arrange
        var service = CreateService();

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        var gap = new FairValueGap
        {
            Type = FairValueGapType.Bullish,
            StartTime = confirmedAt.AddMinutes(-10),
            ConfirmedAt = confirmedAt,
            LowerPrice = 100m,
            UpperPrice = 100m,
            GapSize = 0m
        };

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(5)
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.False(result.IsQualified);

        Assert.Contains(
            "FVG has an invalid gap size.",
            result.RejectionReasons);
    }

    [Fact]
    public void Qualify_FutureUnconfirmedFvg_ReturnsRejected()
    {
        // Arrange
        var service = CreateService();

        var currentTime = new DateTime(
            2026, 9, 7, 10, 10, 0,
            DateTimeKind.Utc);

        var confirmedAt =
            currentTime.AddMinutes(5);

        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 105m);

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = currentTime
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.False(result.IsQualified);

        Assert.Contains(
            "FVG is not confirmed yet.",
            result.RejectionReasons);
    }

    [Fact]
    public void Qualify_GapBelowMinimumAtrRatio_ReturnsRejected()
    {
        // Arrange
        var service = CreateService(
            minimumGapAtrRatio: 0.10m);

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        // GapSize = 5
        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 105m);

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(5),

            // 5 / 100 = 0.05
            AverageTrueRange = 100m
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.False(result.IsQualified);

        Assert.Contains(
            "FVG is too small relative to ATR.",
            result.RejectionReasons);
    }

    [Fact]
    public void Qualify_GapEqualToMinimumAtrRatio_ReturnsQualified()
    {
        // Arrange
        var service = CreateService(
            minimumGapAtrRatio: 0.10m);

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        // GapSize = 10
        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 110m);

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(5),

            // 10 / 100 = 0.10
            AverageTrueRange = 100m
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.True(result.IsQualified);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void Qualify_GapAboveMinimumAtrRatio_ReturnsQualified()
    {
        // Arrange
        var service = CreateService(
            minimumGapAtrRatio: 0.10m);

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        // GapSize = 20
        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 120m);

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(5),

            // 20 / 100 = 0.20
            AverageTrueRange = 100m
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.True(result.IsQualified);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void Qualify_WithoutAtr_DoesNotRejectOtherwiseValidFvg()
    {
        // Arrange
        var service = CreateService();

        var confirmedAt = new DateTime(
            2026, 9, 7, 10, 15, 0,
            DateTimeKind.Utc);

        var gap = CreateGap(
            confirmedAt,
            lowerPrice: 100m,
            upperPrice: 101m);

        var state = new FairValueGapState(gap);

        var context = new FairValueGapQualificationContext
        {
            CurrentTime = confirmedAt.AddMinutes(5),
            AverageTrueRange = null
        };

        // Act
        var result =
            service.Qualify(state, context);

        // Assert
        Assert.True(result.IsQualified);
        Assert.Empty(result.RejectionReasons);
    }

    private static FairValueGap CreateGap(
        DateTime confirmedAt,
        decimal lowerPrice,
        decimal upperPrice)
    {
        return new FairValueGap
        {
            Type = FairValueGapType.Bullish,
            StartTime = confirmedAt.AddMinutes(-10),
            ConfirmedAt = confirmedAt,
            LowerPrice = lowerPrice,
            UpperPrice = upperPrice,
            GapSize = upperPrice - lowerPrice
        };
    }
}