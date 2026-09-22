using FlightPlanExtractor.Core.Reading;
using Microsoft.Extensions.DependencyInjection;

namespace FlightPlanExtractor.Pdf;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPdfPigReader(this IServiceCollection services) =>
        services.AddSingleton<IPdfReader, PdfPigReader>();
}
