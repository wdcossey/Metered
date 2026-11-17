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
/// Drawn pixels of what CheckboxMark draws: Base Web's tick, its SVG background image at its own 17 px over the whole
/// box, under a transparent border; an unchecked box's 2 px border round nothing; the 2 px focus outline 2 px out; the
/// hover tints of the container and the mark, one over the other. And, when METERED_FRAMES names a folder, a gallery of
/// Base Web's checkbox-v2 scenarios, light and dark, saved there for docs/controls/Checkbox.
/// </summary>
public class CheckboxFrames
{
    // A checkbox at (20, 20): its 33 px CheckmarkContainer there, the 17 px Checkmark centred in it at (28, 28) (the
    // oracle's sizes).
    private static Scene Show(Checkbox box) => new(new StackPanel { Margin = new Thickness(20), Children = { box } });

    // The colours are the oracle's, light; the positions follow from its sizes.
    private static CheckboxColourCase Case(bool isChecked, string state = "rest") =>
        CheckboxOracle.Value.Colours.Single(c => c is { Theme: "Light", Indeterminate: false, Error: false, Disabled: false } && c.Checked == isChecked && c.State == state);

    /// <summary>The window's own background, under everything: not Base Web's.</summary>
    private static readonly Color Page = Colors.White;

    [AvaloniaFact]
    public void The_tick_is_drawn_at_its_own_size_over_the_box()
    {
        // Light: contentPrimary #000000 under a transparent border; the tick contentInversePrimary #FFFFFF, its 24 px
        // viewBox drawn at 17 px from (28, 28), so a viewBox unit is 17/24 px.
        using var scene = Show(new Checkbox { IsChecked = true });
        var frame = scene.Capture();
        var fill = Color.Parse(Case(true).Background);
        var tick = Color.Parse(Case(true).Tick!);

        Assert.Equal(fill, Frames.PixelAt(frame, 28, 36)); // the fill reaches under the border
        Assert.Equal(tick, Frames.PixelAt(frame, 37, 36)); // the long stroke: (13.4, 12.0) in the viewBox
        Assert.Equal(tick, Frames.PixelAt(frame, 33, 37)); // the short stroke: (7.8, 13.4)
        Assert.Equal(fill, Frames.PixelAt(frame, 31, 31)); // above the short stroke: (4.9, 4.9)
        Assert.Equal(fill, Frames.PixelAt(frame, 40, 30)); // above the long stroke: (17.6, 3.5)
        Assert.Equal(fill, Frames.PixelAt(frame, 37, 41)); // under the long stroke: (13.4, 18.4)
    }

    [AvaloniaFact]
    public void An_unchecked_box_is_its_border_round_nothing()
    {
        // Light: contentTertiary #5E5E5E, 2 px; transparent inside, so the page shows.
        using var scene = Show(new Checkbox());
        var frame = scene.Capture();
        var border = Color.Parse(Case(false).Border);

        Assert.Equal(Page, Frames.PixelAt(frame, 27, 36));
        Assert.Equal(border, Frames.PixelAt(frame, 28, 36));
        Assert.Equal(border, Frames.PixelAt(frame, 29, 36));
        Assert.Equal(Page, Frames.PixelAt(frame, 30, 36));
        Assert.Equal(Page, Frames.PixelAt(frame, 36, 36));
    }

    [AvaloniaFact]
    public void Keyboard_focus_is_a_2_px_outline_2_px_outside_the_box()
    {
        // Light: outline: 2px solid brandBorderAccessible, outline-offset 2px: the box's edge at 28, the gap 26 to 28,
        // the line 24 to 26.
        var box = new Checkbox { IsChecked = true };
        using var scene = Show(box);
        scene.Key(Key.Tab);
        var frame = scene.Capture();
        var c = Case(true, "focusVisible");
        var ring = Color.Parse(c.Outline!.Colour);
        Assert.Equal((2, 2), (c.Outline.Thickness, c.Outline.Offset));

        Assert.Equal(Page, Frames.PixelAt(frame, 23, 36));
        Assert.Equal(ring, Frames.PixelAt(frame, 24, 36));
        Assert.Equal(ring, Frames.PixelAt(frame, 25, 36));
        Assert.Equal(Page, Frames.PixelAt(frame, 26, 36));
        Assert.Equal(Page, Frames.PixelAt(frame, 27, 36));
        Assert.Equal(Color.Parse(c.Background), Frames.PixelAt(frame, 28, 36));
        Assert.Equal(ring, Frames.PixelAt(frame, 36, 24)); // above
        Assert.Equal(ring, Frames.PixelAt(frame, 36, 47)); // below: the box ends at 45, the line 47 to 49
    }

