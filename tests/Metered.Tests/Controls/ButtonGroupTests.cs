using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
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

    // However they leave: removed, the group's items cleared, or the source cleared, a reset that names no old items
    // (#45). Each button has its own values back and no longer answers to the group.
    [AvaloniaTheory]
    [InlineData("remove")]
    [InlineData("clear")]
    [InlineData("source")]
    public void Buttons_leaving_the_group_are_let_go_however_they_leave(string leave)
    {
        var buttons = new[] { new Button { Content = "Item 0" }, new Button { Content = "Item 1" } };
        var group = new ButtonGroup { Mode = ButtonGroupMode.Radio, Selected = [0], Kind = ButtonKind.Tertiary };
        var source = new System.Collections.ObjectModel.ObservableCollection<Button>(buttons);
        if (leave == "source") group.ItemsSource = source;
        else foreach (var button in buttons) group.Items.Add(button);
        var clicks = new List<int>();
        group.Click += (_, e) => clicks.Add(e.Index);
        var elsewhere = new StackPanel();
        using var scene = new Scene(new StackPanel { Children = { group, elsewhere } });
        Assert.All(buttons, b => Assert.Equal(ButtonKind.Tertiary, b.Kind));
        Assert.True(buttons[0].IsSelected);
        Assert.False(buttons[1].IsTabStop);

        if (leave == "remove") { group.Items.RemoveAt(1); group.Items.RemoveAt(0); }
        else if (leave == "clear") group.Items.Clear();
        else source.Clear();
        scene.Pump();

        foreach (var button in buttons)
        {
            Assert.Equal(ButtonKind.Primary, button.Kind);
            Assert.False(button.IsSet(Button.IsSelectedProperty));
            Assert.True(button.IsTabStop);
            Assert.False(button.IsSet(AutomationProperties.ControlTypeOverrideProperty));
        }

        // Clicked where it now is, it changes nothing in the group.
        foreach (var button in buttons) elsewhere.Children.Add(button);
        scene.Pump();
        foreach (var button in buttons) scene.Click(button);
        Assert.Empty(clicks);
        Assert.Equal([0], group.Selected!);

        // Back in the group, a click is reported once: the group's old hold on it went when it left.
        elsewhere.Children.Remove(buttons[0]);
        if (leave == "source") source.Add(buttons[0]);
        else group.Items.Add(buttons[0]);
        scene.Pump();
        scene.Click(buttons[0]);
        Assert.Equal([0], clicks);
        Assert.Empty(group.Selected!);
    }

    // A reset lets go only of the buttons that are gone: those still in the source stay the group's, a button's own
    // selection kept.
    [AvaloniaFact]
    public void A_reset_keeps_the_buttons_still_in_the_group()
    {
        var gone = new Button { Content = "Item 0" };
        var kept = new Button { Content = "Item 1" };
        var ownSelected = new Button { Content = "Item 2", IsSelected = true };
        var source = new ResettingCollection { gone, kept, ownSelected };
        var group = new ButtonGroup { Mode = ButtonGroupMode.Radio, Kind = ButtonKind.Tertiary, ItemsSource = source };
        var clicks = new List<int>();
        group.Click += (_, e) => clicks.Add(e.Index);
        using var scene = new Scene(group);
        Assert.False(kept.IsTabStop);
        Assert.True(ownSelected.IsSelected);

        source.RemoveFirstAndReset();
        scene.Pump();
        Assert.Equal(ButtonKind.Primary, gone.Kind);
        Assert.Equal(ButtonKind.Tertiary, kept.Kind);
        // Now the first, with none selected: the radio group's one tab stop, before any click applies it again.
        Assert.True(kept.IsTabStop);
        Assert.Equal(AutomationControlType.RadioButton, AutomationProperties.GetControlTypeOverride(kept));
        Assert.True(ownSelected.IsSelected);

        scene.Click(kept);
        Assert.Equal([0], clicks);
        Assert.Equal([0], group.Selected!);
        Assert.True(kept.IsSelected);
    }

    /// <summary>A group of the given buttons, through its items or a source, and how they leave it, as above.</summary>
    private static (ButtonGroup Group, Action Leave) Joined(Button[] buttons, Action<ButtonGroup> setup, string leave)
    {
        var group = new ButtonGroup();
        setup(group);
        if (leave == "source")
        {
            var source = new System.Collections.ObjectModel.ObservableCollection<Button>(buttons);
            group.ItemsSource = source;
            return (group, source.Clear);
        }

        foreach (var button in buttons) group.Items.Add(button);
        return leave switch
        {
            "remove" => (group, () => { foreach (var button in buttons) group.Items.Remove(button); }),
            "clear" => (group, group.Items.Clear),
            _ => throw new ArgumentOutOfRangeException(nameof(leave), leave, "remove, clear or source"),
        };
    }

    // #70, 1. Base Web's cloneElement keeps a child's own isSelected of true, and doesn't change the child: outside
    // the group it renders with its own props again, so its own true is still there.
    [AvaloniaTheory]
    [InlineData("remove")]
    [InlineData("clear")]
    [InlineData("source")]
    public void A_buttons_own_selection_stays_when_it_leaves(string leave)
    {
        var own = new Button { Content = "Own", IsSelected = true };
        var other = new Button { Content = "Other" };
        var (group, go) = Joined([own, other], g => { g.Mode = ButtonGroupMode.Radio; g.Selected = [1]; }, leave);
        using var scene = new Scene(group);
        Assert.True(own.IsSelected);
        Assert.True(other.IsSelected);

        go();
        scene.Pump();
        Assert.True(own.IsSet(Button.IsSelectedProperty));
        Assert.True(own.IsSelected);
        Assert.False(other.IsSet(Button.IsSelectedProperty));
    }

    // #70, 2. Inside the group, the group's kind, size and shape replace the button's own, and so do its tabIndex and
    // role (button-group.js's cloneElement). The child element itself is unchanged: once it leaves, it is its own again.
    [AvaloniaTheory]
    [InlineData("remove")]
    [InlineData("clear")]
    [InlineData("source")]
    public void A_buttons_own_kind_size_shape_tab_stop_and_role_come_back_when_it_leaves(string leave)
    {
        var button = new Button { Content = "Own", Kind = ButtonKind.Secondary, Size = ButtonSize.Compact, Shape = ButtonShape.Pill, IsTabStop = false };
        AutomationProperties.SetControlTypeOverride(button, AutomationControlType.Hyperlink);
        var plain = new Button { Content = "Plain" };
        var (group, go) = Joined([button, plain], g =>
        {
            g.Mode = ButtonGroupMode.Radio;
            g.Selected = [0];
            g.Kind = ButtonKind.Tertiary;
            g.Size = ButtonSize.Large;
            g.Shape = ButtonShape.Circle;
        }, leave);
        using var scene = new Scene(group);
        Assert.Equal(ButtonKind.Tertiary, button.Kind);
        Assert.Equal(ButtonSize.Large, button.Size);
        Assert.Equal(ButtonShape.Circle, button.Shape);
        Assert.True(button.IsTabStop);
        Assert.Equal(AutomationControlType.RadioButton, AutomationProperties.GetControlTypeOverride(button));
        // The group's kind changed while it holds the button: the new one replaces the old.
        group.Kind = ButtonKind.Outline;
        scene.Pump();
        Assert.Equal(ButtonKind.Outline, button.Kind);

        go();
        scene.Pump();
        Assert.Equal(ButtonKind.Secondary, button.Kind);
        Assert.Equal(ButtonSize.Compact, button.Size);
        Assert.Equal(ButtonShape.Pill, button.Shape);
        Assert.False(button.IsTabStop);
        Assert.Equal(AutomationControlType.Hyperlink, AutomationProperties.GetControlTypeOverride(button));
        // One with none of its own has the control's defaults, none of them left set.
        Assert.Equal(ButtonKind.Primary, plain.Kind);
        Assert.False(plain.IsSet(Button.KindProperty));
        Assert.False(plain.IsSet(Button.SizeProperty));
        Assert.False(plain.IsSet(Button.ShapeProperty));
        Assert.False(plain.IsSet(InputElement.IsTabStopProperty));
        Assert.False(plain.IsSet(AutomationProperties.ControlTypeOverrideProperty));
    }

    // #70, 3. Base Web's selected is by place: isIndexSelected(selected, index), with React.Children.map's index. After a
    // button before it comes or goes, a button is selected, and a tab stop, by its new place.
    [AvaloniaFact]
    public void Selection_and_tab_stop_follow_the_place_after_a_removal_an_insertion_or_a_move()
    {
        var group = Group(3, g => { g.Mode = ButtonGroupMode.Radio; g.Selected = [0]; });
        var (a, b, c) = (At(group, 0), At(group, 1), At(group, 2));
        var source = new System.Collections.ObjectModel.ObservableCollection<Button>(
            Enumerable.Range(0, 3).Select(i => new Button { Content = $"Item {i}" }));
        var (first, second) = (source[0], source[1]);
        var sourced = new ButtonGroup { Mode = ButtonGroupMode.Radio, Selected = [0], ItemsSource = source };
        using var scene = new Scene(new StackPanel { Children = { group, sourced } });
        Assert.True(a.IsSelected);
        Assert.True(first.IsSelected);

        group.Items.RemoveAt(0);
        scene.Pump();
        Assert.True(b.IsSelected);
        Assert.True(b.IsTabStop);
        Assert.False(c.IsSelected);
        Assert.False(c.IsTabStop);

        var inserted = new Button { Content = "Inserted" };
        group.Items.Insert(0, inserted);
        scene.Pump();
        Assert.True(inserted.IsSelected);
        Assert.True(inserted.IsTabStop);
        Assert.False(b.IsSelected);
        Assert.False(b.IsTabStop);

        // A group with none selected: the first, whichever it now is, is the tab stop.
        group.Selected = [];
        scene.Pump();
        group.Items.RemoveAt(0);
        scene.Pump();
        Assert.True(b.IsTabStop);
        Assert.False(c.IsTabStop);

        // A move, through a source: the button that was first is selected no more, and the one now first is.
        source.Move(0, 2);
        scene.Pump();
        Assert.False(first.IsSelected);
        Assert.False(first.IsTabStop);
        Assert.True(second.IsSelected);
        Assert.True(second.IsTabStop);
    }

    // #70, as 1 and 2, for a button's own values however it has them: a style's, a binding's, or one set on it while
    // it is in the group. While it is in the group, the group's replace them (an own isSelected of true kept); once it
    // leaves, they are all its own again, the style and the binding still live.
    [AvaloniaFact]
    public void A_buttons_own_values_stay_beneath_the_groups_styled_bound_or_set_while_in_it()
    {
        var model = new Switchable();
        var button = new Button { Content = "Own", Classes = { "danger" }, DataContext = model };
        button.Bind(Button.IsSelectedProperty, new ReflectionBinding(nameof(Switchable.IsOn)));
        var group = new ButtonGroup { Mode = ButtonGroupMode.Radio, Selected = [1], Kind = ButtonKind.Tertiary };
        group.Items.Add(button);
        group.Items.Add(new Button { Content = "Other" });
        var elsewhere = new StackPanel();
        var root = new StackPanel { Children = { group, elsewhere } };
        root.Styles.Add(new Style(x => x.Class("danger")) { Setters = { new Setter(Button.KindProperty, ButtonKind.DangerPrimary) } });
        using var scene = new Scene(root);
        Assert.Equal(ButtonKind.Tertiary, button.Kind);
        Assert.False(button.IsSelected);
        Assert.False(button.IsTabStop);
        // Held at Avalonia's Animation priority, the one above a local value (the README's known difference 5).
        Assert.True(button.IsAnimating(Button.KindProperty));

        // The own true, read when the group applies (Avalonia tells of no change beneath the group's value), is kept;
        // gone again, the group's selection shows at once.
        model.IsOn = true;
        group.Selected = [1];
        scene.Pump();
        Assert.True(button.IsSelected);
        Assert.True(button.IsTabStop);
        model.IsOn = false;
        scene.Pump();
        Assert.False(button.IsSelected);
        Assert.False(button.IsTabStop);

        button.Size = ButtonSize.Compact;
        Assert.Equal(ButtonSize.Default, button.Size);

        group.Items.Remove(button);
        elsewhere.Children.Add(button);
        scene.Pump();
        Assert.Equal(ButtonKind.DangerPrimary, button.Kind);
        Assert.False(button.IsAnimating(Button.KindProperty));
        Assert.Equal(ButtonSize.Compact, button.Size);
        Assert.True(button.IsTabStop);
        button.Classes.Remove("danger");
        Assert.Equal(ButtonKind.Primary, button.Kind);
        model.IsOn = true;
        Assert.True(button.IsSelected);
    }

    // A TwoWay binding writes the group's selection back to its source, beneath the group's value. That isn't the
    // button's own isSelected: the group still decides. A true the source sets itself is the button's own, kept.
    [AvaloniaFact]
    public void A_two_way_bound_selection_follows_the_group_and_keeps_an_own_true()
    {
        var model = new Switchable();
        var bound = new Button { Content = "Bound", DataContext = model };
        bound.Bind(Button.IsSelectedProperty, new ReflectionBinding(nameof(Switchable.IsOn)) { Mode = BindingMode.TwoWay });
        var group = new ButtonGroup { Mode = ButtonGroupMode.Radio, Selected = [1] };
        group.Items.Add(new Button { Content = "First" });
        group.Items.Add(bound);
        using var scene = new Scene(group);
        Assert.True(bound.IsSelected);
        Assert.True(model.IsOn);

        group.Selected = [0];
        scene.Pump();
        Assert.False(bound.IsSelected);
        Assert.False(bound.IsTabStop);
        Assert.False(model.IsOn);

        model.IsOn = true;
        group.Selected = [0];
        scene.Pump();
        Assert.True(bound.IsSelected);
        Assert.True(bound.IsTabStop);
    }

    // When a button's own true goes, the group's selection takes its place, after the change has been told: a listener
    // that came after the group hears the values in order, the last of them the one the button has.
    [AvaloniaFact]
    public void A_listener_hears_the_groups_selection_after_the_buttons_own()
    {
        var button = new Button { Content = "Own", IsSelected = true };
        var group = new ButtonGroup { Mode = ButtonGroupMode.Radio, Selected = [0] };
        group.Items.Add(button);
        using var scene = new Scene(group);
        var heard = new List<bool>();
        button.PropertyChanged += (_, e) => { if (e.Property == Button.IsSelectedProperty) heard.Add((bool)e.NewValue!); };

        button.IsSelected = false;
        scene.Pump();
        Assert.True(button.IsSelected);
        Assert.Equal([false, true], heard);
    }

    private sealed class Switchable : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isOn;

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public bool IsOn
        {
            get => _isOn;
            set
            {
                _isOn = value;
                PropertyChanged?.Invoke(this, new(nameof(IsOn)));
            }
        }
    }

    /// <summary>A source that tells of a removal only as a reset, as a collection that rebuilds itself does.</summary>
    private sealed class ResettingCollection : System.Collections.ObjectModel.ObservableCollection<Button>
    {
        public void RemoveFirstAndReset()
        {
            Items.RemoveAt(0);
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }
}
