namespace TradingAnalyst.Business.Domain.Entities;

public sealed class FairValueGapState
{
    public FairValueGapState(FairValueGap gap)
    {
        Gap = gap;
    }

    public FairValueGap Gap { get; }

    public bool IsMitigated { get; private set; }

    public DateTime? MitigatedAt { get; private set; }

    public decimal FillPercentage { get; private set; }

    public void UpdateFill(decimal fillPercentage, DateTime time)
    {
        var clampedPercentage = Math.Clamp(fillPercentage, 0m, 100m);

        if (clampedPercentage <= FillPercentage)
        {
            return;
        }

        FillPercentage = clampedPercentage;

        if (FillPercentage >= 100m)
        {
            IsMitigated = true;
            MitigatedAt ??= time;
        }
    }
    public FairValueGapState CreateSnapshot()
    {
        var snapshot = new FairValueGapState(Gap);

        if (FillPercentage > 0m)
        {
            snapshot.UpdateFill( FillPercentage, MitigatedAt ?? Gap.ConfirmedAt);
        }

        return snapshot;
    }
}