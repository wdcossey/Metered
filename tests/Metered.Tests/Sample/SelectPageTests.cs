using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;
using Metered.Tests.Controls;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Select page: its place in the catalogue (its issue's number, 97, after FormControl and before Popover), the
/// documentation's examples as set, each within a phone's width; a finger's drag over the page scrolling it and opening
/// nothing; a tap opening a select's dropdown on the screen, and a tap there choosing.
/// </summary>
public class SelectPageTests
{
    [AvaloniaFact]
    public void The_Select_Page_Takes_Its_Issues_Number_After_FormControl()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(SelectPage));
        Assert.Equal("Select", entry.Title);
        Assert.Equal(97, typeof(SelectPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.Equal(titles.IndexOf("FormControl") + 1, titles.IndexOf(entry.Title));
        Assert.Equal(titles.IndexOf("Popover") - 1, titles.IndexOf(entry.Title));
    }

    [AvaloniaFact]
    public void The_Examples_Are_As_Base_Webs_Documentation_Sets_Them_Within_A_Phones_Width()
    {
        var page = new SelectPage();
        using var scene = SelectKit.Show(page);
        var selects = page.GetVisualDescendants().OfType<Select>().ToList();
        Assert.Equal(15, selects.Count);
        Assert.All(selects, s => Assert.True(s.TranslatePoint(new Point(s.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width));
        Assert.Equal(SelectType.Search, page.Search.Type);
        Assert.True(page.SearchMulti.Multi);
        Assert.Equal(2, page.SearchMulti.Tags.Count);
        Assert.True(page.Creatable.Creatable && page.CreatableMulti.Creatable && page.CreatableMulti.Multi);
        Assert.Equal([SelectSize.Mini, SelectSize.Compact, SelectSize.Default, SelectSize.Large], new[] { page.Mini, page.Compact, page.Default, page.Large }.Select(s => s.Size));
        // Base Web's heights by size, a value in each.
        var heights = new[] { "mini", "compact", "default", "large" }.Select(size => SelectOracle.Data.Layout.Single(l => l.Size == size && l.Kind == "single, a value").Height);
        Assert.Equal(heights, new[] { page.Mini, page.Compact, page.Default, page.Large }.Select(s => s.Bounds.Height));
        Assert.Equal(2, page.Grouped.Options!.OfType<SelectGroup>().Count());
        Assert.False(page.Native.Searchable);
        Assert.True(page.Error.Error && page.Positive.Positive && !page.Disabled.IsEnabled && page.Loading.IsLoading);
    }

    [AvaloniaFact]
    public void A_Fingers_Drag_Over_The_Page_Scrolls_It_And_Opens_Nothing()
    {
        var page = new SelectPage();
        using var scene = SelectKit.Show(page);
        var scroller = page.GetVisualDescendants().OfType<ScrollViewer>().First();
        SelectKit.Drag(scene, SelectKit.ControlPoint(scene, page.Basic), dy: -150);
        Assert.True(scroller.Offset.Y > 60, "the page moved " + scroller.Offset.Y);
        Assert.False(page.Basic.IsOpen);
    }

    [AvaloniaFact]
    public void A_Tap_Opens_The_Basic_Selects_Dropdown_On_The_Screen_And_A_Tap_There_Chooses()
    {
        var page = new SelectPage();
        using var scene = SelectKit.Show(page);
        SelectKit.Tap(scene, SelectKit.ControlPoint(scene, page.Basic));
        PopoverTests.Settle(scene);
        Assert.True(page.Basic.IsOpen);
        var list = (Control)page.Basic.DropdownMenu!.Part(Metered.Controls.Menu.ListPart)!;
        var bottom = list.TranslatePoint(new Point(0, list.Bounds.Height), scene.Window)!.Value.Y;
        Assert.True(bottom <= Scene.Height, "the dropdown's foot at " + bottom);
        SelectKit.Tap(scene, scene.CentreOf(SelectKit.Option(page.Basic, "Aqua")));
        Assert.Equal(["Aqua"], SelectKit.Labels(page.Basic.Value));
        Assert.False(page.Basic.IsOpen);
    }
}
