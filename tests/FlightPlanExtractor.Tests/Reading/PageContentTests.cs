using FlightPlanExtractor.Core.Reading;

namespace FlightPlanExtractor.Tests.Reading;

public class PageContentTests
{
    [Fact]
    public void Lines_are_ordered_top_to_bottom_and_words_left_to_right()
    {
        var page = new PageContent(1,
        [
            new Word("34066", Left: 100, Right: 130, Baseline: 500),
            new Word("To:",   Left: 10,  Right: 25,  Baseline: 700),
            new Word("ZFM:",  Left: 60,  Right: 85,  Baseline: 500),
            new Word("LIMC",  Left: 40,  Right: 65,  Baseline: 700),
        ]);

        Assert.Equal(["To: LIMC", "ZFM: 34066"], page.Lines.Select(l => l.Text));
    }

    [Fact]
    public void Words_with_slightly_different_baselines_stay_on_one_line()
    {
        var page = new PageContent(1,
        [
            new Word("ZFM:",  Left: 10, Right: 35, Baseline: 500.0),
            new Word("34066", Left: 40, Right: 70, Baseline: 499.2),
            new Word("kg",    Left: 75, Right: 85, Baseline: 500.6),
        ]);

        Assert.Equal("ZFM: 34066 kg", Assert.Single(page.Lines).Text);
    }

    [Fact]
    public void Page_without_words_has_no_lines()
    {
        Assert.Empty(new PageContent(1, []).Lines);
    }
}
