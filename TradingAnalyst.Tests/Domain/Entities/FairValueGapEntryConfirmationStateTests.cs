using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Tests.Domain.Entities;

public sealed class FairValueGapEntryConfirmationStateTests
{
    [Fact]
    public void Confirm_WhenWaiting_StoresFirstConfirmation()
    {
        var state =
            new FairValueGapEntryConfirmationState(
                CreateInteractionState());

        var confirmedAt =
            new DateTime(
                2026,
                9,
                9,
                10,
                25,
                0,
                DateTimeKind.Utc);

        state.Confirm(
            confirmedAt,
            124m,
            new[] { "Bullish rejection confirmed above the FVG." });

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            state.Status);

        Assert.Equal(
            confirmedAt,
            state.ConfirmedAt);

        Assert.Equal(
            124m,
            state.ConfirmationPrice);

        Assert.Contains(
            "Bullish rejection confirmed above the FVG.",
            state.Reasons);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_DoesNotOverwriteFirstConfirmation()
    {
        var state =
            new FairValueGapEntryConfirmationState(
                CreateInteractionState());

        var firstConfirmationTime =
            new DateTime(
                2026,
                9,
                9,
                10,
                25,
                0,
                DateTimeKind.Utc);

        state.Confirm(
            firstConfirmationTime,
            124m,
            new[] { "First confirmation." });

        state.Confirm(
            firstConfirmationTime.AddMinutes(5),
            130m,
            new[] { "Second confirmation." });

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            state.Status);

        Assert.Equal(
            firstConfirmationTime,
            state.ConfirmedAt);

        Assert.Equal(
            124m,
            state.ConfirmationPrice);

        Assert.Contains(
            "First confirmation.",
            state.Reasons);

        Assert.DoesNotContain(
            "Second confirmation.",
            state.Reasons);
    }

    [Fact]
    public void CreateSnapshot_DoesNotChangeAfterOriginalStateIsConfirmed()
    {
        var state =
            new FairValueGapEntryConfirmationState(
                CreateInteractionState());

        var waitingSnapshot =
            state.CreateSnapshot();

        var confirmedAt =
            new DateTime(
                2026,
                9,
                9,
                10,
                25,
                0,
                DateTimeKind.Utc);

        state.Confirm(
            confirmedAt,
            124m,
            new[] { "Confirmed." });

        Assert.Equal(
            EntryConfirmationStatus.Waiting,
            waitingSnapshot.Status);

        Assert.Null(
            waitingSnapshot.ConfirmedAt);

        Assert.Null(
            waitingSnapshot.ConfirmationPrice);

        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            state.Status);

        Assert.Equal(
            confirmedAt,
            state.ConfirmedAt);

        Assert.Equal(
            124m,
            state.ConfirmationPrice);
    }

    private static FairValueGapInteractionState CreateInteractionState()
    {
        var gap =
            new FairValueGap
            {
                Type = FairValueGapType.Bullish,

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

        var gapState =
            new FairValueGapState(gap);

        var qualification =
            new FairValueGapQualification
            {
                State = gapState,
                IsQualified = true,
                RejectionReasons =
                    Array.Empty<string>()
            };

        var alignment =
            new FairValueGapAlignment
            {
                State = gapState,
                MarketTrend = MarketTrend.Bullish,
                IsAligned = true,
                Reason =
                    "Bullish FVG is aligned with bullish market structure."
            };

        var candidate =
            new FairValueGapCandidate
            {
                State = gapState,
                Qualification = qualification,
                Alignment = alignment,
                IsCandidate = true,
                RejectionReasons =
                    Array.Empty<string>()
            };

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
            115m);

        return interaction;
    }

    [Fact]
    public void CreateSnapshot_WaitingSnapshotRemainsWaitingAfterFutureConfirmation()
    {
        var state =
            new FairValueGapEntryConfirmationState(
                CreateInteractionState());

        // Historical snapshot taken before confirmation.
        var waitingSnapshot =
            state.CreateSnapshot();

        var confirmedAt =
            new DateTime(
                2026,
                9,
                9,
                10,
                30,
                0,
                DateTimeKind.Utc);

        // Future event changes the live state.
        state.Confirm(
            confirmedAt,
            124m,
            new[] { "Bullish rejection confirmed above the FVG." });

        // Historical snapshot must remain unchanged.
        Assert.Equal(
            EntryConfirmationStatus.Waiting,
            waitingSnapshot.Status);

        Assert.Null(
            waitingSnapshot.ConfirmedAt);

        Assert.Null(
            waitingSnapshot.ConfirmationPrice);

        // Current/live state should now be confirmed.
        Assert.Equal(
            EntryConfirmationStatus.Confirmed,
            state.Status);

        Assert.Equal(
            confirmedAt,
            state.ConfirmedAt);

        Assert.Equal(
            124m,
            state.ConfirmationPrice);
    }
}