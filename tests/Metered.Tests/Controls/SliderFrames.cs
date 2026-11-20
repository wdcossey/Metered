using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Slider = Metered.Controls.Slider;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of what the slider's own elements draw: react-range's track background (the selection and the rest,
/// with hard stops at the thumbs), the marks' gaps in it, the thumb's shadow, and its 3 px focus ring. And, when
/// METERED_FRAMES names a folder, a gallery of Base Web's slider scenarios and states, light and dark, saved there for
/// docs/controls/Slider.
/// </summary>
public class SliderFrames
{
    // A slider at (20, 80), 350 wide: its line 16 in (Track's padding), from x 36 to 354, rows 96 and 97; a thumb of 24 on
    // it centred on row 97 (the oracle's lengths).
    private static Scene Show(Slider slider) => new(new StackPanel { Margin = new Thickness(20, 80, 20, 20), Children = { slider } });

    private const int Line = 96;

    private static double X(double fraction) => 36 + 318 * fraction;

    private static SliderColourCase Case(double[] value, string state = "rest") =>
        SliderOracle.Data.Colours.Single(c => c.Theme == "Light" && c.Value.SequenceEqual(value) && c.State == state);

    /// <summary>The window's own background, under everything: not Base Web's.</summary>
    private static readonly Color Page = Colors.White;

    [AvaloniaFact]
    public void One_thumb_selects_from_the_start_to_it()
    {
        using var scene = Show(new Slider { Value = [50] });
        var frame = scene.Capture();
        var c = Case([30]);
        var selection = Color.Parse(c.InnerTrack[0].Colour);
        var rest = Color.Parse(c.InnerTrack[1].Colour);
        Assert.Equal(Page, Frames.PixelAt(frame, 30, Line));
        Assert.Equal(selection, Frames.PixelAt(frame, 40, Line));
        Assert.Equal(selection, Frames.PixelAt(frame, (int)X(0.4), Line + 1));
        Assert.Equal(rest, Frames.PixelAt(frame, (int)X(0.6), Line));
        Assert.Equal(rest, Frames.PixelAt(frame, 350, Line + 1));
        Assert.Equal(Page, Frames.PixelAt(frame, (int)X(0.6), Line - 1));
        Assert.Equal(Page, Frames.PixelAt(frame, (int)X(0.6), Line + 2));
    }

    [AvaloniaFact]
    public void Two_thumbs_select_between_them()
    {
        using var scene = Show(new Slider { Value = [20, 70] });
        var frame = scene.Capture();
        var c = Case([20, 70]);
        Assert.Equal(Color.Parse(c.InnerTrack[0].Colour), Frames.PixelAt(frame, (int)X(0.1), Line));
        Assert.Equal(Color.Parse(c.InnerTrack[1].Colour), Frames.PixelAt(frame, (int)X(0.45), Line));
        Assert.Equal(Color.Parse(c.InnerTrack[2].Colour), Frames.PixelAt(frame, (int)X(0.85), Line));
    }

    // calculateMarkOffsets: a 4 px gap at each tenth of the line's 318 (truncated), the second at 30 to 34.
    [AvaloniaFact]
    public void Marks_are_gaps_in_the_line()
    {
        using var scene = Show(new Slider { Value = [100], Step = 10, Marks = true });
        var frame = scene.Capture();
        var c = Case([30]);
        var selection = Color.Parse(c.InnerTrack[0].Colour);
        Assert.Equal(selection, Frames.PixelAt(frame, 36 + 29, Line));
        Assert.Equal(Color.Parse(c.Mark), Frames.PixelAt(frame, 36 + 30, Line));
        Assert.Equal(Color.Parse(c.Mark), Frames.PixelAt(frame, 36 + 33, Line + 1));
        Assert.Equal(selection, Frames.PixelAt(frame, 36 + 34, Line));
    }

    // The thumb's fill, and under it Base Web's 0 1px 4px shadow at 12 %, which darkens the page just below it.
    [AvaloniaFact]
    public void The_thumb_is_round_over_its_shadow()
    {
        using var scene = Show(new Slider { Value = [50] });
        var frame = scene.Capture();
        var c = Case([30]);
        var centre = (int)X(0.5);
        Assert.Equal(Color.Parse(c.Thumb), Frames.PixelAt(frame, centre, 97));
        Assert.Equal(Color.Parse(c.Thumb), Frames.PixelAt(frame, centre, 86));
        // The corner of its box is outside the circle: the page, a shade darker for the shadow, not the fill.
        var corner = Frames.PixelAt(frame, centre - 11, 86);
        Assert.NotEqual(Color.Parse(c.Thumb), corner);
        var below = Frames.PixelAt(frame, centre, 110);
        Assert.True(below.R < Page.R && below.R > 200, $"under the thumb: {below}");
        Assert.Equal(Page, Frames.PixelAt(frame, centre, 118));
    }

    // Base Web's 0 0 0 3px borderAccent: a ring from 12 to 15 out from the thumb's centre, in place of the shadow.
    [AvaloniaFact]
    public void Keyboard_focus_rings_the_thumb()
    {
        using var scene = Show(new Slider { Value = [50] });
        scene.Key(Key.Tab);
        var frame = scene.Capture();
        var c = Case([30], "focusVisible");
        var ring = Color.Parse(c.Ring!.Colour);
        var centre = (int)Math.Round(X(0.5));
        Assert.Equal(Color.Parse(c.Thumb), Frames.PixelAt(frame, centre, 97 - 11));
        Assert.Equal(ring, Frames.PixelAt(frame, centre, 97 - 14));
        Assert.Equal(ring, Frames.PixelAt(frame, centre, 97 + 13));
        Assert.Equal(ring, Frames.PixelAt(frame, centre + 13, 97));
        Assert.Equal(Page, Frames.PixelAt(frame, centre, 97 - 17));
        Assert.Equal(Page, Frames.PixelAt(frame, centre, 97 + 17));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;

        var hover = new Slider { Value = [30] };
        var drag = new Slider { Value = [60] };
        var focus = new Slider { Value = [45] };
        var column = new StackPanel { Width = 360, Spacing = 44, Margin = new Thickness(12, 64, 12, 12) };
        void Add(string label, Slider slider)
        {
            column.Children.Add(new TextBlock { Text = label, FontSize = 12, Foreground = theme == "Dark" ? Brushes.White : Brushes.Black });
            column.Children.Add(slider);
        }

        // slider.scenario.tsx and its siblings: basic, range, marks, step, disabled, always show label, custom label.
        Add("rest", new Slider { Value = [50] });
        Add("hover (a mouse over the thumb)", hover);
        Add("dragged (a finger on the thumb)", drag);
        Add("keyboard focus", focus);
        Add("range", new Slider { Value = [25, 60] });
        Add("marks, step 10", new Slider { Value = [20], Step = 10, Marks = true });
        Add("step 5, -300 to 300", new Slider { Value = [0], Step = 5, Min = -300, Max = 300 });
        Add("disabled", new Slider { Value = [50], IsEnabled = false });
        var right = new StackPanel { Width = 360, Spacing = 24, Margin = new Thickness(12, 64, 12, 12) };
        right.Children.Add(new TextBlock { Text = "always show label", FontSize = 12, Foreground = theme == "Dark" ? Brushes.White : Brushes.Black });
        right.Children.Add(new Slider { Value = [10, 70], Max = 95, PersistentThumb = true, ValueToLabel = Time, Margin = new Thickness(0, 56, 0, 0) });
        right.Children.Add(new Slider { Value = [50], PersistentThumb = true, IsEnabled = false, Margin = new Thickness(0, 56, 0, 0) });
        right.Children.Add(new Slider { Value = [20], Step = 10, Marks = true, PersistentThumb = true, Margin = new Thickness(0, 56, 0, 0) });
        right.Children.Add(new Slider { Value = [25, 60], PersistentThumb = true, Margin = new Thickness(0, 56, 0, 0) });

        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Children = { column, right } },
            variant, width: 780, height: 1420);
        scene.Window.Background = variant == ThemeVariant.Dark ? Brushes.Black : Brushes.White;
        // A finger's press focuses its thumb, so the keyboard's focus comes after it, and the mouse last.
        using var touch = scene.Window.TouchBegin(scene.CentreOf(drag.Thumbs[0]), RawInputModifiers.None);
        scene.Pump();
        focus.Thumbs[0].Focus(NavigationMethod.Tab);
        scene.Hover(hover.Thumbs[0]);
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"slider-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    /// <summary>slider-always-show-label.scenario.tsx's valueToLabel: quarter hours as a time of day.</summary>
    private static object Time(double value)
    {
        var hour = Math.Floor((value > 52 ? value - 48 : value) / 4);
        var minute = (value % 4 * 15).ToString("00", System.Globalization.CultureInfo.InvariantCulture);
        var meridiem = value > 95 / 2.0 ? "PM" : "AM";
        return $"{(hour == 0 ? "12" : hour.ToString(System.Globalization.CultureInfo.InvariantCulture))}:{minute}  {meridiem}";
    }
}
