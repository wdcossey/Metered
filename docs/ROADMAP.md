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
   head built in CI (#8) and run on a Pixel 8 (`tools/android/run.ps1`, #29), and a scripted
   real-window run (`tools/real-window/drive.ps1`, #6): every page, the shell, and each control's
   input by mouse, keys and touch, on Windows. Its first run found #56. Still to come: a way in on
   macOS and Fedora.

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
- [x] The soft keyboard (#108): `KeyboardInset`, an opt-in decorator that keeps its child clear of a phone's keyboard
      and brings the focused control into view, as a browser does, which Base Web relies on; the sample's shell uses it
      (`docs/controls/KeyboardInset/`). Tested headless through a fake pane; not yet run on the Pixel

Borrowed from Cornflower and Avonic (2026-11-16):

- [x] CI: build, test, the Node tools' syntax, and both `--check` runs (`.github/workflows/ci.yml`)
- [x] An Android head and its CI job (#8): `samples/Metered.Sample.Android`, the `android` job, and
      `tools/android/run.ps1`, the device run, whose self-test reads fixtures with no phone. First run
      on a Pixel 8 on 2026-11-17 (#29)
- [x] A real-window driver (#6): `tools/real-window/drive.ps1`, `helpers.ps1` and `selftest.ps1`, from
      Cornflower's; the self-test runs in CI's `generated` job (Linux) and its `real-window` job (Windows PowerShell
      5.1 and pwsh 7). Windows only
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
| **A test clock and a manual render timer**: `TestClock` (time moves only when the test says, each callback run at its own time), `ManualRenderTimer` (a frame only on a forced tick), and the harness's own tests | `tests/Cornflower.Tests/TestClock.cs`, `ManualRenderTimer.cs`, `Harness/`; the clock it replaces, `CornflowerClock`, is in `src/Cornflower.Controls` | The first control whose motion must match Base Web frame by frame. Avalonia 12's animation clock is internal, so `Transitions` and `Animation` run on real time and are tested at their end value (Cornflower's ADR 0003); the spinner's ring and the progress bar's animations (#53) are the first candidates |

## Inventory

By the research's grouping (`R:\source\baseui-research\README.md` §5): first the controls that
theme Avalonia's (checkbox, radio, switch, input, textarea, slider, progress bar, spinner, tabs,
tooltip, popover, menu, select), then the small new ones, then the overlays and navigation.

- [x] Checkbox (#34, #42): Base Web's checkbox-v2, the one its v1 page says replaces it; parts, overrides, the two
      label placements, indeterminate, error, the touch padding; its toggle style went to Switch (#43)
      (`docs/controls/Checkbox/`)
- [x] Radio and RadioGroup (#35): Base Web's radio-v2, the one its documentation recommends for new work; parts,
      overrides, label placement, align, description, error, the group's tab stop and arrow keys, the touch padding
      (`docs/controls/Radio/`)
- [x] Switch (#43): Base Web's `baseui/switch`, its v2 toggle; parts, overrides, the two sizes and label placements,
      the checkmark, Enter on its release (only one pressed on it, the checkbox's rule: #49), the touch padding, the
      pressed tint Radio and Checkbox have, Metered's (#68) (`docs/controls/Switch/`)
- [x] ProgressBar (#53): Base Web's `ProgressBar`; parts, overrides, sizes, intents, steps, infinite, the label and its
      accessible name, its motion tested at its end values (`docs/controls/ProgressBar/`); its value clamped as
      `RangeBase` clamps it, against Base Web (#67). `ProgressBarRounded` is a follow-up
- [x] Spinner (#54): Base Web's `baseui/spinner`; its one part and overrides, `$size` and `$borderWidth` by name, sizing
      token or length as its style function reads them, `$color`, the turn (at its end values: #13), a progress bar for
      automation; its ring's drawing shared with Button's (`docs/controls/Spinner/`)
- [x] Slider (#55): Base Web's `baseui/slider` (no v2), dragged and stepped as react-range does it; one thumb or a range,
      marks, labels, parts and overrides, a touch drag that never pans the page (`docs/controls/Slider/`)
- [x] Input (#84): Base Web's `baseui/input` (no v2), on Avalonia's `TextBox`: its twelve parts and overrides, sizes,
      enhancers and adjoined, error and positive, disabled and read-only, the clear icon and Escape, a password's mask
      toggle, maxLength, the focus ring, a finger's drag the page's (#94); Base Web's three icons as paths; `BaseInput`
      and the generator's parts shared for Textarea (#85) (`docs/controls/Input/`). MaskedInput and PhoneInput are follow-ups; FormControl is #88
- [x] Textarea (#85): Base Web's `baseui/textarea` (no v2) on Input's `BaseInput`: rows, resize (a browser's grip), sizes,
      error and positive, disabled and read-only, clearable and Escape, maxLength, its parts and overrides; the text wraps,
      scrolls under its padding, and a finger's drag scrolls it, not the page, until it is at its end
      (`docs/controls/Textarea/`). On the Pixel: #90
- [x] FormControl (#88): Base Web's `baseui/form-control` (no v2) round its one child: the label, the caption, the error
      and positive messages with Base Web's two icons, the label's end enhancer and the counter, disabled, what the child
      is given (`cloneElement`'s), its seven parts and overrides; the label focuses what it labels on a click or a tap,
      not a press, so a finger's drag from it scrolls the page; the label names it to UI Automation
      (`docs/controls/FormControl/`). On the Pixel: #102. Then (#105): the label also clicks what it labels (a checkbox
      toggles, a button is clicked), its hit area covers its row's margins, and a Textarea is given the error
- [x] Popover (#106): Base Web's `baseui/popover` (no v2), `Popover` and `StatefulPopover` in one, on Avalonia's
      `Popup` in the window's overlay layer: opened by a click or a hover, placed as popper.js places it (`Tether`, against
      popper.js itself run headless), the arrow, dismissed by a press or a finger's tap outside and Escape by the top layer
      (`Layers`), its entrance and exit, focus and the focus lock, content of any kind or a function of `close`, its three
      parts and overrides (`docs/controls/Popover/`). `Tether` and `Layers` are for Select (#97) and Menu (#107). On
      the Pixel and in a real window: #119. Then (#123): a finger's or a pen's tap toggles a hover popover, and
      Avalonia's back request closes the top popover that Escape would close before the shell goes back
- [x] Menu (#107): Base Web's `baseui/menu` (no v2), `StatefulMenu` and `Menu` in one: items, groups, dividers, the empty
      state, the keyboard highlight, type-ahead and scrolling as its own container and `scrollItemIntoView` do (against
      Base Web's container driven in Node), a mouse's hover and click, a finger's drag that scrolls it and a tap that
      chooses, `keyboardControlNode` and `forceHighlight` for Select (#97), links, the profile option by the `Option`
      override, nested menus in a Popover (`NestedMenus`), its parts and overrides (`docs/controls/Menu/`). On the Pixel
      and in a real window: #126
