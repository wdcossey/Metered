# Checks drive.ps1 without running it: its pure helpers (helpers.ps1) on known answers, the sizes it expects against the
# sample's XAML, and the script itself as parsed: that it parses, that each section's title selects it alone under -Only,
# and that every page it opens and every control it finds by name is in the sample. Takes no input and opens no window, so
# it runs on any OS, as CI's generated job runs it:
#   pwsh -NoProfile -File tools/real-window/selftest.ps1
# Exits 1 if a check fails. From Cornflower's tools/real-window/selftest.ps1.
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'helpers.ps1')

$script:failures = 0
$script:checks = 0

'== the helpers'
$line = 'METERED ready open=212ms scaling=1.25 view=390x844 raw=488x1055 safe=0,0,0,0 page=389.6x788 title=a page, with spaces'
$w, $h = PageOf $line
Check 'PageOf reads page=, not view=' (($w -eq 389.6) -and ($h -eq 788)) "$w x $h"
Check 'ScalingOf reads scaling=' ((ScalingOf $line) -eq 1.25) (ScalingOf $line)
Check 'ScalingOf is 1 without it' ((ScalingOf 'METERED ready open=1ms') -eq 1)
Check 'TitleOf reads the title to the end, spaces and all' ((TitleOf $line) -ceq 'a page, with spaces') (TitleOf $line)
Check 'WithinAPixel takes 389.6 for 390 at 1.25' (WithinAPixel 389.6 390 1.25)
Check 'but not 388.8 (a pixel and a half)' (-not (WithinAPixel 388.8 390 1.25))
Check 'Literal escapes a pattern' (('a*b' -like (Literal 'a*b')) -and -not ('axb' -like (Literal 'a*b')))
# The Android head's line, from tools/android/fixtures: the same fields, so one reader serves both.
$phone = 'METERED ready open=1265ms scaling=2.625 view=411.43x914.29 raw=1080x2400 safe=0,50.29,0,24 page=411.43x784 title=Button'
$pw, $ph = PageOf $phone
Check 'the phone''s line reads the same' (($pw -eq 411.43) -and ($ph -eq 784) -and ((ScalingOf $phone) -eq 2.625) -and ((TitleOf $phone) -ceq 'Button'))

Check 'Selected takes * for every title' (Selected 'switch: anything' '*')
Check 'Selected takes one of several patterns' (Selected 'switch: a tap' 'radio*, switch*')
Check 'Selected leaves a title no pattern is like' (-not (Selected 'switch: a tap' 'radio*,checkbox*'))
Check 'Selected takes a title with a comma, by itself' (Selected 'one, two' 'one, two')
Check 'Selected ignores an empty part' (-not (Selected 'x' 'a,,b'))

Check 'Inside is inside' (Inside 0 0 10 10 5 5)
Check 'Inside leaves out the edges' (-not (Inside 0 0 10 10 0 5) -and -not (Inside 0 0 10 10 5 10))
Check 'Inside leaves out a point beyond them' (-not (Inside 0 0 10 10 11 5))

$whole = @(WholeLines "one`r`ntwo`nthree, still being writ")
Check 'WholeLines leaves out a last line with no line break yet' (($whole.Count -eq 2) -and ($whole[0] -ceq 'one') -and ($whole[1] -ceq 'two')) ($whole -join ' | ')
Check 'WholeLines keeps every line that ends in one' (@(WholeLines "a`nb`n").Count -eq 2)
Check 'WholeLines is nothing for a text with no line break, or none' ((@(WholeLines 'partial').Count -eq 0) -and (@(WholeLines '').Count -eq 0))

