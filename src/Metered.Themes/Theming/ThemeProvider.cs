using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Metered.Themes;

/// <summary>
/// Base Web's <c>ThemeProvider</c>: gives an element and everything under it a <see cref="Theme"/> in
/// place of the app's. Attached, so any element can be the subtree's root:
/// <c>&lt;StackPanel metered:ThemeProvider.Theme="{x:Static app:Brand.Theme}"&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The theme's whole dictionary, every token written out, goes into the element's
/// <c>Resources.MergedDictionaries</c>: a derived token can't follow a foundation colour given lower
/// in the tree (docs/spikes, spike 1), so a subtree gets every key, as Base Web's context gives a
/// subtree a whole theme object.
/// </para>
/// <para>
/// As in Base Web, <see cref="ThemeProperty"/> alone fixes the subtree to that theme whatever the app's
/// theme variant. Avalonia switches light and dark by variant, so <see cref="DarkThemeProperty"/> may
/// be given too: then <c>Theme</c> is used in the light variant and <c>DarkTheme</c> in the dark one.
/// </para>
/// </remarks>
public sealed class ThemeProvider
{
    /// <summary>The theme of the element and its subtree (Base Web's <c>theme</c> prop).</summary>
    public static readonly AttachedProperty<Theme?> ThemeProperty =
        AvaloniaProperty.RegisterAttached<ThemeProvider, StyledElement, Theme?>("Theme");

    /// <summary>The theme in the dark variant, when <see cref="ThemeProperty"/> is to be the light one only.</summary>
    public static readonly AttachedProperty<Theme?> DarkThemeProperty =
        AvaloniaProperty.RegisterAttached<ThemeProvider, StyledElement, Theme?>("DarkTheme");

    /// <summary>The dictionary this class put in the element's resources, to take out again.</summary>
    private static readonly AttachedProperty<ResourceDictionary?> ProvidedProperty =
        AvaloniaProperty.RegisterAttached<ThemeProvider, StyledElement, ResourceDictionary?>("Provided");

    static ThemeProvider()
    {
        ThemeProperty.Changed.AddClassHandler<StyledElement>((element, _) => Provide(element));
        DarkThemeProperty.Changed.AddClassHandler<StyledElement>((element, _) => Provide(element));
    }

    private ThemeProvider()
    {
    }

    public static Theme? GetTheme(StyledElement element) => element.GetValue(ThemeProperty);

    public static void SetTheme(StyledElement element, Theme? value) => element.SetValue(ThemeProperty, value);

    public static Theme? GetDarkTheme(StyledElement element) => element.GetValue(DarkThemeProperty);

    public static void SetDarkTheme(StyledElement element, Theme? value) => element.SetValue(DarkThemeProperty, value);

    /// <summary>
    /// The dictionary <see cref="ThemeProvider"/> gives an element: <paramref name="theme"/>'s tokens, or with
    /// <paramref name="darkTheme"/>, each in its variant's theme dictionary. For an app's own
    /// <c>Application.Resources.MergedDictionaries</c>, which isn't an element.
    /// </summary>
    public static ResourceDictionary CreateResources(Theme? theme, Theme? darkTheme = null)
    {
        if (darkTheme is null)
        {
            return theme?.ToResourceDictionary() ?? new ResourceDictionary();
        }

        var dictionary = new ResourceDictionary();
        if (theme is not null)
        {
            dictionary.ThemeDictionaries[ThemeVariant.Light] = theme.ToResourceDictionary();
        }

        dictionary.ThemeDictionaries[ThemeVariant.Dark] = darkTheme.ToResourceDictionary();
        return dictionary;
    }

    private static void Provide(StyledElement element)
    {
        if (element.GetValue(ProvidedProperty) is { } old)
        {
            element.ResourcesChanged -= KeepProvided;
            element.ClearValue(ProvidedProperty);
            element.Resources.MergedDictionaries.Remove(old);
        }

        var theme = GetTheme(element);
        var dark = GetDarkTheme(element);
        if (theme is null && dark is null)
        {
            return;
        }

        var provided = CreateResources(theme, dark);
        element.Resources.MergedDictionaries.Add(provided);
        element.SetValue(ProvidedProperty, provided);
        element.ResourcesChanged += KeepProvided;
    }

    /// <summary>
    /// Gives the theme again when the element's <c>Resources</c> is replaced by a new dictionary, as XAML does
    /// when a <c>&lt;Border.Resources&gt;&lt;ResourceDictionary&gt;</c> property element follows the attribute.
    /// Avalonia raises <c>ResourcesChanged</c> for a replacement before the new dictionary is in place, so
    /// the check runs again once the dispatcher is free.
    /// </summary>
    private static void KeepProvided(object? sender, ResourcesChangedEventArgs e)
    {
        if (sender is StyledElement element)
        {
            Restore(element);
            Dispatcher.UIThread.Post(() => Restore(element));
        }
    }

    /// <summary>A new copy, if the element's dictionary lost the theme: the one given before belongs to the old one.</summary>
    private static void Restore(StyledElement element)
    {
        if (element.GetValue(ProvidedProperty) is { } provided && !element.Resources.MergedDictionaries.Contains(provided))
        {
            var again = CreateResources(GetTheme(element), GetDarkTheme(element));
            element.SetValue(ProvidedProperty, again);
            element.Resources.MergedDictionaries.Add(again);
        }
    }
}
