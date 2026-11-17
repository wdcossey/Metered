using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Metered.Controls;
using AvaloniaButton = Avalonia.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>
/// Base Web's radio-v2 RadioGroup: the root's layout against its style function (RadioOracle), and the group's
/// behaviour as Base Web's group context, its tests (radio-v2/__tests__/radiogroup.test.tsx,
/// stateful-radiogroup.test.tsx) and a browser's radios of one name give it.
/// </summary>
public class RadioGroupTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, RadioOracle.Data.Group.Length));

    // Three radios of different heights (the second's description is longer), in a wide window.
    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void The_root_lays_the_radios_out_as_Base_Webs(int index)
    {
        var c = RadioOracle.Data.Group[index];
        var (group, radios) = Group("1", ["One", "Two", "Three"]);
        group.Align = Enum.Parse<RadioAlign>(c.Align, ignoreCase: true);
        group.LabelPlacement = Enum.Parse<RadioLabelPlacement>(c.LabelPlacement, ignoreCase: true);
        radios[1].Description = "A description over two lines,\nhere";
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { group } }, width: 1200);
        Assert.Equal("wrap", c.FlexWrap);

        var bounds = radios.Select(r => new Rect(r.TranslatePoint(default, group)!.Value, r.Bounds.Size)).ToArray();
        Assert.NotEqual(bounds[0].Height, bounds[1].Height);
        for (var i = 1; i < radios.Length; i++)
        {
            if (c.FlexDirection == "column")
            {
                Assert.Equal(bounds[i - 1].Bottom + c.RowGap, bounds[i].Top, 3);
                Assert.Equal(bounds[0].Left, bounds[i].Left, 3);
            }
            else
            {
                Assert.Equal(bounds[i - 1].Right + c.ColumnGap, bounds[i].Left, 3);
                if (c.AlignItems == "flex-end") Assert.Equal(bounds[0].Bottom, bounds[i].Bottom, 3);
                else Assert.Equal(bounds[0].Top, bounds[i].Top, 3);
            }
        }
    }

    // flexWrap: a horizontal group wider than the window goes onto more lines, a row gap apart.
    [AvaloniaFact]
    public void A_horizontal_group_wraps_onto_lines_a_row_gap_apart()
    {
        var (group, radios) = Group(null, Enumerable.Range(1, 8).Select(i => $"Option {i}").ToArray());
        group.Align = RadioAlign.Horizontal;
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { group } });
        var c = RadioOracle.Data.Group.First(g => g.Align == "horizontal");

        var tops = radios.Select(r => r.TranslatePoint(default, group)!.Value.Y).Distinct().Order().ToArray();
        Assert.True(tops.Length > 1, $"{tops.Length} line");
        Assert.Equal(tops[0] + radios[0].Bounds.Height + c.RowGap, tops[1], 3);
    }

    [AvaloniaFact]
    public void The_radio_whose_value_is_the_groups_is_checked()
    {
        var (group, radios) = Group("3", ["1", "2", "3"]);
        using var scene = new Scene(group);
        Assert.Equal([false, false, true], radios.Select(r => r.IsChecked == true));

        group.Value = "1";
        Assert.Equal([true, false, false], radios.Select(r => r.IsChecked == true));

        group.Value = null;
        Assert.Equal([false, false, false], radios.Select(r => r.IsChecked == true));
    }

    [AvaloniaFact]
    public void A_click_checks_the_radio_and_sets_the_groups_value()
    {
        var (group, radios) = Group(null, ["1", "2", "3"]);
        using var scene = new Scene(group);

        scene.Click(radios[0]);
        Assert.Equal("1", group.Value);
        Assert.Equal([true, false, false], radios.Select(r => r.IsChecked == true));

        scene.Click(radios[2]);
        Assert.Equal("3", group.Value);
        Assert.Equal([false, false, true], radios.Select(r => r.IsChecked == true));

        // A checked radio stays checked.
        scene.Click(radios[2]);
        Assert.Equal("3", group.Value);
    }

    [AvaloniaFact]
    public void A_disabled_group_disables_every_radio()
    {
        var (group, radios) = Group(null, ["1", "2", "3"]);
        group.IsEnabled = false;
        using var scene = new Scene(group);
        Assert.All(radios, r => Assert.False(r.IsEffectivelyEnabled));

        scene.Click(radios[1]);
        Assert.Null(group.Value);
    }

    [AvaloniaFact]
    public void A_radios_own_disabled_stays_in_an_enabled_group()
    {
        var (group, radios) = Group(null, ["1", "2", "3"]);
        radios[0].IsEnabled = false;
        using var scene = new Scene(group);
        Assert.Equal([false, true, true], radios.Select(r => r.IsEffectivelyEnabled));
    }

    // Base Web's tabIndex: the checked radio, or the first when none is; Tab goes to it.
    [AvaloniaTheory]
    [InlineData("3", 2)]
    [InlineData(null, 0)]
    public void Only_the_checked_or_first_radio_is_a_tab_stop(string? value, int stop)
    {
        var (group, radios) = Group(value, ["1", "2", "3"]);
        using var scene = new Scene(group);
        Assert.Equal(Enumerable.Range(0, 3).Select(i => i == stop), radios.Select(r => r.IsTabStop));

        scene.Key(Key.Tab);
        Assert.True(radios[stop].IsFocused);
    }

    [AvaloniaFact]
    public void Arrow_keys_move_the_selection_and_the_focus_wrapping()
    {
        var (group, radios) = Group("1", ["1", "2", "3"]);
        using var scene = new Scene(group);
        scene.Key(Key.Tab);

        scene.Key(Key.Down);
        Assert.Equal("2", group.Value);
        Assert.True(radios[1].IsFocused);
        scene.Key(Key.Right);
        Assert.Equal("3", group.Value);
        scene.Key(Key.Down);
        Assert.Equal("1", group.Value);
        Assert.True(radios[0].IsFocused);

        scene.Key(Key.Up);
        Assert.Equal("3", group.Value);
        Assert.True(radios[2].IsFocused);
        scene.Key(Key.Left);
        Assert.Equal("2", group.Value);
        Assert.Equal(1, radios.Count(r => r.IsTabStop));
        Assert.True(radios[1].IsTabStop);
    }

    [AvaloniaFact]
    public void Arrow_keys_pass_over_a_disabled_radio()
    {
        var (group, radios) = Group("1", ["1", "2", "3"]);
        radios[1].IsEnabled = false;
        using var scene = new Scene(group);
        scene.Key(Key.Tab);

        scene.Key(Key.Down);
        Assert.Equal("3", group.Value);
        scene.Key(Key.Up);
        Assert.Equal("1", group.Value);
    }

    // Base Web: $isFocusVisible is the group's keyboard focus on this radio, and the outline is drawn when it is checked.
    [AvaloniaFact]
    public void Keyboard_focus_draws_the_outline_on_the_checked_radio_only()
    {
        var (group, radios) = Group(null, ["1", "2"]);
        using var scene = new Scene(group);
        var outline = (Radio r) => ((RadioMark)r.Part(Radio.RadioMarkOuterPart)!).OutlineThickness;

        scene.Key(Key.Tab);
        Assert.Contains(":focus-visible", radios[0].Classes);
        Assert.Equal(0, outline(radios[0]));

        scene.Key(Key.Down);
        Assert.DoesNotContain(":focus-visible", radios[0].Classes);
        Assert.Contains(":focus-visible", radios[1].Classes);
        Assert.Equal(RadioOracle.Data.Colours.First(c => c.State == "focusVisible" && c.Checked).Outline!.Thickness, outline(radios[1]));

        // A click focuses by pointer: no outline.
        scene.Click(radios[0]);
        Assert.DoesNotContain(":focus-visible", radios[0].Classes);
        Assert.Equal(0, outline(radios[0]));
    }

    // A browser's radio checks on Space, and Enter submits the form rather than checking it.
    [AvaloniaFact]
    public void Space_checks_the_focused_radio_and_Enter_doesnt()
    {
        var (group, radios) = Group(null, ["1", "2"]);
        using var scene = new Scene(group);
        scene.Key(Key.Tab);
        Assert.True(radios[0].IsFocused);

        scene.Key(Key.Enter);
        Assert.Null(group.Value);
        scene.Key(Key.Space);
        Assert.Equal("1", group.Value);
    }

    // Base Web's group context (groupContext?.align ?? alignProp): the group's align, label placement and error are
    // drawn in place of the radio's own, which the radio keeps, and draws again when it leaves.
    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_group_draws_its_context_and_the_radio_keeps_its_own(bool clear)
    {
        var radio = new Radio { Value = "1", Content = "One", Align = RadioAlign.Vertical, LabelPlacement = RadioLabelPlacement.Right, Error = true };
        var group = new RadioGroup { Align = RadioAlign.Horizontal, LabelPlacement = RadioLabelPlacement.Left, Error = false, Items = { radio } };
        using var scene = new Scene(group);
        var outer = radio.Part(Radio.RadioMarkOuterPart)!;
        Assert.Contains(":align-horizontal", outer.Classes);
        Assert.Contains(":label-placement-left", outer.Classes);
        Assert.DoesNotContain(":error", outer.Classes);
        Assert.True(((RadioRootPanel)radio.Part(Radio.RootPart)!).LabelFirst);

        group.LabelPlacement = RadioLabelPlacement.Bottom;
        Assert.Contains(":label-placement-bottom", outer.Classes);
        Assert.Equal(RadioLabelPlacement.Right, radio.LabelPlacement);

        // Removed, or the whole group cleared (a reset, which names no old items).
        if (clear) group.Items.Clear();
        else group.Items.Remove(radio);
        scene.Pump();
        Assert.Contains(":align-vertical", outer.Classes);
        Assert.Contains(":label-placement-right", outer.Classes);
        Assert.Contains(":error", outer.Classes);

        // It no longer answers to the group.
        radio.IsChecked = true;
        Assert.Null(group.Value);
    }

    // What the group stood in for comes back: the radio's own checked (Base Web's own prop) and its own tab stop.
    [AvaloniaFact]
    public void A_radio_leaving_has_its_own_checked_and_tab_stop_back()
    {
        var radio = new Radio { Value = "1", Content = "One", IsChecked = false, IsTabStop = false };
        var group = new RadioGroup { Value = "1", Items = { radio } };
        using var scene = new Scene(group);
        Assert.True(radio.IsChecked);
        Assert.True(radio.IsTabStop);

        group.Items.Remove(radio);
        scene.Pump();
        Assert.False(radio.IsChecked);
        Assert.False(radio.IsTabStop);
    }

    // The flex-end alignment is the group's row's: a radio on its own with those props stays at the top of its slot.
    [AvaloniaFact]
    public void Only_a_groups_radios_sit_at_the_bottom_of_their_line()
    {
        var alone = new Radio { Content = "Alone", Align = RadioAlign.Horizontal, LabelPlacement = RadioLabelPlacement.Top };
        var (group, radios) = Group(null, ["One"]);
        group.Align = RadioAlign.Horizontal;
        group.LabelPlacement = RadioLabelPlacement.Top;
        using var scene = new Scene(new StackPanel { Children = { alone, group } });
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Top, alone.VerticalAlignment);
        Assert.Equal(Avalonia.Layout.VerticalAlignment.Bottom, radios[0].VerticalAlignment);
    }

    // A browser's radios take the arrow even with no other radio to go to: the page doesn't scroll.
    [AvaloniaFact]
    public void An_arrow_with_nowhere_to_go_is_still_the_groups()
    {
        var (group, radios) = Group("1", ["1", "2"]);
        radios[1].IsEnabled = false;
        var reached = 0;
        var outer = new StackPanel { Children = { group } };
        outer.KeyDown += (_, _) => reached++;
        using var scene = new Scene(outer);
        scene.Key(Key.Tab);
        scene.Key(Key.Down);
        Assert.Equal("1", group.Value);
        Assert.Equal(0, reached);
    }

    [AvaloniaFact]
    public void A_radio_whose_value_comes_later_is_checked_by_it()
    {
        var radio = new Radio { Content = "Late" };
        var group = new RadioGroup { Value = "late", Items = { new Radio { Value = "early", Content = "Early" }, radio } };
        using var scene = new Scene(group);
        Assert.False(radio.IsChecked);

        radio.Value = "late";
        Assert.True(radio.IsChecked);
        Assert.True(radio.IsTabStop);
    }

    // The tab stop of a group with no value is its first radio, whichever that is now.
    [AvaloniaFact]
    public void The_first_radio_is_the_tab_stop_after_the_first_leaves()
    {
        var (group, radios) = Group(null, ["1", "2", "3"]);
        using var scene = new Scene(group);
        group.Items.Remove(radios[0]);
        scene.Pump();
        Assert.True(radios[1].IsTabStop);
        Assert.False(radios[2].IsTabStop);
        Assert.True(radios[0].IsTabStop); // its own again, out of the group

        var first = new Radio { Value = "0", Content = "0" };
        group.Items.Insert(0, first);
        scene.Pump();
        Assert.True(first.IsTabStop);
        Assert.False(radios[1].IsTabStop);
    }

    // Alt+Left is a back gesture, Ctrl+arrow another shortcut: not the radios' to take.
    [AvaloniaTheory]
    [InlineData(RawInputModifiers.Alt)]
    [InlineData(RawInputModifiers.Control)]
    public void An_arrow_with_a_modifier_is_left_alone(RawInputModifiers modifier)
    {
        var (group, radios) = Group("1", ["1", "2"]);
        using var scene = new Scene(group);
        scene.Key(Key.Tab);
        scene.Window.KeyPress(Key.Down, modifier, PhysicalKey.None, null);
        scene.Window.KeyRelease(Key.Down, modifier, PhysicalKey.None, null);
        scene.Pump();
        Assert.Equal("1", group.Value);
    }

    // Base Web's role="radiogroup": Avalonia has no radio group, so a group.
    [AvaloniaFact]
    public void The_group_is_announced_as_a_group()
    {
        var (group, _) = Group(null, ["1"]);
        Assert.Equal(Avalonia.Automation.Peers.AutomationControlType.Group, Avalonia.Automation.AutomationProperties.GetControlTypeOverride(group));
    }

    // Base Web's containsInteractiveElement: a control in the label takes its own press; the radio isn't checked.
    [AvaloniaFact]
    public void A_control_in_the_label_takes_its_own_click()
    {
        var clicks = 0;
        var inner = new AvaloniaButton { Content = "Pick a colour" };
        inner.Click += (_, _) => clicks++;
        var radio = new Radio { Value = "1", Content = inner };
        var group = new RadioGroup { Items = { radio, new Radio { Value = "2", Content = "Two" } } };
        using var scene = new Scene(group);

        scene.Click(inner);
        Assert.Equal(1, clicks);
        Assert.Null(group.Value);
        Assert.False(radio.IsChecked);
    }

    [AvaloniaFact]
    public void Overrides_reach_the_root()
    {
        var theme = new Avalonia.Styling.ControlTheme(typeof(Border));
        theme.Setters.Add(new Avalonia.Styling.Setter(Border.PaddingProperty, new Thickness(7)));
        var (group, _) = Group(null, ["1"]);
        group.Overrides = [new Override { Part = RadioGroup.RadioGroupRootPart, Style = theme }];
        using var scene = new Scene(group);
        Assert.Equal(new Thickness(7), ((Border)group.Part(RadioGroup.RadioGroupRootPart)!).Padding);
    }

    internal static (RadioGroup Group, Radio[] Radios) Group(string? value, string[] labels)
    {
        var radios = labels.Select((label, i) => new Radio { Value = (i + 1).ToString(), Content = label }).ToArray();
        var group = new RadioGroup { Value = value };
        foreach (var r in radios) group.Items.Add(r);
        return (group, radios);
    }
}
