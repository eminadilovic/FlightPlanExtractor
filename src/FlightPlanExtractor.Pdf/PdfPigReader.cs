using FlightPlanExtractor.Core.Reading;
using UglyToad.PdfPig;
using PdfPigPage = UglyToad.PdfPig.Content.Page;
using PdfPigWord = UglyToad.PdfPig.Content.Word;

namespace FlightPlanExtractor.Pdf;

/// <summary><see cref="IPdfReader"/> backed by PdfPig. The only place that references the library.</summary>
public sealed class PdfPigReader : IPdfReader
{
    public IReadOnlyList<PageContent> Read(Stream pdf)
    {
        try
        {
            if (pdf.CanSeek) return ReadPages(pdf);

            // PdfPig jumps around in the file, which e.g. an HTTP upload stream does not support.
            using var copy = new MemoryStream();
            pdf.CopyTo(copy);
            copy.Position = 0;
            return ReadPages(copy);
        }
        // PdfPig has no common base exception; a broken file can surface as several unrelated types.
        catch (Exception ex)
        {
            throw new DocumentUnreadableException("The file could not be read as a PDF document.", ex);
        }
    }

    private static IReadOnlyList<PageContent> ReadPages(Stream seekablePdf)
    {
        using var document = PdfDocument.Open(seekablePdf);
        return document.GetPages().Select(ToPageContent).ToList();
    }

    private static PageContent ToPageContent(PdfPigPage page)
    {
        var words = page.GetWords()
            .Where(w => !string.IsNullOrWhiteSpace(w.Text))
            .Select(ToWord)
            .ToList();

        return new PageContent(page.Number, words);
    }

    private static Word ToWord(PdfPigWord word) => new(
        word.Text,
        word.BoundingBox.Left,
        word.BoundingBox.Right,
        word.Letters[0].StartBaseLine.Y);
}
