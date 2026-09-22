using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Tests.Chapters;

/// <summary>The Crew Briefing chapter against the real sample document. Expected values were read off the PDF by hand.</summary>
public class CrewBriefingExtractorSampleTests
{
    private static readonly DateOnly Date = new(2024, 3, 19);

    private static readonly CrewMember[] CrewOfHbjvn =
    [
        new("Werner Trütsch", "CMD"),
        new("Luca Andrea Marchetti", "COP"),
        new("Helen Meier", "CAB"),
        new("Ena Ramic", "CAB"),
        new("Nico Verhelst", "SEN"),
    ];

    // Two names here wrap onto a second line in the PDF.
    private static readonly CrewMember[] CrewOfHbjvy =
    [
        new("Steve Krebs", "CMD"),
        new("Gregory Gillioz", "COP"),
        new("Luisa Quadros Vissotto", "CAB"),
        new("Rainer Sattler", "CAB"),
        new("Regine Kathrin Schumacher-Horn", "SEN"),
    ];

    private readonly CrewBriefingExtractor _extractor = new();

    public static TheoryData<int, string, string, int, int, int, double, CrewMember[]> Flights => new()
    {
        { 85, "LX1073", "SWR890M", 1, 26, 28916, 68.7, CrewOfHbjvn },
        { 89, "LX1613", "SWR2TM", 18, 50, 28822, 0, CrewOfHbjvy },
        { 93, "LX1072", "SWR2ET", 9, 31, 28916, 68.7, CrewOfHbjvn },
        { 97, "LX1612", "SWR612Q", 9, 42, 28822, 0, CrewOfHbjvy },
    };

    [Theory]
    [MemberData(nameof(Flights))]
    public void Extracts_every_field_of_every_flight(
        int pageNumber, string flightNumber, string callSign,
        int business, int economy, int dow, double doi, CrewMember[] crew)
    {
        var chapter = _extractor.Extract(SampleDocument.Page(pageNumber));

        Assert.Equal(new FlightKey(Date, flightNumber), chapter.Key);
        Assert.Equal(callSign, chapter.AtcCallSign);
        Assert.Empty(chapter.Diagnostics);

        var briefing = chapter.AddTo(new FlightData { Key = chapter.Key! }).CrewBriefing!;
        Assert.Equal(pageNumber, briefing.SourcePage);
        Assert.Equal(business, briefing.BusinessPassengers);
        Assert.Equal(economy, briefing.EconomyPassengers);
        Assert.Equal(dow, briefing.DryOperatingWeightKg);
        Assert.Equal((decimal)doi, briefing.DryOperatingIndex);
        Assert.Equal(crew, briefing.Crew);
    }

    [Fact]
    public void Recognizes_exactly_the_first_page_of_each_crew_briefing()
    {
        var recognized = SampleDocument.Pages.Where(_extractor.Recognizes).Select(p => p.Number);

        Assert.Equal([85, 89, 93, 97], recognized);
    }
}
