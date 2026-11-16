# Performance

What the theme costs an app, and what stops that creeping up unnoticed. The budget and the breakage
runner are carried over from Cornflower's
[`docs/performance/README.md`](https://github.com/wdcossey/CornflowerUI/blob/main/docs/performance/README.md),
which took them from Avonic; what changed is at the end.

## What it costs

Avalonia checks **every global style rule against every element**, in any app, even one that uses
none of these controls. It keeps no cache by type, and it visits nested rules whether or not the
parent matched. Avonic measured about **30 ns for each alternative between commas**, and about
385 ns for a descendant selector on an element at a depth of 20. Those are Avonic's numbers, from
one desktop PC; they haven't been measured again here.

**Rules inside a `ControlTheme` aren't global**: they cost only the controls that use the theme.
All of Metered's styling is in `ControlTheme`s: a control's template, and each part's
`{Component}.Styled{Part}` (ADR 0001). `BaseTheme` holds resource dictionaries and no rule at all.
`StyleBudgetTests.A_ControlThemes_Rules_Are_Not_Checked_Against_Other_Elements` holds that such
rules stay free for everything else.

## The budget

`StyleBudgetTests` counts `BaseTheme`'s global rules, through Avalonia's own trace, and fails CI when:

| Check | Now | Limit |
|---|---|---|
| Alternatives in all global rules | 0 | 10 |
| Alternatives getting past a `TextBlock`'s type | 0 | 5 |
| Alternatives getting past the type of any other common element, at most | 0 | 5 |
| Alternatives that look at every ancestor of a common element | 0 | 0 |

The common elements are `TextBlock`, `Border`, `Panel`, `StackPanel`, `Grid`, `DockPanel`, `Image`,
Avalonia's `Button`, `ScrollViewer` and `ContentPresenter`. Each is counted alone: the trace listens
only while the element joins the tree, and keeps only checks of the theme's global rules, as
Avalonia reports a `ControlTheme`'s own checks through the same activity.

The trace needs Avalonia's diagnostics, which the tests turn on as their assembly loads
(`TestApp.EnableDiagnostics`). It refuses to run with them off, as they are when `METERED_BENCH` is
set, since it would see nothing and every budget would pass. So run the cost spike and its profile on
their own (the profile takes about three minutes):

```sh
METERED_BENCH=1 dotnet test tests/Metered.Tests -c Release --filter FullyQualifiedName~StyleCost
```

**When a control goes over,** the failure lists the largest rules. First see whether the rule can
move into the control's `ControlTheme`. If not, raise the limit in `StyleBudgetTests` in that
commit, on purpose, and say so in its message, with the new total (`CLAUDE.md`, rule 6).

**100 alternatives is a checkpoint,** about 3 µs per element at Avonic's rate. A test stops the
limit going past it; at that point, measure on a phone before going on.

## What the budget doesn't cover

**A control's own cost.** None of a Button's styles is global, so the budget reads 0 beside it; it
only keeps the cost from spreading to elements that aren't ours. Virtualising and scrolling aren't
measured.

`StyleCostProfile` (spike 2) opens 300 of an element in a window, 41 runs with the variants in a new
order each run, and times each phase in the UI thread's own CPU cycles (Windows'
`QueryThreadCycleTime`, which other processes don't add to), once with the GC as an app has it and
once inside a no-GC region; allocation is `GC.GetAllocatedBytesForCurrentThread`, the same every
run. Each ablation switches one thing off for the test only, through the window's resources or a
subclass, so the difference is that thing's cost. Measured 2026-11-16 on one Windows desktop, with two
other agents building and testing on it; a run's quartiles spread by 20 to 60 kcycles without the
GC, 70 to 300 with it, and the Button's median held within 3 % across runs (four with the GC, two
without). Two reruns at review, under more load, put the Button at 1,198 and 1,322 kcycles without
the GC (+2 % and +13 %, the floor up as much in the second) and at 2,124 and 2,073 with it; each
row's share of the Button held. Rows marked † changed at review and are from the second rerun: read
them as a share of its Button, 1,322.

