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
| `.github/workflows/ci.yml` | CI on push to `main` and on pull requests: `build-test` (Release build and tests) and `generated` (the Node tools' syntax, and both `--check` runs) |
| `Directory.Build.targets` | Auto-discovery: a project that sets `MeteredDiscoverXaml` gets `XamlManifest.Paths` of its `MeteredDiscoveredXaml` globs (from Cornflower) |
| `package.json` | `baseui` 18.2.0, React 18.3.1, `styletron-react`, `playwright-core`: reference material for `tools/`, never shipped |
| `src/Metered.Controls/` | `Overrides/`: `Override` and `OverrideCollection` (Base Web's `overrides`), and `Parts`, a control's hold on its named template parts: mirrors state pseudoclasses onto them and gives each its theme `{Component}.Styled{Part}` with the control's override merged over it as a copy (ADR 0001). `Controls/Button/`: `Button` (an Avalonia `Button`: Kind, Size, Shape, WidthType, MinHitArea, IsLoading, IsSelected, BackgroundSafe, StartEnhancer, EndEnhancer, ArtworkSize, Overrides; its tap band in `ArrangeCore`; `:loaded` for the transition), its enums, `ButtonLoadingSpinner` (the ring). `Controls/ButtonGroup/`: `ButtonGroup` (an `ItemsControl` of Buttons: Mode, Selected, Kind, Size, Shape, PaddingType, Wrap, Overrides, Click) |
| `src/Metered.Themes/` | `BaseTheme` merges every discovered file, in ordinal order, and holds no global style rule. `Tokens/*.g.axaml`: generated, every token under Base Web's name. `Controls/Button.axaml`: the Button's template of named parts. `Controls/Button.Parts.g.axaml`: generated, the parts' themes. `Controls/ButtonGroup.axaml`: the group's template and `ButtonGroup.StyledRoot`. `LICENSE-baseweb.txt`: Base Web's MIT notice, packed |
| `src/Metered/` | Umbrella package. No code |
| `samples/Metered.Sample.Desktop/` | The desktop sample (`net10.0`): one scrolling page of the pilot's controls in a 390 × 844 window, `SimpleTheme` under `BaseTheme`, Inter, a Light/Dark switch. `dotnet run --project samples/Metered.Sample.Desktop` |
| `tests/Metered.Tests/` | Avalonia.Headless with Skia and xunit v3, Inter as the font. `Harness/`: `Scene` (a 390 × 844 window with `BaseTheme`, real mouse, keys and frames), `Frames`, `Oracle` (the embedded `Reference/*.oracle.json`). `Controls/`: Button colours (308 cases), layout (40), parts, spinner (112), frames and gallery (`METERED_FRAMES`), ButtonGroup. `Overrides/`. `Discovery/` (the XAML manifest, on a fixture). `Repository/` (`LiteralColourGuard`, rule 5). `Performance/` (`StyleBudgetTests` over `StyleTrace` and `StyleCensus`, rule 6). `Spikes/` (`METERED_BENCH` for the cost: `StyleCostSpike`, and `StyleCostProfile`, its profile by phase and ablation; run them alone with `--filter FullyQualifiedName~StyleCost`: the style trace refuses to run beside them) |
| `tools/` | `tokens/gen.js` and `tokens/button.js` (the generated files), `breakage/run.js` (breaks a behaviour in a fresh clone and checks a test goes red), `reference/` (Base Web's style functions as the oracle, the cases, CSS conversion, the cascade measured on baseweb.design). See `tools/README.md` |
| `docs/` | `ROADMAP.md`, `decisions/0001` (parts, states and overrides), `spikes/README.md`, `performance/README.md` (the style budget, breakage runs), `controls/Button/`, `controls/ButtonGroup/` |

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
5. **No literal colours outside `src/Metered.Themes/Tokens/`.** `LiteralColourGuardTests` fails on
   one in any file under `src/`, generated part themes included: in XAML, a value Avalonia parses
   as a colour, outside comments; in C#, a hex string, `Color.From*`, `Color.Parse`, `Colors.*` or
   `Brushes.*`. `Transparent` is allowed.
6. **Style budget.** `StyleBudgetTests` fails when `BaseTheme`'s global rules pass their limits
   (now 0 of 10). A change that needs more raises the limit there on purpose and says so in its
   commit, with the new total (`docs/performance/README.md`).
