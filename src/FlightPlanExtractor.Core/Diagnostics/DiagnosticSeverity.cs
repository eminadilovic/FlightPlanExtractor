namespace FlightPlanExtractor.Core.Diagnostics;

// Ordered, so callers can filter with >= (e.g. everything from Warning up).
public enum DiagnosticSeverity
{
    /// <summary>Worth knowing, not a problem (e.g. an optional field is empty).</summary>
    Info,

    /// <summary>Data was extracted but something looks inconsistent.</summary>
    Warning,

    /// <summary>A required field or chapter could not be extracted.</summary>
    Error,

    /// <summary>Nothing could be extracted from the document at all.</summary>
    Fatal,
}
