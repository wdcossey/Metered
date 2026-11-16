using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using Metered.Tests.Performance;

[assembly: AvaloniaTestApplication(typeof(Metered.Tests.TestApp))]

namespace Metered.Tests;

public class TestApp : Application
{
    /// <summary>The embedded OFL substitutes for Uber Move (<c>fonts/</c>, linked into <c>Fonts/</c>).</summary>
    public const string FontCollection = "fonts:Metered";

    public const string TextFamily = "Instrument Sans";

    public const string DisplayFamily = "Figtree";

    // Uber Move is proprietary. Base Web's two families map to the substitutes chosen by measurement
    // against baseweb.design (docs/decisions/0003-font.md): UberMoveText, the primary family, to
    // Instrument Sans; UberMove, the secondary (display) family, to Figtree. The default family stays
    // the platform's, unlike the sample's: a family Avalonia can't find falls back to the default, so a
    // default of Instrument Sans would hide a missing UberMoveText mapping (every text here takes a
    // family from the tokens; FontSubstituteTests checks the default isn't a substitute).
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<TestApp>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
            .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(
                new Uri(FontCollection, UriKind.Absolute), new Uri("avares://Metered.Tests/Fonts", UriKind.Absolute))))
            .With(new FontManagerOptions
            {
                FontFamilyMappings = new Dictionary<string, FontFamily>
                {
                    ["UberMoveText"] = new($"{FontCollection}#{TextFamily}"),
                    ["UberMove"] = new($"{FontCollection}#{DisplayFamily}"),
                },
            });

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
