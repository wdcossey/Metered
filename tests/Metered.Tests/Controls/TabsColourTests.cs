using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A tab in each of its states, light and dark (TabsOracle: tab's style function with its :hover, :disabled and
/// :disabled:hover resolved): its fill once its transition has run, its text's colour, its focus ring (Base Web's
/// inset outline), and the highlight's and the border's colours. The states are reached as a user reaches them: a real
/// mouse over the tab (a mouse's hover), a keyboard's focus, a tab disabled.
/// </summary>
public class TabsColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, TabsOracle.Data.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = TabsOracle.Data.Colours[index];
        var tabs = new Tabs
        {
            ActiveKey = "0",
            Items = { new Tab { Title = "Robot" }, new Tab { Title = "Monster", Disabled = c.State.StartsWith("disabled") }, new Tab { Title = "Watermelon" } },
        };
        using var scene = new Scene(new Border { Padding = new Thickness(0, 20), Child = tabs }, c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        var active = c.State.StartsWith("active") || c.State == "focusVisibleHover";
        var tab = active ? buttons[0] : buttons[1];
        if (c.State.StartsWith("focusVisible")) Assert.True(tab.Focus(NavigationMethod.Directional));
        if (c.State.EndsWith("Hover") || c.State == "hover") scene.Hover(tab);
        else scene.Window.MouseMove(new Point(5, 5));
        // Past the fill's transition (timing200).
        scene.Advance(300);

        Assert.Equal(c.Background, Colour(tab.Background));
        Assert.Equal(c.Foreground, Colour(tab.Foreground));
        if (c.Outline is { } o)
        {
            Assert.Equal(o.Colour, Colour(tab.OutlineBrush));
            Assert.Equal(o.Width, tab.OutlineThickness);
            Assert.Equal(o.Offset, tab.OutlineOffset);
        }
        else
        {
            Assert.True(tab.OutlineBrush is null || tab.OutlineThickness == 0);
        }

        Assert.Equal(c.Highlight, Colour(((TabHighlight)tabs.Part(Tabs.TabHighlightPart)!).Background));
        Assert.Equal(c.Border, Colour(((Border)tabs.Part(Tabs.TabBorderPart)!).Background));
    }

    // The focus ring is drawn inside the tab (outline-offset -3px, as wide as the outline): the tab's own edge pixels are
    // the ring's colour, and the ring takes no room.
    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_focus_ring_is_drawn_inside_the_tab(string theme)
    {
        var c = TabsOracle.Data.Colours.Single(x => x.Theme == theme && x.State == "focusVisible");
        var tabs = new Tabs { ActiveKey = "0", Items = { new Tab { Title = "Robot" }, new Tab { Title = "Monster" } } };
        using var scene = new Scene(new Border { Padding = new Thickness(0, 20), Child = tabs }, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var tab = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ElementAt(1);
        var width = tab.Bounds.Width;
        Assert.True(tab.Focus(NavigationMethod.Directional));
        var frame = scene.Capture();
        Assert.Equal(width, tab.Bounds.Width);
        var at = tab.TranslatePoint(new Point(1, tab.Bounds.Height / 2), scene.Window)!.Value;
        Assert.Equal(Color.Parse(c.Outline!.Colour), Frames.PixelAt(frame, (int)at.X, (int)at.Y));
        var inside = tab.TranslatePoint(new Point(c.Outline.Width + 1, tab.Bounds.Height / 2), scene.Window)!.Value;
        Assert.Equal(Color.Parse(c.Background), Frames.PixelAt(frame, (int)inside.X, (int)inside.Y));
    }

    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);
}
