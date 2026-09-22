namespace FlightPlanExtractor.Core.Diagnostics;

/// <summary>
/// Stable, machine-readable reason. Callers use this (not the message text)
/// to decide what to do or which localized message to show.
/// </summary>
public enum DiagnosticCode
{
    // Document level
    DocumentUnreadable,
    NoRelevantPagesFound,

    // Field level - the three ways a single field can fail
    LabelNotFound,
    ValueFormatMismatch,
    ValueInvalid,

    // Flight level
    FlightKeyUnreadable,
    ChapterMissing,
    ChaptersDisagree,
    DuplicateChapter,
}
