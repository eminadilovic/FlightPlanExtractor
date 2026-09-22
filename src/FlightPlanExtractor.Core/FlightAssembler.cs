using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Model;

namespace FlightPlanExtractor.Core;

/// <summary>
/// Joins chapters into flights by <see cref="FlightKey"/>. The order of chapters in the document
/// plays no role: in the sample file the Crew Briefings come in the reverse order of the OFPs.
/// </summary>
public static class FlightAssembler
{
    /// <param name="expectedChapters">Chapter names every flight should have; a missing one is reported.</param>
    public static ExtractionResult Assemble(
        IReadOnlyList<ExtractedChapter> chapters, IReadOnlyCollection<string> expectedChapters)
    {
        // What the chapters reported themselves, now that we know which flight it belongs to.
        var diagnostics = chapters
            .SelectMany(chapter => chapter.Diagnostics.Select(d => d with { Flight = chapter.Key }))
            .ToList();

        foreach (var chapter in chapters.Where(c => c.Key is null))
        {
            diagnostics.Add(new Diagnostic
            {
                Severity = DiagnosticSeverity.Error,
                Code = DiagnosticCode.FlightKeyUnreadable,
                Message = $"The {chapter.ChapterName} on page {chapter.PageNumber} cannot be assigned to a flight " +
                          "because its date or flight number could not be read. Its data is not part of the result.",
                PageNumber = chapter.PageNumber,
            });
        }

        // GroupBy keeps the order of first appearance, so flights come out in document order.
        var flights = chapters
            .Where(c => c.Key is not null)
            .GroupBy(c => c.Key!)
            .Select(group => AssembleFlight(group.Key, group.ToList(), expectedChapters, diagnostics))
            .ToList();

        return new ExtractionResult { Flights = flights, Diagnostics = diagnostics };
    }

    private static FlightData AssembleFlight(
        FlightKey key,
        IReadOnlyList<ExtractedChapter> chapters,
        IReadOnlyCollection<string> expectedChapters,
        List<Diagnostic> diagnostics)
    {
        var flight = new FlightData { Key = key };

        foreach (var sameName in chapters.GroupBy(c => c.ChapterName))
        {
            var used = sameName.First();
            flight = used.AddTo(flight);

            foreach (var duplicate in sameName.Skip(1))
            {
                diagnostics.Add(FlightProblem(DiagnosticSeverity.Warning, DiagnosticCode.DuplicateChapter, key,
                    $"The document contains more than one {used.ChapterName} for this flight. " +
                    $"The one on page {used.PageNumber} is used, the one on page {duplicate.PageNumber} is ignored.",
                    duplicate.PageNumber));
            }
        }

        foreach (var missing in expectedChapters.Except(chapters.Select(c => c.ChapterName)))
        {
            diagnostics.Add(FlightProblem(DiagnosticSeverity.Error, DiagnosticCode.ChapterMissing, key,
                $"The document contains no {missing} for this flight."));
        }

        ReportDisagreement(key, "ATC call sign", chapters, c => c.AtcCallSign, diagnostics);
        ReportDisagreement(key, "registration", chapters, c => c.Registration, diagnostics);

        return flight;
    }

    // Chapters that do not print the value (null) simply do not take part.
    private static void ReportDisagreement(
        FlightKey key,
        string what,
        IReadOnlyList<ExtractedChapter> chapters,
        Func<ExtractedChapter, string?> value,
        List<Diagnostic> diagnostics)
    {
        var printed = chapters.Where(c => value(c) is not null).ToList();
        if (printed.Select(value).Distinct().Count() <= 1) return;

        var details = string.Join(", ", printed.Select(c => $"{c.ChapterName} (page {c.PageNumber}): {value(c)}"));
        diagnostics.Add(FlightProblem(DiagnosticSeverity.Warning, DiagnosticCode.ChaptersDisagree, key,
            $"The chapters of this flight show a different {what} - {details}."));
    }

    private static Diagnostic FlightProblem(
        DiagnosticSeverity severity, DiagnosticCode code, FlightKey key, string message, int? pageNumber = null) => new()
    {
        Severity = severity,
        Code = code,
        Message = message,
        Flight = key,
        PageNumber = pageNumber,
    };
}
