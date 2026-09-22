using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Core.Extraction;

/// <summary>Describes a table by its column headers. Knows nothing about what the table contains.</summary>
public sealed record TableDefinition
{
    /// <summary>Column name to the accepted header texts of that column. Columns not listed are ignored.</summary>
    public required IReadOnlyDictionary<string, IReadOnlyList<string>> Columns { get; init; }

    /// <summary>A line with a cell in this column starts a new row; other lines continue the previous row (wrapped text).</summary>
    public required string KeyColumn { get; init; }

    /// <summary>The table stops at a line starting with one of these.</summary>
    public IReadOnlyList<string> EndMarkers { get; init; } = [];
}

/// <summary>
/// Reads a table using word positions. Text alone is not enough: when a long name wraps,
/// "Vissotto Attendant" is one text line, and only the X position tells that "Vissotto"
/// belongs to the Name column and "Attendant" to the next one.
/// </summary>
public static class TableReader
{
    // Fallback for columns that are not listed in the definition: words in a cell are ~5pt apart,
    // such a column 30pt or more. Listed columns are separated by where they start, not by this gap -
    // neighbouring columns can be as close as 9pt, too close to a normal space to rely on.
    private const double CellGap = 15;

    // A cell belongs to a column when it starts where the column header starts (left-aligned columns).
    private const double ColumnTolerance = 5;

    // A wrapped line sits ~9pt below the line it continues.
    private const double MaxContinuationGap = 12;

    // Rows are 13-23pt apart. Something further down is not part of this table.
    private const double MaxRowGap = 40;

    /// <returns>One dictionary (column name to cell text) per row; null when the header line is not on the page.</returns>
    public static IReadOnlyList<IReadOnlyDictionary<string, string>>? ReadRows(PageContent page, TableDefinition table)
    {
        var headerIndex = FindHeader(page, table, out var columnStarts);
        if (headerIndex < 0) return null;

        var rows = new List<Dictionary<string, string>>();
        var previousBaseline = page.Lines[headerIndex].Baseline;

        foreach (var line in page.Lines.Skip(headerIndex + 1))
        {
            if (table.EndMarkers.Any(marker => line.Text.StartsWith(marker, StringComparison.Ordinal))) break;

            var cells = CellsByColumn(line, columnStarts);
            var gap = previousBaseline - line.Baseline;

            if (cells.ContainsKey(table.KeyColumn))
            {
                if (rows.Count > 0 && gap > MaxRowGap) break;
                rows.Add(cells);
            }
            else if (rows.Count > 0)
            {
                if (gap > MaxContinuationGap) break;

                foreach (var (column, text) in cells)
                    rows[^1][column] = rows[^1].TryGetValue(column, out var existing) ? $"{existing} {text}" : text;
            }
            // else: still between the header and the first row (e.g. a second header line)

            previousBaseline = line.Baseline;
        }

        return rows;
    }

    private static int FindHeader(PageContent page, TableDefinition table, out Dictionary<string, double> columnStarts)
    {
        for (var i = 0; i < page.Lines.Count; i++)
        {
            columnStarts = [];
            foreach (var (column, headers) in table.Columns)
            {
                var header = page.Lines[i].Words.FirstOrDefault(w => headers.Contains(w.Text));
                if (header is not null) columnStarts[column] = header.Left;
            }

            if (columnStarts.Count == table.Columns.Count) return i;
        }

        columnStarts = [];
        return -1;
    }

    private static Dictionary<string, string> CellsByColumn(TextLine line, Dictionary<string, double> columnStarts)
    {
        var result = new Dictionary<string, string>();

        foreach (var cell in SplitIntoCells(line, columnStarts.Values))
        {
            var column = columnStarts.FirstOrDefault(c => Math.Abs(c.Value - cell[0].Left) <= ColumnTolerance).Key;
            if (column is not null)
                result[column] = string.Join(' ', cell.Select(w => w.Text));
        }

        return result;
    }

    // A new cell starts where a listed column starts, or after a gap too wide to be a space.
    private static List<List<Word>> SplitIntoCells(TextLine line, IEnumerable<double> columnStarts)
    {
        var cells = new List<List<Word>>();

        foreach (var word in line.Words)
        {
            var startsColumn = columnStarts.Any(start => Math.Abs(start - word.Left) <= ColumnTolerance);
            var afterWideGap = cells.Count > 0 && word.Left - cells[^1][^1].Right > CellGap;

            if (cells.Count == 0 || startsColumn || afterWideGap)
                cells.Add([word]);
            else
                cells[^1].Add(word);
        }

        return cells;
    }
}
