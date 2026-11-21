using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of the form control's text: the label, the counter past its maximum and the caption each in Base Web's
/// colour, where the layout puts them. And, when METERED_FRAMES names a folder, a gallery of Base Web's form control
/// scenarios and documentation examples, light and dark, saved there for docs/controls/FormControl.
/// </summary>
public class FormControlFrames
{
    private static FormControlColourCase Case(string state, string theme = "Light") =>
        FormControlOracle.Value.Colours.Single(c => c.Theme == theme && c.State == state);

    /// <summary>
    /// Whether the element's box has ink of the colour: a pixel that is the colour blended over the page (its box's top-left
    /// pixel, above the text), at least half of it. Not the colour exactly: how much of a pixel a glyph's stem fills is the
    /// platform's font rasterizer's (Windows' and Linux's Skia differ), but whatever it fills, the blend is of this colour.
    /// </summary>
    private static bool Draws(Scene scene, Visual element, string colour)
    {
        var frame = scene.Capture();
        var want = Color.Parse(colour);
        var at = element.TranslatePoint(default, scene.Window)!.Value;
        var page = Frames.PixelAt(frame, (int)at.X, (int)at.Y);
        // The channel the colour and the page differ most in gives how much of the colour a pixel is.
        var spans = new[] { want.R - page.R, want.G - page.G, want.B - page.B };
        var main = Array.IndexOf(spans, spans.MaxBy(Math.Abs));
        for (var y = (int)at.Y; y < (int)(at.Y + element.Bounds.Height); y++)
        {
            for (var x = (int)at.X; x < (int)(at.X + element.Bounds.Width); x++)
            {
                var p = Frames.PixelAt(frame, x, y);
                var got = new[] { p.R - page.R, p.G - page.G, p.B - page.B };
                var t = (double)got[main] / spans[main];
                if (t < 0.5 || t > 1.05) continue;
                if (Enumerable.Range(0, 3).All(i => Math.Abs(got[i] - t * spans[i]) <= 8)) return true;
            }
        }

        return false;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_label_the_counter_and_the_caption_are_drawn_in_Base_Webs_colours(string theme)
    {
        var c = Case("counterError", theme);
        var form = new FormControl
        {
            Label = "Label", Caption = "Caption", Counter = new FormControlCounter { Length = 5, MaxLength = 4 }, Content = new Input(),
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { form } },
            theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        Assert.True(Draws(scene, (Visual)form.Part(FormControl.LabelPart)!, c.Label!));
        Assert.True(Draws(scene, (Visual)form.Part(FormControl.LabelEndEnhancerPart)!, c.LabelEndEnhancer!));
        Assert.True(Draws(scene, (Visual)form.Part(FormControl.CaptionMessagePart)!, c.Caption!));
        // And nothing of the caption's colour in the label's row: each in its own.
        Assert.False(Draws(scene, (Visual)form.Part(FormControl.LabelPart)!, c.Caption!));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;

        var dark = theme == "Dark";
        StackPanel Column() => new() { Width = 360, Margin = new Thickness(12, 12, 12, 12) };
        var left = Column();
        var right = Column();
        Func<FormControlState, object?> caption = _ => "caption";

        // form-control.scenario.tsx: the states round a compact input, a textarea, a checkbox and a radio group.
        left.Children.Add(new FormControl { Label = "Input label", Caption = "Input caption", Content = new Input { Size = InputSize.Compact } });
        left.Children.Add(new FormControl { IsEnabled = false, Label = "Input label disabled", Caption = "Input caption", Content = new Input { Size = InputSize.Compact } });
        left.Children.Add(new FormControl { Positive = true, Label = "Input label positive", Caption = "Input caption", Content = new Input { Size = InputSize.Compact } });
        left.Children.Add(new FormControl { Error = true, Label = "Input label error", Caption = "Input caption", Content = new Input { Size = InputSize.Compact } });
        left.Children.Add(new FormControl { Label = "Textarea label", Caption = "Textarea caption", Content = new Textarea { Size = InputSize.Compact } });
        left.Children.Add(new FormControl { Label = "Checkbox label", Caption = "Checkbox caption", Content = new Checkbox { Content = "Checkbox control" } });
        // form-control-with-counter.scenario.tsx, with a value typed; input-form-control-states.scenario.tsx.
        right.Children.Add(new FormControl { Label = "Enable counter", Caption = "Input caption", Counter = true, Content = new Input { Size = InputSize.Compact, Text = "Hello" } });
        right.Children.Add(new FormControl { Label = "Counter with max value", Caption = "Input caption", Counter = true, Content = new Input { Size = InputSize.Compact, MaxLength = 40, Text = "Hello" } });
        right.Children.Add(new FormControl { Label = "Override counter with max length", Caption = "Input caption", Counter = new FormControlCounter { Length = 120, MaxLength = 100 }, Content = new Input { Size = InputSize.Compact } });
        right.Children.Add(new FormControl { Label = "Positive", Caption = caption, Positive = "Success!", Content = new Input() });
        right.Children.Add(new FormControl { Label = "Error", Caption = caption, Error = "Error!", Content = new Input() });
        // The documentation's validation example, its error shown.
        right.Children.Add(new FormControl { Label = "Your email", Error = "Please input a valid email address", Content = new Input { Error = true, Text = "not an email" } });

        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { left, right } },
            variant, width: 780, height: 860);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"form-control-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
