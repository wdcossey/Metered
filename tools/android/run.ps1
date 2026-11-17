#Requires -Version 7.4
<#
.SYNOPSIS
    The device run: builds the Android sample, installs it on the phone over ADB, opens each page asked for, waits
    for its ready line, and pulls back a frame and what the app and the device reported. -SelfTest checks the
    script's parsers on fixtures, with no adb and no device.

.DESCRIPTION
    For each page of -Page, by its catalogue title in any case: a cold start at that page (`am start -W -S` with the
    intent's `page` extra), the page's ready line, a frame of the screen, then -Reopen more opens in the running app:
    Back to the list, and the page again through a new intent, each with its own ready line. A Debug build writes
    each open's cost line after its ready line.

    Writes one folder per run (artifacts/device/<yyyyMMdd-HHmmss>-<model> by default, which git ignores), with a
    folder per page: frame.png, the phone's screen at its own resolution after the cold start; run.json, the device,
    the start Android timed, and each open's ready and cost lines; logcat.txt, the page's log. summary.json, beside
    the page folders, holds every page's run.json. Exits non-zero if any step fails, with logcat.txt kept.

    The phone must be the only device `adb devices` lists, with USB or wireless debugging on. A phone with a secure
    lock has to be unlocked by hand: the script wakes the screen and waits.

    PowerShell 7.4 or later, on Linux, macOS or Windows. adb is taken from ANDROID_HOME, then ANDROID_SDK_ROOT, then
    the SDK's default folder on the OS, then PATH; the build is given the SDK that adb is in. -NoProfile keeps a
    profile's settings out of the run. See tools/android/README.md.

.EXAMPLE
    pwsh -NoProfile -File tools/android/run.ps1 -Page Button,ButtonGroup,Theme
.EXAMPLE
    pwsh -NoProfile -File tools/android/run.ps1 -Configuration Debug -Page Button -Reopen 5
.EXAMPLE
    pwsh -NoProfile -File tools/android/run.ps1 -SelfTest
#>
[CmdletBinding()]
param(
    # Catalogue titles, in any case: one or more, comma-separated (pwsh -File passes `a,b` as one string). Needed
    # for a device run: the list writes no ready line, so a run names the pages it opens.
    [string[]]$Page,
    # How many times to open each page again, warm, in the running app after its cold start.
    [ValidateRange(0, 100)]
    [int]$Reopen = 0,
    [string]$Out,
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',
    [switch]$NoBuild,
    [int]$TimeoutSeconds = 60,
    [int]$UnlockTimeoutSeconds = 60,
    # How long the list is given to show after Back, before a reopen's intent.
    [int]$SettleMilliseconds = 1000,
    # Check the parsers on tools/android/fixtures and exit: no adb, no build, no device.
    [switch]$SelfTest
)

$ErrorActionPreference = 'Stop'
# A native command's exit code is what counts, checked after each call; a profile that turned this on would make a
# failing adb throw before the script could read its output.
$PSNativeCommandUseErrorActionPreference = $false
# Each argument reaches adb as it is, quotes and all; a profile that set Legacy would mangle a title's quotes.
$PSNativeCommandArgumentPassing = 'Standard'
$Repo = (Resolve-Path (Join-Path $PSScriptRoot '..' '..')).Path
$SampleDir = Join-Path $Repo 'samples' 'Metered.Sample.Android'
$Project = Join-Path $SampleDir 'Metered.Sample.Android.csproj'
$Package = 'io.github.wdcossey.metered.sample'
$Activity = "$Package/metered.sample.MainActivity"
# What the sample's shell writes for each page it opens (SampleShell.ReadyMarker), and in a Debug build after it,
# what the page cost to open (SampleShell.CostMarker). Its tests check these are the shell's.
$ReadyMarker = 'METERED ready'
$CostMarker = 'METERED cost'
# The intent extra the app reads a page's title from (App.PageExtra).
$PageExtra = 'page'

# ---------------------------------------------------------------------------------------------------------------------
# What the script reads: pure functions of text, which -SelfTest checks on fixtures.

# The titles -Page gives: comma-separated in each value, trimmed, none empty.
function Split-Pages([string[]]$values) {
    @($values | ForEach-Object { "$_" -split ',' } | ForEach-Object { $_.Trim() } | Where-Object { $_ })
}

# A word for the phone's shell, which adb joins its arguments into one command for: single-quoted, with a single
# quote inside written as '\''.
function ConvertTo-ShellWord([string]$text) { "'" + ($text -replace "'", "'\''") + "'" }

# A folder name for a page's title.
function Get-PageFolder([string]$title) {
    $name = ($title.ToLowerInvariant() -replace '[^a-z0-9]+', '-').Trim('-')
    if ($name) { $name } else { 'page' }
}

# The facts of one ready line, from its marker on:
#   METERED ready open=<ms>ms scaling=<s> view=<w>x<h> raw=<w>x<h> safe=<l>,<t>,<r>,<b> page=<w>x<h> title=<title>
# The title is last, as it can have spaces. Throws on a line that names no page.
function ConvertFrom-ReadyLine([string]$line) {
    $at = $line.IndexOf("$ReadyMarker ", [StringComparison]::Ordinal)
    if ($at -lt 0) { throw "Not a ready line: $line" }
    $facts = $line.Substring($at + $ReadyMarker.Length).Trim()
    $title = if ($facts -match '(?:^| )title=(.+)$') { $Matches[1].Trim() } else { $null }
    if (-not $title) { throw "The ready line names no page: $facts" }
    $openMs = if ($facts -match '(?:^| )open=(\d+)ms(?: |$)') { [int]$Matches[1] } else { $null }
    [pscustomobject]@{ Title = $title; OpenMs = $openMs; Ready = $facts; Cost = $null }
}

# Each page the app opened, in order, from a log: its ready line's facts, and the cost line written after it, if any
# (a Debug build's). A marker counts only as a word of its own, followed by a space. A cost line belongs to the ready
# line before it; one with none before it is left out.
function Read-Opens([string]$log) {
    $opens = [Collections.Generic.List[object]]::new()
    foreach ($line in $log -split "`r?`n") {
        if ($line.Contains("$ReadyMarker ", [StringComparison]::Ordinal)) {
            $opens.Add((ConvertFrom-ReadyLine $line))
        }
        elseif ($line.Contains("$CostMarker ", [StringComparison]::Ordinal) -and $opens.Count -gt 0) {
            $opens[-1].Cost = $line.Substring($line.IndexOf("$CostMarker ", [StringComparison]::Ordinal) + $CostMarker.Length).Trim()
        }
    }
    , $opens.ToArray()
}

# The app's crash in a log, if there is one, with the lines that say why: Java's report (FATAL EXCEPTION, then
# "Process: <package>, PID: ..."), or .NET's own, when the runtime aborts on an unhandled exception before Java
# reports it (UNHANDLED EXCEPTION). Another app's crash isn't the sample's.
function Find-CrashIn([string]$log) {
    $lines = @($log -split "`r?`n")
    $process = "Process: $([regex]::Escape($Package)),"
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $java = $lines[$i] -match 'FATAL EXCEPTION' -and $i + 1 -lt $lines.Count -and $lines[$i + 1] -match $process
        $dotnet = $lines[$i] -match 'UNHANDLED EXCEPTION'
        if ($java -or $dotnet) { return ($lines[$i..([Math]::Min($i + 8, $lines.Count - 1))] -join "`n").TrimEnd() }
    }
    return $null
}

