using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Switch page: its place in the catalogue (its issue's number, 43, before the theme), Metered's switches
/// throughout, the first one's state said under it, and every switch within a phone's width.
/// </summary>
public class SwitchPageTests
{
    [AvaloniaFact]
    public void The_Switch_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(SwitchPage));

        Assert.Equal("Switch", entry.Title);
        Assert.Equal(43, typeof(SwitchPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Radio") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Switch_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new SwitchPage();
        using var scene = new Scene(page);
        var switches = page.GetVisualDescendants().OfType<Switch>().ToList();
        Assert.Equal(10, switches.Count);
        Assert.All(switches, sw =>
        {
            Assert.NotNull(sw.Part(Switch.ToggleTrackPart));
            Assert.True(sw.TranslatePoint(new Point(sw.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });
        Assert.Equal(SwitchSize.Small, page.FindControl<Switch>("Small")!.Size);
        Assert.Equal(SwitchLabelPlacement.Left, page.FindControl<Switch>("Left")!.LabelPlacement);
        Assert.True(page.FindControl<Switch>("Icon")!.ShowIcon);
    }

    [AvaloniaFact]
    public void The_First_Switchs_State_Is_Said_Under_It()
    {
        var page = new SwitchPage();
        using var scene = new Scene(page);
        var basic = page.FindControl<Switch>("Basic")!;
        var state = page.FindControl<Avalonia.Controls.TextBlock>("State")!;
        Assert.Equal("Off", state.Text);
        scene.Click(basic);
        Assert.Equal("On", state.Text);
        scene.Click(basic);
        Assert.Equal("Off", state.Text);
    }
}
