using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;
using Metered.Tests.Controls;
using MeteredButton = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Popover page: its place in the catalogue (its issue's number, 106, after FormControl and before the
/// theme), the documentation's examples as set; a finger's tap opening a popover and its tap outside closing it; the
/// dismiss example closed from its content and by nothing else; each placement.
/// </summary>
public class PopoverPageTests
{
    private static Scene Show(PopoverPage page) => PopoverTests.Show(page);

    private static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    private static void Into(Scene scene, Control control)
    {
        control.BringIntoView();
        scene.Pump();
    }

    [AvaloniaFact]
    public void The_Popover_Page_Takes_Its_Issues_Number_After_FormControl()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(PopoverPage));
        Assert.Equal("Popover", entry.Title);
        Assert.Equal(106, typeof(PopoverPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.Equal(titles.IndexOf("FormControl") + 1, titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void The_Examples_Are_As_Base_Webs_Documentation_Sets_Them()
    {
        var page = new PopoverPage();
        using var scene = Show(page);
        PopoverTests.Settle(scene);
        var popovers = page.GetVisualDescendants().OfType<Popover>().ToList();
        // stateless, stateful-click, with-arrow, stateful-hover, dismiss, the 13 placements, overrides.
        Assert.Equal(19, popovers.Count);
        Assert.True(page.AlwaysOpen.IsOpen);
        Assert.True(page.Overridden.IsOpen);
        Assert.True(page.Arrow.ShowArrow);
        Assert.Equal(PopoverTriggerType.Hover, page.Hover.TriggerType);
        Assert.False(page.Dismiss.DismissOnEsc);
        Assert.False(page.Dismiss.DismissOnClickOutside);
        Assert.Equal(Enum.GetValues<PopoverPlacement>(), page.Placements.Children.OfType<Popover>().Select(p => p.Placement));
        Assert.Equal(3, page.Overridden.Overrides!.Count);
    }

    [AvaloniaFact]
    public void A_Tap_Opens_The_Click_Example_And_A_Tap_Outside_Closes_It()
    {
        var page = new PopoverPage();
        using var scene = Show(page);
        var open = (MeteredButton)page.Click.Anchor!;
        Tap(scene, scene.CentreOf(open));
        PopoverTests.Settle(scene);
        Assert.True(page.Click.IsOpen);
        Tap(scene, new Point(370, 830));
        Assert.False(page.Click.IsOpen);
    }

    [AvaloniaFact]
    public void The_Dismiss_Example_Is_Closed_By_Its_Content_Alone()
    {
        var page = new PopoverPage();
        using var scene = Show(page);
        var anchor = (MeteredButton)page.Dismiss.Anchor!;
        Into(scene, anchor);
        scene.Click(anchor);
        PopoverTests.Settle(scene);
        Assert.True(page.Dismiss.IsOpen);
        scene.Key(Key.Escape);
        scene.Window.MouseDown(new Point(370, 830), MouseButton.Left);
        scene.Window.MouseUp(new Point(370, 830), MouseButton.Left);
        scene.Pump();
        Assert.True(page.Dismiss.IsOpen);
        var dismiss = ((Control)page.Dismiss.Part(Popover.InnerPart)!).GetVisualDescendants().OfType<MeteredButton>().Single();
        Assert.Equal("Dismiss", dismiss.Content);
        scene.Click(dismiss);
        Assert.False(page.Dismiss.IsOpen);
    }

    [AvaloniaFact]
    public void Each_Placement_Opens_On_Its_Side_Or_Flipped()
    {
        var page = new PopoverPage();
        using var scene = Show(page);
        foreach (var popover in page.Placements.Children.OfType<Popover>().Where(p => p.Placement != PopoverPlacement.Auto))
        {
            var anchor = (MeteredButton)popover.Anchor!;
            Into(scene, anchor);
            scene.Click(anchor);
            PopoverTests.Settle(scene, 150);
            Assert.True(popover.IsOpen, $"{popover.Placement}");
            // Where it says, or flipped to the other side where a phone's width has no room (popper.js's flip).
            var asked = Tether.SideOf(popover.Placement);
            var got = Tether.SideOf(popover.ActualPlacement);
            Assert.True(got == asked || (int)got == ((int)asked + 2) % 4, $"{popover.Placement} shown {popover.ActualPlacement}");
            Assert.Equal(popover.Placement.ToString()[asked.ToString().Length..], popover.ActualPlacement.ToString()[got.ToString().Length..]);
            scene.Click(anchor);
            PopoverTests.Settle(scene, 60);
            Assert.False(popover.IsOpen, $"{popover.Placement}");
        }
    }
}
