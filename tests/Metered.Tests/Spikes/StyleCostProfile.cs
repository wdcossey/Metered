using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Spikes;

/// <summary>
/// Spike 2, profiled: where opening a Button's time goes, by phase and by ablation (a piece switched
/// off for the test only, through the window's resources or the test's own subclass). Runs only when
/// METERED_BENCH is set, on its own (see <c>StyleCostSpike</c>); prints the tables that
/// docs/performance/README.md reads.
/// </summary>
/// <remarks>
/// Time is the UI thread's own CPU cycles (Windows' <c>QueryThreadCycleTime</c>), which other
/// processes on the machine don't add to as they do to a stopwatch; elsewhere, a stopwatch's ticks.
/// Allocations are <see cref="GC.GetAllocatedBytesForCurrentThread"/>, the same from run to run.
/// </remarks>
public class StyleCostProfile(ITestOutputHelper output)
{
    private const int Count = 300;
    private const int Runs = 41;

    private enum Phase { Construct, Add, Template, Attach, Measure, Layout, Render, Jobs }

    private static readonly int Phases = Enum.GetValues<Phase>().Length;
    private static readonly long[] Clock = new long[Phases];
    private static readonly long[] Bytes = new long[Phases];

    [DllImport("kernel32.dll")]
    private static extern bool QueryThreadCycleTime(IntPtr thread, out ulong cycles);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentThread();

    private static long Now()
    {
        if (!OperatingSystem.IsWindows()) return Stopwatch.GetTimestamp();
        QueryThreadCycleTime(GetCurrentThread(), out var cycles);
        return (long)cycles;
    }

    private static string Unit => OperatingSystem.IsWindows() ? "kcycles" : "kticks";

    // The phases being timed, innermost last: a phase's time and bytes are its own, less those of a phase timed inside it.
    private static readonly Stack<Phase> Open = new();

    private static T Time<T>(Phase phase, Func<T> action)
    {
        Open.Push(phase);
        var t = Now();
        var b = GC.GetAllocatedBytesForCurrentThread();
        try
        {
            return action();
        }
        finally
        {
            var clock = Now() - t;
            var bytes = GC.GetAllocatedBytesForCurrentThread() - b;
            Open.Pop();
            Clock[(int)phase] += clock;
            Bytes[(int)phase] += bytes;
            if (Open.TryPeek(out var outer))
            {
                Clock[(int)outer] -= clock;
                Bytes[(int)outer] -= bytes;
            }
        }
    }

    private static void Time(Phase phase, Action action) => Time(phase, () => { action(); return 0; });

    /// <summary>The Button, its template and its <c>Parts.Attach</c> timed apart, out of whichever phase they fall in.</summary>
    private class TimedButton : Button
    {
        protected override Size MeasureCore(Size availableSize)
        {
            Time(Phase.Template, () => ApplyTemplate());
            return Time(Phase.Measure, () => base.MeasureCore(availableSize));
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e) => Time(Phase.Attach, () => base.OnApplyTemplate(e));
    }

    /// <summary>A button whose parts <c>Parts</c> doesn't find: nothing is mirrored onto them.</summary>
    private sealed class UnmirroredButton : TimedButton
    {
        private static readonly FieldInfo Names = typeof(Parts).GetField("_names", BindingFlags.NonPublic | BindingFlags.Instance)
            ?? throw new InvalidOperationException("Parts has no _names field: the no-mirroring ablation needs updating.");

        public UnmirroredButton() => Names.SetValue(PartSet, Array.Empty<string>());
    }

    /// <summary>A button whose parts take their states as the template is built, before they are styled.</summary>
    /// <remarks><c>Button.OnApplyTemplate</c> attaches the parts again and finds every state already set: that second
    /// pass is this variant's Attach column; the first is in its Template column.</remarks>
    private sealed class PreseededButton : TimedButton
    {
        public static int ThemedAtBuild, Built;

        static PreseededButton() =>
            TemplateProperty.OverrideMetadata<PreseededButton>(new StyledPropertyMetadata<IControlTemplate?>(
                coerce: (_, t) => t is null or Preseed ? t : new Preseed(t)));

        private sealed class Preseed(IControlTemplate inner) : IControlTemplate
        {
            public TemplateResult<Control>? Build(TemplatedControl param)
            {
                var result = inner.Build(param);
                if (result is null) return null;
                Built++;
                if (result.NameScope.Find<StyledElement>("BaseButton")?.Theme is not null) ThemedAtBuild++;
                ((Button)param).PartSet.Attach(result.NameScope);
                return result;
            }
        }
    }

    private static readonly string[] PartKeys =
        ["BaseButton", "StartEnhancer", "EndEnhancer", "StartEnhancerButtonContentContainer", "LoadingSpinnerContainer", "LoadingSpinner"];

