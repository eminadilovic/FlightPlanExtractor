using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Core.Chapters;

/// <summary>What one chapter page contributed, before chapters are joined into flights.</summary>
public record ExtractedChapter
{
    public required string ChapterName { get; init; }
    public required int PageNumber { get; init; }

    /// <summary>Null when date or flight number could not be read - the chapter then cannot be assigned to a flight.</summary>
    public FlightKey? Key { get; init; }

    // Printed in every chapter; compared between the chapters of a flight as a consistency check.
    public string? Registration { get; init; }
    public string? AtcCallSign { get; init; }

    /// <summary>
    /// Puts this chapter's data into the flight. Each chapter knows its own place in
    /// <see cref="FlightData"/>, so joining chapters needs no knowledge of chapter types.
    /// </summary>
    public required Func<FlightData, FlightData> AddTo { get; init; }

    public IReadOnlyList<Diagnostic> Diagnostics { get; init; } = [];
}
