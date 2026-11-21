using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Input page: its place in the catalogue (its issue's number, 84, before the theme), Metered's inputs
/// throughout, each within a phone's width, the documentation's examples as set, and the first one's value said under
/// it as it is typed, after a finger's tap in the page's ScrollViewer focuses it.
/// </summary>
public class InputPageTests
{
    [AvaloniaFact]
    public void The_Input_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(InputPage));

        Assert.Equal("Input", entry.Title);
        Assert.Equal(84, typeof(InputPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.True(titles.IndexOf("Slider") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Input_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new InputPage();
        using var scene = new Scene(page);
        var inputs = page.GetVisualDescendants().OfType<Input>().ToList();
        Assert.Equal(17, inputs.Count);
        Assert.All(inputs, input =>
        {
            Assert.NotNull(input.Part(Input.RootPart));
            Assert.True(input.TranslatePoint(new Point(input.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });
        Assert.True(page.FindControl<Input>("Error")!.Error);
        Assert.False(page.FindControl<Input>("Disabled")!.IsEnabled);
        Assert.Equal(InputSize.Mini, page.FindControl<Input>("Mini")!.Size);
        Assert.Equal(InputSize.Large, page.FindControl<Input>("Large")!.Size);
        Assert.Equal(InputAdjoined.Left, page.FindControl<Input>("Start")!.Adjoined);
        Assert.Equal(InputAdjoined.Both, page.FindControl<Input>("Both")!.Adjoined);
        Assert.True(page.FindControl<Input>("Clearable")!.Part(BaseInput.ClearIconPart) is Visual { IsEffectivelyVisible: true });
        var password = page.FindControl<Input>("Password")!;
        Assert.True(password.IsMasked);
        Assert.True(password.Part(BaseInput.MaskToggleButtonPart) is Visual { IsEffectivelyVisible: true });
        Assert.NotNull(page.FindControl<Input>("Custom")!.Overrides);
    }

    [AvaloniaFact]
    public void A_Tap_Focuses_The_First_Input_And_Its_Value_Is_Said_As_It_Is_Typed()
    {
        var page = new InputPage();
        using var scene = new Scene(page, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        var basic = page.FindControl<Input>("Basic")!;
        var state = page.FindControl<TextBlock>("State")!;
        Assert.Equal("Value: Hello", state.Text);

        var at = scene.CentreOf((Visual)basic.Part(BaseInput.InputPart)!);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
        Assert.True(basic.IsFocused);

        basic.CaretIndex = basic.Text!.Length;
        scene.Window.KeyTextInput("!");
        scene.Pump();
        Assert.Equal("Hello!", basic.Text);
        Assert.Equal("Value: Hello!", state.Text);
    }
}