    // The styles a default Button (primary, medium, rectangular, enabled, at rest) matches once loaded.
    private static readonly HashSet<string> Matching = ["^", "^:loaded", "^:size-medium", "^:kind-primary"];

    // A default Button's kind, size and shape: a style on another (or on an icon or fixed shape, which follow the shape) can't match it until one changes.
    private static bool Fits(string selector) =>
        selector.Split(':').All(c => !(c.StartsWith("kind-") || c.StartsWith("size-") || c.StartsWith("shape-") || c is "icon-shape" or "fixed-shape")
            || c is "kind-primary" or "size-medium" or "shape-default");

    /// <summary>Finds a resource from the scene's window: a part theme by its key, or a token.</summary>
    private delegate object? Find(object key);

    private sealed record Variant(string Name, Func<int, Control> Make, Action<IResourceDictionary, Find>? Resources = null);

    private static ControlTheme Theme(Find find, string key) =>
        find(key) as ControlTheme ?? throw new InvalidOperationException($"No part theme {key}.");

    private static Action<IResourceDictionary, Find> AllParts(Func<string, bool> keep, bool plain = false, bool literal = false) =>
        (r, find) =>
        {
            var kept = 0;
            foreach (var p in PartKeys)
            {
                var copy = Filter(Theme(find, "Button.Styled" + p), keep, plain, literal ? find : null);
                kept += copy.Children.Count;
                r["Button.Styled" + p] = copy;
            }

            // A selector written otherwise than keep expects would empty every theme, and the variant would measure that instead.
            if (kept == 0 && keep("^:kind-primary")) throw new InvalidOperationException("The filter kept no style.");
        };

    [AvaloniaFact]
    public void Where_the_time_goes()
    {
        if (Environment.GetEnvironmentVariable("METERED_BENCH") is null) return;

        Func<int, Control> timed = i => new TimedButton { Content = "Button " + i };
        var variants = new Variant[]
        {
            new("TextBlock (floor)", i => new TextBlock { Text = "Button " + i }),
            new("Button", i => new Button { Content = "Button " + i }),
            new("Button, timed", timed),
            new("no mirroring", i => new UnmirroredButton { Content = "Button " + i }),
            new("states before styling", i => new PreseededButton { Content = "Button " + i }),
            new("spinner's theme, container's styles", timed, (r, find) =>
            {
                r["Button.StyledLoadingSpinnerContainer"] = Filter(Theme(find, "Button.StyledLoadingSpinnerContainer"), _ => false, plain: false);
                r["Button.StyledLoadingSpinner"] = new ControlTheme(typeof(ButtonLoadingSpinner));
            }),
            new("spinner without its animation", timed, (r, find) =>
                r["Button.StyledLoadingSpinner"] = Filter(Theme(find, "Button.StyledLoadingSpinner"), s => s != "^:is-loading", plain: false)),
            new("one kind's styles (primary's)", timed, AllParts(s => !s.Contains(":kind-") || s.Contains(":kind-primary"))),
            new("kind's, size's and shape's styles", timed, AllParts(Fits)),
            new("matching styles only", timed, AllParts(Matching.Contains)),
            new("matching styles, plain", timed, AllParts(Matching.Contains, plain: true)),
            new("all styles, resources as values", timed, AllParts(_ => true, literal: true)),
            new("part themes without styles", timed, AllParts(_ => false)),
        };

        output.WriteLine($"{Count} elements a run, {Runs} runs, variants interleaved in a new order each run. Per element: thread {Unit} (median, the quartiles' spread, the least), stopwatch µs (median), GC pause µs (median), KB allocated.");
        output.WriteLine("");
        output.WriteLine("With the GC as an app has it:");
        Table(variants, noGc: false);
        output.WriteLine("");
        output.WriteLine("With no collection inside a run (GC.TryStartNoGCRegion): the work alone, by phase:");
        Table(variants, noGc: true);
        output.WriteLine("");
        output.WriteLine($"Preseeded: BaseButton had its theme when the template was built in {PreseededButton.ThemedAtBuild} of {PreseededButton.Built}");
        Counts();
        BuildAlone();
    }

