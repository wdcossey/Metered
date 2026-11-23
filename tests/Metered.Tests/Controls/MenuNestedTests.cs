using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// Nested menus (nested-menus.js, maybe-child-menu.js, stateful-container.js), as child.tsx builds them: an option's child
/// menu in a Popover at its right, its top, open while the option is highlighted; ArrowRight and ArrowLeft between the
/// menus shown; Escape in a child resetting its parent; a mouse back over the parent letting a deeper menu's highlight go,
/// and 200 ms after it has left them all, every one's; a click on an option with a child choosing it; Tab kept in the
/// child; its name; the ChildMenuPopover override.
/// </summary>
public class MenuNestedTests
{
    private const string OpenRecent = "Open Recent ->";

    private static readonly string[] File = ["New File", "New Window", "Open...", "Open Workspace...", OpenRecent, "Add Folder to Workspace...", "Save"];

    private static readonly string[] RecentFiles = ["Reopen Closed Editor", "~/workspace/baseui", "~/workspace/styletron", "More..."];

    private sealed record Nest(NestedMenus Root, Menu Parent, Menu Child);

    /// <summary>child.tsx: the parent's options compact, "Open Recent ->" with a child menu of recent files.</summary>
    private static Nest Make(OverrideCollection? more = null)
    {
        var child = new Menu { ItemsSource = RecentFiles.Select(l => (object)new MenuItemData { Label = l }).ToList(), Width = 200 };
        var props = new ControlTheme(typeof(MenuOptionList));
        props.Setters.Add(new Setter(MenuOptionList.SizeProperty, MenuOptionSize.Compact));
        props.Setters.Add(new Setter(MenuOptionBase.GetChildMenuProperty, (Func<object, object?>)(item => item is MenuItemData { Label: OpenRecent } ? child : null)));
        var overrides = new OverrideCollection { new Override { Part = Menu.OptionPart, Props = props } };
        if (more is not null) overrides.AddRange(more);
        var parent = new Menu
        {
            ItemsSource = File.Select(l => (object)new MenuItemData { Label = l }).ToList(), Overrides = overrides, Width = 300,
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        };
        return new Nest(new NestedMenus { Child = parent, Margin = new Thickness(20) }, parent, child);
    }

    private static Scene Show(Nest n) => new(n.Root, width: 900, height: 700, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);

    private static MenuList List(Menu m) => (MenuList)m.Part(Menu.ListPart)!;

    private static Popover Popover(Menu parent) => parent.OptionAt(4)!.ChildMenuPopover!;

    private static bool Open(Menu parent) => Popover(parent).IsOpen && Popover(parent).PopupHost!.IsOpen;

    [AvaloniaFact]
    public void The_child_menu_is_open_at_its_options_right_while_the_option_is_highlighted()
    {
        var n = Make();
        using var scene = Show(n);
        List(n.Parent).Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.False(Open(n.Parent));
        foreach (var _ in Enumerable.Range(0, 4)) scene.Key(Key.Down);
        PopoverTests.Settle(scene);
        Assert.Equal(4, n.Parent.HighlightedIndex);
        Assert.True(Open(n.Parent));
        Assert.Equal(PopoverPlacement.RightTop, Popover(n.Parent).Placement);
        Assert.Equal(PopoverPlacement.RightTop, Popover(n.Parent).ActualPlacement);
        Assert.True(n.Child.IsAttachedToVisualTree());
        var option = n.Parent.OptionAt(4)!;
        Assert.True(scene.CentreOf(List(n.Child)).X > option.TranslatePoint(new Point(option.Bounds.Width, 0), scene.Window)!.Value.X);
        Assert.Equal(new[] { n.Parent, n.Child }, NestedMenus.Of(n.Parent)!.Menus);
        // nested-menus.js and option-list.js: the option says it opens a nested list.
        Assert.Equal(Menu.ParentMenuItemAriaLabel, MenuKit.Peer(option).GetName());

        scene.Key(Key.Down);
        PopoverTests.Settle(scene);
        Assert.False(Open(n.Parent));
        Assert.Equal(new[] { n.Parent }, NestedMenus.Of(n.Parent)!.Menus);
    }

