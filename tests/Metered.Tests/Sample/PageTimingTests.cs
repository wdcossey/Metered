using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Metered.Sample;
using Metered.Sample.Diagnostics;

using static Metered.Tests.Sample.ShellFrames;

namespace Metered.Tests.Sample;

/// <summary>
/// The shell times every page it opens, from before it is made to its first drawn frame, and writes the page's
/// <c>METERED ready</c> line, which a tool that drives the sample reads, so its format is held here: a page has no
/// timing code of its own. A Debug build also writes a <c>METERED cost</c> line after it and shows it under the title.
/// From Cornflower's, without its page transition.
/// </summary>
public class PageTimingTests
{
    /// <summary>Longer than a page this small takes to make, so that a time that includes it can be told apart.</summary>
    private const int SlowMs = 60;

    /// <summary>A page with no timing code, as a control's page is.</summary>
    [SamplePage("plain", Order = 1)]
    private sealed class Plain : Border
    {
        // Hit-testable, so that a point on the page finds it.
        public Plain() => Background = Brushes.Transparent;
    }

    /// <summary>A page that takes <see cref="SlowMs"/> to make, and no timing code either.</summary>
    [SamplePage("slow page", Order = 2)]
    private sealed class Slow : Border
    {
        public Slow() => Thread.Sleep(SlowMs);
    }

    [AvaloniaFact]
    public void Each_Page_Opened_Gets_One_Ready_Line_With_Its_Title_And_What_The_Device_Reports()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        Draw();
        shell.ShowIndex();
        Draw();
        shell.Open("slow page");
        Draw();

        var page = PageSize(shell);
        Assert.Collection(Ready(lines),
            line => Assert.Matches($"^METERED ready open=[0-9]+ms scaling=1 view=390x844 raw=390x844 safe=0,0,0,0 page={page} title=plain$", line),
            line => Assert.Matches($"^METERED ready open=[0-9]+ms scaling=1 view=390x844 raw=390x844 safe=0,0,0,0 page={page} title=slow page$", line));
    }

    // The button on the list goes the same way as Open.
    [AvaloniaFact]
    public void A_Page_Opened_From_The_List_Gets_Its_Ready_Line()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        scene.Click(ListButtons(shell)[^1]);
        Draw();

        Assert.EndsWith(" title=slow page", Assert.Single(Ready(lines)), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Page_With_No_Timing_Code_Is_Timed_From_Before_It_Is_Made()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("slow page");
        Draw();

        var line = Assert.Single(Ready(lines));
        Assert.True(OpenMs(line) >= SlowMs - 10, line);
    }

    // Until the frame that first draws the page has been drawn, the page hasn't opened: nothing is written as it's shown,
    // and the line has the page's laid-out size. Which frame it waits for isn't held here: the headless render timer
    // ticks on real time, so a test can't hold a frame back (a manual one is on the roadmap, with Cornflower's TestClock).
    [AvaloniaFact]
    public void The_Line_Comes_Once_The_Page_Is_Drawn()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        var sinceOpen = System.Diagnostics.Stopwatch.StartNew();
        shell.Open("slow page");
        Assert.Empty(lines);

        Draw();
        var line = Assert.Single(Ready(lines));
        Assert.EndsWith($" page={PageSize(shell)} title=slow page", line, StringComparison.Ordinal);
        Assert.InRange(OpenMs(line), SlowMs - 10, sinceOpen.ElapsedMilliseconds + 1);
    }

    // The sample's own pages are timed the same way, by the shell.
    [AvaloniaFact]
    public void The_Samples_Own_Pages_Are_Timed_By_The_Shell()
    {
        var lines = new List<string>();
        var shell = new SampleShell { Report = lines.Add };
        using var scene = new Scene(shell);

        foreach (var page in SampleShell.Pages)
        {
            shell.Open(page);
            Draw();
        }

        Assert.Equal(SampleShell.Pages.Select(page => $" page={PageSize(shell)} title={page.Title}"),
            Ready(lines).Select(line => line[line.IndexOf(" page=", StringComparison.Ordinal)..]));
    }

    [AvaloniaFact]
    public void A_Page_That_Leaves_Before_Its_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        shell.ShowIndex();
        Draw();

        Assert.Empty(lines);
    }

    [AvaloniaFact]
    public void A_Page_Left_For_Another_Before_Its_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        shell.Open("slow page");
        Draw();

        Assert.EndsWith(" title=slow page", Assert.Single(Ready(lines)), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Shell_That_Leaves_Its_Window_Before_The_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        // In and out with no dispatcher work between, so no render pass can run the frame requests before it leaves.
        shell.Open("plain");
        scene.Window.Content = null;
        Draw();

        Assert.Empty(lines);
    }

    // Back in the same window, the frame requests made before it left are still waiting; its time would include the gap.
    [AvaloniaFact]
    public void A_Shell_That_Leaves_And_Comes_Back_Before_The_Frame_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);

        shell.Open("plain");
        scene.Window.Content = null;
        scene.Window.Content = shell;
        Draw();

        Assert.True(shell.IsShowingPage);
        Assert.Empty(lines);
    }

    // As on a phone: App opens the start page before Avalonia puts the shell in its view.
    [AvaloniaFact]
    public void A_Page_Opened_Before_The_Shell_Is_In_A_Window_Reports_Once_Drawn()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.Open("slow page");
        Assert.Empty(lines);

        using var scene = new Scene(shell);
        Draw();

        var line = Assert.Single(Ready(lines));
        Assert.EndsWith($" page={PageSize(shell)} title=slow page", line, StringComparison.Ordinal);
        Assert.True(OpenMs(line) >= SlowMs - 10, line);
    }

    // On a phone the start page is opened before Avalonia has made the view; the platform's start between isn't the page's.
    [AvaloniaFact]
    public void A_Page_Opened_Before_The_Shell_Is_In_A_Window_Leaves_Out_The_Wait()
    {
        const int WaitMs = 1000;
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.Open("slow page");
        Thread.Sleep(WaitMs);

        using var scene = new Scene(shell);
        Draw();

        var line = Assert.Single(Ready(lines));
        Assert.InRange(OpenMs(line), SlowMs - 10, WaitMs - 1);
    }

    // A Debug build writes what the page cost to open after its ready line, with the shell's time, so the two agree, and
    // shows it under the title, in a bar of one height in every build.
    [AvaloniaFact]
    public void The_Debug_Line_Reads_The_Shells_Time_And_Is_Shown_Under_The_Title()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell);
        var shown = InBar<TextBlock>(shell, "PerformanceLine");
        var barHeight = Bar(shell).Bounds.Height;

        shell.Open("plain");
        Draw();
        scene.Pump();

