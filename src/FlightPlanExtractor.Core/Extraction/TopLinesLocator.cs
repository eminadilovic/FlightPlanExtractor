using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// For values in a page header that have neither a label nor a column header (the date and the
/// "LX1613/SWR2TM" identification of a Crew Briefing). Offers the words of the first lines;
/// the field's format must be specific enough to pick the right one.
/// </summary>
public sealed record TopLinesLocator(int LineCount) : ILocator
{
    public LocatorResult? Locate(PageContent page, ExtractedValues found)
    {
        var lines = page.Lines.Take(LineCount).ToList();
        if (lines.Count == 0) return null;

        var words = lines.SelectMany(line => line.Words).Select(w => w.Text).ToList();
        return new LocatorResult(words, string.Join(" | ", lines.Select(line => line.Text)));
    }

    public string Describe() => $"the first {LineCount} lines of the page";
}
