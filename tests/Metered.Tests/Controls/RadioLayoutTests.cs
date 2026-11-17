using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A radio with a label and a description, for each label placement, align and pointer (fine, and coarse: a touch
/// screen): its parts' lengths and type against Base Web's radio-v2 style functions (RadioOracle), and where layout
/// puts them, as a browser lays out Base Web's flex boxes.
/// </summary>
public class RadioLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, RadioOracle.Data.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = RadioOracle.Data.Layout[index];
        var radio = new Radio
        {
            Content = "Label",
            Description = "Description",
            Align = Enum.Parse<RadioAlign>(c.Align, ignoreCase: true),
            LabelPlacement = Enum.Parse<RadioLabelPlacement>(c.LabelPlacement, ignoreCase: true),
            CoarsePointer = c.Coarse,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });

        var root = (RadioRootPanel)radio.Part(Radio.RootPart)!;
        var backplate = (Border)radio.Part(Radio.RadioBackplatePart)!;
        var outer = (RadioMark)radio.Part(Radio.RadioMarkOuterPart)!;
        var wrapper = (StackPanel)radio.Part(Radio.LabelWrapperPart)!;
        var row = c.RootDirection == "row";
        var gap = (row ? c.RootColumnGap : c.RootRowGap) ?? 0;

        // Root: Base Web's label element.
        Assert.Equal(row ? Orientation.Horizontal : Orientation.Vertical, root.Orientation);
        Assert.Equal(c.RootAlignItems == "center" ? RadioRootAlignment.Center : RadioRootAlignment.Start, root.AlignItems);
        Assert.Equal(gap, root.Spacing);
        Assert.Equal(new Thickness(c.RootPadding), root.Padding);

        // The backplate and the mark.
        Assert.Equal(new Thickness(c.BackplatePadding[0], c.BackplatePadding[1], c.BackplatePadding[2], c.BackplatePadding[3]), backplate.Padding);
        Assert.Equal(new CornerRadius(c.BackplateRadius), backplate.CornerRadius);
        Assert.Equal(c.BackplateMin[0], backplate.MinWidth);
        Assert.Equal(c.BackplateMin[1], backplate.MinHeight);
        Assert.Equal(new Size(c.BackplateSize[0], c.BackplateSize[1]), backplate.Bounds.Size);
        Assert.Equal(new Size(c.OuterSize, c.OuterSize), outer.Bounds.Size);
        Assert.Equal(new Thickness(c.OuterBorder), outer.BorderThickness);

        // The label wrapper, the label and the description.
        Assert.Equal(c.WrapperRowGap, wrapper.Spacing);
        Assert.Equal(c.WrapperMaxWidth ?? double.PositiveInfinity, wrapper.MaxWidth);
        Assert.Equal(new Thickness(0, c.WrapperPaddingTop, 0, 0), wrapper.Margin);
        AssertText(c.Label, TextOf(radio.Part(Radio.LabelPart)!));
        AssertText(c.Description, (TextBlock)radio.Part(Radio.DescriptionPart)!);

        // Where they are: the backplate and the wrapper in a row or a column, in the order radio.js renders them,
        // a gap apart, aligned across, inside the padding (and the wrapper's top padding, here its margin).
        var pad = c.RootPadding;
        var b = backplate.Bounds.Size;
        var w = wrapper.Bounds.Size;
        var first = c.LabelPlacement is "top" or "left";
        var at = (Visual v) => v.TranslatePoint(default, radio)!.Value;
        Point plate, label;
        if (row)
        {
            plate = new Point(pad + (first ? w.Width + gap : 0), pad);
            label = new Point(pad + (first ? 0 : b.Width + gap), pad + c.WrapperPaddingTop);
            Assert.Equal(new Size(2 * pad + b.Width + gap + w.Width, 2 * pad + Math.Max(b.Height, w.Height + c.WrapperPaddingTop)), radio.Bounds.Size);
        }
        else
        {
            var cross = Math.Max(b.Width, w.Width);
            plate = new Point(pad + (cross - b.Width) / 2, pad + (first ? w.Height + gap : 0));
            label = new Point(pad + (cross - w.Width) / 2, pad + (first ? 0 : b.Height + gap));
            Assert.Equal(new Size(2 * pad + cross, 2 * pad + b.Height + gap + w.Height), radio.Bounds.Size);
        }

        AssertNear(plate, at(backplate));
        AssertNear(label, at(wrapper));
        // The mark is centred in the backplate.
        AssertNear(new Point((b.Width - c.OuterSize) / 2, (b.Height - c.OuterSize) / 2), outer.TranslatePoint(default, backplate)!.Value);
    }

    // Base Web's max3LinesStyle on a horizontal radio: three lines of the label, then an ellipsis; a vertical radio's
    // label wraps as long as it is.
    [AvaloniaTheory]
    [InlineData(RadioAlign.Horizontal)]
    [InlineData(RadioAlign.Vertical)]
    public void A_horizontal_radio_holds_its_label_to_three_lines(RadioAlign align)
    {
        var words = string.Join(" ", Enumerable.Repeat("A label long enough to wrap", 12));
        var radio = new Radio { Content = words, Description = words, Align = align };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { radio } });
        var label = TextOf(radio.Part(Radio.LabelPart)!);
        var description = (TextBlock)radio.Part(Radio.DescriptionPart)!;
        var c = RadioOracle.Data.Layout.First(l => l.Align == align.ToString().ToLowerInvariant());

        if (align == RadioAlign.Horizontal)
        {
            Assert.Equal(3 * c.Label.LineHeight, label.Bounds.Height, 3);
            Assert.Equal(3 * c.Description.LineHeight, description.Bounds.Height, 3);
            Assert.Equal(TextTrimming.CharacterEllipsis, label.TextTrimming);
            Assert.True(((Visual)radio.Part(Radio.LabelWrapperPart)!).Bounds.Width <= c.WrapperMaxWidth!.Value);
        }
        else
        {
            Assert.True(label.Bounds.Height > 3 * c.Label.LineHeight, $"{label.Bounds.Height}");
            Assert.True(description.Bounds.Height > 3 * c.Description.LineHeight, $"{description.Bounds.Height}");
        }
    }

    private static TextBlock TextOf(StyledElement label) => ((Visual)label).GetVisualDescendants().OfType<TextBlock>().Single();

    private static void AssertText(RadioText expected, TextBlock text)
    {
        Assert.Equal(expected.FontSize, text.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(expected.FontWeight), text.FontWeight);
        Assert.Equal(expected.LineHeight, text.LineHeight);
        Assert.Equal(expected.MaxLines, text.MaxLines);
    }

    private static void AssertNear(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }
}