#if DEBUG
        var cost = string.Create(CultureInfo.InvariantCulture, $"plain: {OpenMs(Ready(lines).Single())} ms, ");
        Assert.Collection(lines,
            line => Assert.StartsWith(SampleShell.ReadyMarker, line, StringComparison.Ordinal),
            line => Assert.StartsWith($"{SampleShell.CostMarker} {cost}", line, StringComparison.Ordinal));
        Assert.True(shown.IsEffectivelyVisible);
        Assert.Equal(lines[1][(SampleShell.CostMarker.Length + 1)..], shown.Text);
        Assert.True(shown.Bounds.Height > 0);

        shell.ShowIndex();
        Assert.False(shown.IsVisible);
#else
        Assert.False(shown.IsVisible);
        Assert.DoesNotContain(lines, line => line.StartsWith(SampleShell.CostMarker, StringComparison.Ordinal));
#endif
        Assert.Equal(barHeight, Bar(shell).Bounds.Height);
    }

#if DEBUG
    // The preview turns the shown line off, as its times would change every picture; the line is still written.
    [AvaloniaFact]
    public void A_Shell_That_Shows_No_Cost_Still_Writes_It()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.ShowsCost = false;
        using var scene = new Scene(shell);

        shell.Open("plain");
        Draw();

        Assert.False(InBar<TextBlock>(shell, "PerformanceLine").IsVisible);
        Assert.Contains(lines, line => line.StartsWith($"{SampleShell.CostMarker} plain: ", StringComparison.Ordinal));
    }

    // Each monitor hears every measurement in the process until it's disposed, so one lives only while its page opens:
    // none for a shell never shown, and none once the page is reported or left.
    [AvaloniaFact]
    public void The_Debug_Monitor_Lives_Only_While_A_Page_Opens_In_A_Window()
    {
        var before = PerformanceMonitor.Listening;
        var lines = new List<string>();

        var neverShown = ShellOf(lines);
        neverShown.Open("plain");
        Assert.Equal(before, PerformanceMonitor.Listening);

        var shell = ShellOf(lines);
        using var scene = new Scene(shell);
        shell.Open("plain");
        Assert.Equal(before + 1, PerformanceMonitor.Listening);
        Draw();
        Assert.Equal(before, PerformanceMonitor.Listening);

        shell.Open("plain");
        shell.ShowIndex();
        Assert.Equal(before, PerformanceMonitor.Listening);

        shell.Open("plain");
        shell.Open("slow page");
        Assert.Equal(before + 1, PerformanceMonitor.Listening);
        scene.Window.Content = null;
        Assert.Equal(before, PerformanceMonitor.Listening);
    }
#endif

    private static SampleShell ShellOf(List<string> lines) =>
        new(SamplePageCatalog.Discover([typeof(Plain), typeof(Slow)])) { Report = lines.Add };

    /// <summary>The ready lines among what the shell wrote.</summary>
    private static List<string> Ready(List<string> lines) =>
        lines.Where(line => line.StartsWith(SampleShell.ReadyMarker, StringComparison.Ordinal)).ToList();

    private static Border Bar(SampleShell shell) => InBar<Border>(shell, "Bar");

    /// <summary>A page's size, as the line writes it: the shell's, under its bar.</summary>
    private static string PageSize(SampleShell shell) =>
        string.Create(CultureInfo.InvariantCulture, $"{shell.Bounds.Width:0.##}x{shell.Bounds.Height - Bar(shell).Bounds.Height:0.##}");

    private static int OpenMs(string line) =>
        int.Parse(Regex.Match(line, "^METERED ready open=([0-9]+)ms ").Groups[1].Value, CultureInfo.InvariantCulture);
}
