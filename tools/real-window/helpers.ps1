# drive.ps1's pure helpers: no window, no input and no Win32, so selftest.ps1 checks them on any OS. drive.ps1 dot-sources
# this file. See README.md here. From Cornflower's tools/real-window/helpers.ps1 (wdcossey/CornflowerUI), which took them
# from Avonic's.

# The sample's window and the shell's bar, in view pixels: MainWindow.axaml's Width and Height, and SampleShell.axaml's Bar.
# selftest.ps1 reads both files and fails if these have drifted from them.
$WindowWidth = 390
$WindowHeight = 844
$BarHeight = 56

# A check's line, counted in the script that dot-sources this: drive.ps1 and selftest.ps1 print the same. A failure is also
# kept in $script:failed, with the section it was in ($script:section, drive.ps1's), for the summary to list again (#136).
function Check($name, [bool]$ok, $detail = '') {
    $script:checks++
    if ($ok) { return "  ok    $name" }
    $script:failures++
    if ($null -eq $script:failed) { $script:failed = New-Object 'Collections.Generic.List[string]' }
    $where = if ($script:section) { "$($script:section): " } else { '' }
    $script:failed.Add("$where$name $detail".TrimEnd())
    "  FAIL  $name $detail".TrimEnd()
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

# A drag's points after its start, evenly along it, in whole screen pixels. From Cornflower's.
function DragPath([double]$x, [double]$y, [double]$dx, [double]$dy, [int]$steps) {
    for ($i = 1; $i -le $steps; $i++) {
        [pscustomobject]@{ X = [int][math]::Round($x + $dx * $i / $steps); Y = [int][math]::Round($y + $dy * $i / $steps) }
    }
}

# Where a value is on a Slider's line, in screen pixels, from the slider's left, top and width on the screen: Base Web's
# line is inside its track's 16 view px padding (scale600), 2 high (slider.oracle.json's layout), and react-range puts a
# value at its share of the line's length.
$SliderPadding = 16
$SliderLine = 2
function SliderAt([double]$left, [double]$top, [double]$width, [double]$scale, [double]$value, [double]$minimum = 0, [double]$maximum = 100) {
    $line = $width / $scale - 2 * $SliderPadding
    $x = $left + ($SliderPadding + ($value - $minimum) / ($maximum - $minimum) * $line) * $scale
    [int][math]::Round($x), [int][math]::Round($top + ($SliderPadding + $SliderLine / 2) * $scale)
}

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

# Where a default-size Input's parts are, in screen pixels, from its box on the screen. The lengths are Base Web's, from
# the generated part themes, which selftest.ps1 reads: the root's 2 px border (getRootStyles); the root's padding beside a
# start enhancer (getRootPadding's scale550, 14); a default-size enhancer's own padding (getInputEnhancerPadding's
# scale300, 8); and the text's padding before it (getInputPadding's scale550, 14).
$InputBorder = 2
$InputRootPadding = 14
$EnhancerPadding = 8
$InputPadding = 14
# Where a start enhancer's content starts, half way down: inside the enhancer, whatever its text's width.
function EnhancerAt([double]$left, [double]$top, [double]$height, [double]$scale) {
    [int][math]::Round($left + ($InputBorder + $InputRootPadding + $EnhancerPadding) * $scale), [int][math]::Round($top + $height / 2)
}
# $in view px into an input's text, half way down, in an input with no start enhancer: on its first letter.
function TextAt([double]$left, [double]$top, [double]$height, [double]$scale, [double]$in = 2) {
    [int][math]::Round($left + ($InputBorder + $InputPadding + $in) * $scale), [int][math]::Round($top + $height / 2)
}

# A Textarea's resize grip (Textarea.axaml's Textarea.Resizer): a square of 16 view px in the end corner of the root's
# box, inside its 2 px border. Where its middle is, in screen pixels, left to right.
$GripSize = 16
function GripAt([double]$right, [double]$bottom, [double]$scale) {
    $in = ($InputBorder + $GripSize / 2) * $scale
    [int][math]::Round($right - $in), [int][math]::Round($bottom - $in)
}

# Whether a length grew by what was wanted, within a tolerance; a negative want is a shrink.
function Grew([double]$before, [double]$after, [double]$want, [double]$tolerance) { [math]::Abs(($after - $before) - $want) -le $tolerance }

# A text's lines: Avalonia's text box writes Environment.NewLine for Enter, which is CR LF on Windows.
function LinesOf([string]$text) { @($text -split "`r?`n") }

# Where $text was put into $before to make $after, as an index, or -1 if $after is no such thing: where a caret was.
function InsertedAt([string]$before, [string]$after, [string]$text) {
    for ($i = 0; $i -le $before.Length; $i++) { if ($after -ceq $before.Insert($i, $text)) { return $i } }
    -1
}

# Whether a box's top and bottom are both within an area's.
function Wholly([double]$top, [double]$bottom, [double]$areaTop, [double]$areaBottom) { $top -ge $areaTop -and $bottom -le $areaBottom }
# How far a page has to scroll to show a box wholly in its view, with a margin: negative up, positive down, 0 if it shows.
# A box that starts above the view is brought in by its top.
function ScrollBy([double]$top, [double]$bottom, [double]$areaTop, [double]$areaBottom, [double]$margin) {
    if ($top -lt $areaTop) { $top - $areaTop - $margin }
    elseif ($bottom -gt $areaBottom) { $bottom - $areaBottom + $margin }
    else { 0 }
}
# UI Automation's scroll pattern in percent: the one that scrolls $by pixels from $percent, for a view $viewport pixels high
# that is $viewSize percent of what it scrolls, so that what it can scroll is the rest. Clamped to 0 and 100; a view that
# shows everything can't scroll, and stays where it is.
# How many pixels a view can still scroll down, from its scroll pattern's percent and view size, for a view $viewport high.
function RoomLeft([double]$percent, [double]$viewSize, [double]$viewport) {
    if ($viewSize -ge 100 -or $viewSize -le 0 -or $percent -lt 0) { return 0 }
    (100 - $percent) / 100 * $viewport * (100 / $viewSize - 1)
}
function ScrollPercentFor([double]$percent, [double]$viewSize, [double]$viewport, [double]$by) {
    if ($viewSize -ge 100 -or $viewSize -le 0 -or $percent -lt 0) { return $percent }
    $range = $viewport * (100 / $viewSize - 1)
    [math]::Min(100, [math]::Max(0, $percent + $by / $range * 100))
}
