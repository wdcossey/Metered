using Avalonia.Styling;

namespace Metered.Themes;

// Base Web's theme builders, on BaseTheme rather than on Theme: in a control's code `Theme` is the
// control's own StyledElement.Theme property, so `Theme.CreateTheme(...)` wouldn't compile there (ADR 0002).
public partial class BaseTheme
{
    // PublicationOnly: a build that throws (the defaults loaded off the UI thread) isn't kept.
    private static readonly Lazy<Theme> Light = new(() => CreateLightTheme(), LazyThreadSafetyMode.PublicationOnly);
    private static readonly Lazy<Theme> Dark = new(() => CreateDarkTheme(), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Base Web's <c>LightTheme</c>: what <see cref="BaseTheme"/> gives in the light variant, as a <see cref="Theme"/>.</summary>
    public static Theme LightTheme => Light.Value;

    /// <summary>Base Web's <c>DarkTheme</c>: what <see cref="BaseTheme"/> gives in the dark variant, as a <see cref="Theme"/>.</summary>
    public static Theme DarkTheme => Dark.Value;

    /// <summary>
    /// The foundation colours (Base Web's <c>primaryA</c>, <c>primaryB</c>, <c>primary</c>, <c>accent</c>,
    /// <c>negative</c>, <c>warning</c>, <c>positive</c>): the overrides the other colours are derived from.
    /// </summary>
    public static IReadOnlyList<string> FoundationColors { get; } = Array.AsReadOnly(ThemeDerivation.FoundationColors);

    /// <summary>Base Web's <c>createTheme</c>: <see cref="CreateLightTheme"/>, as Base Web exports it.</summary>
    public static Theme CreateTheme(IReadOnlyDictionary<string, object>? overrides = null) => CreateLightTheme(overrides);

    /// <summary>
    /// Base Web's <c>createLightTheme(overrides)</c>: LightTheme, with the semantic and component colours
    /// derived again from the foundation colours in <paramref name="overrides"/>, then every override
    /// merged over the result, as Base Web's <c>deepMerge</c> does.
    /// </summary>
    /// <param name="overrides">
    /// Tokens by resource key: <c>["colors.accent"] = color</c>, <c>["sizing.scale600"] = 20.0</c>. A key
    /// Base Web's theme hasn't is kept, as <c>deepMerge</c> keeps it.
    /// </param>
    /// <exception cref="ArgumentException">An override isn't of its token's type.</exception>
    public static Theme CreateLightTheme(IReadOnlyDictionary<string, object>? overrides = null) =>
        Theme.Create(ThemeVariant.Light, overrides);

    /// <summary>Base Web's <c>createDarkTheme(overrides)</c>: as <see cref="CreateLightTheme"/>, from DarkTheme.</summary>
    /// <exception cref="ArgumentException">
    /// An override isn't of its token's type; or a foundation colour that Base Web's dark theme takes at an
    /// alpha isn't opaque, for which Base Web gives no colour at all (<c>hexToRgb(x) || ''</c>).
    /// </exception>
    public static Theme CreateDarkTheme(IReadOnlyDictionary<string, object>? overrides = null) =>
        Theme.Create(ThemeVariant.Dark, overrides);
}
