namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// Values extracted from one page so far, by field name. Filled by <see cref="FieldExtractor"/>,
/// readable by locators (a label can depend on an earlier field) and finally mapped to the model.
/// </summary>
public sealed class ExtractedValues
{
    private readonly Dictionary<string, object> _values = [];

    public void Set(string field, object value) => _values[field] = value;

    public bool Has(string field) => _values.ContainsKey(field);

    /// <summary>Null when the field was not extracted.</summary>
    public string? Text(string field) => _values.GetValueOrDefault(field) as string;

    /// <summary>Null when the field was not extracted.</summary>
    public T? Value<T>(string field) where T : struct =>
        _values.GetValueOrDefault(field) is T value ? value : null;
}
