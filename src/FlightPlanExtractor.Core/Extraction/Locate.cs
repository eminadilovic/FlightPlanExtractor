namespace FlightPlanExtractor.Core.Extraction;

/// <summary>Short factory methods so a layout reads like a list: <c>After("ZFM:")</c>.</summary>
public static class Locate
{
    /// <summary>The value is printed after one of these labels, on the same line.</summary>
    public static LabelLocator After(params string[] labels) => new() { Labels = labels };

    /// <summary>Like <see cref="After"/>, but the label is the value of an earlier field plus ":".</summary>
    public static LabelLocator AfterValueOf(string field) => new() { LabelFromField = field };

    /// <summary>The value is printed below one of these column headers.</summary>
    public static ColumnLocator Under(params string[] headers) => new() { Headers = headers };

    /// <summary>The value is somewhere in the first lines of the page and only its shape identifies it.</summary>
    public static TopLinesLocator InTopLines(int lineCount) => new(lineCount);
}
