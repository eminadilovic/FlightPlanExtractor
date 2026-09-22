using FlightPlanExtractor.Core.Extraction;
using static FlightPlanExtractor.Core.Extraction.Locate;

namespace FlightPlanExtractor.Tests.Extraction;

public class ColumnLocatorTests
{
    private static readonly FieldDefinition Economy = new("Economy", Under("PAX"), Formats.PassengerCount(part: 1));

    [Fact]
    public void Reads_the_value_below_the_header_and_skips_a_second_header_line()
    {
        var page = TestPage.FromLines(
            "DEP   ARR   FLT     PAX",
            "            TIME    C/Y",
            "MXP   ZRH   01:05   18/50");

        var result = FieldExtractor.Extract(page, [Economy]);

        Assert.Equal(50, result.Values.Value<int>("Economy"));
    }

    [Fact]
    public void Does_not_take_a_value_from_a_neighbouring_column()
    {
        var page = TestPage.FromLines(
            "DEP   ARR   FLT     PAX     Fuel",
            "MXP   ZRH   01:05           3/4");

        var result = FieldExtractor.Extract(page, [Economy]);

        Assert.False(result.Values.Has("Economy"));
    }

    [Fact]
    public void Does_not_take_a_value_from_far_below()
    {
        var page = TestPage.FromLines("PAX", "", "", "", "", "18/50");

        var result = FieldExtractor.Extract(page, [Economy]);

        Assert.False(result.Values.Has("Economy"));
    }
}
