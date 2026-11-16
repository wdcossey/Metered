using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Spikes;

/// <summary>
/// Spikes 2 and 4 (research §8.3): what the part themes' state styles (about 120 for BaseButton)
/// and a per-instance override cost. Runs only when METERED_BENCH is set; prints the times.
/// </summary>
public class StyleCostSpike(ITestOutputHelper output)
{
    private const int Count = 300;

    [AvaloniaFact]
    public void Open_a_page_of_buttons()
    {
        if (Environment.GetEnvironmentVariable("METERED_BENCH") is null) return;
        var style = new ControlTheme(typeof(Border));
        style.Setters.Add(new Setter(Border.BackgroundProperty, Brushes.Red));

        foreach (var (name, make) in new (string, Func<int, Control>)[]
        {
            ("Avalonia TextBlock (floor)", i => new TextBlock { Text = "Button " + i }),
            ("Metered Button", i => new Button { Content = "Button " + i }),
            ("Metered Button, an override each", i => new Button { Content = "Button " + i, Overrides = [new Override { Part = "BaseButton", Style = style }] }),
            ("Metered Button, state changes", i => new Button { Content = "Button " + i, Kind = (ButtonKind)(i % 7), IsSelected = i % 2 == 0 }),
        })
        {
            var times = new List<double>();
            for (var run = 0; run < 5; run++)
            {
                var panel = new WrapPanel { Orientation = Orientation.Horizontal };
                using var scene = new Scene(panel, width: 1200, height: 4000);
                var watch = Stopwatch.StartNew();
                for (var i = 0; i < Count; i++) panel.Children.Add(make(i));
                scene.Pump();
                times.Add(watch.Elapsed.TotalMilliseconds);
            }

            times.Sort();
            output.WriteLine($"{name}: {times[2]:F1} ms for {Count} (median of 5), {times[2] / Count * 1000:F0} µs each");
        }
    }
}
