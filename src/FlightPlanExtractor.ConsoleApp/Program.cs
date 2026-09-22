// Sample host: shows how an application registers and calls the extraction module.
//
//   dotnet run --project src/FlightPlanExtractor.ConsoleApp -- <file.pdf> [--json]
//
// Exit code: 0 = extracted without errors, 1 = extracted with errors, 2 = nothing extracted / bad usage.
using System.Text.Json;
using System.Text.Json.Serialization;
using FlightPlanExtractor.ConsoleApp;
using FlightPlanExtractor.Core;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Pdf;
using Microsoft.Extensions.DependencyInjection;

var path = args.FirstOrDefault(a => !a.StartsWith("--"));
var asJson = args.Contains("--json");

if (path is null)
{
    Console.Error.WriteLine("Usage: FlightPlanExtractor.ConsoleApp <file.pdf> [--json]");
    return 2;
}

if (!File.Exists(path))
{
    Console.Error.WriteLine($"File not found: {path}");
    return 2;
}

// This is all a host application needs to do to use the module.
using var services = new ServiceCollection()
    .AddFlightDataExtraction()
    .AddPdfPigReader()
    .BuildServiceProvider();

var extractor = services.GetRequiredService<IFlightDataExtractor>();

using var pdf = File.OpenRead(path);
var result = extractor.Extract(pdf);

if (asJson)
{
    var options = new JsonSerializerOptions { WriteIndented = true, Converters = { new JsonStringEnumConverter() } };
    Console.WriteLine(JsonSerializer.Serialize(result, options));
}
else
{
    ConsoleReport.Write(path, result);
}

if (result.Diagnostics.Any(d => d.Severity == DiagnosticSeverity.Fatal)) return 2;
return result.HasErrors ? 1 : 0;