    private void Table(Variant[] variants, bool noGc)
    {
        var clocks = variants.Select(_ => new List<double[]>()).ToArray();
        var walls = variants.Select(_ => new List<double>()).ToArray();
        var pauses = variants.Select(_ => new List<double>()).ToArray();
        var bytes = new long[variants.Length][];
        var random = new Random(2);
        Collections = 0;
        for (var run = 0; run < Runs; run++)
        {
            // A new order each run, so that no variant always follows the same one.
            foreach (var v in Enumerable.Range(0, variants.Length).OrderBy(_ => random.Next()))
            {
                var (c, b, wall, pause) = Measure(variants[v], noGc);
                clocks[v].Add(c);
                walls[v].Add(wall);
                pauses[v].Add(pause);
                bytes[v] = b;
            }
        }

        output.WriteLine($"{"",-32}{Unit,8}{"IQR",6}{"min",7}{"µs",6}{"GC µs",7}{"KB",7} | " + string.Join("", Enum.GetNames<Phase>().Select(n => $"{n,10}")));
        for (var v = 0; v < variants.Length; v++)
        {
            var totals = clocks[v].Select(t => t.Sum()).OrderBy(x => x).ToList();
            var spread = totals[Runs * 3 / 4] - totals[Runs / 4];
            var phases = Enumerable.Range(0, Phases).Select(p => Median(clocks[v].Select(t => t[p])));
            output.WriteLine($"{variants[v].Name,-32}{Median(totals),8:F0}{spread,6:F0}{totals[0],7:F0}{Median(walls[v]),6:F0}{Median(pauses[v]),7:F0}{bytes[v].Sum() / 1024.0 / Count,7:F1} | "
                + string.Join("", phases.Select(p => $"{p,10:F1}")));
            output.WriteLine($"{"",-73} | " + string.Join("", bytes[v].Select(x => $"{x / 1024.0 / Count,9:F1}K")));
        }

        output.WriteLine($"Collections (gen 0) inside the timed runs: {Collections} in {Runs * variants.Length} runs");
    }

    /// <summary>Opens <see cref="Count"/> of a variant in a fresh window: per element, the clock and the bytes by phase, the stopwatch and the GC's pauses.</summary>
    private static (double[] Clock, long[] Bytes, double Wall, double Pause) Measure(Variant variant, bool noGc)
    {
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        using var scene = new Scene(panel, width: 1200, height: 4000);
        variant.Resources?.Invoke(scene.Window.Resources, key => scene.Window.TryFindResource(key, scene.Window.ActualThemeVariant, out var value) ? value : null);
        scene.Pump();
        Array.Clear(Clock);
        Array.Clear(Bytes);
        // Each run starts with the heap collected, so that a collection doesn't fall in one variant's phase by turn.
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        if (noGc && !GC.TryStartNoGCRegion(NoGcBudget)) throw new InvalidOperationException("No no-GC region.");
        double wall, pause;
        try
        {
            var gcs = GC.CollectionCount(0);
            var paused = GC.GetTotalPauseDuration();
            var watch = Stopwatch.StartNew();
            var made = new Control[Count];
            Time(Phase.Construct, () => { for (var i = 0; i < Count; i++) made[i] = variant.Make(i); });
            Time(Phase.Add, () => { foreach (var c in made) panel.Children.Add(c); });
            Time(Phase.Layout, scene.Window.UpdateLayout);
            Time(Phase.Render, () => scene.Window.CaptureRenderedFrame());
            Time(Phase.Jobs, () => Dispatcher.UIThread.RunJobs());
            wall = watch.Elapsed.TotalMicroseconds / Count;
            pause = (GC.GetTotalPauseDuration() - paused).TotalMicroseconds / Count;
            Collections += GC.CollectionCount(0) - gcs;
        }
        finally
        {
            EndNoGcRegion();
        }

        return (Clock.Select(t => t / 1000.0 / Count).ToArray(), Bytes.ToArray(), wall, pause);
    }

    private static int Collections;

    // More than a run allocates (about 52 MB for 300 Buttons), within what the GC allows a no-GC region.
    private const long NoGcBudget = 200L * 1024 * 1024;

    private static void EndNoGcRegion()
    {
        if (System.Runtime.GCSettings.LatencyMode == System.Runtime.GCLatencyMode.NoGCRegion) GC.EndNoGCRegion();
    }

    /// <summary>A copy of a part theme with only the styles whose selector <paramref name="keep"/> keeps; <paramref name="plain"/>, made always true (but <c>:loaded</c>).</summary>
    /// <remarks><paramref name="resources"/>: each setter whose value is a <c>DynamicResource</c> (a binding) sets the
    /// resource's value as it is now instead, so the part looks the same without the binding.
    /// A theme this copy can't reproduce (a base theme, animations of its own, a nested style) fails the bench.</remarks>
    private static ControlTheme Filter(ControlTheme theme, Func<string, bool> keep, bool plain, Find? resources = null)
    {
        if (theme.BasedOn is not null || theme.Animations.Count > 0)
            throw new InvalidOperationException($"The {theme.TargetType?.Name} part theme has a base or animations of its own: Filter would drop them.");
        var copy = new ControlTheme(theme.TargetType!);
        foreach (var setter in theme.Setters) copy.Setters.Add(Value(setter, theme.TargetType!, resources));
        foreach (var child in theme.Children)
        {
            if (child is not Style s) throw new InvalidOperationException($"A part theme holds a {child.GetType().Name}: Filter would drop it.");
            if (s.Children.Count > 0) throw new InvalidOperationException($"{s.Selector} has nested styles: Filter would drop them.");
            if (!keep(s.Selector!.ToString())) continue;
            var c = plain && s.Selector.ToString() != "^:loaded" ? new Style(x => x.Nesting()) : new Style { Selector = s.Selector };
            foreach (var setter in s.Setters) c.Setters.Add(Value(setter, theme.TargetType!, resources));
            foreach (var animation in s.Animations) c.Animations.Add(animation);
            copy.Children.Add(c);
        }

        return copy;
    }

