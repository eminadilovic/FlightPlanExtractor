using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Core.Diagnostics;

/// <summary>
/// One thing the extraction wants the caller to know about. Everything except
/// <see cref="Message"/> is structured so callers can filter, group and localize.
/// </summary>
public record Diagnostic
{
    public required DiagnosticSeverity Severity { get; init; }
    public required DiagnosticCode Code { get; init; }

    /// <summary>Human-readable English description, mainly for logs and the sample app.</summary>
    public required string Message { get; init; }

    /// <summary>Field the diagnostic is about, e.g. "Ofp.ZeroFuelMassKg". Null for document/flight level.</summary>
    public string? Field { get; init; }

    public int? PageNumber { get; init; }
    public FlightKey? Flight { get; init; }

    /// <summary>The text that was found but could not be used, exactly as it appears in the document.</summary>
    public string? RawValue { get; init; }
}
