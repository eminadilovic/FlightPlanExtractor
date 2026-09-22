using FlightPlanExtractor.Core.Extraction;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Core.Layouts;

/// <summary>
/// Field names every chapter layout must provide, because they identify the flight
/// (date + flight number) or are compared between chapters as a consistency check.
/// </summary>
public static class FlightKeyFields
{
    public const string Date = "Date";
    public const string FlightNumber = "FlightNumber";
    public const string Registration = "Registration";
    public const string AtcCallSign = "AtcCallSign";

    /// <returns>Null when date or flight number was not extracted.</returns>
    public static FlightKey? ReadKey(ExtractedValues values)
    {
        var date = values.Value<DateOnly>(Date);
        var flightNumber = values.Text(FlightNumber);

        return date is null || flightNumber is null ? null : new FlightKey(date.Value, flightNumber);
    }
}
