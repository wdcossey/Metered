using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;

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
}
