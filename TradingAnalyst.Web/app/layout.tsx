import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = { title: "TradingAnalyst", description: "AI-assisted crypto day-trading analyst" };
export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return <html lang="en"><body>{children}</body></html>;
}
