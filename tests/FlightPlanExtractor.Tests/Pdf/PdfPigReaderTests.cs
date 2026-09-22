using System.Text;
using FlightPlanExtractor.Core.Reading;
using FlightPlanExtractor.Pdf;

namespace FlightPlanExtractor.Tests.Pdf;

public class PdfPigReaderTests
{
    private readonly PdfPigReader _reader = new();

    [Fact]
    public void Reads_all_pages_of_the_sample_document()
    {
        using var pdf = File.OpenRead(SampleFiles.SamplePdf);

        var pages = _reader.Read(pdf);

        Assert.Equal(140, pages.Count);
        Assert.Equal(Enumerable.Range(1, 140), pages.Select(p => p.Number));
        Assert.Contains(pages[4].Lines, line => line.Text.Contains("Operational Flight Plan"));
    }

    [Fact]
    public void Non_pdf_content_throws_DocumentUnreadableException()
    {
        using var notAPdf = new MemoryStream(Encoding.UTF8.GetBytes("this is not a pdf"));

        Assert.Throws<DocumentUnreadableException>(() => _reader.Read(notAPdf));
    }

    [Fact]
    public void Truncated_pdf_throws_DocumentUnreadableException()
    {
        var firstBytes = File.ReadAllBytes(SampleFiles.SamplePdf).Take(2000).ToArray();
        using var truncated = new MemoryStream(firstBytes);

        Assert.Throws<DocumentUnreadableException>(() => _reader.Read(truncated));
    }

    [Fact]
    public void Empty_stream_throws_DocumentUnreadableException()
    {
        using var empty = new MemoryStream();

        Assert.Throws<DocumentUnreadableException>(() => _reader.Read(empty));
    }
}