# Whether open <count> of a page has come, from the opens read so far: the app has written that many ready lines, the
# last naming <title> (in any case, as the app matches titles), and when <needCost>, as in a Debug build, that line's
# cost line too. Throws if the app opened another page.
function Test-Opened([object[]]$opens, [int]$count, [string]$title, [bool]$needCost) {
    if ($opens.Count -lt $count) { return $false }
    $last = $opens[$count - 1]
    if ($last.Title -ne $title) { throw "Asked for the page '$title', but the app opened '$($last.Title)'." }
    return (-not $needCost) -or [bool]$last.Cost
}

# A page's run.json: <run>'s facts (the device, the APK, the configuration), the start Android timed, and the page's
# opens in order, the cold start's first: each one's time, its ready line's facts, and in Debug its cost line.
function New-PageRun([Collections.IDictionary]$run, $am, $totalTime, [object[]]$opens) {
    $page = [ordered]@{}
    foreach ($key in $run.Keys) { $page[$key] = $run[$key] }
    $page.launchState = $am.LaunchState
    $page.totalTimeMs = $totalTime
    $page.page = $opens[0].Title
    $page.openMs = $opens[0].OpenMs
    $page.opens = @($opens | ForEach-Object { [ordered]@{ openMs = $_.OpenMs; ready = $_.Ready; cost = $_.Cost } })
    $page
}

