using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class RiskManagementService : IRiskManagementService
{
    private readonly RiskManagementOptions _options;

    public RiskManagementService(IOptions<RiskManagementOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        _options = options.Value;
    }

    public TradeRiskAssessment Evaluate(TradeRiskContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.PositionSize);

        ValidateOptions();

        if (context.TradesToday < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context.TradesToday),
                "Trades today cannot be negative.");
        }

        if (context.ActiveTrades < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(context.ActiveTrades),
                "Active trades cannot be negative.");
        }

        var rejectionReasons = new List<string>();

        if (context.TradesToday >= _options.MaximumTradesPerDay)
        {
            rejectionReasons.Add(
                $"Maximum trades per day of {_options.MaximumTradesPerDay} has been reached.");
        }

        if (context.DailyRealizedR <= -_options.MaximumDailyLossR)
        {
            rejectionReasons.Add(
                $"Maximum daily loss of {_options.MaximumDailyLossR}R has been reached.");
        }

        if (context.ActiveTrades >= _options.MaximumActiveTrades)
        {
            rejectionReasons.Add(
                $"Maximum active trades of {_options.MaximumActiveTrades} has been reached.");
        }

        return new TradeRiskAssessment
        {
            PositionSize = context.PositionSize,
            IsApproved = rejectionReasons.Count == 0,
            TradesToday = context.TradesToday,
            ActiveTrades = context.ActiveTrades,
            DailyRealizedR = context.DailyRealizedR,
            RejectionReasons = rejectionReasons
        };
    }

    private void ValidateOptions()
    {
        if (_options.MaximumTradesPerDay <= 0)
        {
            throw new InvalidOperationException(
                "Maximum trades per day must be greater than zero.");
        }

        if (_options.MaximumDailyLossR <= 0)
        {
            throw new InvalidOperationException(
                "Maximum daily loss R must be greater than zero.");
        }

        if (_options.MaximumActiveTrades <= 0)
        {
            throw new InvalidOperationException(
                "Maximum active trades must be greater than zero.");
        }
    }
}