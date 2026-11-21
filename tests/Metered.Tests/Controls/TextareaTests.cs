using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Themes;

namespace Metered.Tests.Controls;

/// <summary>
/// The textarea's behaviour, as Base Web's textarea.js and the BaseInput it renders give it, and its e2e test
/// (textarea.e2e.ts): which parts render and their labels (TextareaOracle's rendered cases); the text typed, wrapped,
/// on new lines, and scrolled when long, by the caret, the wheel and a finger's drag, which doesn't move the page round it
/// until the textarea is at its end; cleared by the clear icon and by Escape; rows; maxLength; resized by its grip;
/// overrides; transitions after load; UI Automation. All by real input.
/// </summary>
public class TextareaTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    /// <summary>Enough lines for any rows the tests give to scroll.</summary>
    private static readonly string Long = Lines(30);

    private static string Lines(int count) => string.Join("\n", Enumerable.Range(1, count).Select(i => $"Line {i}"));

    private static Scene Show(params Control[] controls)
    {
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
        panel.Children.AddRange(controls);
        return new Scene(panel);
    }

    private static T P<T>(BaseInput input, string part) where T : class => (T)(object)input.Part(part)!;

    private static ScrollViewer Scroller(Textarea textarea) =>
        P<Visual>(textarea, BaseInput.InputPart).GetVisualDescendants().OfType<ScrollViewer>().Single();

    private static TextPresenter Presenter(Textarea textarea) =>
        P<Visual>(textarea, BaseInput.InputPart).GetVisualDescendants().OfType<TextPresenter>().Single();

    private static TextareaResizer Grip(Textarea textarea) =>
        P<Visual>(textarea, BaseInput.InputPart).GetVisualDescendants().OfType<TextareaResizer>().Single();

    private static double End(ScrollViewer scroller) => scroller.Extent.Height - scroller.Viewport.Height;

    private static void Type(Scene scene, string text)
    {
        foreach (var ch in text) scene.Window.KeyTextInput(ch.ToString());
        scene.Pump();
    }

    private static void TouchDrag(Scene scene, Point from, Point to, int steps = 15)
    {
        var touch = scene.Window.TouchBegin(from, RawInputModifiers.None);
        scene.Pump();
        for (var i = 1; i <= steps; i++)
        {
            scene.Window.TouchMove(touch, from + (to - from) * ((double)i / steps), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, to, RawInputModifiers.None);
        scene.Pump();
    }

    private static void MouseDrag(Scene scene, Point from, Point to)
    {
        scene.Window.MouseMove(from);
        scene.Window.MouseDown(from, MouseButton.Left);
        scene.Pump();
        scene.Window.MouseMove(from + (to - from) / 2);
        scene.Pump();
        scene.Window.MouseMove(to);
        scene.Window.MouseUp(to, MouseButton.Left);
        scene.Pump();
    }

    /// <summary>A textarea in a page that scrolls, as the sample's: a ScrollViewer from Avalonia's Simple theme, which pans by touch.</summary>
    private static (Textarea Textarea, ScrollViewer Page, Scene Scene) InAPage(Textarea? given = null)
    {
        var textarea = given ?? new Textarea { Text = Long };
        var page = new ScrollViewer
        {
            Content = new StackPanel { Margin = new Thickness(20, 200, 20, 1600), Children = { textarea } },
        };
        return (textarea, page, new Scene(page, themes: [new SimpleTheme(), new BaseTheme()]));
    }

    [AvaloniaFact]
    public void Defaults_are_Base_Webs()
    {
        var d = TextareaOracle.Value.Defaults;
        var textarea = new Textarea();
        Assert.Equal(d.Rows, textarea.Rows);
        Assert.Equal(Enum.Parse<InputSize>(d.Size, ignoreCase: true), textarea.Size);
        Assert.Null(d.Resize);
        Assert.Null(textarea.Resize);
        Assert.Equal(d.Clearable, textarea.Clearable);
        Assert.Equal(d.ClearOnEscape, textarea.ClearOnEscape);
        Assert.Equal(d.Error, textarea.Error);
        Assert.Equal(d.Positive, textarea.Positive);
        Assert.Equal(d.Placeholder, textarea.PlaceholderText ?? "");
        Assert.Equal(!d.Disabled, textarea.IsEnabled);
        Assert.Equal(d.ReadOnly, textarea.IsReadOnly);
        // A textarea isn't a password.
        Assert.False(textarea.IsMasked);
        using var scene = Show(textarea);
        // A <textarea>: Enter is a new line, and its text wraps.
        Assert.True(textarea.AcceptsReturn);
        Assert.Equal(TextWrapping.Wrap, textarea.TextWrapping);
        Assert.Equal(TextWrapping.Wrap, Presenter(textarea).TextWrapping);
    }

    // Input's cut, copy and paste menu, the one shared flyout (by a dynamic resource: Input.axaml is another dictionary).
    [AvaloniaFact]
    public void Its_menu_is_Inputs()
    {
        var textarea = new Textarea();
        var input = new Input();
        using var scene = Show(textarea, input);
        Assert.NotNull(textarea.ContextFlyout);
        Assert.Same(input.ContextFlyout, textarea.ContextFlyout);
    }

    public static TheoryData<int> PartsCases => new(Enumerable.Range(0, TextareaOracle.Value.Parts.Length));

    [AvaloniaTheory]
    [MemberData(nameof(PartsCases))]
    public void Renders_the_parts_Base_Web_renders_with_its_labels(int index)
    {
        var c = TextareaOracle.Value.Parts[index];
        var textarea = new Textarea { Clearable = c.Clearable, Text = c.Value, IsReadOnly = c.ReadOnly, IsEnabled = !c.Disabled };
        using var scene = Show(textarea);

        var shown = Textarea.PartNames.Where(name => textarea.Part(name) is Visual { IsEffectivelyVisible: true }).ToHashSet();
        Assert.Equal(c.Rendered.OrderBy(x => x), shown.OrderBy(x => x));
        // The type is "textarea": no mask toggle, nothing masked.
        Assert.Equal("textarea", c.InputType);
        Assert.Null(textarea.Part(BaseInput.MaskToggleButtonPart));
        Assert.Equal(default, textarea.PasswordChar);
        Assert.Equal(c.Rows, textarea.Rows);

        var clear = P<InputIcon>(textarea, BaseInput.ClearIconPart);
        if (c.ClearLabel is not null)
        {
            Assert.Equal(c.ClearLabel, AutomationProperties.GetName(clear));
            Assert.Equal(c.ClearTitle, ToolTip.GetTip(clear));
            Assert.Equal("button", c.ClearRole);
            Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(clear).GetAutomationControlType());
            Assert.Equal(0, c.ClearTabIndex);
            Assert.True(clear.Focusable);
            Assert.True(clear.IsTabStop);
            // base-input.js's $alignTop for a textarea: its icon at the top (TextareaLayoutTests places it).
            Assert.True(c.AlignTop);
            Assert.Equal(Avalonia.Layout.VerticalAlignment.Top, clear.VerticalAlignment);
        }
    }

    // textarea.e2e.ts: the preset value is shown, and what is typed after a click is added to it.
    [AvaloniaFact]
    public void A_click_then_typing_adds_to_the_value()
    {
        var textarea = new Textarea { Clearable = true, Text = "initial value" };
        using var scene = Show(textarea);
        Assert.Equal("initial value", Presenter(textarea).Text);
        scene.Click(P<Visual>(textarea, BaseInput.InputPart));
        Assert.True(textarea.IsFocused);
        textarea.CaretIndex = textarea.Text!.Length;
        Type(scene, "1");
        Assert.Equal("initial value1", textarea.Text);
        Assert.All(Textarea.PartNames, name => Assert.Contains(":is-focused", textarea.Part(name)!.Classes));
    }

    [AvaloniaFact]
    public void Enter_starts_a_new_line()
    {
        var textarea = new Textarea { Text = "one" };
        using var scene = Show(textarea);
        scene.Key(Key.Tab);
        textarea.CaretIndex = 3;
        scene.Key(Key.Enter);
        Type(scene, "two");
        Assert.Equal(["one", "two"], textarea.Text!.Split(textarea.NewLine));
        Assert.Equal(2, Presenter(textarea).TextLayout.TextLines.Count);
    }

    [AvaloniaFact]
    public void A_long_line_wraps_inside_the_padding()
    {
        var textarea = new Textarea { Text = string.Join(" ", Enumerable.Repeat("wrapping", 30)) };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var presenter = Presenter(textarea);
        Assert.True(presenter.TextLayout.TextLines.Count > 1);
        Assert.True(presenter.TextLayout.WidthIncludingTrailingWhitespace <= field.Bounds.Width - field.Padding.Left - field.Padding.Right + 0.5);
        // No horizontal scrolling: the text box disables it for wrapped text.
        Assert.Equal(Scroller(textarea).Viewport.Width, Scroller(textarea).Extent.Width);
    }

    // The textarea is its rows' height whatever its text; a long text scrolls in it, the caret kept in view.
    [AvaloniaFact]
    public void A_long_text_scrolls_inside_its_rows()
    {
        var textarea = new Textarea { Text = Long };
        using var scene = Show(textarea);
        var c = TextareaOracle.Value.Layout.First(x => x.Size == "default" && x.Resize is null && x.Rows == 3);
        Assert.Equal(c.Height, textarea.Bounds.Height);
        var scroller = Scroller(textarea);
        Assert.True(End(scroller) > 0);
        Assert.Equal(0, scroller.Offset.Y);

        // The caret at the end: its line scrolled into view, above the bottom padding.
        scene.Key(Key.Tab);
        textarea.CaretIndex = textarea.Text!.Length;
        scene.Pump();
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        Assert.InRange(scroller.Offset.Y, End(scroller) - field.Padding.Bottom - 0.5, End(scroller));
        Assert.Equal(c.Height, textarea.Bounds.Height);

        // The wheel too.
        var at = scene.CentreOf(textarea);
        scene.Window.MouseWheel(at, new Vector(0, 1));
        scene.Pump();
        Assert.True(scroller.Offset.Y < End(scroller));
    }

    // The padding is the scroller's: the text scrolls under it, as a textarea's does, and the first line scrolled to sits
    // the padding below the top.
    [AvaloniaFact]
    public void The_text_scrolls_under_the_padding()
    {
        var textarea = new Textarea { Text = Long };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var scroller = Scroller(textarea);
        Assert.Equal(new Rect(field.Bounds.Size), new Rect(scroller.TranslatePoint(default, field)!.Value, scroller.Bounds.Size));
        var line = field.GetValue(TextBlock.LineHeightProperty);
        Assert.Equal(30 * line + field.Padding.Top + field.Padding.Bottom, scroller.Extent.Height, 1);
        scroller.Offset = new Vector(0, 3);
        scene.Pump();
        Assert.Equal(field.Padding.Top - 3, Presenter(textarea).TranslatePoint(default, field)!.Value.Y, 1);
    }

    [AvaloniaFact]
    public void Rows_set_its_height_and_a_value_not_above_0_is_HTMLs_2()
    {
        var textarea = new Textarea { Text = Long, Rows = 5 };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        double Lines() => (field.Bounds.Height - field.Padding.Top - field.Padding.Bottom) / field.GetValue(TextBlock.LineHeightProperty);
        Assert.Equal(5, Lines());
        textarea.Rows = 1;
        scene.Pump();
        Assert.Equal(1, Lines());
        textarea.Rows = -4;
        scene.Pump();
        Assert.Equal(TextareaField.HtmlDefaultRows, Lines());
        Assert.Equal(2, TextareaField.HtmlDefaultRows);
    }

    // The heart of a phone's textarea: a finger's drag on it scrolls it, and the page round it stays where it is.
    [AvaloniaFact]
    public void A_touch_drag_on_it_scrolls_it_not_the_page()
    {
        var (textarea, page, scene) = InAPage();
        using var _ = scene;
        var scroller = Scroller(textarea);
        Assert.True(page.Extent.Height > page.Viewport.Height);
        Assert.True(End(scroller) > 100);

        var at = scene.CentreOf(textarea);
        TouchDrag(scene, at, new Point(at.X, at.Y - 50));
        Assert.True(scroller.Offset.Y > 20, $"the textarea moved {scroller.Offset.Y}");
        Assert.Equal(0, page.Offset.Y);
        // A drag isn't a tap: the caret didn't move, nothing was selected.
        Assert.Equal(0, textarea.SelectionEnd - textarea.SelectionStart);

        // Back down: it scrolls back, and still not the page.
        var was = scroller.Offset.Y;
        TouchDrag(scene, at, new Point(at.X, at.Y + 30));
        Assert.True(scroller.Offset.Y < was);
        Assert.Equal(0, page.Offset.Y);
    }

    // A browser latches a touch's scroll to the scroller that took it: a drag that carries the textarea to its end goes no
    // further in that drag. The next drag, starting at the end, is the page's.
    [AvaloniaFact]
    public void A_drag_past_its_end_stops_there_and_the_next_moves_the_page()
    {
        var (textarea, page, scene) = InAPage(new Textarea { Text = Lines(12) });
        using var _ = scene;
        var scroller = Scroller(textarea);
        var at = scene.CentreOf(textarea);

        // Twice as far as the textarea can go: the rest would move the page, but doesn't.
        var end = End(scroller);
        Assert.InRange(end, 100, 300);
        TouchDrag(scene, at, new Point(at.X, at.Y - 2 * end), steps: 40);
        Assert.Equal(end, scroller.Offset.Y, 1);
        Assert.Equal(0, page.Offset.Y);

        at = scene.CentreOf(textarea);
        TouchDrag(scene, at, new Point(at.X, at.Y - 150));
        Assert.True(page.Offset.Y > 60, $"the page moved {page.Offset.Y}");
        Assert.Equal(End(scroller), scroller.Offset.Y, 1);
    }

    // At its top, a drag down on it is the page's (scrolled down first so that it can move).
    [AvaloniaFact]
    public void At_its_top_a_drag_down_on_it_moves_the_page()
    {
        var (textarea, page, scene) = InAPage();
        using var _ = scene;
        page.Offset = new Vector(0, 120);
        scene.Pump();
        var scroller = Scroller(textarea);
        Assert.Equal(0, scroller.Offset.Y);

        var at = scene.CentreOf(textarea);
        TouchDrag(scene, at, new Point(at.X, at.Y + 80));
        Assert.True(page.Offset.Y < 80, $"the page is at {page.Offset.Y}");
        Assert.Equal(0, scroller.Offset.Y);
    }

    // A textarea whose text fits can't scroll: a drag on it pans the page.
    [AvaloniaFact]
    public void A_drag_on_a_textarea_that_fits_moves_the_page()
    {
        var (textarea, page, scene) = InAPage(new Textarea { Text = "Short" });
        using var _ = scene;
        Assert.Equal(0, End(Scroller(textarea)));
        var at = scene.CentreOf(textarea);
        TouchDrag(scene, at, new Point(at.X, at.Y - 150));
        Assert.True(page.Offset.Y > 60, $"the page moved {page.Offset.Y}");
    }

    // A drag the page took at the textarea's top stays the page's when it turns back up, as a browser latches it: the
    // textarea doesn't take it over half way.
    [AvaloniaFact]
    public void A_drag_the_page_took_stays_the_pages_when_it_turns()
    {
        var (textarea, page, scene) = InAPage();
        using var _ = scene;
        page.Offset = new Vector(0, 120);
        scene.Pump();
        var scroller = Scroller(textarea);
        var at = scene.CentreOf(textarea);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        foreach (var dy in new[] { 10, 20, 30, 40, 50, 40, 30, 20, 10, 0, -10, -20 })
        {
            scene.Window.TouchMove(touch, new Point(at.X, at.Y + dy), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, new Point(at.X, at.Y - 20), RawInputModifiers.None);
        scene.Pump();
        Assert.Equal(0, scroller.Offset.Y);
        Assert.True(page.Offset.Y > 80, $"the page is at {page.Offset.Y}");
    }

    // A tap is the text's: it focuses the textarea and puts the caret there.
    [AvaloniaFact]
    public void A_tap_focuses_it_and_places_the_caret()
    {
        var (textarea, _, scene) = InAPage(new Textarea { Text = "one two three" });
        using var __ = scene;
        var presenter = Presenter(textarea);
        var at = presenter.TranslatePoint(new Point(1, presenter.Bounds.Height / 2), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
        Assert.True(textarea.IsFocused);
        Assert.Equal(0, textarea.CaretIndex);
    }

    // textarea.e2e.ts: cleared by a click on the clear icon, which gives the textarea its focus.
    [AvaloniaFact]
    public void The_clear_icons_click_clears_it_and_focuses_it()
    {
        var textarea = new Textarea { Clearable = true, Text = "initial value" };
        using var scene = Show(textarea);
        scene.Click(P<Visual>(textarea, BaseInput.InputPart));
        textarea.CaretIndex = textarea.Text!.Length;
        Type(scene, "Something or other");
        var clear = P<Visual>(textarea, BaseInput.ClearIconPart);
        Assert.True(clear.IsEffectivelyVisible);

        scene.Click(clear);

        Assert.Equal("", textarea.Text);
        Assert.True(textarea.IsFocused);
        Assert.False(clear.IsEffectivelyVisible);
    }

    // textarea.e2e.ts: cleared by Escape, which goes no further; not if it isn't clearable, or Escape is turned off.
    [AvaloniaTheory]
    [InlineData(true, true, "")]
    [InlineData(false, true, "Something")]
    [InlineData(true, false, "Something")]
    public void Escape_clears_it_when_clearable(bool clearable, bool clearOnEscape, string left)
    {
        var textarea = new Textarea { Clearable = clearable, ClearOnEscape = clearOnEscape, Text = "Something" };
        var reached = 0;
        var panel = new StackPanel { Margin = new Thickness(20), Children = { textarea } };
        panel.KeyDown += (_, e) => { if (e.Key == Key.Escape) reached++; };
        using var scene = new Scene(panel);
        scene.Key(Key.Tab);
        Assert.True(textarea.IsFocused);

        scene.Key(Key.Escape);

        Assert.Equal(left, textarea.Text);
        Assert.Equal(left.Length == 0 ? 0 : 1, reached);
    }

    [AvaloniaFact]
    public void MaxLength_stops_typing_at_it()
    {
        var textarea = new Textarea { MaxLength = 5 };
        using var scene = Show(textarea);
        scene.Key(Key.Tab);
        Type(scene, "abc");
        scene.Key(Key.Enter);
        Type(scene, "defgh");
        Assert.Equal(5, textarea.Text!.Length);
    }

    [AvaloniaFact]
    public void Resize_sets_its_state_on_every_part_and_unset_none()
    {
        var textarea = new Textarea();
        using var scene = Show(textarea);
        string[] states = [":resize-none", ":resize-both", ":resize-horizontal", ":resize-vertical"];
        Assert.All(states, s => Assert.DoesNotContain(s, textarea.Classes));
        foreach (var (resize, state) in new[] { (TextareaResize.None, ":resize-none"), (TextareaResize.Both, ":resize-both"), (TextareaResize.Horizontal, ":resize-horizontal"), (TextareaResize.Vertical, ":resize-vertical") })
        {
            textarea.Resize = resize;
            scene.Pump();
            Assert.All(Textarea.PartNames.Prepend(null), name =>
            {
                var element = name is null ? (StyledElement)textarea : textarea.Part(name)!;
                Assert.All(states, s => Assert.Equal(s == state, element.Classes.Contains(s)));
            });
        }

        textarea.Resize = null;
        scene.Pump();
        Assert.All(states, s => Assert.DoesNotContain(s, textarea.Part(Textarea.RootPart)!.Classes));
    }

    // CSS's resize: a drag on the grip by a mouse sets the textarea's own width and height, as the way allows; the root
    // fits it (fit-content).
    [AvaloniaTheory]
    [InlineData(TextareaResize.Both, true, true)]
    [InlineData(TextareaResize.Horizontal, true, false)]
    [InlineData(TextareaResize.Vertical, false, true)]
    public void A_drag_on_the_grip_resizes_it(TextareaResize resize, bool wider, bool taller)
    {
        var textarea = new Textarea { Resize = resize, Text = "Resize me" };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var root = P<Border>(textarea, Textarea.RootPart);
        var grip = Grip(textarea);
        Assert.True(grip.IsEffectivelyVisible);
        var start = field.Bounds.Size;
        var at = scene.CentreOf(grip);

        MouseDrag(scene, at, at + new Vector(40, 30));

        Assert.Equal(start.Width + (wider ? 40 : 0), field.Bounds.Width);
        Assert.Equal(start.Height + (taller ? 30 : 0), field.Bounds.Height);
        Assert.Equal(field.Bounds.Width + root.BorderThickness.Left + root.BorderThickness.Right, root.Bounds.Width);
        Assert.Equal(field.Bounds.Height + root.BorderThickness.Top + root.BorderThickness.Bottom, textarea.Bounds.Height);
        // The grip's drag isn't the text's: no caret moved, nothing selected.
        Assert.Equal(0, textarea.CaretIndex);
        Assert.Equal(0, textarea.SelectionEnd - textarea.SelectionStart);
        Assert.False(grip.IsDragging);
    }

    [AvaloniaFact]
    public void The_grip_keeps_within_its_bounds()
    {
        var textarea = new Textarea { Resize = TextareaResize.Both, Text = "Resize me" };
        textarea.Overrides = [new Override { Part = BaseInput.InputPart, Style = Theme<TextareaField>(Avalonia.Layout.Layoutable.MinHeightProperty, 60.0) }];
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var grip = Grip(textarea);
        var at = scene.CentreOf(grip);

        // No smaller than the override's minimum height, or the grip itself; the size follows the pointer, so back 10 px
        // in the same drag is still past it.
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseMove(at - new Vector(1000, 1000));
        scene.Pump();
        Assert.Equal(60, field.Bounds.Height);
        Assert.Equal(grip.Bounds.Width, field.Bounds.Width);
        scene.Window.MouseMove(at - new Vector(1000, 1000) + new Vector(10, 10));
        scene.Window.MouseUp(at - new Vector(1000, 1000) + new Vector(10, 10), MouseButton.Left);
        scene.Pump();
        Assert.Equal(60, field.Bounds.Height);
        Assert.Equal(grip.Bounds.Width, field.Bounds.Width);

        // No wider than the room the textarea has: its own width, inside the root's border; back 40 px in the same drag is
        // still past it.
        at = scene.CentreOf(grip);
        var root = P<Border>(textarea, Textarea.RootPart);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseMove(at + new Vector(1000, 0));
        scene.Pump();
        Assert.Equal(textarea.Bounds.Width, root.Bounds.Width);
        var widest = field.Bounds.Width;
        scene.Window.MouseMove(at + new Vector(1000 - 40, 0));
        scene.Window.MouseUp(at + new Vector(1000 - 40, 0), MouseButton.Left);
        scene.Pump();
        Assert.Equal(widest, field.Bounds.Width);
        at = scene.CentreOf(grip);
        MouseDrag(scene, at, at + new Vector(1000, 0));
        Assert.Equal(textarea.Bounds.Width, root.Bounds.Width);
        Assert.True(grip.IsEffectivelyVisible);
        Assert.Equal(root.Bounds.Width, grip.TranslatePoint(new Point(grip.Bounds.Width, 0), root)!.Value.X + root.BorderThickness.Right, 1);
    }

    // A finger on the grip resizes the textarea, and doesn't pan the page round it.
    [AvaloniaFact]
    public void A_finger_on_the_grip_resizes_it_not_the_page()
    {
        var (textarea, page, scene) = InAPage(new Textarea { Resize = TextareaResize.Vertical, Text = "Resize me" });
        using var _ = scene;
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var start = field.Bounds.Height;
        var at = scene.CentreOf(Grip(textarea));
        TouchDrag(scene, at, at + new Vector(0, 60));
        Assert.Equal(start + 60, field.Bounds.Height);
        Assert.Equal(0, page.Offset.Y);

        // Up, the way the page could scroll: the textarea is shorter, and the page stays.
        at = scene.CentreOf(Grip(textarea));
        TouchDrag(scene, at, at - new Vector(0, 80));
        Assert.Equal(start - 20, field.Bounds.Height);
        Assert.Equal(0, page.Offset.Y);
    }

    // A width dragged to is kept within the width the textarea is later given (a phone turned upright): never past its
    // root, so the grip stays in reach.
    [AvaloniaFact]
    public void A_dragged_width_keeps_within_a_narrower_space()
    {
        var textarea = new Textarea { Resize = TextareaResize.Both, Text = "Resize me" };
        var host = new StackPanel { Width = 350, Children = { textarea } };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { host } });
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var root = P<Border>(textarea, Textarea.RootPart);
        var at = scene.CentreOf(Grip(textarea));
        MouseDrag(scene, at, at + new Vector(1000, 0));
        Assert.Equal(350, root.Bounds.Width);

        host.Width = 250;
        scene.Pump();
        Assert.Equal(250, root.Bounds.Width);
        Assert.Equal(250 - root.BorderThickness.Left - root.BorderThickness.Right, field.Bounds.Width);
        var grip = Grip(textarea);
        Assert.Equal(field.Bounds.Width, grip.TranslatePoint(new Point(grip.Bounds.Width, 0), field)!.Value.X);
    }

    // Right to left, the grip is in the bottom-left corner, and a drag outward, to the left, widens it.
    [AvaloniaFact]
    public void Right_to_left_a_drag_to_the_left_widens_it()
    {
        var textarea = new Textarea { Resize = TextareaResize.Horizontal, Text = "Resize me", FlowDirection = FlowDirection.RightToLeft };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var start = field.Bounds.Width;
        var grip = Grip(textarea);
        Assert.True(scene.CentreOf(grip).X < scene.CentreOf(field).X);
        var at = scene.CentreOf(grip);
        MouseDrag(scene, at, at - new Vector(40, 0));
        Assert.Equal(start + 40, field.Bounds.Width);
    }

    // Laid out once: a stretched textarea, or one as wide as its text box (its root fit-content), with text that wraps,
    // doesn't measure itself again on every pass.
    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(TextareaResize.Both)]
    public void Its_layout_settles(TextareaResize? resize)
    {
        var textarea = new Textarea { Resize = resize, Text = string.Join(" ", Enumerable.Repeat("wrapping", 40)) };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        scene.Window.UpdateLayout();
        Assert.True(field.IsMeasureValid && field.IsArrangeValid);
        var presenter = Presenter(textarea);
        textarea.InvalidateMeasure();
        scene.Window.UpdateLayout();
        scene.Window.UpdateLayout();
        Assert.True(field.IsMeasureValid && field.IsArrangeValid);
        Assert.True(presenter.TextLayout.WidthIncludingTrailingWhitespace <= field.Bounds.Width - field.Padding.Left - field.Padding.Right + 0.5);
        // The scroller's extent is the text as laid out at that width, and its padding: the last line can be reached.
        Assert.Equal(presenter.TextLayout.Height + field.Padding.Top + field.Padding.Bottom, Scroller(textarea).Extent.Height, 1);
    }

    [AvaloniaTheory]
    [InlineData(null)]
    [InlineData(TextareaResize.None)]
    public void No_grip_unless_it_resizes(TextareaResize? resize)
    {
        var textarea = new Textarea { Resize = resize };
        using var scene = Show(textarea);
        Assert.False(Grip(textarea).IsEffectivelyVisible);
    }

    // The width a browser gives a textarea of HTML's 20 columns, where nothing stretches it: Chrome's 20 times the font's
    // OS/2 average character width, rounded to a pixel. Read here from the font file itself (Instrument Sans, the
    // substitute for Uber Move Text: ADR 0003).
    [AvaloniaFact]
    public void Its_own_width_is_20_of_the_fonts_average_characters()
    {
        var textarea = new Textarea { Resize = TextareaResize.Both };
        using var scene = Show(textarea);
        var field = P<TextareaField>(textarea, BaseInput.InputPart);
        var size = field.GetValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
        var expected = AverageCharWidth(size);
        Assert.Equal(expected, field.AverageCharWidth());
        Assert.Equal(20, TextareaField.Cols);
        Assert.Equal(20 * expected + field.Padding.Left + field.Padding.Right, field.Bounds.Width);
    }

    /// <summary>
    /// Chrome's average character width for Instrument Sans Regular (the substitute for Uber Move Text: ADR 0003) at a
    /// size: its OS/2 xAvgCharWidth in pixels, rounded, read from the font file itself.
    /// </summary>
    internal static double AverageCharWidth(double size)
    {
        var bytes = File.ReadAllBytes(Path.Combine(Metered.Tests.Repository.RepositoryFiles.FindRoot(), "fonts", "InstrumentSans", "InstrumentSans-Regular.ttf"));
        var (average, em) = Os2(bytes);
        return Math.Round(average * size / em, MidpointRounding.AwayFromZero);
    }

    /// <summary>xAvgCharWidth (OS/2) and unitsPerEm (head) of a TrueType file.</summary>
    internal static (short Average, ushort Em) Os2(byte[] font)
    {
        int Table(string tag)
        {
            var count = (font[4] << 8) | font[5];
            for (var i = 0; i < count; i++)
            {
                var at = 12 + 16 * i;
                if (System.Text.Encoding.ASCII.GetString(font, at, 4) == tag)
                {
                    return (font[at + 8] << 24) | (font[at + 9] << 16) | (font[at + 10] << 8) | font[at + 11];
                }
            }

            throw new InvalidOperationException(tag);
        }

        var os2 = Table("OS/2");
        var head = Table("head");
        return ((short)((font[os2 + 2] << 8) | font[os2 + 3]), (ushort)((font[head + 18] << 8) | font[head + 19]));
    }

    [AvaloniaFact]
    public void Overrides_merge_over_each_parts_theme()
    {
        var textarea = new Textarea
        {
            Clearable = true, Text = "Value",
            Overrides =
            [
                new Override { Part = Textarea.RootPart, Style = Theme<Border>(Border.BorderBrushProperty, Red) },
                new Override { Part = BaseInput.InputContainerPart, Style = Theme<Border>(Border.BackgroundProperty, Red) },
                new Override { Part = BaseInput.InputPart, Style = Theme<TextareaField>(TextareaField.ResizeProperty, TextareaResize.Vertical) },
                new Override { Part = BaseInput.ClearIconPart, Style = Theme<InputIcon>(InputIcon.ForegroundProperty, Red) },
            ],
        };
        using var scene = Show(textarea);
        Assert.Same(Red, P<Border>(textarea, Textarea.RootPart).BorderBrush);
        Assert.Same(Red, P<Border>(textarea, BaseInput.InputContainerPart).Background);
        Assert.Same(Red, P<InputIcon>(textarea, BaseInput.ClearIconPart).Foreground);
        // The Input part's resize, from an override as from the prop: its grip.
        Assert.True(Grip(textarea).IsEffectivelyVisible);
        Assert.Equal(TextareaResize.Vertical, Grip(textarea).Direction);
        // Merged over the part's theme: the root keeps its radius, the icon its path.
        Assert.Equal(new CornerRadius(TextareaOracle.Value.Layout[0].RootRadius), P<Border>(textarea, Textarea.RootPart).CornerRadius);
        Assert.NotNull(P<InputIcon>(textarea, BaseInput.ClearIconPart).Data);

        // Even over a state style: an override's plain setter wins (ADR 0001, item 6).
        scene.Key(Key.Tab);
        Assert.Same(Red, P<Border>(textarea, Textarea.RootPart).BorderBrush);
    }

    // A border an override gives the Input part is round the text and the grip, inside the textarea's size, as a
    // textarea's (box-sizing: border-box).
    [AvaloniaFact]
    public void A_border_an_override_gives_the_textarea_is_round_its_text()
    {
        var plain = new Textarea { Text = "Value", Resize = TextareaResize.Both };
        var bordered = new Textarea
        {
            Text = "Value", Resize = TextareaResize.Both,
            Overrides = [new Override { Part = BaseInput.InputPart, Style = Theme<TextareaField>(Border.BorderThicknessProperty, new Thickness(3)) }],
        };
        using var scene = Show(plain, bordered);
        var a = P<TextareaField>(plain, BaseInput.InputPart);
        var b = P<TextareaField>(bordered, BaseInput.InputPart);
        Assert.Equal(a.Bounds.Width + 6, b.Bounds.Width);
        Assert.Equal(a.Bounds.Height + 6, b.Bounds.Height);
        var scroller = Scroller(bordered);
        Assert.Equal(new Rect(3, 3, b.Bounds.Width - 6, b.Bounds.Height - 6), new Rect(scroller.TranslatePoint(default, b)!.Value, scroller.Bounds.Size));
        var grip = Grip(bordered);
        Assert.Equal(new Point(b.Bounds.Width - 3, b.Bounds.Height - 3), grip.TranslatePoint(new Point(grip.Bounds.Width, grip.Bounds.Height), b));
    }

    [AvaloniaFact]
    public void The_clear_icon_is_Base_Webs_path()
    {
        var textarea = new Textarea { Clearable = true, Text = "1234" };
        using var scene = Show(textarea);
        var facts = TextareaOracle.Value.Icons["clear"];
        var icon = P<InputIcon>(textarea, BaseInput.ClearIconPart);
        Assert.Equal(Geometry.Parse(facts.D).Bounds, icon.Data!.Bounds);
        Assert.Equal(new Rect(facts.ViewBox[0], facts.ViewBox[1], facts.ViewBox[2], facts.ViewBox[3]), icon.ViewBox);
        Assert.Equal(facts.FillRule == "evenodd" ? "F0 " : "F1 ", ClearData()[..3]);
    }

    /// <summary>The clear icon's path data as the generated theme writes it.</summary>
    private static string ClearData()
    {
        var file = File.ReadAllText(Path.Combine(Metered.Tests.Repository.RepositoryFiles.FindRoot(), "src", "Metered.Themes", "Controls", "Textarea.Parts.g.axaml"));
        const string start = "<Setter Property=\"Data\" Value=\"";
        var at = file.IndexOf(start, StringComparison.Ordinal) + start.Length;
        return file[at..file.IndexOf('"', at)];
    }

    [AvaloniaFact]
    public void Transitions_wait_for_load()
    {
        var textarea = new Textarea();
        var host = new Border { Child = textarea };
        using var scene = Show(host);
        var t = TextareaOracle.Value.Transitions;
        void Check(string part, string fact, string css, AvaloniaProperty property)
        {
            var transitions = Assert.IsType<Transitions>(P<Animatable>(textarea, part).Transitions);
            Assert.Equal([property], transitions.OfType<BrushTransition>().Select(x => (AvaloniaProperty)x.Property!));
            Assert.Equal([css], t[fact].Properties);
            Assert.All(transitions.OfType<BrushTransition>(), x => Assert.Equal(TimeSpan.FromSeconds(t[fact].Seconds), x.Duration));
        }

        Check(Textarea.RootPart, "root", "border", Border.BorderBrushProperty);
        Check(BaseInput.InputContainerPart, "container", "background-color", Border.BackgroundProperty);

        host.Child = null;
        scene.Pump();
        Assert.Null(P<Animatable>(textarea, Textarea.RootPart).Transitions);
        host.Child = textarea;
        Assert.Null(P<Animatable>(textarea, Textarea.RootPart).Transitions);
        scene.Pump();
        Assert.NotNull(P<Animatable>(textarea, Textarea.RootPart).Transitions);
    }

    [AvaloniaFact]
    public void It_is_an_edit_to_UI_Automation_and_its_clear_icon_a_button()
    {
        var textarea = new Textarea { Clearable = true, Text = "Thing" };
        using var scene = Show(textarea);
        var peer = ControlAutomationPeer.CreatePeerForElement(textarea);
        Assert.Equal(AutomationControlType.Edit, peer.GetAutomationControlType());
        var clear = ControlAutomationPeer.CreatePeerForElement(P<Control>(textarea, BaseInput.ClearIconPart));
        Assert.Equal(BaseInput.ClearLabel, clear.GetName());
        ((Avalonia.Automation.Provider.IInvokeProvider)clear).Invoke();
        scene.Pump();
        Assert.Equal("", textarea.Text);
    }

    private static ControlTheme Theme<T>(AvaloniaProperty property, object value) =>
        new(typeof(T)) { Setters = { new Setter(property, value) } };
}
