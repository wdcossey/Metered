# tools/android

The device run: the sample's Android head (`samples/Metered.Sample.Android`) on a real phone, over ADB. From
Cornflower's. Anything on the phone is ask-first and goes through the device lease
([`docs/PROCESS.md`](../../docs/PROCESS.md) §2); the self-test needs neither.

```sh
pwsh -NoProfile -File tools/android/run.ps1 -SelfTest                          # the parsers, on fixtures: no adb, no phone
pwsh -NoProfile -File tools/android/run.ps1 -Page Button,ButtonGroup,Theme     # build, install, open each page cold
pwsh -NoProfile -File tools/android/run.ps1 -Configuration Debug -Page Button -Reopen 5   # the cost line, cold then warm
pwsh -NoProfile -File tools/android/run.ps1 -NoBuild -Page Button -Out ~/temp/run1        # the last build, a new folder of your own
```

It runs on Linux, macOS and Windows, under PowerShell 7.4 or later (`pwsh`), with the .NET SDK and its `android`
workload for the build.

## The head

`samples/Metered.Sample.Android` holds only Android's entry points; the app, its shell, its pages and its fonts are
`samples/Metered.Sample`'s, as the desktop head's are:

- `Application` builds the app with the sample's fonts (`UseSampleFonts`: Instrument Sans and Figtree for Base Web's
  UberMoveText and UberMove, ADR 0003) and, in a Debug build, turns on Avalonia's diagnostics before Avalonia
  starts, for the cost line.
- `MainActivity` has a fixed Java name, `metered.sample.MainActivity`, for `am start -n`. Its `OnCreate` sets
  `App.StartPage` from the intent's `page` extra (`App.PageExtra`) before Avalonia makes the view, which opens that
  page; with no extra, as from the launcher, the list. It is single-top: an intent sent while it's on top comes to
  `OnNewIntent`, which opens the page its extra names in the running shell, warm, and keeps the intent as the
  activity's own, so an activity Android makes again opens the page last asked for. A title the catalogue doesn't have
  stops the app, at start or later, with the titles it has; so does a view that isn't the shell, rather than leave the
  script waiting.
- The window behind the view is white, as the sample opens light, so nothing flashes before the first frame.

It isn't in `Metered.sln`, as it needs the `android` workload: CI's `android` job installs the workload and builds it
in Release and in Debug (whose code differs, for the cost line), after running the self-test.

## What `run.ps1` does

1. Checks that `adb devices` lists exactly one phone, and reads its model, Android version, screen size and density.
2. Builds the head (Release by default), and installs the signed APK. The project embeds its assemblies in the APK in
   every configuration, so `-Configuration Debug` installs with `adb install` too.
