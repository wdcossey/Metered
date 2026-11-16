using Avalonia;
using Metered.Sample;
using Metered.Sample.Diagnostics;

namespace Metered.Sample.Desktop;

/// <summary>The desktop head's entry point: the sample's <see cref="App"/>, with its fonts.</summary>
public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
#if DEBUG
        // Avalonia reads the switch once, so it's set before Avalonia starts: the shell's Debug line reads
        // Avalonia's own meter (docs/performance/README.md).
        PerformanceMonitor.EnableDiagnostics();
#endif
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Public and named so, for Avalonia's designer and tools that look for it.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseSampleFonts()
            .LogToTrace();
}
