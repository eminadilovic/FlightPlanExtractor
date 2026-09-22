using FlightPlanExtractor.Core.Extraction;
using FlightPlanExtractor.Core.Model;
using static FlightPlanExtractor.Core.Extraction.Locate;

namespace FlightPlanExtractor.Core.Layouts;

/// <summary>
/// What we know about the first page of a Helvetic Airways OFP. Only data: when the document
/// changes (new label, new value format) or a new field is wanted, this is the file to edit.
/// </summary>
public static class HelveticOfpLayout
{
    /// <summary>A page is an OFP first page when it contains one of these.</summary>
    public static readonly IReadOnlyList<string> PageTitles = ["Operational Flight Plan"];

    /// <summary>Words of letters only that can appear in a route without being a navigation point.</summary>
    public static readonly IReadOnlyCollection<string> IgnoredRouteWords = ["DCT"];

    // Order matters where a field's label depends on an earlier field.
    public static readonly IReadOnlyList<FieldDefinition> Fields =
    [
        new(FlightKeyFields.Date,         After("Date:"),  Formats.Date),
        new(FlightKeyFields.FlightNumber, After("FltNr:"), Formats.Code),
        new(FlightKeyFields.Registration, After("Reg.:"),  Formats.Code),
        new(FlightKeyFields.AtcCallSign,  After("ATC:"),   Formats.Code),

        new(nameof(OperationalFlightPlan.DepartureAirport),   After("From:"),  Formats.IcaoAirport),
        new(nameof(OperationalFlightPlan.DestinationAirport), After("To:"),    Formats.IcaoAirport),
        new(nameof(OperationalFlightPlan.Alternate1),         After("ALTN1:"), Formats.IcaoAirport, Required: false),
        new(nameof(OperationalFlightPlan.Alternate2),         After("ALTN2:"), Formats.IcaoAirport, Required: false),

        new(nameof(OperationalFlightPlan.DepartureTime), After("STD:"), Formats.Time),
        new(nameof(OperationalFlightPlan.ArrivalTime),   After("STA:"), Formats.Time),

        new(nameof(OperationalFlightPlan.ZeroFuelMassKg), After("ZFM:"), Formats.Integer),

        // Fuel table rows are "<label> <time> <fuel>"; the row label is the airport code itself.
        new(nameof(OperationalFlightPlan.TimeToDestination),
            AfterValueOf(nameof(OperationalFlightPlan.DestinationAirport)).Within(2), Formats.Duration),
        new(nameof(OperationalFlightPlan.FuelToDestinationTonnes),
            AfterValueOf(nameof(OperationalFlightPlan.DestinationAirport)).Within(2), Formats.Tonnes),
        new(nameof(OperationalFlightPlan.TimeToAlternate),
            AfterValueOf(nameof(OperationalFlightPlan.Alternate1)).Within(2), Formats.Duration, Required: false),
        new(nameof(OperationalFlightPlan.FuelToAlternateTonnes),
            AfterValueOf(nameof(OperationalFlightPlan.Alternate1)).Within(2), Formats.Tonnes, Required: false),
        new(nameof(OperationalFlightPlan.MinimumFuelTonnes), After("MIN:").Within(2), Formats.Tonnes),

        new(nameof(OperationalFlightPlan.FirstWaypoint),
            After("To DEST:").RestOfLine(), Formats.FirstWaypoint(IgnoredRouteWords)),
        new(nameof(OperationalFlightPlan.LastWaypoint),
            After("To DEST:").RestOfLine(), Formats.LastWaypoint(IgnoredRouteWords)),

        new(nameof(OperationalFlightPlan.GainLoss), After("Gain / Loss:").RestOfLine(), Formats.GainLoss),
    ];
}
