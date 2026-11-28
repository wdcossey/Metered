using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Tabs page: its place in the catalogue (its issue's number, 155, before the theme), Metered's tabs
/// throughout, each within a phone's width, its examples' props (tabs-motion's docs examples), and the scroll scenario's
/// eighth tab centred.
/// </summary>
public class TabsPageTests
{
    [AvaloniaFact]
    public void The_Tabs_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(TabsPage));
        Assert.Equal("Tabs", entry.Title);
        Assert.Equal(155, typeof(TabsPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Tabs_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new TabsPage();
        using var scene = new Scene(page);
        var all = page.GetVisualDescendants().OfType<Tabs>().ToList();
        Assert.Equal(8, all.Count);
        Assert.All(all, t =>
        {
            Assert.NotNull(t.Part(Tabs.TabListPart));
            Assert.True(t.TranslatePoint(new Point(t.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
            Assert.Equal(t.Keys[0], t.ActiveKey == "8" ? "0" : t.ActiveKey);
        });
        Assert.False(page.FindControl<Tabs>("Manual")!.ActivateOnFocus);
        Assert.Equal(Orientation.Vertical, page.FindControl<Tabs>("Vertical")!.Orientation);
        Assert.Equal(TabsFill.Fixed, page.FindControl<Tabs>("Fixed")!.Fill);
        Assert.True(page.FindControl<Tabs>("Disabled")!.Disabled);
        Assert.Equal(3, page.FindControl<Tabs>("Artwork")!.Elements(Tabs.ArtworkContainerPart).Count);
        Assert.True(((Control)page.FindControl<Tabs>("Enhancer")!.Part(Tabs.EndEnhancerContainerPart)!).IsVisible);
        Assert.Equal(20, page.FindControl<Tabs>("Scroll")!.Keys.Count);
    }

    // The Disabled example's other tabs can't be chosen; the Basics' can.
    [AvaloniaFact]
    public void The_Tabs_Are_Chosen_By_A_Click()
    {
        var page = new TabsPage();
        using var scene = new Scene(page);
        var basic = page.FindControl<Tabs>("Basic")!;
        scene.Click((Visual)basic.Elements(Tabs.TabPart)[2]);
        Assert.Equal("2", basic.ActiveKey);
        var disabled = page.FindControl<Tabs>("Disabled")!;
        Assert.False(((Control)disabled.Elements(Tabs.TabPart)[1]).IsEnabled);
    }

    // tabs-motion-scroll.scenario.tsx: the eighth tab centred in the list on mount.
    [AvaloniaFact]
    public void The_Scroll_Examples_Active_Tab_Is_Centred()
    {
        var page = new TabsPage();
        using var scene = new Scene(page);
        var tabs = page.FindControl<Tabs>("Scroll")!;
        tabs.BringIntoView();
        scene.Pump();
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        var b = (Control)tabs.Elements(Tabs.TabPart)[8];
        Assert.True(list.Offset.X > 0);
        Assert.Equal(b.Bounds.X + b.Bounds.Width / 2 - list.Viewport.Width / 2, list.Offset.X, 0);
    }
}
