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

$path = @(DragPath 100 200 30 -60 3)
Check 'DragPath has its steps, and ends where the drag does' (($path.Count -eq 3) -and ($path[2].X -eq 130) -and ($path[2].Y -eq 140)) (($path | ForEach-Object { "$($_.X),$($_.Y)" }) -join ' ')
Check 'DragPath moves evenly, in whole pixels' (($path[0].X -eq 110) -and ($path[0].Y -eq 180) -and ($path[1].X -eq 120) -and ($path[1].Y -eq 160))

Check 'After reads a line''s one value' ((After 'Selected: 3' 'Selected') -ceq '3') (After 'Selected: 3' 'Selected')
$group = 'Clicked: 1; selected: 1, 2'
Check 'After reads each of two, by its label, case and all' (((After $group 'Clicked') -ceq '1') -and ((After $group 'selected') -ceq '1, 2')) "$(After $group 'Clicked') / $(After $group 'selected')"
Check 'After is nothing for a label not there' (($null -eq (After $group 'Selected')) -and ($null -eq (After 'Clicked: none' 'selected')))
Check 'After reads an empty selection as empty' ((After 'Clicked: 0; selected: ' 'selected') -ceq '') "'$(After 'Clicked: 0; selected: ' 'selected')'"

Check 'TapVerdict: a look back at its rest is at rest' (((TapVerdict 100 0 $false) -eq 'at rest') -and ((TapVerdict 100 ($Same - 1) $true) -eq 'at rest'))
Check 'TapVerdict: still drawn, the cursor off it, is left drawn' ((TapVerdict 100 50 $false) -eq 'left drawn')
Check 'TapVerdict: still drawn with the cursor on it is not settled' ((TapVerdict 100 50 $true) -like 'drawn, but*')
Check 'TapVerdict: a hover the samples don''t show settles nothing' ((TapVerdict $Moved 50 $false) -eq 'unsettled')
Check 'the same is under the moved' ($Same -lt $Moved)

'== the sizes it expects, against the sample'
$repo = (Resolve-Path (Join-Path (Join-Path $PSScriptRoot '..') '..')).Path
$sample = Join-Path $repo 'samples'
[xml]$window = Get-Content -Raw (Join-Path (Join-Path $sample 'Metered.Sample') 'MainWindow.axaml')
Check "the window is $WindowWidth x $WindowHeight, as MainWindow.axaml says" (([double]$window.Window.Width -eq $WindowWidth) -and ([double]$window.Window.Height -eq $WindowHeight)) "$($window.Window.Width) x $($window.Window.Height)"
[xml]$shell = Get-Content -Raw (Join-Path (Join-Path $sample 'Metered.Sample') 'SampleShell.axaml')
$bar = @($shell.SelectNodes('//*') | Where-Object { $_.GetAttribute('Name', 'http://schemas.microsoft.com/winfx/2006/xaml') -ceq 'Bar' })
Check "the bar is $BarHeight high, as SampleShell.axaml says" (($bar.Count -eq 1) -and ([double]$bar[0].Height -eq $BarHeight)) "$($bar | ForEach-Object { $_.Height })"

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
# Not bin/ or obj/, where a stale copy could keep a name the source has lost.
function Sources($filter) { Get-ChildItem $sample -Recurse -Filter $filter | Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } }
$pages = @(Sources *.cs | Select-String -Pattern '\[SamplePage\("([^"]+)"' | ForEach-Object { $_.Matches[0].Groups[1].Value })
$names = @(Sources *.axaml | Select-String -Pattern 'x:Name="([^"]+)"' -AllMatches | ForEach-Object { $_.Matches } | ForEach-Object { $_.Groups[1].Value })
Check 'the sample has pages' ($pages.Count -gt 0)
$opened = @(ArgumentsOf 'Open' | Sort-Object -Unique)
Check 'it opens pages by name' ($opened.Count -gt 0)
foreach ($title in $opened) {
    Check "the page it opens, '$title', is in the sample" ($pages -ccontains $title)
}
foreach ($id in @((ArgumentsOf 'ById') + (ArgumentsOf 'ReadOut') | Sort-Object -Unique)) {
    Check "the control it finds as '$id' is named in the sample" ($names -ccontains $id)
}
# Every call outside the two functions' own definitions names its control in single quotes, so the checks above see it.
function InsideFinder($node) {
    for ($up = $node.Parent; $up; $up = $up.Parent) {
        if ($up -is [System.Management.Automation.Language.FunctionDefinitionAst]) { return $up.Name -in 'ById', 'ReadOut' }
    }
    $false
}
$unquoted = @($ast.FindAll({ param($n)
            $n -is [System.Management.Automation.Language.CommandAst] -and $n.GetCommandName() -in 'ById', 'ReadOut' -and
            -not ($n.CommandElements[1] -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
                $n.CommandElements[1].StringConstantType -eq 'SingleQuoted') -and -not (InsideFinder $n)
        }, $true) | ForEach-Object { "line $($_.Extent.StartLineNumber): $($_.Extent.Text)" })
Check 'every control it finds by name is named in single quotes' ($unquoted.Count -eq 0) ($unquoted -join '; ')

"$($script:checks - $script:failures) of $($script:checks) checks passed"
if ($script:failures -gt 0) { exit 1 }
