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
using Metered.Themes;
using MeteredButton = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Tooltip page: its place in the catalogue (its issue's number, 154, after Menu and before the theme), the
/// documentation's examples and scenarios as set; a finger's tap toggling a tooltip and a tap elsewhere closing it
/// (#123); in the shell, Back closing the one open from the start before it goes back (#123).
/// </summary>
public class TooltipPageTests
{
    private static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    [AvaloniaFact]
    public void The_Tooltip_Page_Takes_Its_Issues_Number_After_Menu()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(TooltipPage));
        Assert.Equal("Tooltip", entry.Title);
        Assert.Equal(154, typeof(TooltipPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Menu") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void The_Examples_Are_As_Base_Webs_Documentation_And_Scenarios_Set_Them()
    {
        var page = new TooltipPage();
        using var scene = PopoverTests.Show(page);
        PopoverTests.Settle(scene);
        // Open, stateful, complex content, the arrow, two buttons.
        Assert.Equal(6, page.GetVisualDescendants().OfType<Tooltip>().Count());
        Assert.True(page.Open.IsOpen);
        Assert.Equal(PopoverPlacement.TopLeft, page.Open.Placement);
        Assert.Equal(8, page.Open.PopoverMargin);
        Assert.True(page.Open.ShowArrow);
        Assert.Equal("Tooltips display short messages.", page.Stateful.Content);
        Assert.Equal(TooltipOracle.Value.Defaults.Stateful.PopoverMargin, page.Stateful.PopoverMargin);
        Assert.True(page.Complex.AutoFocus);
        Assert.Same(BaseTheme.DarkTheme, ThemeProvider.GetTheme(page.ComplexContent));
        Assert.True(page.Arrow.ShowArrow);
        Assert.Equal(20, page.Arrow.PopoverMargin);
        Assert.All(page.Buttons.Children.OfType<Tooltip>(), t => Assert.IsType<MeteredButton>(t.Anchor));
        Assert.Equal(2, page.Buttons.Children.OfType<Tooltip>().Count());
    }

    // A finger never hovers: a tap on an anchor toggles its tooltip, and a tap elsewhere closes one a tap opened (#123).
    [AvaloniaFact]
    public void A_Tap_Toggles_A_Tooltip_And_A_Tap_Elsewhere_Closes_It()
    {
        var page = new TooltipPage();
        using var scene = PopoverTests.Show(page);
        var button = (MeteredButton)page.Buttons.Children.OfType<Tooltip>().First().Anchor!;
        button.BringIntoView();
        PopoverTests.Settle(scene);
        var tooltip = page.Buttons.Children.OfType<Tooltip>().First();
        Tap(scene, scene.CentreOf(button));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        Tap(scene, scene.CentreOf(button));
        PopoverTests.Settle(scene, 60);
        Assert.False(tooltip.IsOpen);

        var anchor = (Control)page.Arrow.Anchor!;
        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene);
        Assert.True(page.Arrow.IsOpen);
        Tap(scene, new Point(370, 830));
        Assert.False(page.Arrow.IsOpen);
    }

    // Back closes the tooltip open from the start, and the shell stays on the page; then it goes back (#123).
    [AvaloniaFact]
    public void Back_Closes_The_Open_Tooltip_Before_The_Shell_Goes_Back()
    {
        var shell = new SampleShell { Report = _ => { } };
        using var scene = PopoverTests.Show(shell);
        shell.Open("Tooltip");
        ShellFrames.Draw();
        var page = Assert.IsType<TooltipPage>(shell.Shown);
        PopoverTests.Settle(scene);
        Assert.True(page.Open.IsOpen);
        Assert.True(ShellFrames.Back(scene.Window));
        Assert.False(page.Open.IsOpen);
        Assert.True(shell.IsShowingPage);
        PopoverTests.Settle(scene, 60);
        Assert.True(ShellFrames.Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }
}
