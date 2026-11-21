using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of what the textarea's own elements draw: the root's 2 px border, its colour moving to borderSelected on
/// focus, the clear icon at the top in the container's colour, a long text scrolled under the textarea's padding, and the
/// resize grip. And, when METERED_FRAMES names a folder, a gallery of Base Web's textarea scenarios and documentation
/// examples, light and dark, saved there for docs/controls/Textarea.
/// </summary>
public class TextareaFrames
{
    // A textarea at (20, 40), 350 wide.
    private static Scene Show(Textarea textarea, ThemeVariant? variant = null) =>
        new(new StackPanel { Margin = new Thickness(20, 40, 20, 20), Children = { textarea } }, variant);

    private static TextareaColourCase Case(string state, string theme = "Light") =>
        TextareaOracle.Value.Colours.Single(c => c.Theme == theme && c.State == state);

    [AvaloniaFact]
    public void The_root_draws_its_border_and_fill_and_the_focus_ring_on_focus()
    {
        var textarea = new Textarea { Text = "Value" };
        using var scene = Show(textarea);
        var height = (int)textarea.Bounds.Height;
        Assert.Equal(TextareaOracle.Value.Layout.First(c => c.Size == "default" && c.Resize is null && c.Rows == 3).Height, height);
        var rest = Case("rest");
        var frame = scene.Capture();
        Assert.Equal(Color.Parse(rest.RootBorder), Frames.PixelAt(frame, 200, 40));
        Assert.Equal(Color.Parse(rest.RootBorder), Frames.PixelAt(frame, 200, 41));
        Assert.Equal(Color.Parse(rest.ContainerBackground), Frames.PixelAt(frame, 200, 43));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 200, 39));

        scene.Key(Key.Tab);
        Settle(scene);
        var focused = Case("focused");
        frame = scene.Capture();
        Assert.Equal(Color.Parse(focused.RootBorder), Frames.PixelAt(frame, 200, 40));
        Assert.Equal(Color.Parse(focused.RootBorder), Frames.PixelAt(frame, 200, 40 + height - 1));
        Assert.Equal(Color.Parse(focused.RootBorder), Frames.PixelAt(frame, 20, 40 + height / 2));
        Assert.Equal(Color.Parse(focused.ContainerBackground), Frames.PixelAt(frame, 200, 40 + height - 3));
    }

    [AvaloniaFact]
    public void The_clear_icon_is_drawn_at_the_top_in_the_containers_colour()
    {
        var textarea = new Textarea { Text = "Value", Clearable = true, Size = InputSize.Large, Rows = 4 };
        using var scene = Show(textarea);
        var icon = (InputIcon)textarea.Part(BaseInput.ClearIconPart)!;
        var at = icon.TranslatePoint(default, scene.Window)!.Value;
        var c = Case("rest");
        // The container's top padding above it (12), under the root's border (2).
        var layout = TextareaOracle.Value.Layout.First(l => l.Size == "large");
        Assert.Equal(40 + layout.RootBorder[1] + layout.Clear.Padding[1], at.Y);
        var frame = scene.Capture();
        // delete-alt: a disc of radius 8 round (12, 12) in a 24 viewBox, a cross cut out of it; at 22 px, 11/12 scale.
        var (x, y) = ((int)at.X, (int)at.Y);
        Assert.Equal(Color.Parse(c.ClearIconContainer!), Frames.PixelAt(frame, x + 11, y + 5));
        Assert.NotEqual(Color.Parse(c.ClearIconContainer!), Frames.PixelAt(frame, x + 10, y + 10));
        // Below it, down the textarea's height, the container's fill.
        Assert.Equal(Color.Parse(c.ContainerBackground), Frames.PixelAt(frame, x + 11, y + 40));
    }

    // A textarea's padding is inside its scroller: scrolled, its text is drawn in the padding above it, as a browser's.
    [AvaloniaFact]
    public void A_scrolled_text_is_drawn_in_the_padding()
    {
        var textarea = new Textarea { Text = string.Join("\n", Enumerable.Repeat("MMMMMMMMMMMMMMMM", 12)) };
        using var scene = Show(textarea);
        var field = (TextareaField)textarea.Part(BaseInput.InputPart)!;
        var top = field.TranslatePoint(default, scene.Window)!.Value;
        var fill = Color.Parse(Case("rest").ContainerBackground);
        bool InkInPadding(Avalonia.Media.Imaging.WriteableBitmap frame)
        {
            for (var y = (int)top.Y; y < (int)(top.Y + field.Padding.Top); y++)
            {
                for (var x = (int)(top.X + field.Padding.Left); x < (int)(top.X + field.Padding.Left + 100); x++)
                {
                    if (Frames.PixelAt(frame, x, y) != fill) return true;
                }
            }

            return false;
        }

        Assert.False(InkInPadding(scene.Capture()));
        var scroller = field.GetVisualDescendants().OfType<ScrollViewer>().Single();
        scroller.Offset = new Vector(0, field.GetValue(TextBlock.LineHeightProperty) / 2);
        Assert.True(InkInPadding(scene.Capture()));
    }

    [AvaloniaFact]
    public void The_grip_is_drawn_where_it_resizes()
    {
        var textarea = new Textarea { Text = "Value", Resize = TextareaResize.Both };
        var still = new Textarea { Text = "Value" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20, 40, 20, 20), Spacing = 20, Children = { textarea, still } });
        var fill = Color.Parse(Case("rest").ContainerBackground);
        bool Drawn(Textarea t)
        {
            var field = (Visual)t.Part(BaseInput.InputPart)!;
            var corner = field.TranslatePoint(new Point(field.Bounds.Width, field.Bounds.Height), scene.Window)!.Value;
            var frame = scene.Capture();
            // Across its longer stroke, (-11, -2) to (-2, -11) from the corner, clear of the root's rounded border.
            for (var y = (int)corner.Y - 9; y <= (int)corner.Y - 4; y++)
            {
                for (var x = (int)corner.X - 9; x <= (int)corner.X - 4; x++)
                {
                    if (Frames.PixelAt(frame, x, y) != fill) return true;
                }
            }

            return false;
        }

        Assert.True(Drawn(textarea));
        Assert.False(Drawn(still));
    }

    /// <summary>Ends the parts' transitions at their end values (InputFrames' way).</summary>
    internal static void Settle(Scene scene)
    {
        foreach (var textarea in scene.Window.GetVisualDescendants().OfType<Textarea>())
        {
            foreach (var name in Textarea.PartNames)
            {
                if (textarea.Part(name) is Animatable part) part.Transitions = null;
            }
        }

        scene.Pump();
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;

        var dark = theme == "Dark";
        var ink = dark ? Brushes.White : Brushes.Black;
        StackPanel Column() => new() { Width = 360, Spacing = 10, Margin = new Thickness(12, 24, 12, 12) };
        var left = Column();
        var right = Column();
        void Add(StackPanel column, string label, Control control)
        {
            column.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = ink });
            column.Children.Add(control);
        }

        var focused = new Textarea { Text = "Focused" };
        var scrolled = new Textarea { Text = string.Join("\n", Enumerable.Range(1, 12).Select(i => $"Line {i} of a long text")) };
        // textarea.scenario.tsx and textarea-resize.scenario.tsx.
        Add(left, "textarea.scenario (clearable)", new Textarea { Clearable = true, Text = "initial value", PlaceholderText = "Uncontrolled textarea" });
        Add(left, "textarea-resize.scenario (resize both)", new Textarea { Resize = TextareaResize.Both, Text = "initial value", PlaceholderText = "Uncontrolled textarea" });
        // The documentation's basic and states examples.
        Add(left, "basic (empty)", new Textarea());
        Add(left, "focused", focused);
        Add(left, "positive", new Textarea { Positive = true, Text = "Positive state..." });
        Add(left, "error", new Textarea { Error = true, Text = "Error state..." });
        Add(right, "disabled", new Textarea { IsEnabled = false, Text = "Disabled state..." });
        Add(right, "read only", new Textarea { IsReadOnly = true, Text = "ReadOnly state..." });
        // The documentation's sizes example.
        Add(right, "compact, default, large", new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new Textarea { Size = InputSize.Compact, Text = "Compact size..." },
                new Textarea { Text = "Default size..." },
                new Textarea { Size = InputSize.Large, Text = "Large size..." },
            },
        });
        Add(right, "a long text, scrolled", scrolled);

        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { left, right } },
            variant, width: 780, height: 900);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        var scroller = ((Visual)scrolled.Part(BaseInput.InputPart)!).GetVisualDescendants().OfType<ScrollViewer>().Single();
        scroller.Offset = new Vector(0, 60);
        focused.Focus(NavigationMethod.Tab);
        Settle(scene);
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"textarea-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
