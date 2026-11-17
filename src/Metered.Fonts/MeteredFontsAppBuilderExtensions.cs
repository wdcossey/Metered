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
    /// Call it once.
    /// </summary>
    /// <remarks>
    /// Avalonia takes the mapping only from <see cref="FontManagerOptions"/>, and keeps only the last one an
    /// <see cref="AppBuilder"/> is given. So an app's own options go through this method, never to
    /// <see cref="AppBuilder.With{T}(T)"/> as well: given there after this call they drop the mapping, given there
    /// before it they are dropped themselves; and a second call of this method drops the first one's options.
    /// Merging whatever was given at setup instead would need Avalonia's private API.
    /// </remarks>
    /// <param name="builder">The app's builder.</param>
    /// <param name="options">
    /// The app's own font options, if it has any: its default family, fallbacks and mappings are kept, and its own
    /// mapping of either name wins. Left out, the default family stays the platform's.
    /// </param>
    public static AppBuilder WithMeteredFonts(this AppBuilder builder, FontManagerOptions? options = null) =>
        builder
            .ConfigureFonts(fonts => fonts.AddFontCollection(new MeteredFontCollection()))
            .With(MeteredFonts.WithMappings(options));
}
