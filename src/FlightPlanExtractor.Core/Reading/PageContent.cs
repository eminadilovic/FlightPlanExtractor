namespace FlightPlanExtractor.Core.Reading;

/// <summary>
/// One page as the extraction sees it: positioned words, plus the same words grouped into lines.
/// Fields with a label are read from <see cref="Lines"/>; tables need <see cref="Words"/>
/// because only the X position tells which column a word belongs to.
/// </summary>
public sealed class PageContent
{
    // Words whose baselines differ by less than this are on the same printed line.
    // Line spacing in the documents is ~9pt, so 2pt separates lines without splitting one.
    private const double BaselineTolerance = 2.0;

    private readonly Lazy<IReadOnlyList<TextLine>> _lines;

    public PageContent(int number, IReadOnlyList<Word> words)
    {
        Number = number;
        Words = words;
        _lines = new Lazy<IReadOnlyList<TextLine>>(() => GroupIntoLines(words));
    }

    /// <summary>1-based page number in the source document.</summary>
    public int Number { get; }

    public IReadOnlyList<Word> Words { get; }

    /// <summary>Top to bottom.</summary>
    public IReadOnlyList<TextLine> Lines => _lines.Value;

    private static IReadOnlyList<TextLine> GroupIntoLines(IReadOnlyList<Word> words)
    {
        var lines = new List<List<Word>>();

        foreach (var word in words.OrderByDescending(w => w.Baseline))
        {
            var current = lines.LastOrDefault();
            if (current is not null && current[0].Baseline - word.Baseline <= BaselineTolerance)
                current.Add(word);
            else
                lines.Add([word]);
        }

        return lines
            .Select(line => new TextLine(line.OrderBy(w => w.Left).ToList()))
            .ToList();
    }
}
