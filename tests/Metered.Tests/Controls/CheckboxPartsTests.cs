using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The Checkbox's behaviour (Base Web's checkbox.js and its scenarios): clicks, keys, the
/// indeterminate state, the tap area, touch, the parts' structure and mirrored states, and overrides.
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

    // Base Web's Root is a <label>: a click anywhere on its box, beyond the label's text, reaches the input.
    [AvaloniaFact]
    public void The_whole_row_takes_the_tap()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var label = P(box, Checkbox.LabelPart);
        var far = new Point(20 + box.Bounds.Width - 2, 20 + box.Bounds.Height / 2);
        Assert.True(far.X > label.TranslatePoint(new Point(label.Bounds.Width, 0), scene.Window)!.Value.X + 100);

        scene.Window.MouseMove(far);
        scene.Window.MouseDown(far, MouseButton.Left);
        scene.Window.MouseUp(far, MouseButton.Left);
        scene.Pump();
        Assert.True(box.IsChecked);
    }

    [AvaloniaFact]
    public void Space_toggles_it_and_Enter_does_not()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        box.Focus(NavigationMethod.Tab);
        scene.Pump();

        scene.Key(Key.Enter);
        Assert.False(box.IsChecked);
        scene.Key(Key.Space);
        Assert.True(box.IsChecked);
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

    // checkbox-indeterminate.scenario.tsx and checkbox.e2e.ts: checking the last unchecked child checks the parent.
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
        parent.IsCheckedChanged += (_, _) =>
        {
            if (parent.IsIndeterminate) return;
            first.IsChecked = second.IsChecked = parent.IsChecked;
        };
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

    [AvaloniaFact]
    public void Touch_never_hovers()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        box.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, box, touch, scene.Window,
            scene.CentreOf(box), 0, new PointerPointProperties(), KeyModifiers.None));
        scene.Pump();
        Assert.DoesNotContain(":hover", Checkmark(box).Classes);

        scene.Hover(box);
        Assert.Contains(":hover", Checkmark(box).Classes);
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
        Assert.Equal(CheckboxOracle.Value.Checkmark.First(c => c is { Checked: true, State: "focusVisible" }).OutlineWidth, Checkmark(box).OutlineThickness);
    }

    [AvaloniaFact]
    public void The_states_are_mirrored_onto_every_part()
    {
        var box = new Checkbox { Content = "Label", IsChecked = true, Error = true, CheckmarkType = CheckboxStyleType.Toggle };
        using var scene = Show(box);

        foreach (var name in new[] { Checkbox.RootPart, Checkbox.CheckmarkPart, Checkbox.LabelPart, Checkbox.ToggleTrackPart, Checkbox.TogglePart })
        {
            var part = box.Part(name);
            Assert.NotNull(part);
            Assert.Contains(":checked", part.Classes);
            Assert.Contains(":error", part.Classes);
            Assert.Contains(":label-left", part.Classes);
            Assert.Contains(":loaded", part.Classes);
        }

        box.Error = false;
        box.IsChecked = false;
        Assert.DoesNotContain(":error", P(box, Checkbox.TogglePart).Classes);
        Assert.DoesNotContain(":checked", P(box, Checkbox.TogglePart).Classes);
    }

    [AvaloniaFact]
    public void With_no_label_there_is_no_Label_part_and_the_mark_stands_alone()
    {
        var box = new Checkbox();
        var empty = new Checkbox { Content = "" };
        using var scene = Show(box, empty);

        Assert.False(P(box, Checkbox.LabelPart).IsVisible);
        Assert.False(P(empty, Checkbox.LabelPart).IsVisible);
        Assert.Equal(new Point(2, 2), Checkmark(box).Bounds.Position);

        box.Content = "Now a label";
        scene.Pump();
        Assert.True(P(box, Checkbox.LabelPart).IsVisible);
    }

    [AvaloniaFact]
    public void The_checkmark_type_chooses_the_box_or_the_toggle()
    {
        var box = new Checkbox { Content = "Label" };
        using var scene = Show(box);
        Assert.True(Checkmark(box).IsVisible);
        Assert.False(P(box, Checkbox.ToggleTrackPart).IsVisible);

        box.CheckmarkType = CheckboxStyleType.Toggle;
        scene.Pump();
        Assert.False(Checkmark(box).IsVisible);
        Assert.True(P(box, Checkbox.ToggleTrackPart).IsVisible);
        Assert.Equal(LabelPlacement.Left, box.ActualLabelPlacement);
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
            CheckmarkType = CheckboxStyleType.Toggle,
            Overrides =
            [
                new Override { Part = "Root", Style = Theme<LabelPlacementPanel>(Panel.BackgroundProperty, Red) },
                new Override { Part = "Label", Style = Theme<ContentPresenter>(ContentPresenter.ForegroundProperty, Red) },
                new Override { Part = "ToggleTrack", Style = Theme<Border>(Border.BackgroundProperty, Red) },
                new Override { Part = "Toggle", Style = Theme<CheckboxMark>(CheckboxMark.BackgroundProperty, Red) },
            ],
        };
        using var scene = Show(box);

        Assert.Same(Red, ((Panel)P(box, Checkbox.RootPart)).Background);
        Assert.Same(Red, ((ContentPresenter)P(box, Checkbox.LabelPart)).Foreground);
        Assert.Same(Red, ((Border)P(box, Checkbox.ToggleTrackPart)).Background);
        Assert.Same(Red, ((CheckboxMark)P(box, Checkbox.TogglePart)).Background);
        // Merged over the part's theme: the knob keeps its size.
        Assert.Equal(CheckboxOracle.Value.Sizes.Toggle.Width, P(box, Checkbox.TogglePart).Bounds.Width);
    }

    // CSS doesn't transition a first paint: the transitions are set only once the box has loaded.
    [AvaloniaFact]
    public void Transitions_wait_for_load()
    {
        var box = new Checkbox { Content = "Label", CheckmarkType = CheckboxStyleType.Toggle };
        var host = new Border { Child = box };
        using var scene = Show(host);
        var knob = (CheckboxMark)P(box, Checkbox.TogglePart);
        Assert.NotNull(knob.Transitions);
        Assert.NotNull(Checkmark(box).Transitions);

        // Taken out of the window, it is unloaded; its parts keep their theme but lose the transitions.
        host.Child = null;
        scene.Pump();
        Assert.DoesNotContain(":loaded", knob.Classes);
        Assert.Null(knob.Transitions);
    }
}
