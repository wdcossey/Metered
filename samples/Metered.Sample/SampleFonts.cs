using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;

namespace Metered.Sample;

/// <summary>
/// The sample's fonts, for every head. Uber Move is proprietary: Base Web's two families map to the OFL substitutes
/// chosen by measurement (docs/decisions/0003-font.md), embedded in this library from <c>fonts/</c>, as in the tests.
/// </summary>
public static class SampleFonts
{
    /// <summary>The collection's name, the tests' too, so a family name finds the same font in both.</summary>
    public const string Collection = "fonts:Metered";

    /// <summary>The app's default family, for text the tokens give none (the headings): Instrument Sans.</summary>
    public const string DefaultFamilyName = Collection + "#Instrument Sans";

    /// <summary>
    /// Embeds the substitutes, maps Base Web's <c>UberMoveText</c> to Instrument Sans and <c>UberMove</c> to Figtree,
    /// and makes Instrument Sans the default family.
    /// </summary>
    public static AppBuilder UseSampleFonts(this AppBuilder builder) =>
        builder
            .ConfigureFonts(fonts => fonts.AddFontCollection(new EmbeddedFontCollection(
                new Uri(Collection, UriKind.Absolute), new Uri("avares://Metered.Sample/Fonts", UriKind.Absolute))))
            .With(new FontManagerOptions
            {
                DefaultFamilyName = DefaultFamilyName,
                FontFamilyMappings = new Dictionary<string, FontFamily>
                {
                    ["UberMoveText"] = new(Collection + "#Instrument Sans"),
                    ["UberMove"] = new(Collection + "#Figtree"),
                },
            });
}
