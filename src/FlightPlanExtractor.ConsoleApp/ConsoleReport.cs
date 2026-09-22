using FlightPlanExtractor.Core;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.ConsoleApp;

/// <summary>Prints an <see cref="ExtractionResult"/> for a human. Presentation only.</summary>
internal static class ConsoleReport
{
    private const string Missing = "-";

    public static void Write(string path, ExtractionResult result)
    {
        Console.WriteLine($"File: {path}");
        Console.WriteLine($"Flights: {result.Flights.Count}   " +
                          $"Problems: {result.Diagnostics.Count(IsProblem)}   " +
                          $"Notes: {result.Diagnostics.Count(d => !IsProblem(d))}");

        foreach (var flight in result.Flights)
            Write(flight);

        Write("Problems", result.Diagnostics.Where(IsProblem));
        Write("Notes (nothing to fix)", result.Diagnostics.Where(d => !IsProblem(d)));
    }

    private static bool IsProblem(Diagnostic diagnostic) => diagnostic.Severity > DiagnosticSeverity.Info;

    private static void Write(FlightData flight)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {flight.Key.FlightNumber}   {flight.Key.Date:yyyy-MM-dd} ===");

        if (flight.Ofp is { } ofp)
        {
            Console.WriteLine($"  OFP (page {ofp.SourcePage})");
            Row("Registration", ofp.Registration);
            Row("ATC call sign", ofp.AtcCallSign);
            Row("Route", $"{ofp.DepartureAirport ?? Missing} -> {ofp.DestinationAirport ?? Missing}");
            Row("Alternates", $"1: {ofp.Alternate1 ?? "none"}   2: {ofp.Alternate2 ?? "none"}");
            Row("Departure / arrival", $"{Text(ofp.DepartureTime)} / {Text(ofp.ArrivalTime)}");
            Row("Zero fuel mass", Text(ofp.ZeroFuelMassKg, "kg"));
            Row("To destination", $"{Text(ofp.TimeToDestination)}   {Text(ofp.FuelToDestinationTonnes, "t")}");
            Row("To alternate", $"{Text(ofp.TimeToAlternate)}   {Text(ofp.FuelToAlternateTonnes, "t")}");
            Row("Minimum fuel", Text(ofp.MinimumFuelTonnes, "t"));
            Row("First / last nav point", $"{ofp.FirstWaypoint ?? Missing} - {ofp.LastWaypoint ?? Missing}");
            Row("Gain / loss", ofp.GainLoss?.ToString("+0;-0") ?? Missing);
        }
        else
        {
            Console.WriteLine("  OFP: not found");
        }

        if (flight.CrewBriefing is { } briefing)
        {
            Console.WriteLine($"  Crew Briefing (page {briefing.SourcePage})");
            Row("Passengers C / Y", $"{Text(briefing.BusinessPassengers)} / {Text(briefing.EconomyPassengers)}");
            Row("Dry operating weight", Text(briefing.DryOperatingWeightKg, "kg"));
            Row("Dry operating index", Text(briefing.DryOperatingIndex));
            Row("Crew", briefing.Crew.Count == 0 ? Missing : "");
            foreach (var member in briefing.Crew)
                Console.WriteLine($"      {member.Function,-4} {member.Name}");
        }
        else
        {
            Console.WriteLine("  Crew Briefing: not found");
        }
    }

    private static void Write(string title, IEnumerable<Diagnostic> diagnostics)
    {
        var ordered = diagnostics.OrderByDescending(d => d.Severity).ToList();
        if (ordered.Count == 0) return;

        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");

        foreach (var d in ordered)
        {
            var where = string.Join("  ", new[]
            {
                d.Flight?.FlightNumber,
                d.PageNumber is null ? null : $"p.{d.PageNumber}",
                d.Field,
            }.Where(part => part is not null));

            Console.WriteLine($"  [{d.Severity}] {d.Code}  {where}");
            Console.WriteLine($"      {d.Message}");
        }
    }

    private static void Row(string label, string? value) => Console.WriteLine($"    {label,-24}{value ?? Missing}");

    private static string Text(TimeOnly? time) => time?.ToString("HH:mm") ?? Missing;

    private static string Text(TimeSpan? duration) => duration?.ToString(@"h\:mm") ?? Missing;

    private static string Text(int? number, string unit = "") => number is null ? Missing : $"{number} {unit}".TrimEnd();

    private static string Text(decimal? number, string unit = "") =>
        number is null ? Missing : $"{number.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)} {unit}".TrimEnd();
}
