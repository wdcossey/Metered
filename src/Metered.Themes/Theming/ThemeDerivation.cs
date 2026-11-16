namespace Metered.Themes;

/// <summary>
/// The shapes of the tables tools/tokens/theme.js generates into <c>ThemeDerivation.g.cs</c> from
/// Base Web's own createTheme, createDarkTheme and Button (ADR 0002). Names only, no colours.
/// </summary>
internal static partial class ThemeDerivation;

/// <summary>What Base Web's semantic colours give where a foundation colour isn't a hex colour.</summary>
internal enum AlphaFallback
{
    /// <summary>The theme's own default foundation colour at the alpha (createTheme: <c>hexToRgb(x) || hexToRgb(default)</c>).</summary>
    Default,

    /// <summary>No colour: Base Web writes <c>''</c> (createDarkTheme: <c>hexToRgb(x) || ''</c>).</summary>
    None,
}

/// <summary>
/// A colour token that follows a foundation colour: the foundation itself, or, with an alpha, the
/// foundation's red, green and blue at that alpha (Base Web's <c>hexToRgb(foundation, alpha)</c>).
/// </summary>
internal sealed record ColourRule(string Key, string From, double? Alpha = null, AlphaFallback Fallback = AlphaFallback.Default);

/// <summary>A shadow's length: a number of pixels, or a <c>sizing.*</c> token.</summary>
internal readonly record struct ShadowLength(double Pixels, string? Sizing)
{
    public static implicit operator ShadowLength(double pixels) => new(pixels, null);

    public static implicit operator ShadowLength(string sizing) => new(0, sizing);
}

/// <summary>One shadow of a list: its lengths and colour token, or a whole <c>lighting.*</c> token spliced in.</summary>
internal sealed record ShadowTerm(bool Inset, ShadowLength OffsetX, ShadowLength OffsetY, ShadowLength Blur, ShadowLength Spread, string Colour)
{
    public string? LightingKey { get; private init; }

    public static ShadowTerm Lighting(string key) => new(false, 0, 0, 0, 0, "") { LightingKey = key };
}

/// <summary>A box-shadow token (<c>button.boxShadow.*</c>) and what it is made of.</summary>
internal sealed record ShadowRule(string Key, ShadowTerm[] Terms);
