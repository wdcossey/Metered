using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// Nothing of a popover is drawn over the system's bars (#129, the developer, 2026-11-24): it is placed in the window's
/// safe area, as a phone's browser's viewport ends above its status and navigation bars, and cut off at its edges, while
/// it still follows its anchor. Avalonia.Headless has no bars, so the safe area is a fake the tests set
/// (<c>SafeArea.Fake</c>), as KeyboardInset's tests fake the input pane; the Pixel's are 50.29 at the top and 24 at the
/// foot (tools/android/fixtures). Select's dropdown and Menu's child menu, Popovers both, get the same.
/// </summary>
public class PopoverSafeAreaTests
{
    /// <summary>The Pixel 8's bars, rounded to whole pixels (its ready line: <c>safe=0,50.29,0,24</c>).</summary>
    private static readonly Thickness Bars = new(0, 50, 0, 24);

    /// <summary>A safe area a test sets, and changes as a rotation would.</summary>
    private sealed class Insets(Thickness padding) : SafeArea.ISource
    {
        public Thickness Padding { get; private set; } = padding;

        public event EventHandler? Changed;

        /// <summary>How many popovers listen for a change.</summary>
        public int Listeners => Changed?.GetInvocationList().Length ?? 0;

        public void Set(Thickness padding)
        {
            Padding = padding;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    private static Visual Body(Popover p) => (Visual)p.Part(Popover.BodyPart)!;

    private static OverlayPopupHost Host(Popover p) => Body(p).GetVisualAncestors().OfType<OverlayPopupHost>().First();

    /// <summary>The host's clip in the window's coordinates, or null with none.</summary>
    private static Rect? Cut(Popover p, Scene scene)
    {
        var host = Host(p);
        return host.Clip is { } clip ? clip.Bounds.Translate((Vector)host.TranslatePoint(default, scene.Window)!.Value) : null;
    }

    private static Insets Fake(Scene scene, Thickness padding)
    {
        var insets = new Insets(padding);
        SafeArea.Fake(scene.Window, insets);
        return insets;
    }

    /// <summary>The safe area of a scene's window with these bars.</summary>
    private static Rect Safe(Thickness bars) => new Rect(0, 0, Scene.Width, Scene.Height).Deflate(bars);

    /// <summary>A popover in a page that scrolls, as the sample's: its anchor 1000 down a page 2000 tall.</summary>
    private static (Popover Popover, Border Anchor, ScrollViewer Page) InAPage(PopoverPlacement placement)
    {
        var (popover, anchor, _) = PopoverTests.Fixed(150, 1000);
        popover.Placement = placement;
        var page = new ScrollViewer { Content = new Panel { Height = 2000, Children = { popover } } };
        return (popover, anchor, page);
    }

    /// <summary>The body drawn in its fill just inside its top and its foot: nothing of it cut off.</summary>
    private static void DrawnWhole(Popover popover, Scene scene)
    {
        var fill = Color.Parse(PopoverOracle.Value.Colours.Single(c => c.Theme == "Light").Body.Background);
        var body = PopoverTests.BoxOf(Body(popover), scene);
        var frame = scene.Capture();
        var x = (int)body.Center.X;
        Assert.Equal(fill, Frames.PixelAt(frame, x, (int)body.Top + 3));
        Assert.Equal(fill, Frames.PixelAt(frame, x, (int)body.Bottom - 3));
    }

    private static bool Inside(IInputElement? hit, Visual box) => hit is Visual v && (v == box || v.GetVisualAncestors().Contains(box));

    // ---- The cut ----

    // The issue's case: an anchor scrolled out of view below. The body follows it (the page is popper.js's document) and
    // reaches into the navigation bar's band; what is there is the window's, not the body's, and takes no press.
    [AvaloniaFact]
    public void A_popover_whose_anchor_has_gone_below_is_cut_off_at_the_navigation_bar()
    {
        var (popover, anchor, page) = InAPage(PopoverPlacement.Bottom);
        using var scene = PopoverTests.Show(page);
        scene.Window.Background = Brushes.White;
        Fake(scene, Bars);
        // The anchor's top at 850, just below the window: the body flips above it (it overflows the viewport below), 782 to 842.
        page.Offset = new Vector(0, 150);
        PopoverTests.Settle(scene);
        popover.IsOpen = true;
        PopoverTests.Settle(scene);
        var body = PopoverTests.BoxOf(Body(popover), scene);
        var foot = Scene.Height - Bars.Bottom;
        Assert.True(body.Bottom > foot + 10 && body.Top < foot - 10, $"the body reaches into the bar's band: {body}");
        Assert.Equal(Safe(Bars), Cut(popover, scene));

        var fill = PopoverOracle.Value.Colours.Single(c => c.Theme == "Light").Body.Background;
        var frame = scene.Capture();
        var x = (int)body.Center.X;
        Assert.Equal(Color.Parse(fill), Frames.PixelAt(frame, x, (int)foot - 4));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, x, (int)foot + 4));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, x, (int)body.Bottom - 2));
        // A press in the band isn't the body's.
        Assert.True(Inside(scene.Window.InputHitTest(new Point(x, foot - 4)), Body(popover)));
        Assert.False(Inside(scene.Window.InputHitTest(new Point(x, foot + 4)), Body(popover)));

        // It still follows its anchor: scrolled up into view, it is whole again; down out of the window, cut away.
        page.Offset = new Vector(0, 600);
        PopoverTests.Settle(scene);
        var risen = PopoverTests.BoxOf(Body(popover), scene);
        Assert.True(risen.Bottom < foot, $"back in view: {risen}");
        Assert.Equal(Safe(Bars), Cut(popover, scene));
        Assert.Equal(Color.Parse(fill), Frames.PixelAt(scene.Capture(), (int)risen.Center.X, (int)risen.Bottom - 4));
        page.Offset = new Vector(0, 0);
        PopoverTests.Settle(scene);
        Assert.True(PopoverTests.BoxOf(Body(popover), scene).Top > Scene.Height, "gone with its anchor");
        Assert.Equal(Safe(Bars), Cut(popover, scene));
    }

    // The status bar's band at the top: an anchor scrolled up under it leaves the body's top half in the band, cut off.
    [AvaloniaFact]
    public void A_popover_whose_anchor_has_gone_under_the_status_bar_is_cut_off_there()
    {
        var (popover, anchor, page) = InAPage(PopoverPlacement.Bottom);
        using var scene = PopoverTests.Show(page);
        scene.Window.Background = Brushes.White;
        Fake(scene, Bars);
        // The anchor 1000 to 1040 in the page, at -20 to 20 in the window: the body below it, 28 to 88.
        page.Offset = new Vector(0, 1020);
        PopoverTests.Settle(scene);
        popover.IsOpen = true;
        PopoverTests.Settle(scene);
        var body = PopoverTests.BoxOf(Body(popover), scene);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
        Assert.True(body.Top < Bars.Top - 10 && body.Bottom > Bars.Top + 10, $"the body reaches into the bar's band: {body}");
        var fill = PopoverOracle.Value.Colours.Single(c => c.Theme == "Light").Body.Background;
        var frame = scene.Capture();
        var x = (int)body.Center.X;
        Assert.Equal(Colors.White, Frames.PixelAt(frame, x, (int)Bars.Top - 4));
        Assert.Equal(Color.Parse(fill), Frames.PixelAt(frame, x, (int)Bars.Top + 4));
        Assert.False(Inside(scene.Window.InputHitTest(new Point(x, Bars.Top - 4)), Body(popover)));
    }

    // ---- The viewport ----

    // popper.js's viewport is the safe area, as a browser's ends at the bars: a body that fits above the window's foot
    // but not above the navigation bar flips, as Tether (popper.js, held to it by PopoverTetherTests) places it in that viewport.
    [AvaloniaFact]
    public void Near_the_navigation_bar_it_flips_above_as_popper_js_in_the_safe_area()
    {
        // The anchor 720 to 760: below it, 768 to 828, inside the window less flip's 5 px (839), not the safe area's (815).
        var (popover, anchor, _) = PopoverTests.Fixed(150, 720);
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = PopoverTests.Show(popover);
        scene.Click(anchor);
        PopoverTests.Settle(scene);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
        Assert.Null(Cut(popover, scene));
        popover.IsOpen = false;
        PopoverTests.Settle(scene);

        Fake(scene, Bars);
        scene.Click(anchor);
        PopoverTests.Settle(scene);
        var size = Body(popover).Bounds.Size;
        var expected = Tether.Compute(new Rect(150, 720, 90, 40), size, Popover.MarginFor(PopoverPlacement.Top, 0, 8), null,
            PopoverPlacement.Bottom, Safe(Bars), preventOverflow: true);
        Assert.Equal(PopoverPlacement.Top, expected.Placement);
        Assert.Equal(expected.Placement, popover.ActualPlacement);
        Assert.Equal(new Rect(expected.Offset, size), PopoverTests.BoxOf(Body(popover), scene));
        Assert.Equal(Safe(Bars), Cut(popover, scene));
    }

    // A rotation, or the bars shown or hidden: placed again in the new safe area and cut to it; with none, no cut.
    [AvaloniaFact]
    public void A_safe_area_that_changes_while_it_is_shown_places_and_cuts_it_again()
    {
        var (popover, anchor, _) = PopoverTests.Fixed(150, 720);
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = PopoverTests.Show(popover);
        scene.Window.Background = Brushes.White;
        var insets = Fake(scene, default);
        scene.Click(anchor);
        PopoverTests.Settle(scene);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
        Assert.Null(Cut(popover, scene));

        // Each change is read as it is raised, not at the next layout pass (which the scene's frames would run anyway): a
        // safe area can change with nothing in the window laid out again.
        insets.Set(Bars);
        Assert.Equal(PopoverPlacement.Top, popover.ActualPlacement);
        Assert.Equal(Safe(Bars), Cut(popover, scene));
        PopoverTests.Settle(scene);
        Assert.Equal(PopoverPlacement.Top, popover.ActualPlacement);
        Assert.Equal(Safe(Bars), Cut(popover, scene));
        // Moved by the flip, it is cut where it is now, not where it was: drawn whole, its foot and its top.
        DrawnWhole(popover, scene);

        var side = new Thickness(48, 0, 30, 0);
        insets.Set(side);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
        Assert.Equal(Safe(side), Cut(popover, scene));
        PopoverTests.Settle(scene);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
        Assert.Equal(Safe(side), Cut(popover, scene));
        DrawnWhole(popover, scene);

        insets.Set(default);
        Assert.Null(Cut(popover, scene));
        PopoverTests.Settle(scene);
        Assert.Null(Cut(popover, scene));
    }

    // Each opening is a new popup host: it is cut too, and a closed popover has let its safe area go.
    [AvaloniaFact]
    public void Opened_again_it_is_cut_again()
    {
        var (popover, anchor, _) = PopoverTests.Fixed(150, 720);
        using var scene = PopoverTests.Show(popover);
        var insets = Fake(scene, Bars);
        scene.Click(anchor);
        PopoverTests.Settle(scene);
        Assert.Equal(Safe(Bars), Cut(popover, scene));
        Assert.Equal(1, insets.Listeners);
        scene.Key(Key.Escape);
        PopoverTests.Settle(scene);
        Assert.False(popover.PopupHost!.IsOpen);
        // Closed, it has let the safe area go: a change places nothing and opens nothing.
        Assert.Equal(0, insets.Listeners);
        insets.Set(new Thickness(0, 30, 0, 40));
        PopoverTests.Settle(scene);
        Assert.False(popover.IsOpen);
        Assert.False(popover.PopupHost!.IsOpen);
        insets.Set(Bars);
        scene.Click(anchor);
        PopoverTests.Settle(scene);
        Assert.Equal(1, insets.Listeners);
        Assert.Equal(Safe(Bars), Cut(popover, scene));
    }

    // ---- Select and Menu ----

    // Select's dropdown is a Popover (select-component.js: placement bottom, no margin): near the navigation bar it opens
    // above, and is cut to the safe area.
    [AvaloniaFact]
    public void Selects_dropdown_flips_above_the_navigation_bar_and_is_cut_to_the_safe_area()
    {
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Width = 300, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        var page = new Canvas { Children = { select } };
        using var scene = SelectKit.Show(page);
        // Where its dropdown, below it, ends at the window's foot less flip's 5 px.
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        PopoverTests.Settle(scene);
        var popover = select.DropdownPopover!;
        var below = PopoverTests.BoxOf(Body(popover), scene);
        var top = Scene.Height - 5 - below.Bottom;
        SelectKit.Key(scene, Key.Escape);
        PopoverTests.Settle(scene);
        Assert.False(select.IsOpen);
        Canvas.SetTop(select, top);
        PopoverTests.Settle(scene);

        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        PopoverTests.Settle(scene);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
        Assert.Null(Cut(popover, scene));
        SelectKit.Key(scene, Key.Escape);
        PopoverTests.Settle(scene);

        Fake(scene, Bars);
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        PopoverTests.Settle(scene);
        Assert.True(select.IsOpen);
        Assert.Equal(PopoverPlacement.Top, popover.ActualPlacement);
        Assert.True(PopoverTests.BoxOf(Body(popover), scene).Bottom <= PopoverTests.BoxOf(select, scene).Top + 0.5);
        Assert.Equal(Safe(Bars), Cut(popover, scene));
    }

    // Menu's child menu is a Popover (maybe-child-menu.js: at its option's right, its top, no boundary): its part under the
    // navigation bar is cut off, and takes no press.
    [AvaloniaFact]
    public void Menus_child_menu_is_cut_off_at_the_navigation_bar()
    {
        var child = new Menu { ItemsSource = new[] { "One", "Two", "Three", "Four", "Five", "Six" }.Select(l => (object)new MenuItemData { Label = l }).ToList(), Width = 200 };
        var props = new Avalonia.Styling.ControlTheme(typeof(MenuOptionList));
        props.Setters.Add(new Avalonia.Styling.Setter(MenuOptionBase.GetChildMenuProperty, (Func<object, object?>)(item => item is MenuItemData { Label: "More" } ? child : null)));
        var parent = new Menu
        {
            ItemsSource = new[] { "Open", "Save", "More" }.Select(l => (object)new MenuItemData { Label = l }).ToList(), Width = 150,
            Overrides = [new Override { Part = Menu.OptionPart, Props = props }],
            HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        };
        var root = new NestedMenus { Child = parent, Margin = new Thickness(10, 600, 0, 0) };
        using var scene = PopoverTests.Show(root);
        Fake(scene, Bars);
        var option = parent.OptionAt(2)!;
        scene.Hover(option);
        PopoverTests.Settle(scene);
        var popover = option.ChildMenuPopover!;
        Assert.True(popover.IsOpen && popover.PopupHost!.IsOpen);
        var list = (Visual)child.Part(Menu.ListPart)!;
        var box = PopoverTests.BoxOf(list, scene);
        var foot = Scene.Height - Bars.Bottom;
        Assert.True(box.Bottom > foot + 10, $"the child menu reaches into the bar's band: {box}");
        Assert.Equal(Safe(Bars), Cut(popover, scene));
        Assert.True(Inside(scene.Window.InputHitTest(new Point(box.Center.X, foot - 4)), list));
        Assert.False(Inside(scene.Window.InputHitTest(new Point(box.Center.X, foot + 4)), list));
    }
}
