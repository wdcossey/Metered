using Avalonia.Markup.Xaml.Styling;
using Avalonia.Styling;

namespace Metered.Themes;

/// <summary>
/// Base Web's themes, Light and Dark by Avalonia's theme variant: every token under Base Web's own
/// name (<c>colors.buttonPrimaryFill</c>, <c>sizing.scale600</c>, <c>typography.LabelMedium.fontSize</c>),
/// and the controls' themes. Add it after any other theme.
/// </summary>
public class BaseTheme : Styles
{
    private static readonly Uri BaseUri = new("avares://Metered.Themes/");

    public BaseTheme(IServiceProvider? _ = null)
    {
        foreach (var path in OrderForMerge(XamlManifest.Paths))
        {
            Resources.MergedDictionaries.Add(new ResourceInclude(BaseUri) { Source = new Uri(BaseUri, path) });
        }
    }

    /// <summary>The order the files are merged in: ordinal, so it is the same on every machine.</summary>
    internal static IEnumerable<string> OrderForMerge(IEnumerable<string> paths) => paths.Order(StringComparer.Ordinal);
}
