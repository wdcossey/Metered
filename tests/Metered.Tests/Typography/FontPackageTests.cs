using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using Metered.Fonts;

namespace Metered.Tests.Typography;

/// <summary>
/// Metered.Fonts (#9): the package an app adds for the substitutes ADR 0003 chose for Uber Move. The test app's fonts
/// are only the package's, set up by its <c>WithMeteredFonts()</c> and nothing else (<see cref="TestApp"/>), so what
/// draws here is what an app using the package draws. The expected families aren't Base Web's, whose value is Uber
/// Move itself: they are the substitutes the ADR chose by measurement; Base Web's names are
/// <c>baseui/themes/shared/typography.js</c>'s <c>primaryFontFamily</c> and <c>secondaryFontFamily</c>.
/// </summary>
public class FontPackageTests
{
    // Each of Base Web's two families, at every weight the package carries a face for (ADR 0003: 400 to 700).
    public static TheoryData<string, int, string> Faces => new()
    {
        { "UberMoveText", 400, TestApp.TextFamily },
        { "UberMoveText", 500, TestApp.TextFamily },
        { "UberMoveText", 600, TestApp.TextFamily },
        { "UberMoveText", 700, TestApp.TextFamily },
        { "UberMove", 400, TestApp.DisplayFamily },
        { "UberMove", 500, TestApp.DisplayFamily },
        { "UberMove", 600, TestApp.DisplayFamily },
        { "UberMove", 700, TestApp.DisplayFamily },
    };

    [AvaloniaTheory]
    [MemberData(nameof(Faces))]
    public void A_text_in_Base_Webs_family_draws_in_its_substitute_at_its_weight(string family, int weight, string expected)
    {
        var text = new TextBlock { Text = "Button 1234", FontFamily = new FontFamily(family), FontWeight = (FontWeight)weight };
        using var scene = new Scene(new StackPanel { Children = { text } });

        var runs = text.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>().ToList();
        Assert.NotEmpty(runs);
        Assert.All(runs, run =>
        {
            var face = run.GlyphRun.GlyphTypeface;
            Assert.Equal(expected, FontSubstituteTests.Family(face));
            Assert.Equal((FontWeight)weight, face.Weight);
            Assert.Equal(FontSimulations.None, face.FontSimulations);
        });
    }

    // The package's resources are the eight faces in fonts/, and nothing else: not a licence, not a stray file.
    [AvaloniaFact]
    public void The_package_carries_the_eight_faces_and_nothing_else()
    {
        var names = AssetLoader.GetAssets(new Uri(MeteredFontCollection.Assets, UriKind.Absolute), null)
            .Select(uri => uri.AbsolutePath[(uri.AbsolutePath.LastIndexOf('/') + 1)..])
            .Order(StringComparer.Ordinal);

        Assert.Equal(
        [
            "Figtree-Bold.ttf", "Figtree-Medium.ttf", "Figtree-Regular.ttf", "Figtree-SemiBold.ttf",
            "InstrumentSans-Bold.ttf", "InstrumentSans-Medium.ttf", "InstrumentSans-Regular.ttf", "InstrumentSans-SemiBold.ttf",
        ], names);
    }

    // With no options of the app's, the package's are its two mappings alone, and the default family the platform's.
    [Fact]
    public void Without_the_apps_options_the_package_maps_the_two_families_and_leaves_the_default()
    {
        var options = MeteredFonts.WithMappings(null);

        Assert.Null(options.DefaultFamilyName);
        Assert.Null(options.FontFallbacks);
        Assert.Equal(2, options.FontFamilyMappings!.Count);
        Assert.Equal(new FontFamily(MeteredFonts.InstrumentSans), options.FontFamilyMappings["UberMoveText"]);
        Assert.Equal(new FontFamily(MeteredFonts.Figtree), options.FontFamilyMappings["UberMove"]);
    }

    // Avalonia keeps the last FontManagerOptions it's given, so the app's go through the package: all of them kept, its
    // own mapping of a family winning, and the app's object left as it was.
    [Fact]
    public void The_apps_options_are_kept_and_its_own_mapping_wins()
    {
        var fallbacks = new List<FontFallback> { new() { FontFamily = new FontFamily("Arial") } };
        var own = new Dictionary<string, FontFamily> { ["Brand"] = new("Arial"), ["ubermove"] = new("Georgia") };
        var app = new FontManagerOptions { DefaultFamilyName = MeteredFonts.InstrumentSans, FontFallbacks = fallbacks, FontFamilyMappings = own };

        var options = MeteredFonts.WithMappings(app);

        Assert.Equal(MeteredFonts.InstrumentSans, options.DefaultFamilyName);
        Assert.Same(fallbacks, options.FontFallbacks);
        Assert.Equal(3, options.FontFamilyMappings!.Count);
        Assert.Equal(new FontFamily(MeteredFonts.InstrumentSans), options.FontFamilyMappings["UberMoveText"]);
        Assert.Equal(new FontFamily("Georgia"), options.FontFamilyMappings["UberMove"]);
        Assert.Equal(new FontFamily("Arial"), options.FontFamilyMappings["Brand"]);
        Assert.Same(own, app.FontFamilyMappings);
        Assert.Equal(2, own.Count);
    }

    // The copy names each of FontManagerOptions' properties: one Avalonia adds would be dropped, so this goes red first.
    [Fact]
    public void The_copy_carries_every_option_Avalonia_has()
    {
        Assert.Equal(
            ["DefaultFamilyName", "FontFallbacks", "FontFamilyMappings"],
            typeof(FontManagerOptions).GetProperties().Where(p => p.CanWrite).Select(p => p.Name).Order(StringComparer.Ordinal));
    }
}
