using Avalonia;
using Avalonia.Media;
using Metered.Fonts;

namespace Metered.Sample;

/// <summary>
/// The sample's fonts, for every head: Metered.Fonts, which maps Base Web's two families to the OFL substitutes for Uber
/// Move (docs/decisions/0003-font.md), with Instrument Sans as the default family besides.
/// </summary>
public static class SampleFonts
{
    /// <summary>The app's default family, for text the tokens give none (the headings): Instrument Sans.</summary>
    public const string DefaultFamilyName = MeteredFonts.InstrumentSans;

    /// <summary>
    /// The package's fonts and mapping (<c>WithMeteredFonts</c>: <c>UberMoveText</c> to Instrument Sans,
    /// <c>UberMove</c> to Figtree), and Instrument Sans as the default family.
    /// </summary>
    public static AppBuilder UseSampleFonts(this AppBuilder builder) =>
        builder.WithMeteredFonts(new FontManagerOptions { DefaultFamilyName = DefaultFamilyName });
}
