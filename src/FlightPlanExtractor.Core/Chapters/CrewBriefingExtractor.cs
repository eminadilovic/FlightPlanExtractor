using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Extraction;
using FlightPlanExtractor.Core.Layouts;
using FlightPlanExtractor.Core.Model;
using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Chapters;

public sealed class CrewBriefingExtractor : IChapterExtractor
{
    private const string CrewField = nameof(CrewBriefing.Crew);

    public string ChapterName => "Crew Briefing";

    public bool Recognizes(PageContent page) =>
        page.Lines.Any(line => HelveticCrewBriefingLayout.PageTitles.Any(line.Text.Contains)) &&
        page.Lines.Any(line => HelveticCrewBriefingLayout.FirstPageMarks.Any(line.Text.Contains));

    public ExtractedChapter Extract(PageContent page)
    {
        var (values, fieldDiagnostics) = FieldExtractor.Extract(page, HelveticCrewBriefingLayout.Fields);
        var diagnostics = fieldDiagnostics.ToList();

        var briefing = new CrewBriefing
        {
            SourcePage = page.Number,
            BusinessPassengers = values.Value<int>(nameof(CrewBriefing.BusinessPassengers)),
            EconomyPassengers = values.Value<int>(nameof(CrewBriefing.EconomyPassengers)),
            DryOperatingWeightKg = values.Value<int>(nameof(CrewBriefing.DryOperatingWeightKg)),
            DryOperatingIndex = values.Value<decimal>(nameof(CrewBriefing.DryOperatingIndex)),
            Crew = ReadCrew(page, diagnostics),
        };

        return new ExtractedChapter
        {
            ChapterName = ChapterName,
            PageNumber = page.Number,
            Key = FlightKeyFields.ReadKey(values),
            AtcCallSign = values.Text(FlightKeyFields.AtcCallSign),
            AddTo = flight => flight with { CrewBriefing = briefing },
            Diagnostics = diagnostics.Select(d => d with { Field = $"CrewBriefing.{d.Field}" }).ToList(),
        };
    }

    private static IReadOnlyList<CrewMember> ReadCrew(PageContent page, List<Diagnostic> diagnostics)
    {
        var rows = TableReader.ReadRows(page, HelveticCrewBriefingLayout.CrewTable);
        if (rows is null)
        {
            diagnostics.Add(CrewProblem(DiagnosticCode.LabelNotFound, page,
                "Could not find the header line of the crew table on the page."));
            return [];
        }

        var crew = new List<CrewMember>();
        foreach (var row in rows)
        {
            var function = row.GetValueOrDefault(HelveticCrewBriefingLayout.FunctionColumn);
            var name = row.GetValueOrDefault(HelveticCrewBriefingLayout.NameColumn);

            if (function is not null && Formats.CrewFunction.Matches(function) && !string.IsNullOrWhiteSpace(name))
            {
                crew.Add(new CrewMember(name, function));
                continue;
            }

            diagnostics.Add(CrewProblem(DiagnosticCode.ValueFormatMismatch, page,
                "A row of the crew table does not consist of a function code and a name.",
                rawValue: $"{function} | {name}"));
        }

        if (rows.Count == 0)
            diagnostics.Add(CrewProblem(DiagnosticCode.ValueFormatMismatch, page, "The crew table has no rows."));

        return crew;
    }

    private static Diagnostic CrewProblem(DiagnosticCode code, PageContent page, string message, string? rawValue = null) => new()
    {
        Severity = DiagnosticSeverity.Error,
        Code = code,
        Message = message,
        Field = CrewField,
        PageNumber = page.Number,
        RawValue = rawValue,
    };
}
