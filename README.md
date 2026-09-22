# Flight Plan Extractor

Extracts pre-defined flight data (OFP + Crew Briefing) from a flight planning PDF.
Written for .NET 10.

```bash
dotnet test
dotnet run --project src/FlightPlanExtractor.ConsoleApp -- samples/sample.pdf
dotnet run --project src/FlightPlanExtractor.ConsoleApp -- samples/sample.pdf --json
dotnet run --project src/FlightPlanExtractor.ConsoleApp -- samples/not-a-pdf.pdf    # error handling
dotnet run --project src/FlightPlanExtractor.ConsoleApp -- samples/truncated.pdf    # error handling
```

## Using the module

```csharp
services.AddFlightDataExtraction()   // Core
        .AddPdfPigReader();          // Pdf

ExtractionResult result = extractor.Extract(pdfStream);   // IFlightDataExtractor

result.Flights       // one FlightData per flight: Key, Ofp?, CrewBriefing?
result.Diagnostics   // everything that went wrong or is worth knowing
result.HasErrors
```

`Extract` takes a `Stream` (file, upload, blob) and never throws because of the document's content.

## Structure

| Project | Role | Depends on |
|---|---|---|
| `FlightPlanExtractor.Core` | Model, diagnostics, extraction engine, layouts, joining flights | nothing (only DI abstractions) |
| `FlightPlanExtractor.Pdf` | `PdfPigReader`, the only class that knows the PDF library | Core, PdfPig |
| `FlightPlanExtractor.ConsoleApp` | Sample host | Core, Pdf |
| `FlightPlanExtractor.Tests` | xUnit tests | Core, Pdf |

Core defines `IPdfReader`; the Pdf project implements it. Core therefore has no idea what PdfPig is,
almost all logic is tested from plain text without a PDF, and the library can be swapped in one place.
PdfPig was chosen because it is free (Apache 2.0) and gives the position of every word, which the crew table needs.

## How it works

```
PDF -> IPdfReader -> pages (words with x/y)
                       |
       IChapterExtractor.Recognizes(page)?     no one -> page is skipped (weather, NOTAMs, charts ...)
                       |
       FieldExtractor: for every field of the layout
           locator -> candidates -> format (pattern) -> parser -> value, or a diagnostic
                       |
       FlightAssembler: join chapters by (date, flight number)
                       |
       ExtractionResult { Flights, Diagnostics }
```

**Pages are found by content, never by number.** A chapter extractor recognizes its page by a title
from a list of accepted titles. Number of flights, number of pages and order of chapters do not matter.

**A field is data, not code.** `Layouts/HelveticOfpLayout.cs` and `HelveticCrewBriefingLayout.cs` contain
everything known about the document format, as lists:

```csharp
new(nameof(OperationalFlightPlan.ZeroFuelMassKg), After("ZFM:"), Formats.Integer),
new(nameof(OperationalFlightPlan.FuelToDestinationTonnes),
    AfterValueOf(nameof(OperationalFlightPlan.DestinationAirport)).Within(2), Formats.Tonnes),
```

- Adding a field: one line in the layout, one property in the model, one line in the chapter extractor.
- A label was renamed: add the new spelling, `After("ZFM:", "ZFW:")`. Old documents keep working.
- A value looks different: add a pattern to the format in `Formats.cs`.
- Another airline / OFP provider: a new layout + `IChapterExtractor`. The engine does not change.

The engine (`FieldExtractor`, one loop of about 30 lines) knows nothing about any particular field.

**Locators** say where to look: `After(label)`, `AfterValueOf(field)` (the fuel table rows are labelled with
the airport code, i.e. with the value of another field), `Under(columnHeader)`, `InTopLines(n)`.
`TableReader` reads the crew table by column position, because text alone cannot do it: when a long name
wraps, the PDF text line is `Vissotto Attendant`, and only the X position says that `Vissotto` continues the
name and `Attendant` belongs to the next column.

**Formats** say what a value must look like and how to parse it. They are shared (4 airport fields use
`Formats.IcaoAirport`, 3 fuel fields use `Formats.Tonnes`, ...). Patterns always have to match the whole word
(anchored by the engine, so it cannot be forgotten); otherwise `[A-Z]{4}` would read `VEBI` out of `VEBIT`.

**Wrong data is worse than missing data.** Locators look only at a small window after the label.
With `ZFM: <garbage> kg _______ 40800 kg` a wider search would silently return 40800 (the MAX column).
There is a test for exactly this.

## Error handling

Nothing is thrown for bad input. Every problem is a `Diagnostic` with `Severity`, `Code` (stable, for program
logic and localization), `Message` (English, for logs), `Field`, `PageNumber`, `Flight` and `RawValue`
(the text that was found but could not be used).

