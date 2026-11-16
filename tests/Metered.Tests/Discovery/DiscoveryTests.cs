using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Metered.Themes;

namespace Metered.Tests.Discovery;

/// <summary>
/// Theme files are found from a build-time manifest (Directory.Build.targets), not a registration
/// list. A broken manifest would silently unstyle every control, so these tests make it fail CI.
/// From Cornflower's and Avonic's DiscoveryTests.
/// </summary>
public class DiscoveryTests
{
    private static readonly Uri TestsBaseUri = new("avares://Metered.Tests/");

    [Fact]
    public void Manifest_Lists_Every_Matching_File_With_Forward_Slashes() =>
        Assert.Equal(
            ["Discovery/Fixture/First.axaml", "Discovery/Fixture/Nested/Second.axaml"],
            Metered.Tests.XamlManifest.Paths.Order(StringComparer.Ordinal));

    [AvaloniaFact]
    public void Every_Manifest_Path_Loads_As_Compiled_Xaml()
    {
        foreach (var path in Metered.Tests.XamlManifest.Paths)
        {
            var include = new ResourceInclude(TestsBaseUri) { Source = new Uri(TestsBaseUri, path) };
            Assert.NotEmpty(include.Loaded.Keys);
        }
    }

    [Fact]
    public void Theme_Merge_Order_Is_Ordinal()
    {
        string[] paths = ["Tokens/Z.axaml", "Controls/b.axaml", "Tokens/A.axaml", "Controls/B.axaml"];

        Assert.Equal(
            ["Controls/B.axaml", "Controls/b.axaml", "Tokens/A.axaml", "Tokens/Z.axaml"],
            BaseTheme.OrderForMerge(paths));
    }

    [AvaloniaFact]
    public void Theme_Merges_Exactly_Its_Manifest_In_Order()
    {
        var sources = new BaseTheme().Resources.MergedDictionaries
            .OfType<ResourceInclude>()
            .Select(include => include.Source!.AbsolutePath.TrimStart('/'));

        Assert.Equal(
            BaseTheme.OrderForMerge(Metered.Themes.XamlManifest.Paths),
            sources);
    }

    // The manifest is of the theme's files on disk: none missed by the glob, none listed twice.
    [Fact]
    public void The_Themes_Manifest_Holds_Its_Tokens_And_Controls()
    {
        var paths = Metered.Themes.XamlManifest.Paths;

        Assert.Equal(paths.Length, paths.Distinct(StringComparer.Ordinal).Count());
        Assert.Contains("Tokens/Colors.g.axaml", paths);
        Assert.Contains("Controls/Button.axaml", paths);
        Assert.Contains("Controls/Button.Parts.g.axaml", paths);
        Assert.All(paths, path => Assert.DoesNotContain("\\", path));
    }
}
