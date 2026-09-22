using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// Finds the first line containing one of the labels and offers the words printed after it.
/// A label may be several words ("To DEST:") and is compared word by word, so "ATC:" does not
/// match "ATC Route" and "To:" does not match "To DEST:".
/// </summary>
public sealed record LabelLocator : ILocator
{
    /// <summary>Accepted spellings of the label. Add a new one here when the document changes.</summary>
    public IReadOnlyList<string> Labels { get; init; } = [];

    /// <summary>When set, the label is the value of this earlier field plus ":" (fuel rows are labelled with the airport code).</summary>
    public string? LabelFromField { get; init; }

    /// <summary>
    /// How many words after the label may hold the value. Kept small on purpose: a wider window
    /// could pick a similar-looking number further along the line when the real value is missing.
    /// </summary>
    public int Window { get; init; } = 1;

    /// <summary>Offer everything after the label as one text instead of single words (routes, "GAIN 0$/TON").</summary>
    public bool TakeRestOfLine { get; init; }

    public LabelLocator Within(int words) => this with { Window = words };

    public LabelLocator RestOfLine() => this with { TakeRestOfLine = true };

    public LocatorResult? Locate(PageContent page, ExtractedValues found)
    {
        var labels = ResolveLabels(found).Select(label => label.Split(' ')).ToList();

        foreach (var line in page.Lines)
        {
            foreach (var label in labels)
            {
                var start = IndexOf(line.Words, label);
                if (start < 0) continue;

                var after = line.Words.Skip(start + label.Length).Select(w => w.Text).ToList();
                var context = string.Join(' ', after);

                IReadOnlyList<string> candidates =
                    TakeRestOfLine ? (after.Count > 0 ? [context] : [])
                                   : after.Take(Window).ToList();

                return new LocatorResult(candidates, context);
            }
        }

        return null;
    }

    public string Describe() => LabelFromField is null
        ? $"label {string.Join(" / ", Labels.Select(l => $"\"{l}\""))}"
        : $"the row labelled with the value of {LabelFromField}";

    private IReadOnlyList<string> ResolveLabels(ExtractedValues found)
    {
        if (LabelFromField is null) return Labels;

        var value = found.Text(LabelFromField);
        return value is null ? [] : [$"{value}:"];
    }

    private static int IndexOf(IReadOnlyList<Word> words, string[] label)
    {
        for (var i = 0; i <= words.Count - label.Length; i++)
        {
            var matches = true;
            for (var j = 0; j < label.Length && matches; j++)
                matches = words[i + j].Text == label[j];

            if (matches) return i;
        }

        return -1;
    }
}
