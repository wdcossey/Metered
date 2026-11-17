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
        Assert.Equal(Colors.White, At(9));                           // the outline's gap: the page
        Assert.Equal(Color.Parse(focus.Outline!.Colour), At(11));    // the outline

        // An unchecked radio under the mouse: its inner disc, white, under the hover overlay.
        var hover = RadioOracle.Data.Colours.First(c => c.Theme == "Light" && !c.Checked && !c.Error && !c.Disabled && c.State == "hoverLabel");
        scene.Hover((Visual)radios[1].Part(Radio.LabelPart)!);
        frame = scene.Capture();
        var inner = (Visual)radios[1].Part(Radio.RadioMarkInnerPart)!;
        var c = inner.TranslatePoint(new Point(inner.Bounds.Width / 2, inner.Bounds.Height / 2), scene.Window)!.Value;
        var overlay = Color.Parse(hover.InnerOverlay!);
        var expected = (byte)Math.Round(255 * (1 - overlay.A / 255.0));
        var pixel = Frames.PixelAt(frame, (int)c.X, (int)c.Y);
        Assert.InRange(pixel.R, expected - 1, expected + 1);
        Assert.Equal(pixel.R, pixel.B);
    }
}
