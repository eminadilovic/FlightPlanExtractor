using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>Knows where on a page to look for a field. Knows nothing about what the value means.</summary>
public interface ILocator
{
    /// <returns>Null when the place to look (e.g. the label) is not on the page.</returns>
    LocatorResult? Locate(PageContent page, ExtractedValues found);

    /// <summary>For diagnostics, e.g. <c>label "ZFM:"</c>.</summary>
    string Describe();
}

/// <param name="Candidates">Texts that may be the value, in order of preference. The field's format picks the first that fits.</param>
/// <param name="Context">Everything found at that place, reported as the raw value when no candidate fits.</param>
public record LocatorResult(IReadOnlyList<string> Candidates, string Context);
