using System.Diagnostics.Metrics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Sample.Diagnostics;
using Metered.Themes;

namespace Metered.Tests.Performance;

/// <summary>
/// The sample's line in a Debug build (from Cornflower's). The shell's use of it, with its own time, is held in
/// <c>Sample/PageTimingTests</c>.
/// </summary>
public class PerformanceMonitorTests
{
    // A file names where the row goes; the spikes' flag leaves the benchmark alone; a relative file fails it.
    [Theory]
    [InlineData("1", false)]
    [InlineData("row.md", true)]
    [InlineData("/home/me/row.md", true)]
    [InlineData(@"C:\rows\row", true)]
    public void The_Benchmark_Takes_A_File_And_Leaves_A_Flag(string value, bool file)
    {
        Assert.Equal(file, StyleCostBenchmark.LooksLikeAPath(value));
    }

    [Fact]
    public void The_Line_Has_Each_Number()
    {
        var line = PerformanceMonitor.Format("Button", 41.4, 214, 98, 174, 2.14, 3.44);

        Assert.Equal("Button: 41 ms, 214 elements, 98 rules (174), layout 2.1, render 3.4", line);
    }

    [AvaloniaFact]
    public void It_Reads_The_Pages_Elements_And_The_Time_It_Is_Given()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new StackPanel { Children = { new TextBlock(), new TextBlock() } };

        monitor.Begin();
        host.Content = page;
        Dispatcher.UIThread.RunJobs();
        var line = monitor.Read("a page", page, TimeSpan.FromMilliseconds(123.4));

        Assert.StartsWith("a page: 123 ms, 3 elements, ", line, StringComparison.Ordinal);
    }

    // Against a rename in Avalonia: the line would read zero for ever otherwise.
    [AvaloniaTheory]
    [InlineData(PerformanceMonitor.LayoutMeasure)]
    [InlineData(PerformanceMonitor.LayoutArrange)]
    [InlineData(PerformanceMonitor.Render)]
    public void Avalonias_Meter_Still_Has(string instrument)
    {
        using var scene = new Scene(new Panel());
        var published = new List<string>();
        using var listener = new MeterListener();
        listener.InstrumentPublished = (found, _) =>
        {
            if (found.Meter.Name == "Avalonia.Diagnostic.Meter")
                published.Add(found.Name);
        };
        listener.Start();

        Assert.Contains(instrument, published);
    }

    // Not render: a headless pass with no tick may not have drawn.
    [AvaloniaFact]
    public void Layout_Comes_From_Avalonias_Own_Meter()
    {
        using var monitor = new PerformanceMonitor();
        var host = new ContentControl();
        using var scene = new Scene(host);
        var page = new StackPanel();
        for (var i = 0; i < 200; i++)
            page.Children.Add(new TextBlock { Text = "x" });

        monitor.Begin();
        host.Content = page;
        Dispatcher.UIThread.RunJobs();
        var line = monitor.Read("a page", page, TimeSpan.Zero);

        Assert.DoesNotContain("layout 0.0", line, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Without_Avalonias_Diagnostics_The_Line_Says_So()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        string line;
        // Put back as it was, whatever that is (METERED_BENCH leaves it off).
        var was = PerformanceMonitor.DiagnosticsEnabled;
        AppContext.SetSwitch(PerformanceMonitor.DiagnosticsSwitch, false);
        try
        {
            monitor.Begin();
            line = monitor.Read("a page", page, TimeSpan.Zero);
        }
        finally
        {
            AppContext.SetSwitch(PerformanceMonitor.DiagnosticsSwitch, was);
        }

        Assert.Equal("a page: Avalonia's diagnostics are off. Call PerformanceMonitor.EnableDiagnostics() before Avalonia starts.", line);
    }

    // Large enough that what Avalonia itself reports meanwhile can't change the leading digits.
    [AvaloniaFact]
    public void Measure_And_Arrange_Are_Layout_And_Render_Is_Separate()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);

        monitor.Begin();
        monitor.Record(PerformanceMonitor.LayoutMeasure, 10000);
        monitor.Record(PerformanceMonitor.LayoutArrange, 20000);
        monitor.Record(PerformanceMonitor.Render, 50000);
        Dispatcher.UIThread.RunJobs();
        var line = monitor.Read("a page", page, TimeSpan.Zero);

        Assert.Matches("layout 300[0-9][0-9][.][0-9], render 500[0-9][0-9][.][0-9]$", line);
    }

    // Only the instruments it names: another of Avalonia's, if one were listened to, would not be counted as layout.
    [AvaloniaFact]
    public void An_Instrument_It_Doesnt_Name_Is_Left_Out()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);

        monitor.Begin();
        monitor.Record("avalonia.ui.input.time", 90000);
        var line = monitor.Read("a page", page, TimeSpan.Zero);

        Assert.DoesNotContain("9000", line, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void Times_Start_Again_With_Each_Page()
    {
        using var monitor = new PerformanceMonitor();
        var page = new Panel();
        using var scene = new Scene(page);
        monitor.Record(PerformanceMonitor.LayoutMeasure, 90000);
        monitor.Record(PerformanceMonitor.Render, 90000);

        monitor.Begin();
        Dispatcher.UIThread.RunJobs();
        var line = monitor.Read("a page", page, TimeSpan.Zero);

        Assert.DoesNotContain("9000", line, StringComparison.Ordinal);
    }

    [AvaloniaFact]
    public void A_Disposed_Monitor_Stops_Listening()
    {
        var before = PerformanceMonitor.Listening;
        var monitor = new PerformanceMonitor();
        Assert.Equal(before + 1, PerformanceMonitor.Listening);

        monitor.Dispose();
        monitor.Dispose();
        Assert.Equal(before, PerformanceMonitor.Listening);
    }

    // The application's, as in the sample; the window's, as in a Scene; and the page's own. Not a rule below the page,
    // which costs only its own part. BaseTheme has none.
    [AvaloniaFact]
    public void It_Counts_The_Rules_Of_The_Application_The_Window_And_The_Page()
    {
        static Style Rule(Func<Selector?, Selector> selector)
        {
            var rule = new Style(selector);
            rule.Setters.Add(new Setter(Visual.OpacityProperty, 0.5));
            return rule;
        }

        var appRule = Rule(x => x.OfType<Border>());
        using var monitor = new PerformanceMonitor();
        var below = new Panel { Styles = { Rule(x => x.OfType<Canvas>()) } };
        var page = new Panel { Styles = { Rule(x => x.OfType<TextBox>()) }, Children = { below } };
        using var scene = new Scene(page, themes: [new BaseTheme(), new Styles { Rule(x => Selectors.Or(x.OfType<Image>(), x.OfType<Grid>())) }]);
        Application.Current!.Styles.Add(appRule);
        string line;
        try
        {
            monitor.Begin();
            Dispatcher.UIThread.RunJobs();
            line = monitor.Read("a page", page, TimeSpan.Zero);
        }
        finally
        {
            Application.Current.Styles.Remove(appRule);
        }

        Assert.Contains("3 rules (4)", line, StringComparison.Ordinal);
    }
}
