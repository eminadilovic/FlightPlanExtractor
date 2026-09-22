namespace FlightPlanExtractor.Core.Reading;

/// <summary>
/// The one exception an <see cref="IPdfReader"/> may throw. Readers wrap whatever their PDF
/// library throws into this, so the extraction can catch it without knowing the library
/// and report it to the caller as a Fatal diagnostic.
/// </summary>
public class DocumentUnreadableException(string message, Exception? innerException = null)
    : Exception(message, innerException);
