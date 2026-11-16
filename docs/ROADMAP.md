# Roadmap

## Scope

Base Web 18.2.0 (npm `baseui`, pinned in `package.json`), mobile first, desktop supported. The
research behind it: `R:\source\baseui-research\README.md`.

## Pilot (done, 2026-11-15)

Button and ButtonGroup, to settle the approach before the inventory:

- [x] Tokens generated from Base Web's themes (`tools/tokens/gen.js`)
- [x] Base Web's style functions as the test oracle (`tools/reference/`)
- [x] Parts, mirrored states and the overrides API (ADR 0001)
- [x] Button: kinds, sizes, shapes, loading, selected, enhancers, background-safe, fill, tap area
- [x] ButtonGroup: radio, checkbox, keyboard, padding, wrap, Root override
- [x] Spikes 1 to 5 (`docs/spikes/`)

## Next, before more controls

1. **Cost.** Profile opening a Button (about 1 to 1.5 ms headless); the mirrored pseudoclasses and
   the generated state styles are the suspects.
2. **A theme builder.** Base Web's `createTheme` / `createDarkTheme`, and a subtree theme: spike 1
   says it must write every derived key, not only the foundation.
3. **A font.** An OFL substitute for Uber Move, chosen by metrics against baseweb.design, which
   serves Uber Move.
4. **The sample and the real-window run**, as Cornflower's and Avonic's. A first desktop sample
   exists (`samples/Metered.Sample.Desktop`, one page); still to come: a page per control, an
   Android head, and a scripted real-window run.
5. **CI**: build, test, and `node tools/tokens/gen.js --check` and `node tools/reference/cases.js --check`.

## Inventory

By the research's grouping (`R:\source\baseui-research\README.md` §5): first the controls that
theme Avalonia's (checkbox, radio, switch, input, textarea, slider, progress bar, spinner, tabs,
tooltip, popover, menu, select), then the small new ones, then the overlays and navigation.
