# ADR 0002: the theme builder and the subtree theme

Status: accepted, 2026-11-16.

## Context

Base Web 18.2.0 builds a theme with `createTheme(overrides)` (the same function as
`createLightTheme`) or `createDarkTheme(overrides)`, and gives it to a subtree with
`<ThemeProvider theme={…}>` (`BaseProvider` wraps the same provider). Metered's tokens are generated
dictionaries (`Tokens/*.g.axaml`), Light and Dark by Avalonia's theme variant, merged by `BaseTheme`.
Spike 1 found that a brush whose colour is a `DynamicResource` to another key follows an override
app-wide but not in a subtree: the brush is resolved where its dictionary is.

## What Base Web does

Read in `node_modules/baseui/themes/` and `styles/theme-provider.js`:

1. **One argument.** `createTheme(primitives, overrides)` is the old (v10) signature; 18.2.0's is
   `createTheme(overrides)`. `lightThemePrimitives` and `darkThemePrimitives` are still exported "for
   backward compatibility" and nothing reads them.
2. **Seven foundation colours.** `getFoundationColorOverrides` (`themes/utils.js`) takes `primaryA`,
   `primaryB`, `primary`, `accent`, `negative`, `warning` and `positive` from `overrides.colors`, over
   the theme's own. The semantic colours are a function of that foundation
   (`color-semantic-tokens.js`), the component colours a function of the semantic ones
   (`color-component-tokens.js`). Everything else (the primitives, sizing, typography, borders,
   lighting, animation) is the theme's own.
3. **Then a deep merge** of all the overrides over the result (`utils/deep-merge.js`). A semantic or
   component colour given directly replaces that token only: nothing is derived from it again.
4. **Two derivations aren't aliases.** `borderTransparent` and `borderInverseTransparent` are
   `hexToRgb(primaryA, '0.08')` and `hexToRgb(primaryB, '0.2')`. `hexToRgb` reads only a 3 or
   6 digit hex colour; for anything else the light theme falls back to its own default foundation and
   the dark theme gives `''`, no colour at all.
5. **Shared objects.** The deep merge goes into a `structuredClone` of the theme, which keeps
   `typography.font350` and `typography.LabelMedium` one object (18 such pairs). Overriding
   `LabelMedium.fontSize` changes `font350.fontSize` too.
6. **`ThemeProvider`** is a React context provider: the subtree gets the whole theme object, not a
   merge with the parent's.

## Decision

1. **`Theme`** (`Metered.Themes`) is a built theme: every token Metered's generated dictionaries
   define, under the same key, written out. `BaseTheme.CreateTheme`, `CreateLightTheme` and
   `CreateDarkTheme` take the overrides as a dictionary by resource key
   (`["colors.accent"] = color`, `["sizing.scale600"] = 20.0`): Base Web's nested object flattened to
   the keys Metered already uses, a leaf per key. `BaseTheme.LightTheme` and `BaseTheme.DarkTheme` are
   Base Web's two themes; `BaseTheme.FoundationColors` lists the seven.
2. **The derivation is generated, not ported.** `tools/tokens/theme.js` calls Base Web's own
   `createLightTheme` and `createDarkTheme` with sentinel foundations and reads off which tokens
   follow which foundation colour, and how (itself, or at an alpha with a fallback). The same way, it
   evaluates Base Web's Button style function on a theme whose every colour, lighting and sizing
   value is a sentinel, and reads off what each `button.boxShadow.*` is made of. It also finds the
   shared objects of item 5 above. It writes these as tables of names, no colours, into
   `Theming/ThemeDerivation.g.cs`, and checks them against Base Web's LightTheme and DarkTheme first;
   anything it doesn't recognise throws. Porting the token files by hand would have meant about 600
   lines, light and dark, to keep in step with each baseui release; the probe re-reads them.
3. **The constants are read, not written.** Every colour that doesn't follow the foundation, and
   every other token, starts as the generated dictionaries hold it (`ThemeDefaults` loads
   `Tokens/*.g.axaml`). So no default exists twice, and none is written outside `Tokens/` (rule 5).
   `createTheme({})` is checked to be exactly `LightTheme` before the tables are written, so these
   are the right defaults.
4. **The order is Base Web's**: the foundation, the derived colours, the overrides merged (with the
   shared paths), then the Button's shadows from the merged theme, since Base Web's style function
   reads the finished theme.
