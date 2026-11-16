# Metered — CLAUDE.md

**Metered** is a faithful reproduction of Uber's Base design system (Base Web, npm
`baseui` 18.2.0) for AvaloniaUI, mobile first. Sibling of `R:\source\CornflowerUI` and
`R:\source\Avonic`; the research is `R:\source\baseui-research\README.md`.

This file states **only what exists**. Plans live in `docs/ROADMAP.md`; decisions in
`docs/decisions/`; spike results in `docs/spikes/`.

## Persona

Principal engineer on an AvaloniaUI control library. You care about fidelity to the reference,
correctness on touch devices, and never declaring something that doesn't work. You read Base Web's
implementation, and evaluate its style functions, before writing a line; you build and test before
every commit.

## Repository

| Path | What it is |
|---|---|
| `Metered.sln` | The solution. Libraries `net8.0`, tests `net10.0`; `global.json` pins SDK 10; Avalonia 12.1.4, xunit v3 (`Directory.Packages.props`) |
| `Directory.Build.targets` | Auto-discovery: a project that sets `MeteredDiscoverXaml` gets `XamlManifest.Paths` of its `MeteredDiscoveredXaml` globs (from Cornflower) |
| `package.json` | `baseui` 18.2.0, React 18.3.1, `styletron-react`, `playwright-core`: reference material for `tools/`, never shipped |
| `src/Metered.Controls/` | `Overrides/`: `Override` and `OverrideCollection` (Base Web's `overrides`), and `Parts`, a control's hold on its named template parts: mirrors state pseudoclasses onto them and gives each its theme `{Component}.Styled{Part}` with the control's override merged over it as a copy (ADR 0001). `Controls/Button/`: `Button` (an Avalonia `Button`: Kind, Size, Shape, WidthType, MinHitArea, IsLoading, IsSelected, BackgroundSafe, StartEnhancer, EndEnhancer, ArtworkSize, Overrides; its tap band in `ArrangeCore`; `:loaded` for the transition), its enums, `ButtonLoadingSpinner` (the ring). `Controls/ButtonGroup/`: `ButtonGroup` (an `ItemsControl` of Buttons: Mode, Selected, Kind, Size, Shape, PaddingType, Wrap, Overrides, Click) |
| `src/Metered.Themes/` | `BaseTheme` merges every discovered file. `Tokens/*.g.axaml`: generated, every token under Base Web's name. `Controls/Button.axaml`: the Button's template of named parts. `Controls/Button.Parts.g.axaml`: generated, the parts' themes. `Controls/ButtonGroup.axaml`: the group's template and `ButtonGroup.StyledRoot`. `LICENSE-baseweb.txt`: Base Web's MIT notice, packed |
| `src/Metered/` | Umbrella package. No code |
| `samples/Metered.Sample.Desktop/` | The desktop sample (`net10.0`): one scrolling page of the pilot's controls in a 390 × 844 window, `SimpleTheme` under `BaseTheme`, Inter, a Light/Dark switch. `dotnet run --project samples/Metered.Sample.Desktop` |
| `tests/Metered.Tests/` | Avalonia.Headless with Skia and xunit v3, Inter as the font. `Harness/`: `Scene` (a 390 × 844 window with `BaseTheme`, real mouse, keys and frames), `Frames`, `Oracle` (the embedded `Reference/*.oracle.json`). `Controls/`: Button colours (308 cases), layout (40), parts, spinner (112), frames and gallery (`METERED_FRAMES`), ButtonGroup. `Overrides/`. `Spikes/` (`METERED_BENCH` for the cost) |
| `tools/` | `tokens/gen.js` and `tokens/button.js` (the generated files), `reference/` (Base Web's style functions as the oracle, the cases, CSS conversion, the cascade measured on baseweb.design). See `tools/README.md` |
| `docs/` | `ROADMAP.md`, `decisions/0001` (parts, states and overrides), `spikes/README.md`, `controls/Button/`, `controls/ButtonGroup/` |

## Build

```sh
npm install
dotnet build Metered.sln
dotnet test  Metered.sln
node tools/tokens/gen.js --check
node tools/reference/cases.js --check
```

## Rules

1. **Never hand-edit a `*.g.axaml` or an `*.oracle.json`.** Change the generator and run it.
2. **A part's look lives only in its own `Styled{Part}` theme**, never in the owner's
   `/template/` styles, which would outrank an app's override (ADR 0001).
3. **Every value a test expects comes from Base Web**: the oracle, or a value read from its source
   with the file named.
4. **Avalonia's style priority**: a style with a pseudoclass outranks a plain setter whatever the
   order. A plain setter that must win goes in a style (ADR 0001, item 6).
