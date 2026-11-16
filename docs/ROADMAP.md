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
   about 0.6 ms and 174 KB to open a Button headless, half of it the part themes' state styles, as
   Avalonia instances every style of a `ControlTheme` on every element. The mirroring isn't the cost.
   No reduction within ADR 0001; a theme pruned by kind, size and shape (23 % of the work, 35 % of the
   allocation) or kind and state in code (at most 32 % and 45 %) would need a new ADR. Not measured on
   a phone.
2. **A theme builder.** Base Web's `createTheme` / `createDarkTheme`, and a subtree theme: spike 1
   says it must write every derived key, not only the foundation.
3. **A font.** An OFL substitute for Uber Move, chosen by metrics against baseweb.design, which
   serves Uber Move.
4. **The sample and the real-window run**, as Cornflower's and Avonic's. A first desktop sample
   exists (`samples/Metered.Sample.Desktop`, one page); still to come: a page per control, an
   Android head, and a scripted real-window run.

Done since the pilot (2026-11-16):

- [x] The cost, profiled by phase and ablation (`Spikes/StyleCostProfile`, spike 2)

Borrowed from Cornflower and Avonic (2026-11-16):

- [x] CI: build, test, the Node tools' syntax, and both `--check` runs (`.github/workflows/ci.yml`)
- [x] The literal-colour guard (`tests/Metered.Tests/Repository/`)
- [x] The style budget for global rules (`tests/Metered.Tests/Performance/`,
      `docs/performance/README.md`). It doesn't replace item 1: a control's own cost isn't global.
- [x] The breakage runner (`tools/breakage/run.js`)
- [x] The discovery tests (`tests/Metered.Tests/Discovery/`)

## Still to borrow from Cornflower and Avonic

Reviewed 2026-11-16 and left for when each is needed. Paths are in `R:\source\CornflowerUI`
unless Avonic is named.

| What | Where it is there | Take it when |
|---|---|---|
| **A test clock and a manual render timer**: `TestClock` (time moves only when the test says, each callback run at its own time), `ManualRenderTimer` (a frame only on a forced tick), and the harness's own tests | `tests/Cornflower.Tests/TestClock.cs`, `ManualRenderTimer.cs`, `Harness/`; the clock it replaces, `CornflowerClock`, is in `src/Cornflower.Controls` | The first control whose motion must match Base Web frame by frame. Avalonia 12's animation clock is internal, so `Transitions` and `Animation` run on real time and are tested at their end value (Cornflower's ADR 0003); the spinner's ring is the first candidate |
| **The control loop and a pull request template**: plan, build, compare, review, document, with a checklist per control and the rule of one control per pull request | `docs/PROCESS.md`, `.github/PULL_REQUEST_TEMPLATE.md`; Avonic's `docs/PROCESS.md` and `docs/LESSONS_LEARNED.md` | Before the inventory starts, when controls go through pull requests one at a time. The plan step's spec here is Base Web's props and style functions, which the oracle already reads |
| **The sample as a library with a page per control, and its tests**: a shared sample project the desktop and Android heads both show, `CatalogueTests` and a test per page | `samples/Cornflower.Sample`, `tests/Cornflower.Tests/Sample/` | With item 4 above. Also brings `PerformanceMonitor` (the Debug line) and the timing benchmark with its trend table (`Performance/StyleCostBenchmark.cs`), which item 1 wants |
| **An Android head, its CI job and the device run** | `samples/Cornflower.Sample.Android`, the `android` job in `.github/workflows/ci.yml`, `tools/android/run.ps1` | With item 4. The job builds the head so it can't break unnoticed; the run puts it on the phone over ADB |
| **A real-window driver**: opens the desktop sample's real window, clicks through every page and saves frames; its self-test runs without a window | `tools/real-window/drive.ps1`, `selftest.ps1`; Avonic's `tools/real-window/` | With item 4. Avonic's found a bug 1,486 headless tests had missed. It is PowerShell and Windows only today: on macOS and Fedora it needs another way in. CI then parses every `.ps1` (Cornflower's `tools` job) |

## Inventory

By the research's grouping (`R:\source\baseui-research\README.md` §5): first the controls that
theme Avalonia's (checkbox, radio, switch, input, textarea, slider, progress bar, spinner, tabs,
tooltip, popover, menu, select), then the small new ones, then the overlays and navigation.