Check 'After reads a line''s one value' ((After 'Selected: 3' 'Selected') -ceq '3') (After 'Selected: 3' 'Selected')
$group = 'Clicked: 1; selected: 1, 2'
Check 'After reads each of two, by its label, case and all' (((After $group 'Clicked') -ceq '1') -and ((After $group 'selected') -ceq '1, 2')) "$(After $group 'Clicked') / $(After $group 'selected')"
Check 'After is nothing for a label not there' (($null -eq (After $group 'Selected')) -and ($null -eq (After 'Clicked: none' 'selected')))
Check 'After reads an empty selection as empty' ((After 'Clicked: 0; selected: ' 'selected') -ceq '') "'$(After 'Clicked: 0; selected: ' 'selected')'"

$path = @(DragPath 10 20 30 -60 3)
Check 'DragPath ends where the drag does, in even steps' (($path.Count -eq 3) -and ($path[0].X -eq 20) -and ($path[0].Y -eq 0) -and ($path[2].X -eq 40) -and ($path[2].Y -eq -40)) (($path | ForEach-Object { "$($_.X),$($_.Y)" }) -join ' ')
$sx, $sy = SliderAt 100 200 350 1 50
Check 'SliderAt puts 50 in the middle of the line, on it' (($sx -eq 275) -and ($sy -eq 217)) "$sx, $sy"
$sx, $sy = SliderAt 100 200 700 2 0
Check 'SliderAt puts the minimum at the line''s start, at a scaling of 2' (($sx -eq 132) -and ($sy -eq 234)) "$sx, $sy"
$sx, $null = SliderAt 0 0 350 1 300 -300 300
Check 'SliderAt reads a custom minimum and maximum' ($sx -eq 334) "$sx"
Check 'TapVerdict: a look back at its rest is at rest' (((TapVerdict 100 0 $false) -eq 'at rest') -and ((TapVerdict 100 ($Same - 1) $true) -eq 'at rest'))
Check 'TapVerdict: still drawn, the cursor off it, is left drawn' ((TapVerdict 100 50 $false) -eq 'left drawn')
Check 'TapVerdict: between the same and the moved settles nothing' (((TapVerdict 100 $Same $false) -eq 'unsettled') -and ((TapVerdict 100 $Moved $false) -eq 'unsettled'))
Check 'TapVerdict: still drawn with the cursor on it is not settled' ((TapVerdict 100 50 $true) -like 'drawn, but*')
Check 'TapVerdict: a hover the samples don''t show settles nothing' ((TapVerdict $Moved 50 $false) -eq 'unsettled')
Check 'the same is under the moved' ($Same -lt $Moved)

