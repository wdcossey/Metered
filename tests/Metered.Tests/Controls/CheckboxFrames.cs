using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of what CheckboxMark draws itself: Base Web's tick, an SVG background image
/// contained in the box inside its 3 px border; the 3 px outline keyboard focus gives a checked box;
/// the toggle's knob, at the track's start or moved by its width. And, when METERED_FRAMES names a
/// folder, a gallery of Base Web's checkbox scenarios, light and dark, saved there for docs/controls/Checkbox.
/// </summary>
public class CheckboxFrames
{
    // A box at (20, 20): its Checkmark, with its 2 px margin, is the 20 × 20 square at (22, 22) (the oracle's sizes).
    private static Scene Show(Checkbox box) => new(new StackPanel { Margin = new Thickness(20), Children = { box } });

    // The colours are the oracle's, light, at rest unless said; the positions follow from its sizes.
    private static CheckmarkCase Mark(bool isChecked, string state = "rest") =>
        CheckboxOracle.Value.Checkmark.Single(c => c is { Theme: "Light", Indeterminate: false, Error: false, Disabled: false } && c.Checked == isChecked && c.State == state);

    private static ToggleCase Knob(bool isChecked) =>
        CheckboxOracle.Value.Toggle.Single(c => c is { Theme: "Light", Error: false, Disabled: false, State: "rest" } && c.Checked == isChecked);

    /// <summary>The window's own background, under everything: not Base Web's.</summary>
    private static readonly Color Page = Colors.White;

    [AvaloniaFact]
    public void The_tick_is_drawn_inside_the_border_contained_and_centred()
    {
        // Light: tickFillSelected #000000 under a transparent border; the tick tickMarkFill #FFFFFF.
        // The 17 × 13 SVG contained in the 14 × 14 box inside the border: 14 × 10.71 at (25, 26.65).
        using var scene = Show(new Checkbox { IsChecked = true });
        var frame = scene.Capture();
        var fill = Color.Parse(Mark(true).Background);
        var tick = Color.Parse(Mark(true).Tick!);

        Assert.Equal(fill, Frames.PixelAt(frame, 22, 22)); // the fill reaches under the border
        Assert.Equal(tick, Frames.PixelAt(frame, 30, 35)); // the tick's foot: (6.5, 10.5) in the SVG
        Assert.Equal(tick, Frames.PixelAt(frame, 36, 29)); // its long stroke: (13.5, 3) in the SVG
        Assert.Equal(fill, Frames.PixelAt(frame, 34, 35)); // under the long stroke: (11, 10) in the SVG
        Assert.Equal(fill, Frames.PixelAt(frame, 30, 27)); // above the foot: (6, 0.5) in the SVG
        Assert.Equal(fill, Frames.PixelAt(frame, 39, 28)); // past the tip at (16.1, 3.1): the glyph is contained, not covering
    }

    [AvaloniaFact]
    public void An_unchecked_box_is_its_border_round_its_fill()
    {
        // Light: tickBorder #5E5E5E, 3 px, around tickFill #FFFFFF.
        using var scene = Show(new Checkbox());
        var frame = scene.Capture();
        var border = Color.Parse(Mark(false).Border);
        var fill = Color.Parse(Mark(false).Background);

        Assert.Equal(border, Frames.PixelAt(frame, 22, 32));
        Assert.Equal(border, Frames.PixelAt(frame, 24, 32));
        Assert.Equal(fill, Frames.PixelAt(frame, 25, 32));
        Assert.Equal(fill, Frames.PixelAt(frame, 32, 32));
    }

    [AvaloniaFact]
    public void Keyboard_focus_on_a_checked_box_is_a_3_px_outline_outside_it()
    {
        // Light: outline: 3px solid borderAccent #276EF1, square, on the box's outer edge.
        var box = new Checkbox { IsChecked = true };
        using var scene = Show(box);
        box.Focus(NavigationMethod.Tab);
        var frame = scene.Capture();
        var ring = Color.Parse(Mark(true, "focusVisible").Outline!);
        Assert.Equal(3, Mark(true, "focusVisible").OutlineWidth);

        Assert.NotEqual(ring, Frames.PixelAt(frame, 18, 32));
        Assert.Equal(ring, Frames.PixelAt(frame, 19, 32));
        Assert.Equal(ring, Frames.PixelAt(frame, 21, 32));
        Assert.Equal(Color.Parse(Mark(true, "focusVisible").Background), Frames.PixelAt(frame, 22, 32));
        Assert.Equal(ring, Frames.PixelAt(frame, 19, 19)); // a square corner
        Assert.Equal(ring, Frames.PixelAt(frame, 32, 43));
    }