# What `am start` said: whether it started (or delivered to) the activity, and with -W, the start Android timed.
# am can exit 0 when it can't start the activity; it says so in its output.
function ConvertFrom-AmStart([string]$output) {
    $failed = $output -match '(?m)^Error' -or $output -match '(?m)^Status:\s*(?!ok\b)\S+'
    $totalTime = if ($output -match 'TotalTime:\s*(\d+)') { [int]$Matches[1] } else { $null }
    $launchState = if ($output -match 'LaunchState:\s*(\w+)') { $Matches[1] } else { $null }
    [pscustomobject]@{ Ok = -not $failed; TotalTimeMs = $totalTime; LaunchState = $launchState }
}

# ---------------------------------------------------------------------------------------------------------------------
# -SelfTest: every parser above on tools/android/fixtures, which hold logcat and am output in the phone's own format.
# The fixtures' ready and cost lines have the shape the sample's shell writes (tests/Metered.Tests/Sample/
# AndroidRunTests.cs checks them against it), so a parser that passes here reads the app.

function Invoke-SelfTest {
    $fixtures = Join-Path $PSScriptRoot 'fixtures'
    $failures = [Collections.Generic.List[string]]::new()
    $checks = 0
    function Fixture([string]$name) { Get-Content -Raw -LiteralPath (Join-Path $fixtures $name) }
    function Check([string]$what, $actual, $expected) {
        $script:checks++
        $a = ConvertTo-Json -InputObject $actual -Depth 5 -Compress
        $e = ConvertTo-Json -InputObject $expected -Depth 5 -Compress
        if ($a -ceq $e) { Write-Host "  ok    $what" }
        else { Write-Host "  FAIL  $what`n          expected $e`n          got      $a"; $failures.Add($what) }
    }
    function Throws([string]$what, [scriptblock]$block) {
        $script:checks++
        try { & $block | Out-Null; Write-Host "  FAIL  $what (didn't throw)"; $failures.Add($what) }
        catch { Write-Host "  ok    $what" }
    }
    $script:checks = 0

    Write-Host '-- Arguments'
    Check '-Page values are split on commas and trimmed' (Split-Pages @('Button, ButtonGroup', 'Theme', ' ', ',')) @('Button', 'ButtonGroup', 'Theme')
    Check 'a title is one word for the phone''s shell' (ConvertTo-ShellWord 'Button') "'Button'"
    Check 'a quote in a title survives the phone''s shell' (ConvertTo-ShellWord "it's a page") "'it'\''s a page'"
    Check 'a page''s folder' (Get-PageFolder 'ButtonGroup') 'buttongroup'
    Check 'a title with spaces and marks makes one folder name' (Get-PageFolder ' Slow page / 2 ') 'slow-page-2'
    Check 'a title with no letter still makes a folder' (Get-PageFolder '***') 'page'

    Write-Host '-- A cold start, Release: one ready line among the phone''s own'
    $opens = Read-Opens (Fixture 'logcat-release.txt')
    Check 'one open' $opens.Count 1
    Check 'its title' $opens[0].Title 'Button'
    Check 'its time to open' $opens[0].OpenMs 412
    Check 'its facts, from open= to the title' $opens[0].Ready 'open=412ms scaling=2.625 view=411.43x914.29 raw=1080x2400 safe=0,50.29,0,24 page=411.43x784 title=Button'
    Check 'no cost line in Release' $opens[0].Cost $null
    Check 'no crash' (Find-CrashIn (Fixture 'logcat-release.txt')) $null

    Write-Host '-- A Debug run with two reopens: three ready lines, each with its cost line'
    $opens = Read-Opens (Fixture 'logcat-debug-reopen.txt')
    Check 'three opens' $opens.Count 3
    Check 'every open is the page asked for' @($opens | ForEach-Object Title) @('Button', 'Button', 'Button')
    Check 'each time to open' @($opens | ForEach-Object OpenMs) @(1265, 148, 131)
    Check 'each cost line goes with the ready line before it' @($opens | ForEach-Object Cost) @(
        'Button: 1265 ms, 332 elements, 3 rules (3), layout 302.5, render 88.1',
        'Button: 148 ms, 332 elements, 3 rules (3), layout 41.7, render 12.9',
        'Button: 131 ms, 332 elements, 3 rules (3), layout 38.2, render 11.4')

    Write-Host '-- Waiting for an open'
    Check 'not yet, with fewer ready lines than opens' (Test-Opened $opens 4 'Button' $false) $false
    Check 'the second open has come' (Test-Opened $opens 2 'Button' $true) $true
    Check 'a title is matched in any case' (Test-Opened $opens 3 'button' $false) $true
    Throws 'another page than the one asked for is an error' { Test-Opened $opens 1 'ButtonGroup' $false }
    $uncosted = Read-Opens ((Fixture 'logcat-debug-reopen.txt').TrimEnd() -split "`n" | Select-Object -SkipLast 1 | Join-String -Separator "`n")
    Check 'in Debug, not until its cost line has come' (Test-Opened $uncosted 3 'Button' $true) $false
    Check 'in Release, the ready line is enough' (Test-Opened $uncosted 3 'Button' $false) $true

    Write-Host '-- run.json'
    $run = New-PageRun ([ordered]@{ model = 'Pixel 8'; configuration = 'Debug' }) (ConvertFrom-AmStart (Fixture 'am-start-cold.txt')) 1093 (Read-Opens (Fixture 'logcat-debug-reopen.txt'))
    $json = $run | ConvertTo-Json -Depth 4 | ConvertFrom-Json
    Check 'the run''s own facts first' @($json.model, $json.configuration) @('Pixel 8', 'Debug')
    Check 'the start Android timed' @($json.launchState, $json.totalTimeMs) @('COLD', 1093)
    Check 'the page and its cold open' @($json.page, $json.openMs) @('Button', 1265)
    Check 'every open, cold then warm' @($json.opens | ForEach-Object openMs) @(1265, 148, 131)
    Check 'each open''s cost line' $json.opens[2].cost 'Button: 131 ms, 332 elements, 3 rules (3), layout 38.2, render 11.4'
    Check 'each open''s ready line' $json.opens[1].ready 'open=148ms scaling=2.625 view=411.43x914.29 raw=1080x2400 safe=0,50.29,0,24 page=411.43x784 title=Button'

    Write-Host '-- A title with spaces, and lines that only look like the app''s'
    $opens = Read-Opens (Fixture 'logcat-spaces.txt')
    Check 'the title is the rest of the line' @($opens | ForEach-Object Title) @('slow page')
    Check 'a cost line before any ready line is left out' $opens[0].Cost 'slow page: 75 ms, 4 elements, 3 rules (3), layout 1.0, render 0.5'
    Throws 'a ready line with no title is an error' { ConvertFrom-ReadyLine '11-17 09:00:00.000  1  1 I DOTNET  : METERED ready open=5ms scaling=1' }
    Throws 'a marker inside a longer word is not a ready line' { ConvertFrom-ReadyLine 'METERED readying open=5ms title=x' }

    Write-Host '-- Crashes'
    Check 'Java''s report for the sample, with why' (Find-CrashIn (Fixture 'crash-java.txt')) ((@(
        '11-17 09:14:02.118  9311  9311 E AndroidRuntime: FATAL EXCEPTION: main',
        '11-17 09:14:02.118  9311  9311 E AndroidRuntime: Process: io.github.wdcossey.metered.sample, PID: 9311',
        '11-17 09:14:02.118  9311  9311 E AndroidRuntime: android.runtime.JavaProxyThrowable: [System.ArgumentException]: The sample has no page titled ''nowhere''. Its pages are: ''Button'', ''ButtonGroup'', ''Theme''. (Parameter ''title'')',
        '11-17 09:14:02.118  9311  9311 E AndroidRuntime: 	at Metered.Sample.SampleShell.Open(String title)',
        '11-17 09:14:02.118  9311  9311 E AndroidRuntime: 	at Metered.Sample.App.PhoneView(IReadOnlyList`1 pages, String startPage)'
    )) -join "`n")
    Check '.NET''s own report' ((Find-CrashIn (Fixture 'crash-dotnet.txt')) -split "`n" | Select-Object -First 2) @(
        '11-17 09:20:44.902 10022 10022 E DOTNET  : UNHANDLED EXCEPTION:',
        '11-17 09:20:44.902 10022 10022 E DOTNET  : System.ArgumentException: The sample has no page titled ''nowhere''. Its pages are: ''Button'', ''ButtonGroup'', ''Theme''. (Parameter ''title'')')
    Check 'another app''s crash is not the sample''s' (Find-CrashIn (Fixture 'crash-other-app.txt')) $null

    Write-Host '-- am start'
    $am = ConvertFrom-AmStart (Fixture 'am-start-cold.txt')
    Check 'a cold start: ok, COLD, its TotalTime' @($am.Ok, $am.LaunchState, $am.TotalTimeMs) @($true, 'COLD', 1093)
    $am = ConvertFrom-AmStart (Fixture 'am-start-delivered.txt')
    Check 'an intent delivered to the running activity is ok, untimed' @($am.Ok, $am.LaunchState, $am.TotalTimeMs) @($true, $null, $null)
    $am = ConvertFrom-AmStart (Fixture 'am-start-error.txt')
    Check 'an activity am can''t start is a failure, though am exits 0' $am.Ok $false
    $am = ConvertFrom-AmStart (Fixture 'am-start-timeout.txt')
    Check 'a status other than ok is a failure' $am.Ok $false

    Write-Host ''
    if ($failures.Count) {
        Write-Host "$($failures.Count) of $script:checks checks failed."
        exit 1
    }
    Write-Host "All $script:checks checks passed."
    exit 0
}

