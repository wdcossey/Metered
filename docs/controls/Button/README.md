# Button

Base Web's `Button` (baseui 18.2.0, `button/`), as `Metered.Controls.Button`, an Avalonia `Button`.

![light](frames/button-gallery-light.png)
![dark](frames/button-gallery-dark.png)

Each row is a kind (primary, secondary, tertiary, dangerPrimary, dangerSecondary, dangerTertiary,
outline), each column a state; then the four sizes with the circle and pill shapes, and two
ButtonGroups. Drawn by `Controls/ButtonFrames` with `METERED_FRAMES` set, in Instrument Sans, the
substitute for Uber Move Text ([ADR 0003](../../decisions/0003-font.md)).

## API

| Base Web | Metered |
|---|---|
| `kind` | `Kind` (`ButtonKind`, with `Outline`, Base Web's button-group-only kind) |
| `size` | `Size`: all seven names (`mini` = `xSmall`, `compact` = `small`, `default` = `medium`) |
| `shape` | `Shape`: all seven names (`default` = `rectangular`, `rounded` = `pill`) |
| `widthType` | `WidthType` |
| `minHitArea` | `MinHitArea` |
| `isLoading` | `IsLoading` (no click while loading) |
| `isSelected` | `IsSelected` |
| `backgroundSafe` | `BackgroundSafe` |
| `startEnhancer`, `endEnhancer` | `StartEnhancer`, `EndEnhancer`; `artworkSize` is `ArtworkSize` |
| `disabled` | `IsEnabled` |
| `onClick` | `Click`, `Command` |
| `overrides` | `Overrides`: `BaseButton` (or `Root`), `StartEnhancer`, `EndEnhancer`, `StartEnhancerButtonContentContainer`, `LoadingSpinnerContainer`, `LoadingSpinner` |
| `colors` | not yet |

## Held by

- `ButtonColourTests`: 308 cases (each kind × 11 states × backgroundSafe × light and dark): the
  `BaseButton` part's background, text colour and box-shadows against Base Web's own style function.
  Hover, press and keyboard focus are real input.
- `ButtonLayoutTests`: 40 cases (each size × shape × width type): padding, radius, sizes, type,
  and the laid-out height.
- `ButtonPartsTests`: the spinner (112 cases), loading, that the loading ring turns (also when its
  look at rest lands before its theme), that it sets no transform at rest, and that `:metered-still` holds it at 0°, the enhancers, `artworkSize`, touch not hovering, the tap band,
  and the parts' mirrored state.
- `ButtonFrames`: the hover overlay's and the focus ring's pixels.

## Known differences

1. **`component` replaces a part's theme, not its element** (ADR 0001).
2. **Loading height** is the larger of the content's and the spinner's; Base Web's is the
   spinner's, as it squashes the content to no height. The same for text at every size.
3. **`StartEnhancerButtonContentContainer` is always there**; Base Web renders it only for
   `widthType="fill"`, so an override of it applies in more cases here.
4. **`outline`'s hover overlay** is Base Web's `::after` (a fill and a ring) drawn as the button's
   own box-shadows, first, on top.
5. **The spinner's arc** ends square; a CSS border's colour changes on a diagonal mitre.
   Its turn (Base Web's `rotate(0deg)` to `rotate(360deg)`, 700 ms, linear, endless) runs only
   while the ring isn't `:metered-still`, a style inside its `ControlTheme` that exists for the
   pictures: `PreviewGallery` sets it, and the ring stands at 0°, as the Spinner's does
   ([`docs/controls/Spinner/`](../Spinner/README.md)). Like Base Web's, the ring sets no transform
   at rest: Avalonia 12.1.4's `TransformAnimator` ignores a turn whose `RenderTransform` is a
   `TransformOperations` (a `rotate(0deg)` setter's value). It puts its own group of transforms
   there, as a local value, which holds 0° while the ring doesn't turn (#77). That local value
   outranks any style, so an override's `RenderTransform` on the ring does nothing; in CSS a
   `transform` on the ring would apply while it doesn't turn.
6. **`round`** (deprecated) is drawn with a 999 radius: Base Web's 50% makes an ellipse of a
   button that isn't square.
7. **`minHitArea="tap"`** widens the Button's bounds to the 48 px band after layout (spikes);
   `Bounds` is the band, the `BaseButton` part keeps the button's own size.
8. **Fonts**: Base Web's stack starts with Uber Move, which can't ship. The label's UberMoveText
   draws in Instrument Sans ([ADR 0003](../../decisions/0003-font.md)): at the label's four sizes its
   width is within 0.53 px of baseweb.design's on average (worst 1.1 px narrower), and its baseline
   sits 0.04 to 0.76 px lower.
