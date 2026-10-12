using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IRiskManagementService
{
    TradeRiskAssessment Evaluate(TradeRiskContext context);
}