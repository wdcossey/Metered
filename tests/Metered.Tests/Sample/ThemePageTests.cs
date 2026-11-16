using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Sample;
using Metered.Sample.Pages;
using Metered.Themes;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Theme page: last in the catalogue, its subtree drawn in the theme the page builds from its foundation
/// (Base Web's <c>createTheme</c> and <c>createDarkTheme</c>, held to Base Web in <c>Theming/</c>) and the rest of the
/// page in the app's, light and dark.
/// </summary>
public class ThemePageTests
{
    [AvaloniaFact]
    public void The_Theme_Page_Is_Last_In_The_Catalogue()
    {
        var entry = SampleShell.Pages[^1];

        Assert.Equal(typeof(ThemePage), entry.Type);
        Assert.Equal("Theme", entry.Title);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_Subtree_Draws_The_Theme_The_Page_Builds_And_The_Rest_The_Apps(string variant)
    {
        var dark = variant == "Dark";
        var page = new ThemePage();
        using var scene = new Scene(page, dark ? ThemeVariant.Dark : ThemeVariant.Light);
        var built = dark ? BaseTheme.CreateDarkTheme(ThemePage.DarkFoundation) : BaseTheme.CreateTheme(ThemePage.LightFoundation);
        var app = dark ? BaseTheme.DarkTheme : BaseTheme.LightTheme;

        var subtree = page.FindControl<Border>("Branded")!;
        var inside = page.FindControl<Button>("BrandedPrimary")!;
        var outside = page.FindControl<WrapPanel>("Plain")!.Children.OfType<Button>().First();

        // The subtree's own background, its primaryB, which differs from the app's in both variants.
        Assert.NotEqual(Hex(app["colors.backgroundPrimary"]), Hex(built["colors.backgroundPrimary"]));
        Assert.Equal(Hex(built["colors.backgroundPrimary"]), Colour(subtree.Background));
        Assert.Equal(Hex(built["colors.buttonPrimaryFill"]), Fill(inside));
        Assert.Equal(Hex(app["colors.buttonPrimaryFill"]), Fill(outside));
    }

    // Built once: opening the page again doesn't build the themes again, inside the time the shell measures.
    [AvaloniaFact]
    public void The_Page_Builds_Its_Themes_Once()
    {
        var first = new ThemePage().FindControl<Border>("Branded")!;
        var second = new ThemePage().FindControl<Border>("Branded")!;

        Assert.NotNull(ThemeProvider.GetTheme(first));
        Assert.Same(ThemeProvider.GetTheme(first), ThemeProvider.GetTheme(second));
        Assert.Same(ThemeProvider.GetDarkTheme(first), ThemeProvider.GetDarkTheme(second));
    }

    [AvaloniaFact]
    public void Every_Button_On_The_Page_Is_Metereds_Drawn_By_Its_Theme()
    {
        var page = new ThemePage();
        using var scene = new Scene(page);

        var buttons = page.GetVisualDescendants().OfType<Button>().ToList();
        Assert.Equal(7, buttons.Count);
        Assert.All(buttons, button => Assert.NotNull(button.Part(Button.BaseButtonPart)));
    }

    private static string Fill(Button button) =>
        Colour(((Border)button.Part(Button.BaseButtonPart)!).GetBaseValue(Border.BackgroundProperty).GetValueOrDefault());

    private static string Colour(Avalonia.Media.IBrush? brush) => Metered.Tests.Controls.ButtonColourTests.Colour(brush);

    private static string Hex(object colour) => "#" + ((Avalonia.Media.Color)colour).ToUInt32().ToString("X8");
}