    [AvaloniaFact]
    public void ArrowRight_focuses_the_child_and_ArrowLeft_the_parent()
    {
        var n = Make();
        using var scene = Show(n);
        List(n.Parent).Focus(NavigationMethod.Tab);
        scene.Pump();
        foreach (var _ in Enumerable.Range(0, 4)) scene.Key(Key.Down);
        PopoverTests.Settle(scene);
        scene.Key(Key.Right);
        Assert.True(List(n.Child).IsFocused);
        Assert.True(n.Child.IsMenuFocused);
        Assert.Equal(0, n.Child.HighlightedIndex);
        Assert.False(n.Parent.IsMenuFocused);
        Assert.Equal(4, n.Parent.HighlightedIndex);
        Assert.True(Open(n.Parent));
        // A keyboard's focus: the child's outline.
        Assert.Contains(":focus-visible", List(n.Child).Classes);
        scene.Key(Key.Down);
        Assert.Equal(1, n.Child.HighlightedIndex);
        Assert.Equal(4, n.Parent.HighlightedIndex);

        // Tab stays in the child menu (maybe-child-menu.js's Body onKeyDown).
        scene.Key(Key.Tab);
        Assert.True(List(n.Child).IsFocused);

        scene.Key(Key.Left);
        Assert.True(List(n.Parent).IsFocused);
        Assert.True(n.Parent.IsMenuFocused);
        Assert.False(n.Child.IsMenuFocused);
        Assert.Equal(4, n.Parent.HighlightedIndex);
        Assert.True(Open(n.Parent));
    }

    // A closed child is unmounted (renderAll false): opened again, it is a new container, at its initialState.
    [AvaloniaFact]
    public void A_child_menu_opened_again_starts_afresh()
    {
        var n = Make();
        using var scene = Show(n);
        List(n.Parent).Focus(NavigationMethod.Tab);
        scene.Pump();
        foreach (var _ in Enumerable.Range(0, 4)) scene.Key(Key.Down);
        PopoverTests.Settle(scene);
        scene.Key(Key.Right);
        scene.Key(Key.Down);
        scene.Key(Key.Down);
        Assert.Equal(2, n.Child.HighlightedIndex);
        scene.Key(Key.Left);
        scene.Key(Key.Down);
        PopoverTests.Settle(scene);
        Assert.False(Open(n.Parent));
        scene.Key(Key.Up);
        PopoverTests.Settle(scene);
        Assert.True(Open(n.Parent));
        Assert.Equal(-1, n.Child.HighlightedIndex);
        Assert.False(n.Child.IsMenuFocused);
    }

    // nested-menus.js's addMenuToNesting: only a menu shown (its offsetHeight not 0).
    [AvaloniaFact]
    public void A_hidden_menu_isnt_nested()
    {
        var shown = new Menu { ItemsSource = new List<object> { "a" } };
        var hidden = new Menu { ItemsSource = new List<object> { "b" }, IsVisible = false };
        var nest = new NestedMenus { Child = new StackPanel { Children = { shown, hidden } } };
        using var scene = new Scene(nest);
        Assert.Equal(new[] { shown }, nest.Menus);
    }

    // maybe-child-menu.js: the popover's onEsc is the parent's resetMenu.
    [AvaloniaFact]
    public void Escape_in_the_child_resets_the_parent_and_closes_the_child()
    {
        var n = Make();
        using var scene = Show(n);
        List(n.Parent).Focus(NavigationMethod.Tab);
        scene.Pump();
        foreach (var _ in Enumerable.Range(0, 4)) scene.Key(Key.Down);
        PopoverTests.Settle(scene);
        scene.Key(Key.Right);
        scene.Key(Key.Escape);
        PopoverTests.Settle(scene);
        Assert.Equal(-1, n.Parent.HighlightedIndex);
        Assert.False(n.Parent.IsMenuFocused);
        Assert.False(Open(n.Parent));
    }

