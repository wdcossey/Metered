using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's FormControl page: its place in the catalogue (its issue's number, 88, after Textarea and before the
/// theme), Metered's form controls throughout, each within a phone's width, the documentation's examples and the
/// scenarios as set; a finger's tap on a label focusing its control, the one its HtmlFor names (by name, in XAML) among
/// two; the counter following what is typed; and the validation example's error after the field is left.
/// </summary>
public class FormControlPageTests
{
    private static Scene Show(FormControlPage page) =>
        new(page, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);

    private static void Tap(Scene scene, Visual visual)
    {
        var at = scene.CentreOf(visual);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    /// <summary>Scrolls the page to the control, as a finger would, so that a tap lands on it.</summary>
    private static void Into(Scene scene, Control control)
    {
        control.BringIntoView();
        scene.Pump();
    }

    [AvaloniaFact]
    public void The_FormControl_Page_Takes_Its_Issues_Number_After_Textarea()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(FormControlPage));

        Assert.Equal("FormControl", entry.Title);
        Assert.Equal(88, typeof(FormControlPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.Equal(titles.IndexOf("Textarea") + 1, titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Form_Control_Is_Metereds_And_Fits_A_Phones_Width_With_The_Examples_As_Set()
    {
        var page = new FormControlPage();
        using var scene = Show(page);
        var forms = page.GetVisualDescendants().OfType<FormControl>().ToList();
        Assert.Equal(18, forms.Count);
        Assert.All(forms, form =>
        {
            Assert.NotNull(form.Part(FormControl.LabelPart));
            Assert.True(form.TranslatePoint(new Point(form.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });

        Assert.Equal("Positive caption", Hint(page.FindControl<FormControl>("Positive")!));
        Assert.Equal("Error caption", Hint(page.FindControl<FormControl>("Error")!));
        Assert.False(page.FindControl<FormControl>("Disabled")!.IsEnabled);
        // {x:True} in XAML is the boolean: the state, with the caption under it.
        var positive = page.FindControl<FormControl>("PositiveState")!;
        Assert.Equal(true, positive.Positive);
        Assert.Equal("Input caption", Hint(positive));
        Assert.True(((Input)positive.Content!).Positive);
        var error = page.FindControl<FormControl>("ErrorState")!;
        Assert.Equal(true, error.Error);
        Assert.Equal("Input caption", Hint(error));
        Assert.True(((Input)error.Content!).Error);
        Assert.Equal("Success!", Hint(page.FindControl<FormControl>("Success")!));
        Assert.Equal("Error!", Hint(page.FindControl<FormControl>("Failure")!));
        Assert.Equal("0", End(page.FindControl<FormControl>("Counter")!));
        Assert.Equal("27/40", End(page.FindControl<FormControl>("CounterMax")!));
        Assert.Equal("0/40", End(page.FindControl<FormControl>("CounterTextarea")!));
        Assert.Equal("0/100", End(page.FindControl<FormControl>("CounterOverride")!));
        // HtmlFor="Five", by name.
        Assert.Same(page.FindControl<Input>("Five"), page.FindControl<FormControl>("For")!.HtmlFor);
    }

    [AvaloniaFact]
    public void A_Tap_On_A_Label_Focuses_Its_Control_And_HtmlFor_Names_Another()
    {
        var page = new FormControlPage();
        using var scene = Show(page);
        var basic = page.FindControl<FormControl>("Basic")!;
        Tap(scene, (Visual)basic.Part(FormControl.LabelPart)!);
        Assert.True(page.FindControl<Input>("BasicInput")!.IsFocused);

        var form = page.FindControl<FormControl>("For")!;
        Into(scene, form);
        Tap(scene, (Visual)form.Part(FormControl.LabelPart)!);
        Assert.True(page.FindControl<Input>("Five")!.IsFocused);
        Assert.False(page.FindControl<Input>("Four")!.IsFocused);
    }

    [AvaloniaFact]
    public void The_Counter_Follows_What_Is_Typed_And_The_Override_Its_Own_Length()
    {
        var page = new FormControlPage();
        using var scene = Show(page);
        var counter = page.FindControl<FormControl>("Counter")!;
        var input = (Input)counter.Content!;
        Into(scene, counter);
        scene.Click(input);
        foreach (var ch in "hello") scene.Window.KeyTextInput(ch.ToString());
        scene.Pump();
        Assert.Equal("5", End(counter));

        var over = page.FindControl<FormControl>("CounterOverride")!;
        page.FindControl<Input>("CounterOverrideInput")!.Text = "abc";
        scene.Pump();
        Assert.Equal("3/100", End(over));
    }

    [AvaloniaFact]
    public void The_Validation_Example_Shows_Its_Error_Once_The_Field_Is_Left_Invalid()
    {
        var page = new FormControlPage();
        using var scene = Show(page);
        var form = page.FindControl<FormControl>("Validation")!;
        var email = page.FindControl<Input>("Email")!;
        email.Text = "not an email";
        scene.Pump();
        Assert.Null(Hint(form));

        email.Focus();
        page.FindControl<Input>("BasicInput")!.Focus();
        scene.Pump();
        Assert.Equal("Please input a valid email address", Hint(form));
        Assert.True(email.Error);

        email.Text = "someone@example.com";
        scene.Pump();
        Assert.Null(Hint(form));
        Assert.False(email.Error);
    }

    private static string? Hint(FormControl form) =>
        form.Part(FormControl.CaptionPart) is Visual { IsEffectivelyVisible: true }
            ? ((ContentPresenter)form.Part(FormControl.CaptionMessagePart)!).Content as string
            : null;

    private static string? End(FormControl form) =>
        form.Part(FormControl.LabelEndEnhancerPart) is Visual { IsEffectivelyVisible: true }
            ? ((ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!).Content as string
            : null;
}