    /// <summary>The setter; or, given <paramref name="resources"/> and a <c>DynamicResource</c> value, a setter of the resource's value now.</summary>
    private static SetterBase Value(SetterBase setter, Type target, Find? resources)
    {
        if (resources is null || setter is not Setter { Value: Avalonia.Data.BindingBase binding, Property: { } property }) return setter;
        if (binding is not Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension { ResourceKey: { } key })
            throw new InvalidOperationException($"{target.Name}.{property.Name} is bound by a {binding.GetType().Name}, not a DynamicResource.");
        var value = resources(key) ?? throw new InvalidOperationException($"No resource {key}.");
        // As the resource binding does: a colour for a brush property becomes a brush.
        if (value is Avalonia.Media.Color colour && typeof(Avalonia.Media.IBrush).IsAssignableFrom(property.PropertyType))
            value = new Avalonia.Media.Immutable.ImmutableSolidColorBrush(colour);
        if (!property.PropertyType.IsInstanceOfType(value))
            throw new InvalidOperationException($"{key} is a {value.GetType().Name}, which {target.Name}.{property.Name} can't take.");
        return new Setter(property, value);
    }

    /// <summary>Whether BaseButton has its theme, and how many class listeners, when <c>Parts.Attach</c> runs.</summary>
    private sealed class ProbeButton : Button
    {
        public string? Seen;

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            var part = e.NameScope.Find<StyledElement>("BaseButton")!;
            var listeners = typeof(Classes).GetProperty("ListenerCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
            Seen = $"at Parts.Attach, BaseButton has a theme: {part.Theme is not null}, class listeners: {listeners.GetValue(part.Classes)}";
            base.OnApplyTemplate(e);
        }
    }

    /// <summary>What one default Button carries: class listeners and value frames, per element.</summary>
    private void Counts()
    {
        var probe = new ProbeButton { Content = "Button" };
        using (new Scene(new StackPanel { Children = { probe } })) output.WriteLine(probe.Seen ?? "no template applied");

        var button = new Button { Content = "Button" };
        using var scene = new Scene(new StackPanel { Children = { button } });
        var listeners = typeof(Classes).GetProperty("ListenerCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var store = typeof(AvaloniaObject).GetMethod("GetValueStore", BindingFlags.NonPublic | BindingFlags.Instance)!;
        foreach (var element in new StyledElement[] { button }.Concat(PartKeys.Select(p => button.Part(p)!)))
        {
            var vs = store.Invoke(element, null)!;
            var frames = (System.Collections.IList)vs.GetType().GetField("_frames", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(vs)!;
            var active = frames.Cast<object>().Count(f => (bool)f.GetType().GetMethod("IsActive")!.Invoke(f, null)!);
            output.WriteLine($"{element.GetType().Name,-22}{element.Name,-37} pseudoclasses {element.Classes.Count,2}, class listeners {listeners.GetValue(element.Classes),4}, value frames {frames.Count,4} ({active} active)");
        }
    }

    /// <summary>The template's <c>Build</c> alone, unstyled: the elements and their bindings.</summary>
    private void BuildAlone()
    {
        using var scene = new Scene();
        var theme = (ControlTheme)scene.Window.FindResource(typeof(Button))!;
        var template = (IControlTemplate)theme.Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value!;
        var clocks = new List<double>();
        long bytes = 0;
        for (var run = 0; run < Runs; run++)
        {
            var owners = Enumerable.Range(0, Count).Select(_ => new Button()).ToArray();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            // As the profile's second table: no collection inside the timed loop.
            if (!GC.TryStartNoGCRegion(NoGcBudget)) throw new InvalidOperationException("No no-GC region.");
            try
            {
                var b = GC.GetAllocatedBytesForCurrentThread();
                var t = Now();
                foreach (var owner in owners) template.Build(owner);
                clocks.Add((Now() - t) / 1000.0 / Count);
                bytes = GC.GetAllocatedBytesForCurrentThread() - b;
            }
            finally
            {
                EndNoGcRegion();
            }
        }

        output.WriteLine($"Template Build alone, no collection: {Median(clocks):F1} {Unit}, {bytes / 1024.0 / Count:F1} KB per Button");
    }

    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.OrderBy(x => x).ToList();
        return sorted[sorted.Count / 2];
    }
}
