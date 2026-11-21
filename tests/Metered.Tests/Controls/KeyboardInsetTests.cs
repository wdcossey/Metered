using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Metered.Controls;
using Metered.Themes;

namespace Metered.Tests.Controls;

/// <summary>
/// <see cref="KeyboardInset"/> (#108): while a phone's soft keyboard is up, its child ends above it and the focused
/// control is brought into view, as a phone's browser shrinks its visual viewport to the space above the keyboard and
/// scrolls the focused field into it, which Base Web relies on. Base Web has no value for this: what each test expects
/// is the scene's own geometry, the keyboard's rect against the decorator's. Avalonia's headless platform has no input
/// pane, so the tests drive <see cref="FakeInputPane"/> through the decorator's seam; it raises
/// <see cref="IInputPane.StateChanged"/> as Avalonia's Android backend does, with the keyboard's final rect in the top
/// level's coordinates (<c>AndroidInsetsManager.OnStart</c>).
/// </summary>
public class KeyboardInsetTests
{
    /// <summary>Avalonia's simple theme under Metered's, for the ScrollViewer's and the TextBox's templates.</summary>
    private static IStyle[] Themes => [new SimpleTheme(), new BaseTheme()];

    /// <summary>A keyboard 300 high at the foot of the scene's 844.</summary>
    private static readonly Rect Keyboard = new(0, Scene.Height - 300, Scene.Width, 300);

