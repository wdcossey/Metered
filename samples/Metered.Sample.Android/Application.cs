using Android.Runtime;
using Avalonia;
using Avalonia.Android;

namespace Metered.Sample.Android;

/// <summary>
/// Builds the app, as the desktop head's <c>Program</c> does: the sample's fonts (Instrument Sans and Figtree, for Base
/// Web's UberMoveText and UberMove), and in a Debug build Avalonia's diagnostics, so the shell's <c>METERED cost</c> line
/// reads Avalonia's own meter.
/// </summary>
[global::Android.App.Application]
public class Application : AvaloniaAndroidApplication<App>
{
#if DEBUG
    // Avalonia reads the switch once, so it's set before Avalonia starts, first of all (docs/performance/README.md).
    static Application() => Diagnostics.PerformanceMonitor.EnableDiagnostics();
#endif

    protected Application(nint javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override AppBuilder CustomizeAppBuilder(AppBuilder builder) =>
        base.CustomizeAppBuilder(builder).UseSampleFonts().LogToTrace();
}
