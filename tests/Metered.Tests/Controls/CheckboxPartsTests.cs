using Avalonia;
using Avalonia.Automation;
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
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The Checkbox's behaviour (Base Web's checkbox-v2/checkbox.js, its tests and its scenarios): clicks, keys, the
/// indeterminate state, hover and press by mouse, finger and key, the coarse pointer, focus, the parts' structure and
/// mirrored states, and overrides.
/// </summary>
public class CheckboxPartsTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    private static Scene Show(params Control[] boxes)
    {
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.AddRange(boxes);
        return new Scene(panel);
    }

    private static Control P(Checkbox box, string part) => (Control)box.Part(part)!;

    private static CheckboxMark Checkmark(Checkbox box) => (CheckboxMark)P(box, Checkbox.CheckmarkPart);

    private static Point Away => new(Scene.Width - 1, Scene.Height - 1);

    [AvaloniaFact]
    public void A_click_toggles_it_and_says_so()
    {
        var box = new Checkbox { Content = "Label" };
        var changes = new List<bool?>();
        box.IsCheckedChanged += (_, _) => changes.Add(box.IsChecked);
        using var scene = Show(box);

        scene.Click(Checkmark(box));
        Assert.True(box.IsChecked);
        scene.Click(P(box, Checkbox.LabelPart));
        Assert.False(box.IsChecked);
        Assert.Equal([true, false], changes);
    }

    // Base Web's Root is a <label>: a click anywhere on its box, the container's corner beyond the mark included, reaches
    // the input. An inline-flex label: nothing beyond its content does.
    [AvaloniaFact]
    public void The_container_and_the_label_take_the_tap_and_nothing_beside_them()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var container = P(box, Checkbox.CheckmarkContainerPart);
        var corner = container.TranslatePoint(new Point(2, 2), scene.Window)!.Value;

        scene.Window.MouseMove(corner);
        scene.Window.MouseDown(corner, MouseButton.Left);
        scene.Window.MouseUp(corner, MouseButton.Left);
        scene.Pump();
        Assert.True(box.IsChecked);

        var beside = box.TranslatePoint(new Point(box.Bounds.Width + 20, box.Bounds.Height / 2), scene.Window)!.Value;
        scene.Window.MouseDown(beside, MouseButton.Left);
        scene.Window.MouseUp(beside, MouseButton.Left);
        scene.Pump();
        Assert.True(box.IsChecked);
    }

    // checkbox.js: Space is the native input's toggle; its onKeyUp toggles on Enter, as onChange with !checked.
    [AvaloniaFact]
    public void Space_toggles_it_and_Enter_toggles_it_when_let_go()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        scene.Key(Key.Tab);
        Assert.True(box.IsFocused);

        scene.Key(Key.Space);
        Assert.True(box.IsChecked);

        scene.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        scene.Pump();
        Assert.True(box.IsChecked);
        scene.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        scene.Pump();
        Assert.False(box.IsChecked);
    }

    // checkbox.js's onKeyDown sets isActive for Space and Enter, its onKeyUp clears it; the CheckmarkContainer, the
    // input's sibling, isn't :active.
    [AvaloniaTheory]
    [InlineData(Key.Space, PhysicalKey.Space, " ")]
    [InlineData(Key.Enter, PhysicalKey.Enter, "\r")]
    public void A_key_held_is_active(Key key, PhysicalKey physical, string text)
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        scene.Key(Key.Tab);

        scene.Window.KeyPress(key, RawInputModifiers.None, physical, text);
        scene.Pump();
        Assert.Contains(":is-active", Checkmark(box).Classes);
        Assert.DoesNotContain(":active", P(box, Checkbox.CheckmarkContainerPart).Classes);

        scene.Window.KeyRelease(key, RawInputModifiers.None, physical, text);
        scene.Pump();
        Assert.DoesNotContain(":is-active", Checkmark(box).Classes);
    }

    // Base Web's key handlers are its input's: a key in a control in the label neither presses nor toggles it.
    [AvaloniaTheory]
    [InlineData(Key.Space, PhysicalKey.Space, " ")]
    [InlineData(Key.Enter, PhysicalKey.Enter, "\r")]
    public void A_key_in_a_control_in_the_label_isnt_the_checkboxs(Key key, PhysicalKey physical, string text)
    {
        var inner = new TextBox { Width = 120 };
        var box = new Checkbox { Content = inner };
        using var scene = Show(box);
        inner.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(inner.IsFocused);

        scene.Window.KeyPress(key, RawInputModifiers.None, physical, text);
        scene.Pump();
        Assert.DoesNotContain(":is-active", box.Classes);
        scene.Window.KeyRelease(key, RawInputModifiers.None, physical, text);
        scene.Pump();
        Assert.False(box.IsChecked);
    }

    // The Enter the checkbox acts on is its own: it doesn't reach a parent too, and only an Enter pressed on the
    // checkbox toggles it when let go.
    [AvaloniaFact]
    public void Enter_is_taken_and_only_its_own_press_toggles()
    {
        var before = new Avalonia.Controls.Button { Content = "Before" };
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(before, box);
        var reached = 0;
        ((Control)box.Parent!).AddHandler(InputElement.KeyDownEvent, (_, e) => { if (e.Key == Key.Enter) reached++; });

        // Pressed on another control, let go on the checkbox: no toggle.
        before.Focus(NavigationMethod.Tab);
        scene.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        box.Focus(NavigationMethod.Tab);
        scene.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        scene.Pump();
        Assert.True(box.IsFocused);
        Assert.False(box.IsChecked);

        reached = 0;
        scene.Window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        scene.Window.KeyRelease(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        scene.Pump();
        Assert.True(box.IsChecked);
        Assert.Equal(0, reached);
    }

    // The StatefulCheckbox's reducer sets only `checked`: isIndeterminate stays as the app set it.
    [AvaloniaFact]
    public void A_click_on_an_indeterminate_box_toggles_checked_and_leaves_it_indeterminate()
    {
        var box = new Checkbox { Content = "Label", IsIndeterminate = true };
        using var scene = Show(box);
        Assert.Contains(":is-indeterminate", Checkmark(box).Classes);

        scene.Click(box);
        Assert.True(box.IsChecked);
        Assert.True(box.IsIndeterminate);
        Assert.Contains(":is-indeterminate", Checkmark(box).Classes);
        Assert.Contains(":checked", Checkmark(box).Classes);
    }

    // checkbox-v2-indeterminate.scenario.tsx: checking the last unchecked child checks the parent.
    [AvaloniaFact]
    public void The_indeterminate_scenario_checks_the_parent_when_both_children_are_checked()
    {
        var parent = new Checkbox { Content = "Indeterminate checkbox if not all subcheckboxes are checked" };
        var first = new Checkbox { Content = "First subcheckbox", IsChecked = true };
        var second = new Checkbox { Content = "Second subcheckbox" };
        void Sync()
        {
            var all = first.IsChecked == true && second.IsChecked == true;
            parent.IsChecked = all;
            parent.IsIndeterminate = !all && (first.IsChecked == true || second.IsChecked == true);
        }

        first.IsCheckedChanged += (_, _) => Sync();
        second.IsCheckedChanged += (_, _) => Sync();
        Sync();
        using var scene = Show(parent, first, second);
        Assert.True(parent.IsIndeterminate);

        scene.Click(P(second, Checkbox.LabelPart));
        Assert.True(parent.IsChecked);
        Assert.False(parent.IsIndeterminate);
    }

    [AvaloniaFact]
    public void Avalonias_null_is_drawn_indeterminate()
    {
        var box = new Checkbox { Content = "Label", IsThreeState = true, IsChecked = null };
        using var scene = Show(box);

        Assert.Contains(":is-indeterminate", Checkmark(box).Classes);
        Assert.DoesNotContain(":checked", Checkmark(box).Classes);
        Assert.Equal(Geometry.Parse(CheckboxOracle.Value.Glyphs["indeterminate"].D).Bounds, Checkmark(box).Glyph!.Bounds);
    }

    [AvaloniaFact]
    public void A_disabled_box_does_not_toggle()
    {
        var box = new Checkbox { Content = "Label", IsEnabled = false };
        using var scene = Show(box);

        scene.Click(box);
        Assert.False(box.IsChecked);
        Assert.Contains(":disabled", Checkmark(box).Classes);
    }

    // Base Web: $isHovered is the whole checkbox's (every part), the CheckmarkContainer's CSS :hover its own.
    [AvaloniaFact]
    public void A_mouse_over_the_label_hovers_the_checkbox_but_not_the_container()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var container = P(box, Checkbox.CheckmarkContainerPart);

        scene.Hover(P(box, Checkbox.LabelPart));
        Assert.Contains(":is-hovered", Checkmark(box).Classes);
        Assert.DoesNotContain(":hover", container.Classes);

        scene.Hover(container);
        Assert.Contains(":hover", container.Classes);
        Assert.DoesNotContain(":hover", Checkmark(box).Classes);

        scene.Window.MouseMove(Away);
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", box.Classes);
        Assert.DoesNotContain(":hover", container.Classes);
    }

    // The container's hover is inside @media (hover: hover): a finger doesn't hover. A press is any pointer's, and ends.
    [AvaloniaFact]
    public void A_finger_presses_but_never_hovers()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var container = P(box, Checkbox.CheckmarkContainerPart);
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        box.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, box, touch, scene.Window,
            scene.CentreOf(container), 0, new PointerPointProperties(), KeyModifiers.None));
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", box.Classes);
        Assert.DoesNotContain(":hover", container.Classes);

        RadioColourTests.Touch(scene, container);
        Assert.Contains(":is-active", box.Classes);
        Assert.Contains(":active", container.Classes);

        scene.Click(box);
        Assert.DoesNotContain(":is-active", box.Classes);
        Assert.DoesNotContain(":active", container.Classes);
    }

    // checkbox.js's onMouseLeave clears isActive though the button is still held (radio-v2's doesn't); coming back
    // doesn't press it again. The container's CSS :active lasts until the button is let go.
    [AvaloniaFact]
    public void The_mouse_leaving_ends_the_press()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var container = P(box, Checkbox.CheckmarkContainerPart);
        scene.Press(container);
        Assert.Contains(":is-active", box.Classes);

        scene.Window.MouseMove(Away);
        scene.Pump();
        Assert.DoesNotContain(":is-active", box.Classes);
        Assert.DoesNotContain(":is-hovered", box.Classes);
        Assert.Contains(":active", container.Classes);

        scene.Window.MouseMove(scene.CentreOf(container));
        scene.Pump();
        Assert.Contains(":is-hovered", box.Classes);
        Assert.DoesNotContain(":is-active", box.Classes);

        scene.Window.MouseUp(scene.CentreOf(container), MouseButton.Left);
        scene.Pump();
        Assert.DoesNotContain(":active", container.Classes);
    }

    // checkbox.js's onMouseLeave is any pointer's (a browser sends mouse events for a pen and a finger): a finger
    // dragged off the checkbox ends its press too.
    [AvaloniaFact]
    public void A_finger_dragged_off_ends_the_press()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var container = P(box, Checkbox.CheckmarkContainerPart);
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        var held = new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other);
        box.RaiseEvent(new PointerPressedEventArgs(box, touch, scene.Window, scene.CentreOf(container), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        box.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, box, touch, scene.Window, scene.CentreOf(container), 0, held, KeyModifiers.None));
        scene.Pump();
        Assert.Contains(":is-active", box.Classes);

        box.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, box, touch, scene.Window, Away, 0, held, KeyModifiers.None));
        scene.Pump();
        Assert.DoesNotContain(":is-active", box.Classes);
    }

    [AvaloniaFact]
    public void A_mouse_click_focuses_without_the_focus_ring_and_Tab_gives_it()
    {
        var other = new Checkbox { Content = "Before" };
        var box = new Checkbox { Content = "Label", IsChecked = true };
        using var scene = Show(other, box);

        scene.Click(box);
        scene.Click(box);
        Assert.True(box.IsFocused);
        Assert.DoesNotContain(":focus-visible", Checkmark(box).Classes);
        Assert.Equal(0, Checkmark(box).OutlineThickness);

        scene.Click(other);
        scene.Key(Key.Tab);
        Assert.True(box.IsFocused);
        Assert.Contains(":focus-visible", Checkmark(box).Classes);
        var ring = CheckboxOracle.Value.Colours.First(c => c is { Checked: true, State: "focusVisible" }).Outline!;
        Assert.Equal(ring.Thickness, Checkmark(box).OutlineThickness);
        Assert.Equal(ring.Offset, Checkmark(box).OutlineOffset);

        // Base Web's onBlur ends it.
        scene.Key(Key.Tab);
        Assert.False(box.IsFocused);
        Assert.DoesNotContain(":focus-visible", Checkmark(box).Classes);
    }

    // Base Web's onFocus is the hidden input's: a control in the label taking the keyboard's focus isn't the checkbox's.
    [AvaloniaFact]
    public void Focus_on_a_control_in_the_label_isnt_the_checkboxs()
    {
        var inner = new Avalonia.Controls.Button { Content = "Terms" };
        var box = new Checkbox { Content = inner };
        using var scene = Show(box);
        inner.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(inner.IsFocused);
        Assert.DoesNotContain(":focus-visible", box.Classes);
        Assert.Equal(0, Checkmark(box).OutlineThickness);
    }

    [AvaloniaFact]
    public void The_states_are_mirrored_onto_every_part()
    {
        var box = new Checkbox { Content = "Label", IsChecked = true, Error = true, LabelPlacement = CheckboxLabelPlacement.Left };
        using var scene = Show(box);

        foreach (var name in new[] { Checkbox.RootPart, Checkbox.CheckmarkContainerPart, Checkbox.CheckmarkPart, Checkbox.LabelPart })
        {
            var part = box.Part(name);
            Assert.NotNull(part);
            Assert.Contains(":checked", part.Classes);
            Assert.Contains(":error", part.Classes);
            Assert.Contains(":label-placement-left", part.Classes);
            Assert.DoesNotContain(":label-placement-right", part.Classes);
            Assert.Contains(":loaded", part.Classes);
        }

        box.Error = false;
        box.IsChecked = false;
        box.LabelPlacement = CheckboxLabelPlacement.Right;
        Assert.DoesNotContain(":error", Checkmark(box).Classes);
        Assert.DoesNotContain(":checked", Checkmark(box).Classes);
        Assert.Contains(":label-placement-right", P(box, Checkbox.LabelPart).Classes);
        Assert.False(((RadioRootPanel)P(box, Checkbox.RootPart)).LabelFirst);
    }

    // Base Web renders the label only with children: an empty string is none either.
    [AvaloniaFact]
    public void The_label_is_there_only_when_given()
    {
        var box = new Checkbox();
        var empty = new Checkbox { Content = "" };
        using var scene = Show(box, empty);

        Assert.False(P(box, Checkbox.LabelPart).IsVisible);
        Assert.False(P(empty, Checkbox.LabelPart).IsVisible);

        box.Content = "Now a label";
        scene.Pump();
        Assert.True(P(box, Checkbox.LabelPart).IsVisible);
    }

    // Base Web's @media (pointer: coarse): padding round the root, which takes the pointer, for a 48 px target.
    [AvaloniaFact]
    public void A_coarse_pointer_pads_the_root_and_the_padding_takes_a_tap()
    {
        Assert.Equal(OperatingSystem.IsAndroid() || OperatingSystem.IsIOS(), new Checkbox().CoarsePointer);
        var box = new Checkbox { CoarsePointer = true };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(40), Children = { box } });
        var container = P(box, Checkbox.CheckmarkContainerPart);
        var padding = CheckboxOracle.Value.Layout.First(c => c.Coarse).RootPadding;
        Assert.Contains(":pointer-coarse", P(box, Checkbox.RootPart).Classes);
        Assert.Equal(container.Bounds.Height + 2 * padding, box.Bounds.Height);
        Assert.True(box.Bounds.Height >= 48, $"{box.Bounds.Height}");

        // A tap in the padding, above the container.
        var above = container.TranslatePoint(new Point(container.Bounds.Width / 2, -padding / 2), scene.Window)!.Value;
        scene.Window.MouseDown(above, MouseButton.Left);
        scene.Window.MouseUp(above, MouseButton.Left);
        scene.Pump();
        Assert.True(box.IsChecked);

        box.CoarsePointer = false;
        scene.Pump();
        Assert.Equal(container.Bounds.Height, box.Bounds.Height);
    }

    // Base Web sets its input's indeterminate, which assistive technology reads as mixed.
    [AvaloniaFact]
    public void An_indeterminate_box_is_mixed_to_automation()
    {
        var box = new Checkbox { Content = "Label", IsIndeterminate = true };
        using var scene = Show(box);
        var toggle = (IToggleProvider)ControlAutomationPeer.CreatePeerForElement(box);

        var peer = ControlAutomationPeer.CreatePeerForElement(box);
        var said = new List<object?>();
        peer.PropertyChanged += (_, e) => { if (e.Property == TogglePatternIdentifiers.ToggleStateProperty) said.Add(e.NewValue); };
        var clicks = 0;
        box.Click += (_, _) => clicks++;

        Assert.Equal(AutomationControlType.CheckBox, peer.GetAutomationControlType());
        Assert.Equal(ToggleState.Indeterminate, toggle.ToggleState);
        // A click while indeterminate: checked, still drawn indeterminate, so nothing changes for automation.
        scene.Click(box);
        Assert.Empty(said);
        box.IsIndeterminate = false;
        Assert.Equal(ToggleState.On, toggle.ToggleState);
        // A toggle through automation is a click, said once.
        toggle.Toggle();
        Assert.False(box.IsChecked);
        Assert.Equal(ToggleState.Off, toggle.ToggleState);
        Assert.Equal(2, clicks);
        Assert.Equal([ToggleState.On, ToggleState.Off], said);
    }

    [AvaloniaFact]
    public void A_control_can_be_the_label()
    {
        var text = new TextBlock { Text = "A control" };
        var box = new Checkbox { Content = text };
        using var scene = Show(box);

        Assert.Same(P(box, Checkbox.LabelPart), text.GetVisualParent());
        Assert.Same(box, text.Parent);
        Assert.True(P(box, Checkbox.LabelPart).IsVisible);

        var other = new TextBlock { Text = "Another" };
        box.Content = other;
        scene.Pump();
        Assert.Same(P(box, Checkbox.LabelPart), other.GetVisualParent());
        Assert.Same(box, other.Parent);
    }

    // A disabled checkbox takes no pointer, so it can't see the mouse leave: its hover and press end when it is disabled.
    [AvaloniaFact]
    public void Disabling_it_ends_its_hover_and_press()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var container = P(box, Checkbox.CheckmarkContainerPart);
        scene.Press(container);
        Assert.Contains(":is-hovered", Checkmark(box).Classes);
        Assert.Contains(":active", container.Classes);

        box.IsEnabled = false;
        scene.Pump();
        Assert.DoesNotContain(":is-hovered", Checkmark(box).Classes);
        Assert.DoesNotContain(":is-active", Checkmark(box).Classes);
        Assert.DoesNotContain(":hover", container.Classes);
        Assert.DoesNotContain(":active", container.Classes);
    }

    // Base Web renders children as given: an underscore is drawn, not taken as an access key.
    [AvaloniaFact]
    public void A_label_is_drawn_as_given()
    {
        var box = new Checkbox { Content = "snake_case option" };
        using var scene = Show(box);

        var text = P(box, Checkbox.LabelPart).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal("snake_case option", text.Text);
        Assert.IsNotType<AccessText>(text);
    }

    // ADR 0001: an override's plain setter replaces the default however the default reached it, a $ prop included.
    [AvaloniaFact]
    public void A_Checkmark_style_override_wins_over_the_checked_state()
    {
        var style = new ControlTheme(typeof(CheckboxMark));
        style.Setters.Add(new Setter(CheckboxMark.BackgroundProperty, Red));
        var box = new Checkbox { Content = "Label", IsChecked = true, Overrides = [new Override { Part = "Checkmark", Style = style }] };
        using var scene = Show(box);

        Assert.Same(Red, Checkmark(box).GetBaseValue(CheckboxMark.BackgroundProperty).GetValueOrDefault());
        // The rest of the theme stays: the glyph is still Base Web's tick.
        Assert.NotNull(Checkmark(box).Glyph);

        box.Overrides = null;
        scene.Pump();
        Assert.NotSame(Red, Checkmark(box).GetBaseValue(CheckboxMark.BackgroundProperty).GetValueOrDefault());
    }

    [AvaloniaFact]
    public void Each_part_takes_its_override()
    {
        ControlTheme Theme<T>(AvaloniaProperty property, object value)
        {
            var theme = new ControlTheme(typeof(T));
            theme.Setters.Add(new Setter(property, value));
            return theme;
        }

        var box = new Checkbox
        {
            Content = "Label",
            Overrides =
            [
                new Override { Part = "Root", Style = Theme<RadioRootPanel>(Panel.BackgroundProperty, Red) },
                new Override { Part = "CheckmarkContainer", Style = Theme<Border>(Border.BackgroundProperty, Red) },
                new Override { Part = "Checkmark", Style = Theme<CheckboxMark>(RadioMark.BorderBrushProperty, Red) },
                new Override { Part = "Label", Style = Theme<ContentPresenter>(ContentPresenter.ForegroundProperty, Red) },
            ],
        };
        using var scene = Show(box);

        Assert.Same(Red, ((Panel)P(box, Checkbox.RootPart)).Background);
        Assert.Same(Red, ((Border)P(box, Checkbox.CheckmarkContainerPart)).Background);
        Assert.Same(Red, Checkmark(box).BorderBrush);
        Assert.Same(Red, ((ContentPresenter)P(box, Checkbox.LabelPart)).Foreground);
        // Merged over the part's theme: the mark keeps its size.
        Assert.Equal(CheckboxOracle.Value.Layout[0].MarkSize[0], Checkmark(box).Bounds.Width);
    }

    // CSS doesn't transition a first paint: the transitions are set only once the box has loaded.
    [AvaloniaFact]
    public void Transitions_wait_for_load()
    {
        var box = new Checkbox { Content = "Label" };
        var host = new Border { Child = box };
        using var scene = Show(host);
        var mark = Checkmark(box);
        var transitions = Assert.IsType<Avalonia.Animation.Transitions>(mark.Transitions);
        var t = CheckboxOracle.Value.Transition;
        Assert.Equal(["background-image", "border-color", "background-color"], t.Properties);
        Assert.Equal(
            [CheckboxMark.BackgroundProperty, RadioMark.BorderBrushProperty],
            transitions.OfType<Avalonia.Animation.BrushTransition>().Select(x => (AvaloniaProperty)x.Property!));
        Assert.All(transitions.OfType<Avalonia.Animation.BrushTransition>(), x => Assert.Equal(TimeSpan.FromSeconds(t.Seconds), x.Duration));

        // Taken out of the window, it is unloaded; its parts keep their theme but lose the transitions.
        host.Child = null;
        scene.Pump();
        Assert.DoesNotContain(":loaded", mark.Classes);
        Assert.Null(mark.Transitions);
    }
}
