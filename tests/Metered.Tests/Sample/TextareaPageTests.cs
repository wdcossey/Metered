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
/// The sample's Textarea page: its place in the catalogue (its issue's number, 85, after Input and before the theme),
/// Metered's textareas throughout, each within a phone's width, the documentation's examples as set, the first one's
/// value said under it as it is typed after a finger's tap, and a finger's drag on the long text scrolling it, not the
/// page.
/// </summary>
public class TextareaPageTests
{
    private static Scene Show(TextareaPage page) =>
        new(page, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);

    private static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    [AvaloniaFact]
    public void The_Textarea_Page_Takes_Its_Issues_Number_After_Input()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(TextareaPage));

        Assert.Equal("Textarea", entry.Title);
        Assert.Equal(85, typeof(TextareaPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.Equal(titles.IndexOf("Input") + 1, titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Textarea_On_The_Page_Is_Metereds_And_Fits_A_Phones_Width()
    {
        var page = new TextareaPage();
        using var scene = new Scene(page);
        var textareas = page.GetVisualDescendants().OfType<Textarea>().ToList();
        Assert.Equal(11, textareas.Count);
        Assert.All(textareas, textarea =>
        {
            Assert.NotNull(textarea.Part(Textarea.RootPart));
            Assert.True(textarea.TranslatePoint(new Point(textarea.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width);
        });
        Assert.True(page.FindControl<Textarea>("Positive")!.Positive);
        Assert.True(page.FindControl<Textarea>("Error")!.Error);
        Assert.False(page.FindControl<Textarea>("Disabled")!.IsEnabled);
        Assert.True(page.FindControl<Textarea>("ReadOnly")!.IsReadOnly);
        Assert.Equal(InputSize.Compact, page.FindControl<Textarea>("Compact")!.Size);
        Assert.Equal(InputSize.Large, page.FindControl<Textarea>("Large")!.Size);
        Assert.Equal(TextareaResize.Both, page.FindControl<Textarea>("Resizable")!.Resize);
        Assert.True(page.FindControl<Textarea>("Clearable")!.Part(BaseInput.ClearIconPart) is Visual { IsEffectivelyVisible: true });
    }

    [AvaloniaFact]
    public void A_Tap_Focuses_The_First_Textarea_And_Its_Value_Is_Said_As_It_Is_Typed()
    {
        var page = new TextareaPage();
        using var scene = Show(page);
        var basic = page.FindControl<Textarea>("Basic")!;
        var state = page.FindControl<TextBlock>("State")!;
        Assert.Equal("Value: ", state.Text);

        Tap(scene, scene.CentreOf((Visual)basic.Part(BaseInput.InputPart)!));
        Assert.True(basic.IsFocused);

        scene.Window.KeyTextInput("Hello");
        scene.Key(Key.Enter);
        scene.Window.KeyTextInput("there");
        scene.Pump();
        Assert.Equal("Hello" + basic.NewLine + "there", basic.Text);
        Assert.Equal("Value: " + basic.Text, state.Text);
    }

    [AvaloniaFact]
    public void A_Drag_On_The_Long_Text_Scrolls_It_Not_The_Page()
    {
        var page = new TextareaPage();
        using var scene = Show(page);
        var pageScroller = page.GetVisualDescendants().OfType<ScrollViewer>().First();
        var textarea = page.FindControl<Textarea>("Long")!;
        textarea.BringIntoView();
        scene.Pump();
        var before = pageScroller.Offset.Y;
        var field = (Visual)textarea.Part(BaseInput.InputPart)!;
        var scroller = field.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.Equal(0, scroller.Offset.Y);

        var at = scene.CentreOf(field);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        for (var i = 1; i <= 10; i++)
        {
            scene.Window.TouchMove(touch, new Point(at.X, at.Y - 4 * i), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, new Point(at.X, at.Y - 40), RawInputModifiers.None);
        scene.Pump();
        Assert.True(scroller.Offset.Y > 10, $"the textarea moved {scroller.Offset.Y}");
        Assert.Equal(before, pageScroller.Offset.Y);
    }
}
