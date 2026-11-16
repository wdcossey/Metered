using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Metered.Tests.Performance;

[assembly: AvaloniaTestApplication(typeof(Metered.Tests.TestApp))]

namespace Metered.Tests;

public class TestApp : Application
{
    // Uber Move isn't available (nor anywhere else here): Base Web's stack falls back, to Inter.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .WithInterFont()
            .With(new FontManagerOptions { DefaultFamilyName = "fonts:Inter#Inter" });

    /// <summary>
    /// Turns on Avalonia's diagnostics, which <see cref="StyleTrace"/> reads. Avalonia reads the switch
    /// once, so it's set as the assembly loads, before Avalonia starts. The cost spike leaves it off,
    /// as an app has it, so <see cref="StyleTrace.ChecksFor"/> refuses to run then.
    /// </summary>
    [ModuleInitializer]
    internal static void EnableDiagnostics()
    {
        if (Environment.GetEnvironmentVariable(StyleTrace.BenchVariable) is null)
        {
            AppContext.SetSwitch(StyleTrace.DiagnosticsSwitch, true);
        }
    }
}
