using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Tests.Controls;
using Metered.Themes;

using static Metered.Tests.Sample.ShellFrames;

namespace Metered.Tests.Sample;

/// <summary>
/// The shell keeps its page clear of a phone's soft keyboard (#108): its host, the list or the page in its place, is in a
/// <see cref="KeyboardInset"/>, and its Debug footer isn't. Found on the Pixel on the Input page's Clearable input: near
/// the page's foot, the keyboard covered it, and the page couldn't be scrolled to show it.
/// </summary>
public class ShellKeyboardTests
{
    /// <summary>A keyboard 300 high at the foot of the scene's 844.</summary>
    private static readonly Rect Keyboard = new(0, Scene.Height - 300, Scene.Width, 300);

    [AvaloniaFact]
    public void The_Inputs_Pages_Clearable_Input_Near_Its_Foot_Is_Scrolled_Above_The_Keyboard()
    {
        var pane = new FakeInputPane();
        var shell = new SampleShell { Report = _ => { }, ShowsCost = false };
        InShell<KeyboardInset>(shell, "Keyboard").InputPane = pane;
        using var scene = new Scene(shell, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new BaseTheme()]);
        shell.Open("Input");
        Draw();
        scene.Pump();
        var page = ((Control)shell.Shown!).GetVisualDescendants().OfType<ScrollViewer>().First();
        page.Offset = new Vector(0, page.Extent.Height - page.Viewport.Height);
        scene.Pump();
        var clearable = shell.Shown!.FindControl<Input>("Clearable")!;
        clearable.Focus();
        scene.Pump();
        Assert.True(BottomOf(clearable, scene) > Keyboard.Y,
            $"Clearable ends at {BottomOf(clearable, scene)}, above the keyboard's top at {Keyboard.Y}: the scene doesn't test the bug.");

        pane.Open(Keyboard);
        scene.Pump();

        Assert.True(BottomOf(clearable, scene) <= Keyboard.Y,
            $"Clearable ends at {BottomOf(clearable, scene)}, under the keyboard's top at {Keyboard.Y}.");
        Assert.True(clearable.IsFocused);
    }

    /// <summary>
    /// With METERED_FRAMES naming a folder, the Input page scrolled to its foot with Clearable focused, under a stand-in
    /// for the keyboard (a block in <c>colors.backgroundInverseSecondary</c>, as Avalonia's headless platform draws none):
    /// <c>keyboard-before.png</c> as the Pixel showed it with nothing keeping the page clear (#108), and
    /// <c>keyboard-after.png</c> with the shell's <see cref="KeyboardInset"/> told the keyboard is up. Asserts nothing.
    /// </summary>
    [AvaloniaFact]
    public void Frames_Of_The_Keyboard_Over_The_Input_Page()
    {
        if (Environment.GetEnvironmentVariable(Metered.Tests.Controls.ButtonFrames.Variable) is not { Length: > 0 } folder)
            return;

        var pane = new FakeInputPane();
        var shell = new SampleShell { Report = _ => { }, ShowsCost = false };
        InShell<KeyboardInset>(shell, "Keyboard").InputPane = pane;
        var keyboard = new Border { Height = Keyboard.Height, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom, IsHitTestVisible = false };
        keyboard.Bind(Border.BackgroundProperty, keyboard.GetResourceObservable("colors.backgroundInverseSecondary"));
        using var scene = new Scene(new Panel { Children = { shell, keyboard } }, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new BaseTheme()]);
        shell.Open("Input");
        Draw();
        scene.Pump();
        var page = ((Control)shell.Shown!).GetVisualDescendants().OfType<ScrollViewer>().First();
        page.Offset = new Vector(0, page.Extent.Height - page.Viewport.Height);
        scene.Pump();
        shell.Shown!.FindControl<Input>("Clearable")!.Focus();
        scene.Pump();
        Directory.CreateDirectory(folder);
        scene.Capture().Save(Path.Combine(folder, "keyboard-before.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);

        pane.Open(Keyboard);
        scene.Pump();
        scene.Capture().Save(Path.Combine(folder, "keyboard-after.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    [AvaloniaFact]
    public void The_Host_Is_In_The_Inset_And_The_Footer_Is_Not()
    {
        var shell = new SampleShell { Report = _ => { } };
        var inset = InShell<KeyboardInset>(shell, "Keyboard");

        Assert.Same(InShell<Border>(shell, "Host"), inset.Child);
        Assert.False(inset.IsLogicalAncestorOf(InShell<Border>(shell, "Footer")));
    }

    private static double BottomOf(Visual visual, Scene scene) =>
        visual.TranslatePoint(new Point(0, visual.Bounds.Height), scene.Window)!.Value.Y;
}
