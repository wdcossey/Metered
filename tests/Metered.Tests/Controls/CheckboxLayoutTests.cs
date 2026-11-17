using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Where Base Web's Checkbox puts things (CheckboxOracle): the Root's direction and cross alignment
/// and the label's padding by placement, the label before the mark for top and left, the default
/// placement by checkmark type, and the sizes of the checkmark, the track, the knob and the label's type.
/// </summary>
public class CheckboxLayoutTests
{
    private static CheckboxSizes Sizes => CheckboxOracle.Value.Sizes;

    public static TheoryData<string, CheckboxStyleType> Placements
    {
        get
        {
            var data = new TheoryData<string, CheckboxStyleType>();
            foreach (var c in CheckboxOracle.Value.Layout)
            {
                data.Add(c.Placement, CheckboxStyleType.Default);
                data.Add(c.Placement, CheckboxStyleType.Toggle);
            }

            return data;
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Placements))]
    public void Placement_matches_Base_Web(string placement, CheckboxStyleType type)
    {
        var c = CheckboxOracle.Value.Layout.Single(l => l.Placement == placement);
        var box = new Checkbox { Content = "Label", CheckmarkType = type, LabelPlacement = Enum.Parse<LabelPlacement>(placement, ignoreCase: true) };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { box } });

        var root = (LabelPlacementPanel)box.Part(Checkbox.RootPart)!;
        var label = (ContentPresenter)box.Part(Checkbox.LabelPart)!;
        var mark = (Control)box.Part(type == CheckboxStyleType.Toggle ? Checkbox.ToggleTrackPart : Checkbox.CheckmarkPart)!;
        var other = (Control)box.Part(type == CheckboxStyleType.Toggle ? Checkbox.CheckmarkPart : Checkbox.ToggleTrackPart)!;
        Assert.False(other.IsVisible);

        Assert.Equal(c.Direction == "row" ? Orientation.Horizontal : Orientation.Vertical, root.Orientation);
        Assert.Equal(c.AlignItems switch { "flex-start" => AlignItems.Start, "center" => AlignItems.Center, _ => throw new InvalidOperationException(c.AlignItems) }, root.AlignItems);
        Assert.Equal(Thickness(c.LabelPadding), label.Padding);

        // The mark's box with its margin (its flex item), and the label's, in the Root.
        var m = type == CheckboxStyleType.Toggle ? Sizes.Track : Sizes.Checkmark;
        var slot = new Size(m.Width + m.Margin![0] + m.Margin[2], m.Height + m.Margin[1] + m.Margin[3]);
        var markAt = mark.Bounds.Position - new Point(m.Margin[0], m.Margin[1]);
        var labelAt = label.Bounds.Position;
        var row = c.Direction == "row";
        var labelFirst = placement is "top" or "left";
        if (row)
        {
            Assert.Equal(labelFirst ? label.Bounds.Width : 0, markAt.X, 3);
            Assert.Equal(labelFirst ? 0 : slot.Width, labelAt.X, 3);
            // flex-start: both at the top of the line.
            Assert.Equal(0, markAt.Y, 3);
            Assert.Equal(0, labelAt.Y, 3);
        }
        else
        {
            Assert.Equal(labelFirst ? label.Bounds.Height : 0, markAt.Y, 3);
            Assert.Equal(labelFirst ? 0 : slot.Height, labelAt.Y, 3);
            // center: each centred across the Root, which is as wide as the line it sits in (to the
            // half pixel layout rounding moves an odd width by).
            Assert.Equal(root.Bounds.Width / 2, markAt.X + slot.Width / 2, 0.5);
            Assert.Equal(root.Bounds.Width / 2, labelAt.X + label.Bounds.Width / 2, 0.5);
        }
    }

    [AvaloniaTheory]
    [InlineData(CheckboxStyleType.Default)]
    [InlineData(CheckboxStyleType.Toggle)]
    public void Unset_placement_is_Base_Webs_default_for_the_type(CheckboxStyleType type)
    {
        var box = new Checkbox { Content = "Label", CheckmarkType = type };
        using var scene = new Scene(box);

        var expected = CheckboxOracle.Value.DefaultPlacement[type == CheckboxStyleType.Toggle ? "toggle" : "default"];
        Assert.Equal(Enum.Parse<LabelPlacement>(expected, ignoreCase: true), box.ActualLabelPlacement);
        Assert.Contains(":label-" + expected, box.Part(Checkbox.LabelPart)!.Classes);
        Assert.Equal(expected == "left", ((LabelPlacementPanel)box.Part(Checkbox.RootPart)!).LabelFirst);

        box.LabelPlacement = LabelPlacement.Bottom;
        scene.Pump();
        Assert.Equal(LabelPlacement.Bottom, box.ActualLabelPlacement);
        Assert.DoesNotContain(":label-" + expected, box.Part(Checkbox.LabelPart)!.Classes);
    }

    [AvaloniaFact]
    public void Sizes_match_Base_Web()
    {
        var box = new Checkbox { Content = "Label" };
        var toggle = new Checkbox { Content = "Label", CheckmarkType = CheckboxStyleType.Toggle };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { box, toggle } });

        var mark = (CheckboxMark)box.Part(Checkbox.CheckmarkPart)!;
        Assert.Equal(new Size(Sizes.Checkmark.Width, Sizes.Checkmark.Height), mark.Bounds.Size);
        Assert.Equal(Thickness(Sizes.Checkmark.Margin!), mark.Margin);
        Assert.Equal(new Thickness(Sizes.Checkmark.BorderWidth), mark.BorderThickness);
        Assert.Equal(new CornerRadius(Sizes.Checkmark.Radius), mark.CornerRadius);

        var track = (Border)toggle.Part(Checkbox.ToggleTrackPart)!;
        Assert.Equal(new Size(Sizes.Track.Width, Sizes.Track.Height), track.Bounds.Size);
        Assert.Equal(Thickness(Sizes.Track.Margin!), track.Margin);
        Assert.Equal(new CornerRadius(Sizes.Track.Radius), track.CornerRadius);

        // The knob, centred across the track (its alignItems: center), at the track's start.
        var knob = (CheckboxMark)toggle.Part(Checkbox.TogglePart)!;
        Assert.Equal(new Size(Sizes.Toggle.Width, Sizes.Toggle.Height), knob.Bounds.Size);
        Assert.Equal(new CornerRadius(Sizes.Toggle.Radius), knob.CornerRadius);
        Assert.Equal(new Point(0, (Sizes.Track.Height - Sizes.Toggle.Height) / 2), knob.Bounds.Position);

        var label = (ContentPresenter)box.Part(Checkbox.LabelPart)!;
        Assert.Equal(Sizes.Label.FontSize, label.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(Sizes.Label.FontWeight), label.FontWeight);
        Assert.Equal(Sizes.Label.LineHeight, label.LineHeight);
        // One line: the row is the label's line, as tall as the checkmark with its margin.
        Assert.Equal(Sizes.Label.LineHeight, label.Bounds.Height, 3);
        Assert.Equal(Sizes.Checkmark.Height + Sizes.Checkmark.Margin![1] + Sizes.Checkmark.Margin[3], box.Bounds.Height, 3);
    }

    [AvaloniaFact]
    public void A_long_label_wraps_beside_the_mark_which_stays_at_the_top()
    {
        var box = new Checkbox
        {
            Content = "long label that should wrap to multiple lines to test how the toggle aligns with the text",
            CheckmarkType = CheckboxStyleType.Toggle,
            LabelPlacement = LabelPlacement.Right,
        };
        // checkbox-toggle.scenario.tsx: a 200 px column.
        using var scene = new Scene(new StackPanel { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Children = { box } });

        var label = (ContentPresenter)box.Part(Checkbox.LabelPart)!;
        var track = (Border)box.Part(Checkbox.ToggleTrackPart)!;
        var slot = Sizes.Track.Width + Sizes.Track.Margin![0] + Sizes.Track.Margin[2];
        Assert.Equal(slot, label.Bounds.X, 3);
        Assert.True(label.Bounds.Right <= 200.001, $"{label.Bounds.Right}");
        Assert.True(label.Bounds.Height >= 3 * Sizes.Label.LineHeight - 0.001, $"{label.Bounds.Height}");
        Assert.Equal(Sizes.Track.Margin[1], track.Bounds.Y, 3);
    }

    private static Thickness Thickness(double[] ltrb) => new(ltrb[0], ltrb[1], ltrb[2], ltrb[3]);
}
