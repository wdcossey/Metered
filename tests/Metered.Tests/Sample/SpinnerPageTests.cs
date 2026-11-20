using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;
using Spinner = Metered.Controls.Spinner;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Spinner page: its place in the catalogue (its issue's number, 54, before the theme), Metered's spinners
/// throughout, each within a phone's width, the lengths its XAML gives read as Base Web reads them, and the customised one.
/// </summary>
public class SpinnerPageTests
{
    [AvaloniaFact]
    public void The_Spinner_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(SpinnerPage));

        Assert.Equal("Spinner", entry.Title);
        Assert.Equal(54, typeof(SpinnerPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Switch") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Spinner_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new SpinnerPage();
        using var scene = new Scene(page);
        var spinners = page.GetVisualDescendants().OfType<Spinner>().ToList();
        Assert.Equal(11, spinners.Count);
        Assert.All(spinners, spinner =>
        {
            Assert.NotNull(spinner.Part(Spinner.SpinnerPart));
            Assert.True(spinner.Bounds.Width > 0);
            Assert.True(spinner.TranslatePoint(new Point(spinner.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });
    }

    [AvaloniaFact]
    public void Its_Lengths_And_Overrides_Are_Read()
    {
        var page = new SpinnerPage();
        using var scene = new Scene(page);
        Spinner Named(string name) => page.FindControl<Spinner>(name)!;

        Assert.Equal(SpinnerSize.Small, Named("Small").Size.Size);
        Assert.Equal(SpinnerSize.Large, Named("Large").Size.Size);
        Assert.Equal(60, Named("Length").Size.Number);
        Assert.Equal(new Size(64, 64), Named("Length").Bounds.Size);
        Assert.Contains(":size-scale1000", Named("Token").Classes);
        Assert.Equal(new Size(56, 56), Named("Token").Bounds.Size);
        Assert.Contains(":has-color", Named("Negative").Classes);
        // The documentation's withStyle example: a 96 box and a 12 border, 120 across.
        Assert.Equal(new Size(120, 120), Named("Custom").Bounds.Size);
    }
}
