using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>Base Web's ButtonGroup and StatefulButtonGroup.</summary>
public class ButtonGroupTests
{
    private static ButtonGroup Group(int count, Action<ButtonGroup>? setup = null)
    {
        var group = new ButtonGroup();
        for (var i = 0; i < count; i++) group.Items.Add(new Button { Content = $"Item {i}" });
        setup?.Invoke(group);
        return group;
    }

    private static Button At(ButtonGroup group, int index) => (Button)group.Items[index]!;

    public static TheoryData<int> RootCases => new(Enumerable.Range(0, Oracle.ButtonGroup.Root.Length));

    [AvaloniaTheory]
    [MemberData(nameof(RootCases))]
    public void Root_matches_Base_Web(int index)
    {
        var c = Oracle.ButtonGroup.Root[index];
        var group = Group(8, g =>
        {
            g.Size = Enum.Parse<ButtonSize>(c.Size, ignoreCase: true);
            g.PaddingType = Enum.Parse<ButtonGroupPadding>(c.Padding, ignoreCase: true);
            g.Wrap = c.Wrap;
        });
        using var scene = new Scene(group);
        var root = (Border)group.Part("Root")!;
        if (c.PaddingLeft is { } left) Assert.Equal(new Thickness(left, 0, c.PaddingRight!.Value, 0), root.Padding);

        // The gap between neighbours on a row, and between rows when wrapping.
        var a = At(group, 0).Bounds;
        var b = At(group, 1).Bounds;
        Assert.Equal(c.ColumnGap, b.X - a.Right, 3);
        Assert.Equal(c.PaddingLeft ?? 0, At(group, 0).TranslatePoint(default, root)!.Value.X, 3);
        if (c.FlexWrap == "wrap")
        {
            var rows = group.Items.Cast<Button>().Select(x => x.Bounds.Y).Distinct().OrderBy(y => y).ToArray();
            Assert.True(rows.Length > 1, "eight buttons wrap at a phone's width");
            Assert.Equal(c.RowGap, rows[1] - (rows[0] + At(group, 0).Bounds.Height), 3);
        }
        else
        {
            Assert.Single(group.Items.Cast<Button>().Select(x => x.Bounds.Y).Distinct());
        }

        // overflowX auto scrolls sideways with no bar; otherwise nothing scrolls or clips.
        if (c.OverflowX == "auto") Assert.Equal(ScrollBarVisibility.Hidden, Assert.IsType<ScrollViewer>(root.Child).HorizontalScrollBarVisibility);
        else Assert.IsType<ItemsPresenter>(root.Child);
    }

    [AvaloniaFact]
    public void Defaults_are_Base_Webs()
    {
        var group = new ButtonGroup();
        Assert.Equal(ButtonKind.Secondary, group.Kind);
        Assert.Equal(ButtonSize.Default, group.Size);
        Assert.Equal(ButtonShape.Default, group.Shape);
        Assert.Equal(ButtonGroupPadding.None, group.PaddingType);
        Assert.Equal(ButtonGroupMode.None, group.Mode);
        Assert.Null(group.Selected);
        Assert.Null(group.Wrap);
    }

    [AvaloniaFact]
    public void The_groups_kind_size_and_shape_replace_the_buttons_own()
    {
        var group = Group(2, g => { g.Kind = ButtonKind.Outline; g.Size = ButtonSize.Compact; g.Shape = ButtonShape.Pill; });
        At(group, 0).Kind = ButtonKind.DangerPrimary;
        At(group, 0).Size = ButtonSize.Large;
        using var scene = new Scene(group);
        foreach (var button in group.Items.Cast<Button>())
        {
            Assert.Equal(ButtonKind.Outline, button.Kind);
            Assert.Equal(ButtonSize.Compact, button.Size);
            Assert.Equal(ButtonShape.Pill, button.Shape);
        }

        group.Kind = ButtonKind.Tertiary;
        scene.Pump();
        Assert.All(group.Items.Cast<Button>(), b => Assert.Equal(ButtonKind.Tertiary, b.Kind));
    }

    [AvaloniaFact]
    public void Radio_selects_the_clicked_button_and_clears_it_on_a_second_click()
    {
        var clicks = new List<int>();
        var group = Group(3, g => g.Mode = ButtonGroupMode.Radio);
        group.Click += (_, e) => clicks.Add(e.Index);
        using var scene = new Scene(group);

        scene.Click(At(group, 1));
        Assert.Equal([1], group.Selected!);
        Assert.True(At(group, 1).IsSelected);
        Assert.False(At(group, 0).IsSelected);

        scene.Click(At(group, 2));
        Assert.Equal([2], group.Selected!);
        Assert.False(At(group, 1).IsSelected);

        scene.Click(At(group, 2));
        Assert.Empty(group.Selected!);
        Assert.False(At(group, 2).IsSelected);
        Assert.Equal([1, 2, 2], clicks);
    }

    [AvaloniaFact]
    public void Checkbox_toggles_each_button()
    {
        var group = Group(3, g => g.Mode = ButtonGroupMode.Checkbox);
        using var scene = new Scene(group);
        scene.Click(At(group, 0));
        scene.Click(At(group, 2));
        Assert.Equal([0, 2], group.Selected!);
        scene.Click(At(group, 0));
        Assert.Equal([2], group.Selected!);
        Assert.False(At(group, 0).IsSelected);
        Assert.True(At(group, 2).IsSelected);
    }