    [AvaloniaFact]
    public void An_Open_Keyboard_Pads_The_Childs_Foot_By_As_Much_As_It_Covers()
    {
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);
        Assert.Equal(0, inset.Inset);
        Assert.Equal(Scene.Height, child.Bounds.Height);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(300, inset.Inset);
        Assert.Equal(Scene.Height - 300, child.Bounds.Height);
        Assert.Equal(Scene.Height, inset.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Closed_Keyboard_Gives_The_Child_Its_Height_Back()
    {
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);
        pane.Open(Keyboard);
        scene.Pump();

        pane.Close(Keyboard);
        scene.Pump();

        Assert.Equal(0, inset.Inset);
        Assert.Equal(Scene.Height, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Closed_Pane_With_A_Rect_Pads_Nothing()
    {
        // Avalonia's Closed is also a keyboard that floats or overlays, and so takes no part in the insets.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        using var scene = new Scene(inset);

        pane.Raise(InputPaneState.Closed, Keyboard);
        scene.Pump();

        Assert.Equal(0, inset.Inset);
    }

    [AvaloniaFact]
    public void Only_The_Part_Of_The_Decorator_The_Keyboard_Covers_Is_Padded()
    {
        // The decorator ends 100 above the view's foot, so the keyboard covers 200 of it.
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,100") };
        grid.Children.Add(inset);
        using var scene = new Scene(grid);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(200, inset.Inset);
        Assert.Equal(Scene.Height - 300, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Keyboard_Below_The_Decorator_Pads_Nothing()
    {
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,400") };
        grid.Children.Add(inset);
        using var scene = new Scene(grid);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(0, inset.Inset);
    }

    [AvaloniaFact]
    public void A_Keyboard_Over_All_Of_It_Pads_No_More_Than_Its_Height()
    {
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border(), Height = 200, VerticalAlignment = VerticalAlignment.Bottom };
        using var scene = new Scene(inset);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(200, inset.Inset);
    }

    [AvaloniaFact]
    public void Its_Own_Padding_Is_Kept_Above_The_Keyboard()
    {
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child, Padding = new Thickness(0, 0, 0, 16) };
        using var scene = new Scene(inset);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(300, inset.Inset);
        Assert.Equal(Scene.Height - 300 - 16, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void The_Focused_Input_At_The_Foot_Of_A_Page_Is_Scrolled_Above_The_Keyboard()
    {
        // The bug (#108): the Input page's Clearable input, near its foot, under the keyboard, and the page unable to
        // scroll it out.
        var (pane, inset, page, last) = PageOfInputs();
        using var scene = new Scene(inset, themes: Themes);
        last.Focus();
        scene.Pump();
        var before = BottomOf(last, scene);
        Assert.True(before > Keyboard.Y, $"The last input ends at {before}, above the keyboard's top at {Keyboard.Y}: the scene doesn't test the bug.");

        pane.Open(Keyboard);
        scene.Pump();

        Assert.True(BottomOf(last, scene) <= Keyboard.Y, $"The focused input ends at {BottomOf(last, scene)}, under the keyboard's top at {Keyboard.Y}.");
        Assert.True(page.Offset.Y > 0);
    }

    [AvaloniaFact]
    public void The_Page_Scrolls_As_Far_Again_As_The_Keyboard_Covers()
    {
        var (pane, inset, page, _) = PageOfInputs();
        using var scene = new Scene(inset, themes: Themes);
        var range = page.Extent.Height - page.Viewport.Height;

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(range + 300, page.Extent.Height - page.Viewport.Height, 3);
        Assert.Equal(0, page.Offset.Y);
    }

    [AvaloniaFact]
    public void A_Focused_Control_Outside_It_Is_Left_Where_It_Is()
    {
        var pane = new FakeInputPane();
        var (_, inset, page, _) = PageOfInputs(pane);
        var outside = new TextBox { Height = 40 };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(inset);
        Grid.SetRow(outside, 1);
        grid.Children.Add(outside);
        using var scene = new Scene(grid, themes: Themes);
        outside.Focus();
        scene.Pump();

        pane.Open(Keyboard);
        scene.Pump();

        Assert.True(outside.IsFocused);
        Assert.Equal(0, page.Offset.Y);
    }

    [AvaloniaFact]
    public void A_Keyboard_Already_Up_When_It_Is_Attached_Is_Kept_Clear_Of()
    {
        var pane = new FakeInputPane();
        pane.Open(Keyboard);
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);

        Assert.Equal(300, inset.Inset);
        Assert.Equal(Scene.Height - 300, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Resize_Reads_The_Keyboard_Again()
    {
        // A rotation needn't animate the keyboard's insets, and on API 31 and later only an animation raises StateChanged:
        // the pane's rect changes with no event, and the view's size with it.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        using var scene = new Scene(inset);
        pane.Open(Keyboard);
        scene.Pump();

        pane.Move(new Rect(0, 500 - 200, Scene.Width, 200));
        scene.Window.Height = 500;
        scene.Pump();

        Assert.Equal(200, inset.Inset);
    }

    [AvaloniaFact]
    public void Once_Detached_It_No_Longer_Listens_And_Pads_Nothing()
    {
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        using var scene = new Scene(inset);
        pane.Open(Keyboard);
        scene.Pump();

        scene.Window.Content = null;
        scene.Pump();

        Assert.Equal(0, inset.Inset);
        Assert.False(pane.HasListeners);
    }

    [AvaloniaFact]
    public void With_No_Input_Pane_It_Pads_Nothing()
    {
        // Avalonia's headless top level, like a desktop with no touch keyboard, offers no input pane.
        var child = new Border();
        var inset = new KeyboardInset { Child = child };
        using var scene = new Scene(inset);

        Assert.Null(scene.Window.InputPane);
        Assert.Equal(0, inset.Inset);
        Assert.Equal(Scene.Height, child.Bounds.Height);
    }

    /// <summary>A page of 20 inputs, taller than the view, in a ScrollViewer in a decorator: scrolled to its end, the last ends under the keyboard.</summary>
    private static (FakeInputPane Pane, KeyboardInset Inset, ScrollViewer Page, TextBox Last) PageOfInputs(FakeInputPane? pane = null)
    {
        pane ??= new FakeInputPane();
        var stack = new StackPanel { Spacing = 16, Margin = new Thickness(16, 0, 16, 32) };
        TextBox last = null!;
        for (var i = 0; i < 20; i++)
            stack.Children.Add(last = new TextBox { Height = 40 });
        var page = new ScrollViewer { Content = stack, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        return (pane, new KeyboardInset { InputPane = pane, Child = page }, page, last);
    }

    private static double BottomOf(Visual visual, Scene scene) =>
        visual.TranslatePoint(new Point(0, visual.Bounds.Height), scene.Window)!.Value.Y;
}

/// <summary>
/// An input pane a test opens and closes, raising <see cref="IInputPane.StateChanged"/> as Avalonia's Android backend
/// does: the new state, and the keyboard's final rect in the top level's coordinates, after the pane's own
/// <see cref="State"/> and <see cref="OccludedRect"/> are the new ones.
/// </summary>
internal sealed class FakeInputPane : KeyboardInset.IPane
{
    public InputPaneState State { get; private set; }

    public Rect OccludedRect { get; private set; }

    public event EventHandler<InputPaneStateEventArgs>? StateChanged;

    public bool HasListeners => StateChanged is not null;

    public void Open(Rect keyboard) => Raise(InputPaneState.Open, keyboard);

    /// <summary>Closed, from <paramref name="from"/>: Android's end rect for a closing keyboard is its empty one.</summary>
    public void Close(Rect from)
    {
        var start = OccludedRect;
        State = InputPaneState.Closed;
        OccludedRect = new Rect(from.X, from.Bottom, from.Width, 0);
        Dispatcher.UIThread.Invoke(() => StateChanged?.Invoke(this, new InputPaneStateEventArgs(State, start, OccludedRect)));
    }

    public void Raise(InputPaneState state, Rect end)
    {
        var start = OccludedRect;
        State = state;
        OccludedRect = end;
        Dispatcher.UIThread.Invoke(() => StateChanged?.Invoke(this, new InputPaneStateEventArgs(state, start, end)));
    }

    /// <summary>The keyboard's rect changes with no event, as it does on a rotation with no insets animation.</summary>
    public void Move(Rect keyboard) => OccludedRect = keyboard;
}
