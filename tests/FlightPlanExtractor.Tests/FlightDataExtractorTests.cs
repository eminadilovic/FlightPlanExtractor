using FlightPlanExtractor.Core;
using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Model;
using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Tests;

/// <summary>
/// The whole pipeline with a fake reader, so broken or rearranged documents can be written as text.
/// </summary>
public class FlightDataExtractorTests
{
    private static readonly FlightKey Lx1612 = new(new DateOnly(2024, 3, 19), "LX1612");
    private static readonly FlightKey Lx1613 = new(new DateOnly(2024, 3, 19), "LX1613");

    [Fact]
    public void Joins_chapters_by_flight_even_when_they_come_in_a_different_order()
    {
        var result = Extract(
            Ofp(1, "LX1612", "SWR612Q"),
            Ofp(2, "LX1613", "SWR2TM"),
            Irrelevant(3),
            CrewBriefing(4, "LX1613", "SWR2TM", captain: "Steve Krebs"),
            CrewBriefing(5, "LX1612", "SWR612Q", captain: "Werner Trütsch"));

        Assert.Equal([Lx1612, Lx1613], result.Flights.Select(f => f.Key));
        Assert.Equal((1, 5), (result.Flights[0].Ofp!.SourcePage, result.Flights[0].CrewBriefing!.SourcePage));
        Assert.Equal((2, 4), (result.Flights[1].Ofp!.SourcePage, result.Flights[1].CrewBriefing!.SourcePage));
        Assert.Equal("Werner Trütsch", result.Flights[0].CrewBriefing!.Crew[0].Name);
        Assert.False(result.HasErrors);
    }

    [Fact]
    public void Unreadable_document_is_a_fatal_diagnostic_not_an_exception()
    {
        var extractor = NewExtractor(new FakePdfReader(new DocumentUnreadableException("Not a PDF.")));

        var result = extractor.Extract(Stream.Null);

        Assert.Empty(result.Flights);
        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal((DiagnosticSeverity.Fatal, DiagnosticCode.DocumentUnreadable), (diagnostic.Severity, diagnostic.Code));
    }

    [Fact]
    public void Document_without_any_known_page_is_a_fatal_diagnostic()
    {
        var result = Extract(Irrelevant(1), Irrelevant(2));

        Assert.Empty(result.Flights);
        Assert.Equal(DiagnosticCode.NoRelevantPagesFound, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Flight_without_crew_briefing_is_returned_with_what_there_is()
    {
        var result = Extract(Ofp(1, "LX1612", "SWR612Q"));

        var flight = Assert.Single(result.Flights);
        Assert.NotNull(flight.Ofp);
        Assert.Null(flight.CrewBriefing);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCode.ChapterMissing);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal(Lx1612, diagnostic.Flight);
        Assert.Contains("Crew Briefing", diagnostic.Message);
    }

    [Fact]
    public void One_unreadable_field_costs_only_that_field()
    {
        var broken = Ofp(1, "LX1612", "SWR612Q", zfmLine: "ZFM: 34.066 t _______ 40800 kg");

        var result = Extract(broken, Ofp(2, "LX1613", "SWR2TM"));

        Assert.Null(result.Flights[0].Ofp!.ZeroFuelMassKg);
        Assert.Equal("LIMC", result.Flights[0].Ofp!.DestinationAirport);
        Assert.Equal(34066, result.Flights[1].Ofp!.ZeroFuelMassKg);

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCode.ValueFormatMismatch && d.Severity == DiagnosticSeverity.Error);
        Assert.Equal("Ofp.ZeroFuelMassKg", diagnostic.Field);
        Assert.Equal(Lx1612, diagnostic.Flight);
        Assert.Equal(1, diagnostic.PageNumber);
        Assert.Equal("34.066 t _______ 40800 kg", diagnostic.RawValue);
    }

    [Fact]
    public void Renamed_label_costs_only_that_field()
    {
        // Confirmed by the team as the expected kind of change: the text that identifies a field changes.
        var renamed = Ofp(1, "LX1612", "SWR612Q", zfmLine: "ZFW: 34066 kg _______ 40800 kg");

        var result = Extract(renamed, CrewBriefing(2, "LX1612", "SWR612Q"));

        var flight = Assert.Single(result.Flights);
        Assert.Null(flight.Ofp!.ZeroFuelMassKg);
        Assert.Equal(3.6m, flight.Ofp.MinimumFuelTonnes);
        Assert.NotNull(flight.CrewBriefing);

        var diagnostic = Assert.Single(result.DiagnosticsFor(Lx1612), d => d.Severity == DiagnosticSeverity.Error);
        Assert.Equal((DiagnosticCode.LabelNotFound, "Ofp.ZeroFuelMassKg"), (diagnostic.Code, diagnostic.Field));
    }

