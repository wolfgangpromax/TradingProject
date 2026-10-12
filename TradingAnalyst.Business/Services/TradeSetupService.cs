using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class TradeSetupService : ITradeSetupService
{
    private readonly TradeSetupOptions _options;

    public TradeSetupService(IOptions<TradeSetupOptions> options)
    {
        _options = options.Value;
    }

    public TradeSetup? Create(TradeSetupContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var confirmation = context.EntryConfirmation;

        if (confirmation.Status != EntryConfirmationStatus.Confirmed ||
            !confirmation.ConfirmedAt.HasValue ||
            !confirmation.ConfirmationPrice.HasValue)
        {
            return null;
        }

        if (!context.AverageTrueRange.HasValue || context.AverageTrueRange.Value <= 0)
        {
            return null;
        }

        var gap = confirmation.Interaction.Candidate.State.Gap;
        var entryPrice = confirmation.ConfirmationPrice.Value;
        var atr = context.AverageTrueRange.Value;
        var stopLossBuffer = atr * _options.StopLossAtrBufferMultiplier;

        return gap.Type switch
        {
            FairValueGapType.Bullish => CreateLongSetup(confirmation, entryPrice, gap.LowerPrice, stopLossBuffer),
            FairValueGapType.Bearish => CreateShortSetup(confirmation, entryPrice, gap.UpperPrice, stopLossBuffer),
            _ => null
        };
    }

    private TradeSetup? CreateLongSetup(FairValueGapEntryConfirmation confirmation, decimal entryPrice, decimal lowerPrice, decimal stopLossBuffer)
    {
        var stopLossPrice = lowerPrice - stopLossBuffer;
        var riskPerUnit = entryPrice - stopLossPrice;

        if (riskPerUnit <= 0)
        {
            return null;
        }

        var rewardPerUnit = riskPerUnit * _options.MinimumRiskRewardRatio;
        var takeProfitPrice = entryPrice + rewardPerUnit;
        var riskRewardRatio = rewardPerUnit / riskPerUnit;

        return new TradeSetup
        {
            EntryConfirmation = confirmation,
            Direction = TradeDirection.Long,
            EntryPrice = entryPrice,
            StopLossPrice = stopLossPrice,
            TakeProfitPrice = takeProfitPrice,
            RiskPerUnit = riskPerUnit,
            RewardPerUnit = rewardPerUnit,
            RiskRewardRatio = riskRewardRatio,
            CreatedAt = confirmation.ConfirmedAt!.Value,
            Reasons = new[]
            {
                "Bullish FVG entry confirmation created a long trade setup."
            }
        };
    }

    private TradeSetup? CreateShortSetup(FairValueGapEntryConfirmation confirmation, decimal entryPrice, decimal upperPrice, decimal stopLossBuffer)
    {
        var stopLossPrice = upperPrice + stopLossBuffer;
        var riskPerUnit = stopLossPrice - entryPrice;

        if (riskPerUnit <= 0)
        {
            return null;
        }

        var rewardPerUnit = riskPerUnit * _options.MinimumRiskRewardRatio;
        var takeProfitPrice = entryPrice - rewardPerUnit;
        var riskRewardRatio = rewardPerUnit / riskPerUnit;

        return new TradeSetup
        {
            EntryConfirmation = confirmation,
            Direction = TradeDirection.Short,
            EntryPrice = entryPrice,
            StopLossPrice = stopLossPrice,
            TakeProfitPrice = takeProfitPrice,
            RiskPerUnit = riskPerUnit,
            RewardPerUnit = rewardPerUnit,
            RiskRewardRatio = riskRewardRatio,
            CreatedAt = confirmation.ConfirmedAt!.Value,
            Reasons = new[]
            {
                "Bearish FVG entry confirmation created a short trade setup."
            }
        };
    }
}