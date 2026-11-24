using Avalonia;
using Avalonia.Controls;
using Avalonia.Animation;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of what the input's own elements draw: the root's 2 px border, its colour moving to borderSelected on
/// focus, the clear icon's and the toggle's icons in the container's colour, and their 3 px focus ring. And, when
/// METERED_FRAMES names a folder, a gallery of Base Web's input scenarios, light and dark, saved there for
/// docs/controls/Input.
/// </summary>
public class InputFrames
{
    // An input at (20, 40), 350 wide, 48 high at the default size (the oracle's height).
    private static Scene Show(Input input, ThemeVariant? variant = null) =>
        new(new StackPanel { Margin = new Thickness(20, 40, 20, 20), Children = { input } }, variant);

    private static InputColourCase Case(string state, string theme = "Light") =>
        InputOracle.Value.Colours.Single(c => c.Theme == theme && c.State == state);

    [AvaloniaFact]
    public void The_root_draws_its_border_and_fill_and_the_focus_ring_on_focus()
    {
        var input = new Input { Text = "Value" };
        using var scene = Show(input);
        var rest = Case("rest");
        var frame = scene.Capture();
        // The border's 2 px at the top, mid-way along; the fill inside it.
        Assert.Equal(Color.Parse(rest.RootBorder), Frames.PixelAt(frame, 200, 40));
        Assert.Equal(Color.Parse(rest.RootBorder), Frames.PixelAt(frame, 200, 41));
        Assert.Equal(Color.Parse(rest.ContainerBackground), Frames.PixelAt(frame, 200, 43));
        Assert.Equal(Colors.White, Frames.PixelAt(frame, 200, 39));

        scene.Key(Key.Tab);
        Settle(scene);
        scene.Pump();
        var focused = Case("focused");
        frame = scene.Capture();
        Assert.Equal(Color.Parse(focused.RootBorder), Frames.PixelAt(frame, 200, 40));
        Assert.Equal(Color.Parse(focused.RootBorder), Frames.PixelAt(frame, 200, 87));
        Assert.Equal(Color.Parse(focused.RootBorder), Frames.PixelAt(frame, 20, 64));
        Assert.Equal(Color.Parse(focused.ContainerBackground), Frames.PixelAt(frame, 200, 85));
    }

    [AvaloniaFact]
    public void The_clear_icon_is_drawn_in_the_containers_colour_with_a_ring_on_keyboard_focus()
    {
        var input = new Input { Text = "Value", Clearable = true, Size = InputSize.Large };
        using var scene = Show(input);
        var icon = (InputIcon)input.Part(BaseInput.ClearIconPart)!;
        var at = icon.TranslatePoint(default, scene.Window)!.Value;
        Assert.Equal(at, new Point(Math.Round(at.X), Math.Round(at.Y)));
        var frame = scene.Capture();
        var c = Case("rest");
        // delete-alt: a disc of radius 8 round (12, 12) in a 24 viewBox, a cross cut out of it; at 22 px, 11/12 scale.
        var x = (int)at.X;
        var y = (int)at.Y;
        Assert.Equal(Color.Parse(c.ClearIconContainer!), Frames.PixelAt(frame, x + 11, y + 5));
        Assert.NotEqual(Color.Parse(c.ClearIconContainer!), Frames.PixelAt(frame, x + 10, y + 10));
        Assert.Equal(Color.Parse(c.ContainerBackground), Frames.PixelAt(frame, x + 11, y + 1));

        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        Assert.True(icon.IsFocused);
        Settle(scene);
        frame = scene.Capture();
        var ring = Color.Parse(InputOracle.Value.Outlines.Single(o => o.Theme == "Light" && o.Part == "ClearIcon" && o.FocusVisible).Outline!.Colour);
        // A 3 px line round the 22 px box, outside it.
        Assert.Equal(ring, Frames.PixelAt(frame, x + 11, y - 2));
        Assert.Equal(ring, Frames.PixelAt(frame, x + 11, y + 23));
        Assert.Equal(Color.Parse(c.ContainerBackground), Frames.PixelAt(frame, x + 11, y - 4));
    }

    [AvaloniaFact]
    public void The_toggle_draws_its_ring_on_keyboard_focus()
    {
        var input = new Input { Text = "1234", Type = InputType.Password };
        using var scene = Show(input);
        var toggle = (InputMaskToggleButton)input.Part(BaseInput.MaskToggleButtonPart)!;
        var at = toggle.TranslatePoint(default, scene.Window)!.Value;
        var (x, y) = ((int)Math.Round(at.X), (int)Math.Round(at.Y));
        var mid = y + (int)(toggle.Bounds.Height / 2);
        var c = Case("rest");
        var fill = Color.Parse(c.ContainerBackground);
        Assert.Equal(fill, Frames.PixelAt(scene.Capture(), x - 2, mid));

        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        Assert.True(toggle.IsFocused);
        Settle(scene);
        var frame = scene.Capture();
        var ring = Color.Parse(InputOracle.Value.Outlines.Single(o => o.Theme == "Light" && o.Part == "MaskToggleButton" && o.FocusVisible).Outline!.Colour);
        // A 3 px line outside the button's box, on its left.
        Assert.Equal(ring, Frames.PixelAt(frame, x - 2, mid));
        Assert.Equal(fill, Frames.PixelAt(frame, x - 4, mid));
    }

