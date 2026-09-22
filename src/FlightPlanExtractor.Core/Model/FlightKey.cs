namespace FlightPlanExtractor.Core.Model;

/// <summary>
/// Identifies one flight inside a document. Both chapters (OFP and Crew Briefing) print
/// these two values, so they are used to join the chapters - never the order of the pages.
/// </summary>
public record FlightKey(DateOnly Date, string FlightNumber)
{
    public override string ToString() => $"{FlightNumber} {Date:yyyy-MM-dd}";
}
