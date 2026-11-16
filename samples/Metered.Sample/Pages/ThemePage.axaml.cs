using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Metered.Themes;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's <c>ThemeProvider</c>: Buttons under the app's theme, and the same in a subtree given its own, built by
/// <c>createTheme</c> and <c>createDarkTheme</c> from three foundation colours (docs/theming.md).
/// </summary>
[SamplePage("Theme", Order = SamplePageAttribute.AfterTheControls)]
public partial class ThemePage : UserControl
{
    /// <summary>The subtree's light foundation: Base Web's <c>createTheme({ colors: { primaryA, primaryB, accent } })</c>.</summary>
    internal static readonly IReadOnlyDictionary<string, object> LightFoundation = new Dictionary<string, object>
    {
        ["colors.primaryA"] = Color.Parse("#1F2A44"),
        ["colors.primaryB"] = Color.Parse("#FFFDF7"),
        ["colors.accent"] = Color.Parse("#7356BF"),
    };

    /// <summary>The subtree's dark foundation, for <c>createDarkTheme</c>.</summary>
    internal static readonly IReadOnlyDictionary<string, object> DarkFoundation = new Dictionary<string, object>
    {
        ["colors.primaryA"] = Color.Parse("#F5F1E8"),
        ["colors.primaryB"] = Color.Parse("#121826"),
        ["colors.accent"] = Color.Parse("#9F8AE0"),
    };

    /// <summary>The subtree's themes, built once, so that opening the page again doesn't build them again.</summary>
    private static readonly Lazy<(Theme Light, Theme Dark)> Built = new(() =>
        (BaseTheme.CreateTheme(LightFoundation), BaseTheme.CreateDarkTheme(DarkFoundation)));

    public ThemePage()
    {
        InitializeComponent();

        // Every semantic and component colour follows the foundation.
        ThemeProvider.SetTheme(Branded, Built.Value.Light);
        ThemeProvider.SetDarkTheme(Branded, Built.Value.Dark);
    }

    private void OnLoad(object? sender, RoutedEventArgs e) => ButtonPage.Load((Metered.Controls.Button)sender!);
}
