using Microsoft.Extensions.Options;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Domain.Entities;
using TradingAnalyst.Business.Interfaces.Services;

namespace TradingAnalyst.Business.Services;

public sealed class FairValueGapQualificationService
    : IFairValueGapQualificationService
{
    private readonly FairValueGapQualificationOptions _options;

    public FairValueGapQualificationService(
        IOptions<FairValueGapQualificationOptions> options)
    {
        _options = options.Value;
    }

    public FairValueGapQualification Qualify(
        FairValueGapState state,
        FairValueGapQualificationContext context)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(context);

        var reasons = new List<string>();

        if (state.IsMitigated)
        {
            reasons.Add("FVG is fully mitigated.");
        }

        if (state.Gap.GapSize <= 0m)
        {
            reasons.Add("FVG has an invalid gap size.");
        }

        if (state.Gap.ConfirmedAt > context.CurrentTime)
        {
            reasons.Add("FVG is not confirmed yet.");
        }

        if (context.AverageTrueRange is > 0m &&
            state.Gap.GapSize > 0m)
        {
            var gapAtrRatio =
                state.Gap.GapSize /
                context.AverageTrueRange.Value;

            if (gapAtrRatio < _options.MinimumGapAtrRatio)
            {
                reasons.Add("FVG is too small relative to ATR.");
            }
        }

        return new FairValueGapQualification
        {
            State = state,
            IsQualified = reasons.Count == 0,
            RejectionReasons = reasons
        };
    }
}