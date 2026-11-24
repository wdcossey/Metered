using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Headless.XUnit;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;
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
    public void A_Decorator_Below_A_Bar_Is_Padded_From_Its_Own_Place()
    {
        // As the shell's, under its 56 bar: the decorator is 788 high, from 56, and the keyboard covers its last 300.
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        var grid = new Grid { RowDefinitions = new RowDefinitions("56,*") };
        Grid.SetRow(inset, 1);
        grid.Children.Add(inset);
        using var scene = new Scene(grid);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(300, inset.Inset);
        Assert.Equal(Scene.Height - 56 - 300, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void An_Open_Pane_With_No_Rect_Pads_Nothing()
    {
        // Android's OccludedRect is empty, at the view's origin, while the window has no insets yet.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        using var scene = new Scene(inset);

        pane.Open(default);
        scene.Pump();

        Assert.Equal(0, inset.Inset);
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
    public void A_Keyboard_Beside_The_Window_Pads_Nothing()
    {
        // Windows' touch keyboard floating beside the window, as Avonic measured it on Windows 11 at 125% (its #189): at
        // 547.2, 797.6, 1194.4 x 488 in the client's coordinates, right of a 389.6 wide window. Its top is 46.4 above the
        // scene's foot, but it covers none of it (#120).
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);

        pane.Open(new Rect(547.2, 797.6, 1194.4, 488));
        scene.Pump();

        Assert.Equal(0, inset.Inset);
        Assert.Equal(Scene.Height, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Keyboard_Beside_The_Decorator_Pads_Nothing()
    {
        // The decorator fills the left half of the view; the keyboard covers the foot of the right half only (#120).
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        grid.Children.Add(inset);
        using var scene = new Scene(grid);

        pane.Open(new Rect(Scene.Width / 2, Scene.Height - 300, Scene.Width / 2, 300));
        scene.Pump();

        Assert.Equal(0, inset.Inset);
    }

    [AvaloniaFact]
    public void A_Keyboard_That_Overlaps_It_By_Less_Than_A_Device_Pixel_Is_Beside_It()
    {
        // The decorator fills the left half of the view, to 195; the keyboard under the right half starts at 194.8, its
        // rect converted from the screen's pixels: at a render scaling of 1, 0.2 across is less than a pixel (#120).
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        grid.Children.Add(inset);
        using var scene = new Scene(grid);
        Assert.Equal(1, scene.Window.RenderScaling);
        Assert.Equal(Scene.Width / 2, inset.Bounds.Right);

        pane.Open(new Rect(Scene.Width / 2 - 0.2, Scene.Height - 300, Scene.Width / 2 + 0.2, 300));
        scene.Pump();

        Assert.Equal(0, inset.Inset);
    }

    [AvaloniaTheory]
    [InlineData(Scene.Width / 2)]
    [InlineData(0)]
    public void A_Docked_Keyboard_Is_Read_For_Its_Top_Alone(double width)
    {
        // On a phone: the decorator fills the right half of the view, and the keyboard's rect, from the view's left, is
        // as wide as the left half (Android's, its width the client's before a rotation) or has no width at all. The
        // keyboard is the screen's own, docked across it, and covers the decorator's last 300 (#120).
        var pane = new FakeInputPane { Docked = true };
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        Grid.SetColumn(inset, 1);
        grid.Children.Add(inset);
        using var scene = new Scene(grid);

        pane.Open(new Rect(0, Scene.Height - 300, width, 300));
        scene.Pump();

        Assert.Equal(300, inset.Inset);
    }

    [AvaloniaFact]
    public void A_Keyboard_Over_Part_Of_Its_Width_Pads_It_From_The_Keyboards_Top()
    {
        // A keyboard 200 wide, from 100, at the view's foot: it overlaps the decorator, and the room below its top is lost.
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);

        pane.Open(new Rect(100, Scene.Height - 300, 200, 300));
        scene.Pump();

        Assert.Equal(300, inset.Inset);
        Assert.Equal(Scene.Height - 300, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Keyboard_That_Ends_Above_Its_Foot_Pads_It_From_The_Keyboards_Top()
    {
        // The keyboard spans 544 to 744 and the decorator reaches the view's foot at 844: the 100 below the keyboard is no
        // room for the child either, so the inset is 300, not the overlap's 200 high.
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);

        pane.Open(new Rect(0, Scene.Height - 300, Scene.Width, 200));
        scene.Pump();

        Assert.Equal(300, inset.Inset);
    }

    [AvaloniaFact]
    public void A_Decorator_Wholly_Below_The_Keyboards_Rect_Is_Padded_Whole()
    {
        // As a strip at the view's foot under Android's navigation bar, below the keyboard's rect, which ends above the
        // bar: the keyboard spans 544 to 744 and the decorator 744 to 844, all of it below the keyboard's top.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,100") };
        Grid.SetRow(inset, 1);
        grid.Children.Add(inset);
        using var scene = new Scene(grid);

        pane.Open(new Rect(0, Scene.Height - 300, Scene.Width, 200));
        scene.Pump();

        Assert.Equal(100, inset.Inset);
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
        // The focused input is in another scroller, below the decorator, scrolled out of its view.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var outside = new TextBox { Height = 40 };
        var other = new ScrollViewer { Height = 100, Content = new StackPanel { Children = { new Border { Height = 300 }, outside } } };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(inset);
        Grid.SetRow(other, 1);
        grid.Children.Add(other);
        using var scene = new Scene(grid, themes: Themes);
        outside.Focus();
        scene.Pump();
        other.Offset = default;
        scene.Pump();
        Assert.True(outside.IsFocused);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(200, inset.Inset);
        Assert.Equal(0, other.Offset.Y);
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
    public void An_Open_Pane_That_Loses_Its_Rect_Brings_The_Focus_Into_The_Whole_Room()
    {
        // The keyboard is up and the page scrolled back to its top, the focused input, its last, out of view below. The
        // pane then says open with no rect (as Android's before the window has insets): nothing to keep clear of, and the
        // input brought into view in the page's whole height, its foot at the view's, not at the keyboard's old top.
        var (pane, inset, page, last) = PageOfInputs();
        using var scene = new Scene(inset, themes: Themes);
        last.Focus();
        scene.Pump();
        pane.Open(Keyboard);
        scene.Pump();
        page.Offset = default;
        scene.Pump();

        pane.Open(default);
        scene.Pump();

        Assert.Equal(0, inset.Inset);
        Assert.Equal(Scene.Height, BottomOf(last, scene), 3);
    }

    [AvaloniaFact]
    public void A_Resize_That_Only_Moves_Its_Parent_Reads_The_Keyboard_Again()
    {
        // The decorator fills a 400 high panel centred in the window, from 222 to 622: the keyboard's top at 544 covers 78.
        // The window shrinks to 744 with no insets event, the keyboard's top moving to 444 with it: the panel moves up 50,
        // to 172 to 572, and the decorator's own bounds, its parent's, stay the same. The keyboard now covers 128.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var panel = new Border { Height = 400, VerticalAlignment = VerticalAlignment.Center, Child = inset };
        using var scene = new Scene(panel);
        pane.Open(Keyboard);
        scene.Pump();
        Assert.Equal(78, inset.Inset);
        var bounds = inset.Bounds;

        pane.Move(new Rect(0, 744 - 300, Scene.Width, 300));
        scene.Window.Height = 744;
        scene.Pump();

        Assert.Equal(bounds, inset.Bounds);
        Assert.Equal(128, inset.Inset);
    }

    [AvaloniaFact]
    public void A_Keyboard_Hidden_With_No_Animation_Is_Read_From_The_Insets()
    {
        // The keyboard hidden while the app is in the background: on API 31 and later the pane's state changes with no
        // StateChanged, and only the insets' change is raised (OnApplyWindowInsets).
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        using var scene = new Scene(inset);
        pane.Open(Keyboard);
        scene.Pump();

        pane.ChangeInsets(InputPaneState.Closed, default);
        scene.Pump();

        Assert.Equal(0, inset.Inset);
        Assert.Equal(Scene.Height, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Decorator_Whose_Height_Follows_Its_Child_Never_Grows_By_Its_Inset()
    {
        // In a StackPanel, the decorator is as high as its child, 600, and the keyboard covers its last 56. Read again and
        // again, it stays 600 high and pads 56.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border { Height = 600 } };
        using var scene = new Scene(new StackPanel { Children = { inset } });

        pane.Open(Keyboard);
        scene.Pump();
        pane.ChangeInsets(InputPaneState.Open, Keyboard);
        scene.Pump();
        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(600, inset.Bounds.Height);
        Assert.Equal(56, inset.Inset);
    }

    [AvaloniaFact]
    public void Attached_Again_In_The_Same_Place_Under_An_Open_Keyboard_It_Is_Kept_Clear_Of()
    {
        // Taken out and put back where it was: its bounds are the same, so only its attachment says to read the pane.
        var pane = new FakeInputPane();
        var child = new Border();
        var inset = new KeyboardInset { InputPane = pane, Child = child };
        var holder = new Border { Child = inset };
        using var scene = new Scene(holder);
        pane.Open(Keyboard);
        scene.Pump();

        holder.Child = null;
        scene.Pump();
        Assert.Equal(0, inset.Inset);
        holder.Child = inset;
        scene.Pump();

        Assert.Equal(300, inset.Inset);
        Assert.Equal(Scene.Height - 300, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Layout_Changed_Just_Before_The_Keyboard_Opens_Is_Read_As_It_Will_Be()
    {
        // The banner under it goes in the same turn as the keyboard opens, before any layout: the decorator will reach the
        // view's foot, and the keyboard cover 300 of it.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var banner = new Border { Height = 100 };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(inset);
        Grid.SetRow(banner, 1);
        grid.Children.Add(banner);
        using var scene = new Scene(grid);

        banner.IsVisible = false;
        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(300, inset.Inset);
    }

    [AvaloniaFact]
    public void A_Decorator_That_Moves_Under_An_Open_Keyboard_Is_Read_Again()
    {
        // A banner under it, 100 high, is taken away: the decorator now reaches the view's foot, and the keyboard covers
        // 300 of it, not 200.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        var banner = new Border { Height = 100 };
        var grid = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        grid.Children.Add(inset);
        Grid.SetRow(banner, 1);
        grid.Children.Add(banner);
        using var scene = new Scene(grid);
        pane.Open(Keyboard);
        scene.Pump();
        Assert.Equal(200, inset.Inset);

        banner.IsVisible = false;
        scene.Pump();

        Assert.Equal(300, inset.Inset);
    }

    [AvaloniaFact]
    public void One_Inside_Another_Pads_Nothing_More()
    {
        // A page with its own, in a shell with one: the outer makes the room, and the inner, read in it, adds nothing.
        var pane = new FakeInputPane();
        var child = new Border();
        var inner = new KeyboardInset { InputPane = pane, Child = child };
        var outer = new KeyboardInset { InputPane = pane, Child = inner };
        using var scene = new Scene(outer);

        pane.Open(Keyboard);
        scene.Pump();

        Assert.Equal(300, outer.Inset);
        Assert.Equal(0, inner.Inset);
        Assert.Equal(Scene.Height - 300, child.Bounds.Height);
    }

    [AvaloniaFact]
    public void A_Rotation_Under_An_Open_Keyboard_Brings_The_Focused_Input_Back_Into_View()
    {
        // The view shrinks to 600 and the keyboard's top moves to 300, with no StateChanged: the focused input, in view
        // above the keyboard before, is brought into view again.
        var (pane, inset, _, last) = PageOfInputs();
        using var scene = new Scene(inset, themes: Themes);
        last.Focus();
        scene.Pump();
        pane.Open(Keyboard);
        scene.Pump();

        var turned = new Rect(0, 600 - 300, Scene.Width, 300);
        pane.Move(turned);
        scene.Window.Height = 600;
        scene.Pump();

        Assert.True(BottomOf(last, scene) <= turned.Y, $"The focused input ends at {BottomOf(last, scene)}, under the keyboard's top at {turned.Y}.");
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void The_Caret_Of_A_Text_Taller_Than_The_Room_Left_Is_Brought_Above_The_Keyboard(bool readOnly)
    {
        // A text box 700 high, its 60 lines scrolled in its own scroller to its caret on the last: brought into view
        // alone, it would show its top and leave the line being typed under the keyboard. Read-only too: its caret is
        // still where the reader is.
        var pane = new FakeInputPane();
        var text = new TextBox
        {
            Height = 700, AcceptsReturn = true, IsReadOnly = readOnly, TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            Text = string.Join(Environment.NewLine, Enumerable.Range(1, 60).Select(line => $"line {line}")),
        };
        var page = new ScrollViewer { Content = new StackPanel { Children = { new Border { Height = 400 }, text } } };
        var inset = new KeyboardInset { InputPane = pane, Child = page };
        using var scene = new Scene(inset, themes: Themes);
        text.Focus();
        text.CaretIndex = text.Text.Length;
        scene.Pump();
        var presenter = text.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.True(presenter.Bounds.Height > text.Bounds.Height, "The text doesn't scroll in its own scroller: the scene doesn't test it.");

        pane.Open(Keyboard);
        scene.Pump();

        var caret = CaretOf(text, scene);
        Assert.True(caret.Height > 0);
        // Within half a pixel: the caret's line is laid out at the font's fractional height, and the page's offset is
        // rounded to whole pixels (Linux's font metrics left it 0.125 lower than Windows').
        Assert.True(caret.Bottom <= Keyboard.Y + 0.5, $"The caret ends at {caret.Bottom}, under the keyboard's top at {Keyboard.Y}.");
        Assert.True(caret.Top >= 0, $"The caret starts at {caret.Top}, above the view.");
    }

    [AvaloniaFact]
    public void A_Reading_Posted_Before_The_Keyboard_Says_It_Opened_Keeps_Its_Final_Rect()
    {
        // Android: OnApplyWindowInsets raises SafeAreaChanged, which posts a reading, then the animation's OnStart raises
        // StateChanged with the keyboard's final rect, while the pane's own rect may still be its start.
        var pane = new FakeInputPane();
        var inset = new KeyboardInset { InputPane = pane, Child = new Border() };
        using var scene = new Scene(inset);

        pane.Animate(InputPaneState.Open, Keyboard);
        scene.Pump();

        Assert.Equal(300, inset.Inset);
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

    /// <summary>A text box's caret in the window's coordinates, from its presenter's layout.</summary>
    private static Rect CaretOf(TextBox text, Scene scene)
    {
        var presenter = text.GetVisualDescendants().OfType<TextPresenter>().Single();
        return presenter.TextLayout.HitTestTextPosition(presenter.CaretIndex).TransformToAABB(presenter.TransformToVisual(scene.Window)!.Value);
    }

    private static double BottomOf(Visual visual, Scene scene) =>
        visual.TranslatePoint(new Point(0, visual.Bounds.Height), scene.Window)!.Value.Y;
}

/// <summary>
/// An input pane a test opens and closes, raising <see cref="IInputPane.StateChanged"/> as Avalonia's Android backend
/// does: the new state, and the keyboard's final rect in the top level's coordinates, after the pane's own
/// <see cref="State"/> and <see cref="OccludedRect"/> are the new ones. Or changed with no state event, as Android's
/// pane changes when its insets change with no animation, and only the insets' change raised.
/// </summary>
internal sealed class FakeInputPane : KeyboardInset.IPane
{
    public InputPaneState State { get; private set; }

    public Rect OccludedRect { get; private set; }

    /// <summary>A phone's keyboard, read for its top alone; else, as by default, a desktop's, its sides read too.</summary>
    public bool Docked { get; init; }

    public event EventHandler<InputPaneStateEventArgs>? StateChanged;

    public event EventHandler? InsetsChanged;

    public bool HasListeners => StateChanged is not null || InsetsChanged is not null;

    public void Open(Rect keyboard) => Raise(InputPaneState.Open, keyboard);

    /// <summary>Closed, from <paramref name="from"/>: Android's end rect for a closing keyboard is its empty one.</summary>
    public void Close(Rect from) => Raise(InputPaneState.Closed, new Rect(from.X, from.Bottom, from.Width, 0));

    public void Raise(InputPaneState state, Rect end)
    {
        var start = OccludedRect;
        State = state;
        OccludedRect = end;
        Dispatcher.UIThread.Invoke(() => StateChanged?.Invoke(this, new InputPaneStateEventArgs(state, start, end)));
    }

    /// <summary>The keyboard's rect changes with no event at all, as the view's size changes on a rotation.</summary>
    public void Move(Rect keyboard) => OccludedRect = keyboard;

    /// <summary>
    /// As Android opens or closes it: the insets' change first, then the state event with the final rect, while the
    /// pane's own rect is still where the animation starts.
    /// </summary>
    public void Animate(InputPaneState state, Rect end)
    {
        var start = OccludedRect;
        State = state;
        Dispatcher.UIThread.Invoke(() => InsetsChanged?.Invoke(this, EventArgs.Empty));
        Dispatcher.UIThread.Invoke(() => StateChanged?.Invoke(this, new InputPaneStateEventArgs(state, start, end)));
    }

    /// <summary>The pane changes with no state event, and the platform's insets say they changed.</summary>
    public void ChangeInsets(InputPaneState state, Rect keyboard)
    {
        State = state;
        OccludedRect = keyboard;
        Dispatcher.UIThread.Invoke(() => InsetsChanged?.Invoke(this, EventArgs.Empty));
    }
}
