using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;

namespace Metered.Themes;

/// <summary>
/// A Base Web theme: every token Metered's generated dictionaries define, under the same keys
/// (<c>colors.buttonPrimaryFill</c>, <c>sizing.scale600</c>, <c>button.boxShadow.primary.hover</c>),
/// written out whole, so it can be given to a subtree (<see cref="ThemeProvider"/>) as well as to an
/// app. Built by <see cref="BaseTheme.CreateTheme"/>, <see cref="BaseTheme.CreateLightTheme"/> and
/// <see cref="BaseTheme.CreateDarkTheme"/>, as Base Web's functions of those names build one (ADR 0002).
/// </summary>
/// <remarks>
/// Colours are held as <see cref="Color"/> and given to the resources as solid brushes, as the
/// generated dictionaries give them; every other value has the generated dictionaries' type
/// (<see cref="double"/> for sizing and font sizes, <see cref="CornerRadius"/> for radii,
/// <see cref="BoxShadows"/> for lighting, and so on).
/// </remarks>
public sealed class Theme
{
    private readonly Dictionary<string, object> _tokens;
    private Dictionary<string, object>? _resources;

    private Theme(string name, ThemeVariant variant, Dictionary<string, object> tokens)
    {
        Name = name;
        Variant = variant;
        _tokens = tokens;
        Tokens = new ReadOnlyDictionary<string, object>(tokens);
    }

    /// <summary>Base Web's <c>theme.name</c>: <c>light-theme</c> or <c>dark-theme</c>.</summary>
    public string Name { get; }

    /// <summary>The Base Web theme this one was built from: <see cref="ThemeVariant.Light"/> or <see cref="ThemeVariant.Dark"/>.</summary>
    public ThemeVariant Variant { get; }

    /// <summary>Every token, by its resource key. Colours are <see cref="Color"/>s.</summary>
    public IReadOnlyDictionary<string, object> Tokens { get; }

    /// <summary>A token by its resource key.</summary>
    public object this[string key] => _tokens[key];

    /// <summary>
    /// A resource dictionary of every token, each a value of its own (the brushes are shared between the
    /// dictionaries of one theme). Give it to an app's or a control's <c>Resources.MergedDictionaries</c>;
    /// <see cref="ThemeProvider"/> does this for a subtree.
    /// </summary>
    public ResourceDictionary ToResourceDictionary()
    {
        // Immutable: one brush is shared by every dictionary of the theme, so none may change it, and it
        // belongs to no thread.
        _resources ??= _tokens.ToDictionary(p => p.Key, p => p.Value is Color c ? new Avalonia.Media.Immutable.ImmutableSolidColorBrush(c) : p.Value, StringComparer.Ordinal);
        var dictionary = new ResourceDictionary();
        foreach (var (key, value) in _resources)
        {
            dictionary.Add(key, value);
        }

        return dictionary;
    }

    public override string ToString() => $"{Name} ({_tokens.Count} tokens)";

    /// <summary>createLightTheme or createDarkTheme (BaseTheme's builders): the order is Base Web's (ADR 0002, item 4).</summary>
    internal static Theme Create(ThemeVariant variant, IReadOnlyDictionary<string, object>? overrides)
    {
        var dark = variant == ThemeVariant.Dark;
        var name = dark ? "dark-theme" : "light-theme";
        var rules = dark ? ThemeDerivation.Dark : ThemeDerivation.Light;
        var defaults = ThemeDefaults.For(variant);
        var merged = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var (key, value) in overrides ?? ReadOnlyDictionary<string, object>.Empty)
        {
            merged[key] = Normalise(key, value, defaults);
        }

        // The foundation: the theme's own, with the overrides' foundation colours over it
        // (getFoundationColorOverrides).
        var foundation = new Dictionary<string, Color>(StringComparer.Ordinal);
        foreach (var f in ThemeDerivation.FoundationColors)
        {
            var key = "colors." + f;
            foundation[f] = merged.TryGetValue(key, out var o) ? (Color)o : (Color)defaults[key];
        }

        // The semantic and component colours, derived from it.
        var tokens = new Dictionary<string, object>(defaults, StringComparer.Ordinal);
        foreach (var rule in rules)
        {
            tokens[rule.Key] = Derive(name, rule, foundation, defaults);
        }

