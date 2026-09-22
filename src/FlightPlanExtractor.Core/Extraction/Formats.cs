using System.Globalization;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// The value formats that occur in the documents: accepted shapes plus the parser.
/// Boxing a null nullable gives a null object, which is how "parser said no" reaches the engine.
/// </summary>
public static class Formats
{
    public static readonly ValueFormat Integer = new(
        "a whole number",
        [@"\d+"],
        text => int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var number) ? number : null);

    /// <summary>With or without decimals ("68.7", "0").</summary>
    public static readonly ValueFormat Decimal = new(
        "a number",
        [@"\d+(\.\d+)?"],
        text => Parsers.Decimal(text));

    /// <summary>Fuel is always printed with decimals, which is what tells it apart from the time next to it.</summary>
    public static readonly ValueFormat Tonnes = new(
        "an amount in tonnes (e.g. 1.7)",
        [@"\d+\.\d+"],
        text => Parsers.Decimal(text));

    public static readonly ValueFormat Date = new(
        "a date (19MAR24 or 19.Mar.2024)",
        [@"\d{2}[A-Za-z]{3}\d{2}", @"\d{2}\.[A-Za-z]{3}\.\d{4}"],
        text => Parsers.Date(text));

    public static readonly ValueFormat Time = new(
        "a time (hh:mm)",
        [@"\d{2}:\d{2}"],
        text => Parsers.Time(text));

    public static readonly ValueFormat Duration = new(
        "a duration (h:mm)",
        [@"\d{1,2}:\d{2}"],
        text => Parsers.Duration(text));

    public static readonly ValueFormat IcaoAirport = new(
        "an ICAO airport code (4 letters)",
        [@"[A-Z]{4}"],
        text => text);

    /// <summary>Registrations, flight numbers, call signs: one word of capitals and digits.</summary>
    public static readonly ValueFormat Code = new(
        "a code of capital letters and digits",
        [@"[A-Z0-9]+"],
        text => text);

    /// <summary>Crew function codes: CMD, COP, CAB, SEN, ...</summary>
    public static readonly ValueFormat CrewFunction = new(
        "a crew function code (2-4 capital letters)",
        [@"[A-Z]{2,4}"],
        text => text);

    /// <summary>"28822kg" - the unit is printed right behind the number.</summary>
    public static readonly ValueFormat Kilograms = new(
        "a mass in kg (e.g. 28822kg)",
        [@"\d+kg", @"\d+"],
        text => Integer.Parse(text.Replace("kg", "")));

    /// <summary>One side of a "business/economy" passenger count like "18/50".</summary>
    public static ValueFormat PassengerCount(int part) => new(
        "passenger numbers as C/Y (e.g. 18/50)",
        [@"\d+/\d+"],
        text => Integer.Parse(text.Split('/')[part]));

    /// <summary>One side of the "LX1613/SWR2TM" identification: flight number (0) or ATC call sign (1).</summary>
    public static ValueFormat FlightIdentification(int part) => new(
        "flight number and call sign (e.g. LX1613/SWR2TM)",
        [@"[A-Z0-9]{2}\d{1,4}[A-Z]?/[A-Z]{3}[A-Z0-9]+"],
        text => text.Split('/')[part]);

    public static readonly ValueFormat GainLoss = new(
        "GAIN or LOSS with an amount (e.g. LOSS 40$/TON)",
        [@"[A-Z]+ \d+\$/TON"],
        text => Parsers.GainLoss(text));

    public static ValueFormat FirstWaypoint(IReadOnlyCollection<string> ignoredWords) => new(
        "a route with at least one navigation point",
        [".+"],
        route => Parsers.FirstWaypoint(route, ignoredWords));

    public static ValueFormat LastWaypoint(IReadOnlyCollection<string> ignoredWords) => new(
        "a route with at least one navigation point",
        [".+"],
        route => Parsers.LastWaypoint(route, ignoredWords));
}