$ex, $ey = EnhancerAt 100 200 48 1
Check 'EnhancerAt is past the border, the root''s padding and the enhancer''s own, half way down' (($ex -eq 124) -and ($ey -eq 224)) "$ex, $ey"
$ex, $ey = EnhancerAt 100 200 96 2
Check 'EnhancerAt scales its lengths, not the box' (($ex -eq 148) -and ($ey -eq 248)) "$ex, $ey"
$tx, $ty = TextAt 100 200 48 1
Check 'TextAt is 2 view px into the text, past the border and its padding' (($tx -eq 118) -and ($ty -eq 224)) "$tx, $ty"
$tx, $null = TextAt 0 0 48 1.25 0
Check 'TextAt scales, and takes how far in' ($tx -eq 20) "$tx"
$gx, $gy = GripAt 300 400 1
Check 'GripAt is the grip''s middle, inside the border, in the end corner' (($gx -eq 290) -and ($gy -eq 390)) "$gx, $gy"
$gx, $gy = GripAt 300 400 2
Check 'GripAt scales' (($gx -eq 280) -and ($gy -eq 380)) "$gx, $gy"
Check 'Grew takes a growth within its tolerance' ((Grew 100 141 40 2) -and (Grew 100 70 -30 2))
Check 'Grew leaves one beyond it, or the wrong way' (-not (Grew 100 137 40 2) -and -not (Grew 100 130 -30 2))
$lines = @(LinesOf "one`r`ntwo")
Check 'LinesOf splits at CR LF' (($lines.Count -eq 2) -and ($lines[0] -ceq 'one') -and ($lines[1] -ceq 'two')) ($lines -join ' | ')
Check 'LinesOf splits at LF, and keeps an empty text one line' ((@(LinesOf "a`nb").Count -eq 2) -and (@(LinesOf '').Count -eq 1))
Check 'InsertedAt reads where a letter went in' (((InsertedAt 'Clear me!Z' 'YClear me!Z' 'Y') -eq 0) -and ((InsertedAt 'Clear me!Z' 'CYlear me!Z' 'Y') -eq 1) -and ((InsertedAt 'Clear me!Z' 'Clear me!ZY' 'Y') -eq 10))
Check 'InsertedAt is -1 when the rest has changed' (((InsertedAt 'Clear me!Z' 'Ylear me!Z' 'Y') -eq -1) -and ((InsertedAt 'Clear me!Z' 'Clear me!Z' 'Y') -eq -1))
Check 'Wholly takes a box within the area, edges and all' ((Wholly 10 20 0 100) -and (Wholly 0 100 0 100))
Check 'Wholly leaves one over either edge' (-not (Wholly -1 20 0 100) -and -not (Wholly 10 101 0 100))
Check 'ScrollBy is nothing for a box that shows' ((ScrollBy 10 20 0 100 4) -eq 0)
Check 'ScrollBy scrolls down to a box below, with the margin' ((ScrollBy 90 120 0 100 4) -eq 24) (ScrollBy 90 120 0 100 4)
Check 'ScrollBy scrolls up to a box above, by its top' ((ScrollBy -30 200 0 100 4) -eq -34) (ScrollBy -30 200 0 100 4)
Check 'ScrollPercentFor: a view half of what it scrolls, 400 high, scrolls 400; 100 of it is 25 %' ((ScrollPercentFor 0 50 400 100) -eq 25) (ScrollPercentFor 0 50 400 100)
Check 'ScrollPercentFor goes back, too' ((ScrollPercentFor 50 50 400 -100) -eq 25) (ScrollPercentFor 50 50 400 -100)
Check 'ScrollPercentFor stops at either end' (((ScrollPercentFor 90 50 400 100) -eq 100) -and ((ScrollPercentFor 10 50 400 -100) -eq 0))
Check 'ScrollPercentFor leaves a view that can''t scroll' (((ScrollPercentFor 0 100 400 100) -eq 0) -and ((ScrollPercentFor -1 50 400 100) -eq -1))

'== the sizes it expects, against the sample'
$repo = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')).Path
$sample = Join-Path $repo 'samples'
[xml]$window = Get-Content -Raw (Join-Path (Join-Path $sample 'Metered.Sample') 'MainWindow.axaml')
Check "the window is $WindowWidth x $WindowHeight, as MainWindow.axaml says" (([double]$window.Window.Width -eq $WindowWidth) -and ([double]$window.Window.Height -eq $WindowHeight)) "$($window.Window.Width) x $($window.Window.Height)"
[xml]$shell = Get-Content -Raw (Join-Path (Join-Path $sample 'Metered.Sample') 'SampleShell.axaml')
$bar = @($shell.SelectNodes('//*') | Where-Object { $_.GetAttribute('Name', 'http://schemas.microsoft.com/winfx/2006/xaml') -ceq 'Bar' })
Check "the bar is $BarHeight high, as SampleShell.axaml says" (($bar.Count -eq 1) -and ([double]$bar[0].Height -eq $BarHeight)) "$($bar | ForEach-Object { $_.Height })"

