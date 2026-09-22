using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Extraction;
using static FlightPlanExtractor.Core.Extraction.Locate;

namespace FlightPlanExtractor.Tests.Extraction;

public class FieldExtractorTests
{
    private static readonly FieldDefinition Zfm = new("ZeroFuelMassKg", After("ZFM:"), Formats.Integer);

    [Fact]
    public void Reads_the_value_printed_after_the_label()
    {
        var page = TestPage.FromLines("RTE: ZRH-MXP05 ZFM: 34066 kg _______ 40800 kg");

        var result = FieldExtractor.Extract(page, [Zfm]);

        Assert.Equal(34066, result.Values.Value<int>("ZeroFuelMassKg"));
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void Missing_label_is_reported_with_field_and_page()
    {
        var page = TestPage.FromLines(pageNumber: 5, "RTE: ZRH-MXP05 TOM: 37437 kg");

        var result = FieldExtractor.Extract(page, [Zfm]);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.LabelNotFound, diagnostic.Code);
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Equal("ZeroFuelMassKg", diagnostic.Field);
        Assert.Equal(5, diagnostic.PageNumber);
        Assert.False(result.Values.Has("ZeroFuelMassKg"));
    }

    [Fact]
    public void Unexpected_value_is_reported_with_the_raw_text_and_never_replaced_by_a_later_number()
    {
        // 40800 (the MAX column) looks like a valid ZFM - it must not be picked up.
        var page = TestPage.FromLines("ZFM: 34.066 t _______ 40800 kg");

        var result = FieldExtractor.Extract(page, [Zfm]);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ValueFormatMismatch, diagnostic.Code);
        Assert.Equal("34.066 t _______ 40800 kg", diagnostic.RawValue);
        Assert.False(result.Values.Has("ZeroFuelMassKg"));
    }

    [Fact]
    public void Value_with_the_right_shape_that_cannot_be_parsed_is_reported_as_invalid()
    {
        var page = TestPage.FromLines("ZFM: 99999999999999999999 kg");

        var result = FieldExtractor.Extract(page, [Zfm]);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticCode.ValueInvalid, diagnostic.Code);
        Assert.Equal("99999999999999999999", diagnostic.RawValue);
    }

    [Fact]
    public void Empty_optional_field_is_only_info()
    {
        var alternate2 = new FieldDefinition("Alternate2", After("ALTN2:"), Formats.IcaoAirport, Required: false);
        var page = TestPage.FromLines("ALTN2: Delay (Code/Min): _______/_____");

        var result = FieldExtractor.Extract(page, [alternate2]);

        var diagnostic = Assert.Single(result.Diagnostics);
        Assert.Equal(DiagnosticSeverity.Info, diagnostic.Severity);
        Assert.Null(result.Values.Text("Alternate2"));
    }

    [Fact]
    public void Pattern_must_cover_the_whole_word()
    {
        var alternate1 = new FieldDefinition("Alternate1", After("ALTN1:"), Formats.IcaoAirport);
        var page = TestPage.FromLines("To ALTN1: VEBIT T52 LUMEL");

        var result = FieldExtractor.Extract(page, [alternate1]);

        Assert.Null(result.Values.Text("Alternate1"));
    }

    [Fact]
    public void Any_of_the_accepted_labels_is_found()
    {
        var field = new FieldDefinition("ZeroFuelMassKg", After("ZFW:", "ZFM:"), Formats.Integer);
        var page = TestPage.FromLines("ZFM: 34066 kg");

        var result = FieldExtractor.Extract(page, [field]);

        Assert.Equal(34066, result.Values.Value<int>("ZeroFuelMassKg"));
    }

    [Fact]
    public void Label_is_matched_as_whole_words()
    {
        var atc = new FieldDefinition("AtcCallSign", After("ATC:"), Formats.Code);
        var page = TestPage.FromLines("ATC Route", "ATC: SWR612Q ACT: MAX:");

        var result = FieldExtractor.Extract(page, [atc]);

        Assert.Equal("SWR612Q", result.Values.Text("AtcCallSign"));
    }

    [Fact]
    public void Label_can_be_the_value_of_an_earlier_field()
    {
        FieldDefinition[] fields =
        [
            new("Destination", After("To:"), Formats.IcaoAirport),
            new("TripFuelKg", AfterValueOf("Destination").Within(2), Formats.Integer),
        ];
        var page = TestPage.FromLines("To: LIMC MXP MILAN/MALPENSA", "LIMC: 0:48 1700 _____");

        var result = FieldExtractor.Extract(page, fields);

        Assert.Equal(1700, result.Values.Value<int>("TripFuelKg"));
    }

    [Fact]
    public void Dependent_field_is_reported_when_the_field_it_depends_on_is_missing()
    {
        var page = TestPage.FromLines("LIMC: 0:48 1700 _____");

        var result = FieldExtractor.Extract(page,
            [new FieldDefinition("TripFuelKg", AfterValueOf("Destination").Within(2), Formats.Integer)]);

        Assert.Equal(DiagnosticCode.LabelNotFound, Assert.Single(result.Diagnostics).Code);
    }

    [Fact]
    public void Multi_word_label_with_rest_of_line()
    {
        var anyText = new ValueFormat("any text", [".+"], text => text);
        var route = new FieldDefinition("Route", After("To DEST:").RestOfLine(), anyText);
        var page = TestPage.FromLines("To: LIMC MXP", "To DEST: VEBIT T53 GERSA/N0401F230 RIXUV");

        var result = FieldExtractor.Extract(page, [route]);

        Assert.Equal("VEBIT T53 GERSA/N0401F230 RIXUV", result.Values.Text("Route"));
    }
}
