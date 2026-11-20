using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Metered.Sample;
using Metered.Sample.Pages;
using Slider = Metered.Controls.Slider;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Slider page: its place in the catalogue (its issue's number, 55, before the theme), Metered's sliders
/// throughout, each within a phone's width, the first one's value said under it, and a drag on it in the page's
/// ScrollViewer moving the thumb, not the page.
/// </summary>
public class SliderPageTests
{
    [AvaloniaFact]
    public void The_Slider_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(SliderPage));

        Assert.Equal("Slider", entry.Title);
        Assert.Equal(55, typeof(SliderPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Switch") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Slider_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new SliderPage();
        using var scene = new Scene(page);
        var sliders = page.GetVisualDescendants().OfType<Slider>().ToList();
        Assert.Equal(7, sliders.Count);
        Assert.All(sliders, slider =>
        {
            Assert.NotNull(slider.Part(Slider.InnerTrackPart));
            Assert.True(slider.TranslatePoint(new Point(slider.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });
        Assert.Equal([25.0, 75.0], page.FindControl<Slider>("Range")!.Value);
        Assert.False(page.FindControl<Slider>("Disabled")!.IsEnabled);
        Assert.True(page.FindControl<Slider>("Marks")!.Marks);
        Assert.Equal("1.0km", page.FindControl<Slider>("Distance")!.ValueToLabel!(1000));
        Assert.All(page.FindControl<Slider>("Persistent")!.Thumbs, thumb => Assert.NotEmpty(thumb.Children));
    }

    [AvaloniaFact]
    public void The_First_Sliders_Value_Is_Said_Under_It_And_A_Touch_Drag_Moves_It_Not_The_Page()
    {
        var page = new SliderPage();
        using var scene = new Scene(page, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        var basic = page.FindControl<Slider>("Basic")!;
        var state = page.FindControl<TextBlock>("State")!;
        var scroller = page.GetVisualDescendants().OfType<ScrollViewer>().First();
        Assert.Equal("Value: 60", state.Text);

        var inner = (Visual)basic.Part(Slider.InnerTrackPart)!;
        Point On(double fraction) => inner.TranslatePoint(new Point(inner.Bounds.Width * fraction, 1), scene.Window)!.Value;
        var from = scene.CentreOf(basic.Thumbs[0]);
        var touch = scene.Window.TouchBegin(from, RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            scene.Window.TouchMove(touch, new Point(from.X + (On(0.2).X - from.X) * i / 10, from.Y - 12 * i), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, new Point(On(0.2).X, from.Y - 120), RawInputModifiers.None);
        scene.Pump();
        Assert.Equal("Value: 20", state.Text);
        Assert.Equal(0, scroller.Offset.Y);
    }
}
