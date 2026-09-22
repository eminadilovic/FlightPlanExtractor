using FlightPlanExtractor.Core.Extraction;
using FlightPlanExtractor.Core.Layouts;
using FlightPlanExtractor.Core.Model;
using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Chapters;

public sealed class OfpExtractor : IChapterExtractor
{
    public string ChapterName => "OFP";

    // Only the first page of an OFP carries the title; its continuation pages do not.
    public bool Recognizes(PageContent page) =>
        page.Lines.Any(line => HelveticOfpLayout.PageTitles.Any(line.Text.Contains));

    public ExtractedChapter Extract(PageContent page)
    {
        var (values, diagnostics) = FieldExtractor.Extract(page, HelveticOfpLayout.Fields);

        var ofp = new OperationalFlightPlan
        {
            SourcePage = page.Number,
            Registration = values.Text(nameof(OperationalFlightPlan.Registration)),
            AtcCallSign = values.Text(nameof(OperationalFlightPlan.AtcCallSign)),
            DepartureAirport = values.Text(nameof(OperationalFlightPlan.DepartureAirport)),
            DestinationAirport = values.Text(nameof(OperationalFlightPlan.DestinationAirport)),
            Alternate1 = values.Text(nameof(OperationalFlightPlan.Alternate1)),
            Alternate2 = values.Text(nameof(OperationalFlightPlan.Alternate2)),
            DepartureTime = values.Value<TimeOnly>(nameof(OperationalFlightPlan.DepartureTime)),
            ArrivalTime = values.Value<TimeOnly>(nameof(OperationalFlightPlan.ArrivalTime)),
            ZeroFuelMassKg = values.Value<int>(nameof(OperationalFlightPlan.ZeroFuelMassKg)),
            TimeToDestination = values.Value<TimeSpan>(nameof(OperationalFlightPlan.TimeToDestination)),
            FuelToDestinationTonnes = values.Value<decimal>(nameof(OperationalFlightPlan.FuelToDestinationTonnes)),
            TimeToAlternate = values.Value<TimeSpan>(nameof(OperationalFlightPlan.TimeToAlternate)),
            FuelToAlternateTonnes = values.Value<decimal>(nameof(OperationalFlightPlan.FuelToAlternateTonnes)),
            MinimumFuelTonnes = values.Value<decimal>(nameof(OperationalFlightPlan.MinimumFuelTonnes)),
            FirstWaypoint = values.Text(nameof(OperationalFlightPlan.FirstWaypoint)),
            LastWaypoint = values.Text(nameof(OperationalFlightPlan.LastWaypoint)),
            GainLoss = values.Value<int>(nameof(OperationalFlightPlan.GainLoss)),
        };

        return new ExtractedChapter
        {
            ChapterName = ChapterName,
            PageNumber = page.Number,
            Key = FlightKeyFields.ReadKey(values),
            Registration = ofp.Registration,
            AtcCallSign = ofp.AtcCallSign,
            AddTo = flight => flight with { Ofp = ofp },
            Diagnostics = diagnostics.Select(d => d with { Field = $"Ofp.{d.Field}" }).ToList(),
        };
    }
}