    [AvaloniaFact]
    public void The_toggle_draws_the_show_icon_then_the_hide_icon()
    {
        var input = new Input { Text = "1234", Type = InputType.Password, Size = InputSize.Large };
        using var scene = Show(input);
        var show = (InputIcon)input.Part(BaseInput.MaskToggleShowIconPart)!;
        var at = show.TranslatePoint(default, scene.Window)!.Value;
        // At the large size the icon is as large as its viewBox (24): a viewBox unit is a pixel.
        Assert.Equal(at, new Point(Math.Round(at.X), Math.Round(at.Y)));
        var size = InputOracle.Value.Layout.First(l => l.Size == "large" && l.Toggle is not null).Toggle!.ShowIconSize;
        Assert.Equal(InputOracle.Value.Icons["show"].ViewBox[2], size);
        Assert.Equal(new Size(size, size), show.Bounds.Size);
        var (x, y) = ((int)at.X, (int)at.Y);
        var c = Case("rest");
        var icon = Color.Parse(c.MaskToggleButton!);
        var fill = Color.Parse(c.ContainerBackground);
        var frame = scene.Capture();
        // show (nonzero): the eye, a hole of radius 2 to 4 round (10, 10), and the pupil inside it.
        Assert.Equal(icon, Frames.PixelAt(frame, x + 9, y + 9));
        Assert.Equal(icon, Frames.PixelAt(frame, x + 2, y + 10));
        Assert.Equal(fill, Frames.PixelAt(frame, x + 10, y + 20));

        scene.Click((Visual)input.Part(BaseInput.MaskToggleButtonPart)!);
        frame = scene.Capture();
        // hide: no pupil where it was, and the slash, the band from (2.2, 16.4) to (16.4, 2.2), across the middle.
        Assert.Equal(fill, Frames.PixelAt(frame, x + 11, y + 11));
        Assert.Equal(icon, Frames.PixelAt(frame, x + 9, y + 10));
    }

    /// <summary>
    /// Ends the parts' transitions at their end values (CheckboxFrames' way), so a frame shows where they end without
    /// waiting for them.
    /// </summary>
    internal static void Settle(Scene scene)
    {
        foreach (var input in scene.Window.GetVisualDescendants().OfType<Input>())
        {
            foreach (var name in Input.PartNames)
            {
                if (input.Part(name) is Animatable part) part.Transitions = null;
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
        var focused = new Input { Text = "Active", EndEnhancer = "00" };
        StackPanel Column() => new() { Width = 360, Spacing = 10, Margin = new Thickness(12, 24, 12, 12) };
        var left = Column();
        var right = Column();
        void Add(StackPanel column, string label, Control control)
        {
            column.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = ink });
            column.Children.Add(control);
        }

        // input-states.scenario.tsx: default, active, positive, error, disabled, read only (with an end enhancer).
        Add(left, "default", new Input { Text = "Default", EndEnhancer = "00" });
        Add(left, "focused", focused);
        Add(left, "positive", new Input { Text = "Positive", EndEnhancer = "00", Positive = true });
        Add(left, "error", new Input { Text = "Error", EndEnhancer = "00", Error = true });
        Add(left, "disabled", new Input { Text = "Disabled", EndEnhancer = "00", IsEnabled = false });
        Add(left, "read only", new Input { Text = "Read Only", EndEnhancer = "00", IsReadOnly = true });
        Add(left, "placeholder", new Input { PlaceholderText = "Placeholder" });
        // input-sizes.scenario.tsx.
        Add(left, "mini, compact, default, large", new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new Input { Size = InputSize.Mini, Text = "Mini", EndEnhancer = ".com" },
                new Input { Size = InputSize.Compact, Text = "Compact", EndEnhancer = ".com" },
                new Input { Text = "Default", EndEnhancer = ".com" },
                new Input { Size = InputSize.Large, Text = "Large", EndEnhancer = ".com" },
            },
        });
        // enhancers.tsx, input-clearable.scenario.tsx, input-password.scenario.tsx.
        Add(right, "start enhancer", new Input { StartEnhancer = "@", PlaceholderText = "Input with a start enhancer" });
        Add(right, "both enhancers", new Input { StartEnhancer = "$", EndEnhancer = ".00", PlaceholderText = "Input with start and end enhancers" });
        Add(right, "clearable, compact", new Input { Text = "Some", Clearable = true, Size = InputSize.Compact });
        Add(right, "clearable", new Input { Text = "Thing", Clearable = true });
        Add(right, "clearable, large", new Input { Text = "Or other", Clearable = true, Size = InputSize.Large });
        Add(right, "password, mini to large", new StackPanel
        {
            Spacing = 8,
            Children =
            {
                new Input { Size = InputSize.Mini, Type = InputType.Password, Text = "1234" },
                new Input { Size = InputSize.Compact, Type = InputType.Password, Text = "1234" },
                new Input { Type = InputType.Password, Text = "1234" },
                new Input { Size = InputSize.Large, Type = InputType.Password, Text = "1234" },
            },
        });
        var shown = new Input { Type = InputType.Password, Text = "1234" };
        Add(right, "password, shown", shown);
        // The scenario's value is Base Web's own example; Metered carries no Uber mark, so it reads "example" here.
        Add(right, "start enhancer, compact (input.scenario.tsx)", new Input { Text = "example", StartEnhancer = "@", EndEnhancer = ".com", Size = InputSize.Compact });

        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { left, right } },
            variant, width: 780, height: 980);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        scene.Click((Visual)shown.Part(BaseInput.MaskToggleButtonPart)!);
        scene.Pump();
        focused.Focus(NavigationMethod.Tab);
        Settle(scene);
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"input-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