if ($SelfTest) { Invoke-SelfTest }

# ---------------------------------------------------------------------------------------------------------------------
# The device run.

$Pages = Split-Pages $Page
if (-not $Pages) {
    throw "Name the pages to open, by their catalogue titles: -Page Button,ButtonGroup,Theme. The list writes no ready line to wait for."
}

# Where Android Studio puts the SDK on this OS.
function Get-DefaultSdk {
    if ($IsWindows) { if ($env:LOCALAPPDATA) { Join-Path $env:LOCALAPPDATA 'Android' 'Sdk' } }
    elseif ($IsMacOS) { Join-Path $HOME 'Library' 'Android' 'sdk' }
    else { Join-Path $HOME 'Android' 'Sdk' }
}

# adb in an SDK folder: Get-Command finds adb.exe on Windows and adb elsewhere by the same name.
function Find-AdbIn([string]$sdk) {
    if (-not $sdk) { return $null }
    Get-Command (Join-Path $sdk 'platform-tools' 'adb') -CommandType Application -ErrorAction SilentlyContinue |
        Select-Object -First 1
}

# adb from ANDROID_HOME, ANDROID_SDK_ROOT, the default SDK folder, then PATH. The order matters: a different adb
# restarts the adb server, which can drop the phone's wireless pairing.
function Find-Adb {
    $found = (Find-AdbIn $env:ANDROID_HOME) ?? (Find-AdbIn $env:ANDROID_SDK_ROOT) ?? (Find-AdbIn (Get-DefaultSdk)) ??
        (Get-Command adb -CommandType Application -ErrorAction SilentlyContinue | Select-Object -First 1)
    if ($found) { return $found.Source }
    throw "adb not found. Set ANDROID_HOME to the Android SDK, or put its platform-tools on PATH."
}

