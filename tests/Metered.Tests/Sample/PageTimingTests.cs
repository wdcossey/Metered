using System.Globalization;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;
using Metered.Sample;
using Metered.Sample.Diagnostics;

using static Metered.Tests.Sample.ShellFrames;

namespace Metered.Tests.Sample;

/// <summary>
/// The shell times every page it opens, from before it is made to its first drawn frame, and writes the page's
/// <c>METERED ready</c> line, which a tool that drives the sample reads, so its format is held here: a page has no
/// timing code of its own. A Debug build also writes a <c>METERED cost</c> line after it and shows it in a footer over
/// the page's foot, which takes nothing from the page's size.
/// From Cornflower's, without its page transition. Its scenes are on real time, not the test's clock: the shell times a
/// page by a stopwatch, and its wait for a view's size is a dispatcher timer these tests run in the dispatcher's own loop.
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
        using var scene = new Scene(shell, realTime: true);

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
        using var scene = new Scene(shell, realTime: true);

        scene.Click(ListButtons(shell)[^1]);
        Draw();

        Assert.EndsWith(" title=slow page", Assert.Single(Ready(lines)), StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Page_With_No_Timing_Code_Is_Timed_From_Before_It_Is_Made()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell, realTime: true);

        shell.Open("slow page");
        Draw();

        var line = Assert.Single(Ready(lines));
        Assert.True(OpenMs(line) >= SlowMs - 10, line);
    }

    // Until the frame that first draws the page has been drawn, the page hasn't opened: nothing is written as it's shown,
    // and the line has the page's laid-out size. Which frame it waits for isn't held here: the headless render timer
    // ticks on its own, so a test can't hold a frame back (a manual one is on the roadmap, #13).
    [AvaloniaFact]
    public void The_Line_Comes_Once_The_Page_Is_Drawn()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell, realTime: true);

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
        using var scene = new Scene(shell, realTime: true);

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
        using var scene = new Scene(shell, realTime: true);

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
        using var scene = new Scene(shell, realTime: true);

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
        using var scene = new Scene(shell, realTime: true);

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
        using var scene = new Scene(shell, realTime: true);

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

        using var scene = new Scene(shell, realTime: true);
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

        using var scene = new Scene(shell, realTime: true);
        Draw();

        var line = Assert.Single(Ready(lines));
        Assert.InRange(OpenMs(line), SlowMs - 10, WaitMs - 1);
    }

    // As on a phone's cold start (#37): the start page is opened, and the shell put in its view, while the view is still
    // Android's 1 × 1 placeholder; Avalonia's frames come all the same, as they're the process's, not the view's. The
    // line waits for the view's size and the frames after it, so the page it reports has been laid out at that size. The
    // wait is the platform's, so it's left out of the page's time.
    [AvaloniaFact]
    public void A_Page_Opened_While_The_View_Has_No_Size_Reports_Once_Laid_Out_At_Its_Size()
    {
        const int WaitMs = 500;
        var sinceOpen = System.Diagnostics.Stopwatch.StartNew();
        var lines = new List<string>();
        var pageAsWritten = new List<Size>();
        var shell = ShellOf(lines);
        shell.Report = line =>
        {
            lines.Add(line);
            pageAsWritten.Add(shell.Shown!.Bounds.Size);
        };
        shell.Open("plain");
        using var scene = new Scene(shell, width: 1, height: 1, realTime: true);

        Draw();
        Draw();
        Assert.Equal(new Size(1, 1), scene.Window.ClientSize);
        Assert.Empty(lines);
        Thread.Sleep(WaitMs);

        scene.Window.Width = Scene.Width;
        scene.Window.Height = Scene.Height;
        Dispatcher.UIThread.RunJobs();
        Draw();

        var line = Assert.Single(Ready(lines));
        var page = PageSize(shell);
        Assert.Matches($"^METERED ready open=[0-9]+ms scaling=1 view=390x844 raw=390x844 safe=0,0,0,0 page={page} title=plain$", line);
        Assert.Equal(new Size(Scene.Width, Scene.Height - Bar(shell).Bounds.Height), pageAsWritten[0]);
        Assert.InRange(OpenMs(line), 0, sinceOpen.ElapsedMilliseconds - WaitMs + 1);
    }

    // A view that gets its size and loses it again before the frames after it waits again, for the size it gets next.
    [AvaloniaFact]
    public void A_View_That_Loses_Its_Size_Before_The_Frames_After_It_Waits_Again()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.Open("plain");
        using var scene = new Scene(shell, width: 1, height: 1, realTime: true);
        Draw();

        // Only the resizes: a layout pass asks the headless window for its size, which it gives at Send priority; the
        // frames the first asks for come after both.
        scene.Window.Width = Scene.Width;
        scene.Window.Height = Scene.Height;
        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Send);
        Assert.Equal(new Size(Scene.Width, Scene.Height), scene.Window.ClientSize);
        scene.Window.Width = 1;
        scene.Window.Height = 1;
        scene.Window.UpdateLayout();
        Dispatcher.UIThread.RunJobs(DispatcherPriority.Send);
        Assert.Equal(new Size(1, 1), scene.Window.ClientSize);
        Draw();
        Assert.Empty(lines);

        scene.Window.Width = Scene.Width;
        scene.Window.Height = Scene.Height;
        Dispatcher.UIThread.RunJobs();
        Draw();

        var line = Assert.Single(Ready(lines));
        Assert.Matches($"^METERED ready open=[0-9]+ms scaling=1 view=390x844 raw=390x844 safe=0,0,0,0 page={PageSize(shell)} title=plain$", line);
    }

    // A page left while it waits for its view's size says nothing, even once its wait would have run out.
    [AvaloniaFact]
    public void A_Page_Left_While_It_Waits_For_A_Size_Reports_Nothing()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.SizeWait = TimeSpan.FromMilliseconds(100);
        using var scene = new Scene(shell, width: 1, height: 1, realTime: true);

        shell.Open("plain");
        Draw();
        shell.ShowIndex();
        // Past the wait's end, run by the dispatcher's own loop as a timer is.
        using (var after = new CancellationTokenSource(TimeSpan.FromMilliseconds(400)))
            Dispatcher.UIThread.MainLoop(after.Token);
        Draw();

        Assert.Empty(lines);
    }

    // A view that never gets a size still has its line, once the wait is over, as it is: its view= and page= say so, and
    // its time is the page's own, without the wait.
    [AvaloniaFact]
    public void A_Page_Whose_View_Never_Gets_A_Size_Is_Reported_As_It_Is_Once_The_Wait_Is_Over()
    {
        var lines = new List<string>();
        using var written = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var shell = ShellOf(lines);
        shell.SizeWait = TimeSpan.FromMilliseconds(300);
        shell.Report = line =>
        {
            lines.Add(line);
            written.Cancel();
        };
        using var scene = new Scene(shell, width: 1, height: 1, realTime: true);

        var sinceOpen = System.Diagnostics.Stopwatch.StartNew();
        shell.Open("plain");
        Draw();
        Assert.Empty(lines);

        // The wait's end is a dispatcher timer, which only the dispatcher's own loop runs.
        Dispatcher.UIThread.MainLoop(written.Token);

        var line = Assert.Single(Ready(lines));
        Assert.Matches("^METERED ready open=[0-9]+ms scaling=1 view=1x1 raw=1x1 safe=0,0,0,0 page=1x0 title=plain$", line);
        Assert.InRange(OpenMs(line), 0, sinceOpen.ElapsedMilliseconds - 300 + 1);
    }

    // A Debug build writes what the page cost to open after its ready line, with the shell's time, so the two agree, and
    // shows it in its footer, already shown as the ready line is written, so that a frame taken after that line has it;
    // a Release build has neither.
    [AvaloniaFact]
    public void The_Debug_Line_Reads_The_Shells_Time_And_Is_Shown_In_The_Footer_Before_The_Ready_Line()
    {
        var lines = new List<string>();
        var shownAsWritten = new List<bool>();
        var shell = ShellOf(lines);
        var footer = Footer(shell);
        var shown = InShell<TextBlock>(shell, "PerformanceLine");
        shell.Report = line =>
        {
            lines.Add(line);
            shownAsWritten.Add(footer.IsVisible);
        };
        using var scene = new Scene(shell, realTime: true);

        shell.Open("plain");
        Draw();
        scene.Pump();

#if DEBUG
        var cost = string.Create(CultureInfo.InvariantCulture, $"plain: {OpenMs(Ready(lines).Single())} ms, ");
        Assert.Collection(lines,
            line => Assert.StartsWith(SampleShell.ReadyMarker, line, StringComparison.Ordinal),
            line => Assert.StartsWith($"{SampleShell.CostMarker} {cost}", line, StringComparison.Ordinal));
        Assert.Equal([true, true], shownAsWritten);
        Assert.True(shown.IsEffectivelyVisible);
        Assert.Equal(lines[1][(SampleShell.CostMarker.Length + 1)..], shown.Text);
        Assert.True(shown.Bounds.Height > 0);
#else
        Assert.False(footer.IsVisible);
        Assert.False(shown.IsEffectivelyVisible);
        Assert.Equal([false], shownAsWritten);
        Assert.DoesNotContain(lines, line => line.StartsWith(SampleShell.CostMarker, StringComparison.Ordinal));
#endif
    }

    // The footer lies over the page's foot, the view's whole width: the page and the bar keep their sizes as it's shown,
    // as they do in every build (docs/performance/README.md). Shown by hand, so that a Release build checks it too.
    [AvaloniaFact]
    public void The_Footer_Lies_Over_The_Pages_Foot_Across_The_View_And_Takes_Nothing_From_It()
    {
        var shell = HandShown();
        using var scene = new Scene(shell, realTime: true);
        var page = new Border();
        shell.ShowPage(page, "a page");
        Draw();
        scene.Pump();
        var bar = Bar(shell).Bounds.Height;
        var before = InWindow(page, scene);
        Assert.Equal(new Rect(0, bar, Scene.Width, Scene.Height - bar), before);

        var footer = Footer(shell);
        shell.ShowCost("a page: 12 ms, 34 elements, 3 rules (3), layout 5.6, render 7.8");
        scene.Pump();

        Assert.True(footer.IsEffectivelyVisible);
        Assert.True(footer.Bounds.Height > 0);
        Assert.Equal(new Rect(0, Scene.Height - footer.Bounds.Height, Scene.Width, footer.Bounds.Height), InWindow(footer, scene));
        Assert.True(InWindow(footer, scene).Top > before.Top);
        Assert.Equal(before, InWindow(page, scene));
        Assert.Equal(bar, Bar(shell).Bounds.Height);
    }

    // The page has the view under the bar as it's shown and once its line is, in every build: the footer is laid over
    // it, not beside it, and the bar keeps its height.
    [AvaloniaFact]
    public void A_Page_Is_The_View_Under_The_Bar_Before_And_After_Its_Line_In_Every_Build()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        using var scene = new Scene(shell, realTime: true);
        var bar = Bar(shell).Bounds.Height;
        var under = new Rect(0, bar, Scene.Width, Scene.Height - bar);

        shell.Open("plain");
        scene.Window.UpdateLayout();
        var page = shell.Shown!;
        Assert.Empty(lines);
        Assert.Equal(under, InWindow(page, scene));

        Draw();
        scene.Pump();
        Assert.NotEmpty(Ready(lines));
        Assert.Equal(under, InWindow(page, scene));
        Assert.Equal(bar, Bar(shell).Bounds.Height);
