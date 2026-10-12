export type Candle = { openTime:string; closeTime:string; open:number; high:number; low:number; close:number; volume:number; isClosed:boolean };
export type ChartSwing = { type:string; price:number; openTime:string; confirmedAt:string };
export type ChartBreak = { type:string; direction:string; brokenPrice:number; breakTime:string; breakClosePrice:number };
export type LiveAnalysis = {
  exchange:string; symbol:string; strategy:{name:string;contextTimeframe:string;setupTimeframe:string;entryTimeframe:string}; chartTimeframe:string;
  currentPrice:number; updatedAt:string; status:string; direction?:string; contextTrend:string; setupTrend:string; entryTrend:string;
  checklist:{contextAligned:boolean;liquiditySweep:boolean;displacement:boolean;qualifiedFvg:boolean;fvgInteraction:boolean;entryStructureConfirmation:boolean};
  liquidity:{swept:boolean;side:string;level?:number;time?:string};
  setup?:{type:string;lowerPrice:number;upperPrice:number;confirmedAt:string;gapSize:number;fillPercentage:number;interactionTime?:string;interactionPrice?:number;existingFiveMinuteConfirmation?:string;entryConfirmationTime?:string;entryConfirmationType?:string;entryConfirmationPrice?:number};
  tradeReference?:{entry:number;technicalInvalidation:number;riskDistance:number;target1R:number;target2R:number;opposingLiquidity?:number;outcome:string};
  missingConditions:string[]; explanation:string;
  chart:{candles:Candle[];swings:ChartSwing[];structureBreaks:ChartBreak[];activeFvg?:{lowerPrice:number;upperPrice:number;type:string;confirmedAt:string};entryMarker?:{time:string;price:number;type:string}};
};
export type ScannerRow = { symbol:string; status:string; direction?:string; contextTrend:string; currentPrice:number; missingCondition:string; updatedAt:string };
export type ScannerSnapshot = { discordConfigured:boolean; rows:ScannerRow[] };
