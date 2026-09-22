namespace FlightPlanExtractor.Core.Model;

/// <summary>
/// Everything extracted for one flight. A chapter is null when the document
/// did not contain it for this flight; a diagnostic explains why.
/// </summary>
public record FlightData
{
    public required FlightKey Key { get; init; }
    public OperationalFlightPlan? Ofp { get; init; }
    public CrewBriefing? CrewBriefing { get; init; }
}
