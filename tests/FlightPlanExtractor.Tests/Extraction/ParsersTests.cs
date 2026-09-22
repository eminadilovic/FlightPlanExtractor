using System.Globalization;
using FlightPlanExtractor.Core.Extraction;

namespace FlightPlanExtractor.Tests.Extraction;

public class ParsersTests
{
    private static readonly string[] Ignored = ["DCT"];

    [Theory]
    [InlineData("19MAR24", 2024, 3, 19)]
    [InlineData("19.Mar.2024", 2024, 3, 19)]
    [InlineData("01JAN25", 2025, 1, 1)]
    [InlineData("31.Dec.2024", 2024, 12, 31)]
    public void Date_reads_both_formats(string text, int year, int month, int day)
    {
        Assert.Equal(new DateOnly(year, month, day), Parsers.Date(text));
    }

    [Theory]
    [InlineData("31FEB24")]
    [InlineData("19XYZ24")]
    [InlineData("")]
    public void Date_is_null_for_nonsense(string text)
    {
        Assert.Null(Parsers.Date(text));
    }

    [Fact]
    public void Date_does_not_depend_on_the_culture_of_the_machine()
    {
        using var _ = new CultureScope("de-DE");

        Assert.Equal(new DateOnly(2024, 3, 19), Parsers.Date("19MAR24"));
    }

    [Theory]
    [InlineData("08:00", 8, 0)]
    [InlineData("23:59", 23, 59)]
    public void Time_reads_hours_and_minutes(string text, int hour, int minute)
    {
        Assert.Equal(new TimeOnly(hour, minute), Parsers.Time(text));
    }

    [Theory]
    [InlineData("25:80")]
    [InlineData("24:00")]
    public void Time_is_null_for_nonsense(string text)
    {
        Assert.Null(Parsers.Time(text));
    }

    [Theory]
    [InlineData("0:48", 0, 48)]
    [InlineData("1:43", 1, 43)]
    [InlineData("12:05", 12, 5)]
    public void Duration_reads_hours_and_minutes(string text, int hours, int minutes)
    {
        Assert.Equal(new TimeSpan(hours, minutes, 0), Parsers.Duration(text));
    }

    [Fact]
    public void Duration_is_null_when_minutes_are_60_or_more()
    {
        Assert.Null(Parsers.Duration("1:75"));
    }

    [Theory]
    [InlineData("1.7", 1.7)]
    [InlineData("68.7", 68.7)]
    [InlineData("0", 0)]
    public void Decimal_reads_a_decimal_point(string text, double expected)
    {
        Assert.Equal((decimal)expected, Parsers.Decimal(text));
    }

    [Fact]
    public void Decimal_does_not_depend_on_the_culture_of_the_machine()
    {
        // In German "1.7" would be read as 17 (the point is a thousands separator).
        using var _ = new CultureScope("de-DE");

        Assert.Equal(1.7m, Parsers.Decimal("1.7"));
    }

    [Theory]
    [InlineData("GAIN 0$/TON", 0)]
    [InlineData("GAIN 60$/TON", 60)]
    [InlineData("LOSS 40$/TON", -40)]
    [InlineData("LOSS 96$/TON", -96)]
    public void GainLoss_is_positive_for_gain_and_negative_for_loss(string text, int expected)
    {
        Assert.Equal(expected, Parsers.GainLoss(text));
    }

    [Fact]
    public void GainLoss_is_null_for_an_unknown_word()
    {
        Assert.Null(Parsers.GainLoss("EVEN 0$/TON"));
    }

    // The four routes of the sample document, plus the traps found in them.
    [Theory]
    [InlineData("VEBIT T53 GERSA/N0401F230 N850 DEGAD Z424 RIXUV", "VEBIT", "RIXUV")]
    [InlineData("PEPAG L995 CANNE Z651 KELIP", "PEPAG", "KELIP")]
    [InlineData("DEGES Z1 SUDEN T164 KOVAN/N0408F240 T163 SPESA SPESA4C", "DEGES", "SPESA")]
    [InlineData("ANEKI1Y ANEKI/N0400F230 Y163 NEKLO Y171 INKAM T725 LAMGO", "ANEKI", "LAMGO")]
    [InlineData("DCT TOBAK DCT GULKO DCT", "TOBAK", "GULKO")]
    [InlineData("SRN SRN", "SRN", "SRN")]
    [InlineData("T53 VEBIT/GERSA N850 DEGAD/RIXUV", "VEBIT", "RIXUV")] // "/" separates words just like a space
    public void Waypoints_are_the_first_and_last_letter_only_words(string route, string first, string last)
    {
        Assert.Equal(first, Parsers.FirstWaypoint(route, Ignored));
        Assert.Equal(last, Parsers.LastWaypoint(route, Ignored));
    }

    [Fact]
    public void Waypoints_are_null_when_the_route_has_none()
    {
        Assert.Null(Parsers.FirstWaypoint("T53 N850 DCT", Ignored));
        Assert.Null(Parsers.LastWaypoint("T53 N850 DCT", Ignored));
    }

    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _previous = CultureInfo.CurrentCulture;

        public CultureScope(string name) => CultureInfo.CurrentCulture = new CultureInfo(name);

        public void Dispose() => CultureInfo.CurrentCulture = _previous;
    }
}
