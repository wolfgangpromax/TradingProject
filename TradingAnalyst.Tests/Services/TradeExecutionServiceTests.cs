using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Domain.Enums;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Tests.Services;

public sealed class TradeExecutionServiceTests
{
    private readonly TradeExecutionService _service = new();

    [Fact]
    public void CreateOrder_ApprovedRiskAssessment_CreatesPendingMarketOrder()
    {
        var riskAssessment = CreateRiskAssessment(isApproved: true);

        var order = _service.CreateOrder(riskAssessment);

        Assert.Same(riskAssessment, order.RiskAssessment);
        Assert.Equal(TradeDirection.Long, order.Direction);
        Assert.Equal(OrderType.Market, order.OrderType);
        Assert.Equal(0.5m, order.Quantity);
        Assert.Equal(100m, order.RequestedPrice);
        Assert.Equal(riskAssessment.PositionSize.Setup.CreatedAt, order.CreatedAt);
        Assert.Equal(OrderStatus.Pending, order.Status);
    }

    [Fact]
    public void CreateOrder_RejectedRiskAssessment_Throws()
    {
        var riskAssessment = CreateRiskAssessment(isApproved: false);

        Assert.Throws<InvalidOperationException>(() =>
            _service.CreateOrder(riskAssessment));
    }

    [Fact]
    public void Execute_NextClosedCandle_FillsOrder()
    {
        var order = _service.CreateOrder(CreateRiskAssessment(isApproved: true));

        var candle = CreateCandle(
            order.CreatedAt.AddMinutes(5),
            isClosed: true);

        var execution = _service.Execute(order, candle);

        Assert.Equal(OrderStatus.Filled, execution.Status);
        Assert.Equal(order.RequestedPrice, execution.FilledPrice);
        Assert.Equal(order.Quantity, execution.FilledQuantity);
        Assert.Equal(candle.CloseTime, execution.ExecutedAt);
    }

    [Fact]
    public void Execute_SameCreationTime_DoesNotFillOrder()
    {
        var order = _service.CreateOrder(CreateRiskAssessment(isApproved: true));

        var candle = CreateCandle(
            order.CreatedAt,
            isClosed: true);

        var execution = _service.Execute(order, candle);

        Assert.Equal(OrderStatus.Pending, execution.Status);
        Assert.Null(execution.FilledPrice);
        Assert.Equal(0m, execution.FilledQuantity);
        Assert.Null(execution.ExecutedAt);
    }

    [Fact]
    public void Execute_CandleBeforeOrder_DoesNotFillOrder()
    {
        var order = _service.CreateOrder(CreateRiskAssessment(isApproved: true));

        var candle = CreateCandle(
            order.CreatedAt.AddMinutes(-5),
            isClosed: true);

        var execution = _service.Execute(order, candle);

        Assert.Equal(OrderStatus.Pending, execution.Status);
        Assert.Null(execution.FilledPrice);
        Assert.Equal(0m, execution.FilledQuantity);
        Assert.Null(execution.ExecutedAt);
    }

    [Fact]
    public void Execute_UnclosedCandle_DoesNotFillOrder()
    {
        var order = _service.CreateOrder(CreateRiskAssessment(isApproved: true));

        var candle = CreateCandle(
            order.CreatedAt.AddMinutes(5),
            isClosed: false);

        var execution = _service.Execute(order, candle);

        Assert.Equal(OrderStatus.Pending, execution.Status);
        Assert.Null(execution.FilledPrice);
        Assert.Equal(0m, execution.FilledQuantity);
        Assert.Null(execution.ExecutedAt);
    }

    [Fact]
    public void Execute_NonPendingOrder_Throws()
    {
        var pendingOrder = _service.CreateOrder(CreateRiskAssessment(isApproved: true));

        var order = new TradeOrder
        {
            RiskAssessment = pendingOrder.RiskAssessment,
            Direction = pendingOrder.Direction,
            OrderType = pendingOrder.OrderType,
            Quantity = pendingOrder.Quantity,
            RequestedPrice = pendingOrder.RequestedPrice,
            CreatedAt = pendingOrder.CreatedAt,
            Status = OrderStatus.Filled,
            Reasons = pendingOrder.Reasons
        };

        var candle = CreateCandle(
            order.CreatedAt.AddMinutes(5),
            isClosed: true);

        Assert.Throws<InvalidOperationException>(() =>
            _service.Execute(order, candle));
    }

    private static TradeRiskAssessment CreateRiskAssessment(bool isApproved)
    {
        var createdAt = new DateTime(
            2026, 9, 19, 10, 0, 0,
            DateTimeKind.Utc);

        var gap = new FairValueGap
        {
            Type = FairValueGapType.Bullish,
            StartTime = createdAt.AddMinutes(-25),
            ConfirmedAt = createdAt.AddMinutes(-15),
            LowerPrice = 95m,
            UpperPrice = 98m,
            GapSize = 3m
        };

        var gapState = new FairValueGapState(gap);

        var qualification = new FairValueGapQualification
        {
            State = gapState.CreateSnapshot(),
            IsQualified = true,
            RejectionReasons = []
        };

        var alignment = new FairValueGapAlignment
        {
            State = gapState.CreateSnapshot(),
            MarketTrend = MarketTrend.Bullish,
            IsAligned = true,
            Reason = "Aligned."
        };

        var candidate = new FairValueGapCandidate
        {
            State = gapState.CreateSnapshot(),
            Qualification = qualification,
            Alignment = alignment,
            IsCandidate = true,
            RejectionReasons = []
        };

        var interactionState = new FairValueGapInteractionState(candidate);
        interactionState.RecordInteraction(
            createdAt.AddMinutes(-5),
            97m);

        var confirmation = new FairValueGapEntryConfirmation
        {
            Interaction = interactionState.CreateSnapshot(),
            Status = EntryConfirmationStatus.Confirmed,
            ConfirmedAt = createdAt,
            ConfirmationPrice = 100m,
            Reasons = ["Bullish entry confirmed."]
        };

        var setup = new TradeSetup
        {
            EntryConfirmation = confirmation,
            Direction = TradeDirection.Long,
            EntryPrice = 100m,
            StopLossPrice = 98m,
            TakeProfitPrice = 104m,
            RiskPerUnit = 2m,
            RewardPerUnit = 4m,
            RiskRewardRatio = 2m,
            CreatedAt = createdAt,
            Reasons = []
        };

        var positionSize = new PositionSize
        {
            Setup = setup,
            AccountEquity = 10_000m,
            RiskPercentage = 1m,
            AccountRiskAmount = 100m,
            RiskPerUnit = 2m,
            Quantity = 0.5m,
            PositionValue = 50m,
            Reasons = []
        };

        return new TradeRiskAssessment
        {
            PositionSize = positionSize,
            IsApproved = isApproved,
            TradesToday = 0,
            ActiveTrades = 0,
            DailyRealizedR = 0m,
            RejectionReasons = isApproved ? [] : ["Risk rejected."]
        };
    }

    private static Candle CreateCandle(
        DateTime closeTime,
        bool isClosed)
    {
        return new Candle
        {
            OpenTime = closeTime.AddMinutes(-5),
            CloseTime = closeTime,
            Open = 100m,
            High = 105m,
            Low = 95m,
            Close = 102m,
            Volume = 100m,
            IsClosed = isClosed
        };
    }
}