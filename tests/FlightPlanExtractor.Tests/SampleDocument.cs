using FlightPlanExtractor.Core.Reading;
using FlightPlanExtractor.Pdf;

namespace FlightPlanExtractor.Tests;

/// <summary>The sample PDF, read once and shared by all tests that need real pages.</summary>
internal static class SampleDocument
{
    private static readonly Lazy<IReadOnlyList<PageContent>> AllPages = new(() =>
    {
        using var pdf = File.OpenRead(SampleFiles.SamplePdf);
        return new PdfPigReader().Read(pdf);
    });

    public static IReadOnlyList<PageContent> Pages => AllPages.Value;

    public static PageContent Page(int number) => Pages[number - 1];
}
