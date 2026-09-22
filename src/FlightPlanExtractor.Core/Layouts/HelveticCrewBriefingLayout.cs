using FlightPlanExtractor.Core.Extraction;
using FlightPlanExtractor.Core.Model;
using static FlightPlanExtractor.Core.Extraction.Locate;

namespace FlightPlanExtractor.Core.Layouts;

/// <summary>
/// What we know about the first page of a Helvetic Airways Crew Briefing. Only data, like
/// <see cref="HelveticOfpLayout"/>. The aircraft registration is printed here too, but without
/// any label or column header, so it is not read; the call sign serves as the consistency check.
/// </summary>
public static class HelveticCrewBriefingLayout
{
    /// <summary>Every page of a Crew Briefing carries the title...</summary>
    public static readonly IReadOnlyList<string> PageTitles = ["Flight Crew Briefing"];

    /// <summary>...but only its first page has one of these.</summary>
    public static readonly IReadOnlyList<string> FirstPageMarks = ["DOW:"];

    public static readonly IReadOnlyList<FieldDefinition> Fields =
    [
        // The page header has no labels: "19.Mar.2024 DEP ARR ... LX1613/SWR2TM".
        new(FlightKeyFields.Date,         InTopLines(3), Formats.Date),
        new(FlightKeyFields.FlightNumber, InTopLines(3), Formats.FlightIdentification(part: 0)),
        new(FlightKeyFields.AtcCallSign,  InTopLines(3), Formats.FlightIdentification(part: 1)),

        new(nameof(CrewBriefing.BusinessPassengers), Under("PAX"), Formats.PassengerCount(part: 0)),
        new(nameof(CrewBriefing.EconomyPassengers),  Under("PAX"), Formats.PassengerCount(part: 1)),

        new(nameof(CrewBriefing.DryOperatingWeightKg), After("DOW:"), Formats.Kilograms),
        new(nameof(CrewBriefing.DryOperatingIndex),    After("DOI:"), Formats.Decimal),
    ];

    public const string FunctionColumn = "Function";
    public const string CodeColumn = "ThreeLetterCode";
    public const string NameColumn = "Name";

    public static readonly TableDefinition CrewTable = new()
    {
        Columns = new Dictionary<string, IReadOnlyList<string>>
        {
            [FunctionColumn] = ["Func"],
            // Not extracted, but listed so the reader knows where it ends and the name begins.
            [CodeColumn] = ["3LC"],
            [NameColumn] = ["Name"],
        },
        KeyColumn = FunctionColumn,
        EndMarkers = ["X:"],
    };
}
