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
set, since it would see nothing and every budget would pass. So run the cost spike on its own:

```sh
METERED_BENCH=1 dotnet test tests/Metered.Tests --filter FullyQualifiedName~StyleCostSpike
```

**When a control goes over,** the failure lists the largest rules. First see whether the rule can
move into the control's `ControlTheme`. If not, raise the limit in `StyleBudgetTests` in that
commit, on purpose, and say so in its message, with the new total (`CLAUDE.md`, rule 6).

**100 alternatives is a checkpoint,** about 3 µs per element at Avonic's rate. A test stops the
limit going past it; at that point, measure on a phone before going on.

## What the budget doesn't cover

**A control's own cost.** Opening a Button takes about 1 to 1.5 ms headless
([spike 2](../spikes/README.md)), and that is its `ControlTheme`s' state styles and its mirrored
pseudoclasses, none of them global, so the budget reads 0 beside it. Profiling that is the
roadmap's first item; the budget only keeps the cost from spreading to elements that aren't ours.
Layout, rendering, virtualising and scrolling aren't measured either.

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

- **`StyleCensus` lives in the tests**, not in a shared sample library, as Metered has none yet.
- **No exemption for our own controls after a descendant combinator.** Cornflower lets
  `Pivot PivotItem > TextBlock` pass, as its controls are rare. Metered's `Button` shares its name
  with Avalonia's, so a rule can't be told apart by name, and no global rule is needed.
- **Not carried over yet**: the timing benchmark and its trend table, and the sample's Debug line
  (`PerformanceMonitor`). They come with the sample (`docs/ROADMAP.md`).
