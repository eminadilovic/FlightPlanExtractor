namespace FlightPlanExtractor.Core.Model;

/// <summary>
/// Fields taken from the first page of a flight's OFP. A null value means the field
/// could not be read (or is legitimately empty, e.g. Alternate2); see the diagnostics.
/// Times are as printed on the OFP (UTC). Units are part of the property name.
/// </summary>
public record OperationalFlightPlan
{
    /// <summary>Page of the source document this data was read from (1-based).</summary>
    public required int SourcePage { get; init; }

    public string? Registration { get; init; }
    public string? AtcCallSign { get; init; }

    public string? DepartureAirport { get; init; }
    public string? DestinationAirport { get; init; }
    public string? Alternate1 { get; init; }
    public string? Alternate2 { get; init; }

    public TimeOnly? DepartureTime { get; init; }
    public TimeOnly? ArrivalTime { get; init; }

    public int? ZeroFuelMassKg { get; init; }

    public TimeSpan? TimeToDestination { get; init; }
    public decimal? FuelToDestinationTonnes { get; init; }
    public TimeSpan? TimeToAlternate { get; init; }
    public decimal? FuelToAlternateTonnes { get; init; }
    public decimal? MinimumFuelTonnes { get; init; }

    public string? FirstWaypoint { get; init; }
    public string? LastWaypoint { get; init; }

    /// <summary>Positive for GAIN, negative for LOSS.</summary>
    public int? GainLoss { get; init; }
}
