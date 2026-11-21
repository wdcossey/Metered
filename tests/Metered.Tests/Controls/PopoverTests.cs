using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Metered.Controls;
using Metered.Themes;
using MeteredButton = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>
/// The popover's behaviour against Base Web's (popover.js, stateful-container.js, layer/layers-manager.js): what opens it
/// and closes it, by real mouse, touch and keys; its entrance and exit; focus; its content; its overrides and UI
/// Automation.
/// </summary>
public class PopoverTests
{
    private static PopoverDefaults Defaults => PopoverOracle.Value.Defaults;

    /// <summary>A window with a template (SimpleTheme's): the overlay layer a popover is shown in, as an app's window has.</summary>
    internal static Scene Show(Control content, ThemeVariant? variant = null) =>
        new(content, variant, themes: [new SimpleTheme(), new BaseTheme()]);

    /// <summary>A popover round a fixed box at a place in the window, with content of a fixed size.</summary>
    internal static (Popover Popover, Border Anchor, Border Content) Fixed(double x, double y, double width = 90, double height = 40,
        double contentWidth = 120, double contentHeight = 60)
    {
        var anchor = new Border { Width = width, Height = height, Background = Brushes.Transparent };
        var content = new Border { Width = contentWidth, Height = contentHeight };
        var popover = new Popover
        {
            Anchor = anchor, Content = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(x, y, 0, 0),
        };
        return (popover, anchor, content);
    }

    /// <summary>
    /// Runs the dispatcher's loop for a while, so its timers fire (the entrance's 20 ms, the hover delays) and transitions
    /// run: headless, a timer fires only in the loop, not from RunJobs.
    /// </summary>
    internal static void Settle(Scene scene, int ms = 200)
    {
        using var stop = new CancellationTokenSource(ms);
        Dispatcher.UIThread.MainLoop(stop.Token);
        scene.Pump();
    }

    internal static Rect BoxOf(Visual v, Scene scene) => new(v.TranslatePoint(default, scene.Window)!.Value, v.Bounds.Size);

    private static Visual Body(Popover p) => (Visual)p.Part(Popover.BodyPart)!;

    /// <summary>Shown: the popup open and the body drawn (its entrance under way or done).</summary>
    private static bool Shown(Popover p) => p.PopupHost!.IsOpen && Body(p).Opacity > 0;

    private static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    private static void Press(Scene scene, Point at)
    {
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Pump();
        scene.Window.MouseUp(at, MouseButton.Left);
        scene.Pump();
    }

    // ---- Defaults ----

    [AvaloniaFact]
    public void Its_defaults_are_Base_Webs()
    {
        var p = new Popover();
        Assert.Equal(Defaults.Placement, Camel(p.Placement));
        Assert.Equal(Defaults.TriggerType, p.TriggerType.ToString().ToLowerInvariant());
        Assert.Equal(Defaults.AccessibilityType, p.AccessibilityType.ToString().ToLowerInvariant());
        Assert.Equal(Defaults.ShowArrow, p.ShowArrow);
        Assert.Equal(Defaults.FocusLock, p.FocusLock);
        Assert.Equal(Defaults.AutoFocus, p.AutoFocus);
        Assert.Equal(Defaults.ReturnFocus, p.ReturnFocus);
        Assert.Equal(Defaults.IgnoreBoundary, p.IgnoreBoundary);
        Assert.Equal(Defaults.OnMouseEnterDelay, p.MouseEnterDelay);
        Assert.Equal(Defaults.OnMouseLeaveDelay, p.MouseLeaveDelay);
        Assert.Equal(Defaults.DismissOnClickOutside, p.DismissOnClickOutside);
        Assert.Equal(Defaults.DismissOnEsc, p.DismissOnEsc);
        Assert.Equal(Defaults.PopoverMargin, p.PopoverMargin);
        Assert.Equal(PopoverOracle.Value.Constants.AnimateOutTime, p.AnimateOutTime);
        int animateIn = Popover.AnimateInTime;
        double arrowSize = Popover.ArrowSize;
        Assert.Equal(PopoverOracle.Value.Constants.AnimateInTime, animateIn);
        Assert.Equal(PopoverOracle.Value.Constants.ArrowSize, arrowSize);
        Assert.False(p.IsOpen);
    }

    private static string Camel(PopoverPlacement p) => char.ToLowerInvariant(p.ToString()[0]) + p.ToString()[1..];

    // ---- Click trigger ----

