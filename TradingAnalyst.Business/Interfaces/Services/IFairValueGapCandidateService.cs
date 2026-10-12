using TradingAnalyst.Business.Domain.Entities;

namespace TradingAnalyst.Business.Interfaces.Services;

public interface IFairValueGapCandidateService
{
    FairValueGapCandidate Evaluate(FairValueGapQualification qualification, FairValueGapAlignment alignment);
}