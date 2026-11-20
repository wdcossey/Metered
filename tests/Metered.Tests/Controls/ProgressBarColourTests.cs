using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Metered.Controls;
using ProgressBar = Metered.Controls.ProgressBar;

namespace Metered.Tests.Controls;

/// <summary>
/// A progress bar for each intent and none, light and dark, determinate with its label and infinite: the track's, the
/// fill's, the infinite bars' and the label's colours against Base Web's (ProgressBarOracle).
/// </summary>
public class ProgressBarColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, ProgressBarOracle.Data.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = ProgressBarOracle.Data.Colours[index];
        var intent = c.Intent is null ? (ProgressBarIntent?)null : Enum.Parse<ProgressBarIntent>(c.Intent, ignoreCase: true);
        var bar = new ProgressBar { Value = 40, ShowLabel = true, Intent = intent };
        var infinite = new ProgressBar { Infinite = true, Intent = intent };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Spacing = 20, Children = { bar, infinite } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Equal(c.Bar, Colour(((Border)bar.Part(ProgressBar.BarPart)!).Background));
        Assert.Equal(c.BarProgress, Colour(((ProgressBarFill)bar.Part(ProgressBar.BarProgressPart)!).Background));
        Assert.Equal(c.Label, Colour(((TextBlock)bar.Part(ProgressBar.LabelPart)!).Foreground));
        var halves = infinite.Elements(ProgressBar.InfiniteBarPart);
        Assert.Equal(2, halves.Count);
        Assert.All(halves, half => Assert.Equal(c.Infinite, Colour(((ProgressBarInfiniteBar)half).Fill)));
        // No track in the infinite bar: Base Web renders its two halves alone.
        Assert.Empty(infinite.Elements(ProgressBar.BarPart));
    }

    private static string Colour(Avalonia.Media.IBrush? brush) => RadioColourTests.Colour(brush);
}