5. **`ThemeProvider`** is attached: `ThemeProvider.Theme` on any element puts the theme's whole
   dictionary (`Theme.ToResourceDictionary()`) in that element's `Styles`, as a `Styles` of
   resources and no rule. Not in its `Resources`, which XAML replaces whole when a
   `<Border.Resources><ResourceDictionary>` follows the attribute; `Styles` can't be replaced, and
   the element's own `Resources` still come first. Avalonia looks in an element's styles from the
   last to the first, so the theme is kept last there: a `<Window.Styles><BaseTheme/>` after the
   attribute doesn't outrank it. The brushes are immutable, as one set is shared
   by every dictionary of a theme.
   Every key is a value of its own, so nothing resolves at the app's level (spike 1). Set on a
   window it is app-wide for that window; for `Application.Resources`,
   `ThemeProvider.CreateResources(theme, darkTheme)` gives the same dictionary.

## What Avalonia forced

- **Light and dark by variant.** A Base Web theme is light or dark, and `ThemeProvider` fixes it.
  `ThemeProvider.Theme` alone does the same here, whatever the app's variant. Avalonia apps switch by
  variant, so `ThemeProvider.DarkTheme` may be given too: the two go into the dictionary's
  `ThemeDictionaries`, Light and Dark, and follow the variant as `BaseTheme` does.
- **`Theme` is taken.** A control has `StyledElement.Theme` (its `ControlTheme`). So the builders
  live on `BaseTheme`, not on `Theme`: in a window's code `Theme.CreateTheme(…)` names the window's
  own property and doesn't compile (the sample found it). And there is no `ThemeProvider` element: a
  control's `Theme` property would hide `StyledElement.Theme`. An attached property needs no wrapper
  element and keeps Base Web's name.
- **A dictionary has one owner.** `ToResourceDictionary()` makes a new dictionary each time; the
  brushes in it are made once per theme and shared.
- **Box-shadows are whole values.** `BoxShadows` can't hold a `DynamicResource` colour, which is why
  the Button's are tokens (ADR 0001, item 8). A built theme derives them again from its colours,
  `sizing.scale0` and `lighting.shallowBelow`, as Base Web's style function reads them.
- **Colours are `Color`s, not CSS strings.** `hexToRgb`'s test becomes "opaque": a colour with an
  alpha of its own (`#RRGGBBAA`, `rgba()` in Base Web) isn't a hex colour, and falls back as Base Web
  does. Base Web's dark theme then gives no colour; `CreateDarkTheme` throws, naming the token,
  rather than give one Base Web doesn't.

## Rejected

- **Only the foundation in the subtree**, the rest as `DynamicResource`s to it: spike 1, it doesn't
  follow below the dictionary.
- **A hand port** of `color-semantic-tokens.js` and `color-component-tokens.js`: see item 2.
- **The defaults generated into C#** under `Tokens/`: a second copy of 1,100 values the dictionaries
  already hold.
- **CSS strings as override values**, parsed as Base Web reads them: a port of `css.js` into the
  library for one difference (an opaque `rgb()`, which `hexToRgb` refuses and a `Color` can't tell
  apart from hex).
- **`createTheme(primitives, overrides)`**: not 18.2.0's signature.

## Consequences

- A theme's lengths reach only what reads them by `DynamicResource`. The Button's part themes hold
  their lengths as numbers (ADR 0001), so `sizing` or `typography` overrides don't resize a Button.
  The exceptions: the box-shadows' `sizing.scale0`, derived as above, and what the Button reads in
  code, its `ArtworkSize` (`sizing.scale500` to `scale800`) and its tap band (`sizing.scale1200`),
  which it reads again when its resources change.
- gen.js leaves out an outline Button's `::after` overlay when its colour is the string
  `transparent`. A built theme can't tell an override of `buttonOutlineHoverOverlay` or
  `buttonOutlineActiveOverlay` to `transparent` from any other fully transparent colour, so it keeps
  the overlay's shadows there: invisible, but not the same `BoxShadows` value.
- `tools/reference/themes.js` runs Base Web's `createTheme` and `createDarkTheme` on eight cases into
  `theme.oracle.json`; `ThemeBuilderTests` checks every token of each, `ThemeProviderTests` the
  subtree, the variant switch, app-wide use through `Scene` and through `Application.Resources`
  (`CreateResources`).