#if DEBUG
        Assert.True(Footer(shell).IsEffectivelyVisible);
#else
        Assert.False(Footer(shell).IsVisible);
#endif
    }

    // On a phone's width a line can be longer than the view: it wraps onto as many lines as it needs, never trimmed, and
    // the footer grows over the page to hold them, the page's size as it was.
    [AvaloniaFact]
    public void A_Long_Line_Wraps_In_The_Footer_Rather_Than_Being_Cut()
    {
        const string Title = "a page whose title alone is longer than a phone's view is wide, so its line must wrap";
        var shell = HandShown();
        using var scene = new Scene(shell, realTime: true);
        var footer = Footer(shell);
        var shown = InShell<TextBlock>(shell, "PerformanceLine");
        var page = new Border();
        shell.ShowPage(page, Title);
        Draw();
        scene.Pump();
        var before = InWindow(page, scene);

        shell.ShowCost($"{Title}: 196 ms, 326 elements, 3 rules (3), layout 61.0, render 29.8");
        scene.Pump();

        Assert.Equal(TextTrimming.None, shown.TextTrimming);
        Assert.True(shown.TextLayout.TextLines.Count >= 2, $"{shown.TextLayout.TextLines.Count} line(s)");
        Assert.True(shown.Bounds.Height >= shown.TextLayout.Height, $"{shown.Bounds.Height} against {shown.TextLayout.Height}");
        Assert.True(shown.TextLayout.WidthIncludingTrailingWhitespace <= shown.Bounds.Width + 0.5,
            $"{shown.TextLayout.WidthIncludingTrailingWhitespace} against {shown.Bounds.Width}");
        Assert.True(footer.Bounds.Height > shown.TextLayout.TextLines[0].Height * 2);
        Assert.Equal(Scene.Width, footer.Bounds.Width);
        Assert.Equal(before, InWindow(page, scene));
    }

    // A press on the footer never reaches a control it hides; a tap hides it, and then what was under it has the press.
    [AvaloniaFact]
    public void A_Tap_On_The_Footer_Hides_It_And_Uncovers_The_Page()
    {
        var shell = HandShown();
        using var scene = new Scene(shell, realTime: true);
        var page = new Border { Background = Brushes.Transparent };
        var pressed = 0;
        page.PointerPressed += (_, _) => pressed++;
        shell.ShowPage(page, "a page");
        Draw();
        shell.ShowCost("a page: 12 ms, 34 elements, 3 rules (3), layout 5.6, render 7.8");
        scene.Pump();
        var footer = Footer(shell);
        var at = scene.CentreOf(footer);

        scene.Click(footer);
        Assert.False(footer.IsVisible);
        Assert.Equal(0, pressed);
        Assert.True(shell.IsShowingPage);

        // Hidden until the next page: the frames after the tap don't bring it back.
        Draw();
        Assert.False(footer.IsVisible);

        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        scene.Pump();
        Assert.Equal(1, pressed);
    }

    // The footer is the page's: leaving the page hides it and its line, and the next page has none until its own.
    [AvaloniaFact]
    public void Leaving_A_Page_Hides_Its_Footer()
    {
        var shell = ShellOf([]);
        using var scene = new Scene(shell, realTime: true);
        var footer = Footer(shell);

        shell.ShowPage(new Border(), "a page");
        shell.ShowCost("a page: 12 ms");
        shell.ShowIndex();
        Assert.False(footer.IsVisible);
        Assert.Null(InShell<TextBlock>(shell, "PerformanceLine").Text);

        shell.ShowCost("a page: 12 ms");
        shell.ShowPage(new Border(), "another page");
        Assert.False(footer.IsVisible);
    }