| A default Button, per element | kcycles, no GC | KB |
|---|---|---|
| TextBlock (the floor) | 297 | 9 |
| **Button** (with the GC: 2,136 kcycles, about 600 µs, of which 206 µs GC pauses) | **1,170** | **174** |
| Its template's `Build` alone, unstyled † | 45 | 28 |
| Its parts' six themes without their 121 styles, their own setters kept † | −563 (43 %) | −94 (54 %) |
| The 114 state styles a default Button doesn't match (of 121) | −370 | −78 |
| The other six kinds' styles alone | −208 | −50 |
| Those of other kinds, sizes and shapes | −269 | −60 |
| Every `DynamicResource` setter given its resource's value instead: the bindings alone, the look kept † | −104 (8 %) | −27 (15 %) |
| The spinner's theme, and its container's five state styles | −103 | −18 |
| The spinner's turning `Animation`, alone | −45, within the spread (−19 in the second rerun) | −7 |
| The pseudoclasses' mirroring (`Parts.Attach` is 100 kcycles, the matched styles taking effect) | −239, with what the matched styles draw | −9 |
| Mirroring before the parts are styled, in place of after (the parts are then attached twice; the second pass, 8 kcycles, is counted) | −30, within the spread | +0.5 |

**Where it goes.** Nearly half of a Button's work and more than half its allocation is its parts'
themes' styles, and most of that is state styles it doesn't match: Avalonia instances every style of a
`ControlTheme` on every element that uses it, at about 3 kcycles and 0.7 KB each (spike 2's
finding). The allocation then costs again in the GC: in a page of 300, its pauses are a third of the
time, and they fall in steps as the allocation crosses the GC's budget. The mirrored pseudoclasses
cost what the matched styles cost to take effect, which they would cost at any time; the rest of the
gap to the floor is the template's ten elements, their measure, and drawing the fill and shadow.

**Not reduced.** Nothing within ADR 0001 removes the state styles: a theme of one part is one
`ControlTheme`, all of whose styles Avalonia instances. Two designs would, and both change the ADR:

- **A part's theme copied without the styles its control's kind, size and shape can't match**,
  cached per theme and combination as an override's copy is (ADR item 5), and copied again when
  one of them changes: 23 % of the work and 35 % of the allocation (35 % of the time with the GC),
  measured as "those of other kinds, sizes and shapes" above.
  The part's `Theme` would no longer be the `Styled{Part}` resource itself.
