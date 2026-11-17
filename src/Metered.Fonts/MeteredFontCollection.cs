using Avalonia.Media.Fonts;

namespace Metered.Fonts;

/// <summary>
/// The package's faces, Instrument Sans and Figtree at 400 to 700, as one collection under
/// <see cref="MeteredFonts.Collection"/>: Avalonia.Fonts.Inter's <c>InterFontCollection</c>, for these.
/// <see cref="Avalonia.MeteredFontsAppBuilderExtensions.WithMeteredFonts"/> adds it.
/// </summary>
public sealed class MeteredFontCollection : EmbeddedFontCollection
{
    /// <summary>Where the faces are in the assembly.</summary>
    internal const string Assets = "avares://Metered.Fonts/Assets";

    public MeteredFontCollection()
        : base(new Uri(MeteredFonts.Collection, UriKind.Absolute), new Uri(Assets, UriKind.Absolute))
    {
    }
}