$Adb = Find-Adb
# The SDK adb is in, for the build, so that one SDK is used for both: adb's folder's parent, if adb is in an SDK's
# platform-tools (an SDK has platforms or build-tools beside it). Otherwise, such as a packaged adb on PATH, the build
# finds its SDK itself.
$AdbDir = Split-Path -Parent $Adb
$AdbSdk = if ((Split-Path -Leaf $AdbDir) -eq 'platform-tools') { Split-Path -Parent $AdbDir }
if ($AdbSdk -and -not ((Test-Path (Join-Path $AdbSdk 'platforms')) -or (Test-Path (Join-Path $AdbSdk 'build-tools')))) { $AdbSdk = $null }

# Runs adb and returns its output, stderr included, as one string; throws if adb fails. The exit code alone
# decides: adb writes some status to stderr.
function Adb {
    $output = & $Adb @args 2>&1 | ForEach-Object { "$_" } | Out-String
    if ($LASTEXITCODE -ne 0) { throw "adb $($args -join ' ') failed ($LASTEXITCODE): $output" }
    return $output
}

function Step([string]$text) { Write-Host "-- $text" }

# The text as it is, in UTF-8 without a BOM, which JSON readers reject.
function Write-Utf8([string]$path, [string]$text) {
    Set-Content -LiteralPath $path -Value $text -Encoding utf8NoBOM -NoNewline
}

