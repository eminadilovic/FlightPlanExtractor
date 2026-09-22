using System.Text.RegularExpressions;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// What a value must look like and how to turn it into a typed value. Defined once, shared by every field of that kind.
/// </summary>
/// <param name="Description">Used in diagnostics: "expected {Description}".</param>
/// <param name="Patterns">Accepted shapes as regular expressions. Add a new one here when the document changes.</param>
/// <param name="Parse">Returns null when the text has the right shape but makes no sense (31FEB24, 25:80).</param>
public sealed record ValueFormat(string Description, IReadOnlyList<string> Patterns, Func<string, object?> Parse)
{
    /// <summary>
    /// A pattern must cover the whole text. Anchoring here rather than in every pattern means
    /// nobody can forget it - otherwise "[A-Z]{4}" would happily read "VEBI" out of "VEBIT".
    /// </summary>
    public bool Matches(string text) =>
        Patterns.Any(pattern => Regex.IsMatch(text, $"^(?:{pattern})$"));
}
