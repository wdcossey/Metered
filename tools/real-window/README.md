# tools/real-window

The desktop sample in a real window, with real input: the mouse, the keyboard and touch through Windows' touch injection.
Headless tests can't see everything: in Avonic, the first run of its version of this found a control bug that 1,486
tests had missed.

**It takes over the cursor and focus for a few seconds a page, so ask first** (PROCESS §2), and run it through the
desktop lease, which other projects' sessions share. It puts the cursor back and closes only the window it opened.

```powershell
node ~/.claude/device-lease/lease.js run desktop --who metered --for "real-window run" --idle-seconds 60 -- pwsh -NoProfile -File tools/real-window/drive.ps1
# the script alone:
pwsh -NoProfile -File tools/real-window/drive.ps1
# or, under Windows PowerShell 5.1:
powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
# only some sections, by their titles as -like patterns, comma-separated:
pwsh -NoProfile -File tools/real-window/drive.ps1 -Only 'switch*,radio*'
```

**Windows only**, by nature: it moves the real cursor through Win32 and finds the window through UI Automation. It is
written for PowerShell 7 (`pwsh`) and Windows PowerShell 5.1 alike. macOS and Fedora need another way in.

It builds the sample first, from the repo so that `global.json` picks the SDK, as `tools/android/run.ps1` does: a stale
build runs the old code, and fails checks the new code passes. `-Configuration Release` builds and runs Release instead
of Debug; only a Debug build has the cost line to check. `-NoBuild` runs the build as it is. **Close the sample first**
if it's open: it locks its files, so the script stops rather than build. Frames go to a new folder in the temp folder for
each run, which it prints, unless you pass `-Out`; the sample's standard output goes there too, as `stdout.txt`, and the
run's own report, every line it prints from the sample's start, as `report.txt` (#136), closed even when the run stops
on an error or Ctrl+C, so a failure can be read after the run
however much of its output a terminal or a tool kept. Every failure is listed again at the end, with its section. It
exits 1 if a check fails.

`-Only` runs the sections whose title is like the pattern, or like one of its comma-separated parts (`'switch*'`,
`'checkbox*,radio*'`); it fails if none is. The whole run is the gate.

**If something else keeps the focus, it stops sending input** rather than type into another app, and fails those checks.
It stops at once if the window doesn't fit on the screen. It never starts a press outside the sample's window; whatever
an error leaves held, the mouse button or a touch, it lets go of.

## What it does

- **Every page on the list,** with nothing to add for a new control: opens it with a real click, checks that the shell
  writes the page's ready line under its title, saves a frame, fails on a button, check box or radio with no size and, in a Debug build, on
  a missing cost line in the shell's footer, then clicks Back and checks the list is back. A page that throws fails on its own, and the pages after it still run. A page below the fold of a long list
  is opened through UI Automation instead: a click never goes outside the window. Titles are matched exactly, case and
  all, from each page's `[SamplePage]`.
- **The phone's view:** the Theme page's ready line,
  `METERED ready open=<ms>ms scaling=<s> view=<w>x<h> raw=<w>x<h> safe=<l>,<t>,<r>,<b> page=<w>x<h> title=<title>`, has to
  report a 390 × 788 page: `MainWindow`'s 390 × 844, less the shell's 56 bar, to the pixel. Windows gives a window whole
  pixels, so at 125 % a 390 page can come out at 389.6. Only `page=`, `scaling=` and `title=` are read.
- **Alt+Left,** Avalonia's back request on desktop, from a page to the list.
- **Light and Dark:** the bar's switch names the other variant, and the list's background changes, and changes back.
- **Resizing:** the window has to say it can be resized, and the page is laid out wider once the window is; then the
  window goes back to its size.

Then a section for each control with input of its own, on its sample page. A control is found by its `x:Name`, which
Avalonia gives UI Automation as its AutomationId, or by its text, which is its name there. A checkbox's or a switch's
state is read through UI Automation's toggle pattern; a page's line under a control, as its text. A labelled control is
pressed on its mark, at its start: its root is a flex row whose empty end takes no input. Pixels are compared as how
many of a region's sampled pixels differ: under 5 is the same, over 20 a change (`helpers.ps1`).

- **button:** the mouse over Primary draws its hover, which goes when it leaves; a held touch draws it pressed. Whether a
  lifted touch leaves anything drawn (a hover stuck after a tap) is counted only when it settles: Windows moves the cursor
  to a touch it injects, which hovers the button as the mouse would, and then it prints why it isn't counted. On this
  PC it never settles; a phone is where that is seen (`tools/android`). "Click to load" turns its spinner, and after
  `ButtonPage.LoadingFor` (2 s) is drawn as it was.
- **buttongroup:** taps and a click on the checkbox group's pills, read from the page's `Clicked` line.
- **checkbox:** the parent follows its two children, and UI Automation reads it indeterminate (`CheckboxAutomationPeer`);
  a click focuses a box, Space and an Enter on it toggle it; a held touch draws the backplate's pressed tint (#49: kept,
  where Base Web shows none), and the lift checks it.
- **radio:** the mouse hovers a radio; a held touch draws the pressed tint and leaves the checked one checked; a tap and a
  click select; Down and Up move the selection, wrapping.