# The app's crash since the log was cleared, if any. Only errors, from the main and crash buffers, so that a poll
# doesn't read the whole log. ('*:E' is left unquoted for the phone's shell, where it matches no file.)
function Find-Crash { Find-CrashIn (Adb logcat -d -b main -b crash '*:E') }

# Waits until the app has written <count> ready lines since the log was cleared, the last for <title>, and in a
# Debug build its cost line too; stops at once on a crash. Returns every open so far.
function Wait-Opens([int]$count, [string]$title) {
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ($true) {
        $opens = Read-Opens (Adb logcat -d -s DOTNET:I)
        if (Test-Opened $opens $count $title ($Configuration -eq 'Debug')) { return $opens }
        $crash = Find-Crash
        if ($crash) { throw "The app stopped before its ready line:`n$crash" }
        if ((Get-Date) -gt $deadline) {
            throw "No '$ReadyMarker' line$(if ($Configuration -eq 'Debug') { " and '$CostMarker' line" }) for open $count of '$title' within $TimeoutSeconds s."
        }
        Start-Sleep -Milliseconds 500
    }
}

# Any failure from here on keeps the phone's log next to the page's run, if its folder exists yet.
$PageOut = $null
trap {
    if ($PageOut -and (Test-Path $PageOut)) {
        try { Write-Utf8 (Join-Path $PageOut 'logcat.txt') (Adb logcat -d) } catch { }
        Write-Host "   Log kept in $(Join-Path $PageOut 'logcat.txt')"
    }
    break
}

# 1. One phone.
$devices = @((Adb devices) -split "`r?`n" | Where-Object { $_ -match "^\S+\s+device$" })
if ($devices.Count -ne 1) {
    throw "Expected one device in 'adb devices', found $($devices.Count). Is the phone connected (USB switch) or paired for wireless debugging?"
}
$model = (Adb shell getprop ro.product.model).Trim()
$release = (Adb shell getprop ro.build.version.release).Trim()
$sdk = (Adb shell getprop ro.build.version.sdk).Trim()
$wmSize = (Adb shell wm size).Trim()
$wmDensity = (Adb shell wm density).Trim()
Step "$model, Android $release (API $sdk); $wmSize; $wmDensity"

