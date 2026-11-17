using Avalonia.Media;

namespace Metered.Fonts;

/// <summary>
/// The names of the package's fonts, and the mapping of Base Web's families to them. Uber Move is proprietary: Base
/// Web's <c>UberMoveText</c> (its primary family, the Paragraph and Label styles) draws in Instrument Sans and
/// <c>UberMove</c> (its secondary, Heading and Display) in Figtree, chosen by measurement against baseweb.design
/// (docs/decisions/0003-font.md). The theme's tokens keep Base Web's names; the mapping puts these behind them.
/// </summary>
public static class MeteredFonts
{
    /// <summary>The collection's key, <c>fonts:Metered</c>: a family in it is named <c>fonts:Metered#Family</c>.</summary>
    public const string Collection = "fonts:Metered";

    /// <summary>Instrument Sans, which Base Web's <c>UberMoveText</c> draws in.</summary>
    public const string InstrumentSans = Collection + "#Instrument Sans";

    /// <summary>Figtree, which Base Web's <c>UberMove</c> draws in.</summary>
    public const string Figtree = Collection + "#Figtree";

    // Base Web's family names (baseui/themes/shared/typography.js: primaryFontFamily and secondaryFontFamily).
    internal const string UberMoveText = "UberMoveText";
    internal const string UberMove = "UberMove";

    /// <summary>
    /// <paramref name="options"/> with Base Web's two families mapped to the package's fonts, as new options, the app's
    /// left as they were: its default family and its fallbacks (the same list) kept, and its mappings copied with the
    /// two added, its own mapping of either name winning over the package's. The copy matches names whatever their
    /// case, last one winning, as Avalonia's <see cref="FontManager"/> does with the mappings it is given. With no
    /// options, the two mappings alone, and the platform's default family.
    /// </summary>
    internal static FontManagerOptions WithMappings(FontManagerOptions? options)
    {
        var mappings = new Dictionary<string, FontFamily>(StringComparer.OrdinalIgnoreCase)
        {
            [UberMoveText] = new(InstrumentSans),
            [UberMove] = new(Figtree),
        };
        foreach (var (name, family) in options?.FontFamilyMappings ?? new Dictionary<string, FontFamily>())
        {
            mappings[name] = family;
        }

        return new FontManagerOptions
        {
            DefaultFamilyName = options?.DefaultFamilyName,
            FontFallbacks = options?.FontFallbacks,
            FontFamilyMappings = mappings,
        };
    }
}
