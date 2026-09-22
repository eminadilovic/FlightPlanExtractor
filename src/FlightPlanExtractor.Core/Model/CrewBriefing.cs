namespace FlightPlanExtractor.Core.Model;

/// <summary>Fields taken from the first page of a flight's Crew Briefing.</summary>
public record CrewBriefing
{
    /// <summary>Page of the source document this data was read from (1-based).</summary>
    public required int SourcePage { get; init; }

    public int? BusinessPassengers { get; init; }
    public int? EconomyPassengers { get; init; }

    public int? DryOperatingWeightKg { get; init; }
    public decimal? DryOperatingIndex { get; init; }

    public IReadOnlyList<CrewMember> Crew { get; init; } = [];
}

/// <param name="Function">Function code as printed, e.g. CMD, COP, CAB, SEN.</param>
public record CrewMember(string Name, string Function);
