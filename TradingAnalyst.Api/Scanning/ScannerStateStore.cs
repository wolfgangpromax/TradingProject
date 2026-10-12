using System.Collections.Concurrent;

namespace TradingAnalyst.Api.Scanning;

public sealed record ScannerRow(string Symbol, string Status, string? Direction, string ContextTrend, decimal CurrentPrice, string MissingCondition, DateTime UpdatedAt);

public sealed class ScannerStateStore
{
    private readonly ConcurrentDictionary<string, ScannerRow> _rows = new(StringComparer.OrdinalIgnoreCase);
    public void Upsert(ScannerRow row) => _rows[row.Symbol] = row;
    public IReadOnlyCollection<ScannerRow> GetAll() => _rows.Values.OrderBy(x => Rank(x.Status)).ThenBy(x => x.Symbol).ToList();
    private static int Rank(string status) => status switch { "ENTRY_CONFIRMED" => 0, "WAITING_FOR_ENTRY" => 1, "WATCHING_SETUP" => 2, "SCANNING" => 3, _ => 4 };
}
