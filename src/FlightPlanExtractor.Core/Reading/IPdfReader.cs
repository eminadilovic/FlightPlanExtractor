namespace FlightPlanExtractor.Core.Reading;

/// <summary>
/// Turns a PDF into positioned words. Implementations know a PDF library and nothing
/// about flights; the extraction knows flights and nothing about PDF libraries.
/// </summary>
public interface IPdfReader
{
    /// <exception cref="DocumentUnreadableException">The stream is not a PDF that can be read.</exception>
    IReadOnlyList<PageContent> Read(Stream pdf);
}
