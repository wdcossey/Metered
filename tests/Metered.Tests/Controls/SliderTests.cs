using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Metered.Controls;
using Metered.Themes;
using Slider = Metered.Controls.Slider;

namespace Metered.Tests.Controls;

/// <summary>
/// A slider's own behaviour, as Base Web's slider.js and react-range's Range.js and utils.js do it: the value a drag or
/// a key lands on (react-range's normalizeValue, against the oracle), a press and a drag by the mouse and by touch, in a
/// page that scrolls and not, the keys, Change and FinalChange, the thumbs' labels, focus, the value's checks, the parts'
/// states, overrides and UI Automation.
/// </summary>
public class SliderTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    /// <summary>A slider 350 wide (its line 318), 80 down a phone's window.</summary>
    private static (Slider Slider, Scene Scene) Show(Slider slider) =>
        (slider, new Scene(new StackPanel { Margin = new Thickness(20, 80, 20, 20), Children = { slider } }));

    /// <summary>The window's point on the slider's line at a fraction of its length.</summary>
    private static Point At(Scene scene, Slider slider, double fraction)
    {
        var inner = (Visual)slider.Part(Slider.InnerTrackPart)!;
        return inner.TranslatePoint(new Point(inner.Bounds.Width * fraction, inner.Bounds.Height / 2), scene.Window)!.Value;
    }

    /// <summary>Change and FinalChange as the slider raises them, each with its value.</summary>
    private sealed class Events
    {
        public readonly List<double[]> Changes = [];
        public readonly List<double[]> Finals = [];

        public Events(Slider slider)
        {
            slider.Change += (_, e) => Changes.Add(e.Value.ToArray());
            slider.FinalChange += (_, e) => Finals.Add(e.Value.ToArray());
        }
    }

    private static void Drag(Scene scene, Point from, params Point[] path)
    {
        scene.Window.MouseMove(from);
        scene.Window.MouseDown(from, MouseButton.Left);
        scene.Pump();
        foreach (var p in path)
        {
            scene.Window.MouseMove(p);
            scene.Pump();
        }

        scene.Window.MouseUp(path.Length > 0 ? path[^1] : from, MouseButton.Left);
        scene.Pump();
    }

    // --- The value a move lands on: react-range's normalizeValue.

    public static TheoryData<int> NormalizeCases => new(Enumerable.Range(0, SliderOracle.Data.Normalize.Length));

    [AvaloniaTheory]
    [MemberData(nameof(NormalizeCases))]
    public void A_value_lands_where_react_ranges_normalizeValue_puts_it(int index)
    {
        var c = SliderOracle.Data.Normalize[index];
        Assert.Equal(c.Expected, Slider.Normalize(c.Value, c.Index, c.Min, c.Max, c.Step, c.Values));
    }

    public static TheoryData<int> NumberCases => new(Enumerable.Range(0, SliderOracle.Data.Numbers.Length));

    // A label with no valueToLabel is the number as JavaScript writes it (String(n)), which react-range's getStepDecimals
    // also reads a step's decimals from: plain from 1e-6 to 1e21, an exponent beyond.
    [AvaloniaTheory]
    [MemberData(nameof(NumberCases))]
    public void A_number_is_written_as_JavaScript_writes_it(int index)
    {
        var c = SliderOracle.Data.Numbers[index];
        Assert.Equal(c.Text, Slider.JsNumber(c.Value));
    }

    // react-range's normalizeValue holds a thumb at a neighbour only when that neighbour is truthy: a neighbour at 0 lets
    // it pass. Here a neighbour at 0 holds it as one at 10 does (docs/controls/Slider/, known differences).
    [AvaloniaFact]
    public void A_neighbour_at_zero_holds_a_thumb_as_any_other_does()
    {
        var oracle = SliderOracle.Data.Normalize.First(c => c is { Index: 1, Min: -300 } && c.Value < c.Values[0]);
        Assert.Equal(oracle.Values[0], oracle.Expected);
        Assert.Equal(0, Slider.Normalize(-10, 1, -50, 50, 1, [0, 20]));
        Assert.Equal(0, Slider.Normalize(10, 0, -50, 50, 1, [-20, 0]));
    }

    // --- The pointer.

    // onMouseDownTrack: a press on the track off the thumb moves the closest thumb to it (onMove: x / width × (max − min)
    // + min, then normalizeValue), focuses it with no ring, and the release makes it final.
    [AvaloniaFact]
    public void A_click_on_the_track_moves_the_thumb_there()
    {
        var (slider, scene) = Show(new Slider { Value = [60] });
        using var _ = scene;
        var events = new Events(slider);
        scene.Window.MouseMove(At(scene, slider, 0.25));
        scene.Window.MouseDown(At(scene, slider, 0.25), MouseButton.Left);
        scene.Pump();
        Assert.Equal([25.0], slider.Value);
        Assert.Equal(0, slider.DraggedThumb);
        Assert.True(slider.Thumbs[0].IsFocused);
        Assert.DoesNotContain(":focus-visible", slider.Thumbs[0].Classes);
        Assert.Equal([[25.0]], events.Changes);
        Assert.Empty(events.Finals);

        scene.Window.MouseUp(At(scene, slider, 0.25), MouseButton.Left);
        scene.Pump();
        Assert.Equal(-1, slider.DraggedThumb);
        Assert.Equal([[25.0]], events.Finals);
    }

    // onMove takes a pointer only half a step or more from the thumb's value; a press on the thumb's centre changes nothing,
    // and still ends final (onEnd fires onFinalChange whenever a thumb was dragged).
    [AvaloniaFact]
    public void A_press_on_the_thumb_moves_nothing_until_half_a_step()
    {
        var (slider, scene) = Show(new Slider { Value = [50], Step = 10 });
        using var _ = scene;
        var events = new Events(slider);
        Drag(scene, At(scene, slider, 0.5), At(scene, slider, 0.54), At(scene, slider, 0.56));
        Assert.Equal([[60.0]], events.Changes);
        Assert.Equal([60.0], slider.Value);
        Assert.Equal([[60.0]], events.Finals);

        events.Changes.Clear();
        Drag(scene, scene.CentreOf(slider.Thumbs[0]));
        Assert.Empty(events.Changes);
        Assert.Equal(2, events.Finals.Count);
    }

    // The drag follows the pointer off the slider (react-range listens on the document), held at the ends.
    [AvaloniaFact]
    public void A_mouse_drag_follows_the_pointer_off_the_slider()
    {
        var (slider, scene) = Show(new Slider { Value = [30] });
        using var _ = scene;
        var events = new Events(slider);
        var thumb = scene.CentreOf(slider.Thumbs[0]);
        var end = At(scene, slider, 1);
        Drag(scene, thumb, At(scene, slider, 0.8), new Point(end.X + 10, end.Y + 300), new Point(2, end.Y - 60));
        Assert.Equal([0.0], slider.Value);
        Assert.Equal([[80.0], [100.0], [0.0]], events.Changes);
        Assert.Equal([[0.0]], events.Finals);
    }

    // react-range ignores any but the primary button.
    [AvaloniaFact]
    public void A_right_press_does_nothing()
    {
        var (slider, scene) = Show(new Slider { Value = [30] });
        using var _ = scene;
        var events = new Events(slider);
        var at = At(scene, slider, 0.8);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Window.MouseUp(at, MouseButton.Right);
        scene.Pump();
        Assert.Equal([30.0], slider.Value);
        Assert.Empty(events.Changes);
        Assert.Empty(events.Finals);
    }

    // getClosestThumbIndex picks which of two a press on the track moves; the first wins a tie.
    [AvaloniaFact]
    public void A_press_between_two_thumbs_moves_the_closer()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70] });
        using var _ = scene;
        Drag(scene, At(scene, slider, 0.5));
        Assert.Equal([20.0, 50.0], slider.Value);
        Assert.True(slider.Thumbs[1].IsFocused);
        Drag(scene, At(scene, slider, 0.3));
        Assert.Equal([30.0, 50.0], slider.Value);
        Assert.True(slider.Thumbs[0].IsFocused);
    }

    // Two thumbs on one value are equally close to a press on the line: the first takes it, and goes below the second.
    [AvaloniaFact]
    public void Of_two_thumbs_on_one_value_a_press_off_them_moves_the_first()
    {
        var (slider, scene) = Show(new Slider { Value = [50, 50] });
        using var _ = scene;
        Drag(scene, At(scene, slider, 0.2));
        Assert.Equal([20.0, 50.0], slider.Value);
        Assert.True(slider.Thumbs[0].IsFocused);
    }

    // onMouseOrTouchStart, after onMouseDownTrack: a press on a thumb drags that one, even where another is closer. Two
    // thumbs on one value: the later is drawn on top and takes the press, and the earlier holds it from going below.
    [AvaloniaFact]
    public void A_press_on_a_thumb_drags_that_one_and_thumbs_never_cross()
    {
        var (slider, scene) = Show(new Slider { Value = [50, 50] });
        using var _ = scene;
        var on = scene.CentreOf(slider.Thumbs[1]);
        Drag(scene, on, At(scene, slider, 0.3));
        Assert.Equal([50.0, 50.0], slider.Value);
        Drag(scene, on, At(scene, slider, 0.8));
        Assert.Equal([50.0, 80.0], slider.Value);

        // Thumb 0 dragged past thumb 1 stops at it.
        Drag(scene, scene.CentreOf(slider.Thumbs[0]), At(scene, slider, 0.95));
        Assert.Equal([80.0, 80.0], slider.Value);
    }

    // A finger drags as the mouse does, and never hovers: its label shows while it drags, and goes with it.
    [AvaloniaFact]
    public void A_touch_drags_the_thumb_and_shows_its_label_only_while_it_does()
    {
        var (slider, scene) = Show(new Slider { Value = [30] });
        using var _ = scene;
        var events = new Events(slider);
        var thumb = slider.Thumbs[0];
        var touch = scene.Window.TouchBegin(scene.CentreOf(thumb), RawInputModifiers.None);
        scene.Pump();
        Assert.Equal(0, slider.DraggedThumb);
        Assert.NotEmpty(thumb.Children);
        Assert.Contains(":is-dragged", thumb.Classes);
        Assert.Contains(":is-dragged", slider.Part(Slider.TrackPart)!.Classes);
        scene.Window.TouchMove(touch, At(scene, slider, 0.6), RawInputModifiers.None);
        scene.Pump();
        Assert.Equal([60.0], slider.Value);
        scene.Window.TouchEnd(touch, At(scene, slider, 0.6), RawInputModifiers.None);
        scene.Pump();
        Assert.Equal(-1, slider.DraggedThumb);
        Assert.Empty(thumb.Children);
        Assert.DoesNotContain(":is-dragged", thumb.Classes);
        Assert.Equal([[60.0]], events.Finals);
    }

    // Metered's hover is a mouse's (ADR 0001): a finger over a thumb, pressing nothing, shows no label.
    [AvaloniaFact]
    public void A_finger_never_hovers_a_thumb()
    {
        var (slider, scene) = Show(new Slider { Value = [30] });
        using var _ = scene;
        var thumb = slider.Thumbs[0];
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        foreach (var e in new[] { InputElement.PointerEnteredEvent, InputElement.PointerMovedEvent })
        {
            thumb.RaiseEvent(new PointerEventArgs(e, thumb, touch, scene.Window, scene.CentreOf(thumb), 0, new PointerPointProperties(), KeyModifiers.None));
        }

        scene.Pump();
        Assert.Empty(thumb.Children);
        scene.Hover(thumb);
        Assert.NotEmpty(thumb.Children);
    }

    // onTouchStartTrack: a touch on the track off the thumb moves the closest thumb to it at once.
    [AvaloniaFact]
    public void A_touch_on_the_track_moves_the_thumb_there()
    {
        var (slider, scene) = Show(new Slider { Value = [30] });
        using var _ = scene;
        var at = At(scene, slider, 0.9);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        Assert.Equal([90.0], slider.Value);
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
        Assert.Equal([90.0], slider.Value);
    }

    /// <summary>A slider in a page that scrolls, as the sample's: a ScrollViewer from Avalonia's Simple theme, which pans by touch.</summary>
    private static (Slider Slider, ScrollViewer Page, Scene Scene) InAPage(Slider? given = null)
    {
        var slider = given ?? new Slider { Value = [20] };
        var page = new ScrollViewer
        {
            Content = new StackPanel { Margin = new Thickness(20, 200, 20, 1600), Children = { slider } },
        };
        return (slider, page, new Scene(page, themes: [new SimpleTheme(), new BaseTheme()]));
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

    // Base Web's touchmove handler prevents the page's scrolling: a touch that starts on the track is the slider's. Across,
    // the thumb follows the finger; mostly up the page, it still does, and the page doesn't move; off the slider, the same
    // drag up pans the page, so the first two prove something.
    [AvaloniaFact]
    public void In_a_page_that_scrolls_a_touch_drag_on_the_slider_moves_the_thumb_not_the_page()
    {
        var (slider, page, scene) = InAPage();
        using var _ = scene;
        Assert.True(page.Extent.Height > page.Viewport.Height);

        TouchDrag(scene, scene.CentreOf(slider.Thumbs[0]), At(scene, slider, 0.8));
        Assert.Equal([80.0], slider.Value);
        Assert.Equal(0, page.Offset.Y);

        var from = At(scene, slider, 0.3);
        TouchDrag(scene, from, new Point(At(scene, slider, 0.5).X, from.Y - 150));
        Assert.Equal([50.0], slider.Value);
        Assert.Equal(0, page.Offset.Y);

        var off = new Point(195, scene.CentreOf(slider).Y + 120);
        TouchDrag(scene, off, new Point(off.X, off.Y - 150));
        Assert.True(page.Offset.Y > 60, $"the page moved {page.Offset.Y}");
        Assert.Equal([50.0], slider.Value);
    }

    // A press the slider doesn't take (here, a slider with no range: its minimum above its maximum) leaves the page its pan.
    [AvaloniaFact]
    public void A_touch_the_slider_doesnt_take_still_pans_the_page()
    {
        var (slider, page, scene) = InAPage(new Slider { Value = [50], Min = 100, Max = 0 });
        using var _ = scene;
        var on = scene.CentreOf((Visual)slider.Part(Slider.TrackPart)!);
        TouchDrag(scene, on, new Point(on.X, on.Y - 150));
        Assert.True(page.Offset.Y > 60, $"the page moved {page.Offset.Y}");
    }

    // The thumb a finger drags taken away (two values become one): its drag ends, final, and the next press is taken.
    [AvaloniaFact]
    public void Taking_away_the_dragged_thumb_ends_its_drag()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70] });
        using var _ = scene;
        var events = new Events(slider);
        var touch = scene.Window.TouchBegin(scene.CentreOf(slider.Thumbs[1]), RawInputModifiers.None);
        scene.Pump();
        Assert.Equal(1, slider.DraggedThumb);
        slider.Value = [40];
        scene.Pump();
        Assert.Equal(-1, slider.DraggedThumb);
        Assert.Equal([[40.0]], events.Finals);
        scene.Window.TouchEnd(touch, scene.CentreOf(slider), RawInputModifiers.None);
        scene.Pump();
        Drag(scene, At(scene, slider, 0.9));
        Assert.Equal([90.0], slider.Value);
    }

    // react-range makes a mark for each step, however many; past what a count can hold, none rather than a wrong count.
    [AvaloniaFact]
    public void Steps_past_counting_make_no_marks()
    {
        // numOfMarks + 1, rounded up: 10 steps make 11 marks, 2.5 make 4 (i = 0 to 3).
        Assert.Equal(11, Slider.MarkCount(10));
        Assert.Equal(4, Slider.MarkCount(2.5));
        Assert.Equal(0, Slider.MarkCount(1e15));
        Assert.Equal(0, Slider.MarkCount(int.MaxValue));
        var slider = new Slider { Min = 0, Max = 1e12, Step = 1e-3, Value = [0], Marks = true };
        Assert.Empty(slider.Elements(Slider.MarkPart));
    }

    // The mouse's hover shows a thumb's label; a disabled slider shows none, unless persistentThumb.
    [AvaloniaFact]
    public void A_mouse_over_a_thumb_shows_its_label()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70] });
        using var _ = scene;
        scene.Hover(slider.Thumbs[1]);
        Assert.Empty(slider.Thumbs[0].Children);
        Assert.Equal(2, slider.Thumbs[1].Children.Count);
        Assert.Equal("70", ((ContentPresenter)slider.Thumbs[1].Children[0]).Content);
        scene.Window.MouseMove(new Point(5, 5));
        scene.Pump();
        Assert.Empty(slider.Thumbs[1].Children);
    }

    // slider.js's onMouseEnter and onMouseLeave on the Thumb: its label is its child, so a mouse moved up onto the label is
    // still over the thumb, and the label stays.
    [AvaloniaFact]
    public void A_mouse_moved_onto_the_label_keeps_it()
    {
        var (slider, scene) = Show(new Slider { Value = [50] });
        using var _ = scene;
        var thumb = slider.Thumbs[0];
        scene.Hover(thumb);
        var label = (Visual)thumb.Children[0];
        scene.Window.MouseMove(scene.CentreOf(thumb) - new Point(0, 2));
        scene.Window.MouseMove(scene.CentreOf(label));
        scene.Pump();
        Assert.Equal(2, thumb.Children.Count);
        scene.Window.MouseMove(new Point(5, 5));
        scene.Pump();
        Assert.Empty(thumb.Children);
    }

    [AvaloniaFact]
    public void PersistentThumb_shows_every_label_disabled_too()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70], PersistentThumb = true });
        using var _ = scene;
        Assert.All(slider.Thumbs, t => Assert.Equal(2, t.Children.Count));
        slider.IsEnabled = false;
        scene.Pump();
        Assert.All(slider.Thumbs, t => Assert.Equal(2, t.Children.Count));
        slider.PersistentThumb = false;
        Assert.All(slider.Thumbs, t => Assert.Empty(t.Children));
    }

    // A disabled slider takes no press and no focus (react-range: no tabIndex, its handlers off).
    [AvaloniaFact]
    public void A_disabled_slider_takes_nothing()
    {
        var (slider, scene) = Show(new Slider { Value = [30], IsEnabled = false });
        using var _ = scene;
        var events = new Events(slider);
        Drag(scene, At(scene, slider, 0.3), At(scene, slider, 0.9));
        scene.Key(Key.Tab);
        Assert.False(slider.Thumbs[0].IsFocused);
        Assert.Equal([30.0], slider.Value);
        Assert.Empty(events.Changes);
        Assert.Empty(events.Finals);
    }

    // --- The keys: react-range's onKeyDown and onKeyUp, on a focused thumb.

    private static void Down(Scene scene, Key key, string? symbol = null, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        scene.Window.KeyPress(key, modifiers, PhysicalKey.None, symbol);
        scene.Pump();
    }

    private static void Up(Scene scene, Key key, string? symbol = null, RawInputModifiers modifiers = RawInputModifiers.None)
    {
        scene.Window.KeyRelease(key, modifiers, PhysicalKey.None, symbol);
        scene.Pump();
    }

    public static TheoryData<Key, string?, double> Keys => new()
    {
        { Key.Right, null, 51 },
        { Key.Up, null, 51 },
        { Key.K, "k", 51 },
        { Key.PageUp, null, 60 },
        { Key.Left, null, 49 },
        { Key.Down, null, 49 },
        { Key.J, "j", 49 },
        { Key.PageDown, null, 40 },
    };

    // INCREASE_KEYS and DECREASE_KEYS: a step, or ten for PageUp and PageDown; the thumb is dragged (its label shows)
    // until the key's release, which makes the change final.
    [AvaloniaTheory]
    [MemberData(nameof(Keys))]
    public void A_key_on_a_focused_thumb_steps_it(Key key, string? symbol, double expected)
    {
        var (slider, scene) = Show(new Slider { Value = [50] });
        using var _ = scene;
        var events = new Events(slider);
        scene.Key(Key.Tab);
        Assert.True(slider.Thumbs[0].IsFocused);
        Assert.Contains(":focus-visible", slider.Thumbs[0].Classes);

        Down(scene, key, symbol);
        Assert.Equal([expected], slider.Value);
        Assert.Equal([[expected]], events.Changes);
        Assert.Equal(0, slider.DraggedThumb);
        Assert.NotEmpty(slider.Thumbs[0].Children);
        Assert.Empty(events.Finals);
        Up(scene, key, symbol);
        Assert.Equal(-1, slider.DraggedThumb);
        Assert.Empty(slider.Thumbs[0].Children);
        Assert.Equal([[expected]], events.Finals);
    }

    // react-range reads the key's own name: Shift+K is "K", which isn't an increase key; any other key ends a change the
    // keys made (fireOnFinalChange), as Tab does.
    [AvaloniaFact]
    public void Shift_K_does_nothing_and_another_key_makes_a_change_final()
    {
        var (slider, scene) = Show(new Slider { Value = [50] });
        using var _ = scene;
        var events = new Events(slider);
        scene.Key(Key.Tab);
        Down(scene, Key.K, "K", RawInputModifiers.Shift);
        Up(scene, Key.K, "K", RawInputModifiers.Shift);
        Assert.Equal([50.0], slider.Value);
        Assert.Empty(events.Changes);
        Assert.Empty(events.Finals);

        Down(scene, Key.Right);
        Down(scene, Key.A, "a");
        Assert.Equal([[51.0]], events.Finals);
        Up(scene, Key.A, "a");
        Up(scene, Key.Right);
        Assert.Single(events.Finals);

        // Tab, held, ends the drag and makes the change final before the focus moves on.
        Down(scene, Key.Left);
        Assert.Equal(0, slider.DraggedThumb);
        Down(scene, Key.Tab);
        Assert.Equal(-1, slider.DraggedThumb);
        Assert.Equal([[51.0], [50.0]], events.Finals);
    }

    // onChange is called even where the value can't move: at the maximum, a step up gives the maximum again.
    [AvaloniaFact]
    public void A_step_past_the_end_stays_and_still_changes()
    {
        var (slider, scene) = Show(new Slider { Value = [100] });
        using var _ = scene;
        var events = new Events(slider);
        scene.Key(Key.Tab);
        scene.Key(Key.Right);
        Assert.Equal([100.0], slider.Value);
        Assert.Equal([[100.0]], events.Changes);
        Assert.Equal([[100.0]], events.Finals);
    }

    // Each thumb is a tab stop, its focus ring its own; a thumb stepped into its neighbour stops at it.
    [AvaloniaFact]
    public void Tab_goes_from_thumb_to_thumb_and_keys_never_cross_them()
    {
        var (slider, scene) = Show(new Slider { Value = [48, 50] });
        using var _ = scene;
        scene.Key(Key.Tab);
        Assert.True(slider.Thumbs[0].IsFocused);
        scene.Key(Key.Right);
        scene.Key(Key.Right);
        scene.Key(Key.Right);
        Assert.Equal([50.0, 50.0], slider.Value);

        scene.Key(Key.Tab);
        Assert.True(slider.Thumbs[1].IsFocused);
        Assert.Contains(":focus-visible", slider.Thumbs[1].Classes);
        Assert.DoesNotContain(":focus-visible", slider.Thumbs[0].Classes);
        scene.Key(Key.Left);
        Assert.Equal([50.0, 50.0], slider.Value);
        scene.Key(Key.PageUp);
        Assert.Equal([50.0, 60.0], slider.Value);
    }

    // slider.js's handleFocus: isFocusVisible only for a keyboard's focus; its blur ends it.
    [AvaloniaFact]
    public void Only_a_keyboards_focus_shows_the_ring()
    {
        var slider = new Slider { Value = [50] };
        var other = new Border { Focusable = true, Height = 20 };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20, 80, 20, 20), Children = { slider, other } });
        var thumb = slider.Thumbs[0];
        scene.Click(thumb);
        Assert.True(thumb.IsFocused);
        Assert.DoesNotContain(":focus-visible", thumb.Classes);
        Assert.Equal(0, thumb.OutlineThickness);

        other.Focus();
        thumb.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.Contains(":focus-visible", thumb.Classes);
        Assert.Contains(":focus-visible", slider.Part(Slider.TrackPart)!.Classes);
        Assert.Equal(SliderOracle.Data.Colours.First(c => c.State == "focusVisible").Ring!.Thickness, thumb.OutlineThickness);

        other.Focus();
        scene.Pump();
        Assert.DoesNotContain(":focus-visible", thumb.Classes);
        Assert.DoesNotContain(":focus-visible", slider.Part(Slider.TrackPart)!.Classes);
    }

    // react-range's press focuses the thumb, which is focused already: no focus event, so a keyboard's ring stays through
    // a drag of it, as in a browser.
    [AvaloniaFact]
    public void A_keyboards_ring_stays_through_a_drag()
    {
        var (slider, scene) = Show(new Slider { Value = [50] });
        using var _ = scene;
        var thumb = slider.Thumbs[0];
        scene.Key(Key.Tab);
        Assert.Contains(":focus-visible", thumb.Classes);
        Drag(scene, scene.CentreOf(thumb), At(scene, slider, 0.7));
        Assert.Equal([70.0], slider.Value);
        Assert.True(thumb.IsFocused);
        Assert.Contains(":focus-visible", thumb.Classes);
    }

    // A thumb dragged by a held key whose release won't come to it (the slider disabled, or its focus gone): the drag ends,
    // and the change is final.
    [AvaloniaFact]
    public void A_held_key_drag_ends_when_the_slider_is_disabled()
    {
        var (slider, scene) = Show(new Slider { Value = [50] });
        using var _ = scene;
        var events = new Events(slider);
        scene.Key(Key.Tab);
        Down(scene, Key.Right);
        Assert.Equal(0, slider.DraggedThumb);
        slider.IsEnabled = false;
        scene.Pump();
        Assert.Equal(-1, slider.DraggedThumb);
        Assert.DoesNotContain(":is-dragged", slider.Part(Slider.TrackPart)!.Classes);
        Assert.Equal([[51.0]], events.Finals);
        slider.IsEnabled = true;
        scene.Pump();
        Assert.Empty(slider.Thumbs[0].Children);
    }

    // The focused thumb taken away (two values become one): no ring is left on the slider's parts.
    [AvaloniaFact]
    public void Taking_away_the_focused_thumb_takes_its_ring()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70] });
        using var _ = scene;
        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        Assert.True(slider.Thumbs[1].IsFocused);
        Assert.Contains(":focus-visible", slider.Part(Slider.TrackPart)!.Classes);
        slider.Value = [40];
        scene.Pump();
        Assert.DoesNotContain(":focus-visible", slider.Classes);
        Assert.DoesNotContain(":focus-visible", slider.Part(Slider.TrackPart)!.Classes);
        Assert.DoesNotContain(":focus-visible", slider.Thumbs[0].Classes);
    }

    // react-range throws for a minimum not below the maximum; set one at a time, such a slider takes no input.
    [AvaloniaFact]
    public void A_minimum_not_below_the_maximum_takes_no_input()
    {
        var (slider, scene) = Show(new Slider { Value = [50] });
        using var _ = scene;
        var events = new Events(slider);
        slider.Min = 100;
        slider.Max = 0;
        scene.Pump();
        Drag(scene, At(scene, slider, 0.2), At(scene, slider, 0.8));
        scene.Key(Key.Tab);
        scene.Key(Key.Right);
        Assert.Empty(events.Changes);
        Assert.Empty(events.Finals);
    }

    // --- The value.

    [AvaloniaFact]
    public void None_is_the_middle_as_StatefulSlider_starts()
    {
        Assert.Equal([50.0], new Slider().Value);
        // [Math.round((max − min) / 2) + min].
        Assert.Equal([0.0], new Slider { Min = -300, Max = 300, Value = [] }.Value);
        Assert.Equal([3.0], new Slider { Max = 5 }.Value);
    }

    [AvaloniaFact]
    public void A_value_is_held_inside_the_ends_and_in_order_and_three_are_refused()
    {
        Assert.Equal([0.0, 100.0], new Slider { Value = [120, -5] }.Value);
        var slider = new Slider { Value = [80] };
        slider.Max = 60;
        Assert.Equal([60.0], slider.Value);
        Assert.Throws<ArgumentException>(() => new Slider { Value = [1, 2, 3] });
        Assert.Throws<ArgumentException>(() => new Slider { Step = 0 });
    }

    [AvaloniaFact]
    public void A_second_value_adds_a_thumb_and_the_range_state()
    {
        var (slider, scene) = Show(new Slider { Value = [30] });
        using var _ = scene;
        Assert.Single(slider.Thumbs);
        Assert.DoesNotContain(":range", slider.Part(Slider.InnerTrackPart)!.Classes);
        slider.Value = [20, 70];
        scene.Pump();
        Assert.Equal(2, slider.Thumbs.Count);
        foreach (var name in new[] { Slider.RootPart, Slider.TrackPart, Slider.InnerTrackPart, Slider.TickBarPart })
            Assert.Contains(":range", slider.Part(name)!.Classes);
        Assert.All(slider.Thumbs, t => Assert.Contains(":range", t.Classes));
        Assert.All(slider.Elements(Slider.TickPart), t => Assert.Contains(":range", t.Classes));
        slider.Value = [40];
        scene.Pump();
        Assert.Single(slider.Thumbs);
        Assert.Single(slider.Elements(Slider.ThumbValuePart));
        Assert.DoesNotContain(":range", slider.Part(Slider.TrackPart)!.Classes);
    }

    // valueToLabel: the thumbs' values and both ticks.
    [AvaloniaFact]
    public void ValueToLabel_writes_the_labels_and_the_ticks()
    {
        var (slider, scene) = Show(new Slider { Min = 1000, Max = 8000, Step = 100, Value = [4500], PersistentThumb = true });
        using var _ = scene;
        var ticks = slider.Elements(Slider.TickPart).Cast<ContentPresenter>().ToList();
        Assert.Equal(["1000", "8000"], ticks.Select(t => t.Content));
        Assert.Equal("4500", ((ContentPresenter)slider.Elements(Slider.ThumbValuePart)[0]).Content);
        slider.ValueToLabel = v => $"{v / 1000:0.0}km";
        Assert.Equal(["1.0km", "8.0km"], ticks.Select(t => t.Content));
        Assert.Equal("4.5km", ((ContentPresenter)slider.Elements(Slider.ThumbValuePart)[0]).Content);
    }

    // --- Parts, states, overrides.

    // $isDragged: the Track's and InnerTrack's for any thumb, a thumb's, its ThumbValue's and InnerThumb's for its own.
    [AvaloniaFact]
    public void Dragging_marks_the_track_and_that_thumb_only()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70], PersistentThumb = true });
        using var _ = scene;
        var at = scene.CentreOf(slider.Thumbs[1]);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Pump();
        Assert.Contains(":is-dragged", slider.Part(Slider.TrackPart)!.Classes);
        Assert.Contains(":is-dragged", slider.Part(Slider.InnerTrackPart)!.Classes);
        Assert.DoesNotContain(":is-dragged", slider.Part(Slider.RootPart)!.Classes);
        Assert.Contains(":is-dragged", slider.Thumbs[1].Classes);
        Assert.Contains(":is-dragged", slider.Elements(Slider.ThumbValuePart)[1].Classes);
        Assert.Contains(":is-dragged", slider.Elements(Slider.InnerThumbPart)[1].Classes);
        Assert.DoesNotContain(":is-dragged", slider.Thumbs[0].Classes);
        Assert.DoesNotContain(":is-dragged", slider.Elements(Slider.ThumbValuePart)[0].Classes);
        scene.Window.MouseUp(at, MouseButton.Left);
        scene.Pump();
        Assert.DoesNotContain(":is-dragged", slider.Part(Slider.TrackPart)!.Classes);
    }

    // A part there is more than one of takes its override on each: both thumbs, both ticks, every mark.
    [AvaloniaFact]
    public void An_override_reaches_every_element_of_its_part()
    {
        var slider = new Slider
        {
            Value = [20, 70],
            Marks = true,
            Step = 25,
            Overrides =
            [
                new Override { Part = Slider.ThumbPart, Style = Theme<SliderThumb>(SliderThumb.BackgroundProperty, Red) },
                new Override { Part = Slider.TickPart, Style = Theme<ContentPresenter>(ContentPresenter.ForegroundProperty, Red) },
                new Override { Part = Slider.MarkPart, Style = Theme<Border>(Border.BackgroundProperty, Red) },
                new Override { Part = Slider.InnerTrackPart, Style = Theme<SliderInnerTrack>(SliderInnerTrack.FillProperty, Red) },
            ],
        };
        var (_, scene) = Show(slider);
        using var __ = scene;
        Assert.All(slider.Thumbs, t => Assert.Same(Red, t.Background));
        Assert.All(slider.Elements(Slider.TickPart), t => Assert.Same(Red, ((ContentPresenter)t).Foreground));
        Assert.Equal(5, slider.Elements(Slider.MarkPart).Count);
        Assert.All(slider.Elements(Slider.MarkPart), m => Assert.Same(Red, ((Border)m).Background));
        Assert.Same(Red, ((SliderInnerTrack)slider.Part(Slider.InnerTrackPart)!).Fill);
        // The rest of each part's theme stays: an override merges over it.
        Assert.Equal(SliderOracle.Data.Layout.Thumb[0], slider.Thumbs[0].Bounds.Width);

        // A thumb made later takes it too; taken away, the theme's own comes back.
        slider.Value = [10, 20];
        slider.Overrides = null;
        scene.Pump();
        var rest = SliderOracle.Data.Colours.First(c => c is { Theme: "Light", State: "rest" });
        Assert.All(slider.Thumbs, t => Assert.Equal(rest.Thumb, SliderColourTests.Colour(t.Background)));
        Assert.All(slider.Elements(Slider.MarkPart), m => Assert.Equal(rest.Mark, SliderColourTests.Colour(((Border)m).Background)));
    }

    private static ControlTheme Theme<T>(AvaloniaProperty property, object value) =>
        new(typeof(T)) { Setters = { new Setter(property, value) } };

    // --- UI Automation: react-range's role="slider" and aria-value* on each thumb.

    [AvaloniaFact]
    public void Each_thumb_is_a_slider_to_UI_Automation_bounded_by_its_neighbour()
    {
        var (slider, scene) = Show(new Slider { Value = [20, 70], Step = 5 });
        using var _ = scene;
        Avalonia.Automation.AutomationProperties.SetName(slider, "Price");
        var first = ControlAutomationPeer.CreatePeerForElement(slider.Thumbs[0]);
        var second = ControlAutomationPeer.CreatePeerForElement(slider.Thumbs[1]);
        Assert.Equal(AutomationControlType.Slider, first.GetAutomationControlType());
        Assert.True(first.IsControlElement());
        Assert.Equal("Price", first.GetName());
        var a = (IRangeValueProvider)first;
        var b = (IRangeValueProvider)second;
        Assert.Equal((0.0, 70.0, 20.0, 5.0, 50.0), (a.Minimum, a.Maximum, a.Value, a.SmallChange, a.LargeChange));
        Assert.Equal((20.0, 100.0, 70.0), (b.Minimum, b.Maximum, b.Value));

        var events = new Events(slider);
        var raised = new List<(AutomationPeer, Avalonia.Automation.AutomationProperty)>();
        second.PropertyChanged += (_, e) => raised.Add((second, e.Property));
        first.PropertyChanged += (_, e) => raised.Add((first, e.Property));
        a.SetValue(33);
        Assert.Equal([35.0, 70.0], slider.Value);
        Assert.Equal(35, a.Value);
        Assert.Equal(35, b.Minimum);
        // Its value, and the neighbour's range, which it bounds.
        Assert.Contains((first, Avalonia.Automation.RangeValuePatternIdentifiers.ValueProperty), raised);
        Assert.Contains((second, Avalonia.Automation.RangeValuePatternIdentifiers.MinimumProperty), raised);
        Assert.Single(events.Finals);
        raised.Clear();
        slider.IsEnabled = false;
        Assert.True(a.IsReadOnly);
        Assert.Contains((first, Avalonia.Automation.RangeValuePatternIdentifiers.IsReadOnlyProperty), raised);
    }
}