    [AvaloniaFact]
    public void A_mouse_over_the_container_tints_it_and_the_mark_over_it()
    {
        // Light: hoverOverlayAlpha on the container (CSS's :hover) and on the unchecked mark ($isHovered), one over the
        // other inside the mark.
        var box = new Checkbox();
        using var scene = Show(box);
        Settle(box);
        scene.Hover((Visual)box.Part(Checkbox.CheckmarkContainerPart)!);
        var frame = scene.Capture();
        var c = Case(false, "hover");
        var container = Blend(Color.Parse(c.Container), Page);

        Assert.Equal(container, Frames.PixelAt(frame, 22, 36), Near);
        Assert.Equal(Blend(Color.Parse(c.Background), container), Frames.PixelAt(frame, 36, 36), Near);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;

        // checkbox-v2-states.scenario.tsx: a column 8 apart.
        var states = new StackPanel { Width = 300, Spacing = 8, Margin = new Thickness(12) };
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

        var hover = new Checkbox { Content = "hover" };
        var press = new Checkbox { Content = "press" };
        var focus = new Checkbox { Content = "keyboard focus", IsChecked = true };
        states.Children.Add(hover);
        states.Children.Add(press);
        states.Children.Add(focus);

        // checkbox-v2-placement.scenario.tsx, and checkbox-v2.scenario.tsx's long label in a 200 px box.
        var others = new StackPanel { Width = 380, Spacing = 16, Margin = new Thickness(12) };
        others.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new Checkbox { Content = "Label on the left", LabelPlacement = CheckboxLabelPlacement.Left },
                new Checkbox { Content = "Label on the right", LabelPlacement = CheckboxLabelPlacement.Right },
            },
        });
        others.Children.Add(new Checkbox { Content = "click me" });
        others.Children.Add(new Border
        {
            Width = 200,
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new Checkbox { Content = "This is a long text. This is a long text. This is a long text. This is a long text. This is a long text." },
        });

        var page = new StackPanel { Orientation = Orientation.Horizontal, Children = { states, others } };
        var variant = theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
        using var scene = new Scene(new Border { Background = Brushes.Transparent, Child = page }, variant, width: 720, height: 560);
        scene.Window.Background = variant == ThemeVariant.Dark ? Brushes.Black : Brushes.White;
        Settle(hover);
        Settle(press);
        // One pointer a frame: a hover, then a press; keyboard focus in both.
        focus.Focus(NavigationMethod.Tab);
        scene.Hover((Visual)hover.Part(Checkbox.CheckmarkContainerPart)!);
        var hoverFrame = scene.Capture();
        // A press takes focus, so the focus comes back after it.
        scene.Press((Visual)press.Part(Checkbox.CheckmarkContainerPart)!);
        focus.Focus(NavigationMethod.Tab);
        var frame = scene.Capture();

        Directory.CreateDirectory(folder);
        hoverFrame.Save(Path.Combine(folder, $"checkbox-gallery-{theme.ToLowerInvariant()}-hover.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
        frame.Save(Path.Combine(folder, $"checkbox-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }

    /// <summary>
    /// No transition on the mark, so a frame shows where its colours end: the headless clock doesn't run, and a frame
    /// taken just after a hover would catch the fill on its way.
    /// </summary>
    private static void Settle(Checkbox box) => ((CheckboxMark)box.Part(Checkbox.CheckmarkPart)!).Transitions = null;

    /// <summary>A colour over another, as source-over draws it.</summary>
    private static Color Blend(Color over, Color under)
    {
        var a = over.A / 255.0;
        byte Mix(byte o, byte u) => (byte)Math.Round(o * a + u * (1 - a));
        return Color.FromRgb(Mix(over.R, under.R), Mix(over.G, under.G), Mix(over.B, under.B));
    }

    /// <summary>Within one step of each channel: blending rounds.</summary>
    private static readonly IEqualityComparer<Color> Near = EqualityComparer<Color>.Create(
        (x, y) => Math.Abs(x.A - y.A) <= 1 && Math.Abs(x.R - y.R) <= 1 && Math.Abs(x.G - y.G) <= 1 && Math.Abs(x.B - y.B) <= 1);
}
