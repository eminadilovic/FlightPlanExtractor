namespace FlightPlanExtractor.Core.Reading;

/// <summary>
/// A word and where it sits on the page. Coordinates are PDF points with the origin
/// in the bottom-left corner, so a larger <see cref="Baseline"/> means higher up the page.
/// </summary>
/// <param name="Baseline">Y of the line the text stands on. Same for all words of a printed line,
/// regardless of descenders (g, y) or underscores, which is why it is used instead of the box bottom.</param>
public record Word(string Text, double Left, double Right, double Baseline);
