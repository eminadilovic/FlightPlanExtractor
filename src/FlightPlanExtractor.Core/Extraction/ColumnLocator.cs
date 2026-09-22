using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// For values printed under a column header instead of after a label ("PAX" above "18/50").
/// Offers the words directly below the header, top to bottom; the field's format picks the value,
/// which also skips a second header line such as "C/Y".
/// </summary>
public sealed record ColumnLocator : ILocator
{
    /// <summary>Accepted spellings of the header. Add a new one here when the document changes.</summary>
    public IReadOnlyList<string> Headers { get; init; } = [];

    /// <summary>How far below the header (in points) the value may be. Small on purpose, see <see cref="LabelLocator.Window"/>.</summary>
    public double MaxDistance { get; init; } = 40;

    public LocatorResult? Locate(PageContent page, ExtractedValues found)
    {
        var header = page.Lines.SelectMany(line => line.Words).FirstOrDefault(w => Headers.Contains(w.Text));
        if (header is null) return null;

        var below = page.Words
            .Where(w => w.Baseline < header.Baseline && header.Baseline - w.Baseline <= MaxDistance)
            .Where(w => w.Left < header.Right && w.Right > header.Left)
            .OrderByDescending(w => w.Baseline)
            .Select(w => w.Text)
            .ToList();

        return new LocatorResult(below, string.Join(' ', below));
    }

    public string Describe() => $"column header {string.Join(" / ", Headers.Select(h => $"\"{h}\""))}";
}