    [AvaloniaFact]
    public void A_click_on_the_anchor_opens_it_and_another_closes_it()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.True(popover.IsOpen);
        Assert.True(Shown(popover));
        scene.Click(anchor);
        Settle(scene);
        Assert.False(popover.IsOpen);
        Assert.False(popover.PopupHost!.IsOpen);
    }

    [AvaloniaFact]
    public void A_tap_on_the_anchor_opens_it()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        using var scene = Show(popover);
        Tap(scene, scene.CentreOf(anchor));
        Settle(scene);
        Assert.True(Shown(popover));
    }

    // A button anchor clicks once for a press and release, a tap, Enter or Space: one toggle each.
    [AvaloniaFact]
    public void A_button_anchor_toggles_it_once_by_mouse_touch_and_keys()
    {
        var button = new MeteredButton { Content = "Open" };
        var popover = new Popover
        {
            Anchor = button, Content = "content", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(100),
        };
        using var scene = Show(popover);
        scene.Click(button);
        Settle(scene, 50);
        Assert.True(popover.IsOpen);
        scene.Click(button);
        Settle(scene, 50);
        Assert.False(popover.IsOpen);
        Tap(scene, scene.CentreOf(button));
        Settle(scene, 50);
        Assert.True(popover.IsOpen);
        Tap(scene, scene.CentreOf(button));
        Settle(scene, 50);
        Assert.False(popover.IsOpen);

        button.Focus(NavigationMethod.Tab);
        scene.Key(Key.Enter);
        Assert.True(popover.IsOpen);
        Settle(scene, 50);
        // "content" has nothing to focus: focus stays on the button, and Space closes it.
        Assert.True(button.IsFocused);
        scene.Key(Key.Space);
        Assert.False(popover.IsOpen);
    }

    [AvaloniaFact]
    public void A_hover_trigger_isnt_opened_by_a_click()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.TriggerType = PopoverTriggerType.Hover;
        popover.MouseEnterDelay = 1000;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene, 50);
        Assert.False(popover.IsOpen);
    }

    // ---- Dismissal: click outside ----

    [AvaloniaFact]
    public void A_mouse_press_outside_closes_it_and_one_inside_the_body_or_on_the_anchor_doesnt()
    {
        var (popover, anchor, content) = Fixed(150, 400);
        var outside = 0;
        popover.ClickOutside += (_, _) => outside++;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);

        // Inside the body: nothing.
        scene.Window.MouseMove(scene.CentreOf(content));
        scene.Window.MouseDown(scene.CentreOf(content), MouseButton.Left);
        scene.Pump();
        Assert.True(popover.IsOpen);
        scene.Window.MouseUp(scene.CentreOf(content), MouseButton.Left);
        Assert.Equal(0, outside);

        // On the anchor: the press isn't outside (its click toggles it, below).
        scene.Window.MouseMove(scene.CentreOf(anchor));
        scene.Window.MouseDown(scene.CentreOf(anchor), MouseButton.Left);
        scene.Pump();
        Assert.True(popover.IsOpen);
        Assert.Equal(0, outside);
        scene.Window.MouseUp(scene.CentreOf(anchor), MouseButton.Left);
        scene.Pump();
        Assert.False(popover.IsOpen);

        scene.Click(anchor);
        Settle(scene);
        Assert.True(popover.IsOpen);
        // A press, not a click: Base Web's mousedown.
        scene.Window.MouseMove(new Point(20, 20));
        scene.Window.MouseDown(new Point(20, 20), MouseButton.Left);
        scene.Pump();
        Assert.False(popover.IsOpen);
        Assert.Equal(1, outside);
        scene.Window.MouseUp(new Point(20, 20), MouseButton.Left);
    }

    // Touch first: a phone's browser sends mousedown only for a tap, so a finger's tap outside closes it and its drag doesn't.
    [AvaloniaFact]
    public void A_tap_outside_closes_it_and_a_finger_that_drags_doesnt()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        using var scene = Show(popover);
        Tap(scene, scene.CentreOf(anchor));
        Settle(scene);
        Assert.True(popover.IsOpen);

        // A finger down outside: not yet.
        var touch = scene.Window.TouchBegin(new Point(20, 700), RawInputModifiers.None);
        scene.Pump();
        Assert.True(popover.IsOpen);
        // Dragged, as a page is scrolled: no tap.
        scene.Window.TouchMove(touch, new Point(20, 600), RawInputModifiers.None);
        scene.Window.TouchEnd(touch, new Point(20, 600), RawInputModifiers.None);
        scene.Pump();
        Assert.True(popover.IsOpen);

        Tap(scene, new Point(20, 700));
        Assert.False(popover.IsOpen);
    }

    [AvaloniaFact]
    public void Without_DismissOnClickOutside_a_press_outside_is_only_told()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.DismissOnClickOutside = false;
        var outside = 0;
        popover.ClickOutside += (_, _) => outside++;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Press(scene, new Point(20, 20));
        Assert.True(popover.IsOpen);
        Assert.Equal(1, outside);
    }

    // popover.js: a hover layer has no document-click handler.
    [AvaloniaFact]
    public void A_hover_popover_isnt_closed_by_a_press_outside()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.TriggerType = PopoverTriggerType.Hover;
        popover.IsOpen = true;
        var outside = 0;
        popover.ClickOutside += (_, _) => outside++;
        using var scene = Show(popover);
        Settle(scene);
        Press(scene, new Point(20, 20));
        Assert.Equal(0, outside);
    }

    // ---- Dismissal: Escape ----

    [AvaloniaFact]
    public void Escape_closes_it_and_without_DismissOnEsc_is_only_told()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        var esc = 0;
        popover.Esc += (_, _) => esc++;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        // layers-manager.js listens to keyup: a press alone does nothing.
        scene.Window.KeyPress(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        scene.Pump();
        Assert.True(popover.IsOpen);
        scene.Window.KeyRelease(Key.Escape, RawInputModifiers.None, PhysicalKey.Escape, null);
        scene.Pump();
        Assert.False(popover.IsOpen);
        Assert.Equal(1, esc);

        popover.DismissOnEsc = false;
        scene.Click(anchor);
        Settle(scene);
        scene.Key(Key.Escape);
        Assert.True(popover.IsOpen);
        Assert.Equal(2, esc);
    }

    // layers-manager.js: Escape and a document click go to the layer on top only.
    [AvaloniaFact]
    public void Escape_and_a_press_outside_close_only_the_layer_on_top()
    {
        var (first, firstAnchor, _) = Fixed(20, 100);
        var (second, secondAnchor, _) = Fixed(200, 500);
        using var scene = Show(new Panel { Children = { first, second } });
        scene.Click(firstAnchor);
        Settle(scene);
        scene.Click(secondAnchor);
        // The press on the second anchor was outside the first, the top layer: it closed.
        Assert.False(first.IsOpen);
        Assert.True(second.IsOpen);
        // Its layer goes once it has (reopened before then, it would keep its layer, under the second's, as Base Web's).
        Settle(scene, 100);

        first.IsOpen = true;
        Settle(scene);
        // first opened last: it is on top.
        scene.Key(Key.Escape);
        Assert.False(first.IsOpen);
        Assert.True(second.IsOpen);

        first.IsOpen = true;
        Settle(scene);
        Press(scene, new Point(370, 20));
        Assert.False(first.IsOpen);
        Assert.True(second.IsOpen);
    }

    // A popover in another's content: a press in the inner one's body is inside it, and the outer never sees it.
    [AvaloniaFact]
    public void A_popover_inside_another_closes_alone()
    {
        var innerAnchor = new Border { Width = 60, Height = 30, Background = Brushes.Transparent };
        var innerContent = new Border { Width = 80, Height = 40, Background = Brushes.Transparent };
        var inner = new Popover { Anchor = innerAnchor, Content = innerContent, Placement = PopoverPlacement.Right };
        var (outer, outerAnchor, _) = Fixed(20, 300);
        outer.Content = new Border { Padding = new Thickness(20), Background = Brushes.Transparent, Child = inner };
        using var scene = Show(outer);
        scene.Click(outerAnchor);
        Settle(scene);
        scene.Click(innerAnchor);
        Settle(scene);
        Assert.True(outer.IsOpen);
        Assert.True(inner.IsOpen);

        Press(scene, scene.CentreOf(innerContent));
        Assert.True(inner.IsOpen);
        Assert.True(outer.IsOpen);

        // In the outer body, outside the inner one: the inner closes, the outer stays.
        Press(scene, BoxOf(Body(outer), scene).TopLeft + new Point(4, 4));
        Assert.False(inner.IsOpen);
        Assert.True(outer.IsOpen);
    }

    // ---- Hover trigger ----

    [AvaloniaFact]
    public void A_mouse_on_the_anchor_opens_it_after_the_delay_and_leaving_closes_it_after_its_own()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.TriggerType = PopoverTriggerType.Hover;
        popover.MouseEnterDelay = 150;
        popover.MouseLeaveDelay = 150;
        using var scene = Show(popover);
        scene.Hover(anchor);
        Settle(scene, 60);
        Assert.False(popover.IsOpen);
        Settle(scene, 200);
        Assert.True(popover.IsOpen);

        scene.Window.MouseMove(new Point(20, 20));
        scene.Pump();
        Settle(scene, 60);
        Assert.True(popover.IsOpen);
        Settle(scene, 200);
        Assert.False(popover.IsOpen);
    }

    [AvaloniaFact]
    public void A_mouse_that_reaches_the_body_keeps_it_open()
    {
        var (popover, anchor, content) = Fixed(150, 400);
        content.Background = Brushes.Transparent;
        popover.TriggerType = PopoverTriggerType.Hover;
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = Show(popover);
        scene.Hover(anchor);
        Settle(scene, 400);
        Assert.True(popover.IsOpen);
        // Into the body before the leave delay ends, past its first 0.1 s (when it takes no pointer).
        scene.Window.MouseMove(scene.CentreOf(content));
        scene.Pump();
        Settle(scene, 400);
        Assert.True(popover.IsOpen);
        scene.Window.MouseMove(new Point(20, 20));
        scene.Pump();
        Settle(scene, 400);
        Assert.False(popover.IsOpen);
    }

    // Touch first: a finger never hovers (as the v2 controls' :is-hovered), so a tap doesn't open a hover popover, nor a
    // finger's pointer entering the anchor (as a phone's browser's emulated mouseenter would).
    [AvaloniaFact]
    public void A_finger_doesnt_open_a_hover_popover()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.TriggerType = PopoverTriggerType.Hover;
        popover.MouseEnterDelay = 0;
        using var scene = Show(popover);
        Tap(scene, scene.CentreOf(anchor));
        Settle(scene, 100);
        Assert.False(popover.IsOpen);

        var presenter = (Visual)Avalonia.VisualTree.VisualExtensions.GetVisualParent(anchor)!;
        var finger = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        ((Interactive)presenter).RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, presenter, finger, scene.Window,
            scene.CentreOf(anchor), 0, new PointerPointProperties(), KeyModifiers.None));
        Settle(scene, 50);
        Assert.False(popover.IsOpen);
        var mouse = new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        ((Interactive)presenter).RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, presenter, mouse, scene.Window,
            scene.CentreOf(anchor), 0, new PointerPointProperties(), KeyModifiers.None));
        Settle(scene, 50);
        Assert.True(popover.IsOpen);
    }

    // A window with no template has no overlay layer: the popover shows nothing, where Avalonia's popup would throw.
    [AvaloniaFact]
    public void In_a_window_with_no_overlay_layer_it_shows_nothing()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        using var scene = new Scene(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.True(popover.IsOpen);
        Assert.False(popover.PopupHost!.IsOpen);
    }

    // stateful-container.js's onFocus: keyboard focus on the anchor opens a hover popover; onBlur closes it unless autoFocus.
    [AvaloniaFact]
    public void Keyboard_focus_on_the_anchor_opens_a_hover_popover_and_leaving_it_closes_it_without_autoFocus()
    {
        var button = new MeteredButton { Content = "Hover" };
        var other = new MeteredButton { Content = "Other" };
        var popover = new Popover { Anchor = button, Content = "content", TriggerType = PopoverTriggerType.Hover, AutoFocus = false };
        using var scene = Show(new StackPanel { Margin = new Thickness(40), Spacing = 200, Children = { popover, other } });
        button.Focus(NavigationMethod.Pointer);
        scene.Pump();
        Assert.False(popover.IsOpen);
        other.Focus(NavigationMethod.Pointer);
        button.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(popover.IsOpen);
        other.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.False(popover.IsOpen);

        popover.AutoFocus = true;
        button.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(popover.IsOpen);
        other.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(popover.IsOpen);
    }

    // ---- Entrance and exit ----

    // popover.js: placed, then 20 ms on (ANIMATE_IN_TIME) $isAnimating, which takes it from its start to its place.
    [AvaloniaFact]
    public void It_opens_hidden_at_its_start_then_enters()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = Show(popover);
        scene.Click(anchor);
        var body = Body(popover);
        Assert.True(popover.PopupHost!.IsOpen);
        Assert.Equal(0, body.Opacity);
        Assert.DoesNotContain(":is-animating", popover.Classes);
        var start = PopoverOracle.Value.Body.Single(b => b.Placement == "bottom" && !b.ShowArrow && b.PopoverMargin == 8).Start;
        Assert.Equal(new Rect(135 + start[0], 448 + start[1], 120, 60), BoxOf(body, scene));

        Settle(scene, 300);
        Assert.Contains(":is-animating", popover.Classes);
        Assert.Contains(":is-animating", ((StyledElement)body).Classes);
        Assert.Equal(1, body.Opacity);
        Assert.Equal(new Rect(135, 448, 120, 60), BoxOf(body, scene));
    }

    // popover.js: $isOpen goes false at once (opacity 0, over animateOutTime: 0 by default), unmounted 20 ms on.
    [AvaloniaFact]
    public void It_closes_at_once_by_default_and_is_gone_soon_after()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        popover.IsOpen = false;
        scene.Pump();
        Assert.Equal(0, Body(popover).Opacity);
        Assert.True(popover.PopupHost!.IsOpen);
        Settle(scene, 100);
        Assert.False(popover.PopupHost!.IsOpen);
    }

    // Body's transition while closing: animateOutTime on easeInCurve; the popup stays until it ends.
    [AvaloniaFact]
    public void With_animateOutTime_it_fades_out_over_it_then_goes()
    {
        var m = PopoverOracle.Value.Motion.Single(x => !x.IsOpen && x.IsAnimating && x.AnimateOutTime == 300 && !x.IsHoverTrigger);
        var (popover, anchor, _) = Fixed(150, 400);
        popover.AnimateOutTime = m.AnimateOutTime;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        popover.IsOpen = false;
        scene.Pump();
        var transitions = ((Border)Body(popover)).Transitions!;
        Assert.Equal(2, transitions.Count);
        Assert.All(transitions, t => Assert.Equal(TimeSpan.FromSeconds(m.Seconds), ((Avalonia.Animation.TransitionBase)t).Duration));
        var easing = (Avalonia.Animation.Easings.SplineEasing)((Avalonia.Animation.TransitionBase)transitions[0]).Easing;
        Assert.Equal(m.Easing, new[] { easing.X1, easing.Y1, easing.X2, easing.Y2 });
        Settle(scene, 120);
        Assert.True(popover.PopupHost!.IsOpen);
        Assert.InRange(Body(popover).Opacity, 0.01, 0.99);
        Settle(scene, 400);
        Assert.False(popover.PopupHost!.IsOpen);
    }

    [AvaloniaFact]
    public void Reopened_while_it_fades_out_it_stays()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.AnimateOutTime = 300;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        popover.IsOpen = false;
        Settle(scene, 60);
        popover.IsOpen = true;
        Settle(scene, 500);
        Assert.True(popover.PopupHost!.IsOpen);
        Assert.Equal(1, Body(popover).Opacity);
    }

    // ---- Focus ----

    [AvaloniaFact]
    public void Once_placed_focus_goes_inside_unless_autoFocus_is_off_or_it_is_a_tooltip()
    {
        foreach (var (autoFocus, type, inside) in new[]
        {
            (true, PopoverAccessibilityType.Menu, true),
            (false, PopoverAccessibilityType.Menu, false),
            (true, PopoverAccessibilityType.Tooltip, false),
            (true, PopoverAccessibilityType.None, true),
        })
        {
            var field = new TextBox();
            var (popover, anchor, _) = Fixed(150, 400);
            popover.Content = new StackPanel { Children = { new TextBlock { Text = "Name" }, field } };
            popover.AutoFocus = autoFocus;
            popover.AccessibilityType = type;
            using var scene = Show(popover);
            scene.Click(anchor);
            Settle(scene);
            Assert.True(inside == field.IsFocused, $"autoFocus {autoFocus}, {type}");
        }
    }

    // react-focus-lock: Tab stays inside, and focus goes back to the anchor when it closes. Without the lock Base Web's
    // leaves focus on the page; Avalonia's popup gives it back all the same (docs/controls/Popover, Known differences).
    [AvaloniaFact]
    public void A_focus_lock_keeps_Tab_inside_and_gives_focus_back()
    {
        var one = new TextBox();
        var two = new TextBox();
        var button = new MeteredButton { Content = "Open" };
        var after = new MeteredButton { Content = "After" };
        var popover = new Popover { Anchor = button, Content = new StackPanel { Children = { one, two } }, FocusLock = true };
        using var scene = Show(new StackPanel { Margin = new Thickness(40), Spacing = 200, Children = { popover, after } });
        button.Focus(NavigationMethod.Tab);
        scene.Key(Key.Enter);
        Settle(scene);
        Assert.True(one.IsFocused);
        scene.Key(Key.Tab);
        Assert.True(two.IsFocused);
        scene.Key(Key.Tab);
        Assert.True(one.IsFocused);
        scene.Key(Key.Escape);
        Settle(scene, 100);
        Assert.False(popover.IsOpen);
        Assert.True(button.IsFocused);

        popover.FocusLock = false;
        scene.Key(Key.Enter);
        Settle(scene);
        Assert.True(one.IsFocused);
        // Without the lock, Tab leaves the body.
        scene.Key(Key.Tab);
        Assert.True(two.IsFocused);
        scene.Key(Key.Tab);
        Assert.False(one.IsFocused);
        Assert.False(two.IsFocused);
        popover.IsOpen = false;
        Settle(scene, 100);
        Assert.False(popover.IsOpen);
    }

    // ---- Content ----

    // StatefulPopover's render prop: content given a close function.
    [AvaloniaFact]
    public void Content_that_is_a_function_is_given_a_close_that_closes_it()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        var dismiss = new MeteredButton { Content = "Dismiss" };
        popover.Content = new Func<Action, object?>(close =>
        {
            dismiss.Click += (_, _) => close();
            return dismiss;
        });
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.Same(dismiss, ((ContentPresenter)popover.Part(Popover.InnerPart)!).Content);
        scene.Click(dismiss);
        Assert.False(popover.IsOpen);
    }

    [AvaloniaFact]
    public void Content_takes_text_a_control_or_data_and_its_template_shown_by_Inner()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.Content = "hello world";
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        var inner = (ContentPresenter)popover.Part(Popover.InnerPart)!;
        Assert.Equal("hello world", Assert.IsType<TextBlock>(inner.Child).Text);

        popover.ContentTemplate = new FuncDataTemplate<int>((n, _) => new TextBlock { Text = $"#{n}" });
        popover.Content = 7;
        scene.Pump();
        Assert.Equal("#7", Assert.IsType<TextBlock>(inner.Child).Text);
        // The content inherits the popover's data context, as a logical child.
        popover.ContentTemplate = null;
        popover.DataContext = "data";
        var control = new TextBlock();
        popover.Content = control;
        scene.Pump();
        Assert.Equal("data", control.DataContext);
    }

    // ---- Controlled ----

    [AvaloniaFact]
    public void IsOpen_set_by_the_app_opens_and_closes_it_and_a_binding_follows_the_clicks()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        var source = new OpenSource();
        popover.Bind(Popover.IsOpenProperty, new Avalonia.Data.Binding(nameof(OpenSource.Open)) { Source = source });
        var opened = 0;
        var closed = 0;
        popover.Open += (_, _) => opened++;
        popover.Close += (_, _) => closed++;
        using var scene = Show(popover);
        source.Open = true;
        Settle(scene);
        Assert.True(Shown(popover));
        Press(scene, new Point(20, 20));
        Assert.False(source.Open);
        scene.Click(anchor);
        Assert.True(source.Open);
        Assert.Equal(2, opened);
        Assert.Equal(1, closed);
    }

    private sealed class OpenSource : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _open;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public bool Open
        {
            get => _open;
            set
            {
                if (_open == value) return;
                _open = value;
                PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Open)));
            }
        }
    }

    [AvaloniaFact]
    public void Opened_on_load_it_shows_once_in_a_window_and_goes_when_taken_out()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.IsOpen = true;
        var host = new Panel { Children = { popover } };
        using var scene = Show(host);
        Settle(scene);
        Assert.True(Shown(popover));
        host.Children.Remove(popover);
        scene.Pump();
        Assert.False(popover.PopupHost!.IsOpen);
    }

    // ---- Overrides ----

    [AvaloniaFact]
    public void Overrides_of_Body_Arrow_and_Inner_win_over_their_themes()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.ShowArrow = true;
        var red = new SolidColorBrush(Colors.Red);
        popover.Overrides =
        [
            new Override
            {
                Part = "Body",
                Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Border.BackgroundProperty, red), new Setter(Border.CornerRadiusProperty, new CornerRadius(2)) } },
            },
            new Override { Part = "Arrow", Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Border.BackgroundProperty, red) } } },
            new Override { Part = "Inner", Style = new ControlTheme(typeof(ContentPresenter)) { Setters = { new Setter(ContentPresenter.ForegroundProperty, red) } } },
        ];
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.Same(red, ((Border)popover.Part("Body")!).Background);
        Assert.Equal(new CornerRadius(2), ((Border)popover.Part("Body")!).CornerRadius);
        Assert.Same(red, ((Border)popover.Part("Arrow")!).Background);
        Assert.Same(red, ((ContentPresenter)popover.Part("Inner")!).Foreground);
        // The state style the theme has (its opacity while open) still runs under the override.
        Assert.Equal(1, Body(popover).Opacity);
    }

    // The values the popover gives a part (the body's margin) are below an override's, as Base Web's props are.
    [AvaloniaFact]
    public void An_override_of_the_body_margin_wins_and_places_it()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        popover.Overrides = [new Override { Part = "Body", Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, 30, 0, 0)) } } }];
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.Equal(new Rect(135, 470, 120, 60), BoxOf(Body(popover), scene));
    }

    // ---- Changes while it is shown ----

    // Any Avalonia button (a toggle button, a checkbox, a switch) raises Click, and its tap is not a second click.
    [AvaloniaFact]
    public void An_Avalonia_toggle_button_anchor_toggles_it_once()
    {
        var toggle = new Avalonia.Controls.Primitives.ToggleButton { Content = "Open", Width = 90, Height = 40 };
        var popover = new Popover { Anchor = toggle, Content = "content", HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(100) };
        using var scene = Show(popover);
        scene.Click(toggle);
        Settle(scene, 50);
        Assert.True(popover.IsOpen);
        Tap(scene, scene.CentreOf(toggle));
        Settle(scene, 50);
        Assert.False(popover.IsOpen);
    }

    [AvaloniaFact]
    public void An_arrow_shown_while_it_is_open_is_placed()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        popover.IsOpen = true;
        using var scene = Show(popover);
        Settle(scene);
        popover.ShowArrow = true;
        Settle(scene);
        var arrow = (Control)popover.Part(Popover.ArrowPart)!;
        // popper.js's arrow offset for this anchor and body: the anchor's centre (195) less half the arrow, from the body's left.
        Assert.Equal(195 - 4.5 - 135, arrow.Bounds.X, 0);
        Assert.Equal(new Rect(135, 400 + 40 + 14, 120, 60), BoxOf(Body(popover), scene));
    }

    // The arrow shown under an override's fixed margin: nothing the key holds changes, and it is placed all the same.
    [AvaloniaFact]
    public void An_arrow_shown_under_an_overrides_margin_is_placed()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        popover.Overrides = [new Override { Part = "Body", Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(0, 20, 0, 0)) } } }];
        popover.IsOpen = true;
        using var scene = Show(popover);
        Settle(scene);
        popover.ShowArrow = true;
        Settle(scene);
        var arrow = (Control)popover.Part(Popover.ArrowPart)!;
        Assert.Equal(195 - 4.5 - 135, arrow.Bounds.X, 0);
    }

    [AvaloniaFact]
    public void A_placement_changed_while_it_is_open_places_it_again()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        popover.IsOpen = true;
        using var scene = Show(popover);
        Settle(scene);
        popover.Placement = PopoverPlacement.Top;
        Settle(scene);
        Assert.Equal(PopoverPlacement.Top, popover.ActualPlacement);
        Assert.Equal(new Rect(135, 400 - 8 - 60, 120, 60), BoxOf(Body(popover), scene));
    }

    [AvaloniaFact]
    public void A_trigger_type_changed_while_it_is_open_takes_its_layers_handlers()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.TriggerType = PopoverTriggerType.Hover;
        popover.IsOpen = true;
        using var scene = Show(popover);
        Settle(scene);
        popover.TriggerType = PopoverTriggerType.Click;
        Press(scene, new Point(20, 20));
        Assert.False(popover.IsOpen);
    }

    [AvaloniaFact]
    public void A_new_anchor_takes_the_tooltips_description_from_the_old()
    {
        var first = new MeteredButton { Content = "One" };
        var second = new MeteredButton { Content = "Two" };
        var popover = new Popover { Anchor = first, Content = "hello world", AccessibilityType = PopoverAccessibilityType.Tooltip, IsOpen = true };
        using var scene = Show(new StackPanel { Margin = new Thickness(40), Children = { popover } });
        Settle(scene);
        Assert.Equal("hello world", AutomationProperties.GetHelpText(first));
        popover.Anchor = second;
        scene.Pump();
        Assert.Null(AutomationProperties.GetHelpText(first));
        Assert.Equal("hello world", AutomationProperties.GetHelpText(second));
    }

    // A tab not shown keeps its content in the logical tree, out of the visual one: its popover's popup closes.
    [AvaloniaFact]
    public void A_popover_in_a_tab_put_away_is_hidden_and_back_again_with_it()
    {
        var (popover, _, _) = Fixed(20, 100);
        popover.IsOpen = true;
        var tabs = new TabControl
        {
            ItemsSource = new[]
            {
                new TabItem { Header = "One", Content = popover },
                new TabItem { Header = "Two", Content = new TextBlock { Text = "two" } },
            },
        };
        using var scene = Show(tabs);
        Settle(scene);
        Assert.True(Shown(popover));
        tabs.SelectedIndex = 1;
        Settle(scene, 100);
        Assert.False(popover.PopupHost!.IsOpen);
        tabs.SelectedIndex = 0;
        Settle(scene);
        Assert.True(Shown(popover));
    }

    // A close its window's handler cancels leaves it shown.
    [AvaloniaFact]
    public void A_window_close_that_is_cancelled_leaves_it_shown()
    {
        var (popover, _, _) = Fixed(150, 400);
        popover.IsOpen = true;
        using var scene = Show(popover);
        Settle(scene);
        scene.Window.Closing += (_, e) => e.Cancel = true;
        scene.Window.Close();
        Settle(scene);
        Assert.True(popover.IsOpen);
        Assert.True(Shown(popover));
        Press(scene, new Point(20, 20));
        Assert.False(popover.IsOpen);
    }

    // A popup the content opens (a combo box's drop-down) is inside the body: a press in it closes nothing.
    [AvaloniaFact]
    public void A_press_in_a_popup_its_content_opened_is_inside()
    {
        var combo = new ComboBox { ItemsSource = new[] { "One", "Two", "Three" }, Width = 100 };
        var (popover, anchor, _) = Fixed(20, 100);
        popover.Content = new Border { Padding = new Thickness(12), Child = combo };
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        combo.IsDropDownOpen = true;
        Settle(scene, 50);
        var item = combo.ContainerFromIndex(2)!;
        Press(scene, scene.CentreOf(item));
        Assert.True(popover.IsOpen);
        Assert.Equal("Three", combo.SelectedItem);
    }

    // Base Web's onClick on the anchor sees every click: one its content handles too.
    [AvaloniaFact]
    public void An_anchor_that_handles_its_tap_still_opens_it()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        anchor.Tapped += (_, e) => e.Handled = true;
        using var scene = Show(popover);
        scene.Click(anchor);
        Assert.True(popover.IsOpen);
    }

    // react-focus-lock's first tab stop is Tab's first: TabIndex before the tree's order.
    [AvaloniaFact]
    public void Focus_goes_to_the_first_tab_stop_in_Tabs_order()
    {
        var later = new TextBox { TabIndex = 2 };
        var first = new TextBox { TabIndex = 1 };
        var (popover, anchor, _) = Fixed(150, 400);
        popover.Content = new StackPanel { Children = { later, first } };
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.True(first.IsFocused);
    }

    // ---- UI Automation ----

    // popover.js: a tooltip's body has role tooltip, and the anchor is described by it (aria-describedby).
    [AvaloniaFact]
    public void A_tooltip_is_a_tooltip_to_UI_Automation_and_describes_its_anchor()
    {
        var button = new MeteredButton { Content = "Open" };
        var popover = new Popover { Anchor = button, Content = "hello world", AccessibilityType = PopoverAccessibilityType.Tooltip };
        using var scene = Show(new StackPanel { Margin = new Thickness(40), Children = { popover } });
        scene.Click(button);
        Settle(scene);
        var body = (Control)popover.Part("Body")!;
        Assert.Equal(AutomationControlType.ToolTip, ControlAutomationPeer.CreatePeerForElement(body).GetAutomationControlType());
        Assert.Equal("hello world", AutomationProperties.GetHelpText(button));
        scene.Click(button);
        Settle(scene);
        Assert.Null(AutomationProperties.GetHelpText(button));

        popover.AccessibilityType = PopoverAccessibilityType.Menu;
        scene.Click(button);
        Settle(scene);
        Assert.NotEqual(AutomationControlType.ToolTip, ControlAutomationPeer.CreatePeerForElement(body).GetAutomationControlType());
        Assert.Null(AutomationProperties.GetHelpText(button));
    }

    // ---- Placement in a page ----

    [AvaloniaFact]
    public void It_follows_its_anchor_as_the_page_scrolls_and_stays_inside_the_scrollers_box()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        var page = new ScrollViewer { Content = new Panel { Height = 2000, Children = { popover } } };
        var bar = new Border { Height = 56 };
        DockPanel.SetDock(bar, Dock.Top);
        using var scene = Show(new DockPanel { Children = { bar, page } });
        scene.Click(anchor);
        Settle(scene);
        var before = BoxOf(Body(popover), scene);
        page.Offset = new Vector(0, 100);
        Settle(scene);
        Assert.Equal(before.Translate(new Vector(0, -100)), BoxOf(Body(popover), scene));

        // The anchor scrolled above the scroller: popper.js's preventOverflow keeps the body in its scroll parent's box.
        page.Offset = new Vector(0, 800);
        Settle(scene);
        Assert.Equal(56 + 8, BoxOf(Body(popover), scene).Top);
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
    }

    [AvaloniaFact]
    public void Near_the_foot_of_the_window_it_flips_above()
    {
        var (popover, anchor, _) = Fixed(150, 780);
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.Equal(PopoverPlacement.Top, popover.ActualPlacement);
        Assert.Contains(":placement-top", ((StyledElement)Body(popover)).Classes);
        Assert.Equal(new Rect(135, 780 - 8 - 60, 120, 60), BoxOf(Body(popover), scene));
        popover.IsOpen = false;
        Settle(scene, 100);
        // Base Web's state's placement goes back to the one asked for once it is gone.
        Assert.Equal(PopoverPlacement.Bottom, popover.ActualPlacement);
    }
}