#if DEBUG
    // The preview turns the shown line off, as its times would change every picture; the line is still written.
    [AvaloniaFact]
    public void A_Shell_That_Shows_No_Cost_Still_Writes_It()
    {
        var lines = new List<string>();
        var shell = ShellOf(lines);
        shell.ShowsCost = false;
        using var scene = new Scene(shell, realTime: true);

        shell.Open("plain");
        Draw();
        scene.Pump();

        Assert.False(Footer(shell).IsVisible);
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
        using var scene = new Scene(shell, realTime: true);
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

    /// <summary>
    /// A shell whose footer shows only what a test gives <see cref="SampleShell.ShowCost"/>, in any build: a Debug build's
    /// own line, once a page is ready, isn't shown.
    /// </summary>
    private static SampleShell HandShown() => new(SamplePageCatalog.Discover([typeof(Plain), typeof(Slow)])) { Report = _ => { }, ShowsCost = false };

    private static SampleShell ShellOf(List<string> lines) =>
        new(SamplePageCatalog.Discover([typeof(Plain), typeof(Slow)])) { Report = lines.Add };

    /// <summary>The ready lines among what the shell wrote.</summary>
    private static List<string> Ready(List<string> lines) =>
        lines.Where(line => line.StartsWith(SampleShell.ReadyMarker, StringComparison.Ordinal)).ToList();

    private static Border Bar(SampleShell shell) => InShell<Border>(shell, "Bar");

    private static Border Footer(SampleShell shell) => InShell<Border>(shell, "Footer");

    /// <summary>Where <paramref name="visual"/> is in the scene's window.</summary>
    private static Rect InWindow(Visual visual, Scene scene) =>
        new(visual.TranslatePoint(default, scene.Window) ?? throw new InvalidOperationException("Not in the window."), visual.Bounds.Size);

    /// <summary>A page's size, as the line writes it: the shell's, under its bar.</summary>
    private static string PageSize(SampleShell shell) =>
        string.Create(CultureInfo.InvariantCulture, $"{shell.Bounds.Width:0.##}x{shell.Bounds.Height - Bar(shell).Bounds.Height:0.##}");

    private static int OpenMs(string line) =>
        int.Parse(Regex.Match(line, "^METERED ready open=([0-9]+)ms ").Groups[1].Value, CultureInfo.InvariantCulture);
}
