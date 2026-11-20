using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;
using ProgressBar = Metered.Controls.ProgressBar;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's ProgressBar page: its place in the catalogue (its issue's number, 53, before the theme), Metered's bars
/// throughout, each within a phone's width, the custom label (custom-label.tsx), the negative override, and the stepped
/// bar's buttons (steps.tsx).
/// </summary>
public class ProgressBarPageTests
{
    [AvaloniaFact]
    public void The_ProgressBar_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(ProgressBarPage));

        Assert.Equal("ProgressBar", entry.Title);
        Assert.Equal(53, typeof(ProgressBarPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Switch") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Bar_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);
        var bars = page.GetVisualDescendants().OfType<ProgressBar>().ToList();
        Assert.Equal(13, bars.Count);
        Assert.All(bars, pb =>
        {
            Assert.NotNull(pb.Part(ProgressBar.BarContainerPart));
            Assert.True(pb.Bounds.Width > 0);
            Assert.True(pb.TranslatePoint(new Point(pb.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });
        Assert.Equal(ProgressBarSize.Small, page.FindControl<ProgressBar>("Small")!.Size);
        Assert.Equal(ProgressBarIntent.Negative, page.FindControl<ProgressBar>("Intent")!.Intent);
        Assert.True(page.FindControl<ProgressBar>("Infinite")!.Infinite);
        Assert.Equal(2, page.FindControl<ProgressBar>("Infinite")!.Elements(ProgressBar.InfiniteBarPart).Count);
    }

    // custom-label.tsx: `${(MAX_MB_VALUE / 100) * value}mb out of ${MAX_MB_VALUE}mb downloaded`, at 50.
    [AvaloniaFact]
    public void The_Custom_Label_Says_Megabytes()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);
        var custom = page.FindControl<ProgressBar>("Custom")!;
        Assert.Equal("500mb out of 1000mb downloaded", ((TextBlock)custom.Part(ProgressBar.LabelPart)!).Text);
    }

    // negative.tsx: BarProgress's backgroundColor, colors.negative, by override.
    [AvaloniaFact]
    public void The_Negative_Bars_Fill_Is_Its_Overrides()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);
        var fill = (ProgressBarFill)page.FindControl<ProgressBar>("Negative")!.Part(ProgressBar.BarProgressPart)!;
        Assert.True(scene.Window.TryFindResource("colors.negative", scene.Window.ActualThemeVariant, out var negative));
        Assert.Equal(Metered.Tests.Controls.RadioColourTests.Colour((Avalonia.Media.IBrush)negative!), Metered.Tests.Controls.RadioColourTests.Colour(fill.Background));
    }

    // steps.tsx: Next and Previous move the value a step (100 / 5); Previous is off at 0 and at 100, Next at 100, and
    // Next says Complete from 80.
    [AvaloniaFact]
    public void The_Steps_Buttons_Move_The_Value_A_Step()
    {
        var page = new ProgressBarPage();
        using var scene = new Scene(page);
        var steps = page.FindControl<ProgressBar>("Steps")!;
        var previous = page.FindControl<Metered.Controls.Button>("Previous")!;
        var next = page.FindControl<Metered.Controls.Button>("Next")!;
        Assert.Equal(40, steps.Value);
        Assert.Equal("Next Step", next.Content);

        next.BringIntoView();
        scene.Pump();
        scene.Click(next);
        Assert.Equal(60, steps.Value);
        scene.Click(next);
        Assert.Equal(80, steps.Value);
        Assert.Equal("Complete", next.Content);
        scene.Click(next);
        Assert.Equal(100, steps.Value);
        Assert.False(next.IsEnabled);
        Assert.False(previous.IsEnabled);
        Assert.All(steps.Elements(ProgressBar.BarProgressPart), fill => Assert.Contains(":step-completed", fill.Classes));

        steps.Value = 20;
        scene.Click(previous);
        Assert.Equal(0, steps.Value);
        Assert.False(previous.IsEnabled);
        Assert.True(next.IsEnabled);
    }
}