    [AvaloniaFact]
    public void A_group_of_plain_buttons_selects_nothing_but_reports_the_click()
    {
        var clicks = new List<int>();
        var group = Group(2);
        group.Click += (_, e) => clicks.Add(e.Index);
        using var scene = new Scene(group);
        scene.Click(At(group, 1));
        Assert.Null(group.Selected);
        Assert.False(At(group, 1).IsSet(Button.IsSelectedProperty));
        Assert.Equal([1], clicks);
    }

    [AvaloniaFact]
    public void A_buttons_own_selection_is_kept()
    {
        var group = Group(2, g => { g.Mode = ButtonGroupMode.Checkbox; g.Selected = []; });
        At(group, 1).IsSelected = true;
        using var scene = new Scene(group);
        Assert.True(At(group, 1).IsSelected);
        Assert.False(At(group, 0).IsSelected);
    }

    [AvaloniaFact]
    public void Selected_set_from_outside_selects_the_buttons()
    {
        var group = Group(3, g => g.Mode = ButtonGroupMode.Checkbox);
        using var scene = new Scene(group);
        group.Selected = [0, 1];
        scene.Pump();
        Assert.True(At(group, 0).IsSelected);
        Assert.True(At(group, 1).IsSelected);
        Assert.False(At(group, 2).IsSelected);
    }

    [AvaloniaFact]
    public void A_disabled_group_disables_its_buttons_and_reports_no_click()
    {
        var clicks = 0;
        var group = Group(2, g => { g.Mode = ButtonGroupMode.Radio; g.IsEnabled = false; });
        group.Click += (_, _) => clicks++;
        using var scene = new Scene(group);
        Assert.False(At(group, 0).IsEffectivelyEnabled);
        scene.Click(At(group, 0));
        Assert.Equal(0, clicks);
        Assert.Null(group.Selected);
    }

    [AvaloniaFact]
    public void Radio_arrow_keys_move_the_selection_and_the_focus_wrapping()
    {
        var group = Group(3, g => g.Mode = ButtonGroupMode.Radio);
        using var scene = new Scene(group);
        At(group, 0).Focus(NavigationMethod.Tab);
        scene.Key(Key.Right);
        Assert.Equal([1], group.Selected!);
        Assert.True(At(group, 1).IsFocused);
        scene.Key(Key.Right);
        scene.Key(Key.Right);
        Assert.Equal([0], group.Selected!);
        scene.Key(Key.Left);
        Assert.Equal([2], group.Selected!);
        Assert.True(At(group, 2).IsFocused);
    }

    [AvaloniaFact]
    public void Radio_makes_only_the_selected_button_a_tab_stop_and_names_the_role()
    {
        var group = Group(3, g => g.Mode = ButtonGroupMode.Radio);
        using var scene = new Scene(group);
        Assert.Equal([true, false, false], group.Items.Cast<Button>().Select(b => b.IsTabStop));
        group.Selected = [2];
        scene.Pump();
        Assert.Equal([false, false, true], group.Items.Cast<Button>().Select(b => b.IsTabStop));
        Assert.All(group.Items.Cast<Button>(), b =>
            Assert.Equal(AutomationControlType.RadioButton, b.GetValue(AutomationProperties.ControlTypeOverrideProperty)));
    }

    [AvaloniaFact]
    public void Selected_buttons_look_selected()
    {
        // A selected secondary button is drawn as a primary one (Base Web's getColorStyles).
        var group = Group(2, g => { g.Mode = ButtonGroupMode.Radio; g.Selected = [0]; });
        using var scene = new Scene(group);
        Assert.Equal("#FF000000", ButtonColourTests.Colour(((Border)At(group, 0).Part("BaseButton")!).GetBaseValue(Border.BackgroundProperty).GetValueOrDefault()));
        Assert.NotEqual("#FF000000", ButtonColourTests.Colour(((Border)At(group, 1).Part("BaseButton")!).GetBaseValue(Border.BackgroundProperty).GetValueOrDefault()));
    }

    [AvaloniaFact]
    public void Root_takes_its_override()
    {
        var style = new ControlTheme(typeof(Border));
        style.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Red));
        var group = Group(2, g => g.Overrides = [new Override { Part = "Root", Style = style }]);
        using var scene = new Scene(group);
        Assert.Same(Brushes.Red, ((Border)group.Part("Root")!).Background);
    }

    [AvaloniaFact]
    public void A_button_leaving_the_group_takes_back_its_own_values()
    {
        var group = Group(2, g => { g.Mode = ButtonGroupMode.Radio; g.Selected = [0]; g.Kind = ButtonKind.Tertiary; });
        using var scene = new Scene(group);
        var first = At(group, 0);
        group.Items.RemoveAt(0);
        scene.Pump();
        Assert.Equal(ButtonKind.Primary, first.Kind);
        Assert.False(first.IsSelected);
        Assert.True(first.IsTabStop);
    }
}