- **Kind and state chosen in code** (Base Web's style functions as code, setting token keys), the
  theme keeping only what a Button matches: at most 32 % and 45 %, measured with the matching styles
  alone. It gives up item 3, states as pseudoclass styles that an override's state styles meet in
  the cascade.

Smaller ones within the ADR were left: comma selectors for the styles that set the same values
(5 fewer of 121), and the spinner's animation (2 to 4 %, within the spread), whose look lives in its
theme.

## The trend

Run by hand at each roll-up, on the same machine, and add a row: a CI machine's timings would mean
nothing. `METERED_BENCH` names the file the row is appended to (any other value, such as the
spikes' `1`, leaves the benchmark alone), and leaves Avalonia's diagnostics off, as an app has them:

```sh
METERED_BENCH=/absolute/path/row.md dotnet test tests/Metered.Tests -c Release --filter FullyQualifiedName~StyleCostBenchmark
```

`StyleCostBenchmark` (from Cornflower's) alternates with and without `BaseTheme`, 25 rounds after 4 to
warm up, attaching 5,000 elements at a depth of 20; times are µs per element, the median. *Rules* and
*Alternatives* are `BaseTheme`'s global rules. *Button* is a default Metered `Button`, 500 to a window,
with `BaseTheme` (it has no look without it): what a control's own cost comes to, which the budget
doesn't see. A row can go straight in the table.

| When | Rules | Alternatives | `TextBlock` | `Border` | `Button` |
|---|---|---|---|---|---|
| 2026-11-16, the pilot and the sample (#7) | 0 | 0 | 19.2 to 19.7 (+0.5) | 2.9 to 3.2 (+0.3) | 545 |

The first row is the pilot's end, with no global rule. Even so, a `TextBlock` or a `Border` costs
0.3 to 0.5 µs more beside the theme, in both runs made for it (the second: 18.8 to 19.1 (+0.3),
2.8 to 3.3 (+0.5), 554); not explained yet, and Cornflower's first row shows the same for its
`Border`. A Button is about half a millisecond, as spike 2 found. Measured on one Windows desktop
with two other agents building and testing on it, so the absolute times are higher than a quiet
machine's.

## In a Debug build

The sample's shell (`SampleShell`) writes a line for each page it opens, of what it cost to its first
drawn frame, so a control's page has no code for it: on standard output after the page's ready line,
as `METERED cost <line>`, and under the page's title in the shell's bar, which has one height in
every build, so a page's size doesn't change with it:

```
METERED cost Button: 196 ms, 326 elements, 3 rules (3), layout 61.0, render 29.8
```

(The Button page, the first opened in a headless Debug run, so its time has the JIT in it.)

The time is the shell's own, from before the page is made to its first drawn frame, the same as its
ready line's `open=`; for a page shown before the shell is in a window (a phone's start page), the time
it took to make and then from the shell's joining the window, not the platform's start between. *Elements* is the page's visual tree. *Rules* are the global rules the page is
checked against (the application's, and those of the page and its logical ancestors), with their
alternatives in brackets: the three are the shell's own, its headings, its labels and the margin
between buttons in a `WrapPanel`, which every element of every page pays; `SimpleTheme` and
`BaseTheme` have none. *Layout* (measure and arrange) and *render* (the UI thread's, not the compositor's,
which Avalonia meters apart) are Avalonia's own meter (`Avalonia.Diagnostic.Meter`),
which each head turns on in Debug before Avalonia starts (`PerformanceMonitor.EnableDiagnostics`);
without it the line says so rather than show zeros. A monitor hears every measurement in the
process, so one lives only while its page opens. Release builds write only the ready line.

## Breakage runs

`tools/breakage/run.js` breaks each behaviour on its own, in a fresh clone, and checks that a test
goes red. A test that can't fail proves nothing.

```sh
git clone <this repo> <temp folder>
node tools/breakage/run.js <temp folder> <list.js> <results.txt> "<test filter>"
```

The list and the meaning of each result are in the script's header. It finds the clone's one
`*.Tests.csproj` under `tests/` unless one is named. A run that times out (10 minutes, or
`BREAKAGE_TIMEOUT_MS`) has whatever it left running from the clone killed, so a hung test host
can't lock the next build. The clone needs no `npm install`: the tests read the committed oracle.

Run on 2026-11-16 against the colour guard, the census, the trace, the budget and the discovery
tests: 10 breaks, all caught (one of them on a second run, as its text to find was mistyped in the
list and the runner reported it not applied).

## From Cornflower

- **`StyleCensus` lives in the sample** (`samples/Metered.Sample/Diagnostics/`), as in Cornflower, for
  the Debug line; the tests reach it through a global using.
- **No exemption for our own controls after a descendant combinator.** Cornflower lets
  `Pivot PivotItem > TextBlock` pass, as its controls are rare. Metered's `Button` shares its name
  with Avalonia's, so a rule can't be told apart by name, and no global rule is needed.
- **The trend has a `Button` column** and no *Themed* one: Cornflower counts its themed controls
  against its holes, which Metered doesn't track, and a Metered control's cost is its own, not
  global.
- **The ready line has no page transition to wait for**: Base Web has none, so a page takes the
  list's place at once, and the line follows its first frame.
