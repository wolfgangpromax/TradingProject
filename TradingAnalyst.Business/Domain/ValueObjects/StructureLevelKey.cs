using TradingAnalyst.Business.Domain.Enums;

namespace TradingAnalyst.Business.Domain.ValueObjects;

public readonly record struct StructureLevelKey(DateTime OpenTime, SwingType SwingType, decimal Price);