using System.Text.RegularExpressions;
using System.Xml.Linq;
using Avalonia.Headless.XUnit;
using Metered.Sample;
using Metered.Sample.Diagnostics;
using Metered.Tests.Repository;

using static Metered.Tests.Sample.ShellFrames;

namespace Metered.Tests.Sample;

/// <summary>
/// <c>tools/android/run.ps1</c> reads what the Android head writes to logcat, and its <c>-SelfTest</c> checks its parsers
/// on fixtures in <c>tools/android/fixtures/</c>, with no phone. These hold the script and its fixtures to the app: the
/// markers, the intent extra and the activity's name are the app's, and every ready and cost line in a fixture has the
/// shape the shell writes. So a parser that passes its self-test reads the app, and a change to the lines fails here
/// rather than on the phone.
/// </summary>
public class AndroidRunTests
{
    private static readonly string Root = RepositoryFiles.FindRoot();

    private static readonly string Script = File.ReadAllText(Path.Combine(Root, "tools", "android", "run.ps1"));

    [Fact]
    public void The_Script_Waits_For_The_Shells_Markers_And_Names_The_Page_In_The_Apps_Extra()
    {
        Assert.Equal(SampleShell.ReadyMarker, ScriptString("ReadyMarker"));
        Assert.Equal(SampleShell.CostMarker, ScriptString("CostMarker"));
        Assert.Equal(App.PageExtra, ScriptString("PageExtra"));
    }

    // The head isn't built with the tests (it needs the android workload), so its names are read from its files: the
    // package and the activity `am start -n` names.
    [Fact]
    public void The_Script_Starts_The_Heads_Package_And_Activity()
    {
        var head = Path.Combine(Root, "samples", "Metered.Sample.Android");
        var applicationId = XDocument.Load(Path.Combine(head, "Metered.Sample.Android.csproj"))
            .Descendants("ApplicationId").Single().Value;
        var source = File.ReadAllText(Path.Combine(head, "MainActivity.cs"));
        // The [Activity] attribute's Name, the Java name `am start -n` gives.
        var activity = Assert.Single(Regex.Matches(source, "\\bName\\s*=\\s*\"([^\"]+)\""), _ => true).Groups[1].Value;

        Assert.Equal(applicationId, ScriptString("Package"));
        Assert.Equal($"$Activity = \"$Package/{activity}\"", ScriptLine("Activity"));
        // The start's intent and a later one's both read the page from the app's extra (another extra may be read too).
        Assert.Equal(2, Regex.Matches(source, "GetStringExtra\\(\\s*App\\.PageExtra\\s*\\)").Count);
    }

    // The shell's own line, for a real page in a window, against each fixture's: the same keys in the same order, a
    // number wherever the shell writes one, and the title last.
    [AvaloniaFact]
    public void Every_Ready_Line_In_The_Fixtures_Has_The_Shape_The_Shell_Writes()
    {
        var lines = new List<string>();
        var shell = new SampleShell { Report = lines.Add, ShowsCost = false };
        using var scene = new Scene(shell);
        shell.Open("Button");
        Draw();
        var written = lines.Single(line => line.StartsWith(SampleShell.ReadyMarker + " ", StringComparison.Ordinal));

        var fixtures = FixtureLines(SampleShell.ReadyMarker);
        Assert.True(fixtures.Count >= 5, $"Only {fixtures.Count} ready lines in the fixtures.");
        Assert.All(fixtures, fixture => Assert.Equal(ReadyShape(written), ReadyShape(fixture.Line)));
    }

    // PerformanceMonitor.Format is what the shell writes after the marker, in a Debug build.
    [Fact]
    public void Every_Cost_Line_In_The_Fixtures_Has_The_Shape_The_Shell_Writes()
    {
        var fixtures = FixtureLines(SampleShell.CostMarker);
        Assert.True(fixtures.Count >= 5, $"Only {fixtures.Count} cost lines in the fixtures.");
        Assert.All(fixtures, fixture =>
        {
            var title = Regex.Match(fixture.Line, $"^{SampleShell.CostMarker} (.+?): [0-9]").Groups[1].Value;
            Assert.NotEmpty(title);
            var written = $"{SampleShell.CostMarker} {PerformanceMonitor.Format(title, 1265.4, 332, 3, 3, 302.46, 88.12)}";
            Assert.Equal(NumbersOut(written), NumbersOut(fixture.Line));
        });
    }

    /// <summary>A line's numbers as <c>#</c>, and its title as <c>title=…</c>, so lines from two runs compare.</summary>
    private static string ReadyShape(string line) =>
        NumbersOut(Regex.Replace(line, " title=.+$", " title=…"));

    private static string NumbersOut(string line) => Regex.Replace(line, "[0-9]+(\\.[0-9]+)?", "#");

    /// <summary>
    /// Each line of the fixtures' logcat dumps that carries <paramref name="marker"/> as a word, from the marker on, as
    /// run.ps1 takes it; with the file it's in.
    /// </summary>
    private static List<(string File, string Line)> FixtureLines(string marker) =>
        Directory.GetFiles(Path.Combine(Root, "tools", "android", "fixtures"), "logcat-*.txt")
            .SelectMany(file => File.ReadAllLines(file).Select(line => (File: Path.GetFileName(file), Line: line)))
            .Where(found => found.Line.Contains(marker + " ", StringComparison.Ordinal))
            .Select(found => (found.File, found.Line[found.Line.IndexOf(marker + " ", StringComparison.Ordinal)..]))
            .ToList();

    /// <summary>The line of run.ps1 that sets <c>$<paramref name="name"/></c>, which has to be exactly one.</summary>
    private static string ScriptLine(string name) =>
        Assert.Single(Script.Split('\n'), line => line.StartsWith($"${name} = ", StringComparison.Ordinal)).TrimEnd('\r');

    /// <summary>The single-quoted string run.ps1 sets <c>$<paramref name="name"/></c> to.</summary>
    private static string ScriptString(string name) =>
        Regex.Match(ScriptLine(name), $"^\\${name} = '([^']*)'$").Groups[1].Value;
}
