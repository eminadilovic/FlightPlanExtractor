using System.Text.RegularExpressions;
using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Tests;

internal static class TestPage
{
    private const double CharWidth = 6;
    private const double LineHeight = 10;

    /// <summary>
    /// Builds a page from plain text, so extraction can be tested without any PDF. Works like a
    /// monospaced printout: a word's X position comes from its column in the string, so text that is
    /// aligned in the test is aligned on the page, and two or more spaces separate table cells.
    /// An empty string leaves a blank line.
    /// </summary>
    public static PageContent FromLines(params string[] lines) => FromLines(pageNumber: 1, lines);

    public static PageContent FromLines(int pageNumber, params string[] lines)
    {
        var words = lines.SelectMany((line, lineIndex) =>
            Regex.Matches(line, @"\S+").Select(match => new Word(
                match.Value,
                Left: match.Index * CharWidth,
                Right: (match.Index + match.Length) * CharWidth,
                Baseline: 800 - lineIndex * LineHeight)));

        return new PageContent(pageNumber, words.ToList());
    }
}
