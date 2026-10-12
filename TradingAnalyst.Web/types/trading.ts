export type Candle = { openTime:string; closeTime:string; open:number; high:number; low:number; close:number; volume:number; isClosed:boolean };
export type CandlesResponse = { exchange:string; symbol:string; timeframe:string; candles:Candle[] };
export type TradeSetup = { direction:string; entryPrice:number; stopLossPrice:number; takeProfitPrice:number; riskPerUnit:number; rewardPerUnit:number; riskRewardRatio:number; createdAt:string };
export type TradeOutcome = { direction:string; entryPrice:number; stopLossPrice:number; takeProfitPrice:number; status:string; closedAt?:string; exitPrice?:number; realizedRiskRewardRatio?:number; createdAt:string; reasons:string[] };
export type PositionSize = { direction:string; accountEquity:number; riskPercentage:number; accountRiskAmount:number; riskPerUnit:number; quantity:number; positionValue:number; createdAt:string; reasons:string[] };
export type RiskAssessment = { direction:string; isApproved:boolean; tradesToday:number; activeTrades:number; dailyRealizedR:number; createdAt:string; rejectionReasons:string[] };
export type ReplayResponse = {
  exchange:string; symbol:string; timeframe:string; candleCount:number; snapshotCount:number; currentAverageTrueRange?:number; currentMarketTrend:string;
  totalGapCount:number; activeGapCount:number; qualifiedActiveGapCount:number; alignedQualifiedActiveGapCount:number;
  waitingEntryConfirmationCount:number; confirmedEntryConfirmationCount:number; invalidatedEntryConfirmationCount:number; tradeSetupCount:number;
  activeTradeCount:number; wonTradeCount:number; lostTradeCount:number; approvedTradeCount:number; rejectedTradeCount:number;
  tradeSetups:TradeSetup[]; tradeOutcomes:TradeOutcome[]; positionSizes:PositionSize[]; tradeRiskAssessments:RiskAssessment[];
};
