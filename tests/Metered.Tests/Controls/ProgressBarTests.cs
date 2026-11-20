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
    // state and its fill's transform; the label; the name assistive technology reads.
    [AvaloniaTheory]
    [MemberData(nameof(Values))]
    public void It_renders_what_Base_Webs_renders_for_the_value(int index)
    {
        var c = ProgressBarOracle.Data.Values[index];
        var pb = Make(c);
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { pb } });

        // Base Web keeps the value as given, past the range too.
        Assert.Equal(c.Value, pb.Value);
        var fills = pb.Elements(ProgressBar.BarProgressPart).Cast<ProgressBarFill>().ToList();
        Assert.Equal(c.Bars.Length, fills.Count);
        Assert.Equal(c.Bars.Length, pb.Elements(ProgressBar.BarPart).Count);
        Assert.Equal(c.InfiniteBars, pb.Elements(ProgressBar.InfiniteBarPart).Count);
        for (var i = 0; i < fills.Count; i++)
        {
            var expected = c.Bars[i];
            var state = expected.State switch
            {
                null => null, "completed" => ":step-completed", "inProgress" => ":step-in-progress", "awaits" => ":step-awaits",
                _ => throw new InvalidOperationException(expected.State),
            };
            foreach (var s in new[] { ":step-completed", ":step-in-progress", ":step-awaits" })
                Assert.Equal(s == state, fills[i].Classes.Contains(s));
            // The value the theme and the bar give, under the step in progress's animation.
            Assert.Equal(expected.Offset, fills[i].GetBaseValue(ProgressBarFill.OffsetProperty).GetValueOrDefault(), 9);
        }

        var label = (TextBlock)pb.Part(ProgressBar.LabelPart)!;
        Assert.Equal(c.Label is not null, label.IsVisible);
        if (c.Label is not null) Assert.Equal(c.Label, label.Text);

        var peer = ControlAutomationPeer.CreatePeerForElement(pb);
        Assert.Equal(c.Name, peer.GetName());
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
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
        ((IPseudoClasses)fill.Classes).Set(":metered-still", true);
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
            ((IPseudoClasses)half.Classes).Set(":metered-still", true);
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
