using FlightPlanExtractor.Core.Diagnostics;
using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// The engine: runs every field definition against a page. It contains no knowledge about any
/// particular field - that lives in the definitions - so supporting a new field never changes this class.
/// </summary>
public static class FieldExtractor
{
    public static FieldExtractionResult Extract(PageContent page, IEnumerable<FieldDefinition> fields)
    {
        var values = new ExtractedValues();
        var diagnostics = new List<Diagnostic>();

        // In order, because a locator may use the value of an earlier field.
        foreach (var field in fields)
        {
            var located = field.Locator.Locate(page, values);
            if (located is null)
            {
                diagnostics.Add(Problem(DiagnosticCode.LabelNotFound, field, page,
                    $"Could not find {field.Locator.Describe()} on the page."));
                continue;
            }

            var text = located.Candidates.FirstOrDefault(field.Format.Matches);
            if (text is null)
            {
                var found = located.Context.Length > 0 ? $"\"{located.Context}\"" : "nothing";

                // For an optional field this is the normal "left empty" case. We still say what was
                // there, because from one document an empty field and a changed format look the same.
                var message = field.Required
                    ? $"Found {found} after {field.Locator.Describe()}, expected {field.Format.Description}."
                    : $"Optional field has no value: {found} after {field.Locator.Describe()} is not {field.Format.Description}.";

                diagnostics.Add(Problem(DiagnosticCode.ValueFormatMismatch, field, page, message, located.Context));
                continue;
            }

            var value = field.Format.Parse(text);
            if (value is null)
            {
                diagnostics.Add(Problem(DiagnosticCode.ValueInvalid, field, page,
                    $"\"{text}\" looks like {field.Format.Description} but is not a valid value.",
                    text));
                continue;
            }

            values.Set(field.Name, value);
        }

        return new FieldExtractionResult(values, diagnostics);
    }

    private static Diagnostic Problem(
        DiagnosticCode code, FieldDefinition field, PageContent page, string message, string? rawValue = null) => new()
    {
        Severity = field.Required ? DiagnosticSeverity.Error : DiagnosticSeverity.Info,
        Code = code,
        Message = message,
        Field = field.Name,
        PageNumber = page.Number,
        RawValue = rawValue,
    };
}

public record FieldExtractionResult(ExtractedValues Values, IReadOnlyList<Diagnostic> Diagnostics);
