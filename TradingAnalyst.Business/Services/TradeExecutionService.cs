using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class TradeExecutionService : ITradeExecutionService
{
    public TradeOrder CreateOrder(TradeRiskAssessment riskAssessment)
    {
        ArgumentNullException.ThrowIfNull(riskAssessment);

        if (!riskAssessment.IsApproved)
        {
            throw new InvalidOperationException(
                "Cannot create a trade order from a rejected risk assessment.");
        }

        var positionSize = riskAssessment.PositionSize;
        var setup = positionSize.Setup;

        if (positionSize.Quantity <= 0m)
        {
            throw new InvalidOperationException(
                "Position quantity must be greater than zero.");
        }

        if (setup.EntryPrice <= 0m)
        {
            throw new InvalidOperationException(
                "Entry price must be greater than zero.");
        }

        return new TradeOrder
        {
            RiskAssessment = riskAssessment,
            Direction = setup.Direction,
            OrderType = OrderType.Market,
            Quantity = positionSize.Quantity,
            RequestedPrice = setup.EntryPrice,
            CreatedAt = setup.CreatedAt,
            Status = OrderStatus.Pending,
            Reasons =
            [
                "Risk assessment approved.",
                "Market order created."
            ]
        };
    }

    public TradeExecution Execute(TradeOrder order, Candle candle)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(candle);

        if (order.Status != OrderStatus.Pending)
        {
            throw new InvalidOperationException(
                "Only pending orders can be executed.");
        }

        if (!candle.IsClosed)
        {
            return Pending(
                order,
                "Execution candle is not closed.");
        }

        if (candle.CloseTime <= order.CreatedAt)
        {
            return Pending(
                order,
                "Execution candle must occur after the order was created.");
        }

        return new TradeExecution
        {
            Order = order,
            Status = OrderStatus.Filled,
            FilledPrice = order.RequestedPrice,
            FilledQuantity = order.Quantity,
            ExecutedAt = candle.CloseTime,
            Reasons =
            [
                "Market order filled."
            ]
        };
    }

    private static TradeExecution Pending(
        TradeOrder order,
        string reason)
    {
        return new TradeExecution
        {
            Order = order,
            Status = OrderStatus.Pending,
            FilledPrice = null,
            FilledQuantity = 0m,
            ExecutedAt = null,
            Reasons =
            [
                reason
            ]
        };
    }
}