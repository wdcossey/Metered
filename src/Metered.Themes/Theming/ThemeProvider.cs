using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Metered.Themes;

/// <summary>
/// Base Web's <c>ThemeProvider</c>: gives an element and everything under it a <see cref="Theme"/> in
/// place of the app's. Attached, so any element can be the subtree's root:
/// <c>&lt;StackPanel metered:ThemeProvider.Theme="{x:Static app:Brand.Theme}"&gt;</c>.
/// </summary>
/// <remarks>
/// <para>
/// The theme's whole dictionary, every token written out, goes into the element's <c>Styles</c>, as a
/// <see cref="Styles"/> of resources and no rule: a derived token can't follow a foundation colour given
/// lower in the tree (docs/spikes, spike 1), so a subtree gets every key, as Base Web's context gives a
/// subtree a whole theme object. <c>Styles</c> rather than <c>Resources</c>, which XAML or code can
/// replace whole (a <c>&lt;Border.Resources&gt;&lt;ResourceDictionary&gt;</c> after the attribute);
/// <c>Styles</c> can't be replaced, and the element's own <c>Resources</c> still come first. Avalonia
/// looks in an element's styles from the last to the first, so the theme is kept last: a
/// <c>&lt;Window.Styles&gt;&lt;BaseTheme/&gt;</c> added after the attribute doesn't outrank it, and
/// one taken out (<c>Styles.Clear()</c>) is put back.
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

    /// <summary>What this class put in the element's styles, to take out again.</summary>
    private static readonly AttachedProperty<Styles?> ProvidedProperty =
        AvaloniaProperty.RegisterAttached<ThemeProvider, StyledElement, Styles?>("Provided");

    /// <summary>The handler that keeps <see cref="ProvidedProperty"/> last in the element's styles.</summary>
    private static readonly AttachedProperty<NotifyCollectionChangedEventHandler?> KeeperProperty =
        AvaloniaProperty.RegisterAttached<ThemeProvider, StyledElement, NotifyCollectionChangedEventHandler?>("Keeper");

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
        if (element.GetValue(KeeperProperty) is { } keeper)
        {
            element.Styles.CollectionChanged -= keeper;
            element.ClearValue(KeeperProperty);
        }

        if (element.GetValue(ProvidedProperty) is { } old)
        {
            element.ClearValue(ProvidedProperty);
            element.Styles.Remove(old);
        }

        var theme = GetTheme(element);
        var dark = GetDarkTheme(element);
        if (theme is null && dark is null)
        {
            return;
        }

        var provided = new Styles { Resources = CreateResources(theme, dark) };
        element.SetValue(ProvidedProperty, provided);
        element.Styles.Add(provided);
        NotifyCollectionChangedEventHandler keep = (_, _) => KeepLast(element.Styles, provided);
        element.SetValue(KeeperProperty, keep);
        element.Styles.CollectionChanged += keep;
    }

    /// <summary>
    /// The theme last in the element's styles, where Avalonia looks first; put back if taken out. One change at
    /// a time: the change raises this again, and then finds the theme last.
    /// </summary>
    private static void KeepLast(Styles styles, Styles provided)
    {
        var index = styles.IndexOf(provided);
        if (index < 0)
        {
            styles.Add(provided);
        }
        else if (index != styles.Count - 1)
        {
            styles.Move(index, styles.Count - 1);
        }
    }
}