'== the lengths it aims by, against the themes'
$themes = Join-Path (Join-Path (Join-Path $repo 'src') 'Metered.Themes') 'Controls'
# A setter's value in a theme file: the ControlTheme by its x:Key, its own setter or a style's in it by the selector.
function SetterOf($file, $key, $selector, $property) {
    [xml]$doc = Get-Content -Raw (Join-Path $themes $file)
    $ns = New-Object Xml.XmlNamespaceManager $doc.NameTable
    $ns.AddNamespace('a', 'https://github.com/avaloniaui')
    $ns.AddNamespace('x', 'http://schemas.microsoft.com/winfx/2006/xaml')
    $owner = $doc.SelectSingleNode("//a:ControlTheme[@x:Key='$key']", $ns)
    if ($owner -and $selector) { $owner = $owner.SelectSingleNode("a:Style[@Selector='$selector']", $ns) }
    if ($owner) { $setter = $owner.SelectSingleNode("a:Setter[@Property='$property']", $ns); if ($setter) { $setter.GetAttribute('Value') } }
}
# A thickness's left, as Avalonia reads '2' or '14,0,0,0'.
function LeftOf($thickness) { if ("$thickness" -match '^\s*([\d.]+)') { [double]$Matches[1] } }
$v = SetterOf 'Input.Parts.g.axaml' 'Input.StyledRoot' $null 'BorderThickness'
Check "an input's border is $InputBorder, as Input.StyledRoot's" ((LeftOf $v) -eq $InputBorder) "$v"
$v = SetterOf 'Input.Parts.g.axaml' 'Input.StyledRoot' '^:adjoined-left' 'Padding'
Check "its root's padding beside a start enhancer is $InputRootPadding" ((LeftOf $v) -eq $InputRootPadding) "$v"
$plain = SetterOf 'Input.Parts.g.axaml' 'Input.StyledRoot' $null 'Padding'
$trailing = SetterOf 'Input.Parts.g.axaml' 'Input.StyledRoot' '^:has-icon-trailing' 'Padding'
Check 'and none before the text with no start enhancer, a trailing icon or not, as TextAt takes it' (((LeftOf $plain) -eq 0) -and ((LeftOf $trailing) -eq 0)) "$plain / $trailing"
$v = SetterOf 'Input.Parts.g.axaml' 'Input.StyledStartEnhancer' $null 'Padding'
Check "a default-size start enhancer's padding is $EnhancerPadding" ((LeftOf $v) -eq $EnhancerPadding) "$v"
$v = SetterOf 'Input.Parts.g.axaml' 'Input.StyledInput' $null 'Padding'
Check "a default-size input's text is $InputPadding in" ((LeftOf $v) -eq $InputPadding) "$v"
$v = SetterOf 'Textarea.Parts.g.axaml' 'Textarea.StyledRoot' $null 'BorderThickness'
Check "a textarea's border is $InputBorder too, as Textarea.StyledRoot's" ((LeftOf $v) -eq $InputBorder) "$v"
$gw = SetterOf 'Textarea.axaml' 'Textarea.Resizer' $null 'Width'
$gh = SetterOf 'Textarea.axaml' 'Textarea.Resizer' $null 'Height'
Check "the grip is $GripSize square, as Textarea.Resizer's" (([double]$gw -eq $GripSize) -and ([double]$gh -eq $GripSize)) "$gw x $gh"
$ga = "$(SetterOf 'Textarea.axaml' 'Textarea.Resizer' $null 'HorizontalAlignment') $(SetterOf 'Textarea.axaml' 'Textarea.Resizer' $null 'VerticalAlignment')"
Check 'in the bottom right corner' ($ga -ceq 'Right Bottom') $ga

'== drive.ps1, as parsed'
$drive = Join-Path $PSScriptRoot 'drive.ps1'
$errors = $null
$ast = [System.Management.Automation.Language.Parser]::ParseFile($drive, [ref]$null, [ref]$errors)
Check 'it parses' ($errors.Count -eq 0) (($errors | ForEach-Object { "line $($_.Extent.StartLineNumber): $($_.Message)" }) -join '; ')

