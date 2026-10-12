using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TradingAnalyst.Business.Configuration;
using TradingAnalyst.Business.Interfaces.Services;
using TradingAnalyst.Business.Services;

namespace TradingAnalyst.Business.DependencyInjection;

public static class BusinessServiceCollectionExtensions
{
    public static IServiceCollection AddBusiness(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TradingUniverseOptions>(configuration.GetSection(TradingUniverseOptions.SectionName));
        services.Configure<FairValueGapQualificationOptions>(configuration.GetSection("Trading:FairValueGapQualification"));
        services.Configure<TradeSetupOptions>(configuration.GetSection("TradeSetup"));
        services.Configure<RiskManagementOptions>(configuration.GetSection("RiskManagement"));

        services.AddScoped<IMarketDataService, MarketDataService>();
        services.AddScoped<ICandleContinuityService, CandleContinuityService>();
        services.AddScoped<ISwingDetectionService, SwingDetectionService>();
        services.AddScoped<IMarketStructureService, MarketStructureService>();
        services.AddScoped<IMarketTrendService, MarketTrendService>();
        services.AddScoped<IStructureBreakDetectionService, StructureBreakDetectionService>();
        services.AddScoped<IMarketStructureAnalysisService, MarketStructureAnalysisService>();
        services.AddScoped<IMarketStructureReplayService, MarketStructureReplayService>();
        services.AddSingleton<IFairValueGapDetectionService, FairValueGapDetectionService>();
        services.AddSingleton<IFairValueGapMitigationService, FairValueGapMitigationService>();
        services.AddSingleton<IFairValueGapReplayService, FairValueGapReplayService>();
        services.AddSingleton<IFairValueGapQualificationService, FairValueGapQualificationService>();
        services.AddSingleton<IAverageTrueRangeService, AverageTrueRangeService>();
        services.AddScoped<IFairValueGapQualificationReplayService, FairValueGapQualificationReplayService>();
        services.AddSingleton<IFairValueGapAlignmentService, FairValueGapAlignmentService>();
        services.AddSingleton<IFairValueGapCandidateService, FairValueGapCandidateService>();
        services.AddSingleton<IFairValueGapInteractionService, FairValueGapInteractionService>();
        services.AddSingleton<IFairValueGapEntryConfirmationService, FairValueGapEntryConfirmationService>();
        services.AddSingleton<ITradeSetupService, TradeSetupService>();
        services.AddSingleton<ITradeOutcomeService, TradeOutcomeService>();
        services.AddSingleton<IPositionSizingService, PositionSizingService>();
        services.AddSingleton<IRiskManagementService, RiskManagementService>();
        services.AddSingleton<ITradeExecutionService, TradeExecutionService>();
        return services;
    }
}