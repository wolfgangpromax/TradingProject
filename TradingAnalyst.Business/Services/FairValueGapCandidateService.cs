using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapCandidateService : IFairValueGapCandidateService
{
    public FairValueGapCandidate Evaluate(FairValueGapQualification qualification, FairValueGapAlignment alignment)
    {
        ArgumentNullException.ThrowIfNull(qualification);
        ArgumentNullException.ThrowIfNull(alignment);

        if (qualification.State.Gap.Key != alignment.State.Gap.Key)
        {
            throw new ArgumentException("Qualification and alignment must refer to the same FVG.");
        }

        var rejectionReasons = new List<string>();

        if (qualification.State.IsMitigated)
        {
            rejectionReasons.Add("FVG is fully mitigated.");
        }

        if (!qualification.IsQualified)
        {
            foreach (var reason in qualification.RejectionReasons)
            {
                if (!rejectionReasons.Contains(reason))
                {
                    rejectionReasons.Add(reason);
                }
            }
        }

        if (!alignment.IsAligned)
        {
            rejectionReasons.Add(alignment.Reason);
        }

        var isCandidate =
            !qualification.State.IsMitigated &&
            qualification.IsQualified &&
            alignment.IsAligned;

        return new FairValueGapCandidate
        {
            State = qualification.State,
            Qualification = qualification,
            Alignment = alignment,
            IsCandidate = isCandidate,
            RejectionReasons = rejectionReasons
        };
    }
}