# The first argument of each call to a command, where it is a string.
function ArgumentsOf($name) {
    $ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq $name }, $true) |
        ForEach-Object { $_.CommandElements[1] } |
        Where-Object { $_ -is [System.Management.Automation.Language.StringConstantExpressionAst] } |
        ForEach-Object { $_.Value }
}
# A section's title may be in double quotes, for an apostrophe: expandable, but with nothing in it to expand.
function TitlesOf($name) {
    $ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq $name }, $true) |
        ForEach-Object { $_.CommandElements[1] } |
        Where-Object { $_ -is [System.Management.Automation.Language.StringConstantExpressionAst] -or
            ($_ -is [System.Management.Automation.Language.ExpandableStringExpressionAst] -and $_.NestedExpressions.Count -eq 0) } |
        ForEach-Object { $_.Value }
}
$sections = @(TitlesOf 'Section')
$all = @($ast.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq 'Section' }, $true))
Check 'it has sections' ($sections.Count -gt 0)
Check 'every section''s title is a plain string' ($sections.Count -eq $all.Count) "$($all.Count - $sections.Count) not"
$twice = @($sections | Group-Object -CaseSensitive | Where-Object Count -gt 1 | ForEach-Object Name)
Check 'no two sections share a title' ($twice.Count -eq 0) ($twice -join ', ')
foreach ($title in $sections) {
    $also = @($sections | Where-Object { $_ -cne $title -and (Selected $_ $title) })
    Check "-Only '$title' runs that section alone" ((Selected $title $title) -and $also.Count -eq 0) ($also -join ', ')
}

# The sample's pages, by their [SamplePage] titles, and its controls' x:Names.
# Not bin/ or obj/, where a stale copy could keep a name the source has lost; matched on the path below samples/, so a
# clone that sits under a folder called bin or obj is still read.
function Sources($filter) { Get-ChildItem $sample -Recurse -Filter $filter | Where-Object { $_.FullName.Substring($sample.Length) -notmatch '[\\/](bin|obj)[\\/]' } }
# Each page's title, from its [SamplePage], and the XAML beside its code. Keyed case and all, as drive.ps1 matches a title
# (Named): PowerShell's own @{} would take 'TextArea' for 'Textarea'.
$pageFile = New-Object Collections.Hashtable ([StringComparer]::Ordinal)
foreach ($cs in @(Sources *.axaml.cs)) {
    $m = Select-String -Path $cs.FullName -Pattern '\[SamplePage\("([^"]+)"' | Select-Object -First 1
    if ($m) { $pageFile[$m.Matches[0].Groups[1].Value] = $cs.FullName -replace '\.cs$', '' }
}
Check 'the sample has pages' ($pageFile.Count -gt 0)
$opened = @(ArgumentsOf 'Open' | Sort-Object -Unique)
Check 'it opens pages by name' ($opened.Count -gt 0)
foreach ($title in $opened) {
    Check "the page it opens, '$title', is in the sample" ($pageFile.ContainsKey($title))
}

