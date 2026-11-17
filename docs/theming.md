# Theming

Base Web's `createTheme`, `createDarkTheme` and `ThemeProvider`, for Avalonia. The design and what
differs from Base Web: [ADR 0002](decisions/0002-theme-builder.md).

## Build a theme

Overrides are keyed as Metered's tokens are (`colors.<name>`, `sizing.<name>`,
`typography.<style>.fontSize`, `borders.<name>`, `lighting.<name>`), each a value of the token's own
type: a `Color` (or a solid brush) for a colour, a `double` for a length or a font size, a
`CornerRadius` for a radius, `BoxShadows` for lighting.

```csharp
using Metered.Themes;

var brand = BaseTheme.CreateTheme(new Dictionary<string, object>
{
    // The foundation: Base Web derives the semantic and component colours from these seven
    // (BaseTheme.FoundationColors): primaryA, primaryB, primary, accent, negative, warning, positive.
    ["colors.primaryA"] = Color.Parse("#1F2A44"),
    ["colors.accent"] = Color.Parse("#7356BF"),
    // Any other token replaces that token only, after the derivation, as Base Web's deep merge does.
    ["colors.buttonPrimaryFill"] = Color.Parse("#0B3D2E"),
    ["sizing.scale0"] = 3.0,
});
var brandDark = BaseTheme.CreateDarkTheme(new Dictionary<string, object> { ["colors.accent"] = Color.Parse("#9F8AE0") });
```

`BaseTheme.CreateTheme` is `BaseTheme.CreateLightTheme`, as in Base Web. `BaseTheme.LightTheme` and
`BaseTheme.DarkTheme` are Base Web's own, what `BaseTheme` gives. A built theme's tokens are in
`theme.Tokens` (colours as `Color`).

Some of Base Web's tokens are one object under two names (`typography.font350` is
`typography.LabelMedium`): an override of either changes both, and if the overrides give both, the
one later in the dictionary's order wins, as in Base Web's deep merge.

## Give it to a subtree

```xml
<Border x:Name="Branded"> … </Border>
```

```csharp
ThemeProvider.SetTheme(Branded, brand);          // the subtree is brand, whatever the variant
ThemeProvider.SetDarkTheme(Branded, brandDark);  // optional: brand in light, brandDark in dark
```

In XAML, `metered:ThemeProvider.Theme="{x:Static app:Themes.Brand}"` with
`xmlns:metered="using:Metered.Themes"`. Set on a window, it is app-wide for that window; for the
application itself:

```csharp
Application.Current!.Resources.MergedDictionaries.Add(ThemeProvider.CreateResources(brand, brandDark));
```

`BaseTheme` still has to be in the styles: it holds the controls' templates, and a theme only
replaces their tokens. With the theme in `Application.Resources`, `BaseTheme` goes in
`Application.Styles`, as in the sample's `App.axaml`: the application looks in its resources before
its styles, but a `BaseTheme` in a window's styles is found before the application, so its default
tokens win in that window (measured; `ThemeProviderTests` covers the application's way). Clearing `ThemeProvider.Theme`, and `ThemeProvider.DarkTheme` if it was
given, gives the subtree back the app's tokens: a `DarkTheme` left alone still holds in the dark
variant.

## What doesn't change

- **A Button's lengths.** Its part themes hold their lengths as numbers (ADR 0001), so a `sizing`
  or `typography` override doesn't resize it. Its box-shadows do follow `sizing.scale0`, and its
  `ArtworkSize` (`sizing.scale500` to `scale800`) and tap band (`sizing.scale1200`) follow theirs.
- **A foundation colour with an alpha** isn't a hex colour to Base Web: the two tokens it takes at
  an alpha (`borderTransparent`, `borderInverseTransparent`) fall back to the light theme's default,
  and in the dark theme Base Web gives no colour at all, so `CreateDarkTheme` throws.
