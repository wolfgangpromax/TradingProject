import type { CandlesResponse, ReplayResponse } from "@/types/trading";
const API = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5244";
async function get<T>(path:string):Promise<T> {
  const response = await fetch(`${API}${path}`, { cache:"no-store" });
  if (!response.ok) throw new Error(await response.text() || `HTTP ${response.status}`);
  return response.json() as Promise<T>;
}
export const tradingApi = {
  candles:(symbol:string,timeframe:number,limit=300) => get<CandlesResponse>(`/api/candles/${symbol}?timeframe=${timeframe}&limit=${limit}`),
  replay:(symbol:string,timeframe:number,limit=500,atrPeriod=14) => get<ReplayResponse>(`/api/market-data/analysis/${symbol}/fvg-qualification-replay?timeframe=${timeframe}&limit=${limit}&atrPeriod=${atrPeriod}`),
  backtest:(symbol:string,timeframe:number,capital:number,risk:number,leverage:number,limit=500) => get<any>(`/api/strategy/backtest/${symbol}?timeframe=${timeframe}&limit=${limit}&startingCapital=${capital}&riskPercentage=${risk}&leverage=${leverage}`),
  watch:(timeframe:number,limit=500) => get<any[]>(`/api/strategy/watch?symbols=BTCUSDT,ETHUSDT,SOLUSDT&timeframe=${timeframe}&limit=${limit}`),
  multiTimeframe:(symbol:string,limit=500) => get<any>(`/api/strategy/multi-timeframe/${symbol}?limit=${limit}`),
  integrations:() => get<any>(`/api/strategy/integrations`)
};
