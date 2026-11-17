using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A checkbox with a label, for each label placement and pointer (fine, and coarse: a touch screen): its parts'
/// lengths and type against Base Web's checkbox-v2 style functions (CheckboxOracle), and where layout puts them, as a
/// browser lays out Base Web's flex boxes; and a long label wrapping beside the mark.
/// </summary>
public class CheckboxLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, CheckboxOracle.Value.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = CheckboxOracle.Value.Layout[index];
        var box = new Checkbox
        {
            Content = "Label",
            LabelPlacement = Enum.Parse<CheckboxLabelPlacement>(c.LabelPlacement, ignoreCase: true),
            CoarsePointer = c.Coarse,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { box } });

        var root = (RadioRootPanel)box.Part(Checkbox.RootPart)!;
        var container = (Border)box.Part(Checkbox.CheckmarkContainerPart)!;
        var mark = (CheckboxMark)box.Part(Checkbox.CheckmarkPart)!;
        var label = (ContentPresenter)box.Part(Checkbox.LabelPart)!;

        // Root: Base Web's label element, a row.
        Assert.Equal("row", c.RootDirection);
        Assert.Equal(Orientation.Horizontal, root.Orientation);
        Assert.Equal(c.RootAlignItems == "center" ? RadioRootAlignment.Center : RadioRootAlignment.Start, root.AlignItems);
        Assert.Equal(new Thickness(c.RootPadding), root.Padding);

        // The container and the mark.
        Assert.Equal(Thickness(c.ContainerPadding), container.Padding);
        Assert.Equal(new CornerRadius(c.ContainerRadius), container.CornerRadius);
        Assert.Equal(c.ContainerMin[0], container.MinWidth);
        Assert.Equal(c.ContainerMin[1], container.MinHeight);
        Assert.Equal(new Size(c.ContainerSize[0], c.ContainerSize[1]), container.Bounds.Size);
        Assert.Equal(new Size(c.MarkSize[0], c.MarkSize[1]), mark.Bounds.Size);
        Assert.Equal(new Thickness(c.MarkBorder), mark.BorderThickness);
        Assert.Equal(new CornerRadius(c.MarkRadius), mark.CornerRadius);

        // The label.
        Assert.Equal(Thickness(c.LabelPadding), label.Padding);
        Assert.Equal(c.Label.FontSize, label.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(c.Label.FontWeight), label.FontWeight);
        Assert.Equal(c.Label.LineHeight, label.LineHeight);

        // Where they are: the container and the label in a row, in the order checkbox.js renders them, at the top of
        // the line (alignItems flex-start), inside the root's padding; the mark centred in the container.
        var pad = c.RootPadding;
        var b = container.Bounds.Size;
        var l = label.Bounds.Size;
        var first = c.LabelPlacement == "left";
        var at = (Visual v) => v.TranslatePoint(default, box)!.Value;
        AssertNear(new Point(pad + (first ? l.Width : 0), pad), at(container));
        AssertNear(new Point(pad + (first ? 0 : b.Width), pad), at(label));
        Assert.Equal(new Size(2 * pad + b.Width + l.Width, 2 * pad + Math.Max(b.Height, l.Height)), box.Bounds.Size);
        AssertNear(new Point((b.Width - c.MarkSize[0]) / 2, (b.Height - c.MarkSize[1]) / 2), mark.TranslatePoint(default, container)!.Value);
        // One line: the label's padding and its line.
        Assert.Equal(c.LabelPadding[1] + c.LabelPadding[3] + c.Label.LineHeight, l.Height, 3);
    }

    // checkbox-v2.scenario.tsx: a long label in a 200 px column wraps beside the mark, which stays at the top.
    [AvaloniaFact]
    public void A_long_label_wraps_beside_the_mark_which_stays_at_the_top()
    {
        var box = new Checkbox
        {
            Content = "This is a long text. This is a long text. This is a long text. This is a long text. This is a long text.",
        };
        using var scene = new Scene(new StackPanel { Width = 200, HorizontalAlignment = HorizontalAlignment.Left, Children = { box } });
        var c = CheckboxOracle.Value.Layout.Single(l => l is { LabelPlacement: "right", Coarse: false });

        var label = (ContentPresenter)box.Part(Checkbox.LabelPart)!;
        var container = (Border)box.Part(Checkbox.CheckmarkContainerPart)!;
        Assert.Equal(c.ContainerSize[0], label.Bounds.X, 3);
        Assert.True(label.Bounds.Right <= 200.001, $"{label.Bounds.Right}");
        Assert.True(label.Bounds.Height >= c.LabelPadding[1] + 3 * c.Label.LineHeight - 0.001, $"{label.Bounds.Height}");
        Assert.Equal(0, container.Bounds.Y, 3);
    }

    // checkbox-v2-unlabeled.scenario.tsx: with no label, the checkbox is its container.
    [AvaloniaFact]
    public void With_no_label_the_checkbox_is_its_container()
    {
        var box = new Checkbox();
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { box } });
        var c = CheckboxOracle.Value.Layout.Single(l => l is { LabelPlacement: "right", Coarse: false });

        Assert.False(((Visual)box.Part(Checkbox.LabelPart)!).IsVisible);
        Assert.Equal(new Size(c.ContainerSize[0], c.ContainerSize[1]), box.Bounds.Size);
    }

    private static Thickness Thickness(double[] ltrb) => new(ltrb[0], ltrb[1], ltrb[2], ltrb[3]);

    private static void AssertNear(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }
}
