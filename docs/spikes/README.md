# Spikes

Questions about Avalonia 12.1.4 that the research raised (`R:\source\baseui-research\README.md`
§8.3), answered by tests in `tests/Metered.Tests/`. Measured 2026-11-15.

| # | Question | Answer | Held by |
|---|---|---|---|
| 1 | Does a brush whose `Color` is a `DynamicResource` to another key (a derived token) follow that key when it is overridden? | **App-wide, yes; in a subtree, no.** The brush is one object, resolved where its dictionary is, not where it is used. A subtree theme (Base Web's `ThemeProvider` with `createTheme`) must give every derived key, not only the foundation: the theme builder has to generate whole dictionaries. | `Spikes/DerivedTokensSpike` |
| 2 | What do per-part themes and per-instance overrides cost? | Profiled 2026-11-16: about **0.6 ms** to open a Button, headless, against 0.09 ms for a TextBlock, and **174 KB** allocated against 9 KB (the pilot's 1 to 1.5 ms was a Debug build: its spike gives 1.0 to 1.9 ms a Button in Debug, 0.67 to 0.92 ms in Release). The cost is the **state styles**: Avalonia instances every style of a part's `ControlTheme` on the part, matched or not, each with its activator and a binding per `DynamicResource` setter, about 0.7 KB and 3 kcycles a style. A default Button carries 121 and matches 7; the other 114 are 31 % of its work and 45 % of its allocation, and the GC's pauses, in a page of 300, are a third of its time. Mirroring the pseudoclasses (`Parts.Attach`) is 9 %, the matched styles taking effect; switching it off saves 20 %, the rest being what those styles then draw, which they would cost at any time. Mirroring before the parts are styled saves nothing. An override adds nothing measurable. No reduction within ADR 0001 was found; see `docs/performance/README.md`. | `Spikes/StyleCostSpike`, `Spikes/StyleCostProfile` (`METERED_BENCH=1`) |
| 3 | Can a part's template be replaced after the template is applied? | Its **theme** can (`StyledElement.Theme` set at run time restyles it at once); its element type can't. That decides what `Override.Component` can do (ADR 0001). | `Overrides/OverridesTests` |
| 4 | Are about 15 state pseudoclasses per control a problem for selector matching? | Not for correctness: all 308 colour cases resolve as Base Web's cascade does. For cost, see 2. | `Controls/ButtonColourTests` |
| 5 | Do Avalonia's `BoxShadows` draw Base Web's overlays and rings? | **Yes, pixel for pixel**: `inset 999px 999px 0` is a full-cover tint over the fill and under the content, clipped to the radius; `0 0 0 2px` a ring outside the bounds, which needs the Button's `ClipToBounds` off (Avalonia's Button clips by default). | `Controls/ButtonFrames` |

## Other findings

- **Avalonia's style priority.** A style with a pseudoclass in its selector is a trigger and
  outranks a plain setter whatever the order, so a theme based on another can't replace a value the
  base sets in a state style with a plain setter. ADR 0001, item 6.
- **Hit-testing stops at a parent's bounds.** A child arranged outside its parent isn't found, and
  `ICustomHitTest` can only narrow a visual's bounds, not widen them. Base Web's `minHitArea="tap"`
  (a 48 px band on an absolutely positioned `::before`) is the Button's own bounds widened after
  layout, with its template arranged back in place (`Button.ArrangeCore`).
- **Transitions run on first appearance.** Avalonia transitions a property's first styled value,
  from none, where CSS doesn't transition a first paint. The background transition is set only once
  the button has loaded (`:loaded`).
- **Every style of a `ControlTheme` is instanced on every element that uses it.** Avalonia attaches
  each child style whatever its selector (a pseudoclass can be set later), nests no pruning (a
  nested style is attached even when its parent can't match), and shares an instance between
  elements only when the style has no activator and no binding. A `DynamicResource` setter is a
  binding, made when the style is attached, active or not. A style's `Animations` are applied to
  the element as it is attached, too.
- **Base Web's cascade, measured** on baseweb.design (`tools/reference/precedence.js`): `:active`
  beats `:hover`, which beats `:focus-visible`. Measured on a tertiary button only.
