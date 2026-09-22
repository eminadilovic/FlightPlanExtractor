using FlightPlanExtractor.Core.Chapters;
using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core;

/// <summary>
/// Read the pages, let each chapter extractor pick and extract the pages it recognizes,
/// join the chapters into flights. Each step is behind its own type; this class only connects them.
/// </summary>
public sealed class FlightDataExtractor(IPdfReader reader, IEnumerable<IChapterExtractor> chapterExtractors)
    : IFlightDataExtractor
{
    private readonly IReadOnlyList<IChapterExtractor> _chapterExtractors = chapterExtractors.ToList();

    public ExtractionResult Extract(Stream pdf)
    {
        IReadOnlyList<PageContent> pages;
        try
        {
            pages = reader.Read(pdf);
        }
        catch (DocumentUnreadableException ex)
        {
            return Failed(DiagnosticCode.DocumentUnreadable, ex.Message);
        }

        var chapters = new List<ExtractedChapter>();
        foreach (var page in pages)
        {
            // Most pages (weather, NOTAMs, charts) are recognized by nobody and skipped.
            var extractor = _chapterExtractors.FirstOrDefault(e => e.Recognizes(page));
            if (extractor is not null)
                chapters.Add(extractor.Extract(page));
        }

        if (chapters.Count == 0)
        {
            return Failed(DiagnosticCode.NoRelevantPagesFound,
                $"None of the {pages.Count} pages was recognized as " +
                $"{string.Join(" or ", _chapterExtractors.Select(e => e.ChapterName))}. " +
                "The document is of a different kind or its layout has changed.");
        }

        return FlightAssembler.Assemble(chapters, _chapterExtractors.Select(e => e.ChapterName).ToList());
    }

    private static ExtractionResult Failed(DiagnosticCode code, string message) => new()
    {
        Diagnostics = [new Diagnostic { Severity = DiagnosticSeverity.Fatal, Code = code, Message = message }],
    };
}
