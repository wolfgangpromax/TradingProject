# TradingAnalyst.Web

Next.js dashboard for the existing TradingAnalyst ASP.NET Core API.

## Run

1. Start TradingAnalyst.Api (expected default in `.env.local`: `https://localhost:7172`).
2. Ensure BINANCE symbols and candles have been synchronized through Swagger.
3. In this folder run:

```powershell
npm.cmd install
npm.cmd run dev
```

Open http://localhost:3000.

## Modules

- Dashboard: live chart, current opportunity, setup watch summary, paper-account preview.
- Setup Watch: lifecycle/status and reason for waiting/rejection.
- Paper Portfolio: editable capital/risk/leverage and historical compounded ledger.
- Trades: historical replay outcomes.
- Backtesting: historical simulation controls and summary.
- Analytics: closed trades, win rate, total R, drawdown preview.
- Settings: simulation inputs.

## Important modeling boundary

The ASP.NET backend remains authoritative for strategy, setup, risk and replay decisions. The editable paper account is currently a **web-side historical preview** derived from backend realized R. Leverage is displayed as a scenario setting but margin, liquidation, fees, slippage and exchange-specific leverage rules are **not yet modeled**. Those should be added to the backend Account/Portfolio State before this UI is treated as an execution simulator.
