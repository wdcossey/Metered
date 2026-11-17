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
/// A radio's own behaviour (radio-v2/radio.js and its tests, radio-v2/__tests__/radio.test.tsx): its parts and their
/// state, its label and description, hover and press, the transition, its overrides, and the pixels its mark draws.
/// </summary>
public class RadioTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    [AvaloniaFact]
    public void A_click_checks_a_radio_on_its_own()
    {
        var radio = new Radio { Content = "Label" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        scene.Click(radio);
        Assert.True(radio.IsChecked);
        Assert.Contains(":checked", radio.Part(Radio.RadioMarkInnerPart)!.Classes);
    }

    [AvaloniaFact]
    public void Every_part_takes_the_radios_state()
    {
        var radio = new Radio { Content = "Label", Description = "Description", IsChecked = true, Error = true, Align = RadioAlign.Horizontal, LabelPlacement = RadioLabelPlacement.Top };
        using var scene = new Scene(radio);
        foreach (var name in new[] { "Root", "RadioBackplate", "RadioMarkOuter", "RadioMarkInner", "LabelWrapper", "Label", "Description" })
        {
            var part = radio.Part(name)!;
            Assert.Contains(":checked", part.Classes);
            Assert.Contains(":error", part.Classes);
            Assert.Contains(":align-horizontal", part.Classes);
            Assert.Contains(":label-placement-top", part.Classes);
            Assert.Contains(":loaded", part.Classes);
        }

        radio.IsChecked = false;
        radio.LabelPlacement = RadioLabelPlacement.Left;
        Assert.DoesNotContain(":checked", radio.Part("RadioMarkInner")!.Classes);
        Assert.Contains(":label-placement-left", radio.Part("Label")!.Classes);
        Assert.DoesNotContain(":label-placement-top", radio.Part("Label")!.Classes);
    }

    // Base Web: $isHovered is the whole radio's (every part), the backplate's CSS :hover its own.
    [AvaloniaFact]
    public void A_mouse_over_the_label_hovers_the_radio_but_not_the_backplate()
    {
        var radio = new Radio { Content = "Label" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        var backplate = radio.Part(Radio.RadioBackplatePart)!;

        scene.Hover((Visual)radio.Part(Radio.LabelPart)!);
        Assert.Contains(":is-hovered", radio.Part(Radio.RadioMarkInnerPart)!.Classes);
        Assert.DoesNotContain(":hover", backplate.Classes);

        scene.Hover((Visual)backplate);
        Assert.Contains(":hover", backplate.Classes);
        Assert.DoesNotContain(":hover", radio.Part(Radio.RadioMarkInnerPart)!.Classes);

        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", radio.Classes);
        Assert.DoesNotContain(":hover", backplate.Classes);
    }

    // The backplate's hover is inside @media (hover: hover): a finger doesn't hover. A press is any pointer's, and ends.
    [AvaloniaFact]
    public void A_finger_presses_but_never_hovers()
    {
        var radio = new Radio { Content = "Label" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        var backplate = radio.Part(Radio.RadioBackplatePart)!;
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        radio.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, radio, touch, scene.Window,
            scene.CentreOf((Visual)backplate), 0, new PointerPointProperties(), KeyModifiers.None));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", radio.Classes);
        Assert.DoesNotContain(":hover", backplate.Classes);

        RadioColourTests.Touch(scene, (Control)backplate);
        Assert.Contains(":is-active", radio.Classes);
        Assert.Contains(":active", backplate.Classes);

        scene.Click(radio);
        Assert.DoesNotContain(":is-active", radio.Classes);
        Assert.DoesNotContain(":active", backplate.Classes);
    }

    // Base Web: a standalone radio's isFocusVisible comes from its app; only a group sets it.
    [AvaloniaFact]
    public void A_radio_on_its_own_draws_no_outline_on_keyboard_focus()
    {
        var radio = new Radio { Content = "Label", IsChecked = true };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        scene.Key(Key.Tab);
        Assert.True(radio.IsFocused);
        Assert.DoesNotContain(":focus-visible", radio.Classes);
        Assert.Equal(0, ((RadioMark)radio.Part(Radio.RadioMarkOuterPart)!).OutlineThickness);
    }

    // Base Web renders the label wrapper only with children, and the description only when there is one.
    [AvaloniaFact]
    public void The_label_and_description_are_there_only_when_given()
    {
        var bare = new Radio();
        var labelled = new Radio { Content = "Label" };
        var described = new Radio { Content = "Label", Description = "Description" };
        using var scene = new Scene(new StackPanel { Children = { bare, labelled, described } });
        var backplate = (Visual)bare.Part(Radio.RadioBackplatePart)!;

        Assert.False(((Visual)bare.Part(Radio.LabelWrapperPart)!).IsVisible);
        Assert.Equal(backplate.Bounds.Size, bare.Bounds.Size);
        Assert.True(((Visual)labelled.Part(Radio.LabelWrapperPart)!).IsVisible);
        Assert.False(((Visual)labelled.Part(Radio.DescriptionPart)!).IsVisible);
        Assert.True(((Visual)described.Part(Radio.DescriptionPart)!).IsVisible);

        described.Description = "";
        scene.Pump();
        Assert.False(((Visual)described.Part(Radio.DescriptionPart)!).IsVisible);

        // !!children: an empty string is no label either.
        labelled.Content = "";
        scene.Pump();
        Assert.False(((Visual)labelled.Part(Radio.LabelWrapperPart)!).IsVisible);
        Assert.Equal(backplate.Bounds.Size, labelled.Bounds.Size);
        labelled.Content = "Back";
        scene.Pump();
        Assert.True(((Visual)labelled.Part(Radio.LabelWrapperPart)!).IsVisible);
    }

    // Base Web's onFocus is the hidden input's: a control in the label taking the keyboard's focus isn't the radio's.
    [AvaloniaFact]
    public void Focus_on_a_control_in_the_label_isnt_the_radios()
    {
        var inner = new Avalonia.Controls.Button { Content = "Pick a colour" };
        var radio = new Radio { Value = "1", Content = inner };
        var group = new RadioGroup { Value = "1", Items = { radio } };
        using var scene = new Scene(group);
        inner.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(inner.IsFocused);
        Assert.DoesNotContain(":focus-visible", radio.Classes);
        Assert.Equal(0, ((RadioMark)radio.Part(Radio.RadioMarkOuterPart)!).OutlineThickness);
    }

    // A press captures the mouse; moved off the radio, it no longer hovers it (Base Web's mouseleave).
    [AvaloniaFact]
    public void A_pressed_mouse_moved_off_the_radio_stops_hovering_it()
    {
        var radio = new Radio { Content = "Label" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        scene.Press(radio);
        Assert.Contains(":is-hovered", radio.Classes);
        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", radio.Classes);
        Assert.DoesNotContain(":hover", radio.Part(Radio.RadioBackplatePart)!.Classes);
    }

    // CSS's :active is the primary button's; Base Web's $isActive is onMouseDown's, any button.
    [AvaloniaFact]
    public void A_right_press_is_the_radios_but_not_the_backplates()
    {
        var radio = new Radio { Content = "Label" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        var backplate = radio.Part(Radio.RadioBackplatePart)!;
        var at = scene.CentreOf((Visual)backplate);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Pump();
        Assert.Contains(":is-active", radio.Classes);
        Assert.DoesNotContain(":active", backplate.Classes);
        scene.Window.MouseUp(at, MouseButton.Right);
        scene.Pump();
        Assert.DoesNotContain(":is-active", radio.Classes);

        // Released off the radio, the press still ends: Avalonia captures a right press too.
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Pump();
        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Window.MouseUp(new Point(Scene.Width - 1, Scene.Height - 1), MouseButton.Right);
        scene.Pump();
        Assert.DoesNotContain(":is-active", radio.Classes);
    }

    // A disabled radio takes no pointer, so it can't see the mouse leave: its hover and press end when it is disabled.
    [AvaloniaFact]
    public void Disabling_a_radio_ends_its_hover_and_press()
    {
        var radio = new Radio { Content = "Label" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        var backplate = radio.Part(Radio.RadioBackplatePart)!;
        scene.Hover((Visual)backplate);
        Assert.Contains(":is-hovered", radio.Classes);
        Assert.Contains(":hover", backplate.Classes);

        radio.IsEnabled = false;
        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Pump();
        radio.IsEnabled = true;
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", radio.Classes);
        Assert.DoesNotContain(":hover", backplate.Classes);

        scene.Press((Visual)backplate);
        Assert.Contains(":is-active", radio.Classes);
        Assert.Contains(":active", backplate.Classes);
        radio.IsEnabled = false;
        scene.Pump();
        Assert.DoesNotContain(":is-active", radio.Classes);
        Assert.DoesNotContain(":active", backplate.Classes);

        // Still holding the press it captured, the disabled radio sees the mouse move over it: no hover comes back.
        scene.Window.MouseMove(scene.CentreOf((Visual)backplate) + new Point(1, 1));
        scene.Window.MouseUp(scene.CentreOf((Visual)backplate), MouseButton.Left);
        scene.Window.MouseMove(new Point(Scene.Width - 1, Scene.Height - 1));
        scene.Pump();
        radio.IsEnabled = true;
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", radio.Classes);
        Assert.DoesNotContain(":hover", backplate.Classes);
    }

    // Base Web's @media (pointer: coarse): padding round the root, which takes the pointer, for a 48 px target.
    [AvaloniaFact]
    public void A_coarse_pointer_pads_the_root_and_the_padding_takes_a_tap()
    {
        Assert.Equal(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS(), new Radio().CoarsePointer);
        var radio = new Radio { CoarsePointer = true };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(40), Children = { radio } });
        var backplate = (Visual)radio.Part(Radio.RadioBackplatePart)!;
        var padding = RadioOracle.Data.Layout.First(c => c.Coarse).RootPadding;
        Assert.Equal(backplate.Bounds.Height + 2 * padding, radio.Bounds.Height);
        Assert.True(radio.Bounds.Height >= 48, $"{radio.Bounds.Height}");

        // A tap in the padding, above the backplate.
        var above = backplate.TranslatePoint(new Point(backplate.Bounds.Width / 2, -padding / 2), scene.Window)!.Value;
        scene.Window.MouseDown(above, MouseButton.Left);
        scene.Window.MouseUp(above, MouseButton.Left);
        scene.Pump();
        Assert.True(radio.IsChecked);

        radio.CoarsePointer = false;
        scene.Pump();
        Assert.Equal(backplate.Bounds.Height, radio.Bounds.Height);
    }

    // transition: width and height, timing200 on easeOutCurve; not on the first paint, as CSS's.
    [AvaloniaFact]
    public void The_inner_disc_moves_between_sizes_once_loaded()
    {
        var radio = new Radio();
        var inner = () => (RadioMark)radio.Part(Radio.RadioMarkInnerPart)!;
        using var scene = new Scene();
        // In the window, its template applied, not yet loaded: no transition.
        scene.Window.Content = radio;
        radio.ApplyTemplate();
        Assert.NotNull(radio.Part(Radio.RadioMarkInnerPart));
        Assert.Null(inner().Transitions);

        scene.Pump();
        var t = RadioOracle.Data.Transition;
        var transitions = inner().Transitions!.OfType<DoubleTransition>().ToArray();
        Assert.Equal(t.Properties, transitions.Select(x => x.Property!.Name.ToLowerInvariant()));
        Assert.All(transitions, x =>
        {
            Assert.Equal(TimeSpan.FromSeconds(t.Seconds), x.Duration);
            var curve = Assert.IsType<SplineEasing>(x.Easing);
            Assert.Equal(t.Curve, new[] { curve.X1, curve.Y1, curve.X2, curve.Y2 });
        });
    }

    // ADR 0001: an override's plain setter wins over the part theme's state styles, as Base Web's deep merge does.
    [AvaloniaFact]
    public void An_override_wins_over_the_parts_state_styles()
    {
        var style = new ControlTheme(typeof(RadioMark));
        style.Setters.Add(new Setter(RadioMark.BackgroundProperty, Red));
        var radio = new Radio { Error = true, Overrides = [new Override { Part = Radio.RadioMarkOuterPart, Style = style }] };
        using var scene = new Scene(radio);
        var outer = (RadioMark)radio.Part(Radio.RadioMarkOuterPart)!;
        Assert.Same(Red, outer.Background);
        // The rest of Base Web's style is still there: the error border.
        var error = RadioOracle.Data.Colours.First(c => c.Theme == "Light" && c.Error && !c.Disabled && c.State == "rest");
        Assert.Equal(error.OuterBorder, RadioColourTests.Colour(outer.BorderBrush));

        radio.Overrides = null;
        scene.Pump();
        Assert.Equal(error.OuterBackground, RadioColourTests.Colour(outer.Background));
    }

    [AvaloniaTheory]
    [InlineData(Radio.RootPart, typeof(RadioRootPanel))]
    [InlineData(Radio.RadioBackplatePart, typeof(Border))]
    [InlineData(Radio.LabelWrapperPart, typeof(StackPanel))]
    [InlineData(Radio.LabelPart, typeof(Border))]
    [InlineData(Radio.DescriptionPart, typeof(TextBlock))]
    [InlineData(Radio.RadioMarkInnerPart, typeof(RadioMark))]
    public void Every_part_takes_an_override(string part, Type type)
    {
        var style = new ControlTheme(type);
        style.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        var radio = new Radio { Content = "Label", Description = "Description", Overrides = [new Override { Part = part, Style = style }] };
        using var scene = new Scene(radio);
        Assert.Equal(0.5, ((Visual)radio.Part(part)!).Opacity);
    }

    // The pixels Avalonia draws for what a border can't: the 5 px dot, the outline beyond its 2 px gap, the overlay.
    [AvaloniaFact]
    public void The_mark_draws_the_dot_the_outline_and_the_overlay()
    {
        var focus = RadioOracle.Data.Colours.First(c => c.Theme == "Light" && c.Checked && !c.Error && c.State == "focusVisible");
        var (group, radios) = RadioGroupTests.Group("1", ["One", "Two"]);
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { group } });
        scene.Key(Key.Tab);
        var outer = (Visual)radios[0].Part(Radio.RadioMarkOuterPart)!;
        var frame = scene.Capture();
        var centre = outer.TranslatePoint(new Point(outer.Bounds.Width / 2, outer.Bounds.Height / 2), scene.Window)!.Value;
        Color At(double dx) => Frames.PixelAt(frame, (int)Math.Floor(centre.X + dx), (int)Math.Floor(centre.Y));

        Assert.Equal(Color.Parse(focus.InnerBackground), At(0));     // the dot
        Assert.Equal(Color.Parse(focus.OuterBackground), At(5));     // the disc round it
        var page = Frames.PixelAt(frame, (int)Scene.Width - 2, (int)Scene.Height - 2);
        Assert.Equal(page, At(9));                                   // the outline's gap: the page, as far from any control
        Assert.Equal(Color.Parse(focus.Outline!.Colour), At(11));    // the outline

        // An unchecked radio under the mouse: its inner disc under the hover overlay, blended as the overlay's alpha says.
        var hover = RadioOracle.Data.Colours.First(c => c.Theme == "Light" && !c.Checked && !c.Error && !c.Disabled && c.State == "hoverLabel");
        scene.Hover((Visual)radios[1].Part(Radio.LabelPart)!);
        frame = scene.Capture();
        var inner = (Visual)radios[1].Part(Radio.RadioMarkInnerPart)!;
        var c = inner.TranslatePoint(new Point(inner.Bounds.Width / 2, inner.Bounds.Height / 2), scene.Window)!.Value;
        var overlay = Color.Parse(hover.InnerOverlay!);
        var under = Color.Parse(hover.InnerBackground);
        var a = overlay.A / 255.0;
        int Blend(byte top, byte bottom) => (int)Math.Round(top * a + bottom * (1 - a));
        var pixel = Frames.PixelAt(frame, (int)c.X, (int)c.Y);
        Assert.InRange(pixel.R, Blend(overlay.R, under.R) - 1, Blend(overlay.R, under.R) + 1);
        Assert.InRange(pixel.G, Blend(overlay.G, under.G) - 1, Blend(overlay.G, under.G) + 1);
        Assert.InRange(pixel.B, Blend(overlay.B, under.B) - 1, Blend(overlay.B, under.B) + 1);
        Assert.NotEqual(under, pixel);
    }

    // A border whose sides differ is drawn, not left out: the box less its padding box.
    [AvaloniaFact]
    public void A_mark_draws_a_border_whose_sides_differ()
    {
        var mark = new RadioMark { Width = 40, Height = 20, Background = Brushes.White, BorderBrush = Red, BorderThickness = new Thickness(6, 0, 6, 0) };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { mark } });
        var frame = scene.Capture();
        var at = mark.TranslatePoint(default, scene.Window)!.Value;
        Assert.Equal(Colors.Red, Frames.PixelAt(frame, (int)at.X + 2, (int)at.Y + 10));
        Assert.Equal(Colors.Red, Frames.PixelAt(frame, (int)at.X + 37, (int)at.Y + 10));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, (int)at.X + 20, (int)at.Y + 10));
    }
}