A field can fail in three ways, reported separately because the fix is different:

| Code | Meaning | Typical fix |
|---|---|---|
| `LabelNotFound` | the label / header is not on the page | add the new label to the layout |
| `ValueFormatMismatch` | label found, value has an unexpected shape | add the new pattern to the format |
| `ValueInvalid` | right shape, impossible value (`31FEB24`) | the document is wrong |

Other codes: `DocumentUnreadable` and `NoRelevantPagesFound` (Fatal), `FlightKeyUnreadable`,
`ChapterMissing` (Error), `ChaptersDisagree`, `DuplicateChapter` (Warning).

Failures are contained: one unreadable field costs that field, one unreadable page costs that chapter,
the other flights are still returned. Optional fields (ALTN1/ALTN2 and the alternate fuel) only produce `Info`.
All model fields are nullable for that reason - the caller decides whether a partial flight is usable.

The ATC call sign is printed in both chapters and compared as a consistency check of the join.

## Things I found in the sample file

- The Crew Briefings are in the **reverse order** of the OFPs. The crew briefing screenshot in the task
  (LX1073, 1/26 PAX) is the first crew briefing in the file but belongs to the *last* OFP. Joining "first with first"
  would produce wrong flights without any error, so flights are joined by date + flight number only.
- `ALTN2:` is empty in all four flights and followed by `Delay (Code/Min):` on the same line. Without a pattern
  for the value, the second alternate would be "Delay".
- Route: first and last word are not always navigation points. `ANEKI1Y ANEKI/N0400F230 ... ` starts with a SID,
  `... SPESA SPESA4C` ends with a STAR, and `DCT` can appear anywhere. See assumptions.
- The date is `19MAR24` in the OFP and `19.Mar.2024` in the Crew Briefing.
- The crew member in the task description is "Nico Veheist"; the PDF says "Nico Verhelst". The PDF wins.
- In the crew table the 3LC and Name columns are only 9pt apart - about the width of two spaces. Cells are
  therefore split where a known column starts, not by the size of the gap.

## Assumptions

1. OFP times (STD/STA) are UTC and are returned as printed. The Crew Briefing confirms it: it prints the same
   time followed by a second one in brackets that is one hour later (`13:45 (14:45)` for FRA-ZRH in March, UTC+1),
   so the bracketed value is the local time its `(LT)` header refers to. Local times are not extracted.
2. Fuel is in tonnes, masses in kg; the unit is part of the property name.
3. A navigation point is the first / last word of the route that contains only letters, where space and `/`
   are both delimiters (definition confirmed by the team). Airways, SIDs and STARs contain digits. On top of that,
   words in `IgnoredRouteWords` (`DCT`) are skipped - my addition, since `DCT` is letters only but not a point.
4. `GAIN n` is `+n`, `LOSS n` is `-n`.
5. `DOI: 0` is returned as 0, not interpreted as "missing".
6. Only the first page of each chapter is needed; the crew table fits on it.
7. Date + flight number identify a flight within one document.

## Not done / next steps

- One document format, as confirmed by the team. Another format would be a new layout + `IChapterExtractor`.
- The aircraft registration in the Crew Briefing is not read (it has neither label nor column header there), so
  only the call sign is cross-checked.
- `Remarks:` in the Crew Briefing looks like useful free text; it would be one more line in the layout.
- Layouts are C# (type-safe, parsers are functions). They are shaped so they could move to configuration if
  formats need to change without a deployment.
- No logging. `ILogger` in `FlightDataExtractor` would be the place; diagnostics already carry the content.
- `Extract` is synchronous because PdfPig is. For a web host I would wrap the call rather than fake an async API.
- Unexpected exceptions (bugs) are not caught and turned into diagnostics; I think they should surface.
- The tolerances in `TableReader` / `PageContent` (points) come from this one sample.

## Clarified with the team

| Question | Answer | Effect |
|---|---|---|
| First / last navigation point when the route starts with a SID or ends with a STAR? | First / last word with letters only; space and `/` are delimiters. `ANEKI`, `SPESA`. | `Parsers.Waypoints` splits on both; see assumption 3. |
| One field cannot be read: partial flight or reject the flight? | Return the flight, report the problem per field. | Already the design. `ExtractionResult.DiagnosticsFor(flight)` gives the per-field diagnostics of one flight. |
| Other OFP formats to expect? | The format stays. Pages can be added or removed and texts can change; a changed label is an error of that field, the rest must still work. | Pages are found by content, not by number. Test: `Renamed_label_costs_only_that_field`. Fix for a renamed label: add it to the field's label list in the layout. |
