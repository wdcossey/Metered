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
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Menu page: its place in the catalogue (its issue's number, 107, after Popover and before the theme), the
/// documentation's examples as set, each within a phone's width; a finger's drag over the long list scrolling it; a tap on
/// Open Recent opening its child beside it, on the screen, and a tap there choosing; in the shell, Back closing that child
/// before it goes back (#131).
/// </summary>
public class MenuPageTests
{
    [AvaloniaFact]
    public void The_Menu_Page_Takes_Its_Issues_Number_After_Popover()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(MenuPage));
        Assert.Equal("Menu", entry.Title);
        Assert.Equal(107, typeof(MenuPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Popover") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void The_Examples_Are_As_Base_Webs_Documentation_Sets_Them_Within_A_Phones_Width()
    {
        var page = new MenuPage();
        using var scene = PopoverTests.Show(page);
        var menus = page.GetVisualDescendants().OfType<Menu>().ToList();
        // basic, compact, profile, the child menu's parent, grouped, long, href, dividers, empty.
        Assert.Equal(9, menus.Count);
        Assert.All(menus, m => Assert.True(m.TranslatePoint(new Point(m.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width));
        // basic.tsx's 250 px and dividers.tsx's, each and its padding; its Item Three and Four disabled.
        Assert.Equal(266, ((Control)page.Basic.Part(Menu.ListPart)!).Bounds.Height);
        Assert.Equal(266, ((Control)page.Dividers.Part(Menu.ListPart)!).Bounds.Height);
        Assert.True(page.Basic.OptionAt(2)!.Disabled && page.Basic.OptionAt(3)!.Disabled);
        Assert.All(page.Compact.Elements(Menu.OptionPart).Cast<MenuOptionList>(), o => Assert.Equal(MenuOptionSize.Compact, o.Size));
        Assert.All(page.Profile.Elements(Menu.OptionPart), o => Assert.IsType<MenuOptionProfile>(o));
        Assert.Equal(2, page.Grouped.Elements(Menu.OptgroupHeaderPart).Count);
        Assert.Equal(MenuPage.LongCount, page.Long.Elements(Menu.OptionPart).Count);
        Assert.Equal(3, page.Href.Elements(Menu.OptionPart).Count(o => ((MenuOptionList)o).Part(MenuOptionList.ListItemAnchorPart) is not null));
        Assert.Equal(2, page.Dividers.Elements(Menu.MenuDividerPart).Count);
        Assert.Single(page.Empty.Elements(Menu.EmptyStatePart));
    }

    [AvaloniaFact]
    public void A_Fingers_Drag_Scrolls_The_Long_List_And_Chooses_Nothing()
    {
        var page = new MenuPage();
        using var scene = PopoverTests.Show(page);
        var chosen = new List<object>();
        page.Long.ItemSelect += (_, e) => chosen.Add(e.Item);
        page.Long.BringIntoView();
        scene.Pump();
        var list = (MenuList)page.Long.Part(Menu.ListPart)!;
        var at = scene.CentreOf(list);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        for (var i = 1; i <= 15; i++)
        {
            scene.Window.TouchMove(touch, at - new Vector(0, 8 * i), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, at - new Vector(0, 120), RawInputModifiers.None);
        scene.Pump();
        Assert.True(list.Offset.Y > 60, $"the list moved {list.Offset.Y}");
        Assert.Empty(chosen);
        Assert.Equal(-1, page.Long.HighlightedIndex);
    }

    [AvaloniaFact]
    public void A_Tap_On_Open_Recent_Opens_Its_Child_On_The_Screen_And_A_Tap_There_Chooses()
    {
        var page = new MenuPage();
        using var scene = PopoverTests.Show(page);
        var parent = page.Child;
        var option = parent.OptionAt(4)!;
        option.BringIntoView();
        scene.Pump();
        MenuTests.Tap(scene, scene.CentreOf(option));
        PopoverTests.Settle(scene);
        Assert.Equal(4, parent.HighlightedIndex);
        var child = Assert.IsType<Menu>(option.ChildMenu);
        Assert.True(option.ChildMenuPopover!.IsOpen);
        var list = (Control)child.Part(Menu.ListPart)!;
        var right = list.TranslatePoint(new Point(list.Bounds.Width, 0), scene.Window)!.Value.X;
        Assert.True(right <= Scene.Width, $"the child's right edge at {right}");
        var chosen = new List<object>();
        child.ItemSelect += (_, e) => chosen.Add(e.Item);
        MenuTests.Tap(scene, scene.CentreOf(child.OptionAt(1)!));
        Assert.Equal("~/workspace/baseui", ((MenuItemData)Assert.Single(chosen)).Label);
        Assert.True(option.ChildMenuPopover!.IsOpen);
    }

    // Back with the child menu open is its Escape: the parent reset and the child closed, and the shell stays on the page;
    // with the child closed, the shell goes back (#131).
    [AvaloniaFact]
    public void Back_Closes_The_Child_Menu_Before_The_Shell_Goes_Back()
    {
        // No cost footer over the page's foot, where the option is brought into view.
        var shell = new SampleShell { Report = _ => { }, ShowsCost = false };
        using var scene = PopoverTests.Show(shell);
        shell.Open("Menu");
        ShellFrames.Draw();
        var page = Assert.IsType<MenuPage>(shell.Shown);
        var option = page.Child.OptionAt(4)!;
        option.BringIntoView();
        scene.Pump();
        MenuTests.Tap(scene, scene.CentreOf(option));
        PopoverTests.Settle(scene);
        Assert.True(option.ChildMenuPopover!.IsOpen);

        Assert.True(ShellFrames.Back(scene.Window));
        Assert.False(option.ChildMenuPopover!.IsOpen);
        Assert.Equal(-1, page.Child.HighlightedIndex);
        Assert.True(shell.IsShowingPage);

        PopoverTests.Settle(scene);
        Assert.True(ShellFrames.Back(scene.Window));
        Assert.False(shell.IsShowingPage);
    }
}
