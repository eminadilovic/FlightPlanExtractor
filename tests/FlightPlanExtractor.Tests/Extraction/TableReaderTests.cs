using FlightPlanExtractor.Core.Extraction;

namespace FlightPlanExtractor.Tests.Extraction;

public class TableReaderTests
{
    private static readonly TableDefinition Crew = new()
    {
        Columns = new Dictionary<string, IReadOnlyList<string>>
        {
            ["Function"] = ["Func"],
            ["Name"] = ["Name", "Member"],
        },
        KeyColumn = "Function",
        EndMarkers = ["X:"],
    };

    [Fact]
    public void Reads_one_row_per_line_and_ignores_columns_that_were_not_asked_for()
    {
        var page = TestPage.FromLines(
            "Crew",
            "Func  3LC  Name                   LX1613",
            "CMD   KRE  Steve Krebs            Commander",
            "COP   GIG  Gregory Gillioz        Copilot");

        var rows = TableReader.ReadRows(page, Crew);

        Assert.Equal(["CMD", "COP"], rows!.Select(r => r["Function"]));
        Assert.Equal(["Steve Krebs", "Gregory Gillioz"], rows!.Select(r => r["Name"]));
    }

    [Fact]
    public void Wrapped_cell_continues_the_row_above_and_does_not_mix_with_the_neighbouring_column()
    {
        // As text, the second line is "Vissotto Attendant" - only the position says which word is part of the name.
        var page = TestPage.FromLines(
            "Func  3LC  Name                   LX1613",
            "CAB   QVL  Luisa Quadros          Cabin",
            "           Vissotto               Attendant",
            "CAB   SNA  Rainer Sattler         Cabin",
            "                                  Attendant");

        var rows = TableReader.ReadRows(page, Crew);

        Assert.Equal(["Luisa Quadros Vissotto", "Rainer Sattler"], rows!.Select(r => r["Name"]));
    }

    [Fact]
    public void Line_between_header_and_first_row_is_skipped()
    {
        var page = TestPage.FromLines(
            "Func  3LC  Name                   LX1613",
            "                                  LIMC-LSZH",
            "CMD   KRE  Steve Krebs            Commander");

        var rows = TableReader.ReadRows(page, Crew);

        Assert.Equal("Steve Krebs", Assert.Single(rows!)["Name"]);
    }

    [Fact]
    public void Stops_at_the_end_marker()
    {
        var page = TestPage.FromLines(
            "Func  3LC  Name",
            "CMD   KRE  Steve Krebs",
            "X: Operating crewmember, T: Trainee",
            "OBS   XXX  Not Crew");

        Assert.Single(TableReader.ReadRows(page, Crew)!);
    }

    [Fact]
    public void Stops_at_text_further_down_even_without_an_end_marker()
    {
        var page = TestPage.FromLines(
            "Func  3LC  Name",
            "CMD   KRE  Steve Krebs",
            "",
            "           Observer and Jumpseat Assignments");

        Assert.Equal("Steve Krebs", Assert.Single(TableReader.ReadRows(page, Crew)!)["Name"]);
    }

    [Fact]
    public void Any_of_the_accepted_headers_is_found()
    {
        var page = TestPage.FromLines(
            "Func  Member",
            "CMD   Steve Krebs");

        Assert.Equal("Steve Krebs", Assert.Single(TableReader.ReadRows(page, Crew)!)["Name"]);
    }

    [Fact]
    public void Missing_header_gives_null()
    {
        var page = TestPage.FromLines("CMD   KRE  Steve Krebs");

        Assert.Null(TableReader.ReadRows(page, Crew));
    }

    [Fact]
    public void Header_without_rows_gives_an_empty_list()
    {
        var page = TestPage.FromLines("Func  3LC  Name", "X: Operating crewmember");

        Assert.Empty(TableReader.ReadRows(page, Crew)!);
    }
}
