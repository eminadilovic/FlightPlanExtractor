using FlightPlanExtractor.Core;
using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Pdf;

namespace FlightPlanExtractor.Tests;

/// <summary>The real PDF through the real reader and the whole pipeline.</summary>
public class SampleDocumentEndToEndTests
{
    private readonly FlightDataExtractor _extractor = new(new PdfPigReader(), [new OfpExtractor(), new CrewBriefingExtractor()]);

    [Fact]
    public void Finds_the_four_flights_and_pairs_each_ofp_with_its_crew_briefing()
    {
        using var pdf = File.OpenRead(SampleFiles.SamplePdf);

        var result = _extractor.Extract(pdf);

        Assert.Equal(["LX1612", "LX1613", "LX1072", "LX1073"], result.Flights.Select(f => f.Key.FlightNumber));

        // The Crew Briefings are in the reverse order of the OFPs.
        Assert.Equal(
            [(5, 97), (12, 89), (19, 93), (26, 85)],
            result.Flights.Select(f => (f.Ofp!.SourcePage, f.CrewBriefing!.SourcePage)));

        // A quick check that the pairing makes sense: each flight got its own crew.
        Assert.Equal("Steve Krebs", result.Flights[0].CrewBriefing!.Crew[0].Name);
        Assert.Equal("Werner Trütsch", result.Flights[3].CrewBriefing!.Crew[0].Name);
    }

    [Fact]
    public void Reports_nothing_but_the_empty_second_alternates()
    {
        using var pdf = File.OpenRead(SampleFiles.SamplePdf);

        var result = _extractor.Extract(pdf);

        Assert.False(result.HasErrors);
        Assert.Equal(4, result.Diagnostics.Count);
        Assert.All(result.Diagnostics, d =>
        {
            Assert.Equal(DiagnosticSeverity.Info, d.Severity);
            Assert.Equal("Ofp.Alternate2", d.Field);
            Assert.NotNull(d.Flight);
        });
    }

    [Fact]
    public void Does_not_close_the_callers_stream()
    {
        using var pdf = File.OpenRead(SampleFiles.SamplePdf);

        _extractor.Extract(pdf);

        Assert.True(pdf.CanRead);
    }
}
