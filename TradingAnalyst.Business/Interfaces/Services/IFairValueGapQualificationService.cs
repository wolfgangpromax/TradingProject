using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapQualificationService
{
    FairValueGapQualification Qualify(FairValueGapState state, FairValueGapQualificationContext context);
}