    [AvaloniaTheory]
    [InlineData(false, 36, 56)]
    [InlineData(true, 56, 36)]
    public void The_knob_sits_at_the_tracks_start_or_one_width_along(bool isChecked, int knobX, int trackX)
    {
        // No label: the 40 × 14 track at (26, 26) after its 6 px margins, the 20 px knob centred across it.
        using var scene = Show(new Checkbox { CheckmarkType = CheckboxStyleType.Toggle, IsChecked = isChecked });
        var frame = scene.Capture();
        var knob = Color.Parse(Knob(isChecked).Background);

        Assert.Equal(knob, Frames.PixelAt(frame, knobX, 33));
        Assert.Equal(knob, Frames.PixelAt(frame, knobX, 24)); // above the track
        Assert.Equal(Color.Parse(Knob(isChecked).Track), Frames.PixelAt(frame, trackX, 33));
        Assert.Equal(Page, Frames.PixelAt(frame, trackX, 24)); // beside the knob, above the track
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;

        // checkbox-states.scenario.tsx.
        var states = new StackPanel { Width = 260, Margin = new Thickness(12) };
        foreach (var (label, check, indeterminate, disabled, error) in new[]
        {
            ("Checkbox", false, false, false, false), ("Checkbox checked", true, false, false, false),
            ("Checkbox isIndeterminate", false, true, false, false), ("Checkbox disabled", false, false, true, false),
            ("Checkbox disabled checked", true, false, true, false), ("Checkbox disabled isIndeterminate", false, true, true, false),
            ("Checkbox error", false, false, false, true), ("Checkbox error checked", true, false, false, true),
            ("Checkbox error isIndeterminate", false, true, false, true),
        })
        {
            states.Children.Add(new Checkbox { Content = label, IsChecked = check, IsIndeterminate = indeterminate, IsEnabled = !disabled, Error = error });
        }

        var hover = new Checkbox { Content = "hover", IsChecked = true };
        var press = new Checkbox { Content = "press" };
        var focus = new Checkbox { Content = "keyboard focus", IsChecked = true };
        states.Children.Add(hover);
        states.Children.Add(press);
        states.Children.Add(focus);

        // checkbox-toggle.scenario.tsx: a 200 px column, 12 px apart.
        var toggles = new StackPanel { Width = 200, Spacing = 12, Margin = new Thickness(12) };
        foreach (var (label, check, disabled, error) in new[]
        {
            ("default unchecked", false, false, false), ("default checked", true, false, false),
            ("disabled unchecked", false, true, false), ("disabled checked", true, true, false),
            ("error unchecked", false, false, true), ("error checked", true, false, true),
            ("long label that should wrap to multiple lines to test how the toggle aligns with the text", true, false, false),
        })
        {
            toggles.Children.Add(new Checkbox { Content = label, CheckmarkType = CheckboxStyleType.Toggle, IsChecked = check, IsEnabled = !disabled, Error = error });
        }

        var toggleFocus = new Checkbox { Content = "keyboard focus", CheckmarkType = CheckboxStyleType.Toggle };
        toggles.Children.Add(toggleFocus);

        // checkbox-placement.scenario.tsx.
        var placements = new StackPanel { Width = 380, Margin = new Thickness(12) };
        placements.Children.Add(new Checkbox { Content = "Label on the top", LabelPlacement = LabelPlacement.Top });
        placements.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(12, 8, 0, 8),
            Children =
            {
                new Checkbox { Content = "Label on the left", LabelPlacement = LabelPlacement.Left },
                new Checkbox { Content = "Label on the right", LabelPlacement = LabelPlacement.Right },
            },
        });
        placements.Children.Add(new Checkbox { Content = "Label on the bottom", LabelPlacement = LabelPlacement.Bottom });

        var page = new StackPanel { Orientation = Orientation.Horizontal, Children = { states, toggles, placements } };
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new Border { Background = Brushes.Transparent, Child = page }, variant, width: 920, height: 520);
        scene.Window.Background = variant == ThemeVariant.Dark ? Brushes.Black : Brushes.White;
        // One pointer and one focus a frame: the box's focus and a hover, then the toggle's focus and a press.
        focus.Focus(NavigationMethod.Tab);
        scene.Hover(hover.Part(Checkbox.CheckmarkPart) as Visual ?? hover);
        var hoverFrame = scene.Capture();
        // A press takes focus, so it comes first.
        scene.Press(press.Part(Checkbox.CheckmarkPart) as Visual ?? press);
        toggleFocus.Focus(NavigationMethod.Tab);
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        hoverFrame.Save(Path.Combine(folder, $"checkbox-gallery-{theme.ToLowerInvariant()}-hover.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        frame.Save(Path.Combine(folder, $"checkbox-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
