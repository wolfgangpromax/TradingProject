using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class PositionSizingService : IPositionSizingService
{
    private readonly RiskManagementOptions _options;

    public PositionSizingService(IOptions<RiskManagementOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public PositionSize Calculate(TradeSetup setup, decimal accountEquity)
    {
        ArgumentNullException.ThrowIfNull(setup);

        if (accountEquity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(accountEquity),
                "Account equity must be greater than zero.");
        }

        if (_options.RiskPerTradePercentage <= 0)
        {
            throw new InvalidOperationException(
                "Risk per trade percentage must be greater than zero.");
        }

        if (_options.RiskPerTradePercentage > 100m)
        {
            throw new InvalidOperationException(
                "Risk per trade percentage cannot exceed 100%.");
        }

        if (setup.RiskPerUnit <= 0)
        {
            throw new InvalidOperationException(
                "Trade setup risk per unit must be greater than zero.");
        }

        var riskPercentage = _options.RiskPerTradePercentage;
        var accountRiskAmount = accountEquity * (riskPercentage / 100m);
        var quantity = accountRiskAmount / setup.RiskPerUnit;
        var positionValue = quantity * setup.EntryPrice;

        return new PositionSize
        {
            Setup = setup,
            AccountEquity = accountEquity,
            RiskPercentage = riskPercentage,
            AccountRiskAmount = accountRiskAmount,
            RiskPerUnit = setup.RiskPerUnit,
            Quantity = quantity,
            PositionValue = positionValue,
            Reasons =
            [
                $"Risking {riskPercentage}% of account equity.",
                $"Account risk amount is {accountRiskAmount}.",
                $"Position quantity is {quantity}."
            ]
        };
    }
}