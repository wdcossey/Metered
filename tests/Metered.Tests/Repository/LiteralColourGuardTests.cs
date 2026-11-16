using System.Text;

namespace Metered.Tests.Repository;

/// <summary>
/// No literal colours outside the theme's <c>Tokens/</c> (rule 5). The last test scans the real repository; the
/// others prove each check on files made in memory.
/// </summary>
public class LiteralColourGuardTests
{
    private static IReadOnlyList<string> Check(string path, string content) =>
        LiteralColourGuard.Check([(path, () => new MemoryStream(Encoding.UTF8.GetBytes(content)))]);

    private const string ControlTheme = "src/Metered.Themes/Controls/Button.axaml";

    private static string Xaml(string body) =>
        $"""<ResourceDictionary xmlns="https://github.com/avaloniaui" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">{body}</ResourceDictionary>""";

    [Theory]
    [InlineData("""<Border Background="#FF276EF1" />""")]
    [InlineData("""<Border Background="#FFF" />""")]
    [InlineData("""<Border BorderBrush="Red" />""")]
    [InlineData("""<Color>#40000000</Color>""")]
    public void A_Colour_In_A_Control_Theme_Fails(string body) =>
        Assert.Contains("literal colour", Assert.Single(Check(ControlTheme, Xaml(body))));

    // A generated part theme is held to it too: it names its colours by token.
    [Fact]
    public void A_Colour_In_A_Generated_Part_Theme_Fails() =>
        Assert.Single(Check("src/Metered.Themes/Controls/Button.Parts.g.axaml", Xaml("""<Border Background="#FF000000" />""")));

    [Theory]
    [InlineData("""<Border Background="{DynamicResource colors.buttonPrimaryFill}" />""")]
    [InlineData("""<Border Background="Transparent" />""")]
    [InlineData("""<Border Width="100" HorizontalAlignment="Left" />""")]
    [InlineData("""<!-- Pressed: #FF276EF1, as Base Web gives it --><Border />""")]
    public void A_Resource_Transparent_Or_Comment_Passes(string body) =>
        Assert.Empty(Check(ControlTheme, Xaml(body)));

    [Fact]
    public void A_Colour_In_Tokens_Passes() =>
        Assert.Empty(Check("src/Metered.Themes/Tokens/Colors.g.axaml", Xaml("""<Color x:Key="K">#FF000000</Color>""")));

    // Only the theme's own Tokens/ folder, not any path that has the word in it.
    [Fact]
    public void A_Colour_In_Another_Tokens_Folder_Fails() =>
        Assert.Single(Check("src/Metered.Controls/Tokens/Colors.axaml", Xaml("""<Color x:Key="K">#FF000000</Color>""")));

    [Theory]
    [InlineData("""var c = Color.Parse("#FF276EF1");""")]
    [InlineData("""var c = Color.FromRgb(1, 2, 3);""")]
    [InlineData("""Background = Brushes.Red;""")]
    [InlineData("""var c = Colors.White;""")]
    public void A_Colour_In_Code_Fails(string line) =>
        Assert.NotEmpty(Check("src/Metered.Controls/Controls/Button/Button.cs", line));

    [Theory]
    [InlineData("""context.FillRectangle(Brushes.Transparent, new Rect(Bounds.Size));""")]
    [InlineData("""// See #19 and #123.""")]
    public void Transparent_And_Issue_Numbers_Pass_In_Code(string line) =>
        Assert.Empty(Check("src/Metered.Controls/Controls/Button/Button.cs", line));

    [Fact]
    public void Tests_Samples_And_Tools_Are_Not_Checked() =>
        Assert.Empty(Check("tests/Metered.Tests/Spikes/DerivedTokens.axaml", Xaml("""<Border Background="#FFFF0000" />""")));

    [Fact]
    public void The_Scan_Sees_The_Shipped_Code()
    {
        var paths = RepositoryFiles.All().Select(file => file.Path).ToList();

        Assert.Contains("src/Metered.Themes/Controls/Button.axaml", paths);
        Assert.Contains("src/Metered.Controls/Controls/Button/Button.cs", paths);
    }

    [Fact]
    public void The_Shipped_Code_Holds_No_Literal_Colour_Outside_Tokens() =>
        Assert.Empty(LiteralColourGuard.Check(RepositoryFiles.All()));
}
