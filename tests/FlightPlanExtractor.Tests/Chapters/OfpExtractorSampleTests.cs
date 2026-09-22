using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Tests.Chapters;

/// <summary>The OFP chapter against the real sample document. Expected values were read off the PDF by hand.</summary>
public class OfpExtractorSampleTests
{
    private static readonly DateOnly Date = new(2024, 3, 19);
    private readonly OfpExtractor _extractor = new();

    public static TheoryData<string, OperationalFlightPlan> Flights => new()
    {
        {
            "LX1612", new OperationalFlightPlan
            {
                SourcePage = 5, Registration = "HBJVY", AtcCallSign = "SWR612Q",
                DepartureAirport = "LSZH", DestinationAirport = "LIMC", Alternate1 = "LIML", Alternate2 = null,
                DepartureTime = new TimeOnly(8, 0), ArrivalTime = new TimeOnly(8, 55),
                ZeroFuelMassKg = 34066,
                TimeToDestination = new TimeSpan(0, 48, 0), FuelToDestinationTonnes = 1.7m,
                TimeToAlternate = new TimeSpan(0, 20, 0), FuelToAlternateTonnes = 0.8m,
                MinimumFuelTonnes = 3.6m,
                FirstWaypoint = "VEBIT", LastWaypoint = "RIXUV", GainLoss = 0,
            }
        },
        {
            "LX1613", new OperationalFlightPlan
            {
                SourcePage = 12, Registration = "HBJVY", AtcCallSign = "SWR2TM",
                DepartureAirport = "LIMC", DestinationAirport = "LSZH", Alternate1 = "LFSB", Alternate2 = null,
                DepartureTime = new TimeOnly(9, 40), ArrivalTime = new TimeOnly(10, 45),
                ZeroFuelMassKg = 35337,
                TimeToDestination = new TimeSpan(0, 48, 0), FuelToDestinationTonnes = 1.7m,
                TimeToAlternate = new TimeSpan(0, 39, 0), FuelToAlternateTonnes = 1.5m,
                MinimumFuelTonnes = 4.3m,
                FirstWaypoint = "PEPAG", LastWaypoint = "KELIP", GainLoss = -40,
            }
        },
        {
            "LX1072", new OperationalFlightPlan
            {
                SourcePage = 19, Registration = "HBJVN", AtcCallSign = "SWR2ET",
                DepartureAirport = "LSZH", DestinationAirport = "EDDF", Alternate1 = "EDDK", Alternate2 = null,
                DepartureTime = new TimeOnly(11, 15), ArrivalTime = new TimeOnly(12, 20),
                ZeroFuelMassKg = 33107,
                TimeToDestination = new TimeSpan(0, 57, 0), FuelToDestinationTonnes = 1.9m,
                TimeToAlternate = new TimeSpan(0, 47, 0), FuelToAlternateTonnes = 1.6m,
                MinimumFuelTonnes = 4.6m,
                FirstWaypoint = "DEGES", LastWaypoint = "SPESA", GainLoss = -96,
            }
        },
        {
            "LX1073", new OperationalFlightPlan
            {
                SourcePage = 26, Registration = "HBJVN", AtcCallSign = "SWR890M",
                DepartureAirport = "EDDF", DestinationAirport = "LSZH", Alternate1 = "LFSB", Alternate2 = null,
                DepartureTime = new TimeOnly(13, 45), ArrivalTime = new TimeOnly(14, 40),
                ZeroFuelMassKg = 31536,
                TimeToDestination = new TimeSpan(0, 40, 0), FuelToDestinationTonnes = 1.4m,
                TimeToAlternate = new TimeSpan(0, 44, 0), FuelToAlternateTonnes = 1.7m,
                MinimumFuelTonnes = 4.2m,
                FirstWaypoint = "ANEKI", LastWaypoint = "LAMGO", GainLoss = 60,
            }
        },
    };

    [Theory]
    [MemberData(nameof(Flights))]
    public void Extracts_every_field_of_every_flight(string flightNumber, OperationalFlightPlan expected)
    {
        var chapter = _extractor.Extract(SampleDocument.Page(expected.SourcePage));

        Assert.Equal(new FlightKey(Date, flightNumber), chapter.Key);

        // Records compare by value, so this checks all fields at once.
        var flight = chapter.AddTo(new FlightData { Key = chapter.Key! });
        Assert.Equal(expected, flight.Ofp);

        // The only thing to report is the empty ALTN2.
        var diagnostic = Assert.Single(chapter.Diagnostics);
        Assert.Equal("Ofp.Alternate2", diagnostic.Field);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
    }

    [Fact]
    public void Recognizes_exactly_the_first_page_of_each_ofp()
    {
        var recognized = SampleDocument.Pages.Where(_extractor.Recognizes).Select(p => p.Number);

        Assert.Equal([5, 12, 19, 26], recognized);
    }
}
