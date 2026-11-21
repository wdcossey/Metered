using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Diagnostics;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Metered.Controls;
using ProgressBar = Metered.Controls.ProgressBar;

namespace Metered.Tests.Controls;

/// <summary>
/// A progress bar's own behaviour (progress-bar/progressbar.js, its unit tests progressbar.test.tsx and its scenarios):
/// for each value Base Web's scenarios and tests give, the bars it renders, each step's state and where its fill is, the
/// label and the accessible name; the parts' state, a value or steps changing, the motion (at its end values: a
/// frame-by-frame test needs a test clock, #13), overrides, and the pixels the fill and the infinite bars draw.
/// </summary>
public class ProgressBarTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    public static TheoryData<int> Values => new(Enumerable.Range(0, ProgressBarOracle.Data.Values.Length));

    private static ProgressBar Make(ProgressBarValueCase c)
    {
        var pb = new ProgressBar
        {
            Minimum = c.MinValue, Maximum = c.MaxValue, Value = c.Value, Steps = c.Steps, Infinite = c.Infinite, ShowLabel = c.ShowLabel,
        };
        if (c.AriaLabel is { } name) AutomationProperties.SetName(pb, name);
        return pb;
    }

    // Base Web's ProgressBar, rendered: a Bar and a BarProgress for each step, or the two InfiniteBars; each step's
    // state and its fill's transform; the label; the name assistive technology reads. A value past the range is
    // clamped, as Avalonia's RangeBase clamps it, against Base Web, which keeps it as given (#67; the developer's choice,
    // docs/controls/ProgressBar/, known difference 11): the bar renders what Base Web renders for the clamped value, a
    // case of the oracle of its own, and so not what Base Web renders for the value as given, which stays recorded.
    [AvaloniaTheory]
    [MemberData(nameof(Values))]
    public void It_renders_what_Base_Webs_renders_for_the_value(int index)
    {
        var c = ProgressBarOracle.Data.Values[index];
        var pb = Make(c);
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { pb } });

        var clamped = Clamped(c);
        Assert.Equal(clamped.Value, pb.Value);
        Assert.Equal(clamped.MaxValue, pb.Maximum);
        AssertRenders(clamped, pb);
        // Past the range, the difference is a real one: Base Web's own output for the value as given isn't the bar's.
        if (!ReferenceEquals(clamped, c)) Assert.NotEqual(Output(c), Output(clamped));
    }

    // Base Web's own output past the range, kept beside the clamped bar's: its formulas, as Metered writes them, still
    // give what Base Web renders for the value as given (JavaScript's digits, the dropped 'translateX(--50%)', the step
    // states under the minimum).
    [Theory]
    [MemberData(nameof(PastTheRange))]
    public void Base_Webs_own_output_past_the_range_is_its_formulas_for_the_value_as_given(int index)
    {
        var c = ProgressBarOracle.Data.Values[index];
        Assert.NotSame(c, Clamped(c));
        if (c.Label is not null) Assert.Equal(c.Label, ProgressBar.DefaultLabel(c.Value, c.MaxValue, c.MinValue));
        if (c.AriaLabel is null && !c.Infinite)
        {
            Assert.Equal(c.Name, c.Steps > 1
                ? ProgressBar.StepLabel(c.Value, c.MaxValue, c.MinValue, c.Steps)
                : ProgressBar.DefaultLabel(c.Value, c.MaxValue, c.MinValue));
        }

        for (var i = 0; i < c.Bars.Length; i++)
        {
            if (c.Steps > 1) Assert.Equal(StepState(c.Bars[i].State), ProgressBar.StepState(c.Value, c.MinValue, c.MaxValue, c.Steps, i));
            else Assert.Equal(c.Bars[i].Offset, ProgressBar.Offset(c.Value, c.MinValue, c.MaxValue), 9);
        }
    }

    public static TheoryData<int> PastTheRange =>
        new(Enumerable.Range(0, ProgressBarOracle.Data.Values.Length).Where(i => !ReferenceEquals(ProgressBarOracle.Data.Values[i], Clamped(ProgressBarOracle.Data.Values[i]))));

    // RangeBase's coercion as the range moves under a value (RangeBase.cs, Avalonia 12.1.4): the value is clamped
    // again whenever the minimum or the maximum changes, from the value as it was set, so a range that grows back
    // gives it back; a maximum under the minimum is the minimum; a number that isn't finite is refused. Each look is
    // Base Web's for the clamped case.
    [AvaloniaFact]
    public void The_value_is_clamped_again_as_the_range_moves()
    {
        var pb = new ProgressBar { Value = 120, ShowLabel = true };
        using var scene = new Scene(pb);
        Assert.Equal(100, pb.Value);
        AssertRenders(Case(100, 0, 100), pb);

        // The range grows to take the value as it was set: 120 of 100 to 200.
        pb.Maximum = 200;
        pb.Minimum = 100;
        Assert.Equal(120, pb.Value);
        AssertRenders(Case(120, 100, 200), pb);

        // A value under the minimum is the minimum.
        pb.Value = 20;
        Assert.Equal(100, pb.Value);
        AssertRenders(Case(100, 100, 200), pb);

        // The minimum lowered under it gives it back.
        pb.Minimum = 0;
        pb.Maximum = 100;
        Assert.Equal(20, pb.Value);
        AssertRenders(Case(20, 0, 100), pb);

        // A maximum under the minimum is the minimum: an empty range, the value at it. It reads "0% Complete", not Base
        // Web's "NaN% Complete" (#82: Shown).
        pb.Minimum = 50;
        pb.Maximum = 10;
        Assert.Equal(50, pb.Maximum);
        Assert.Equal(50, pb.Value);
        AssertRenders(Case(50, 50, 50), pb);

        // A range again: the value as it was set comes back.
        pb.Minimum = 0;
        pb.Maximum = 100;
        Assert.Equal(20, pb.Value);
        AssertRenders(Case(20, 0, 100), pb);

        // A value or an end that isn't finite is refused: each keeps what it had.
        pb.Value = double.NaN;
        pb.Maximum = double.PositiveInfinity;
        pb.Minimum = double.NegativeInfinity;
        Assert.Equal((20.0, 0.0, 100.0), (pb.Value, pb.Minimum, pb.Maximum));
        AssertRenders(Case(20, 0, 100), pb);
    }

    // XAML sets the properties in the order written, inside BeginInit and EndInit, and code in any order: the value is
    // clamped again once the bar is initialized, so a value set before its range isn't lost, and a minimum set over the
    // maximum still standing (100) before the value is no trouble.
    [AvaloniaTheory]
    [InlineData("Value Minimum Maximum", true)]
    [InlineData("Value Maximum Minimum", true)]
    [InlineData("Minimum Value Maximum", true)]
    [InlineData("Minimum Maximum Value", true)]
    [InlineData("Maximum Value Minimum", true)]
    [InlineData("Maximum Minimum Value", true)]
    [InlineData("Minimum Value Maximum", false)]
    [InlineData("Value Minimum Maximum", false)]
    public void A_value_set_in_any_order_with_its_range_is_kept(string order, bool init)
    {
        var pb = new ProgressBar { ShowLabel = true };
        if (init) pb.BeginInit();
        foreach (var name in order.Split(' '))
        {
            if (name == "Value") pb.Value = 120;
            else if (name == "Minimum") pb.Minimum = 100;
            else pb.Maximum = 200;
        }

        if (init) pb.EndInit();
        using var scene = new Scene(pb);
        Assert.Equal((120.0, 100.0, 200.0), (pb.Value, pb.Minimum, pb.Maximum));
        AssertRenders(Case(120, 100, 200), pb);
    }

    // A minimum over the maximum still standing, set before the value: RangeBase's own clamp throws here
    // (MathUtilities.Clamp(160, 150, 100)), in XAML's order as in code's. The bar takes the value and its range as set.
    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_minimum_over_the_maximum_standing_doesnt_throw(bool init)
    {
        var pb = new ProgressBar();
        if (init) pb.BeginInit();
        pb.Minimum = 150;
        pb.Value = 160;
        pb.Maximum = 200;
        if (init) pb.EndInit();
        using var scene = new Scene(pb);
        Assert.Equal((160.0, 150.0, 200.0), (pb.Value, pb.Minimum, pb.Maximum));
    }

    // A new data context brings a value and its range by bindings, which RangeBase doesn't clamp again while the
    // context changes, nor once it has: the value is clamped to the new range once the change is over, from the value
    // as the binding gave it. Each look is Base Web's for the clamped case.
    [AvaloniaFact]
    public void A_new_data_context_is_clamped_to_its_own_range()
    {
        var pb = new ProgressBar { ShowLabel = true };
        pb.Bind(RangeBase.ValueProperty, new ReflectionBinding(nameof(Model.Value)));
        pb.Bind(RangeBase.MaximumProperty, new ReflectionBinding(nameof(Model.Maximum)));
        pb.Bind(RangeBase.MinimumProperty, new ReflectionBinding(nameof(Model.Minimum)));
        pb.DataContext = new Model(20, 0, 100);
        using var scene = new Scene(pb);
        AssertRenders(Case(20, 0, 100), pb);

        pb.DataContext = new Model(120, 100, 200);
        Assert.Equal((120.0, 100.0, 200.0), (pb.Value, pb.Minimum, pb.Maximum));
        AssertRenders(Case(120, 100, 200), pb);

        pb.DataContext = new Model(150, 0, 100);
        Assert.Equal((100.0, 0.0, 100.0), (pb.Value, pb.Minimum, pb.Maximum));
        AssertRenders(Case(100, 0, 100), pb);

        // A maximum under the minimum, its binding ahead of the minimum's: the maximum is the minimum once it's over.
        pb.DataContext = new Model(50, 50, 10);
        Assert.Equal((50.0, 50.0, 50.0), (pb.Value, pb.Minimum, pb.Maximum));
        AssertRenders(Case(50, 50, 50), pb);

        pb.DataContext = new Model(20, 0, 100);
        Assert.Equal(20, pb.Value);
        AssertRenders(Case(20, 0, 100), pb);
    }

    // A two-way binding, the app's own choice (Value binds one way by default), with its value bound first: the value
    // isn't clamped to the old range while the context changes, so nothing clamped is written into the new view model,
    // which keeps its value; the bar clamps it once the change is over.
    [AvaloniaFact]
    public void A_new_data_context_keeps_its_two_way_value()
    {
        var pb = new ProgressBar { ShowLabel = true };
        pb.Bind(RangeBase.ValueProperty, new ReflectionBinding(nameof(Model.Value)) { Mode = BindingMode.TwoWay });
        pb.Bind(RangeBase.MinimumProperty, new ReflectionBinding(nameof(Model.Minimum)));
        pb.Bind(RangeBase.MaximumProperty, new ReflectionBinding(nameof(Model.Maximum)));
        pb.DataContext = new Progress { Value = 20, Minimum = 0, Maximum = 100 };
        using var scene = new Scene(pb);
        AssertRenders(Case(20, 0, 100), pb);

        var next = new Progress { Value = 120, Minimum = 100, Maximum = 200 };
        pb.DataContext = next;
        Assert.Equal(120, next.Value);
        Assert.Equal((120.0, 100.0, 200.0), (pb.Value, pb.Minimum, pb.Maximum));
        AssertRenders(Case(120, 100, 200), pb);
    }

    /// <summary>A view model's progress: a value and its range.</summary>
    public sealed record Model(double Value, double Minimum, double Maximum);

    /// <summary>A view model's progress whose value a two-way binding can write.</summary>
    public sealed class Progress
    {
        public double Value { get; set; }

        public double Minimum { get; init; }

        public double Maximum { get; init; }
    }

    // GetProgressLabel is given the clamped value, as the default label reads it.
    [AvaloniaFact]
    public void GetProgressLabel_is_given_the_clamped_value()
    {
        var calls = new List<(double, double, double)>();
        var pb = new ProgressBar
        {
            Minimum = 100, Maximum = 200, Value = 250, ShowLabel = true,
            GetProgressLabel = (value, max, min) => { calls.Add((value, max, min)); return "label"; },
        };
        using var scene = new Scene(pb);
        Assert.Equal((200.0, 200.0, 100.0), calls.Last());
        Assert.DoesNotContain(calls, call => call.Item1 > 200);
    }

    // #82, against Base Web (the developer's choice of 2026-11-21; docs/controls/ProgressBar/, known difference 12):
    // over an empty range Base Web's default label, (value - minValue) / (maxValue - minValue), divides zero by zero and
    // reads "NaN% Complete", and so does its aria-label, which a screen reader reads out. Here both read "0% Complete".
    // The rest is Base Web's: the oracle's case stays as Base Web rendered it, its fill full (translateX(-NaN%) isn't
    // CSS); a stepped bar's name stays Base Web's "Step NaN of 3"; an app's own GetProgressLabel gets the range as it is.
    [AvaloniaFact]
    public void The_default_label_reads_0_percent_on_an_empty_range_where_Base_Webs_reads_NaN()
    {
        var c = Case(50, 50, 50);
        Assert.Equal("NaN% Complete", c.Label);
        Assert.Equal("NaN% Complete", c.Name);
        Assert.Equal("NaN% Complete", ProgressBar.DefaultLabel(c.Value, c.MaxValue, c.MinValue));

        var pb = Make(c);
        var stepped = ProgressBarOracle.Data.Values.Single(v => v.MinValue == 50 && v.MaxValue == 50 && v.Steps == 3);
        var steps = Make(stepped);
        using var scene = new Scene(new StackPanel { Children = { pb, steps } });
        Assert.Equal("0% Complete", ((TextBlock)pb.Part(ProgressBar.LabelPart)!).Text);
        Assert.Equal("0% Complete", ControlAutomationPeer.CreatePeerForElement(pb).GetName());
        Assert.Equal(c.Bars[0].Offset, pb.Elements(ProgressBar.BarProgressPart).Cast<ProgressBarFill>().Single().Offset, 9);
        Assert.Equal("Step NaN of 3", stepped.Name);
        Assert.Equal(stepped.Name, ControlAutomationPeer.CreatePeerForElement(steps).GetName());
        // Base Web shows a stepped bar's label as a bar of one step's: "0% Complete" here too.
        steps.ShowLabel = true;
        Assert.Equal("0% Complete", ((TextBlock)steps.Part(ProgressBar.LabelPart)!).Text);

        var calls = new List<(double, double, double)>();
        pb.GetProgressLabel = (value, max, min) => { calls.Add((value, max, min)); return $"{value} of {min} to {max}"; };
        Assert.Equal((50.0, 50.0, 50.0), calls.Last());
        Assert.Equal("50 of 50 to 50", ((TextBlock)pb.Part(ProgressBar.LabelPart)!).Text);
        Assert.Equal("50 of 50 to 50", ControlAutomationPeer.CreatePeerForElement(pb).GetName());

        // A range again: Base Web's label.
        pb.GetProgressLabel = null;
        pb.Maximum = 100;
        Assert.Equal("0% Complete", ((TextBlock)pb.Part(ProgressBar.LabelPart)!).Text);
        pb.Value = 75;
        Assert.Equal(ProgressBar.DefaultLabel(75, 100, 50), ((TextBlock)pb.Part(ProgressBar.LabelPart)!).Text);
    }

    /// <summary>
    /// The case as RangeBase coerces it (RangeBase.cs, Avalonia 12.1.4: CoerceMaximum, then CoerceValue): the maximum at
    /// least the minimum, the value in the range; the oracle's own case for that, which Base Web rendered.
    /// </summary>
    private static ProgressBarValueCase Clamped(ProgressBarValueCase c)
    {
        var max = Math.Max(c.MaxValue, c.MinValue);
        var value = Math.Clamp(c.Value, c.MinValue, max);
        if (value == c.Value && max == c.MaxValue) return c;
        var found = ProgressBarOracle.Data.Values.Where(v => v.Value == value && v.MinValue == c.MinValue && v.MaxValue == max
            && v.Steps == c.Steps && v.Infinite == c.Infinite && v.ShowLabel == c.ShowLabel && v.AriaLabel == c.AriaLabel).ToList();
        return found.Count == 1 ? found[0] : throw new InvalidOperationException(
            $"The oracle has {found.Count} cases for {c} clamped to {value} of {c.MinValue} to {max}: add one to tools/reference/progress-bar.js's VALUES.");
    }

    /// <summary>The oracle's labelled bar of one step, with no name given, for a value and a range.</summary>
    private static ProgressBarValueCase Case(double value, double min, double max) =>
        ProgressBarOracle.Data.Values.Single(v => v.Value == value && v.MinValue == min && v.MaxValue == max && v.Steps == 1
            && !v.Infinite && v.ShowLabel && v.AriaLabel is null);

    /// <summary>
    /// The label and the name the bar shows for a case Base Web rendered: Base Web's, but for its default label over an
    /// empty range, which reads "NaN% Complete" in Base Web and "0% Complete" here (#82, the developer's choice;
    /// docs/controls/ProgressBar/, known difference 12). The case itself stays as Base Web rendered it.
    /// </summary>
    private static (string? Label, string Name) Shown(ProgressBarValueCase c)
    {
        if (c.MaxValue != c.MinValue) return (c.Label, c.Name);
        var label = c.Label is null ? null : EmptyRange(c.Label);
        var name = c.AriaLabel is null && !c.Infinite && c.Steps == 1 ? EmptyRange(c.Name) : c.Name;
        return (label, name);

        static string EmptyRange(string baseWebs)
        {
            Assert.Equal("NaN% Complete", baseWebs);
            return "0% Complete";
        }
    }

    /// <summary>What Base Web renders for a case, to tell two apart: the name, the label and each bar's state and offset.</summary>
    private static string Output(ProgressBarValueCase c) =>
        $"{c.Name} | {c.Label} | {string.Join(" ", c.Bars.Select(b => $"{b.State}:{b.Offset}"))} | {c.InfiniteBars}";

    private static string? StepState(string? state) => state switch
    {
        null => null, "completed" => ":step-completed", "inProgress" => ":step-in-progress", "awaits" => ":step-awaits",
        _ => throw new InvalidOperationException(state),
    };

    /// <summary>The bar renders what Base Web renders for the case: its bars, each step's state and fill, the label, the name.</summary>
    private static void AssertRenders(ProgressBarValueCase c, ProgressBar pb)
    {
        var fills = pb.Elements(ProgressBar.BarProgressPart).Cast<ProgressBarFill>().ToList();
        Assert.Equal(c.Bars.Length, fills.Count);
        Assert.Equal(c.Bars.Length, pb.Elements(ProgressBar.BarPart).Count);
        Assert.Equal(c.InfiniteBars, pb.Elements(ProgressBar.InfiniteBarPart).Count);
        for (var i = 0; i < fills.Count; i++)
        {
            var expected = c.Bars[i];
            var state = StepState(expected.State);
            foreach (var s in new[] { ":step-completed", ":step-in-progress", ":step-awaits" })
                Assert.Equal(s == state, fills[i].Classes.Contains(s));
            // The value the theme and the bar give, under the step in progress's animation.
            Assert.Equal(expected.Offset, fills[i].GetBaseValue(ProgressBarFill.OffsetProperty).GetValueOrDefault(), 9);
        }

        var shown = Shown(c);
        var label = (TextBlock)pb.Part(ProgressBar.LabelPart)!;
        Assert.Equal(shown.Label is not null, label.IsVisible);
        if (shown.Label is not null) Assert.Equal(shown.Label, label.Text);

        var peer = ControlAutomationPeer.CreatePeerForElement(pb);
        Assert.Equal(shown.Name, peer.GetName());
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        // aria-valuenow and aria-valuemax: the range assistive technology reads is the bar's, clamped.
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(peer);
        Assert.Equal(pb.Value, range.Value);
        Assert.Equal(pb.Maximum, range.Maximum);
    }

    // progressbar.test.tsx: 'should render label' with getProgressLabel, called as Base Web calls it: (value, max, min).
    [AvaloniaFact]
    public void GetProgressLabel_gives_the_label_and_the_name()
    {
        var calls = new List<(double, double, double)>();
        var pb = new ProgressBar
        {
            Minimum = 100, Maximum = 200, Value = 120, ShowLabel = true,
            GetProgressLabel = (value, max, min) => { calls.Add((value, max, min)); return "label"; },
        };
        using var scene = new Scene(pb);
        Assert.Equal("label", ((TextBlock)pb.Part(ProgressBar.LabelPart)!).Text);
        Assert.Equal("label", ControlAutomationPeer.CreatePeerForElement(pb).GetName());
        Assert.Contains((120.0, 200.0, 100.0), calls);

        pb.GetProgressLabel = null;
        Assert.Equal(ProgressBarOracle.Data.Values.First(v => v.Value == 120 && v.Steps == 1).Label, ((TextBlock)pb.Part(ProgressBar.LabelPart)!).Text);
    }

    // aria-valuenow, -min and -max: Avalonia's progress bar peer reads the range, which it can't change.
    [AvaloniaFact]
    public void Its_range_is_read_only_to_assistive_technology()
    {
        var pb = new ProgressBar { Value = 75 };
        using var scene = new Scene(pb);
        var range = Assert.IsAssignableFrom<IRangeValueProvider>(ControlAutomationPeer.CreatePeerForElement(pb));
        Assert.Equal(75, range.Value);
        Assert.Equal(0, range.Minimum);
        Assert.Equal(100, range.Maximum);
        Assert.True(range.IsReadOnly);
        Assert.Equal(AutomationLiveSetting.Polite, AutomationProperties.GetLiveSetting(pb));
    }

    [AvaloniaFact]
    public void Every_part_takes_the_bars_state()
    {
        var pb = new ProgressBar { Size = ProgressBarSize.Large, Intent = ProgressBarIntent.Warning, Steps = 3, Value = 50, ShowLabel = true };
        using var scene = new Scene(pb);
        var parts = new[] { ProgressBar.RootPart, ProgressBar.BarContainerPart, ProgressBar.LabelPart, ProgressBar.BarPart, ProgressBar.BarProgressPart }
            .SelectMany(pb.Elements).ToList();
        Assert.Equal(3 + 3 + 3, parts.Count);
        foreach (var part in parts)
        {
            Assert.Contains(":size-large", part.Classes);
            Assert.DoesNotContain(":size-medium", part.Classes);
            Assert.Contains(":intent-warning", part.Classes);
            Assert.Contains(":stepped", part.Classes);
            Assert.DoesNotContain(":infinite", part.Classes);
            Assert.Contains(":loaded", part.Classes);
        }

        // Infinite: new halves, with the state; the first is the left one.
        pb.Infinite = true;
        pb.Intent = null;
        pb.Size = ProgressBarSize.Small;
        scene.Pump();
        Assert.Empty(pb.Elements(ProgressBar.BarPart));
        var halves = pb.Elements(ProgressBar.InfiniteBarPart);
        Assert.Equal([true, false], halves.Select(h => h.Classes.Contains(":is-left")));
        foreach (var half in halves)
        {
            Assert.Contains(":infinite", half.Classes);
            Assert.Contains(":size-small", half.Classes);
            Assert.DoesNotContain(":intent-warning", half.Classes);
            Assert.Contains(":loaded", half.Classes);
        }
    }

    // The steps change: as many bars, each with its state for the value, and no step state on a bar of one.
    [AvaloniaFact]
    public void The_bars_follow_the_steps_and_the_value()
    {
        var pb = new ProgressBar { Steps = 4, Value = 75 };
        using var scene = new Scene(pb);
        string[] States() => [.. pb.Elements(ProgressBar.BarProgressPart).Select(f =>
            f.Classes.FirstOrDefault(s => s.StartsWith(":step-", StringComparison.Ordinal)) ?? "")];
        Assert.Equal([":step-completed", ":step-completed", ":step-completed", ":step-in-progress"], States());
        pb.Value = 25;
        Assert.Equal([":step-completed", ":step-in-progress", ":step-awaits", ":step-awaits"], States());
        pb.Steps = 2;
        scene.Pump();
        Assert.Equal([":step-in-progress", ":step-awaits"], States());
        pb.Steps = 1;
        scene.Pump();
        Assert.Equal([""], States());
        pb.Value = 20;
        var fill = (ProgressBarFill)pb.Part(ProgressBar.BarProgressPart)!;
        Assert.Equal(ProgressBarOracle.Data.Values.First(v => v.Value == 20 && v.Steps == 1).Bars[0].Offset, fill.GetBaseValue(ProgressBarFill.OffsetProperty).GetValueOrDefault(), 9);
        pb.Steps = 0;
        scene.Pump();
        Assert.Empty(pb.Elements(ProgressBar.BarPart));
    }

    // transition: transform 0.5s, CSS's ease; not on the first paint, as CSS's. The fill is where Base Web's transform
    // puts it once the transition is over.
    [AvaloniaFact]
    public void The_fill_moves_once_loaded()
    {
        var pb = new ProgressBar { Value = 20 };
        using var scene = new Scene();
        scene.Window.Content = pb;
        pb.ApplyTemplate();
        var fill = (ProgressBarFill)pb.Part(ProgressBar.BarProgressPart)!;
        Assert.Null(fill.Transitions);

        scene.Pump();
        var t = ProgressBarOracle.Data.Motion.Transition;
        Assert.Equal("transform", t.Property);
        var transition = Assert.IsType<DoubleTransition>(Assert.Single(fill.Transitions!));
        Assert.Equal(ProgressBarFill.OffsetProperty, transition.Property);
        Assert.Equal(TimeSpan.FromSeconds(t.Seconds), transition.Duration);
        var curve = Assert.IsType<SplineEasing>(transition.Easing);
        Assert.Equal(t.Curve, new[] { curve.X1, curve.Y1, curve.X2, curve.Y2 });

        pb.Value = 100;
        scene.Pump();
        Assert.Equal(ProgressBarOracle.Data.Values.First(v => v.Value == 100 && v.Steps == 1).Bars[0].Offset,
            fill.GetBaseValue(ProgressBarFill.OffsetProperty).GetValueOrDefault());
    }

    // The step in progress: Base Web's 2.1 s endless slide in and fade, its keyframes; only while the element isn't
    // :metered-still (the preview gallery's), when it stands at its first keyframe.
    [AvaloniaFact]
    public void The_step_in_progress_slides_and_fades_endlessly()
    {
        var a = ProgressBarOracle.Data.Motion.InProgress;
        var pb = new ProgressBar { Steps = 2, Value = 50 };
        using var scene = new Scene(pb);
        var fill = (ProgressBarFill)pb.Elements(ProgressBar.BarProgressPart)[1];
        Assert.Contains(":step-in-progress", fill.Classes);

        var animation = Assert.Single(Animations(fill.Theme!));
        Assert.Equal(TimeSpan.FromSeconds(a.Seconds), animation.Duration);
        Assert.Equal("infinite", a.Iterations);
        Assert.Equal(IterationCount.Infinite, animation.IterationCount);
        var curve = Assert.IsType<SplineEasing>(animation.Easing);
        Assert.Equal(a.Curve, new[] { curve.X1, curve.Y1, curve.X2, curve.Y2 });
        Assert.Equal(a.Frames.Select(f => f.Cue / 100), animation.Children.Select(k => k.Cue.CueValue));
        for (var i = 0; i < a.Frames.Length; i++)
        {
            Assert.Equal(a.Frames[i].Offset, Setter(animation.Children[i], ProgressBarFill.OffsetProperty));
            Assert.Equal(a.Frames[i].Opacity, Setter(animation.Children[i], Visual.OpacityProperty));
        }

        // Running: the offset is the animation's. Still: the plain values, which are its first keyframe.
        Assert.Equal(BindingPriority.Animation, fill.GetDiagnostic(ProgressBarFill.OffsetProperty).Priority);
        ((IPseudoClasses)fill.Classes).Set(Metered.Tests.Sample.PreviewGallery.Still, true);
        scene.Pump();
        Assert.NotEqual(BindingPriority.Animation, fill.GetDiagnostic(ProgressBarFill.OffsetProperty).Priority);
        Assert.Equal(a.Frames[0].Offset, fill.Offset);
        Assert.Equal(a.Frames[0].Opacity, fill.Opacity);

        // The other steps have none.
        var done = (ProgressBarFill)pb.Elements(ProgressBar.BarProgressPart)[0];
        Assert.NotEqual(BindingPriority.Animation, done.GetDiagnostic(ProgressBarFill.OffsetProperty).Priority);
    }

    // infinite: each half's gradient, 300 % wide, swept for 1.5 s endlessly across it, the left one way and the right the
    // other; still, each stands at its first keyframe.
    [AvaloniaFact]
    public void The_infinite_halves_sweep_endlessly()
    {
        var pb = new ProgressBar { Infinite = true };
        using var scene = new Scene(pb);
        var halves = pb.Elements(ProgressBar.InfiniteBarPart).Cast<ProgressBarInfiniteBar>().ToList();
        foreach (var half in halves)
        {
            var s = ProgressBarOracle.Data.Motion.Infinite.Single(x => x.IsLeft == half.Classes.Contains(":is-left"));
            Assert.Equal(s.ImageSize, half.ImageSize);
            Assert.Equal(s.Start, half.FillStart);
            Assert.Equal(s.End, half.FillEnd);
            // Symmetric, so the angle, 90 or 270 degrees, draws the same.
            Assert.Equal(100, s.Start + s.End);

            var animation = Animations(half.Theme!).Single(x => Applies(x, half));
            Assert.Equal(TimeSpan.FromSeconds(s.Seconds), animation.Duration);
            Assert.Equal(IterationCount.Infinite, animation.IterationCount);
            var curve = Assert.IsType<SplineEasing>(animation.Easing);
            Assert.Equal(s.Curve, new[] { curve.X1, curve.Y1, curve.X2, curve.Y2 });
            Assert.Equal(s.Frames.Select(f => f.Cue / 100), animation.Children.Select(k => k.Cue.CueValue));
            Assert.Equal(s.Frames.Select(f => f.Position), animation.Children.Select(k => Setter(k, ProgressBarInfiniteBar.PositionProperty)));

            Assert.Equal(BindingPriority.Animation, half.GetDiagnostic(ProgressBarInfiniteBar.PositionProperty).Priority);
            ((IPseudoClasses)half.Classes).Set(Metered.Tests.Sample.PreviewGallery.Still, true);
            scene.Pump();
            Assert.Equal(s.Position, half.Position);
            Assert.Equal(s.Frames[0].Position, half.Position);
        }
    }

    // Base Web's gradient, drawn: from the left edge of an image three times the bar's width, placed by CSS's
    // background-position (box − image) × position: transparent, the colour from 25 % to 75 %, transparent.
    [AvaloniaTheory]
    [InlineData(50.0)]
    [InlineData(-50.0)]
    [InlineData(150.0)]
    [InlineData(0.0)]
    public void An_infinite_half_draws_Base_Webs_gradient_where_its_position_puts_it(double position)
    {
        var s = ProgressBarOracle.Data.Motion.Infinite[0];
        var colour = Color.Parse(ProgressBarOracle.Data.Colours.First(c => c.Theme == "Light" && c.Intent is null).Infinite);
        const double width = 200;
        var half = new ProgressBarInfiniteBar
        {
            Width = width, Height = 8, Fill = new ImmutableSolidColorBrush(colour), ImageSize = s.ImageSize,
            FillStart = s.Start, FillEnd = s.End, Position = position,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { half } });
        var frame = scene.Capture();
        var page = Frames.PixelAt(frame, (int)Scene.Width - 2, (int)Scene.Height - 2);
        var image = width * s.ImageSize / 100;
        var left = (width - image) * position / 100;
        foreach (var x in new[] { 10.0, 75.0, 100.0, 150.0, 190.0 })
        {
            // The colour's alpha along the image, by CSS's stops.
            var t = (x - left) / image * 100;
            var alpha = t <= 0 || t >= 100 ? 0 : t < s.Start ? t / s.Start : t > s.End ? (100 - t) / (100 - s.End) : 1;
            var p = half.TranslatePoint(new Point(x + 0.5, 4), scene.Window)!.Value;
            var pixel = Frames.PixelAt(frame, (int)p.X, (int)p.Y);
            int Blend(byte top, byte under) => (int)Math.Round(top * alpha + under * (1 - alpha));
            Assert.InRange(pixel.R, Blend(colour.R, page.R) - 3, Blend(colour.R, page.R) + 3);
            Assert.InRange(pixel.G, Blend(colour.G, page.G) - 3, Blend(colour.G, page.G) + 3);
            Assert.InRange(pixel.B, Blend(colour.B, page.B) - 3, Blend(colour.B, page.B) + 3);
        }
    }

    // A half keeps its gradient while the colour holds, and draws the new one when it changes (an intent, a theme).
    [AvaloniaFact]
    public void An_infinite_half_draws_a_new_colour_when_its_fill_changes()
    {
        var s = ProgressBarOracle.Data.Motion.Infinite[0];
        Color Of(string intent) => Color.Parse(ProgressBarOracle.Data.Colours.First(c => c.Theme == "Light" && c.Intent == intent).Infinite);
        var half = new ProgressBarInfiniteBar
        {
            Width = 200, Height = 8, Fill = new ImmutableSolidColorBrush(Of("positive")), ImageSize = s.ImageSize,
            FillStart = s.Start, FillEnd = s.End, Position = 50,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { half } });
        Color Middle() { var p = scene.CentreOf(half); return Frames.PixelAt(scene.Capture(), (int)p.X, (int)p.Y); }
        Assert.Equal(Of("positive"), Middle());
        half.Fill = new ImmutableSolidColorBrush(Of("negative"));
        Assert.Equal(Of("negative"), Middle());
    }

    // The fill, drawn: translateX(-80%) at 20 leaves the bar's first fifth filled, the rest its track.
    [AvaloniaFact]
    public void The_fill_covers_the_bar_as_far_as_the_value()
    {
        var c = ProgressBarOracle.Data.Values.First(v => v.Value == 20 && v.Steps == 1);
        var colours = ProgressBarOracle.Data.Colours.First(x => x.Theme == "Light" && x.Intent is null);
        var pb = new ProgressBar { Value = c.Value, Size = ProgressBarSize.Large };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { pb } });
        var frame = scene.Capture();
        var bar = (Visual)pb.Part(ProgressBar.BarPart)!;
        var w = bar.Bounds.Width;
        var end = w * (100 + c.Bars[0].Offset) / 100;
        Color At(double x) { var p = bar.TranslatePoint(new Point(x, 4), scene.Window)!.Value; return Frames.PixelAt(frame, (int)p.X, (int)p.Y); }
        Assert.Equal(Color.Parse(colours.BarProgress), At(end / 2));
        Assert.Equal(Color.Parse(colours.BarProgress), At(end - 4));
        Assert.Equal(Color.Parse(colours.Bar), At(end + 4));
        Assert.Equal(Color.Parse(colours.Bar), At(w - 4));
    }

    // A stepped bar, drawn: the completed steps filled, the awaiting ones their track.
    [AvaloniaFact]
    public void Completed_steps_are_filled_and_awaiting_ones_empty()
    {
        var colours = ProgressBarOracle.Data.Colours.First(x => x.Theme == "Light" && x.Intent == "positive");
        var pb = new ProgressBar { Value = 40, Steps = 5, Intent = ProgressBarIntent.Positive, Size = ProgressBarSize.Large };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { pb } });
        var frame = scene.Capture();
        var bars = pb.Elements(ProgressBar.BarPart).Cast<Visual>().ToList();
        Color Middle(Visual v) { var p = scene.CentreOf(v); return Frames.PixelAt(frame, (int)p.X, (int)p.Y); }
        Assert.Equal(Color.Parse(colours.BarProgress), Middle(bars[0]));
        Assert.Equal(Color.Parse(colours.BarProgress), Middle(bars[1]));
        Assert.Equal(Color.Parse(colours.Bar), Middle(bars[3]));
        Assert.Equal(Color.Parse(colours.Bar), Middle(bars[4]));
    }

    // ADR 0001: an override's plain setter wins over the part theme's state styles (progressbar-negative.scenario.tsx:
    // BarProgress's backgroundColor), and over the value's offset the bar sets.
    [AvaloniaFact]
    public void An_override_wins_over_the_parts_state_styles_and_the_values_offset()
    {
        var style = new ControlTheme(typeof(ProgressBarFill));
        style.Setters.Add(new Setter(ProgressBarFill.BackgroundProperty, Red));
        style.Setters.Add(new Setter(ProgressBarFill.OffsetProperty, -50.0));
        var pb = new ProgressBar { Value = 20, Intent = ProgressBarIntent.Positive, Overrides = [new Override { Part = ProgressBar.BarProgressPart, Style = style }] };
        using var scene = new Scene(pb);
        var fill = (ProgressBarFill)pb.Part(ProgressBar.BarProgressPart)!;
        Assert.Same(Red, fill.Background);
        Assert.Equal(-50, fill.Offset);
        // The rest of Base Web's style is still there.
        Assert.Equal(new CornerRadius(ProgressBarOracle.Data.Layout[0].BarProgressRadii[0]), fill.CornerRadius);

        pb.Overrides = null;
        scene.Pump();
        var c = ProgressBarOracle.Data.Colours.First(x => x.Theme == "Light" && x.Intent == "positive");
        Assert.Equal(c.BarProgress, RadioColourTests.Colour(fill.Background));
        Assert.Equal(ProgressBarOracle.Data.Values.First(v => v.Value == 20 && v.Steps == 1).Bars[0].Offset, fill.Offset);
    }

    [AvaloniaTheory]
    [InlineData(ProgressBar.RootPart, typeof(StackPanel), false)]
    [InlineData(ProgressBar.BarContainerPart, typeof(ProgressBarContainer), false)]
    [InlineData(ProgressBar.BarPart, typeof(Border), false)]
    [InlineData(ProgressBar.BarProgressPart, typeof(ProgressBarFill), false)]
    [InlineData(ProgressBar.InfiniteBarPart, typeof(ProgressBarInfiniteBar), true)]
    [InlineData(ProgressBar.LabelPart, typeof(TextBlock), false)]
    public void Every_part_takes_an_override(string part, Type type, bool infinite)
    {
        var style = new ControlTheme(type);
        style.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
        // Every step completed: a step in progress animates its opacity, and an animation wins over any style, as CSS's
        // does over an override's.
        var pb = new ProgressBar { Steps = 3, Value = 100, Infinite = infinite, ShowLabel = true, Overrides = [new Override { Part = part, Style = style }] };
        using var scene = new Scene(pb);
        var elements = pb.Elements(part);
        Assert.NotEmpty(elements);
        Assert.All(elements, e => Assert.Equal(0.5, ((Visual)e).Opacity));

        // Made again, for new steps, they take it too; taken away, they go back to the theme's.
        pb.Steps = 4;
        scene.Pump();
        Assert.All(pb.Elements(part), e => Assert.Equal(0.5, ((Visual)e).Opacity));
        pb.Overrides = null;
        scene.Pump();
        Assert.All(pb.Elements(part), e => Assert.Equal(1, ((Visual)e).Opacity));
    }

    // The value's offset is set at Template priority on every change: each replaces the last, so a bar updated many
    // times holds no more values than after its first change (Avalonia keeps a frame for each value set at that priority).
    [AvaloniaFact]
    public void A_bar_updated_many_times_holds_one_offset()
    {
        var pb = new ProgressBar { Value = 1 };
        using var scene = new Scene(pb);
        var fill = (ProgressBarFill)pb.Part(ProgressBar.BarProgressPart)!;
        pb.Value = 2;
        scene.Pump();
        var once = ValueFrames(fill);
        for (var v = 3; v <= 50; v++) pb.Value = v;
        scene.Pump();
        Assert.Equal(once, ValueFrames(fill));
        Assert.Equal(ProgressBar.Offset(50, 0, 100), fill.GetBaseValue(ProgressBarFill.OffsetProperty).GetValueOrDefault());
    }

    // A bar made while the page is up (steps or infinite changed) has its look at once, as a browser gives a new element
    // its first style without a transition; the fill doesn't slide in from empty.
    [AvaloniaFact]
    public void A_new_bar_has_its_value_at_once()
    {
        var c = ProgressBarOracle.Data.Values.First(v => v.Value == 40 && v.AriaLabel is not null);
        var pb = new ProgressBar { Value = c.Value, Infinite = true };
        using var scene = new Scene(pb);
        pb.Infinite = false;
        scene.Pump();
        var fill = (ProgressBarFill)pb.Part(ProgressBar.BarProgressPart)!;
        Assert.Contains(":loaded", fill.Classes);
        Assert.NotEqual(BindingPriority.Animation, fill.GetDiagnostic(ProgressBarFill.OffsetProperty).Priority);
        Assert.Equal(c.Bars[0].Offset, fill.Offset);

        pb.Steps = 3;
        scene.Pump();
        var first = (ProgressBarFill)pb.Elements(ProgressBar.BarProgressPart)[0];
        Assert.NotEqual(BindingPriority.Animation, first.GetDiagnostic(ProgressBarFill.OffsetProperty).Priority);
    }

    // aria-live="polite" says a changed aria-label: the peer raises the name's change as the value moves it.
    [AvaloniaFact]
    public void The_name_changes_with_the_value()
    {
        var from = ProgressBarOracle.Data.Values.First(v => v.Value == 20 && v.Steps == 1 && v.AriaLabel is null);
        var to = ProgressBarOracle.Data.Values.First(v => v.Value == 100 && v.Steps == 1);
        var pb = new ProgressBar { Value = from.Value };
        using var scene = new Scene(pb);
        var peer = ControlAutomationPeer.CreatePeerForElement(pb);
        var changes = new List<AutomationPropertyChangedEventArgs>();
        peer.PropertyChanged += (_, e) => { if (e.Property == AutomationElementIdentifiers.NameProperty) changes.Add(e); };
        pb.Value = to.Value;
        var change = Assert.Single(changes);
        Assert.Equal(from.Name, change.OldValue);
        Assert.Equal(to.Name, change.NewValue);

        // An app's own name is the name, and changes with it.
        AutomationProperties.SetName(pb, "Uploading");
        Assert.Equal("Uploading", changes.Last().NewValue);
        pb.Value = from.Value;
        Assert.Equal(2, changes.Count);
    }

    // showLabel renders the label or not; rendered, an override's style can still hide it, as on Base Web's.
    [AvaloniaFact]
    public void Only_ShowLabel_shows_the_label_and_an_override_may_hide_it()
    {
        var hide = new ControlTheme(typeof(TextBlock));
        hide.Setters.Add(new Setter(Visual.IsVisibleProperty, false));
        var show = new ControlTheme(typeof(TextBlock));
        show.Setters.Add(new Setter(Visual.IsVisibleProperty, true));
        var hidden = new ProgressBar { Value = 20, ShowLabel = true, Overrides = [new Override { Part = ProgressBar.LabelPart, Style = hide }] };
        var none = new ProgressBar { Value = 20, Overrides = [new Override { Part = ProgressBar.LabelPart, Style = show }] };
        using var scene = new Scene(new StackPanel { Children = { hidden, none } });
        Assert.False(((Visual)hidden.Part(ProgressBar.LabelPart)!).IsVisible);
        Assert.False(((Visual)none.Part(ProgressBar.LabelPart)!).IsVisible);
        none.ShowLabel = true;
        Assert.True(((Visual)none.Part(ProgressBar.LabelPart)!).IsVisible);
        none.ShowLabel = false;
        Assert.False(((Visual)none.Part(ProgressBar.LabelPart)!).IsVisible);
    }

    /// <summary>The frames in an element's value store: Avalonia keeps one for each value set at a priority other than local.</summary>
    private static int ValueFrames(AvaloniaObject o)
    {
        const System.Reflection.BindingFlags Private = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var store = typeof(AvaloniaObject).GetField("_values", Private)!.GetValue(o)!;
        var frames = store.GetType().GetField("_frames", Private)!.GetValue(store)!;
        return ((System.Collections.ICollection)frames).Count;
    }

    // errorMessage is deprecated and ignored in 18.2.0 (types.d.ts, progressbar.js): Metered has no such property.
    [AvaloniaFact]
    public void It_has_no_error_message()
    {
        Assert.Null(typeof(ProgressBar).GetProperty("ErrorMessage"));
    }

    private static IEnumerable<Animation> Animations(ControlTheme theme) =>
        theme.Children.OfType<Style>().SelectMany(s => s.Animations.OfType<Animation>());

    private static bool Applies(Animation animation, StyledElement element)
    {
        var style = ((ControlTheme)element.Theme!).Children.OfType<Style>().Single(s => s.Animations.Contains(animation));
        return style.Selector!.ToString().Contains(":not(:is-left)") != element.Classes.Contains(":is-left");
    }

    private static double Setter(KeyFrame frame, AvaloniaProperty property) =>
        Convert.ToDouble(frame.Setters.OfType<Setter>().Single(s => s.Property == property).Value, System.Globalization.CultureInfo.InvariantCulture);
}
