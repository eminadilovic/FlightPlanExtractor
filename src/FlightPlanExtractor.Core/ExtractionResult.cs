using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Core;

/// <summary>
/// What the caller gets back. Extraction never throws for bad input: whatever could be
/// read is in <see cref="Flights"/>, and everything that went wrong is in <see cref="Diagnostics"/>.
/// </summary>
public record ExtractionResult
{
    public IReadOnlyList<FlightData> Flights { get; init; } = [];
    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];

    public bool HasErrors => Diagnostics.Any(d => d.Severity >= DiagnosticSeverity.Error);

    /// <summary>The per-field (and per-chapter) diagnostics of one flight, e.g. to show next to that flight.</summary>
    public IEnumerable<Diagnostic> DiagnosticsFor(FlightKey flight) => Diagnostics.Where(d => d.Flight == flight);
}
