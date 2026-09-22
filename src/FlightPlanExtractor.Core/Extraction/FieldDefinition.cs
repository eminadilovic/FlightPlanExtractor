namespace FlightPlanExtractor.Core.Extraction;

/// <summary>
/// Everything needed to extract one field: where to look, what it must look like, how to read it.
/// Adding a field to the solution means adding one of these to a layout.
/// </summary>
/// <param name="Required">A missing required field is an Error, a missing optional one only Info.</param>
public sealed record FieldDefinition(string Name, ILocator Locator, ValueFormat Format, bool Required = true);
