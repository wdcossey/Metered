using System.Diagnostics;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Themes;

namespace Metered.Tests.Performance;

/// <summary>
/// What the theme costs in time, by hand, at each roll-up: a CI machine's timings mean nothing (from Cornflower's). It
/// does nothing unless <c>METERED_BENCH</c> is the absolute path of a file, which gets a row for the trend table in
/// docs/performance/README.md. <c>METERED_BENCH</c> also leaves Avalonia's diagnostics off, as an app has them
/// (<c>TestApp</c>), so this runs on its own. A <see cref="TextBlock"/> and a <see cref="Border"/>, with and without
/// <see cref="BaseTheme"/>, alternating, because warm-up flatters whichever comes second: what the theme costs elements
/// that aren't Metered's. And a default Metered Button, which has no look without the theme. Run it in Release.
/// </summary>
public class StyleCostBenchmark
{
    /// <summary>Names the file for the row; a value that isn't a path (the spikes' <c>1</c>) leaves this alone.</summary>
    internal const string Variable = StyleTrace.BenchVariable;

    private const int Elements = 5000;
    private const int Buttons = 500;
    private const int Depth = 20;
    private const int WarmUpRounds = 4;
    private const int Rounds = 25;

    /// <summary>
    /// Milliseconds to open a window of <paramref name="count"/> of an element, at a depth of <see cref="Depth"/>, with
    /// <paramref name="themes"/> as its styles, made before the clock starts.
    /// </summary>
    private static double Attach(Func<Control> make, int count, IStyle[] themes)
    {
        var panel = new StackPanel();
        for (var i = 0; i < count; i++)
            panel.Children.Add(make());

        Control root = panel;
        for (var i = 0; i < Depth; i++)
            root = new Border { Child = root };

        GC.Collect();
        GC.WaitForPendingFinalizers();
        var stopwatch = Stopwatch.StartNew();
        using var scene = new Scene(root, themes: themes);
        Dispatcher.UIThread.RunJobs();
        stopwatch.Stop();
        // A window with no theme for it could leave its content out of the tree, and time nothing.
        if (TopLevel.GetTopLevel(panel.Children[0]) is null)
            throw new InvalidOperationException("The elements never joined the window.");
        return stopwatch.Elapsed.TotalMilliseconds;
    }

    /// <summary>A file the row could go to, rather than a flag such as the spikes' <c>1</c>.</summary>
    internal static bool LooksLikeAPath(string value) =>
        value.Contains('/') || value.Contains('\\') || value.Contains('.');

    private static double Median(List<double> values) => values.Order().ElementAt(values.Count / 2);

    [AvaloniaFact]
    public void Attach_Time()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { } file || !LooksLikeAPath(file))
            return;
        if (!Path.IsPathFullyQualified(file))
            throw new ArgumentException($"{Variable} must be an absolute file for the row; it is '{file}'.");

#if DEBUG
        Assert.Fail("Run this in Release: a Debug timing in the table would mean nothing.");
#endif
        Assert.False(AppContext.TryGetSwitch(StyleTrace.DiagnosticsSwitch, out var diagnostics) && diagnostics,
            "Avalonia's diagnostics are on: an app has them off, and they cost.");

        var census = StyleCensus.Count(new BaseTheme());
        var cells = new List<string>();
        foreach (var make in new Func<Control>[] { () => new TextBlock { Text = "x" }, () => new Border() })
        {
            var without = new List<double>();
            var with = new List<double>();
            for (var round = 0; round < WarmUpRounds + Rounds; round++)
            {
                var plain = Attach(make, Elements, []);
                var themed = Attach(make, Elements, [new BaseTheme()]);
                if (round >= WarmUpRounds)
                {
                    without.Add(plain);
                    with.Add(themed);
                }
            }

            var before = Median(without) * 1000 / Elements;
            var after = Median(with) * 1000 / Elements;
            // The difference of the values as shown, so the three agree; a difference that rounds to zero is +0.0.
            var delta = Math.Round(after, 1) - Math.Round(before, 1);
            cells.Add(string.Create(CultureInfo.InvariantCulture, $"{before:0.0} to {after:0.0} ({delta:+0.0;-0.0;+0.0})"));
        }

        var buttons = new List<double>();
        for (var round = 0; round < WarmUpRounds + Rounds; round++)
        {
            var time = Attach(() => new Metered.Controls.Button { Content = "x" }, Buttons, [new BaseTheme()]);
            if (round >= WarmUpRounds)
                buttons.Add(time);
        }

        cells.Add(string.Create(CultureInfo.InvariantCulture, $"{Median(buttons) * 1000 / Buttons:0}"));
        var when = DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        File.AppendAllText(file, $"| {when} | {census.Rules} | {census.Alternatives} | {cells[0]} | {cells[1]} | {cells[2]} |{Environment.NewLine}");
    }
}