        // deepMerge(theme, overrides), into one object wherever Base Web's theme has one object under
        // two paths (typography.font350 is typography.LabelMedium), in the overrides' order.
        foreach (var (key, value) in merged)
        {
            tokens[key] = value;
            foreach (var alias in SharedPaths(key))
            {
                tokens[alias] = value;
            }
        }

        // The Button's shadows, from the merged theme, as Base Web's style function reads it. A shadow
        // given by key (a Metered token, not Base Web's) is kept.
        foreach (var rule in ThemeDerivation.Shadows)
        {
            if (!merged.ContainsKey(rule.Key))
            {
                tokens[rule.Key] = Shadows(rule, tokens);
            }
        }

        return new Theme(name, variant, tokens);
    }

    /// <summary>The same token under the other paths of a shared object, if the key is under one.</summary>
    private static IEnumerable<string> SharedPaths(string key)
    {
        foreach (var paths in ThemeDerivation.SharedObjects)
        {
            foreach (var path in paths)
            {
                if (key.Length > path.Length && key[path.Length] == '.' && key.StartsWith(path, StringComparison.Ordinal))
                {
                    var leaf = key[path.Length..];
                    return paths.Where(p => p != path).Select(p => p + leaf);
                }
            }
        }

        return [];
    }

    private static Color Derive(string theme, ColourRule rule, Dictionary<string, Color> foundation, IReadOnlyDictionary<string, object> defaults)
    {
        var from = foundation[rule.From];
        if (rule.Alpha is not { } alpha)
        {
            return from;
        }

        // hexToRgb(x, alpha) reads a hex colour, so an opaque one: one with an alpha of its own isn't.
        if (from.A != 255)
        {
            if (rule.Fallback == AlphaFallback.None)
            {
                throw new ArgumentException(
                    $"{theme}: colors.{rule.From} has an alpha, and Base Web's {rule.Key} is then no colour ('').",
                    "overrides");
            }

            from = (Color)defaults["colors." + rule.From];
        }

        // As tools/reference/css.js reads rgba(): the alpha times 255, rounded half up.
        return new Color((byte)Math.Round(alpha * 255, MidpointRounding.AwayFromZero), from.R, from.G, from.B);
    }

    private static BoxShadows Shadows(ShadowRule rule, Dictionary<string, object> tokens)
    {
        var list = new List<BoxShadow>();
        foreach (var term in rule.Terms)
        {
            if (term.LightingKey is { } lighting)
            {
                var shadows = (BoxShadows)tokens[lighting];
                for (var i = 0; i < shadows.Count; i++)
                {
                    list.Add(shadows[i]);
                }

                continue;
            }

            list.Add(new BoxShadow
            {
                IsInset = term.Inset,
                OffsetX = Length(term.OffsetX, tokens),
                OffsetY = Length(term.OffsetY, tokens),
                Blur = Length(term.Blur, tokens),
                Spread = Length(term.Spread, tokens),
                Color = (Color)tokens[term.Colour],
            });
        }

        return list.Count == 0 ? default : new BoxShadows(list[0], list.Skip(1).ToArray());
    }

    private static double Length(ShadowLength length, Dictionary<string, object> tokens) =>
        length.Sizing is { } key ? (double)tokens[key] : length.Pixels;

    /// <summary>
    /// An override as its token holds it: a brush's colour, a number as a double, any easing for a curve
    /// (Base Web's is any CSS timing function); else of the token's own type.
    /// </summary>
    private static object Normalise(string key, object value, IReadOnlyDictionary<string, object> defaults)
    {
        ArgumentNullException.ThrowIfNull(value, key);
        if (value is ISolidColorBrush brush)
        {
            value = brush.Opacity == 1
                ? brush.Color
                : throw new ArgumentException($"{key}: a brush with an opacity; give the colour its alpha instead.", "overrides");
        }

        if (!defaults.TryGetValue(key, out var token))
        {
            return value;
        }

        if (token is double && value is sbyte or byte or short or ushort or int or uint or long or ulong or float or decimal)
        {
            return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
        }

        if (token is Avalonia.Animation.Easings.Easing && value is Avalonia.Animation.Easings.Easing)
        {
            return value;
        }

        return token.GetType().IsInstanceOfType(value)
            ? value
            : throw new ArgumentException($"{key}: a {value.GetType().Name}, where Base Web's token is a {token.GetType().Name}.", "overrides");
    }
}
