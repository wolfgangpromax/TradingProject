using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.Entities;

public sealed class TradeOutcomeState
{
    public TradeOutcomeState(TradeSetup setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        Setup = setup;
    }

    public TradeSetup Setup { get; }
    public TradeOutcomeStatus Status { get; private set; } = TradeOutcomeStatus.Active;
    public DateTime? ClosedAt { get; private set; }
    public decimal? ExitPrice { get; private set; }
    public decimal? RealizedRiskRewardRatio { get; private set; }
    public IReadOnlyCollection<string> Reasons { get; private set; } = Array.Empty<string>();

    public void Win(DateTime closedAt, decimal exitPrice, IReadOnlyCollection<string> reasons)
    {
        if (Status != TradeOutcomeStatus.Active) return;

        Status = TradeOutcomeStatus.Won;
        ClosedAt = closedAt;
        ExitPrice = exitPrice;
        RealizedRiskRewardRatio = Setup.RiskRewardRatio;
        Reasons = reasons;
    }

    public void Lose(DateTime closedAt, decimal exitPrice, IReadOnlyCollection<string> reasons)
    {
        if (Status != TradeOutcomeStatus.Active) return;

        Status = TradeOutcomeStatus.Lost;
        ClosedAt = closedAt;
        ExitPrice = exitPrice;
        RealizedRiskRewardRatio = -1m;
        Reasons = reasons;
    }

    public TradeOutcome CreateSnapshot()
    {
        return new TradeOutcome
        {
            Setup = Setup,
            Status = Status,
            ClosedAt = ClosedAt,
            ExitPrice = ExitPrice,
            RealizedRiskRewardRatio = RealizedRiskRewardRatio,
            Reasons = Reasons.ToArray()
        };
    }
}