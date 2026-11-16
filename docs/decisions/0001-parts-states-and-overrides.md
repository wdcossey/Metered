# ADR 0001: parts, states and overrides

Status: accepted for the pilot (Button, ButtonGroup), 2026-11-15.

## Context

Base Web's components are trees of named subcomponents (`BaseButton`, `StartEnhancer`,
`LoadingSpinner`, …). Each subcomponent's style is a function of the theme and the component's `$`
props (`$kind`, `$size`, `$isSelected`, …). An app can change any subcomponent per instance with
`overrides={{ Part: { style, props, component } }}`, and reach a nested Base Web component's own
overrides through `props.overrides`. The project reproduces this faithfully.

## Decision

1. **A part is a named template element under Base Web's name**, with no `PART_` prefix.
   Elements that aren't Base Web parts keep Avalonia's `PART_` names (`PART_ContentPresenter`).
2. **Each part has its own theme**, a `ControlTheme` keyed `{Component}.Styled{Part}`
   (`Button.StyledBaseButton`), the equivalent of Base Web's exported `Styled<Part>`. A part's look
   lives only in its own theme, never in the owner's `/template/` styles, because those would
   outrank a theme the app gives the part.
3. **`$` props are pseudoclasses**, set on the control and mirrored onto every part (`Parts`), so a
   part theme's state styles read them as Base Web's style functions read the props: `:kind-*`,
   `:size-*` (the four sizes by their newer names), `:shape-*`, `:is-selected`, `:is-loading`,
   `:background-safe`, `:width-fill`, `:hit-tap`, `:icon-shape`, `:fixed-shape`, and CSS's own
   `:hover` (a mouse only: Base Web's hover styles sit in `@media (hover: hover)`), `:active` and
   `:focus-visible`. `:disabled` is Avalonia's.
4. **`Overrides` is a collection of `Override { Part, Style, Props, Component }`**, each a
   `ControlTheme` for the part's type. `Component` replaces the part's theme; `Style` and then
   `Props` are merged over it. An alias can reach a part (Button's `Root` is its `BaseButton`), and
   the part's own name wins, as `overrides.BaseButton || overrides.Root` does.
5. **The merge is a copy.** The app's theme is never changed: `Parts` makes a copy based on what it
   merges over, once for each pair, kept while the app's theme lives, so one theme serves any
   number of controls, parts and theme instances. A theme with a `BasedOn` of its own is used as it is.
6. **An override's plain setters win over the default's state styles**, as Base Web's deep merge
   replaces a value however the default reached it. Avalonia gives any style with a pseudoclass in
   its selector trigger priority, above a plain setter's, so in the copy the plain setters become a
   style whose selector is always true but is a trigger, `^:not(:metered-never)`, ahead of the
   override's own state styles.
7. **Nested overrides** are `Props` setting the nested control's `Overrides`. A part with no
   `Styled{Part}` theme (a Metered control inside another) is merged over its type's theme.
8. **Lengths are read from Base Web; colours stay tokens.** The part themes are generated
   (`tools/tokens/button.js`): colours by token name, through `DynamicResource`, so they follow the
   theme variant; lengths read from Base Web's own style functions as numbers.

## Consequences

- An override can't change a part's element type, only its theme (Base Web's `component` can
  render anything). A part that needs that would need a template of its own.
- Every part carries about 30 mirrored pseudoclasses. The pilot measured about 1 to 1.5 ms to
  open a Button headless, against 0.15 ms for a TextBlock (docs/spikes).
- Changing a length token at run time (Base Web's `createTheme({ sizing })`) doesn't reach the
  part themes, whose lengths are numbers. Colours do.
