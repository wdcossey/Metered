using Avalonia.Controls;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Styling;

namespace Metered.Themes;

/// <summary>
/// Base Web's LightTheme and DarkTheme as Metered's generated token dictionaries hold them
/// (<c>Tokens/*.g.axaml</c>, tools/tokens/gen.js): every key, a solid brush read as its colour. A built
/// theme starts from these, so no default is written twice and none is written outside Tokens/.
/// </summary>
internal static class ThemeDefaults
{
    private static readonly Uri BaseUri = new("avares://Metered.Themes/");

    private static readonly Lazy<(IReadOnlyDictionary<string, object> Light, IReadOnlyDictionary<string, object> Dark)> Loaded =
        new(Load, LazyThreadSafetyMode.ExecutionAndPublication);

    public static IReadOnlyDictionary<string, object> For(ThemeVariant variant) =>
        variant == ThemeVariant.Dark ? Loaded.Value.Dark : Loaded.Value.Light;

    private static (IReadOnlyDictionary<string, object>, IReadOnlyDictionary<string, object>) Load()
    {
        var light = new Dictionary<string, object>(StringComparer.Ordinal);
        var dark = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var path in BaseTheme.OrderForMerge(XamlManifest.Paths).Where(p => p.StartsWith("Tokens/", StringComparison.Ordinal)))
        {
            var include = new ResourceInclude(BaseUri) { Source = new Uri(BaseUri, path) };
            var dictionary = (ResourceDictionary)include.Loaded;
            Read(dictionary, light);
            Read(dictionary, dark);
            foreach (var (variant, provider) in dictionary.ThemeDictionaries)
            {
                var target = variant == ThemeVariant.Light ? light
                    : variant == ThemeVariant.Dark ? dark
                    : throw new InvalidOperationException($"{path}: a theme dictionary for {variant}, which Base Web hasn't");
                Read((ResourceDictionary)provider, target);
            }
        }

        return (light, dark);
    }

    private static void Read(ResourceDictionary dictionary, Dictionary<string, object> into)
    {
        foreach (var key in dictionary.Keys)
        {
            var name = key as string ?? throw new InvalidOperationException($"A token keyed {key}, not by name");
            var value = dictionary[key] ?? throw new InvalidOperationException($"{name}: no value");
            into[name] = value is ISolidColorBrush brush ? brush.Color : value;
        }
    }
}