# Each section's names against its own page and the shell, not the whole sample, where two pages can both have a 'Basic'.
# A control found by ById or ReadOut is an x:Name there; one found by Named, by its text, is a quoted value there: a
# Content, a Text, an AutomationProperties.Name, or the shell's IndexTitle.
$shellFiles = @('SampleShell.axaml', 'SampleShell.axaml.cs') | ForEach-Object { Join-Path (Join-Path $sample 'Metered.Sample') $_ }
function XNames($files) { @($files | ForEach-Object { Select-String -Path $_ -Pattern 'x:Name="([^"]+)"' -AllMatches } | ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value }) }
# Only where a control's text comes from: a Content, a Text or an AutomationProperties.Name, or a C# constant (the shell's
# IndexTitle): not any quoted value, as Kind="Primary" beside Content="Primary" would let a changed label pass.
function Quoted($files, $text) {
    $t = [regex]::Escape($text)
    [bool]@($files | Where-Object { (Get-Content -Raw $_) -cmatch "(\b(Content|Text|AutomationProperties\.Name)=`"$t`")|(const string \w+ = `"$t`")" })
}
function Calls($root, $name) { @($root.FindAll({ param($n) $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq $name }, $true)) }
function Literally($call, $at) {
    $e = $call.CommandElements
    if ($e.Count -gt $at -and $e[$at] -is [System.Management.Automation.Language.StringConstantExpressionAst]) { $e[$at].Value }
}
function InSection($node) {
    for ($up = $node.Parent; $up; $up = $up.Parent) {
        if ($up -is [System.Management.Automation.Language.CommandAst] -and $up.GetCommandName() -eq 'Section') { return $true }
    }
    $false
}
# What Inner finds inside a control is a part of its template, named by the control's code: a C# constant there.
$library = Join-Path (Join-Path $repo 'src') 'Metered.Controls'
$controls = @(Get-ChildItem $library -Recurse -Filter *.cs | Where-Object { $_.FullName.Substring($library.Length) -notmatch '[\\/](bin|obj)[\\/]' } | ForEach-Object { $_.FullName })
Check 'the library has sources' ($controls.Count -gt 0)
$found = 0
foreach ($section in (Calls $ast 'Section')) {
    $title = $section.CommandElements[1].Value
    $body = $section.CommandElements[2]
    $page = @(Calls $body 'Open' | ForEach-Object { Literally $_ 1 } | Where-Object { $_ } | Select-Object -First 1)
    $files = @($shellFiles)
    if ($page.Count -and $pageFile.ContainsKey($page[0])) { $files += $pageFile[$page[0]] }
    $ids = XNames $files
    foreach ($call in @(@(Calls $body 'ById') + @(Calls $body 'RawById') + @(Calls $body 'ReadOut'))) {
        $id = Literally $call 1
        if ($id) { $found++; Check "'$title' finds '$id', an x:Name on its page or the shell" ($ids -ccontains $id) ($files -join ', ') }
    }
    foreach ($call in (Calls $body 'Named')) {
        $text = Literally $call 2
        if ($text) { $found++; Check "'$title' finds '$text' by its text, on its page or the shell" (Quoted $files $text) ($files -join ', ') }
    }
    foreach ($call in (Calls $body 'Inner')) {
        $text = Literally $call 3
        if ($text) { $found++; Check "'$title' finds '$text' inside a control, a label of Metered.Controls" (Quoted $controls $text) }
    }
}
# Outside the sections: what the list's and the pages' helpers find, which is the shell's.
foreach ($call in @(Calls $ast 'Named' | Where-Object { -not (InSection $_) })) {
    $text = Literally $call 2
    if ($text) { $found++; Check "the shell has '$text', which the helpers find by its text" (Quoted $shellFiles $text) }
}
Check 'it finds controls by name' ($found -gt 0)
# Every call outside the two functions' own definitions names its control in single quotes, so the checks above see it.
function InsideFinder($node) {
    for ($up = $node.Parent; $up; $up = $up.Parent) {
        if ($up -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return $up.Name -in 'ById', 'RawById', 'ReadOut' }
    }
    $false
}
$unquoted = @($ast.FindAll({ param($n)
            $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -in 'ById', 'RawById', 'ReadOut' -and
            -not ($n.CommandElements[1] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
                $n.CommandElements[1].StringConstantType -eq 'SingleQuoted') -and -not (InsideFinder $n)
        }, $true) | ForEach-Object { "line $($_.Extent.StartLineNumber): $($_.Extent.Text)" })
Check 'every control it finds by name is named in single quotes' ($unquoted.Count -eq 0) ($unquoted -join '; ')
$unquoted = @($ast.FindAll({ param($n)
            $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -eq 'Inner' -and
            -not ($n.CommandElements.Count -gt 3 -and $n.CommandElements[3] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
                $n.CommandElements[3].StringConstantType -eq 'SingleQuoted')
        }, $true) | ForEach-Object { "line $($_.Extent.StartLineNumber): $($_.Extent.Text)" })
Check 'every part it finds inside a control is named in single quotes' ($unquoted.Count -eq 0) ($unquoted -join '; ')

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