- **switch:** a tap on its track, a click, Space and an Enter on it each toggle it, read from the page's `State` line and
  through UI Automation; an Enter pressed on the next switch, then let go once the
  focus is back on the first, toggles neither (#49); a held touch draws the pressed tint (#68).
- **progress bar:** the stepped bar's Next (a click) and Previous (a tap) move it a step; UI Automation reads its range,
  read only, and Base Web's name for it, "Step 2 of 5" (#53).
- **slider:** in the page's ScrollViewer, a touch drag across the first slider moves its thumb from 60 to 80 and leaves the
  page; a tap on its line puts it at 30; a drag mostly up the page, 150 view px up and a fifth of the line across, still
  moves it, to 50, and still leaves the page; a mouse drag moves it back to 20; and a touch drag up off the slider, on its
  heading, does scroll the page, so the first two prove something. Read from the page's `State` line (`Value: 60`).
- **input:** a click focuses the first input and what is typed goes in at its end (the page's `Value:` line and UI
  Automation's value pattern); Tab goes from the clearable input to its clear icon, to the password, to its mask toggle,
  and Shift+Tab back. The clear icon by a click, a tap and Space (which types no space), and Escape in the input, each
  clear it and leave the input focused. Enter in the password leaves it masked; Enter on its toggle and a click relabel it
  (`Show password text`, `Hide password text`) and draw the text unmasked, compared in the input's left half, and back. A
  click on the `@` enhancer and a tap on the `$` one focus their inputs (known difference 1 in `docs/controls/Input`).
  #94: a finger's drag up on an input scrolls the page, unfocused and focused, and leaves the caret where it was (a typed
  `Z` goes at the end); a tap on its first letter puts the caret there.
- **textarea:** a click focuses it and Enter starts a line; the clear icon by a click, and Escape, clear it. With the page
  scrolled just far enough to show the long text, so the page can still scroll on: a notch of the wheel scrolls the text
  and not the page, and a notch back undoes it; a finger's short drag scrolls the text partway, and a long one to its end
  with the page left where it was (latched); a drag that starts with the text at its end scrolls the page. Where the text
  is, from its own scroller's scroll pattern. The resize grip: a mouse drag makes it 40 wider and 30 taller, a finger's 30
  narrower and 20 shorter, within 2 view px, and neither moves the page.

A control below the fold is brought into view through UI Automation's scroll pattern, which sends no input (`Reveal`,
just far enough; `ScrollToEnd`). A part inside a control, such as the clear icon or the mask toggle, is found inside it
by its type and its label (`Inner`). A touch drag that a scroller shouldn't carry on after the lift is held still for a
moment first, so it ends with no speed for inertia.

The input and textarea sections (#98) haven't been run against a real window yet: the first run is the coordinator's,
through the device lease.

**Look at the frames as well.** Most of what's wrong with a page isn't something it checks.

## Checking it without a run

`helpers.ps1` holds the script's pure helpers: the ready line's fields, `-Only`'s match, a page line's
values, the whole lines of a growing file, the pixel thresholds and the tap's verdict, and the sizes it expects; where an
input's start enhancer and its text are and where a textarea's grip is, from Base Web's lengths; a text's lines, where a
letter was typed, how much a box grew, and how far, in the scroll pattern's percent, a page has to scroll to show a box.
`selftest.ps1` checks them on known answers, checks those sizes against `MainWindow.axaml` and `SampleShell.axaml` and
those lengths against the generated `Input.Parts.g.axaml` and `Textarea.Parts.g.axaml` and `Textarea.axaml`'s grip, and
checks the script as parsed: that it parses, that each section's title selects it alone, that every page it opens is in
the sample, and that every control a section finds is on that section's page or the shell: an `x:Name` for `ById` and
`ReadOut`, a quoted value (a `Content`, a `Text`, an `AutomationProperties.Name`) for `Named`; and that every part `Inner`
finds inside a control is named by a constant in `Metered.Controls` (BaseInput's `Clear value`, `Show password text`). It takes no input and opens no window, so it runs on any OS, under pwsh 7 (and Windows PowerShell 5.1, as
`helpers.ps1` has to, since `drive.ps1` loads it under both). CI's `generated` job runs it on Linux, and its `real-window`
job on Windows under both:

```powershell
pwsh -NoProfile -File tools/real-window/selftest.ps1
```

## What it can't tell you

- **Scale** is whatever this PC's display is set to.
- **Windows only.** CI doesn't run it, as CI has no display.
- **Not a phone's touch.** Its touch is Windows' injected touch, through Avalonia's Win32 backend, with one contact; a
  phone's comes through Android (`:pointer-coarse` is set only there), and the phone is the real touch test
  (`tools/android`).
- **Not a phone's soft keyboard.** The input and textarea sections type with the real keyboard. A touch that focuses a
  text box may bring up Windows' touch keyboard on a PC set to show it, over the window, where a later press would miss;
  the caret, the selection handles and the soft keyboard under a finger are the phone's to show (#90).

The frames come from the screen, not `PrintWindow`, which offsets a window by its invisible border, and that looks like a
bug in the app. They are the window's client area: UI Automation reports that as the window's bounds.

Ported from Cornflower's `tools/real-window/` (`wdcossey/CornflowerUI`), which came from Avonic's: the window, the input,
touch, the pixel reads, the frames, the every-page loop, `-Only`, the pure helpers and the self-test. Metered's own: the
sections after the loop, the sizes read from the sample's XAML and the lengths from the themes, `Mark`, `After`,
`TapVerdict`, `Reveal`, `Inner` and the wheel.