    [Fact]
    public void Chapter_whose_flight_cannot_be_identified_is_reported_and_left_out()
    {
        var result = Extract(
            Ofp(1, "LX1612", "SWR612Q"),
            CrewBriefing(2, "LX1612", "SWR612Q", dateText: "??.???.????"));

        Assert.Null(Assert.Single(result.Flights).CrewBriefing);
        var diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCode.FlightKeyUnreadable);
        Assert.Equal(2, diagnostic.PageNumber);
    }

    [Fact]
    public void Different_call_signs_in_the_chapters_of_a_flight_are_a_warning()
    {
        var result = Extract(
            Ofp(1, "LX1612", "SWR612Q"),
            CrewBriefing(2, "LX1612", "SWR999X"));

        var diagnostic = Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCode.ChaptersDisagree);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.Severity);
        Assert.Contains("SWR612Q", diagnostic.Message);
        Assert.Contains("SWR999X", diagnostic.Message);
    }

    [Fact]
    public void Second_ofp_for_the_same_flight_is_ignored_with_a_warning()
    {
        var result = Extract(
            Ofp(1, "LX1612", "SWR612Q"),
            Ofp(2, "LX1612", "SWR612Q"),
            CrewBriefing(3, "LX1612", "SWR612Q"));

        Assert.Equal(1, Assert.Single(result.Flights).Ofp!.SourcePage);
        Assert.Equal(2, Assert.Single(result.Diagnostics, d => d.Code == DiagnosticCode.DuplicateChapter).PageNumber);
    }

    private static ExtractionResult Extract(params PageContent[] pages) =>
        NewExtractor(new FakePdfReader(pages)).Extract(Stream.Null);

    private static FlightDataExtractor NewExtractor(IPdfReader reader) =>
        new(reader, [new OfpExtractor(), new CrewBriefingExtractor()]);

    private static PageContent Irrelevant(int page) => TestPage.FromLines(page, "WX for Flight Group", "LSZH 190520Z 25004KT");

    private static PageContent Ofp(int page, string flightNumber, string callSign, string zfmLine = "ZFM: 34066 kg _______ 40800 kg") =>
        TestPage.FromLines(page,
            "Helvetic Airways - E-Jet Operational Flight Plan",
            "Date: 19MAR24 STD: 08:00 STA: 08:55",
            "Reg.: HBJVY",
            "From: LSZH ZRH ZURICH",
            "To: LIMC MXP MILAN/MALPENSA",
            "ALTN1: LIML LIN MILAN/LINATE",
            "ALTN2: LIMF TRN TURIN",
            $"FltNr: {flightNumber}",
            $"ATC: {callSign}",
            zfmLine,
            "LIMC: 0:48 1.7 _____",
            "LIML: 0:20 0.8 _____",
            "MIN: 1:43 3.6",
            "Gain / Loss: GAIN 0$/TON",
            "To DEST: VEBIT T53 GERSA/N0401F230 RIXUV");

    private static PageContent CrewBriefing(
        int page, string flightNumber, string callSign, string captain = "Steve Krebs", string dateText = "19.Mar.2024") =>
        TestPage.FromLines(page,
            "Flight Assignment / Flight Crew Briefing",
            $"{dateText}  DEP  ARR  FLT   PAX   {flightNumber}/{callSign}",
            "             ZRH  MXP  00:55 9/42",
            "DOW: 28822kg",
            "DOI: 68.7",
            "Func  3LC  Name",
            $"CMD   KRE  {captain}",
            "X: Operating crewmember, T: Trainee");

    private sealed class FakePdfReader : IPdfReader
    {
        private readonly IReadOnlyList<PageContent>? _pages;
        private readonly DocumentUnreadableException? _failure;

        public FakePdfReader(IReadOnlyList<PageContent> pages) => _pages = pages;
        public FakePdfReader(DocumentUnreadableException failure) => _failure = failure;

        public IReadOnlyList<PageContent> Read(Stream pdf) => _pages ?? throw _failure!;
    }
}
