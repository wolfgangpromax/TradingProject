using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class TradeOutcomeService : ITradeOutcomeService
{
    public TradeOutcome Evaluate(TradeSetup setup, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ArgumentNullException.ThrowIfNull(candle);

        if (!candle.IsClosed || candle.CloseTime <= setup.CreatedAt)
        {
            return Active(setup);
        }

        return setup.Direction switch
        {
            TradeDirection.Long => EvaluateLong(setup, candle),
            TradeDirection.Short => EvaluateShort(setup, candle),
            _ => Active(setup)
        };
    }

    public void Update(TradeOutcomeState state, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(candle);

        if (state.Status != TradeOutcomeStatus.Active) return;

        var result = Evaluate(state.Setup, candle);

        if (result.Status == TradeOutcomeStatus.Won && result.ClosedAt.HasValue && result.ExitPrice.HasValue)
        {
            state.Win(result.ClosedAt.Value, result.ExitPrice.Value, result.Reasons);
            return;
        }

        if (result.Status == TradeOutcomeStatus.Lost && result.ClosedAt.HasValue && result.ExitPrice.HasValue)
        {
            state.Lose(result.ClosedAt.Value, result.ExitPrice.Value, result.Reasons);
        }
    }

    private static TradeOutcome EvaluateLong(TradeSetup setup, Candle candle)
    {
        var stopLossHit = candle.Low <= setup.StopLossPrice;
        var takeProfitHit = candle.High >= setup.TakeProfitPrice;

        if (stopLossHit)
        {
            return Lost(setup, candle.CloseTime, setup.StopLossPrice,
                takeProfitHit
                    ? "Both stop loss and take profit were touched in the same candle. Conservative rule applied."
                    : "Long trade stop loss was hit.");
        }

        if (takeProfitHit)
        {
            return Won(setup, candle.CloseTime, setup.TakeProfitPrice, "Long trade take profit was hit.");
        }

        return Active(setup);
    }

    private static TradeOutcome EvaluateShort(TradeSetup setup, Candle candle)
    {
        var stopLossHit = candle.High >= setup.StopLossPrice;
        var takeProfitHit = candle.Low <= setup.TakeProfitPrice;

        if (stopLossHit)
        {
            return Lost(setup, candle.CloseTime, setup.StopLossPrice,
                takeProfitHit
                    ? "Both stop loss and take profit were touched in the same candle. Conservative rule applied."
                    : "Short trade stop loss was hit.");
        }

        if (takeProfitHit)
        {
            return Won(setup, candle.CloseTime, setup.TakeProfitPrice, "Short trade take profit was hit.");
        }

        return Active(setup);
    }

    private static TradeOutcome Active(TradeSetup setup)
    {
        return new TradeOutcome
        {
            Setup = setup,
            Status = TradeOutcomeStatus.Active,
            ClosedAt = null,
            ExitPrice = null,
            RealizedRiskRewardRatio = null,
            Reasons = Array.Empty<string>()
        };
    }

    private static TradeOutcome Won(TradeSetup setup, DateTime closedAt, decimal exitPrice, string reason)
    {
        return new TradeOutcome
        {
            Setup = setup,
            Status = TradeOutcomeStatus.Won,
            ClosedAt = closedAt,
            ExitPrice = exitPrice,
            RealizedRiskRewardRatio = setup.RiskRewardRatio,
            Reasons = new[] { reason }
        };
    }

    private static TradeOutcome Lost(TradeSetup setup, DateTime closedAt, decimal exitPrice, string reason)
    {
        return new TradeOutcome
        {
            Setup = setup,
            Status = TradeOutcomeStatus.Lost,
            ClosedAt = closedAt,
            ExitPrice = exitPrice,
            RealizedRiskRewardRatio = -1m,
            Reasons = new[] { reason }
        };
    }
}