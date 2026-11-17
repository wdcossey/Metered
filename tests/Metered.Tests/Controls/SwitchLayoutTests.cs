using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A labelled switch for each size, label placement and pointer (fine, and coarse: a touch screen), on and off, enabled
/// and disabled: its parts' lengths and type against Base Web's switch style functions (SwitchOracle), and where layout
/// puts them, as a browser lays out Base Web's flex boxes: the track and the label in a row from the top, the knob at the
/// start of the track inside its border and padding, centred across it, moved along by its transform.
/// </summary>
public class SwitchLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SwitchOracle.Data.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = SwitchOracle.Data.Layout[index];
        var sw = new Switch
        {
            Content = "Label",
            Size = Enum.Parse<SwitchSize>(c.Size, ignoreCase: true),
            LabelPlacement = Enum.Parse<SwitchLabelPlacement>(c.LabelPlacement, ignoreCase: true),
            CoarsePointer = c.Coarse,
            IsChecked = c.Checked,
            IsEnabled = !c.Disabled,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { sw } });

        var root = (RadioRootPanel)sw.Part(Switch.RootPart)!;
        var track = (RadioMark)sw.Part(Switch.ToggleTrackPart)!;
        var toggle = (SwitchToggle)sw.Part(Switch.TogglePart)!;
        var label = (Border)sw.Part(Switch.LabelPart)!;

        // Root: Base Web's label element, a row from the top.
        Assert.Equal("row", c.RootDirection);
        Assert.Equal(Orientation.Horizontal, root.Orientation);
        Assert.Equal("flex-start", c.RootAlignItems);
        Assert.Equal(RadioRootAlignment.Start, root.AlignItems);
        Assert.Equal(new Thickness(c.RootPadding), root.Padding);

        // The track, the knob and the label.
        Assert.Equal(new Size(c.Track[0], c.Track[1]), track.Bounds.Size);
        Assert.Equal(new Thickness(c.TrackBorder), track.BorderThickness);
        Assert.Equal(new Thickness(c.TrackPadding[0], c.TrackPadding[1], c.TrackPadding[2], c.TrackPadding[3]), track.Padding);
        Assert.Equal(new CornerRadius(c.TrackRadius), track.CornerRadius);
        Assert.Equal(new Size(c.Toggle, c.Toggle), toggle.Bounds.Size);
        Assert.Equal(new CornerRadius(c.Toggle / 2), toggle.CornerRadius);
        Assert.Equal(new Thickness(c.LabelPadding[0], c.LabelPadding[1], c.LabelPadding[2], 0), label.Padding);
        var text = label.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(c.Label.FontSize, text.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(c.Label.FontWeight), text.FontWeight);
        Assert.Equal(c.Label.LineHeight, text.LineHeight);

        // Where they are: the label first for the left placement, both at the top, inside the root's padding.
        var pad = c.RootPadding;
        var t = track.Bounds.Size;
        var l = label.Bounds.Size;
        Assert.Equal(c.LabelPadding[1] + c.Label.LineHeight, l.Height);
        var left = c.LabelPlacement == "left";
        AssertNear(new Point(pad + (left ? l.Width : 0), pad), track.TranslatePoint(default, sw)!.Value);
        AssertNear(new Point(pad + (left ? 0 : t.Width), pad), label.TranslatePoint(default, sw)!.Value);
        Assert.Equal(new Size(2 * pad + t.Width + l.Width, 2 * pad + Math.Max(t.Height, l.Height)), sw.Bounds.Size);

        // The knob: at the start of the track's box inside its border and padding, centred across it (alignItems
        // center: it overflows both ways when larger, as the disabled, checked knob does), then translated.
        Assert.Equal("center", c.TrackAlignItems);
        var inner = c.Track[1] - 2 * c.TrackBorder - c.TrackPadding[1] - c.TrackPadding[3];
        AssertNear(new Point(c.TrackBorder + c.TrackPadding[0] + c.ToggleTranslate, c.TrackBorder + c.TrackPadding[1] + (inner - c.Toggle) / 2),
            toggle.TranslatePoint(default, track)!.Value);
    }

    // switch.scenario.tsx: a long label in a 200 px box wraps beside the track, which keeps its size.
    [AvaloniaFact]
    public void A_long_label_wraps_beside_the_track()
    {
        var words = string.Join(" ", Enumerable.Repeat("This is a long text.", 5));
        var sw = new Switch { Content = words };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { new Border { Width = 200, Child = sw } } });
        var c = SwitchOracle.Data.Layout.First(x => x.Size == "default" && !x.Coarse);
        var track = (Visual)sw.Part(Switch.ToggleTrackPart)!;
        var label = (Visual)sw.Part(Switch.LabelPart)!;
        Assert.Equal(new Size(c.Track[0], c.Track[1]), track.Bounds.Size);
        Assert.True(sw.Bounds.Width <= 200, $"{sw.Bounds.Width}");
        Assert.True(label.Bounds.Height >= c.LabelPadding[1] + 3 * c.Label.LineHeight, $"{label.Bounds.Height}");
        Assert.Equal(0, track.TranslatePoint(default, sw)!.Value.Y);
    }

    private static void AssertNear(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }
}
