using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Controls.Primitives;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The tabs' pointer, touch, focus, motion, scrolling and API, each expectation Base Web's with its file named: a click or a
/// tap chooses a tab, a disabled one isn't (tabs-motion.e2e.ts); a finger's drag sideways scrolls tabs that overflow and
/// chooses none, one up or down is the page's; the highlight placed on mount, moved to a new tab by its transition
/// (tabs.js's $animate, styled-components.js's timing400) and to a resized one (the ResizeObserver); the active tab centred
/// on mount and brought into view on a change; the tab stops (e2e: *tab*); renderAll; the reducer; overrides; UI Automation.
/// </summary>
public class TabsTests
{
    private static Tabs Three(string active = "0") => new()
    {
        ActiveKey = active,
        Items = { new Tab { Title = "Robot", Content = "One" }, new Tab { Title = "Monster", Content = "Two" }, new Tab { Title = "Watermelon", Content = "Three" } },
    };

    private static List<TabButton> Buttons(Tabs tabs) => tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();

    private static Tabs Many(string active)
    {
        // tabs-motion-scroll.scenario.tsx: twenty tabs, One to Twenty.
        var words = new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Eleven", "Twelve", "Thirteen",
            "Fourteen", "Fifteen", "Sixteen", "Seventeen", "Eighteen", "Nineteen", "Twenty" };
        var tabs = new Tabs { ActiveKey = active };
        foreach (var w in words) tabs.Items.Add(new Tab { Title = w, Content = w });
        return tabs;
    }

    /// <summary>The tabs in a page that scrolls: Avalonia's own scroller, its template Simple's (a phone's page).</summary>
    private static (ScrollViewer Page, Scene Scene) InAPage(Tabs tabs)
    {
        var page = new ScrollViewer { Content = new StackPanel { Children = { new Border { Height = 300 }, tabs, new Border { Height = 1500 } } } };
        return (page, new Scene(page, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]));
    }

    internal static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    private static void Drag(Scene scene, Point at, Vector by, int steps = 15)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        for (var i = 1; i <= steps; i++)
        {
            scene.Window.TouchMove(touch, at + by * i / steps, RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, at + by, RawInputModifiers.None);
        scene.Pump();
    }

    // e2e: *click* selects tab; onChange({activeKey}).
    [AvaloniaFact]
    public void A_click_chooses_a_tab_and_says_so()
    {
        var tabs = Three();
        var changes = new List<string>();
        tabs.Change += (_, e) => changes.Add(e.ActiveKey);
        using var scene = new Scene(tabs);
        scene.Click(Buttons(tabs)[1]);
        Assert.Equal("1", tabs.ActiveKey);
        Assert.Equal(["1"], changes);
        Assert.False(((ContentPresenter)tabs.Elements(Tabs.TabPanelPart)[0]).IsVisible);
        Assert.Equal("Two", ((ContentPresenter)tabs.Elements(Tabs.TabPanelPart)[1]).Content);
        // A pointer's focus isn't a keyboard's: no ring.
        Assert.DoesNotContain(":focus-visible", Buttons(tabs)[1].Classes);
    }

    // e2e: *click* does not select disabled tab (tabs-motion-disabled.scenario.tsx).
    [AvaloniaFact]
    public void A_disabled_tab_isnt_chosen()
    {
        var tabs = Three();
        tabs.Items[1].Disabled = true;
        using var scene = new Scene(tabs);
        scene.Click(Buttons(tabs)[1]);
        Tap(scene, scene.CentreOf(Buttons(tabs)[1]));
        Assert.Equal("0", tabs.ActiveKey);
    }

    [AvaloniaFact]
    public void A_tap_chooses_a_tab()
    {
        var tabs = Three();
        using var scene = new Scene(tabs);
        Tap(scene, scene.CentreOf(Buttons(tabs)[2]));
        Assert.Equal("2", tabs.ActiveKey);
    }

    // A phone's browser scrolls the overflowing list (overflow-x: scroll) by a sideways drag, and gives no click for it.
    [AvaloniaFact]
    public void A_fingers_drag_sideways_scrolls_tabs_that_overflow_and_chooses_none()
    {
        var tabs = Many("0");
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        Assert.Equal(0, list.Offset.X);
        Drag(scene, scene.CentreOf(Buttons(tabs)[2]), new Vector(-150, 0));
        Assert.True(list.Offset.X > 50, $"the list moved {list.Offset.X}");
        Assert.Equal("0", tabs.ActiveKey);
    }

    // A drag mostly up or down is the page's: it scrolls, and chooses no tab.
    [AvaloniaFact]
    public void A_fingers_drag_up_is_the_pages()
    {
        var tabs = Many("0");
        var (page, scene) = InAPage(tabs);
        using var _ = scene;
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        Drag(scene, scene.CentreOf(Buttons(tabs)[1]), new Vector(-20, -150));
        Assert.True(page.Offset.Y > 50, $"the page moved {page.Offset.Y}");
        Assert.Equal(0, list.Offset.X);
        Assert.Equal("0", tabs.ActiveKey);
    }

    // Tabs that fit don't scroll: a sideways drag over them is no tap either.
    [AvaloniaFact]
    public void A_drag_over_tabs_that_fit_chooses_none()
    {
        var tabs = Three();
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var b = Buttons(tabs)[1];
        Drag(scene, scene.CentreOf(b) - new Vector(20, 0), new Vector(40, 0), steps: 4);
        Assert.Equal("0", tabs.ActiveKey);
    }

    // The wheel's turn over the tabs goes on to the page: a browser scrolls an overflow-x box by the wheel only sideways.
    [AvaloniaFact]
    public void The_wheel_over_the_tabs_scrolls_the_page()
    {
        var tabs = Many("0");
        var (page, scene) = InAPage(tabs);
        using var _ = scene;
        scene.Window.MouseWheel(scene.CentreOf(Buttons(tabs)[1]), new Vector(0, -1));
        scene.Pump();
        Assert.True(page.Offset.Y > 0);
        Assert.Equal(0, ((TabList)tabs.Part(Tabs.TabListPart)!).Offset.X);
    }

    // tabs.js: $animate only once the key has changed after mount; the highlight then moves over timing400.
    [AvaloniaFact]
    public void The_highlight_is_placed_on_mount_and_moves_to_a_new_tab()
    {
        var tabs = Three();
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var highlight = (TabHighlight)tabs.Part(Tabs.TabHighlightPart)!;
        var buttons = Buttons(tabs);
        Assert.DoesNotContain(":animate", highlight.Classes);
        Assert.Equal(Math.Round(buttons[0].Bounds.Width), highlight.Length);
        Assert.Equal(0, highlight.Distance);

        scene.Click(buttons[2]);
        Assert.Contains(":animate", highlight.Classes);
        // Its transition starts where it was.
        Assert.True(highlight.Distance < buttons[2].Bounds.X);
        var motion = TabsOracle.Data.Motion.Highlight;
        Assert.Equal("all", motion.Property);
        scene.Advance((int)(motion.Seconds * 1000 / 2));
        Assert.InRange(highlight.Distance, 1, buttons[2].Bounds.X - 1);
        scene.Advance((int)(motion.Seconds * 1000 / 2) + 20);
        Assert.Equal(Math.Round(buttons[2].Bounds.X), highlight.Distance);
        Assert.Equal(Math.Round(buttons[2].Bounds.Width), highlight.Length);
        Assert.Equal(Math.Round(buttons[2].Bounds.X), highlight.TranslatePoint(default, (Visual)buttons[2].Parent!)!.Value.X);
        Assert.Null(TabsOracle.Data.Motion.HighlightStill);
    }

    // The ResizeObserver: a tab whose size changes takes the highlight with it.
    [AvaloniaFact]
    public void The_highlight_follows_the_active_tab_as_it_resizes()
    {
        var tabs = Three("1");
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var highlight = (TabHighlight)tabs.Part(Tabs.TabHighlightPart)!;
        tabs.Items[0].Title = "A much longer robot";
        scene.Advance(500);
        var b = Buttons(tabs)[1];
        Assert.Equal(Math.Round(b.Bounds.X), highlight.Distance);
        Assert.Equal(Math.Round(b.Bounds.Width), highlight.Length);
    }

    // scrollParentToCentreTarget: on mount the active tab (tabs-motion-scroll.scenario.tsx's eighth) at the list's centre.
    [AvaloniaFact]
    public void The_active_tab_is_centred_on_mount()
    {
        var tabs = Many("8");
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        var b = Buttons(tabs)[8];
        Assert.Equal(b.Bounds.X + b.Bounds.Width / 2 - list.Viewport.Width / 2, list.Offset.X, 0);
    }

    // scrollIntoView nearest on a change: the new active tab brought into the list's view.
    [AvaloniaFact]
    public void A_new_active_tab_is_brought_into_view()
    {
        var tabs = Many("0");
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        tabs.ActiveKey = "19";
        scene.Pump();
        var b = Buttons(tabs)[19];
        Assert.Equal(list.Extent.Width - list.Viewport.Width, list.Offset.X, 0);
        Assert.True(b.Bounds.Right - list.Offset.X <= list.Viewport.Width + 0.5);
    }

    // e2e: *tab* moves focus to active tab, then to tab content (tabs-motion-focus.scenario.tsx: the second tab active).
    [AvaloniaFact]
    public void Tab_goes_to_the_active_tab_then_into_its_panel()
    {
        var first = new TextBox();
        var inner = new TextBox();
        var tabs = new Tabs
        {
            ActiveKey = "1",
            Items = { new Tab { Title = "Robot", Content = "nada" }, new Tab { Title = "Monster", Content = inner }, new Tab { Title = "Watermelon", Content = "nada" } },
        };
        using var scene = new Scene(new StackPanel { Children = { first, tabs } });
        first.Focus();
        scene.Key(Key.Tab);
        Assert.Same(Buttons(tabs)[1], scene.Window.FocusManager!.GetFocusedElement());
        Assert.Contains(":focus-visible", Buttons(tabs)[1].Classes);
        scene.Key(Key.Tab);
        Assert.Same(inner, scene.Window.FocusManager!.GetFocusedElement());
    }

    // tabs-motion-vertical-pageScroll.scenario.tsx: a vertical list takes its arrows, so the page doesn't scroll.
    [AvaloniaFact]
    public void A_vertical_lists_arrows_dont_scroll_the_page()
    {
        var tabs = Three();
        tabs.Orientation = Orientation.Vertical;
        var (page, scene) = InAPage(tabs);
        using var _ = scene;
        page.Offset = new Vector(0, 100);
        scene.Pump();
        Buttons(tabs)[0].Focus(NavigationMethod.Tab);
        scene.Key(Key.Down);
        scene.Key(Key.Down);
        scene.Key(Key.Up);
        Assert.Equal("1", tabs.ActiveKey);
        Assert.Equal(100, page.Offset.Y);
    }

    // renderAll.tsx; e2e: does not mount non selected tab content, [renderAll] mounts it.
    [AvaloniaFact]
    public void Only_the_active_panel_holds_its_content_unless_all_are_rendered()
    {
        var tabs = Three();
        using var scene = new Scene(tabs);
        var panels = tabs.Elements(Tabs.TabPanelPart).Cast<ContentPresenter>().ToList();
        Assert.Equal([ "One", null, null ], panels.Select(p => p.Content as string));
        tabs.RenderAll = true;
        Assert.Equal([ "One", "Two", "Three" ], panels.Select(p => p.Content as string));
        Assert.Equal([true, false, false], panels.Select(p => p.IsVisible));
    }

    // The content is the app's: a control taken out of the panel and shown again is the same one.
    [AvaloniaFact]
    public void A_panels_control_is_shown_again_as_it_was()
    {
        var box = new TextBox { Text = "kept" };
        var tabs = new Tabs { Items = { new Tab { Title = "A", Content = box }, new Tab { Title = "B", Content = "b" } } };
        using var scene = new Scene(tabs);
        Assert.Same(tabs, ((Tab)box.Parent!).Parent);
        scene.Click(Buttons(tabs)[1]);
        Assert.Null(box.GetVisualParent());
        scene.Click(Buttons(tabs)[0]);
        Assert.Same(tabs.Elements(Tabs.TabPanelPart)[0], box.GetVisualParent());
        Assert.Equal("kept", box.Text);
    }

    // stateful-tabs.js: the reducer decides the state; onChange is called whatever it decides.
    [AvaloniaFact]
    public void The_reducer_decides_the_state()
    {
        var tabs = Three();
        var seen = new List<TabsAction>();
        tabs.StateReducer = (state, action) => { seen.Add(action); return action.Payload == "2" ? state : new TabsState(action.Payload); };
        var changes = new List<string>();
        tabs.Change += (_, e) => changes.Add(e.ActiveKey);
        using var scene = new Scene(tabs);
        scene.Click(Buttons(tabs)[2]);
        Assert.Equal("0", tabs.ActiveKey);
        scene.Click(Buttons(tabs)[1]);
        Assert.Equal("1", tabs.ActiveKey);
        Assert.Equal([new TabsAction(TabsStateChangeType.Change, "2"), new TabsAction(TabsStateChangeType.Change, "1")], seen);
        Assert.Equal(["2", "1"], changes);
    }

    // Controlled and stateful in one: a binding gets the key a click sets, and the tabs follow the binding.
    [AvaloniaFact]
    public void ActiveKey_is_bound_both_ways()
    {
        var source = new Avalonia.Controls.TextBlock { Text = "1" };
        var tabs = Three();
        tabs.Bind(Tabs.ActiveKeyProperty, new Binding(nameof(TextBlock.Text)) { Source = source, Mode = BindingMode.TwoWay });
        using var scene = new Scene(tabs);
        Assert.True(Buttons(tabs)[1].IsActive);
        scene.Click(Buttons(tabs)[2]);
        Assert.Equal("2", source.Text);
        source.Text = "0";
        Assert.True(Buttons(tabs)[0].IsActive);
    }

    // tabs-motion-overrides.scenario.tsx: the tabs' overrides and a tab's own.
    [AvaloniaFact]
    public void Overrides_reach_the_tabs_parts_and_a_tabs_own()
    {
        Avalonia.Styling.ControlTheme Theme(Type t, AvaloniaProperty p, object v) => new ControlThemeBuilder(t).Set(p, v).Theme;
        var tabs = Three();
        tabs.Overrides =
        [
            new Override { Part = Tabs.TabBorderPart, Style = Theme(typeof(Border), Border.BackgroundProperty, Brushes.Transparent) },
            new Override { Part = Tabs.TabHighlightPart, Style = Theme(typeof(TabHighlight), Layoutable.HeightProperty, 2.0) },
        ];
        tabs.Items[1].Overrides =
        [
            new Override { Part = Tabs.TabPart, Style = Theme(typeof(TabButton), TemplatedControl.PaddingProperty, new Thickness(4)) },
            new Override { Part = Tabs.TabPanelPart, Style = Theme(typeof(ContentPresenter), ContentPresenter.PaddingProperty, new Thickness(0)) },
        ];
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        Assert.Equal(Brushes.Transparent, ((Border)tabs.Part(Tabs.TabBorderPart)!).Background);
        Assert.Equal(2, ((TabHighlight)tabs.Part(Tabs.TabHighlightPart)!).Bounds.Height);
        Assert.Equal(new Thickness(4), Buttons(tabs)[1].Padding);
        Assert.Equal(new Thickness(16), Buttons(tabs)[0].Padding);
        Assert.Equal(new Thickness(0), ((ContentPresenter)tabs.Elements(Tabs.TabPanelPart)[1]).Padding);
        Assert.Equal(new Thickness(16), ((ContentPresenter)tabs.Elements(Tabs.TabPanelPart)[0]).Padding);
    }

    // UI Automation: selecting a tab item is its click; a disabled one isn't chosen.
    [AvaloniaFact]
    public void Selecting_a_tab_item_chooses_it()
    {
        var tabs = Three();
        tabs.Items[2].Disabled = true;
        using var scene = new Scene(tabs);
        ((ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(Buttons(tabs)[1])).Select();
        Assert.Equal("1", tabs.ActiveKey);
        ((ISelectionItemProvider)ControlAutomationPeer.CreatePeerForElement(Buttons(tabs)[2])).Select();
        Assert.Equal("1", tabs.ActiveKey);
    }

    // Tabs added, removed and renamed after it is shown: built again, the state kept.
    [AvaloniaFact]
    public void Tabs_that_change_are_built_again()
    {
        var tabs = Three("2");
        using var scene = new Scene(tabs);
        tabs.Items.RemoveAt(0);
        // Keys are indices: the active key "2" names no tab now, as Base Web's.
        Assert.Equal(2, Buttons(tabs).Count);
        Assert.DoesNotContain(Buttons(tabs), b => b.IsActive);
        tabs.Items.Add(new Tab { Title = "Dragon", Content = "Four" });
        Assert.True(Buttons(tabs)[2].IsActive);
        Assert.Equal("Dragon", tabs.TextOf(Buttons(tabs)[2]));
        tabs.Items[2].Title = "Lizard";
        Assert.Equal("Lizard", tabs.TextOf(Buttons(tabs)[2]));
    }

    // A mouse over a disabled tab is its :hover too (styled-components.js's :disabled:hover, background none).
    [AvaloniaFact]
    public void A_mouse_over_a_disabled_tab_hovers_it_and_a_finger_never_does()
    {
        var tabs = Three();
        tabs.Items[1].Disabled = true;
        using var scene = new Scene(tabs);
        scene.Hover(Buttons(tabs)[1]);
        Assert.Contains(":is-hovered", Buttons(tabs)[1].Classes);
        scene.Hover(Buttons(tabs)[2]);
        Assert.DoesNotContain(":is-hovered", Buttons(tabs)[1].Classes);
        Assert.Contains(":is-hovered", Buttons(tabs)[2].Classes);
        scene.Window.MouseMove(new Point(5, 700));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", Buttons(tabs)[2].Classes);
        var touch = scene.Window.TouchBegin(scene.CentreOf(Buttons(tabs)[2]), RawInputModifiers.None);
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", Buttons(tabs)[2].Classes);
        scene.Window.TouchEnd(touch, scene.CentreOf(Buttons(tabs)[2]), RawInputModifiers.None);
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", Buttons(tabs)[2].Classes);
    }
}
