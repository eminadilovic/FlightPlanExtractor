using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Chapters;

/// <summary>
/// Handles one kind of page (OFP, Crew Briefing, ...). Pages are found by asking every
/// chapter extractor whether it recognizes them - never by page number or position.
/// Supporting another document format means adding another implementation.
/// </summary>
public interface IChapterExtractor
{
    /// <summary>For diagnostics, e.g. "OFP".</summary>
    string ChapterName { get; }

    bool Recognizes(PageContent page);

    ExtractedChapter Extract(PageContent page);
}
