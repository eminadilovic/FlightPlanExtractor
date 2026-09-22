using System.Globalization;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// Text to typed value. Every parser returns null when the text makes no sense; it never throws.
/// The shape of the text was already checked by the format's pattern.
/// Always InvariantCulture: the documents do not change with the language of the machine reading them.
/// </summary>
public static class Parsers
{
    private static readonly string[] DateFormats = ["ddMMMyy", "dd.MMM.yyyy"];

    /// <summary>"19MAR24" (OFP) or "19.Mar.2024" (Crew Briefing).</summary>
    public static DateOnly? Date(string text) =>
        DateOnly.TryParseExact(text, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date
            : null;

    /// <summary>"08:00" - a time of day.</summary>
    public static TimeOnly? Time(string text) =>
        TimeOnly.TryParseExact(text, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            ? time
            : null;

    /// <summary>"0:48", "1:43" - hours:minutes as a length of time.</summary>
    public static TimeSpan? Duration(string text)
    {
        // Not TimeSpan.Parse: it has its own ideas about what "1:43" or "25:10" mean.
        var parts = text.Split(':');
        if (parts.Length != 2) return null;

        if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)) return null;
        if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)) return null;
        if (minutes >= 60) return null;

        return new TimeSpan(hours, minutes, 0);
    }

    /// <summary>"1.7" - always with a decimal point, whatever the culture of the machine.</summary>
    public static decimal? Decimal(string text) =>
        decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    /// <summary>"GAIN 60$/TON" is 60, "LOSS 40$/TON" is -40.</summary>
    public static int? GainLoss(string text)
    {
        var parts = text.Split(' ');
        if (parts.Length != 2) return null;

        var amountText = parts[1].Split('$')[0];
        if (!int.TryParse(amountText, NumberStyles.None, CultureInfo.InvariantCulture, out var amount)) return null;

        return parts[0] switch
        {
            "GAIN" => amount,
            "LOSS" => -amount,
            _ => null,
        };
    }

    /// <summary>
    /// First navigation point of a route like "ANEKI1Y ANEKI/N0400F230 Y163 NEKLO T725 LAMGO".
    /// Definition confirmed by the team: the first word that contains only letters, where space and "/"
    /// are both delimiters. Words in <paramref name="ignoredWords"/> (e.g. DCT) are not navigation points.
    /// </summary>
    public static string? FirstWaypoint(string route, IReadOnlyCollection<string> ignoredWords) =>
        Waypoints(route, ignoredWords).FirstOrDefault();

    /// <summary>Last navigation point, same rules as <see cref="FirstWaypoint"/>.</summary>
    public static string? LastWaypoint(string route, IReadOnlyCollection<string> ignoredWords) =>
        Waypoints(route, ignoredWords).LastOrDefault();

    // Airways (T53), procedures (SPESA4C) and speed/level changes (N0401F230) all contain digits,
    // which is what tells them apart from a navigation point.
    private static IEnumerable<string> Waypoints(string route, IReadOnlyCollection<string> ignoredWords) =>
        route.Split([' ', '/'], StringSplitOptions.RemoveEmptyEntries)
            .Where(word => word.All(char.IsAsciiLetterUpper))
            .Where(word => !ignoredWords.Contains(word));
}