    [AvaloniaFact]
    public void A_mouse_back_over_the_parent_lets_the_childs_highlight_go_and_leaving_them_all_every_ones()
    {
        var n = Make();
        using var scene = Show(n);
        scene.Hover(n.Parent.OptionAt(4)!);
        PopoverTests.Settle(scene);
        Assert.Equal(4, n.Parent.HighlightedIndex);
        Assert.True(Open(n.Parent));
        var nest = NestedMenus.Of(n.Parent)!;
        Assert.Equal(0, nest.HoverIndex);

        // Along the option to the child, then over its second option.
        var to = scene.CentreOf(n.Child.OptionAt(1)!);
        scene.Window.MouseMove(new Point(to.X, scene.CentreOf(n.Parent.OptionAt(4)!).Y));
        scene.Pump();
        scene.Hover(n.Child.OptionAt(1)!);
        Assert.Equal(1, nest.HoverIndex);
        Assert.True(n.Child.IsMenuFocused);
        Assert.Equal(1, n.Child.HighlightedIndex);
        Assert.Equal(4, n.Parent.HighlightedIndex);

        // Back over the parent's option: the child, deeper, lets its highlight go.
        scene.Hover(n.Parent.OptionAt(4)!);
        Assert.Equal(0, nest.HoverIndex);
        Assert.Equal(-1, n.Child.HighlightedIndex);
        Assert.Equal(4, n.Parent.HighlightedIndex);

        // Off them all: 200 ms later, every menu's.
        scene.Window.MouseMove(new Point(880, 680));
        scene.Pump();
        Assert.Equal(4, n.Parent.HighlightedIndex);
        PopoverTests.Settle(scene, 400);
        Assert.Equal(-1, nest.HoverIndex);
        Assert.Equal(-1, n.Parent.HighlightedIndex);
        Assert.False(Open(n.Parent));
    }

    [AvaloniaFact]
    public void A_click_on_an_option_with_a_child_chooses_it_once_and_keeps_the_child_open()
    {
        var n = Make();
        var chosen = new List<object>();
        n.Parent.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = Show(n);
        scene.Click(n.Parent.OptionAt(4)!);
        PopoverTests.Settle(scene);
        Assert.Equal(OpenRecent, ((MenuItemData)Assert.Single(chosen)).Label);
        Assert.True(Open(n.Parent));
        // A controlled popover's anchor click changes nothing: it doesn't close, even for a moment.
        var closes = 0;
        Popover(n.Parent).Close += (_, _) => closes++;
        scene.Click(n.Parent.OptionAt(4)!);
        PopoverTests.Settle(scene);
        Assert.Equal(2, chosen.Count);
        Assert.True(Open(n.Parent));
        Assert.Equal(0, closes);
    }

    // A finger alone (a mouse's leaving the menus is the nested hover's, a finger's isn't): its taps choose the option and
    // keep the child open, as a controlled popover's anchor click changes nothing.
    [AvaloniaFact]
    public void A_tap_on_an_option_with_a_child_chooses_it_and_keeps_the_child_open()
    {
        var n = Make();
        var chosen = new List<object>();
        n.Parent.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = Show(n);
        MenuTests.Tap(scene, scene.CentreOf(n.Parent.OptionAt(4)!));
        PopoverTests.Settle(scene);
        Assert.Equal(OpenRecent, ((MenuItemData)Assert.Single(chosen)).Label);
        Assert.True(Open(n.Parent));
        var closes = 0;
        Popover(n.Parent).Close += (_, _) => closes++;
        MenuTests.Tap(scene, scene.CentreOf(n.Parent.OptionAt(4)!));
        PopoverTests.Settle(scene);
        Assert.Equal(2, chosen.Count);
        Assert.True(Open(n.Parent));
        Assert.Equal(0, closes);
        // A tap on the child chooses there; the child stays open.
        var inChild = new List<object>();
        n.Child.ItemSelect += (_, e) => inChild.Add(e.Item);
        MenuTests.Tap(scene, scene.CentreOf(n.Child.OptionAt(2)!));
        Assert.Equal("~/workspace/styletron", ((MenuItemData)Assert.Single(inChild)).Label);
        Assert.True(Open(n.Parent));
    }

    [AvaloniaFact]
    public void The_ChildMenuPopover_override_reaches_the_popover()
    {
        var props = new ControlTheme(typeof(Popover));
        props.Setters.Add(new Setter(Metered.Controls.Popover.PopoverMarginProperty, 24.0));
        var n = Make([new Override { Part = MenuOptionBase.ChildMenuPopoverPart, Props = props }]);
        using var scene = Show(n);
        n.Parent.HighlightedIndex = 4;
        PopoverTests.Settle(scene);
        Assert.Equal(24, Popover(n.Parent).PopoverMargin);
        Assert.True(Open(n.Parent));
        Assert.Same(Popover(n.Parent), n.Parent.OptionAt(4)!.Part(MenuOptionBase.ChildMenuPopoverPart));
    }
}
