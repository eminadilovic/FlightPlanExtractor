namespace FlightPlanExtractor.Core.Reading;

/// <summary>Words that share a baseline, ordered left to right.</summary>
public record TextLine(IReadOnlyList<Word> Words)
{
    public double Baseline => Words[0].Baseline;
    public string Text => string.Join(' ', Words.Select(w => w.Text));

    public override string ToString() => Text;
}
