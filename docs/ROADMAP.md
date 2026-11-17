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

1. **Cost: decide whether to reduce it** (#10). Profiled 2026-11-16 (`docs/performance/README.md`):
   about 0.6 ms and 174 KB to open a Button headless, nearly half of it its parts' themes' styles,
   most of that state styles it doesn't match (31 % of the work, 45 % of the allocation), as Avalonia
   instances every style of a `ControlTheme` on every element. The mirroring itself is 9 %.
   No reduction within ADR 0001; a theme pruned by kind, size and shape (23 % of the work, 35 % of the
   allocation) or kind and state in code (at most 32 % and 45 %) would need a new ADR. On a Pixel 8 in
   Release, about 4.4 ms a Button, some seven times the headless cost (#10,
   `tools/android/README.md`); whether to cut it is the developer's decision.
2. **The sample and the real-window run**, as Cornflower's and Avonic's. The sample is a library
   with a page per control, shown by a desktop head (`samples/Metered.Sample`, #7) and an Android
   head built in CI (#8) and run on a Pixel 8 (`tools/android/run.ps1`, #29); still to come: a
   scripted real-window run. The
   sample's shell, its subtree theme and the new fonts haven't been seen in a real window (#5).

Done since the pilot (2026-11-16):

- [x] A theme builder: `BaseTheme.CreateTheme` / `CreateDarkTheme` and `ThemeProvider` for a
      subtree, every derived key written out, the derivation read from Base Web's own functions
      (ADR 0002, `docs/theming.md`). 18.2.0's `createTheme` takes overrides only, not primitives
- [x] The cost, profiled by phase and ablation (`Spikes/StyleCostProfile`, spike 2)
- [x] A font: Instrument Sans for UberMoveText, Figtree for UberMove, chosen by measurement against
      baseweb.design (ADR 0003, `tools/reference/fonts.js`)
- [x] The fonts shipped (#9): `Metered.Fonts`, a package of its own after Avalonia.Fonts.Inter,
      with `WithMeteredFonts()` for the mapping; the tests and the sample use it. Not published
- [x] The sample as a library (#7, from Cornflower's): a shell that lists a page per control and
      times each to its first frame, `CatalogueTests` and a test per page, `PerformanceMonitor` (the
      Debug line) and the timing benchmark with its trend table (`docs/performance/README.md`)

Borrowed from Cornflower and Avonic (2026-11-16):

- [x] CI: build, test, the Node tools' syntax, and both `--check` runs (`.github/workflows/ci.yml`)
- [x] An Android head and its CI job (#8): `samples/Metered.Sample.Android`, the `android` job, and
      `tools/android/run.ps1`, the device run, whose self-test reads fixtures with no phone. First run
      on a Pixel 8 on 2026-11-17 (#29)
- [x] The literal-colour guard (`tests/Metered.Tests/Repository/`)
- [x] The style budget for global rules (`tests/Metered.Tests/Performance/`,
      `docs/performance/README.md`). It doesn't replace item 1: a control's own cost isn't global.
- [x] The breakage runner (`tools/breakage/run.js`)
- [x] The discovery tests (`tests/Metered.Tests/Discovery/`)
- [x] How work runs: `docs/PROCESS.md`, `docs/agent-brief.md` and the pull request template, from the
      developer's shared ways of working, which carry Cornflower's and Avonic's loops forward

## Still to borrow from Cornflower and Avonic

Reviewed 2026-11-16 and left for when each is needed. Paths are in `R:\source\CornflowerUI`
unless Avonic is named.

| What | Where it is there | Take it when |
|---|---|---|
| **A test clock and a manual render timer**: `TestClock` (time moves only when the test says, each callback run at its own time), `ManualRenderTimer` (a frame only on a forced tick), and the harness's own tests | `tests/Cornflower.Tests/TestClock.cs`, `ManualRenderTimer.cs`, `Harness/`; the clock it replaces, `CornflowerClock`, is in `src/Cornflower.Controls` | The first control whose motion must match Base Web frame by frame. Avalonia 12's animation clock is internal, so `Transitions` and `Animation` run on real time and are tested at their end value (Cornflower's ADR 0003); the spinner's ring is the first candidate |
| **A real-window driver**: opens the desktop sample's real window, clicks through every page and saves frames; its self-test runs without a window | `tools/real-window/drive.ps1`, `selftest.ps1`; Avonic's `tools/real-window/` | With item 2. Avonic's found a bug 1,486 headless tests had missed. It is PowerShell and Windows only today: on macOS and Fedora it needs another way in. CI then parses every `.ps1` (Cornflower's `tools` job) |

## Inventory

By the research's grouping (`R:\source\baseui-research\README.md` §5): first the controls that
theme Avalonia's (checkbox, radio, switch, input, textarea, slider, progress bar, spinner, tabs,
tooltip, popover, menu, select), then the small new ones, then the overlays and navigation.

- [x] Checkbox (#34): Base Web's v1, the one baseweb.design shows, with its toggle style and four label placements (`docs/controls/Checkbox/`)
- [x] Radio and RadioGroup (#35): Base Web's radio-v2, the one its documentation recommends for new work; parts,
      overrides, label placement, align, description, error, the group's tab stop and arrow keys, the touch padding
      (`docs/controls/Radio/`)
