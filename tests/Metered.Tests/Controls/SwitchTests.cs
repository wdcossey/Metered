using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A switch's own behaviour (switch/switch.js and its tests, switch/__tests__/switch.browser.test.tsx): toggling by
/// click, Space and Enter, its parts and their state, hover, press and focus, its label, the coarse pointer, the
/// transition, its overrides, and the pixels its knob and track draw.
/// </summary>
public class SwitchTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    private static (Switch Switch, Scene Scene) Make(Switch sw) =>
        (sw, new Scene(new StackPanel { Margin = new Thickness(20), Children = { sw } }));

    // switch.browser.test.tsx: 'updates checked state on click'.
    [AvaloniaFact]
    public void A_click_toggles_it()
    {
        var (sw, scene) = Make(new Switch { Content = "Label 1" });
        using var _ = scene;
        scene.Click(sw);
        Assert.True(sw.IsChecked);
        Assert.Contains(":checked", sw.Part(Switch.TogglePart)!.Classes);
        scene.Click((Visual)sw.Part(Switch.LabelPart)!);
        Assert.False(sw.IsChecked);
        Assert.DoesNotContain(":checked", sw.Part(Switch.ToggleTrackPart)!.Classes);
    }

    // switch.browser.test.tsx: 'updates checked state on change(keyboard)': Space, then Enter, each toggles. Enter on its
    // release (Base Web's onKeyUp), not on its press.
    [AvaloniaFact]
    public void Space_and_Enter_toggle_it_Enter_on_its_release()
    {
        var (sw, scene) = Make(new Switch { Content = "Label 2" });
        using var _ = scene;
        scene.Key(Key.Tab);
        Assert.True(sw.IsFocused);
        scene.Key(Key.Space);
        Assert.True(sw.IsChecked);

        scene.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        scene.Pump();
        Assert.True(sw.IsChecked);
        Assert.Contains(":is-active", sw.Part(Switch.ToggleTrackPart)!.Classes);
        scene.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, null);
        scene.Pump();
        Assert.False(sw.IsChecked);
        Assert.DoesNotContain(":is-active", sw.Classes);

        scene.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        scene.Pump();
        Assert.Contains(":is-active", sw.Part(Switch.TogglePart)!.Classes);
        scene.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        scene.Pump();
        Assert.True(sw.IsChecked);
        Assert.DoesNotContain(":is-active", sw.Classes);
    }

    [AvaloniaFact]
    public void Every_part_takes_the_switchs_state()
    {
        var sw = new Switch { Content = "Label", IsChecked = true, Size = SwitchSize.Small, LabelPlacement = SwitchLabelPlacement.Left, ShowIcon = true };
        using var scene = new Scene(sw);
        foreach (var name in new[] { Switch.RootPart, Switch.ToggleTrackPart, Switch.TogglePart, Switch.LabelPart })
        {
            var part = sw.Part(name)!;
            Assert.Contains(":checked", part.Classes);
            Assert.Contains(":size-small", part.Classes);
            Assert.DoesNotContain(":size-default", part.Classes);
            Assert.Contains(":label-placement-left", part.Classes);
            Assert.DoesNotContain(":label-placement-right", part.Classes);
            Assert.Contains(":show-icon", part.Classes);
            Assert.Contains(":loaded", part.Classes);
        }

        sw.IsChecked = false;
        sw.Size = SwitchSize.Default;
        sw.LabelPlacement = SwitchLabelPlacement.Right;
        sw.ShowIcon = false;
        var toggle = sw.Part(Switch.TogglePart)!;
        Assert.DoesNotContain(":checked", toggle.Classes);
        Assert.Contains(":size-default", toggle.Classes);
        Assert.Contains(":label-placement-right", toggle.Classes);
        Assert.DoesNotContain(":show-icon", toggle.Classes);
    }

    // Base Web's onMouseEnter and onMouseLeave on the root: the whole switch, label included; leaving ends a press too.
    [AvaloniaFact]
    public void A_mouse_hovers_it_and_leaving_ends_the_hover_and_the_press()
    {
        var (sw, scene) = Make(new Switch { Content = "Label" });
        using var _ = scene;
        scene.Hover((Visual)sw.Part(Switch.LabelPart)!);
        Assert.Contains(":is-hovered", sw.Part(Switch.ToggleTrackPart)!.Classes);

        scene.Press((Visual)sw.Part(Switch.ToggleTrackPart)!);
        Assert.Contains(":is-active", sw.Part(Switch.TogglePart)!.Classes);
        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", sw.Classes);
        Assert.DoesNotContain(":is-active", sw.Classes);
        Assert.DoesNotContain(":is-hovered", sw.Part(Switch.ToggleTrackPart)!.Classes);
    }

    // Metered's hover is a mouse's (ADR 0001); a press is any pointer's, and ends on the release.
    [AvaloniaFact]
    public void A_finger_presses_but_never_hovers()
    {
        var (sw, scene) = Make(new Switch { Content = "Label" });
        using var _ = scene;
        var track = (Control)sw.Part(Switch.ToggleTrackPart)!;
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        sw.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, sw, touch, scene.Window,
            scene.CentreOf(track), 0, new PointerPointProperties(), KeyModifiers.None));
        sw.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, sw, touch, scene.Window,
            scene.CentreOf(track), 0, new PointerPointProperties(), KeyModifiers.None));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", sw.Classes);

        RadioColourTests.Touch(scene, track);
        Assert.Contains(":is-active", sw.Classes);
        Assert.DoesNotContain(":is-hovered", sw.Classes);
        scene.Click(sw);
        Assert.DoesNotContain(":is-active", sw.Classes);
    }

    // Taken out of the window while hovered, pressed and with a key held, the switch sees none of them end: put back,
    // it has none of them (Avalonia ends them on the way out, as on disabling).
    [AvaloniaFact]
    public void Taken_out_and_put_back_it_has_no_stale_hover_or_press()
    {
        var sw = new Switch { Content = "Label" };
        var panel = new StackPanel { Margin = new Thickness(20), Children = { sw } };
        using var scene = new Scene(panel);
        scene.Key(Key.Tab);
        scene.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        scene.Press(sw);
        Assert.Contains(":is-hovered", sw.Classes);
        Assert.Contains(":is-active", sw.Classes);

        panel.Children.Remove(sw);
        scene.Pump();
        scene.Window.MouseUp(new Point(Scene.Width - 1, Scene.Height - 1), MouseButton.Left);
        scene.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Pump();
        panel.Children.Add(sw);
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", sw.Classes);
        Assert.DoesNotContain(":is-active", sw.Classes);
        Assert.DoesNotContain(":is-active", sw.Part(Switch.TogglePart)!.Classes);
    }

    // Base Web's switch is on or off: an app's IsThreeState doesn't give it a third state.
    [AvaloniaFact]
    public void It_has_no_third_state()
    {
        var (sw, scene) = Make(new Switch { Content = "Label", IsThreeState = true });
        using var _ = scene;
        Assert.False(sw.IsThreeState);
        scene.Click(sw);
        Assert.True(sw.IsChecked);
        scene.Click(sw);
        Assert.False(sw.IsChecked);
    }

    // A right press is Base Web's onMouseDown too, and ends when it is let go.
    [AvaloniaFact]
    public void Any_buttons_press_is_active_until_released()
    {
        var (sw, scene) = Make(new Switch { Content = "Label" });
        using var _ = scene;
        var at = scene.CentreOf(sw);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Pump();
        Assert.Contains(":is-active", sw.Classes);
        scene.Window.MouseUp(at, MouseButton.Right);
        scene.Pump();
        Assert.DoesNotContain(":is-active", sw.Classes);
        Assert.False(sw.IsChecked);
    }

    // Base Web's onFocus: the outline only when the focus came by keyboard.
    [AvaloniaFact]
    public void Keyboard_focus_draws_the_outline_and_a_click_doesnt()
    {
        var sw = new Switch { Content = "Label" };
        var other = new Avalonia.Controls.Button { Content = "Other" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { sw, other } });
        var track = (RadioMark)sw.Part(Switch.ToggleTrackPart)!;
        var focus = SwitchOracle.Data.Colours.First(c => c.State == "focusVisible").Outline!;

        scene.Click(sw);
        Assert.True(sw.IsFocused);
        Assert.DoesNotContain(":focus-visible", track.Classes);
        Assert.Equal(0, track.OutlineThickness);

        // From the last control, Tab comes round to the switch.
        other.Focus();
        scene.Pump();
        scene.Key(Key.Tab);
        Assert.True(sw.IsFocused);
        Assert.Contains(":focus-visible", track.Classes);
        Assert.Equal(focus.Thickness, track.OutlineThickness);

        other.Focus();
        scene.Pump();
        Assert.DoesNotContain(":focus-visible", track.Classes);
    }

    // Base Web's onFocus is the hidden input's: a control in the label taking the keyboard's focus isn't the switch's.
    [AvaloniaFact]
    public void Focus_on_a_control_in_the_label_isnt_the_switchs()
    {
        var inner = new TextBox { Text = "Name" };
        var (sw, scene) = Make(new Switch { Content = inner });
        using var _ = scene;
        inner.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(inner.IsFocused);
        Assert.DoesNotContain(":focus-visible", sw.Classes);
        Assert.Equal(0, ((RadioMark)sw.Part(Switch.ToggleTrackPart)!).OutlineThickness);
    }

    // switch.js renders the label only with children (children && ...): not for none or an empty string.
    [AvaloniaFact]
    public void The_label_is_there_only_with_content()
    {
        var bare = new Switch();
        var labelled = new Switch { Content = "Label" };
        using var scene = new Scene(new StackPanel { Children = { bare, labelled } });
        var track = (Visual)bare.Part(Switch.ToggleTrackPart)!;
        Assert.False(((Visual)bare.Part(Switch.LabelPart)!).IsVisible);
        Assert.Equal(track.Bounds.Size, bare.Bounds.Size);
        Assert.True(((Visual)labelled.Part(Switch.LabelPart)!).IsVisible);

        labelled.Content = "";
        scene.Pump();
        Assert.False(((Visual)labelled.Part(Switch.LabelPart)!).IsVisible);
        Assert.Equal(track.Bounds.Size, labelled.Bounds.Size);
        labelled.Content = "Back";
        scene.Pump();
        Assert.True(((Visual)labelled.Part(Switch.LabelPart)!).IsVisible);
    }

    // A disabled switch takes no pointer and no key, so it never sees the mouse leave or the key go up: its hover and
    // press end when it is disabled (Avalonia's pointer exit, capture loss and focus loss, which the switch follows).
    [AvaloniaFact]
    public void Disabling_it_ends_its_hover_and_press()
    {
        var (sw, scene) = Make(new Switch { Content = "Label" });
        using var _ = scene;
        // A hover alone, the mouse left where it is.
        scene.Hover(sw);
        Assert.Contains(":is-hovered", sw.Classes);
        sw.IsEnabled = false;
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", sw.Classes);
        Assert.DoesNotContain(":is-hovered", sw.Part(Switch.ToggleTrackPart)!.Classes);
        sw.IsEnabled = true;
        scene.Pump();

        // A held key.
        scene.Key(Key.Tab);
        scene.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        scene.Pump();
        Assert.Contains(":is-active", sw.Classes);
        sw.IsEnabled = false;
        scene.Pump();
        Assert.DoesNotContain(":is-active", sw.Classes);
        scene.Window.KeyRelease(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        sw.IsEnabled = true;
        scene.Pump();
        Assert.DoesNotContain(":is-active", sw.Classes);

        scene.Press(sw);
        Assert.Contains(":is-hovered", sw.Classes);
        Assert.Contains(":is-active", sw.Classes);
        sw.IsEnabled = false;
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", sw.Classes);
        Assert.DoesNotContain(":is-active", sw.Classes);

        // Still holding the press it captured, the disabled switch sees the mouse move over it: no hover comes back.
        scene.Window.MouseMove(scene.CentreOf(sw) + new Point(1, 1));
        scene.Window.MouseUp(scene.CentreOf(sw), MouseButton.Left);
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", sw.Classes);
        Assert.False(sw.IsChecked);
    }

    // Base Web's @media (pointer: coarse): padding round the root (8, or 12 small) for a 48 px target, which takes a tap.
    [AvaloniaTheory]
    [InlineData(SwitchSize.Default)]
    [InlineData(SwitchSize.Small)]
    public void A_coarse_pointer_pads_the_root_and_the_padding_takes_a_tap(SwitchSize size)
    {
        Assert.Equal(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS(), new Switch().CoarsePointer);
        var (sw, scene) = Make(new Switch { Size = size, CoarsePointer = true });
        using var _ = scene;
        var track = (Visual)sw.Part(Switch.ToggleTrackPart)!;
        var padding = SwitchOracle.Data.Layout.First(c => c.Coarse && c.Size == size.ToString().ToLowerInvariant()).RootPadding;
        Assert.Equal(track.Bounds.Height + 2 * padding, sw.Bounds.Height);
        Assert.Equal(48, sw.Bounds.Height);

        var above = track.TranslatePoint(new Point(track.Bounds.Width / 2, -padding / 2), scene.Window)!.Value;
        scene.Window.MouseDown(above, MouseButton.Left);
        scene.Window.MouseUp(above, MouseButton.Left);
        scene.Pump();
        Assert.True(sw.IsChecked);

        sw.CoarsePointer = false;
        scene.Pump();
        Assert.Equal(track.Bounds.Height, sw.Bounds.Height);
    }

    // transition: transform 350ms on Base Web's curve; not on the first paint, as CSS's.
    [AvaloniaFact]
    public void The_knob_slides_once_loaded()
    {
        var sw = new Switch();
        var toggle = () => (SwitchToggle)sw.Part(Switch.TogglePart)!;
        using var scene = new Scene();
        scene.Window.Content = sw;
        sw.ApplyTemplate();
        Assert.Null(toggle().Transitions);

        scene.Pump();
        var t = SwitchOracle.Data.Transition;
        Assert.Equal("transform", t.Property);
        var transition = Assert.IsType<TransformOperationsTransition>(Assert.Single(toggle().Transitions!));
        Assert.Equal(Visual.RenderTransformProperty, transition.Property);
        Assert.Equal(TimeSpan.FromSeconds(t.Seconds), transition.Duration);
        var curve = Assert.IsType<SplineEasing>(transition.Easing);
        Assert.Equal(t.Curve, new[] { curve.X1, curve.Y1, curve.X2, curve.Y2 });

        // Checked, the knob moves to where Base Web's translateX puts it, and is there once the transition is over.
        var on = SwitchOracle.Data.Layout.First(c => c.Size == "default" && c.Checked && !c.Disabled && !c.Coarse);
        sw.IsChecked = true;
        scene.Pump();
        Assert.Equal(on.ToggleTranslate, toggle().GetBaseValue(Visual.RenderTransformProperty).GetValueOrDefault()!.Value.M31);
    }

    // ADR 0001: an override's plain setter wins over the part theme's state styles, as Base Web's deep merge does.
    [AvaloniaFact]
    public void An_override_wins_over_the_parts_state_styles()
    {
        var style = new ControlTheme(typeof(SwitchToggle));
        style.Setters.Add(new Setter(RadioMark.BackgroundProperty, Red));
        var sw = new Switch { IsChecked = true, Overrides = [new Override { Part = Switch.TogglePart, Style = style }] };
        using var scene = new Scene(sw);
        var toggle = (SwitchToggle)sw.Part(Switch.TogglePart)!;
        Assert.Same(Red, toggle.Background);
        // The rest of Base Web's style is still there: the checked knob's size.
        var on = SwitchOracle.Data.Layout.First(c => c.Size == "default" && c.Checked && !c.Disabled);
        Assert.Equal(on.Toggle, toggle.Width);

        sw.Overrides = null;
        scene.Pump();
        var rest = SwitchOracle.Data.Colours.First(c => c.Theme == "Light" && c.Checked && !c.Disabled && c.State == "rest");
        Assert.Equal(rest.ToggleBackground, SwitchColourTests.Colour(toggle.Background));
    }

    [AvaloniaTheory]
    [InlineData(Switch.RootPart, typeof(RadioRootPanel))]
    [InlineData(Switch.ToggleTrackPart, typeof(RadioMark))]
    [InlineData(Switch.TogglePart, typeof(SwitchToggle))]
    [InlineData(Switch.LabelPart, typeof(Border))]
    public void Every_part_takes_an_override(string part, Type type)
    {
        var style = new ControlTheme(type);
        style.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        var sw = new Switch { Content = "Label", Overrides = [new Override { Part = part, Style = style }] };
        using var scene = new Scene(sw);
        Assert.Equal(0.5, ((Visual)sw.Part(part)!).Opacity);
    }

    // The pixels Avalonia draws for what a border can't: the checkmark, the track's outline beyond its gap, the overlay.
    [AvaloniaFact]
    public void The_knob_draws_the_checkmark_and_the_track_its_outline()
    {
        var c = SwitchOracle.Data.Colours.First(x => x.Theme == "Light" && x.Checked && x.ShowIcon && x.State == "focusVisible");
        var (sw, scene) = Make(new Switch { IsChecked = true, ShowIcon = true });
        using var _ = scene;
        scene.Key(Key.Tab);
        var frame = scene.Capture();
        var toggle = (Visual)sw.Part(Switch.TogglePart)!;
        var track = (Visual)sw.Part(Switch.ToggleTrackPart)!;
        Color At(Visual v, double x, double y)
        {
            var p = v.TranslatePoint(new Point(x, y), scene.Window)!.Value;
            return Frames.PixelAt(frame, (int)Math.Floor(p.X), (int)Math.Floor(p.Y));
        }

        // The 16 px mark in the middle of the 24 px knob: (8.5, 17.7) of its 24 px box is inside its stroke, (15, 19.5)
        // below it.
        var scale = c.Icon!.Size / SwitchToggle.IconViewBox;
        var origin = (toggle.Bounds.Width - c.Icon.Size) / 2;
        Assert.Equal(Color.Parse(c.Icon.Colour), At(toggle, origin + 8.5 * scale, origin + 17.7 * scale));
        Assert.Equal(Color.Parse(c.ToggleBackground), At(toggle, origin + 15 * scale, origin + 19.5 * scale));

        // Above the track's straight top edge: the gap, the page, then the outline.
        var page = Frames.PixelAt(frame, (int)Scene.Width - 2, (int)Scene.Height - 2);
        var mid = track.Bounds.Width / 2;
        Assert.Equal(page, At(track, mid, -c.Outline!.Offset + 0.5));
        Assert.Equal(Color.Parse(c.Outline.Colour), At(track, mid, -c.Outline.Offset - c.Outline.Thickness / 2));
        Assert.Equal(Color.Parse(c.TrackBackground), At(track, mid, 1));
    }

    // An unchecked track under the mouse: its fill under the hover overlay, blended as the overlay's alpha says.
    [AvaloniaFact]
    public void The_track_draws_the_hover_overlay_over_its_fill()
    {
        var c = SwitchOracle.Data.Colours.First(x => x.Theme == "Light" && !x.Checked && !x.Disabled && !x.ShowIcon && x.State == "hover");
        var (sw, scene) = Make(new Switch());
        using var _ = scene;
        scene.Hover(sw);
        var frame = scene.Capture();
        var track = (Visual)sw.Part(Switch.ToggleTrackPart)!;
        // Inside the border, right of the knob.
        var p = track.TranslatePoint(new Point(track.Bounds.Width - 12, track.Bounds.Height / 2), scene.Window)!.Value;
        var pixel = Frames.PixelAt(frame, (int)p.X, (int)p.Y);
        var overlay = Color.Parse(c.TrackOverlay!);
        var under = Color.Parse(c.TrackBackground);
        var a = overlay.A / 255.0;
        int Blend(byte top, byte bottom) => (int)Math.Round(top * a + bottom * (1 - a));
        Assert.InRange(pixel.R, Blend(overlay.R, under.R) - 1, Blend(overlay.R, under.R) + 1);
        Assert.InRange(pixel.G, Blend(overlay.G, under.G) - 1, Blend(overlay.G, under.G) + 1);
        Assert.InRange(pixel.B, Blend(overlay.B, under.B) - 1, Blend(overlay.B, under.B) + 1);
        Assert.NotEqual(under, pixel);
    }
}
