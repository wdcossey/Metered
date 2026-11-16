using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Metered.Sample.Desktop;

public static class Program
{
    /// <summary>The app's default family, for text the tokens give none (the headings): Instrument Sans.</summary>
    public const string DefaultFamilyName = "fonts:Metered#Instrument Sans";

    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Public and named so, for Avalonia's designer and tools that look for it.
    // Uber Move is proprietary: Base Web's two families map to the OFL substitutes chosen by
    // measurement (docs/decisions/0003-font.md), embedded from fonts/, as in the tests.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(
                new Uri("fonts:Metered", UriKind.Absolute), new Uri("avares://Metered.Sample.Desktop/Fonts", UriKind.Absolute))))
            .With(new FontManagerOptions
            {
                DefaultFamilyName = DefaultFamilyName,
                FontFamilyMappings = new Dictionary<string, FontFamily>
                {
                    ["UberMoveText"] = new("fonts:Metered#Instrument Sans"),
                    ["UberMove"] = new("fonts:Metered#Figtree"),
                },
            })
            .LogToTrace();
}