3. For each page of `-Page`, in turn:
   1. Wakes the screen, before each page, as a long run can outlast the screen's timeout. A phone with a secure lock
      has to be unlocked by hand: the script says so and waits (`-UnlockTimeoutSeconds`, 60 by default), then waits
      for the screen to be fully awake. Then it goes home, stops the app, clears the log, and starts it cold at the
      page with `am start -W -S` and `--es page <title>`. Android reports the time from the intent to the
      activity's first frame (`TotalTime`). If it reports a `LaunchState` other than `COLD`, the script warns and
      records no time; before Android 10 (API 29) am writes no `LaunchState`, and `-S` has stopped the app, so the
      time is the cold start's. If `am start` fails, the script first looks for the app's crash report, so that a
      crash at start is reported with the app's own message.
   2. Waits for the page's ready line in logcat (`-TimeoutSeconds`, 60 by default), and in a Debug build its cost line,
      or stops at once on a crash report for the app: Java's (`FATAL EXCEPTION` then `Process: <package>`) or .NET's
      (`UNHANDLED EXCEPTION`: from the app's process once `pidof` has seen it, so another .NET app's isn't the
      sample's; before that, any since the log was cleared, so that a crash at start isn't missed). Each poll is one
      dump: `DOTNET` at Info and up, and every error. The line has to name the page asked for, in any case.
   3. Saves the screen with `adb exec-out screencap -p` to `frame.png`, and checks it starts with PNG's signature
      (`exec-out` passes on neither screencap's exit code nor its errors apart from its output).
   4. `-Reopen <n>` times: clears the log, Back to the list (`KEYCODE_BACK`, the shell's back request),
      `-SettleMilliseconds` (1000) for the list to show, a check for a crash on Back, then `am start` with the page's
      extra again, and waits for that open's ready line (and cost line). am has to say the intent was delivered to the
      running activity (`OnNewIntent`, single-top): a new activity or a new process isn't a warm open, and stops the
      run. Android doesn't time these; the shell does. The log is cleared before every open and saved after it, so a
      long run can't lose an open's lines from logcat's ring buffer, and each wait reads only its own open. The settle
      is a fixed wait, as the list writes no line: if Back hasn't been handled by then, the page is opened over
      itself and its time includes leaving the old one. Raise `-SettleMilliseconds` on a slow phone.
   5. Saves `logcat.txt` (each open's dump in turn) and `run.json`.

`-Page` takes catalogue titles in any case, comma-separated (`pwsh -File` passes `a,b` as one string, so the script
splits it); quote a title with a space. A run needs at least one: the list writes no ready line to wait for. The
pages today are `Button`, `ButtonGroup` and `Theme`.

## What the app writes

The sample's shell writes, on standard output, which .NET for Android sends to logcat under the `DOTNET` tag (as
Cornflower's runs read it; not yet seen for this head), a line
for each page it opens, once the frame after the page's first is laid out, so a frame taken after it is the page at
rest:

```
METERED ready open=<ms>ms scaling=<s> view=<w>x<h> raw=<w>x<h> safe=<l>,<t>,<r>,<b> page=<w>x<h> title=<title>
```

`open` is the shell's time from before the page is made to that frame; for the start page, opened before the shell is
in the view, the time it took to make plus the time from the shell's joining the view, not Android's start between
(that is `TotalTime`). `view` is the view's size in view pixels and `raw` in raw pixels, `safe` the safe area, `page`
the page's own size under the shell's bar, and the title is last, as it can have spaces.

A Debug build writes after it what the page cost to open
([`docs/performance/README.md`](../../docs/performance/README.md#in-a-debug-build)):

```
METERED cost <title>: <ms> ms, <n> elements, <n> rules (<n>), layout <ms>, render <ms>
```

A cold start's cost has the app's start and the JIT in it. For what a page costs to open in a running app, as a user
opens it from the list, read the reopens: `-Configuration Debug -Reopen <n>`.

## What it writes

One folder per run, `artifacts/device/<yyyyMMdd-HHmmss>-<model>/` unless `-Out` says otherwise (`artifacts/` is
ignored by git; `-Out` has to be new or empty, so no earlier run's files are mixed with this one's), with a folder
per page, named from its title (`button/`, `buttongroup/`, `theme/`; a name already taken, by the same title again or
one that differs only in case or marks, gets `-2`, `-3`):

| File | What |
|---|---|
| `<page>/frame.png` | The phone's screen after the cold start, at its own resolution, status bar and all |
| `<page>/run.json` | The device, the APK, `launchState` and `totalTimeMs` from `am start -W`, the page (`page`), the cold start's `openMs`, and `opens`: every open in order, the cold start's then each reopen's, with its `openMs`, its ready line's facts (`ready`) and in Debug its cost line (`cost`) |
| `<page>/logcat.txt` | The page's whole log, one dump per open, also written when a step fails |
| `summary.json` | Every page's `run.json`, in order |

## The self-test

`-SelfTest` runs every parser the device run uses on `tools/android/fixtures/` and exits non-zero if one fails: the
`-Page` splitting, the page folders and the phone shell's quoting, the ready and cost lines (a cold start, a Debug run with reopens, a
title with spaces, lines that only look like the app's), when an open has come (a page's Nth ready line, its title,
and in Debug its cost line), `run.json`'s record of a page, crash reports (Java's, .NET's from the app's process or
with none known, another app's of either), and `am start`'s output (a cold start, one before Android 10 with no
`LaunchState`, a warm one, an intent delivered to the running activity, one that started a new activity, an error, a
timeout). It touches no adb
and no phone; CI runs it in the `android` job. What it can't reach is adb itself: the order of the steps, the waits,
the frame.

The fixtures are written in logcat's own format, as a Cornflower run on the Pixel wrote its lines. Their ready and
cost lines are held to the app by `tests/Metered.Tests/Sample/AndroidRunTests.cs`: each has the shape the shell writes
(the same keys in the same order, numbers where it writes numbers), and the script's markers, page extra, package and
activity are the app's. The crash fixtures are in the shape the script looks for, not yet read from a crash of this
app.

## adb and the SDK

`adb` is taken from `$ANDROID_HOME/platform-tools`, then `$ANDROID_SDK_ROOT/platform-tools`, then the SDK's default
folder (`%LOCALAPPDATA%\Android\Sdk` on Windows, `~/Library/Android/sdk` on macOS, `~/Android/Sdk` on Linux), then
`PATH`. Keep to one adb: a different one restarts the adb server, which can drop the phone's wireless pairing. The
build is given the SDK that adb is in (`-p:AndroidSdkDirectory`, as CI gives it `$ANDROID_HOME`).

Run it with `-NoProfile`, so that nothing in a PowerShell profile changes the run. The script also pins
`$PSNativeCommandArgumentPassing` (`Standard`, so a title's quotes reach adb intact) and
`$PSNativeCommandUseErrorActionPreference` (off, as each call's exit code is checked).

## A long path breaks the Release build on Windows

A Release build compiles the assemblies ahead of time with Mono's AOT compiler, and on Windows that fails from a clone
whose folder has a long path: `Precompiling failed for …\linked\Avalonia.Android.dll`, `Failed to load method
0x6000073`, for every assembly. Measured 2026-11-17 on one commit (.NET SDK 10.0.402, android workload 36.1.118): a
fresh clone at a 134-character path failed, its `linked\Avalonia.Android.dll` 235 characters long; a fresh clone at
`R:\source\Metered-wt\ah-clean` (29) built, in Release and in Debug, which has no AOT step. Clone near the drive's root.
CI builds on Linux, from a short path; Linux and macOS haven't been tried at a long one.

## On a phone

First run on 2026-11-17 (#29, #10), on a Pixel 8 over wireless ADB: Android 17 (API 37), 1080 × 2400 at 420 dpi, so a
render scaling of 2.625 and a view of 411.43 × 914.29. The ready and cost lines land in logcat under the `DOTNET` tag,
and the parsers read them as they read the fixtures. The fixtures' numbers are still made up in the phone's format.

| Release, warm reopens (5 each) | Opens, ms | Median |
|---|---|---|
| Button (24 Buttons) | 97, 124, 128, 126, 111 | 124 |
| Theme (7 Buttons) | 57, 50, 75, 45, 43 | 50 |

The cold starts were 730 to 783 ms (`am start -W`'s TotalTime). The difference between the two pages is about 4.4 ms
per Button, about seven times the headless cost. In Debug, the Button page's five warm reopens took 223 to 265 ms, with layout 150 to 186 ms of
that. Once, a cold Debug start's lines came before the view was sized (`view=1x1 scaling=1 page=1x0`, #37): Avalonia's
frames are the process's, and they ran while the view was still its 1 × 1 placeholder. The shell now waits for the view's
size, and for the frames after it, before it writes them, the wait left out of `open=` as the platform's; a view with no
size after 10 s is reported as it is, so a line with `view=1x1` says the view never got one.
