namespace FlightPlanExtractor.Core;

/// <summary>The entry point of the module: a flight planning PDF in, flights and diagnostics out.</summary>
public interface IFlightDataExtractor
{
    /// <summary>
    /// Never throws because of the document's content. An unreadable file, unknown pages or
    /// unexpected values all end up in <see cref="ExtractionResult.Diagnostics"/>.
    /// </summary>
    /// <param name="pdf">Any readable stream (file, upload, blob). It is not closed.</param>
    ExtractionResult Extract(Stream pdf);
}