if (-not $Out) {
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
    $Out = Join-Path $Repo 'artifacts' 'device' ("$stamp-" + ($model -replace '[^A-Za-z0-9]+', '-'))
}
New-Item -ItemType Directory -Force -Path $Out | Out-Null
$Out = (Resolve-Path $Out).Path

# 2. Build.
if (-not $NoBuild) {
    $sdkArg = @(if ($AdbSdk) { "-p:AndroidSdkDirectory=$AdbSdk" })
    Step "Building the sample ($Configuration)$(if ($AdbSdk) { " with the SDK at $AdbSdk" } else { ", adb not in an SDK: the build finds its own" })"
    & dotnet build $Project -c $Configuration -nologo -v q @sdkArg
    if ($LASTEXITCODE -ne 0) { throw "dotnet build failed ($LASTEXITCODE)." }
}
$bin = Join-Path $SampleDir 'bin' $Configuration
$apk = Get-ChildItem $bin -Recurse -Filter "$Package-Signed.apk" -ErrorAction SilentlyContinue |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
if (-not $apk) { throw "No $Package-Signed.apk under $bin. Build first, or drop -NoBuild." }

# 3. Install.
Step "Installing $($apk.Name)"
Adb install -r $apk.FullName | Out-Null

# 4. Awake and unlocked. A secure lock needs a hand; wait for it.
Adb shell input keyevent KEYCODE_WAKEUP | Out-Null
Adb shell wm dismiss-keyguard | Out-Null
$deadline = (Get-Date).AddSeconds($UnlockTimeoutSeconds)
$asked = $false
while ((Adb shell dumpsys window) -match 'isKeyguardShowing=true') {
    if ((Get-Date) -gt $deadline) { throw "The phone is still locked after $UnlockTimeoutSeconds s." }
    if (-not $asked) { Write-Host "   Unlock the phone; waiting up to $UnlockTimeoutSeconds s."; $asked = $true }
    Start-Sleep -Seconds 1
}
# A screen still waking from doze can bring the last task back on its own, under the launch.
while ((Adb shell dumpsys power) -notmatch 'mWakefulness=Awake') {
    if ((Get-Date) -gt $deadline) { throw "The screen is still not awake after $UnlockTimeoutSeconds s." }
    Start-Sleep -Seconds 1
}

