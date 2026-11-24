using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The cut, copy and paste menu on a coarse pointer (a phone, #110, option 1 of the 2026-11-21 late decisions page): the
/// same menu without shortcuts, placed above the selection so it doesn't cover it, as a phone's browser places Android's
/// floating text toolbar. Base Web leaves the menu to the browser, so the values are Android's
/// (<c>aosp-mirror/platform_frameworks_base</c> at <c>1cdfff555f</c>): <c>floating_toolbar_vertical_margin</c> 8 dp and
/// <c>floating_toolbar_horizontal_margin</c> 16 dp (<c>core/res/res/values/dimens.xml</c>), the place
/// <c>LocalFloatingToolbarPopup.refreshCoordinatesAndOverflowDirection</c>'s, and the content rect Chrome's
/// (<c>SelectionPopupControllerImpl</c>: the selection, its bottom extended by the handles' height).
/// </summary>
public class InputTouchMenuTests
{
    /// <summary>Android's floating_toolbar_vertical_margin.</summary>
    private const double Above = 8;

    /// <summary>Android's floating_toolbar_horizontal_margin.</summary>
    private const double Side = 16;

    /// <summary>The Pixel 8's bars, rounded to whole pixels (its ready line: <c>safe=0,50.29,0,24</c>).</summary>
    private static readonly Thickness Bars = new(0, 50, 0, 24);

    private sealed class Insets(Thickness padding) : SafeArea.ISource
    {
        public Thickness Padding { get; } = padding;

        public event EventHandler? Changed { add { } remove { } }
    }

    /// <summary>A soft keyboard a test opens and closes.</summary>
    private sealed class Keyboard : TouchMenu.IKeyboard
    {
        public Rect? Occluded { get; private set; }

        public event EventHandler? Changed;

        public void Set(Rect? occluded)
        {
            Occluded = occluded;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    // ---- The place: refreshCoordinatesAndOverflowDirection with no overflow, worked by hand from its source ----

    // A menu 100 × 80 has a popup window 132 × 96 (16 either side, 8 above and below).
    [Theory]
    // Room above the content: the window's bottom at its top, so the menu is 8 above it; centred on it.
    [InlineData(100, 300, 50, 40, 0, 0, 390, 844, 75, 212)]
    // No room above (50 < 96): below, the menu 8 below the content's bottom.
    [InlineData(100, 50, 50, 40, 0, 0, 390, 844, 75, 98)]
    // Room below for the menu alone (90 < 96, 90 >= 80): no margin, the menu at the content's bottom.
    [InlineData(100, 50, 50, 60, 0, 0, 390, 200, 75, 110)]
    // No room either side (40 < 80): as high as it goes, the window at the viewport's top.
    [InlineData(100, 50, 50, 60, 0, 0, 390, 150, 75, 8)]
    // The viewport's own top (a status bar): 76 above is too little, so below.
    [InlineData(100, 100, 50, 40, 0, 24, 390, 820, 75, 148)]
    // Kept inside: 16 from the left, and from the right.
    [InlineData(0, 300, 20, 40, 0, 0, 390, 844, 16, 212)]
    [InlineData(370, 300, 20, 40, 0, 0, 390, 844, 274, 212)]
    public void The_menu_goes_where_Androids_toolbar_goes(double x, double y, double width, double height,
        double viewX, double viewY, double viewWidth, double viewHeight, double menuX, double menuY)
    {
        var at = TouchMenu.Place(new Size(100, 80), new Rect(x, y, width, height), new Rect(viewX, viewY, viewWidth, viewHeight));

        Assert.Equal(new Point(menuX, menuY), at);
    }

    // Wider than the viewport: as far left as it goes, its window at the viewport's left.
    [Fact]
    public void A_menu_wider_than_the_viewport_goes_to_its_left()
    {
        var at = TouchMenu.Place(new Size(400, 80), new Rect(100, 300, 50, 40), new Rect(10, 0, 390, 844));

        Assert.Equal(new Point(26, 212), at);
    }

    // ---- Headless, on a coarse pointer ----

    private static Scene Show(Control content, double top = 100) =>
        new(new StackPanel { Margin = new Thickness(20, top), Spacing = 20, Children = { content } },
            themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);

    private static TextPresenter Presenter(BaseInput input) =>
        input.GetVisualDescendants().OfType<TextPresenter>().Single();

    /// <summary>A finger held past the platform's hold wait on a character, then lifted: Avalonia's hold.</summary>
    private static void Hold(Scene scene, BaseInput input, int index)
    {
        var presenter = Presenter(input);
        var box = presenter.TextLayout.HitTestTextPosition(index);
        var at = presenter.TranslatePoint(new Point(box.X + 2, box.Center.Y), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Advance((int)Application.Current!.PlatformSettings!.HoldWaitDuration.TotalMilliseconds + 1);
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    /// <summary>The selection's box on the window: its lines' union.</summary>
    private static Rect Selection(Scene scene, BaseInput input)
    {
        var presenter = Presenter(input);
        var start = Math.Min(input.SelectionStart, input.SelectionEnd);
        var lines = presenter.TextLayout.HitTestTextRange(start, Math.Abs(input.SelectionEnd - input.SelectionStart)).ToList();
        Assert.NotEmpty(lines);
        var box = lines.Aggregate((a, b) => a.Union(b));
        return new Rect(presenter.TranslatePoint(box.TopLeft, scene.Window)!.Value, box.Size);
    }

    /// <summary>The open menu's box on the window.</summary>
    private static Rect Menu(Scene scene)
    {
        var presenter = scene.Window.GetVisualDescendants().OfType<MenuFlyoutPresenter>().Single(p => p.IsEffectivelyVisible);
        return new Rect(presenter.TranslatePoint(default, scene.Window)!.Value, presenter.Bounds.Size);
    }

    /// <summary>The shown touch handles' boxes on the window.</summary>
    private static List<Rect> Handles(Scene scene) =>
        scene.Window.GetVisualDescendants().OfType<TextSelectorLayer>().Single().GetVisualDescendants()
            .OfType<TextSelectionHandle>().Where(h => h.IsEffectivelyVisible)
            .Select(h => new Rect(h.TranslatePoint(default, scene.Window)!.Value, h.Bounds.Size)).ToList();

    private static MenuItem[] Items(BaseInput input) =>
        Assert.IsType<MenuFlyout>(input.ContextFlyout).Items.OfType<MenuItem>().ToArray();

    /// <summary>The text drawn in the open menu.</summary>
    private static string[] MenuText(Scene scene) =>
        scene.Window.GetVisualDescendants().OfType<MenuFlyoutPresenter>().Single(p => p.IsEffectivelyVisible)
            .GetVisualDescendants().OfType<TextBlock>().Where(t => t.IsEffectivelyVisible && !string.IsNullOrEmpty(t.Text))
            .Select(t => t.Text!).ToArray();

    [AvaloniaFact]
    public void A_coarse_pointer_is_a_phones_and_sets_pointer_coarse()
    {
        Assert.Equal(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS(), new Input().CoarsePointer);
        var input = new Input { CoarsePointer = true };
        using var scene = Show(input);
        Assert.Contains(":pointer-coarse", input.Classes);
        Assert.Contains(":pointer-coarse", input.Part(BaseInput.InputPart)!.Classes);

        input.CoarsePointer = false;

        Assert.DoesNotContain(":pointer-coarse", input.Classes);
    }

    [AvaloniaFact]
    public void On_a_phone_its_menu_has_no_shortcuts()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);

        Assert.Same(input.FindResource("Input.ContextFlyout"), input.ContextFlyout);
        Hold(scene, input, 7);
        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.All(Items(input), item => Assert.Null(item.InputGesture));
        Assert.Equal(["Cut", "Copy", "Paste"], MenuText(scene));

        // The theme's one menu has them back once it closes, for a desktop's input.
        input.ContextFlyout.Hide();
        scene.Pump();
        Assert.Equal([TextBox.CutGesture, TextBox.CopyGesture, TextBox.PasteGesture], Items(input).Select(i => i.InputGesture));
    }

    // A fine pointer keeps the desktop's menu: the shortcuts, and Avalonia's place for a right click (at the pointer).
    [AvaloniaFact]
    public void On_a_desktop_its_menu_keeps_the_shortcuts_and_its_place()
    {
        var input = new Input { Text = "hello world", CoarsePointer = false };
        using var scene = Show(input);

        Assert.Same(input.FindResource("Input.ContextFlyout"), input.ContextFlyout);
        Assert.Equal([TextBox.CutGesture, TextBox.CopyGesture, TextBox.PasteGesture], Items(input).Select(i => i.InputGesture));
        var at = scene.CentreOf(Presenter(input));
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Window.MouseUp(at, MouseButton.Right);
        scene.Pump();
        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.Contains(TextBox.CutGesture!.ToString(), MenuText(scene));
        Assert.Equal(at, Menu(scene).TopLeft);
        input.ContextFlyout.Hide();
    }

    // The Pixel's (#110): a long press on a word opened the menu over it. Here it is 8 above the word, centred on it.
    [AvaloniaFact]
    public void A_long_press_opens_its_menu_above_the_word_clear_of_it()
    {
        var input = new Input { StartEnhancer = "@", Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);

        Hold(scene, input, 7);

        Assert.Equal((6, 11), (input.SelectionStart, input.SelectionEnd));
        Assert.True(input.ContextFlyout!.IsOpen);
        var word = Selection(scene, input);
        var menu = Menu(scene);
        Assert.Equal(word.Top - Above, menu.Bottom, tolerance: 0.5);
        Assert.Equal(word.Center.X, menu.Center.X, tolerance: 1);
        input.ContextFlyout.Hide();
    }

    // Near the window's top, with no room above: below, 8 below the handles, so it covers neither.
    [AvaloniaFact]
    public void Near_the_top_its_menu_opens_below_the_handles()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input, top: 10);

        Hold(scene, input, 7);

        var handles = Handles(scene);
        Assert.Equal(2, handles.Count);
        var menu = Menu(scene);
        Assert.Equal(handles.Max(h => h.Bottom) + Above, menu.Top, tolerance: 0.5);
        Assert.True(menu.Top > Selection(scene, input).Bottom);
        input.ContextFlyout!.Hide();
    }

    // A word near the window's side: the menu is kept 16 from it.
    [AvaloniaFact]
    public void Near_the_side_its_menu_is_kept_16_from_it()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);

        Hold(scene, input, 1);

        Assert.Equal((0, 5), (input.SelectionStart, input.SelectionEnd));
        var menu = Menu(scene);
        Assert.True(Selection(scene, input).Center.X - menu.Width / 2 < Side);
        Assert.Equal(Side, menu.Left, tolerance: 0.5);
        input.ContextFlyout!.Hide();
    }

    // The system's bars are no room (Android's viewport is the visible display frame): under the Pixel's status bar,
    // the menu goes below the handles where it would have fitted above in the whole window.
    [AvaloniaFact]
    public void The_status_bar_is_no_room_above()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input, top: 100);
        SafeArea.Fake(scene.Window, new Insets(Bars));
        try
        {
            Hold(scene, input, 7);

            var word = Selection(scene, input);
            var menu = Menu(scene);
            // Room in the window, not below the bar.
            Assert.True(word.Top - menu.Height - 2 * Above >= 0);
            Assert.True(word.Top - menu.Height - 2 * Above < Bars.Top);
            Assert.Equal(Handles(scene).Max(h => h.Bottom) + Above, menu.Top, tolerance: 0.5);
            input.ContextFlyout!.Hide();
        }
        finally
        {
            SafeArea.Fake(scene.Window, null);
        }
    }

    // Nor is the soft keyboard: with no room above, and below only under the keyboard, it goes as high as it can.
    [AvaloniaFact]
    public void The_keyboard_is_no_room_below()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input, top: 10);
        var keyboard = new Keyboard();
        keyboard.Set(new Rect(0, 150, Scene.Width, Scene.Height - 150));
        TouchMenu.FakeKeyboard(scene.Window, keyboard);
        try
        {
            Hold(scene, input, 7);

            var menu = Menu(scene);
            Assert.True(150 - Handles(scene).Max(h => h.Bottom) < menu.Height);
            Assert.Equal(Above, menu.Top, tolerance: 0.5);
            input.ContextFlyout!.Hide();
        }
        finally
        {
            TouchMenu.FakeKeyboard(scene.Window, null);
        }
    }

    // Avalonia's touch path sets its own place on the menu while it asks for it, and gives the menu's own back: so does
    // Metered's, so the menu's own values are as they were.
    [AvaloniaFact]
    public void The_menus_own_place_is_left_as_it_was()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);
        var menu = Assert.IsAssignableFrom<PopupFlyoutBase>(input.ContextFlyout);

        Hold(scene, input, 7);

        Assert.True(menu.IsOpen);
        // The theme's: Placement="Bottom", and a desktop's show mode (Avalonia.Headless is one).
        Assert.Equal(PlacementMode.Bottom, menu.Placement);
        Assert.Equal(FlyoutShowMode.Standard, menu.ShowMode);
        Assert.Null(menu.CustomPopupPlacementCallback);
        menu.Hide();
        scene.Pump();

        // Asked for with no touch path round it (a mouse's, a key's).
        Presenter(input).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();

        Assert.True(menu.IsOpen);
        Assert.Equal(PlacementMode.Bottom, menu.Placement);
        Assert.Equal(FlyoutShowMode.Standard, menu.ShowMode);
        Assert.Null(menu.CustomPopupPlacementCallback);
        menu.Hide();
    }

    // A long press on an empty input gives the menu (Paste) for its caret: above the caret's line.
    [AvaloniaFact]
    public void On_an_empty_input_its_menu_opens_above_the_caret()
    {
        var input = new Input { Text = "", CoarsePointer = true };
        using var scene = Show(input);
        var presenter = Presenter(input);
        var at = scene.CentreOf(presenter);

        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Advance((int)Application.Current!.PlatformSettings!.HoldWaitDuration.TotalMilliseconds + 1);
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();

        Assert.True(input.ContextFlyout!.IsOpen);
        // Shown without taking the focus: the soft keyboard stays.
        Assert.True(input.IsFocused);
        var line = presenter.TextLayout.HitTestTextPosition(0);
        var lineTop = presenter.TranslatePoint(line.TopLeft, scene.Window)!.Value.Y;
        Assert.Equal(lineTop - Above, Menu(scene).Bottom, tolerance: 0.5);
        input.ContextFlyout.Hide();
    }

    // Shown without taking the focus, as an Avalonia flyout on a phone: the input keeps it, and the keyboard stays. (A
    // desktop's menu, Standard, takes it.)
    [AvaloniaFact]
    public void Its_menu_leaves_the_focus_on_the_input()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);
        scene.Click(input);
        Assert.True(input.IsFocused);

        Presenter(input).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();

        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.True(input.IsFocused);
        input.ContextFlyout.Hide();
    }

    // A long value selected whole, past the box's end: the menu is centred on the part the box shows, as Blink clips an
    // input's selection, not on the whole value's.
    [AvaloniaFact]
    public void A_selection_past_the_box_is_placed_by_the_part_shown()
    {
        var input = new Input { Text = string.Concat(Enumerable.Repeat("a long value ", 8)), CoarsePointer = true };
        using var scene = Show(input);
        scene.Click(input);
        input.SelectAll();
        scene.Pump();
        var scroller = input.GetVisualDescendants().OfType<ScrollViewer>().First();
        var shown = new Rect(scroller.TranslatePoint(default, scene.Window)!.Value, scroller.Bounds.Size);
        var whole = Selection(scene, input);
        Assert.True(whole.Right > shown.Right + 200);

        Presenter(input).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();

        Assert.True(input.ContextFlyout!.IsOpen);
        var menu = Menu(scene);
        Assert.Equal(Math.Max(whole.Left, shown.Left) + (shown.Right - Math.Max(whole.Left, shown.Left)) / 2, menu.Center.X, tolerance: 1);
        Assert.Equal(whole.Top - Above, menu.Bottom, tolerance: 0.5);
        input.ContextFlyout.Hide();
    }

    // Placed again while it is open as the input moves: the page scrolled under it (a keyboard making room scrolls it so).
    [AvaloniaFact]
    public void It_follows_the_word_as_the_page_scrolls()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        var page = new ScrollViewer
        {
            Content = new StackPanel { Margin = new Thickness(20, 400, 20, 0), Height = 2000, Children = { input } },
        };
        using var scene = new Scene(page, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        Hold(scene, input, 7);
        Assert.Equal(Selection(scene, input).Top - Above, Menu(scene).Bottom, tolerance: 0.5);

        page.Offset = new Vector(0, 150);
        scene.Pump();

        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.Equal(Selection(scene, input).Top - Above, Menu(scene).Bottom, tolerance: 0.5);
        input.ContextFlyout.Hide();
    }

    // And as the keyboard opens: below the handles, where there was room, until the keyboard covers it.
    [AvaloniaFact]
    public void It_is_placed_again_as_the_keyboard_opens()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input, top: 10);
        var keyboard = new Keyboard();
        TouchMenu.FakeKeyboard(scene.Window, keyboard);
        try
        {
            Hold(scene, input, 7);
            Assert.Equal(Handles(scene).Max(h => h.Bottom) + Above, Menu(scene).Top, tolerance: 0.5);

            keyboard.Set(new Rect(0, 150, Scene.Width, Scene.Height - 150));
            scene.Pump();

            Assert.Equal(Above, Menu(scene).Top, tolerance: 0.5);
            input.ContextFlyout!.Hide();
        }
        finally
        {
            TouchMenu.FakeKeyboard(scene.Window, null);
        }
    }

    // And as the selection changes while it is open (a hardware keyboard's Shift and arrows, or code).
    [AvaloniaFact]
    public void It_follows_the_selection()
    {
        var input = new Input { StartEnhancer = "@", Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);
        Hold(scene, input, 7);
        Assert.Equal(Selection(scene, input).Center.X, Menu(scene).Center.X, tolerance: 1);

        var nudges = 0;
        ((PopupFlyoutBase)input.ContextFlyout!).Popup.PropertyChanged += (_, e) =>
        {
            if (e.Property == Popup.HorizontalOffsetProperty) nudges++;
        };

        input.SelectionStart = 0;
        input.SelectionEnd = 5;
        scene.Pump();

        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.Equal(Selection(scene, input).Center.X, Menu(scene).Center.X, tolerance: 1);
        // Once for the change, its start, end and caret together (a nudge there and back).
        Assert.Equal(2, nudges);
        input.ContextFlyout.Hide();
    }

    // And as the handles' height changes once they are laid out: below them, clear of them.
    [AvaloniaFact]
    public void It_is_placed_again_as_the_handles_are_laid_out()
    {
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input, top: 10);
        Hold(scene, input, 7);
        var handles = scene.Window.GetVisualDescendants().OfType<TextSelectorLayer>().Single().GetVisualDescendants()
            .OfType<TextSelectionHandle>().Where(h => h.IsEffectivelyVisible).ToList();
        Assert.Equal(2, handles.Count);

        foreach (var handle in handles) handle.Height = handle.Bounds.Height + 20;
        scene.Pump();

        Assert.Equal(Handles(scene).Max(h => h.Bottom) + Above, Menu(scene).Top, tolerance: 0.5);
        input.ContextFlyout!.Hide();
    }

    // A selection moved past the box's edge (a hardware keyboard's Shift and End): the text's scroller brings the caret
    // into view in the layout after, and the menu follows to the part of the selection now shown.
    [AvaloniaFact]
    public void It_follows_the_selection_as_the_text_scrolls_to_it()
    {
        var input = new Input { Text = string.Concat(Enumerable.Repeat("a long value ", 8)), CoarsePointer = true };
        using var scene = Show(input);
        scene.Click(input);
        input.CaretIndex = 0;
        scene.Pump();
        var scroller = input.GetVisualDescendants().OfType<ScrollViewer>().First();
        Presenter(input).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();
        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.Equal(0, scroller.Offset.X);

        // Shift and End from the caret, as a hardware keyboard selects, past the box's edge.
        input.CaretIndex = 45;
        scene.Pump();
        scene.Window.KeyPress(Key.End, RawInputModifiers.Shift, PhysicalKey.End, null);
        scene.Pump();
        scene.Pump();

        Assert.True(input.ContextFlyout.IsOpen);
        Assert.Equal((45, input.Text!.Length), (input.SelectionStart, input.SelectionEnd));
        Assert.True(scroller.Offset.X > 0);
        var shown = new Rect(scroller.TranslatePoint(default, scene.Window)!.Value, scroller.Bounds.Size);
        var word = Selection(scene, input).Intersect(shown);
        Assert.True(word.Width > 0);
        Assert.Equal(word.Center.X, Menu(scene).Center.X, tolerance: 1);
        input.ContextFlyout.Hide();
    }

    // And as the text scrolls in its box, the selection with it.
    [AvaloniaFact]
    public void It_follows_the_text_as_it_scrolls()
    {
        var input = new Input { Text = string.Concat(Enumerable.Repeat("a long value ", 8)), CoarsePointer = true };
        using var scene = Show(input);
        scene.Click(input);
        input.CaretIndex = 0;
        scene.Pump();
        input.SelectionStart = 30;
        input.SelectionEnd = 60;
        var scroller = input.GetVisualDescendants().OfType<ScrollViewer>().First();
        Presenter(input).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();
        Assert.Equal(0, scroller.Offset.X);
        Rect Shown() => Selection(scene, input).Intersect(new Rect(scroller.TranslatePoint(default, scene.Window)!.Value, scroller.Bounds.Size));
        Assert.Equal(Shown().Center.X, Menu(scene).Center.X, tolerance: 1);
        var before = Menu(scene).Center.X;

        scroller.Offset = new Vector(120, 0);
        scene.Pump();
        scene.Pump();

        Assert.True(input.ContextFlyout!.IsOpen);
        Assert.True(Math.Abs(Menu(scene).Center.X - before) > 20);
        Assert.Equal(Shown().Center.X, Menu(scene).Center.X, tolerance: 1);
        input.ContextFlyout.Hide();
    }

    // Nothing to place it by (no text presenter in the input's template) while another's handles are shown: it is placed
    // once and settles, not placed again at every layout pass.
    [AvaloniaFact]
    public void A_menu_with_nothing_to_place_it_by_settles()
    {
        var a = new Input { Text = "hello world", CoarsePointer = true };
        var b = new Input { Text = "no presenter", CoarsePointer = true };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20, 100), Spacing = 20, Children = { a, b } },
            themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        // B's text presenter taken out of the window, so its selection can't be found on it.
        var presenter = Presenter(b);
        ((Panel)presenter.GetVisualParent()!).Children.Remove(presenter);
        scene.Pump();
        Hold(scene, a, 7);
        a.ContextFlyout!.Hide();
        scene.Pump();
        Assert.Equal(2, Handles(scene).Count);
        var menu = Assert.IsAssignableFrom<PopupFlyoutBase>(b.ContextFlyout);
        var nudges = 0;
        menu.Popup.PropertyChanged += (_, e) => { if (e.Property == Popup.HorizontalOffsetProperty) nudges++; };

        b.RaiseEvent(new ContextRequestedEventArgs());
        scene.Advance(1000);

        // Placed again once at most, as it starts to follow the input.
        Assert.True(menu.IsOpen);
        Assert.True(nudges <= 2, $"{nudges} changes of the popup's offset");

        // The other input's handles moving, their height the same, are no reason either.
        var settled = nudges;
        foreach (var handle in scene.Window.GetVisualDescendants().OfType<TextSelectionHandle>().Where(h => h.IsEffectivelyVisible))
            Canvas.SetLeft(handle, Canvas.GetLeft(handle) + 5);
        scene.Advance(100);
        Assert.Equal(settled, nudges);
        menu.Hide();
    }

    // A selection the box doesn't show at all (scrolled out of a long value): the menu goes by the box.
    [AvaloniaFact]
    public void A_selection_the_box_doesnt_show_is_placed_by_the_box()
    {
        var input = new Input { Text = string.Concat(Enumerable.Repeat("a long value ", 8)), CoarsePointer = true };
        using var scene = Show(input);
        scene.Click(input);
        input.CaretIndex = input.Text!.Length;
        scene.Pump();
        input.SelectionStart = 0;
        input.SelectionEnd = 1;
        var scroller = input.GetVisualDescendants().OfType<ScrollViewer>().First();
        var shown = new Rect(scroller.TranslatePoint(default, scene.Window)!.Value, scroller.Bounds.Size);
        Assert.True(Selection(scene, input).Right < shown.Left);

        Presenter(input).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();

        var menu = Menu(scene);
        Assert.Equal(shown.Center.X, menu.Center.X, tolerance: 1);
        Assert.Equal(shown.Top - Above, menu.Bottom, tolerance: 0.5);
        input.ContextFlyout!.Hide();
    }

    // An app that cancels the menu's opening: it stays shut, and isn't opened after all in Avalonia's place.
    [AvaloniaFact]
    public void A_cancelled_opening_stays_shut()
    {
        var mine = new MenuFlyout { Items = { new MenuItem { Header = "Mine" } } };
        var asked = 0;
        mine.Opening += (_, e) =>
        {
            asked++;
            if (asked == 1 && e is System.ComponentModel.CancelEventArgs cancel) cancel.Cancel = true;
        };
        var input = new Input { Text = "hello world", CoarsePointer = true, ContextFlyout = mine };
        using var scene = Show(input);
        var request = new ContextRequestedEventArgs();

        Presenter(input).RaiseEvent(request);
        scene.Pump();

        Assert.Equal(1, asked);
        Assert.False(mine.IsOpen);
        Assert.True(request.Handled);
    }

    // An app's placement rect on its menu's popup doesn't move the menu off its place.
    [AvaloniaFact]
    public void A_placement_rect_on_its_popup_is_no_matter()
    {
        var mine = new MenuFlyout { Items = { new MenuItem { Header = "Mine" } } };
        mine.Popup.PlacementRect = new Rect(0, 30, 100, 10);
        var input = new Input { Text = "hello world", CoarsePointer = true, ContextFlyout = mine };
        using var scene = Show(input);

        Hold(scene, input, 7);

        Assert.True(mine.IsOpen);
        Assert.Equal(Selection(scene, input).Top - Above, Menu(scene).Bottom, tolerance: 0.5);
        mine.Hide();
    }

    // An app's own style for its inputs' menu is kept on a phone, as on a desktop: only the theme's own menu loses its
    // shortcuts there.
    [AvaloniaFact]
    public void An_apps_styled_menu_is_kept_on_a_phone()
    {
        var mine = new MenuFlyout { Items = { new MenuItem { Header = "Mine", InputGesture = TextBox.CopyGesture } } };
        var input = new Input { Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);
        scene.Window.Styles.Add(new Style(x => x.OfType<Input>())
        {
            Setters = { new Setter(Control.ContextFlyoutProperty, mine) },
        });
        scene.Pump();

        Assert.Same(mine, input.ContextFlyout);
        Hold(scene, input, 7);
        Assert.True(mine.IsOpen);
        Assert.Equal(Selection(scene, input).Top - Above, Menu(scene).Bottom, tolerance: 0.5);
        // Its own items as it made them.
        Assert.Same(TextBox.CopyGesture, ((MenuItem)mine.Items[0]!).InputGesture);
        mine.Hide();
    }

    // An app's own menu is placed the same: the place is the selection's, not the theme's menu's.
    [AvaloniaFact]
    public void An_apps_own_menu_is_placed_above_the_selection_too()
    {
        var mine = new MenuFlyout { Items = { new MenuItem { Header = "Mine" } } };
        var input = new Input { Text = "hello world", CoarsePointer = true, ContextFlyout = mine };
        using var scene = Show(input);

        Hold(scene, input, 7);

        Assert.True(mine.IsOpen);
        Assert.Equal(Selection(scene, input).Top - Above, Menu(scene).Bottom, tolerance: 0.5);
        mine.Hide();
    }

    // #109's work round holds on a phone too: the touch menu is shared as the desktop one is, and a long press on another
    // input leaves the first with no selection or handles, the menu the other's.
    [AvaloniaFact]
    public void A_long_press_on_another_input_leaves_the_first_with_no_selection_or_handles()
    {
        var a = new Input { StartEnhancer = "@", Text = "hello world", CoarsePointer = true };
        var b = new Input { Clearable = true, Text = "Clear me!", CoarsePointer = true };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20, 100), Spacing = 20, Children = { a, b } },
            themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        Hold(scene, a, 7);
        Assert.True(a.ContextFlyout!.IsOpen);
        var nothing = new Point(Scene.Width - 10, Scene.Height - 10);
        var touch = scene.Window.TouchBegin(nothing, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, nothing, RawInputModifiers.None);
        scene.Pump();
        Assert.False(a.ContextFlyout.IsOpen);

        Hold(scene, b, 2);

        Assert.True(b.IsFocused);
        Assert.Same(a.ContextFlyout, b.ContextFlyout);
        Assert.Same(b, b.ContextFlyout!.Target);
        Assert.True(b.ContextFlyout.IsOpen);
        Assert.Equal(a.SelectionStart, a.SelectionEnd);
        Assert.Equal(2, Handles(scene).Count);
        Assert.Equal(Selection(scene, b).Top - Above, Menu(scene).Bottom, tolerance: 0.5);
        b.ContextFlyout.Hide();
    }

    // Textarea shares BaseInput: the same menu, above its word.
    [AvaloniaFact]
    public void A_textareas_menu_is_the_same_above_its_word()
    {
        var textarea = new Textarea { Text = "hello world", CoarsePointer = true };
        using var scene = Show(textarea);

        Assert.Same(textarea.FindResource("Input.ContextFlyout"), textarea.ContextFlyout);
        Hold(scene, textarea, 7);

        Assert.Equal((6, 11), (textarea.SelectionStart, textarea.SelectionEnd));
        Assert.True(textarea.ContextFlyout!.IsOpen);
        Assert.Equal(["Cut", "Copy", "Paste"], MenuText(scene));
        var word = Selection(scene, textarea);
        var menu = Menu(scene);
        Assert.Equal(word.Top - Above, menu.Bottom, tolerance: 0.5);
        Assert.Equal(word.Center.X, menu.Center.X, tolerance: 1);
        textarea.ContextFlyout.Hide();
    }

    // Drawn: the word stays drawn selected, in colors.accent, with the menu open above it.
    [AvaloniaFact]
    public void The_word_is_drawn_selected_under_the_open_menu()
    {
        var input = new Input { StartEnhancer = "@", Text = "hello world", CoarsePointer = true };
        using var scene = Show(input);
        Hold(scene, input, 7);
        var word = Selection(scene, input);
        var menu = Menu(scene);
        Assert.False(word.Intersects(menu));

        var frame = scene.Capture();

        // The theme's colors.accent.
        var accent = Assert.IsAssignableFrom<ISolidColorBrush>(input.SelectionBrush).Color;
        Assert.Equal(accent, Frames.PixelAt(frame, (int)word.Left + 1, (int)word.Top + 1));
        input.ContextFlyout!.Hide();
    }

    /// <summary>
    /// When METERED_FRAMES names a folder, the menu after a long press on a word: on a phone above it, and below its
    /// handles near the top; and a desktop's (a fine pointer, Avalonia's place, over the word) beside them.
    /// </summary>
    [AvaloniaTheory]
    [InlineData("above", true, 100.0)]
    [InlineData("below", true, 10.0)]
    [InlineData("desktop", false, 100.0)]
    public void Gallery(string name, bool coarse, double top)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;

        var input = new Input { StartEnhancer = "@", Text = "hello world", CoarsePointer = coarse };
        using var scene = Show(input, top);
        scene.Window.Background = Brushes.White;
        Hold(scene, input, 7);
        var frame = scene.Capture();
        input.ContextFlyout!.Hide();

        Directory.CreateDirectory(folder);
        using var crop = new RenderTargetBitmap(new PixelSize((int)Scene.Width, 260));
        using (var context = crop.CreateDrawingContext())
            context.DrawImage(frame, new Rect(0, 0, Scene.Width, 260), new Rect(0, 0, Scene.Width, 260));
        crop.Save(Path.Combine(folder, $"input-touch-menu-{name}.png"), PngBitmapEncoderOptions.Default);
    }
}
