using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Reading;
using Microsoft.Extensions.DependencyInjection;

namespace FlightPlanExtractor.Core;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="IFlightDataExtractor"/> and the chapter extractors. An <see cref="IPdfReader"/>
    /// must be registered separately (e.g. AddPdfPigReader from the Pdf project), so the host decides
    /// which PDF library is used. Everything is stateless, hence singletons.
    /// </summary>
    public static IServiceCollection AddFlightDataExtraction(this IServiceCollection services)
    {
        services.AddSingleton<IChapterExtractor, OfpExtractor>();
        services.AddSingleton<IChapterExtractor, CrewBriefingExtractor>();
        services.AddSingleton<IFlightDataExtractor, FlightDataExtractor>();

        return services;
    }
}