$runs = [Collections.Generic.List[object]]::new()
foreach ($title in $Pages) {
    $PageOut = Join-Path $Out (Get-PageFolder $title)
    New-Item -ItemType Directory -Force -Path $PageOut | Out-Null
    $extra = @('--es', $PageExtra, (ConvertTo-ShellWord $title))

    # 5. Start it cold at the page, from the home screen, and let Android time it. -S stops the app first; going home
    #    first means no task of the app's is on top for the system to resume instead.
    Step "Starting the sample at '$title'"
    Adb shell input keyevent KEYCODE_HOME | Out-Null
    Adb shell am force-stop $Package | Out-Null
    Start-Sleep -Seconds 1
    Adb logcat -c | Out-Null
    # A failure here is read, not thrown: a start that crashes the app (a title the catalogue doesn't have, say) has
    # to be reported with the app's own message, which is in its crash report, not in am's output.
    try { $start = Adb shell am start -W -S -n $Activity @extra } catch { $start = "Error: $($_.Exception.Message)" }
    $am = ConvertFrom-AmStart $start
    if (-not $am.Ok -or $start -notmatch 'Status:\s*ok') {
        # The crash report can come a moment after am gives up.
        for ($try = 0; $try -lt 6; $try++) {
            $crash = Find-Crash
            if ($crash) { throw "The app stopped at start:`n$crash" }
            Start-Sleep -Milliseconds 500
        }
        throw "am start failed:`n$start"
    }
    $totalTime = $am.TotalTimeMs
    if ($am.LaunchState -ne 'COLD' -or -not $totalTime) {
        # Not a cold start Android timed (the activity was already on top, say): no time to record.
        Write-Warning "am start -W did not time a cold start (LaunchState $($am.LaunchState), TotalTime $totalTime):`n$start"
        $totalTime = $null
    }

    # 6. Wait for the page to say it has drawn: the shell writes its ready line once the frame after the page's first
    #    is laid out, so a frame taken after it is the page at rest.
    $opens = Wait-Opens 1 $title
    Step "Ready: $($opens[0].Ready)"
    if ($opens[0].Cost) { Step "Cost: $($opens[0].Cost)" }

    # 7. The frame, straight from screencap: PowerShell 7.4 redirects a native command's output to a file byte for
    #    byte. exec-out, not shell, so that no terminal on the phone's side turns its line feeds into CR LF. exec-out
    #    gives back neither screencap's exit code nor its stderr apart (an error comes down the same stream), so the
    #    file has to start with PNG's signature.
    Start-Sleep -Milliseconds 500
    $frame = Join-Path $PageOut 'frame.png'
    & $Adb exec-out screencap -p > $frame
    if ($LASTEXITCODE -ne 0) { throw "adb exec-out screencap -p failed ($LASTEXITCODE)." }
    $head = [byte[]]::new(256)
    $stream = [IO.File]::OpenRead($frame)
    try { $read = $stream.Read($head, 0, $head.Length) } finally { $stream.Dispose() }
    if ($read -lt 8 -or [Convert]::ToHexString($head, 0, 8) -ne '89504E470D0A1A0A') {
        $text = [Text.Encoding]::UTF8.GetString($head, 0, $read).Trim()
        throw "adb exec-out screencap -p wrote no PNG ($((Get-Item -LiteralPath $frame).Length) bytes): $text"
    }

    # 8. Warm opens: Back to the list, as a user goes, then the page again through an intent to the running activity
    #    (single-top: MainActivity.OnNewIntent opens it in the shell). Not timed by Android; the shell times each.
    for ($i = 1; $i -le $Reopen; $i++) {
        Adb shell input keyevent KEYCODE_BACK | Out-Null
        Start-Sleep -Milliseconds $SettleMilliseconds
        try { $again = Adb shell am start -n $Activity @extra } catch { $again = "Error: $($_.Exception.Message)" }
        if (-not (ConvertFrom-AmStart $again).Ok) { throw "am start failed on reopen $i of '$title':`n$again" }
        $opens = Wait-Opens ($i + 1) $title
        Step "Reopen $($i): $($opens[$i].Ready)"
        if ($opens[$i].Cost) { Step "Cost: $($opens[$i].Cost)" }
    }

    $log = Adb logcat -d
    Write-Utf8 (Join-Path $PageOut 'logcat.txt') $log
    # Read-Opens returns its array as one object: assigned as it is, never wrapped in @(), which would nest it.
    $opens = Read-Opens $log
    $facts = [ordered]@{
        when          = (Get-Date).ToString('o')
        model         = $model
        android       = $release
        sdk           = [int]$sdk
        wmSize        = $wmSize
        wmDensity     = $wmDensity
        configuration = $Configuration
        apk           = $apk.Name
        apkBytes      = $apk.Length
    }
    $run = New-PageRun $facts $am $totalTime $opens
    Write-Utf8 (Join-Path $PageOut 'run.json') ($run | ConvertTo-Json -Depth 4)
    $runs.Add($run)
    Step "'$title': start $($am.LaunchState), $totalTime ms (am start -W TotalTime); opened in $(($opens | ForEach-Object OpenMs) -join ', ') ms (the shell's time, cold then warm)"
}
$PageOut = $null

Write-Utf8 (Join-Path $Out 'summary.json') (ConvertTo-Json -InputObject $runs.ToArray() -Depth 5)
Step "Wrote $Out"
