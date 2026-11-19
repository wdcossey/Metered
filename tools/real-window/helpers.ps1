# drive.ps1's pure helpers: no window, no input and no Win32, so selftest.ps1 checks them on any OS. drive.ps1 dot-sources
# this file. See README.md here. From Cornflower's tools/real-window/helpers.ps1 (wdcossey/CornflowerUI), which took them
# from Avonic's.

# The sample's window and the shell's bar, in view pixels: MainWindow.axaml's Width and Height, and SampleShell.axaml's Bar.
# selftest.ps1 reads both files and fails if these have drifted from them.
$WindowWidth = 390
$WindowHeight = 844
$BarHeight = 56

# A check's line, counted in the script that dot-sources this: drive.ps1 and selftest.ps1 print the same.
function Check($name, [bool]$ok, $detail = '') {
    $script:checks++
    if ($ok) { "  ok    $name" } else { $script:failures++; "  FAIL  $name $detail" }
}

# A section runs when its title is like -Only, as -like matches, or like one of its comma-separated parts: so a title with a
# comma in it is still selected by itself, which selftest.ps1 checks for each.
function Selected([string]$title, [string]$only) {
    ($title -like $only) -or [bool]@($only -split ',' | Where-Object { $_.Trim() -and $title -like $_.Trim() })
}

# The ready line's fields (samples/Metered.Sample/SampleShell.axaml.cs, Diagnostics/DeviceFacts.cs):
#   METERED ready open=<ms>ms scaling=... view=WxH raw=WxH safe=l,t,r,b page=WxH title=<title>
# The page's own size, which the shell's bar takes from; the window's client area is view=.
function PageOf($line) { if ($line -match ' page=([\d.]+)x([\d.]+)') { [double]$Matches[1], [double]$Matches[2] } }
function ScalingOf($line) { if ($line -match 'scaling=([\d.]+) ') { [double]$Matches[1] } else { 1 } }
# The page the line is for: last on the line, as a title can have spaces.
function TitleOf($line) { if ($line -match ' title=(.+)$') { $Matches[1].Trim() } }
# Windows gives a window whole pixels, so at 1.25 a 390 view is 487 pixels, 389.6: within a pixel is the size.
function WithinAPixel($view, $want, $scaling) { [math]::Abs($view - $want) * $scaling -le 1 }
# A title as a -like pattern that matches only itself.
function Literal($text) { [Management.Automation.WildcardPattern]::Escape($text) }

# Whether a point is inside a rectangle, its edges excluded, so that a point on the window's frame is outside it.
function Inside([double]$left, [double]$top, [double]$right, [double]$bottom, [double]$x, [double]$y) { $x -gt $left -and $x -lt $right -and $y -gt $top -and $y -lt $bottom }

# The lines of a text that are whole, with their line break: a writer may be partway through the last.
function WholeLines([string]$text) {
    $lines = @($text -split "`r?`n")
    # After the last line break comes an empty string, or a line not yet finished: either way, not a line. A loop, as
    # PowerShell's 0..-1 counts down rather than being empty.
    for ($i = 0; $i -lt $lines.Count - 1; $i++) { $lines[$i] }
}

# What a page's line says after its label: 'Selected: 3' is '3' (the Radio page), 'Clicked: 1; selected: 1, 2' is '1' for
# 'Clicked' and '1, 2' for 'selected' (the ButtonGroup page). Nothing if the label isn't there.
function After($text, $label) { if ("$text" -cmatch "(?:^|; )$([regex]::Escape($label)): ([^;]*)") { $Matches[1].Trim() } }

# Whether a region looks as it did, as how many of its sampled pixels differ: under 5 is the same, as a frame's
# anti-aliasing can move a pixel or two; over 20 is a change a check can stand on. Between is neither.
$Same = 5
$Moved = 20

# What a mouse that has left a control, then a touch tap on it, leave drawn, from three samples of the control's pixels
# against its look at rest: under the mouse (which has to differ, or the samples show nothing), and after the tap; and
# whether the cursor was on the control after it, as Windows can make a mouse move of a touch, which would hover it.
# Between the same and the moved is neither, as with any compare here.
function TapVerdict([int]$hovered, [int]$afterTap, [bool]$cursorOn) {
    if ($hovered -le $Moved) { 'unsettled' }
    elseif ($afterTap -lt $Same) { 'at rest' }
    elseif ($afterTap -le $Moved) { 'unsettled' }
    elseif ($cursorOn) { 'drawn, but the cursor was on it, so perhaps by the mouse' }
    else { 'left drawn' }
}
