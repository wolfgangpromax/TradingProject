import type { LiveAnalysis,ScannerSnapshot } from "@/types/live";
const API = process.env.NEXT_PUBLIC_API_URL ?? "https://localhost:7172";
async function request<T>(path:string, init?:RequestInit):Promise<T>{const r=await fetch(`${API}${path}`,{cache:"no-store",...init});if(!r.ok)throw new Error(await r.text()||`HTTP ${r.status}`);return r.json() as Promise<T>}
export const liveApi={
  analyze:(symbol:string,chartTimeframe:number)=>request<LiveAnalysis>(`/api/live-analysis/${encodeURIComponent(symbol)}?chartTimeframe=${chartTimeframe}&limit=500`),
  refresh:(symbol:string)=>request<any>(`/api/live-analysis/${encodeURIComponent(symbol)}/refresh`,{method:"POST"}),
  symbols:(search="")=>request<{symbol:string;baseAsset:string;quoteAsset:string}[]>(`/api/live-analysis/symbols?search=${encodeURIComponent(search)}&limit=100`),
  scanner:()=>request<ScannerSnapshot>(`/api/scanner`),
  testDiscord:()=>request<{sent:boolean}>(`/api/scanner/discord/test`,{method:"POST"}),
  syncSymbols:()=>request<any>(`/api/market-data/symbols/sync`,{method:"POST"})
};
