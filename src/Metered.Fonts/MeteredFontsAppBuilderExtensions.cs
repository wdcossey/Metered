using Avalonia.Media;
using Metered.Fonts;

// In Avalonia's namespace, as Avalonia.Fonts.Inter's WithInterFont is: an app's entry point already has `using Avalonia;`.
namespace Avalonia;

/// <summary>The package's <see cref="AppBuilder"/> extension, after Avalonia.Fonts.Inter's <c>WithInterFont</c>.</summary>
public static class MeteredFontsAppBuilderExtensions
{
    /// <summary>
    /// Adds the package's fonts (<see cref="MeteredFontCollection"/>) and maps Base Web's <c>UberMoveText</c> to
    /// Instrument Sans and <c>UberMove</c> to Figtree, the substitutes for Uber Move (docs/decisions/0003-font.md).
    /// </summary>
    /// <param name="builder">The app's builder.</param>
    /// <param name="options">
    /// The app's own font options, if it has any: its default family, fallbacks and mappings are kept, and its own
    /// mapping of either name wins. Give them here rather than to <see cref="AppBuilder.With{T}(T)"/>: Avalonia keeps
    /// only the last <see cref="FontManagerOptions"/> it is given, so a later one would drop the mapping. Left out, the
    /// default family stays the platform's.
    /// </param>
    public static AppBuilder WithMeteredFonts(this AppBuilder builder, FontManagerOptions? options = null) =>
        builder
            .ConfigureFonts(fonts => fonts.AddFontCollection(new MeteredFontCollection()))
            .With(MeteredFonts.WithMappings(options));
}
