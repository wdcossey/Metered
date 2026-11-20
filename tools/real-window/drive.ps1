# Opens the real desktop sample window and uses it with real mouse and keyboard input, and touch through Windows'
# touch injection. Checks what it can, saves a frame of every page, and exits 1 if a check fails. See README.md here.
#
# It takes over the cursor and focus for a few seconds a page, so ask before running it on someone's PC (PROCESS §2),
# and run it through the desktop lease. Windows only, by nature: Win32 input, UI Automation and the screen. PowerShell 7
# or Windows PowerShell 5.1:
#   pwsh -NoProfile -File tools/real-window/drive.ps1
#   powershell -ExecutionPolicy Bypass -File tools\real-window\drive.ps1
#
# Ported from Cornflower's tools/real-window/drive.ps1 (wdcossey/CornflowerUI), which came from Avonic's: the window,
# the input, touch, the pixel reads, the frames, the every-page loop and -Only. The sections after the loop are
# Metered's. The pure helpers are in helpers.ps1, which selftest.ps1 checks.
param(
    # The build to run, as tools/android/run.ps1 takes it. A Debug build also has the cost line to check.
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug',
    # Run the build as it is, without building it first. A stale build runs the old code, and its failures
    # look like the new code's.
    [switch]$NoBuild,
    # Where the frames go. By default a new folder for each run, so that an old frame can't pass for a new one.
    [string]$Out = (Join-Path ([IO.Path]::GetTempPath()) ('metered-real-window\' + (Get-Date -Format 'yyyyMMdd-HHmmss'))),
    # Runs only the sections whose title is like one of these, comma-separated, such as 'switch*,radio*', to
    # work on them. The whole run is the gate.
    [string[]]$Only = '*'
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'helpers.ps1')
# From a PowerShell prompt, -Only a*,b* comes as an array; through -File, as one string. Either way, one comma list.
$Only = $Only -join ','
function Step([string]$text) { Write-Host "-- $text" }

$repo = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repo 'samples\Metered.Sample.Desktop'
$Exe = Join-Path $project "bin\$Configuration\net10.0\Metered.Sample.Desktop.exe"
$debugBuild = $Configuration -eq 'Debug'

if (-not $NoBuild) {
    # A sample left open locks its files, and the build would fail with only MSBuild's copy errors to say why.
    $open = @(Get-Process -Name 'Metered.Sample.Desktop' -ErrorAction SilentlyContinue |
        Where-Object { $_.Path -and ($_.Path -ieq $Exe) })
    if ($open.Count) {
        throw "The $Configuration sample is open (process $($open[0].Id)), which locks its files. Close it, or pass -NoBuild."
    }
    Step "Building the $Configuration desktop sample"
    # From the repo, so that its global.json picks the SDK. 'Continue', as Windows PowerShell 5.1 turns a line
    # dotnet writes to a redirected stderr into a stopping error; the exit code is what counts.
    Push-Location $repo
    $ErrorActionPreference = 'Continue'
    try {
        & dotnet build $project -c $Configuration -nologo -v q
        $built = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = 'Stop'
        Pop-Location
    }
    if ($built -ne 0) { throw "dotnet build failed ($built)." }
    if (-not (Test-Path $Exe)) {
        throw "Built, but there's no sample at $Exe. Has the sample's target framework or output path changed?"
    }
} elseif (-not (Test-Path $Exe)) {
    throw "No sample at $Exe. Build it, or leave out -NoBuild."
}
New-Item -ItemType Directory -Force $Out | Out-Null
$Out = (Resolve-Path $Out).Path
$stdout = Join-Path $Out 'stdout.txt'

Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing, System.Windows.Forms
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class RealInput {
  [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
  [DllImport("user32.dll")] public static extern bool SetProcessDpiAwarenessContext(IntPtr c);
  [DllImport("user32.dll")] public static extern bool GetCursorPos(out PT p);
  public static int[] Cursor() { PT p; GetCursorPos(out p); return new[] { p.x, p.y }; }
  [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
  [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
  [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
  [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
  [DllImport("user32.dll")] static extern void mouse_event(uint f, uint x, uint y, uint d, IntPtr e);
  [DllImport("user32.dll")] public static extern int GetSystemMetrics(int i);
  public static void Down() { mouse_event(2, 0, 0, 0, IntPtr.Zero); }
  public static void Up() { mouse_event(4, 0, 0, 0, IntPtr.Zero); }
  [DllImport("user32.dll")] static extern bool AttachThreadInput(uint from, uint to, bool attach);
  [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
  [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
  // Windows refuses SetForegroundWindow to a process that didn't receive the last input, as when the terminal that
  // started this is minimised and Explorer holds the focus. Sharing the front window's input state for the call lets
  // it through without sending any input to that window.
  public static bool Front(IntPtr h) {
    uint pid; uint them = GetWindowThreadProcessId(GetForegroundWindow(), out pid), me = GetCurrentThreadId();
    bool joined = them != 0 && them != me && AttachThreadInput(me, them, true);
    try { BringWindowToTop(h); return SetForegroundWindow(h); }
    finally { if (joined) AttachThreadInput(me, them, false); }
  }

  // Touch, through InjectTouchInput: one contact, with no feedback drawn (TOUCH_FEEDBACK_NONE), so frames show only the app.
  [StructLayout(LayoutKind.Sequential)] public struct PT { public int x, y; }
  [StructLayout(LayoutKind.Sequential)] struct RC { public int l, t, r, b; }
  [StructLayout(LayoutKind.Sequential)] struct PI {
    public uint type, id, frame, flags; public IntPtr device, hwnd;
    public PT px, him, pxRaw, himRaw; public uint time, history; public int data; public uint keys;
    public ulong perf; public int change; }
  [StructLayout(LayoutKind.Sequential)] struct TI { public PI p; public uint tf, mask; public RC c, cr; public uint orient, press; }
  [DllImport("user32.dll", SetLastError = true)] static extern bool InitializeTouchInjection(uint n, uint mode);
  [DllImport("user32.dll", SetLastError = true)] static extern bool InjectTouchInput(uint n, ref TI t);
  public static bool TouchInit() { return InitializeTouchInjection(1, 3); }
  // The pointer id has to be below the count given to InitializeTouchInjection.
  static void Send(int x, int y, uint flags) {
    var t = new TI();
    t.p.type = 2; t.p.id = 0; t.p.flags = flags; t.p.px.x = x; t.p.px.y = y;
    t.mask = 7; t.orient = 90; t.press = 32000;
    t.c.l = x - 2; t.c.r = x + 2; t.c.t = y - 2; t.c.b = y + 2;
    if (!InjectTouchInput(1, ref t)) throw new InvalidOperationException("Touch injection failed: " + Marshal.GetLastWin32Error());
  }
  public static void TouchDown(int x, int y) { Send(x, y, 0x10000 | 2 | 4); }
  public static void TouchMove(int x, int y) { Send(x, y, 0x20000 | 2 | 4); }
  public static void TouchUp(int x, int y) { Send(x, y, 0x40000); }

  // Enter held and let go on its own, so that the focus can move between its press and its release (#49): SendKeys
  // can only send a whole key.
  [DllImport("user32.dll")] static extern void keybd_event(byte vk, byte scan, uint flags, UIntPtr extra);
  public static void EnterDown() { keybd_event(0x0D, 0, 0, UIntPtr.Zero); }
  public static void EnterUp() { keybd_event(0x0D, 0, 2, UIntPtr.Zero); }
}
'@
# Under PowerShell 7, System.Drawing is a facade that forwards Bitmap to System.Drawing.Common.
$drawing = if ($PSVersionTable.PSEdition -eq 'Core') { 'System.Drawing.Common', 'System.Drawing.Primitives' } else { 'System.Drawing' }
Add-Type -ReferencedAssemblies $drawing @'
using System; using System.Drawing; using System.Drawing.Imaging; using System.Runtime.InteropServices;
public static class Pixels {
  // How many of every other pixel differ between two regions of one size.
  public static int Differ(Bitmap a, Bitmap b) {
    if (a.Width != b.Width || a.Height != b.Height) throw new ArgumentException("The regions differ in size");
    var r = new Rectangle(0, 0, a.Width, a.Height);
    var da = a.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    var db = b.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
    try {
      int n = 0;
      for (int y = 0; y < a.Height; y += 2)
        for (int x = 0; x < a.Width; x += 2)
          if (Marshal.ReadInt32(da.Scan0, y * da.Stride + x * 4) != Marshal.ReadInt32(db.Scan0, y * db.Stride + x * 4)) n++;
      return n;
    } finally { a.UnlockBits(da); b.UnlockBits(db); }
  }
}
'@
# Per-monitor aware (-4, PER_MONITOR_AWARE_V2), so that UI Automation's rectangles, the cursor and a touch are the screen's
# own pixels on any monitor, as the ready line's scaling is: system aware, a window on a monitor at another scale would be
# scaled by Windows and every Px would miss. A host that has set it already keeps its own; then system aware, as before.
if (-not [RealInput]::SetProcessDpiAwarenessContext([IntPtr]::new(-4))) { [void][RealInput]::SetProcessDPIAware() }
$AE = [System.Windows.Automation.AutomationElement]
$TS = [System.Windows.Automation.TreeScope]
$CT = [System.Windows.Automation.ControlType]
$SendKeys = [System.Windows.Forms.SendKeys]
$cursor = [System.Windows.Forms.Cursor]::Position
$script:failures = 0
$script:checks = 0
$script:sections = 0
# The page's render scaling, from its ready line: set by Open, read by Px.
$script:scale = 0

# From the screen, not PrintWindow, which offsets a window with no frame of its own by its invisible border.
# A region of the screen, which the caller disposes of, or hands to Changed as the region taken now.
function Region($x, $y, $wd, $ht) {
    $bmp = New-Object Drawing.Bitmap ([int]$wd), ([int]$ht)
    $g = [Drawing.Graphics]::FromImage($bmp)
    try { $g.CopyFromScreen([int]$x, [int]$y, 0, 0, $bmp.Size) } finally { $g.Dispose() }
    $bmp
}
# The region a control covers on the screen, as UI Automation reports it.
# Each read of the screen first makes sure the sample is in front, as a picture of another app's window would compare as
# well as one of the sample's: a baseline of the wrong window passes or fails checks falsely.
function Look($el) { Guard; $b = $el.Current.BoundingRectangle; Region $b.X $b.Y $b.Width $b.Height }
function Shot($name) {
    Guard
    $r = $win.Current.BoundingRectangle
    $bmp = Region $r.X $r.Y $r.Width $r.Height
    try { $bmp.Save((Join-Path $Out "$name.png")) } finally { $bmp.Dispose() }
}
# One pixel of the screen, as ARGB.
function Pixel($x, $y) {
    Guard
    $bmp = Region $x $y 1 1
    try { $bmp.GetPixel(0, 0).ToArgb() } finally { $bmp.Dispose() }
}
# How many of every other pixel differ between a region taken earlier, which the caller keeps, and one taken now, which this
# disposes of. A disposed bitmap's size reads as nothing in PowerShell, so a reused one would compare nothing and pass.
function Changed($before, $now) {
    try { [Pixels]::Differ($before, $now) } finally { $now.Dispose() }
}
function All($type) {
    $cond = New-Object Windows.Automation.PropertyCondition $AE::ControlTypeProperty, $type
    $win.FindAll($TS::Descendants, $cond) | ForEach-Object { $_ }
}
# By the exact name, case and all: a page's title is a name, not a pattern.
function Named($type, $name) { All $type | Where-Object { $_.Current.Name -ceq $name } | Select-Object -First 1 }
# A control on the page by its x:Name, which Avalonia gives UI Automation as its AutomationId.
function ById($id) {
    $el = $win.FindFirst($TS::Descendants, (New-Object Windows.Automation.PropertyCondition $AE::AutomationIdProperty, $id))
    if (-not $el) { throw "Nothing on the page has the AutomationId $id" }
    $el
}
# A container by its x:Name that isn't in UI Automation's control view, as a slider's root isn't (its thumbs are the
# controls, as Base Web's are): searched in the raw view. Only for a container whose bounds a section aims along; a
# control is found by ById, which would miss one left out of the control view, as it caught #56.
function RawById($id) {
    $request = New-Object Windows.Automation.CacheRequest
    $request.TreeFilter = [Windows.Automation.Automation]::RawViewCondition
    $request.Push()
    try { $el = $win.FindFirst($TS::Descendants, (New-Object Windows.Automation.PropertyCondition $AE::AutomationIdProperty, $id)) }
    finally { $request.Pop() }
    if (-not $el) { throw "Nothing on the page, in the raw view either, has the AutomationId $id" }
    $el
}
# Scrolls the page's scroll viewer to its end through UI Automation's scroll pattern, which sends no input, so that a
# control below the fold can be pressed inside the window.
function ScrollToEnd {
    $cond = New-Object Windows.Automation.PropertyCondition $AE::IsScrollPatternAvailableProperty, $true
    $viewer = $win.FindFirst($TS::Descendants, $cond)
    if (-not $viewer) { throw 'The page has no scroll viewer UI Automation can scroll' }
    $viewer.GetCurrentPattern([Windows.Automation.ScrollPattern]::Pattern).SetScrollPercent(-1, 100)
    Start-Sleep -Milliseconds 400
}
# A TextBlock on the page that a section reads, by its x:Name: UI Automation's name for it is its text, read live from
# .Current.Name. ById by another name, so that selftest.ps1 can tell the two apart; a section finds it once, as a search
# of the whole tree takes long enough to shift a timed read.
function ReadOut($id) { ById $id }
function Texts { (All $CT::Text | ForEach-Object { $_.Current.Name } | Where-Object { $_ }) }
function FirstText($like) { Texts | Where-Object { $_ -like $like } | Select-Object -First 1 }
# A scroll bar's own buttons, which have no size at its ends and aren't the app's.
$scrollParts = 'Line up', 'Page up', 'Page down', 'Line down', 'Line left', 'Page left', 'Page right', 'Line right'
function AppButtons { All $CT::Button | Where-Object { $_.Current.Name -and $_.Current.Name -notin $scrollParts } }
# The list's buttons: the bar's Light/Dark switch is a button too, and is no page.
function PageLinks { AppButtons | Where-Object { $_.Current.AutomationId -cne 'Variant' } }
# A checkbox's or a switch's state through UI Automation's toggle pattern: On, Off, or Indeterminate.
function Toggled($el) { "$($el.GetCurrentPattern([Windows.Automation.TogglePattern]::Pattern).Current.ToggleState)" }
# Throws rather than aim at the corner of the screen, where a click could land in someone's app.
function Centre($el) {
    if (-not $el) { throw 'Nothing to aim at: the element was not found' }
    $b = $el.Current.BoundingRectangle
    if ([double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or [double]::IsNaN($b.Width) -or $b.Width -le 0 -or $b.Height -le 0) { throw "Nothing to aim at: $($el.Current.Name) has no size" }
    [int]($b.X + $b.Width / 2), [int]($b.Y + $b.Height / 2)
}
# On a labelled control's mark (a checkbox's box, a radio's ring, a switch's track), which is at its start, on the first
# line: $in view px in from its left, and half a line down or its middle, whichever is higher. The root is a flex row
# whose empty end takes no input, so its middle can miss.
function Mark($el, $in = 14) {
    $null = Centre $el
    $b = $el.Current.BoundingRectangle
    [int]($b.X + (Px $in)), [int]($b.Y + [math]::Min($b.Height / 2, (Px 14)))
}
# View pixels to the screen's, at the open page's scaling.
function Px($dip) {
    if (-not $script:scale) { throw 'No scaling yet: open a page first' }
    [int][math]::Round($dip * $script:scale)
}
# Windows lets a process that isn't in front only ask, so it also asks through UI Automation, which the app itself acts on.
function Fore {
    # Front shares the front window's input state for the call, so it works with the terminal minimised (RealInput).
    [void][RealInput]::Front($p.MainWindowHandle)
    if ($win -and [RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle) { try { $win.SetFocus() } catch { } }
    Start-Sleep -Milliseconds 150
}
# Called before any input, so that nothing lands in another app if something else takes the focus.
function Guard {
    # A few tries: just after it starts, Windows can be slow to bring the sample forward.
    for ($try = 0; $try -lt 5 -and [RealInput]::GetForegroundWindow() -ne $p.MainWindowHandle; $try++) { Fore }
    $front = [RealInput]::GetForegroundWindow()
    if ($front -ne $p.MainWindowHandle) {
        $owner = 0; [void][RealInput]::GetWindowThreadProcessId($front, [ref]$owner)
        $name = try { (Get-Process -Id $owner).ProcessName } catch { "process $owner" }
        throw "The sample is not in front ($name is), so no input was sent"
    }
}
# Refuses a point outside the window, such as a button below the fold of a long list, which would land in whatever is under it.
function InWindow($x, $y) { $w = $win.Current.BoundingRectangle; Inside $w.X $w.Y $w.Right $w.Bottom $x $y }
# Throws rather than send input to a point outside the sample's window, where it would land in another app.
function Within($x, $y) { if (-not (InWindow $x $y)) { throw "$x, $y is outside the window, so no input was sent" } }
# Where a press may go once the sample holds it, and Windows still takes a touch: anywhere on the screens.
function OnScreen($x, $y) {
    if (-not [System.Windows.Forms.SystemInformation]::VirtualScreen.Contains([int]$x, [int]$y)) { throw "$x, $y is off the screen, so no input was sent" }
}
# The cursor to the window's top left corner, in the bar, off every control the sections use.
function Away { Guard; $r = $win.Current.BoundingRectangle; [void][RealInput]::SetCursorPos(([int]$r.X + 4), ([int]$r.Y + 4)); Start-Sleep -Milliseconds 250 }
function Key($text, $ms = 400) { Guard; $SendKeys::SendWait($text); Start-Sleep -Milliseconds $ms }
function Press { Guard; $script:held = $true; [RealInput]::Down() }
function Release { [RealInput]::Up(); $script:held = $false }
# Lets go of whatever an exception left held, on the user's desktop as well as in the sample: a touch is lifted where it
# last was, as Windows refuses a lift anywhere else.
function LetGo {
    if ($script:held) { Release }
    if ($script:enterHeld) { $script:enterHeld = $false; try { [RealInput]::EnterUp() } catch { } }
    if ($script:touching) {
        $script:touching = $false
        try { [RealInput]::TouchUp($script:touchX, $script:touchY) } catch { }
    }
}
function TouchDown($x, $y) {
    if (-not $script:touch) { throw 'Touch injection did not start on this PC (InitializeTouchInjection failed), so no touch was sent' }
    Guard; Within $x $y; $script:touching = $true; $script:touchX = $x; $script:touchY = $y; [RealInput]::TouchDown($x, $y)
}
function TouchMove($x, $y) { OnScreen $x $y; [RealInput]::TouchMove($x, $y); $script:touchX = $x; $script:touchY = $y }
function TouchUp { [RealInput]::TouchUp($script:touchX, $script:touchY); $script:touching = $false }
# A touch held still for a while, reported every 50 ms as a digitiser does, so the app sees pointer events all along.
function Hold($ms) {
    $until = (Get-Date).AddMilliseconds($ms)
    while ((Get-Date) -lt $until) { Start-Sleep -Milliseconds 50; TouchMove $script:touchX $script:touchY }
}
function Tap($x, $y) {
    TouchDown $x $y; Start-Sleep -Milliseconds 40
    TouchMove $x $y; Start-Sleep -Milliseconds 40
    TouchUp; Start-Sleep -Milliseconds 400
}
# A touch slid to a point in steps, reported every $ms, as a finger moves. From Cornflower's.
function Slide($x, $y, $steps = 10, $ms = 16) {
    foreach ($at in DragPath $script:touchX $script:touchY ($x - $script:touchX) ($y - $script:touchY) $steps) {
        Start-Sleep -Milliseconds $ms; TouchMove $at.X $at.Y
    }
}
# A drag in any direction, in screen pixels, by touch and by the mouse. From Cornflower's.
function TouchDrag($x, $y, $dx, $dy, $steps = 15, $ms = 16) {
    # Windows refuses a touch off the screen (87) partway through.
    OnScreen ($x + $dx) ($y + $dy)
    TouchDown $x $y
    Slide ($x + $dx) ($y + $dy) $steps $ms
    TouchUp
}
function MouseDrag($x, $y, $dx, $dy, $steps = 15, $ms = 16) {
    Guard
    Within $x $y; OnScreen ($x + $dx) ($y + $dy)
    [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
    Press
    foreach ($at in DragPath $x $y $dx $dy $steps) { Start-Sleep -Milliseconds $ms; [void][RealInput]::SetCursorPos($at.X, $at.Y) }
    Start-Sleep -Milliseconds 60
    Release
}
function ClickAt($x, $y) {
    Within $x $y
    Guard
    [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 120
    Press; Start-Sleep -Milliseconds 50; Release; Start-Sleep -Milliseconds 600
}
function Click($el) { $x, $y = Centre $el; ClickAt $x $y }
# A tap and a click on a labelled control's mark (Mark).
function TapMark($el, $in = 14) { $x, $y = Mark $el $in; Tap $x $y }
function ClickMark($el, $in = 14) { $x, $y = Mark $el $in; ClickAt $x $y }
function IsFocused($el) { $f = $AE::FocusedElement; [bool]($f -and $el -and [Windows.Automation.Automation]::Compare($f, $el)) }
function Invoke-Element($el) { $el.GetCurrentPattern([Windows.Automation.InvokePattern]::Pattern).Invoke(); Start-Sleep -Milliseconds 600 }
# What a page reports comes after layout, so it can take a moment.
function WaitForText($like, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $found = FirstText $like
        if ($found) { return $found }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
# The sample's standard output, which the app is still writing to.
function ReadyLines {
    try {
        $stream = [IO.File]::Open($stdout, 'Open', 'Read', 'ReadWrite')
        # Only whole lines: the last, without its line break yet, may still be being written.
        try { $reader = New-Object IO.StreamReader $stream; @(WholeLines $reader.ReadToEnd() | Where-Object { $_ -like 'METERED ready*' }) }
        finally { $stream.Dispose() }
    }
    catch { @() }
}
# The next ready line after the $seen-th: the shell writes one for each page it opens, once the page has drawn. Its
# fields are read by helpers.ps1's PageOf, ScalingOf and TitleOf.
function WaitForReady($seen, $seconds = 5) {
    $until = (Get-Date).AddSeconds($seconds)
    do {
        $lines = @(ReadyLines)
        if ($lines.Count -gt $seen) { return $lines[$seen] }
        Start-Sleep -Milliseconds 100
    } while ((Get-Date) -lt $until)
}
# The list: the bar's title is the shell's own (SampleShell.IndexTitle), and Back is hidden.
function OnIndex { [bool](Named $CT::Text 'Metered') -and -not (Named $CT::Button 'Back') }
function Index { $back = Named $CT::Button 'Back'; if ($back) { Invoke-Element $back } }
# Opens a page afresh, from the list, and takes its scaling from its ready line, which it keeps in $script:ready.
function Open($title) {
    Index
    $link = Named $CT::Button $title
    if (-not $link) { throw "$title isn't on the list" }
    $seen = @(ReadyLines).Count
    Invoke-Element $link
    $line = WaitForReady $seen
    if (-not $line) { throw "$title wrote no ready line" }
    if ($line -notmatch 'scaling=[\d.]+ ') { throw "$title's ready line has no scaling, so nothing can be aimed: $line" }
    $script:ready = $line
    $script:scale = ScalingOf $line
    Away
}
function Section($title, [scriptblock]$body) {
    if (-not (Selected $title $Only)) { return }
    $script:sections++
    "== $title"
    try { & $body }
    # Counted as a check, so the summary's count of checks includes it.
    catch { LetGo; $script:checks++; $script:failures++; "  FAIL  the section stopped: $($_.Exception.Message) at line $($_.InvocationInfo.ScriptLineNumber)" }
    if ($p.HasExited) { throw "The app exited during $title, with code $($p.ExitCode)" }
}

$p = Start-Process $Exe -PassThru -RedirectStandardOutput $stdout
# A process from Start-Process -PassThru reports no ExitCode unless its handle was opened while it ran.
$null = $p.Handle
try {
    $win = $null
    for ($i = 0; $i -lt 40 -and -not $win; $i++) {
        Start-Sleep -Milliseconds 500
        $p.Refresh()
        if ($p.MainWindowHandle -ne 0) { $win = $AE::FromHandle($p.MainWindowHandle) }
    }
    if (-not $win) { throw 'The window never appeared' }
    Fore
    Start-Sleep -Milliseconds 500
    $r = $win.Current.BoundingRectangle
    $area = [System.Windows.Forms.Screen]::FromHandle($p.MainWindowHandle).WorkingArea
    if ($r.Bottom -gt $area.Bottom -or $r.Right -gt $area.Right -or $r.Y -lt $area.Y -or $r.X -lt $area.X) {
        throw "The window ($r) doesn't fit on the screen ($area), so clicks would miss and frames would be cut off. Move it, or if it's taller than the screen, lower the display scale."
    }
    # Before starting injection, which adds a contact of its own.
    $contacts = [RealInput]::GetSystemMetrics(95)
    $script:touch = [RealInput]::TouchInit()
    "$Exe"
    "window $($r.Width) x $($r.Height) on the screen, frames in $Out"
    "touch hardware contacts $contacts, touch injection $($script:touch)"
    $titles = @(PageLinks | ForEach-Object { $_.Current.Name })
    Shot 'index'
    Check 'the list has a page' ($titles.Count -gt 0)

    Section 'Every page on the list: a click opens it and Back leaves it' {
        foreach ($title in $titles) {
          # Each page on its own, so that one that throws is a failure of its own and the pages after it still run.
          try {
            Index
            $link = Named $CT::Button $title
            $x, $y = Centre $link
            $seen = @(ReadyLines).Count
            # A list longer than the window: what's below the fold is opened through UI Automation instead.
            if (InWindow $x $y) { Click $link } else { "        $title is below the fold: opened through UI Automation"; Invoke-Element $link }
            Check "a click on '$title' opens it" ([bool](Named $CT::Button 'Back'))
            # The shell times every page and writes its ready line, with no code in the page.
            $ready = WaitForReady $seen
            Check "$title reports ready, under its title" ((TitleOf $ready) -ceq $title) "$ready"
            if ($ready) { $script:scale = ScalingOf $ready }
            Away
            Shot $title
            # A control with no size is invisible, as Avonic's fixed slot's button once was.
            # Buttons, and the ButtonGroup's pills, which a group gives the check box or radio button type.
            $empty = @(@(AppButtons) + @(All $CT::CheckBox) + @(All $CT::RadioButton) | Where-Object {
                $b = $_.Current.BoundingRectangle
                [double]::IsInfinity($b.Width) -or [double]::IsInfinity($b.Height) -or $b.Width -eq 0 -or $b.Height -eq 0
            } | ForEach-Object { $_.Current.Name })
            Check "$title has no button, check box or radio without a size" ($empty.Count -eq 0) ($empty -join ', ')
            if ($debugBuild) {
                # The shell's Debug line under the title (PerformanceMonitor): '<title>: <ms> ms, <elements> elements, ...'.
                $line = WaitForText "$(Literal $title): * ms, *"
                Check "$title shows its cost under its title" ([bool]$line) ''
                if ($line) { "        $line" }
            }
            Click (Named $CT::Button 'Back')
            Check "Back from '$title' shows the list" (OnIndex)
          }
          catch { LetGo; Check "'$title' could be opened and left" $false "$($_.Exception.Message) at line $($_.InvocationInfo.ScriptLineNumber)" }
        }
    }

    Section "The page opens at the phone's view, under the bar, to the pixel" {
        Open 'Theme'
        $line = $script:ready
        "        $line"
        $w, $h = PageOf $line
        $scaling = ScalingOf $line
        Check 'the ready line is the Theme page''s' ((TitleOf $line) -ceq 'Theme') "$line"
        Check "the page is $WindowWidth x $($WindowHeight - $BarHeight): the window's $WindowWidth x $WindowHeight less the bar's $BarHeight" (
            (WithinAPixel $w $WindowWidth $scaling) -and (WithinAPixel $h ($WindowHeight - $BarHeight) $scaling)) "$line"
    }

    Section 'Alt+Left goes back' {
        Open 'Theme'
        Key '%{LEFT}'
        Check 'Alt+Left on a page shows the list' (OnIndex)
    }

    # The bar's switch sets the window's theme variant: its label names the other, and the page's background follows.
    Section 'Light and Dark: the bar''s switch' {
        Open 'Theme'
        Index
        $variant = ById 'Variant'
        $wr = $win.Current.BoundingRectangle
        # Low in the list's left margin, below its last page: the background, and nothing drawn on it.
        $bx = [int]$wr.X + (Px 6); $by = [int]$wr.Bottom - (Px 12)
        $light = Pixel $bx $by
        Check 'the switch offers Dark while the window is light' ($variant.Current.Name -ceq 'Dark') $variant.Current.Name
        Click $variant
        Away
        Start-Sleep -Milliseconds 300
        $dark = Pixel $bx $by
        Shot 'index-dark'
        Check 'a click turns it to Light' ($variant.Current.Name -ceq 'Light') $variant.Current.Name
        Check 'and the background changes' ($dark -ne $light) ('{0:X8} against {1:X8}' -f $dark, $light)
        Click $variant
        Away
        Start-Sleep -Milliseconds 300
        $back = Pixel $bx $by
        Check 'a second click turns it back to Dark' ($variant.Current.Name -ceq 'Dark') $variant.Current.Name
        Check 'and the background is as it was' ($back -eq $light) ('{0:X8} against {1:X8}' -f $back, $light)
    }

    # Base Web's Button: :hover from the mouse, :active from any press. Avalonia's Win32 backend gives an injected touch
    # as a touch pointer; whether a tap leaves :pointerover behind (a hover stuck on a phone) is what the last check reads.
    Section 'button: the mouse hovers it, a held touch presses it, a tap leaves nothing drawn' {
        Open 'Button'
        $primary = Named $CT::Button 'Primary'
        $x, $y = Centre $primary
        $rest = Look $primary
        try {
            Guard
            Within $x $y; [void][RealInput]::SetCursorPos($x, $y); Start-Sleep -Milliseconds 400
            $hovered = Changed $rest (Look $primary)
            Shot 'button-hovered'
            Check 'the mouse over Primary draws its hover' ($hovered -gt $Moved) "$hovered sampled pixels differ"
            Away
            # The Button's 200 ms background transition (Button.Parts.g.axaml, from Base Web's), with room to spare.
            Start-Sleep -Milliseconds 300
            $left = Changed $rest (Look $primary)
            Check 'and it goes when the mouse leaves' ($left -lt $Same) "$left sampled pixels differ"

            TouchDown $x $y
            Hold 300
            $pressed = Changed $rest (Look $primary)
            Shot 'button-touch-held'
            TouchUp
            Start-Sleep -Milliseconds 600
            Check 'a held touch draws it pressed' ($pressed -gt $Moved) "$pressed sampled pixels differ"
            $afterTap = Changed $rest (Look $primary)
            $at = [RealInput]::Cursor()
            $pb = $primary.Current.BoundingRectangle
            $verdict = TapVerdict $hovered $afterTap (Inside $pb.X $pb.Y $pb.Right $pb.Bottom $at[0] $at[1])
            "        after the touch is lifted, Primary is $verdict ($afterTap sampled pixels differ from its rest; the cursor at $($at[0]), $($at[1]))"
            # Windows moves the cursor to a touch it injects, which hovers the button as a mouse would; then the frames can't
            # tell a hover stuck by the touch from the mouse's, and only a phone can (tools/android). Counted only when it
            # settles, so a run never reads as having shown what it couldn't.
            if ($verdict -in 'at rest', 'left drawn') { Check 'a touch, once lifted, leaves no hover or press drawn' ($verdict -eq 'at rest') $verdict }
            else { "        not counted: whether a lifted touch leaves a hover can't be told on this PC" }
        }
        finally { $rest.Dispose() }
    }

    Section 'button: Click to load shows the spinner, then ends' {
        Open 'Button'
        $loading = ById 'Loading'
        $rest = Look $loading
        try {
            Click $loading
            $first = Look $loading
            try {
                Start-Sleep -Milliseconds 250
                $turned = Changed $first (Look $loading)
            }
            finally { $first.Dispose() }
            Shot 'button-loading'
            Check 'a click shows the spinner, turning' ($turned -gt $Moved) "$turned sampled pixels differ in 250 ms"
            Away
            # ButtonPage.LoadingFor is 2 s, from the click.
            Start-Sleep -Milliseconds 2200
            $ended = Changed $rest (Look $loading)
            Check 'and after 2 s the button is drawn as it was' ($ended -lt $Same) "$ended sampled pixels differ"
        }
        finally { $rest.Dispose() }
    }

    # The checkbox group of pills: the page's line says the last click's index and the selection. A group in checkbox mode
    # gives each of its buttons UI Automation's check box type (ButtonGroup's ControlTypeOverride).
    Section 'buttongroup: taps and clicks on the checkbox group' {
        Open 'ButtonGroup'
        $clicked = ReadOut 'Clicked'
        function Said { $clicked.Current.Name }
        Check 'nothing is clicked yet' ((Said) -ceq 'Clicked: none') (Said)
        $tx, $ty = Centre (Named $CT::CheckBox 'Parking')
        Tap $tx $ty
        Check 'a tap on Parking selects it' (((After (Said) 'Clicked') -eq '1') -and ((After (Said) 'selected') -eq '1')) (Said)
        Click (Named $CT::CheckBox 'Pool')
        Check 'a click on Pool adds it' (((After (Said) 'Clicked') -eq '2') -and ((After (Said) 'selected') -eq '1, 2')) (Said)
        Tap $tx $ty
        Check 'a second tap on Parking takes it off' (((After (Said) 'Clicked') -eq '1') -and ((After (Said) 'selected') -eq '2')) (Said)
        Away
    }

    # checkbox-v2: the page's parent box follows its two children, and UI Automation reads indeterminate as mixed
    # (CheckboxAutomationPeer). Space, and an Enter pressed on it (#49), toggle the focused box; a held touch draws the
    # backplate's pressed tint (#49: kept, where Base Web shows none).
    Section 'checkbox: taps, the indeterminate parent, Space, Enter and the pressed tint' {
        Open 'Checkbox'
        $all = ById 'AllToppings'; $cheese = ById 'Cheese'; $olives = ById 'Olives'
        function States { "$(Toggled $all) / $(Toggled $cheese) / $(Toggled $olives)" }
        Check 'the parent starts indeterminate, as one child is checked' ((States) -ceq 'Indeterminate / On / Off') (States)
        TapMark $olives
        Check 'a tap on Olives checks it, and both children checked check the parent' ((States) -ceq 'On / On / On') (States)
        TapMark $all
        Check 'a tap on the parent clears both' ((States) -ceq 'Off / Off / Off') (States)
        ClickMark $cheese
        Check 'a click on Cheese checks it, and the parent is indeterminate again' ((States) -ceq 'Indeterminate / On / Off') (States)
        Check 'and focuses it' (IsFocused $cheese) "$($AE::FocusedElement.Current.Name)"
        Away
        Key ' '
        Check 'Space on it clears it' ((Toggled $cheese) -ceq 'Off') (States)
        Key '{ENTER}'
        Check 'an Enter on it checks it' ((Toggled $cheese) -ceq 'On') (States)

        $box = Named $CT::CheckBox 'Checkbox'
        $rest = Look $box
        try {
            $bx, $by = Mark $box
            TouchDown $bx $by
            Hold 300
            $tinted = Changed $rest (Look $box)
            Shot 'checkbox-touch-held'
            TouchUp
            Start-Sleep -Milliseconds 400
            Check 'a held touch draws the pressed tint' ($tinted -gt $Moved) "$tinted sampled pixels differ"
            Check 'and lifted, checks it' ((Toggled $box) -ceq 'On') (Toggled $box)
            TapMark $box
            Check 'a second tap clears it' ((Toggled $box) -ceq 'Off') (Toggled $box)
        }
        finally { $rest.Dispose() }
    }

    # radio-v2 in a RadioGroup: the page's line says the group's value. The mouse hovers a radio, and a held touch draws
    # the backplate's pressed tint (#49: kept); a tap selects, and the arrow keys move the selection, wrapping.
    Section 'radio: hover, the pressed tint, taps and the arrow keys' {
        Open 'Radio'
        $selected = ReadOut 'Selected'
        function Value { After $selected.Current.Name 'Selected' }
        Check 'the group starts on its second' ((Value) -eq '2') $selected.Current.Name
        $second = Named $CT::RadioButton 'Second'
        $rest = Look $second
        try {
            Guard
            $sx, $sy = Mark $second
            Within $sx $sy; [void][RealInput]::SetCursorPos($sx, $sy); Start-Sleep -Milliseconds 400
            $hovered = Changed $rest (Look $second)
            Check 'the mouse over a radio draws its hover' ($hovered -gt $Moved) "$hovered sampled pixels differ"
            Away
            TouchDown $sx $sy
            Hold 300
            $tinted = Changed $rest (Look $second)
            Shot 'radio-touch-held'
            TouchUp
            Start-Sleep -Milliseconds 400
            Check 'a held touch draws the pressed tint' ($tinted -gt $Moved) "$tinted sampled pixels differ"
            Check 'and the checked radio stays checked' ((Value) -eq '2') $selected.Current.Name
        }
        finally { $rest.Dispose() }

        TapMark (Named $CT::RadioButton 'Third')
        Check 'a tap on Third selects it' ((Value) -eq '3') $selected.Current.Name
        $first = Named $CT::RadioButton 'First'
        ClickMark $first
        Check 'a click on First selects it' ((Value) -eq '1') $selected.Current.Name
        Check 'and focuses it' (IsFocused $first) "$($AE::FocusedElement.Current.Name)"
        Away
        Key '{DOWN}'
        Check 'Down moves the selection to Second' ((Value) -eq '2') $selected.Current.Name
        Key '{UP}'
        Key '{UP}'
        Check 'Up from First wraps to Third' ((Value) -eq '3') $selected.Current.Name
    }

    # Base Web's switch: the page's line under the first says whether it is on. Touch, the mouse, Space and an Enter
    # pressed on it each toggle it.
    Section 'switch: a tap, a click, Space and Enter' {
        Open 'Switch'
        $basic = ById 'Basic'
        $state = ReadOut 'State'
        function State { $state.Current.Name }
        Check 'the first switch starts off' ((State) -ceq 'Off') (State)
        TapMark $basic 20
        Check 'a tap on its track turns it on' ((State) -ceq 'On') (State)
        Check 'and UI Automation reads it on' ((Toggled $basic) -ceq 'On') (Toggled $basic)
        ClickMark $basic 20
        Check 'a click turns it off' ((State) -ceq 'Off') (State)
        Check 'and focuses it' (IsFocused $basic) "$($AE::FocusedElement.Current.Name)"
        Away
        Key ' '
        Check 'Space on it turns it on' ((State) -ceq 'On') (State)
        Key '{ENTER}'
        Check 'an Enter on it turns it off' ((State) -ceq 'Off') (State)

        # #49: an Enter pressed on the next switch and let go once Basic has the focus is neither's: Basic stays off,
        # and the switch it was pressed on is as it was.
        $multiline = ById 'Multiline'
        $before = Toggled $multiline
        Key '{TAB}'
        Check 'Tab moves the focus to the next switch' (IsFocused $multiline) "$($AE::FocusedElement.Current.Name)"
        Guard; $script:enterHeld = $true; [RealInput]::EnterDown(); Start-Sleep -Milliseconds 200
        Key '+{TAB}'
        [RealInput]::EnterUp(); $script:enterHeld = $false; Start-Sleep -Milliseconds 400
        Check 'Shift+Tab with Enter held brings the focus back' (IsFocused $basic) "$($AE::FocusedElement.Current.Name)"
        Check 'an Enter pressed elsewhere and let go on it leaves it off' ((State) -ceq 'Off') (State)
        Check 'and leaves the switch it was pressed on as it was' ((Toggled $multiline) -ceq $before) (Toggled $multiline)
    }

    # Base Web's stepped bar (steps.tsx): Next and Previous move it a step; UI Automation reads its range, read only,
    # and the name Base Web gives it (#53).
    Section 'progress bar: the steps buttons and its name' {
        Open 'ProgressBar'
        $steps = ById 'Steps'
        $range = $steps.GetCurrentPattern([Windows.Automation.RangeValuePattern]::Pattern)
        Check 'the stepped bar starts at 40' ($range.Current.Value -eq 40) $range.Current.Value
        Check 'and is named as Base Web names it' ($steps.Current.Name -ceq 'Step 2 of 5') $steps.Current.Name
        Check 'and its range is read only' ($range.Current.IsReadOnly)
        # At the foot of the page, below the fold of a phone's window.
        ScrollToEnd
        Click (ById 'Next')
        Check 'a click on Next moves it a step' ($range.Current.Value -eq 60) $range.Current.Value
        Away
        $px, $py = Centre (ById 'Previous')
        Tap $px $py
        Check 'a tap on Previous moves it back' ($range.Current.Value -eq 40) $range.Current.Value
        Away
    }

    # Base Web's touchmove handler prevents the page's scrolling: a touch that starts on the slider's track is the slider's
    # (PreventGestureRecognition, docs/controls/Slider). The page's first slider, its value said under it.
    Section 'slider: a drag in the page''s ScrollViewer moves the thumb, not the page' {
        Open 'Slider'
        # The slider's root, to aim along its track: its thumbs are what UI Automation lists as controls.
        $basic = RawById 'Basic'
        $b = $basic.Current.BoundingRectangle
        $state = ReadOut 'State'
        function Value { $v = After $state.Current.Name 'Value'; if ("$v" -match '^-?\d+$') { [int]$v } }
        function Top { $basic.Current.BoundingRectangle.Y }
        function Near($want) { $v = Value; ($null -ne $v) -and [math]::Abs($v - $want) -le 2 }
        function At($value) { SliderAt $b.X $b.Y $b.Width $script:scale $value }
        $top = Top
        Away
        Check 'the first slider starts at 60' (Near 60) ($state.Current.Name)

        $x60, $y = At 60
        $x80, $null = At 80
        TouchDrag $x60 $y ($x80 - $x60) 0
        Start-Sleep -Milliseconds 400
        Shot 'slider-touch-drag'
        Check 'a touch drag across moves the thumb with the finger, from 60 to 80' (Near 80) ($state.Current.Name)
        Check 'and leaves the page where it was' ((Top) -eq $top) "$(Top) against $top"

        # A touch on the line moves the closest thumb there (react-range's onTouchStartTrack).
        $x30, $null = At 30
        $x50, $null = At 50
        Tap $x30 $y
        Check 'a tap on the line moves the thumb there, to 30' (Near 30) ($state.Current.Name)
        TouchDrag $x30 $y ($x50 - $x30) (-(Px 150)) 20
        Start-Sleep -Milliseconds 1200
        Shot 'slider-touch-drag-up'
        Check 'a drag mostly up the page still moves the thumb across, from 30 to 50' (Near 50) ($state.Current.Name)
        Check 'and still leaves the page where it was' ((Top) -eq $top) "$(Top) against $top"

        $x20, $null = At 20
        MouseDrag $x50 $y ($x20 - $x50) 0
        Start-Sleep -Milliseconds 400
        Check 'a mouse drag moves it back, to 20' (Near 20) ($state.Current.Name)
        Away

        # Off the slider, on the heading above it, the same drag up pans the page, so the two above prove something.
        $hx, $hy = Centre (Named $CT::Text 'Basic usage')
        TouchDrag $hx $hy 0 (-(Px 150)) 20
        Start-Sleep -Milliseconds 1500
        Shot 'slider-page-panned'
        Check 'a touch drag up off the slider scrolls the page' ((Top) -lt ($top - (Px 60))) "$(Top) against $top"
        Check 'and leaves the value' (Near 20) ($state.Current.Name)
    }

    Section 'The window can be resized' {
        $transform = $win.GetCurrentPattern([Windows.Automation.TransformPattern]::Pattern)
        Check 'the window says it can be resized' ($transform.Current.CanResize)
        # The pattern sets the window's outer size, title bar and all, so only the width is checked.
        $r = $win.Current.BoundingRectangle
        $transform.Resize($r.Width + 300, $r.Height)
        Start-Sleep -Milliseconds 500
        Open 'Theme'
        $line = $script:ready
        "        $line"
        $w, $h = PageOf $line
        # 300 pixels wider, less what the frame takes, which is far less than 100.
        Check 'once wider, the page is laid out wider' ($w -gt $WindowWidth + 200 / (ScalingOf $line)) "$line"
        Shot 'theme-resized'
        $transform.Resize($r.Width, $r.Height)
        Start-Sleep -Milliseconds 300
    }

    # A mistyped -Only would otherwise pass with nothing run.
    Check 'a section ran' ($script:sections -gt 0) "none is like '$Only'"
    Check 'the app is still running' (-not $p.HasExited)
}
finally {
    LetGo
    [void][RealInput]::SetCursorPos($cursor.X, $cursor.Y)
    if (-not $p.HasExited) { [void]$p.CloseMainWindow(); Start-Sleep -Milliseconds 500 }
    if (-not $p.HasExited) { $p.Kill() }
}

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
