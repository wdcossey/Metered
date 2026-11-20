using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Metered.Controls;
using ProgressBar = Metered.Controls.ProgressBar;

namespace Metered.Tests.Controls;

/// <summary>
/// A labelled bar for each size, of one step and of three, and an infinite bar of each size: the parts' lengths and type
/// against Base Web's style functions (ProgressBarOracle), and where layout puts them, as a browser lays out Base Web's
/// flex boxes: the bars in a row across the whole width, an equal share each after their margins (<c>flex: 1</c>), the
/// label under them past the root's gap, centred. Avalonia rounds each bar's bounds to whole pixels (layout rounding,
/// at a render scaling of 1), where a browser keeps the fraction: a bar is within a pixel of its share, and the bars and
/// their margins still fill the row exactly.
/// </summary>
public class ProgressBarLayoutTests
{
    private const double Width = 350;

    public static TheoryData<int> Cases => new(Enumerable.Range(0, ProgressBarOracle.Data.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = ProgressBarOracle.Data.Layout[index];
        var size = Enum.Parse<ProgressBarSize>(c.Size, ignoreCase: true);
        var pb = new ProgressBar { Size = size, Steps = c.Steps, Value = 40, ShowLabel = true };
        var infinite = new ProgressBar { Size = size, Infinite = true };
        using var scene = new Scene(new StackPanel { Width = Width, Spacing = 20, Children = { pb, infinite } });

        // Root: a column, the bars and then the label, the root's gap between.
        var root = (StackPanel)pb.Part(ProgressBar.RootPart)!;
        Assert.Equal(Orientation.Vertical, root.Orientation);
        Assert.Equal(c.RootGap, root.Spacing);
        Assert.Equal(Width, pb.Bounds.Width);

        // The bars: Base Web's height and corners, the margin between steps, and an equal share of the row each.
        var bars = pb.Elements(ProgressBar.BarPart).Cast<Border>().ToList();
        Assert.Equal(c.Bars.Length, bars.Count);
        var margins = c.Bars.Sum(b => b.MarginLeft);
        var share = (Width - margins) / bars.Count;
        double x = 0;
        for (var i = 0; i < bars.Count; i++)
        {
            var expected = c.Bars[i];
            var bar = bars[i];
            Assert.Equal(expected.Height, bar.Bounds.Height);
            Assert.Equal(Corners(expected.Radii), bar.CornerRadius);
            Assert.Equal(new Thickness(expected.MarginLeft, 0, 0, 0), bar.Margin);
            Assert.True(bar.ClipToBounds);
            x += expected.MarginLeft;
            var at = bar.TranslatePoint(default, pb)!.Value;
            Assert.InRange(at.X, x - 1, x + 1);
            Assert.Equal(0, at.Y);
            Assert.InRange(bar.Bounds.Width, share - 1, share + 1);
            x += share;

            if (i == bars.Count - 1) Assert.Equal(Width, at.X + bar.Bounds.Width, 6);

            // The fill: the bar's whole box, its own corners.
            var fill = (ProgressBarFill)bar.Child!;
            Assert.Equal(bar.Bounds.Size, fill.Bounds.Size);
            Assert.Equal(Corners(c.BarProgressRadii), fill.CornerRadius);
        }

        // The label: under the bars past the gap, the whole width, centred inside Base Web's padding, its type.
        var label = (TextBlock)pb.Part(ProgressBar.LabelPart)!;
        Assert.Equal("center", c.LabelAlign);
        Assert.Equal(TextAlignment.Center, label.TextAlignment);
        Assert.Equal(new Thickness(c.LabelPadding[0], 0, c.LabelPadding[1], 0), label.Padding);
        Assert.Equal(c.Label.FontSize, label.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(c.Label.FontWeight), label.FontWeight);
        Assert.Equal(c.Label.LineHeight, label.LineHeight);
        AssertNear(new Point(0, c.Bars[0].Height + c.RootGap), label.TranslatePoint(default, pb)!.Value);
        Assert.Equal(Width, label.Bounds.Width);
        Assert.Equal(c.Label.LineHeight, label.Bounds.Height);
        Assert.Equal(c.Bars[0].Height + c.RootGap + c.Label.LineHeight, pb.Bounds.Height);

        // The infinite bar: two halves side by side, each half the width, the outer corners rounded; no label, no gap.
        var halves = infinite.Elements(ProgressBar.InfiniteBarPart).Cast<ProgressBarInfiniteBar>().ToList();
        Assert.Equal(c.InfiniteBars.Length, halves.Count);
        for (var i = 0; i < halves.Count; i++)
        {
            Assert.Equal(c.InfiniteBars[i].Height, halves[i].Bounds.Height);
            Assert.Equal(Corners(c.InfiniteBars[i].Radii), halves[i].CornerRadius);
            Assert.InRange(halves[i].Bounds.Width, Width / 2 - 1, Width / 2 + 1);
            Assert.Equal(i == 0 ? 0 : halves[0].Bounds.Width, halves[i].TranslatePoint(default, infinite)!.Value.X, 6);
        }

        Assert.Equal(c.InfiniteBars[0].Height, infinite.Bounds.Height);
    }

    // The bars share the row whatever its width: a phone's, narrowed, and too narrow for their margins, which overflow it.
    [AvaloniaTheory]
    [InlineData(390.0)]
    [InlineData(100.0)]
    [InlineData(10.0)]
    public void The_steps_share_the_row_equally_at_any_width(double width)
    {
        var pb = new ProgressBar { Steps = 5, Value = 50 };
        using var scene = new Scene(new StackPanel { Width = width, Children = { pb } });
        var bars = pb.Elements(ProgressBar.BarPart).Cast<Control>().ToList();
        var margin = ProgressBarOracle.Data.Layout.First(c => c.Steps > 1).Bars[1].MarginLeft;
        var share = Math.Max(0, (width - 4 * margin) / 5);
        Assert.All(bars, bar => Assert.InRange(bar.Bounds.Width, share - 1, share + 1));
        Assert.Equal(Math.Max(width, 4 * margin), bars.Sum(bar => bar.Bounds.Width) + 4 * margin, 6);
    }

    // CSS takes a negative margin, which an override may give a bar: the bars overlap. Avalonia takes the negative slot
    // that leaves without complaint (a clamp review pass 2 asked for changed nothing) and lays out nothing narrower than
    // nothing.
    [AvaloniaFact]
    public void A_negative_margin_overlaps_the_bars()
    {
        var style = new Avalonia.Styling.ControlTheme(typeof(Border));
        style.Setters.Add(new Avalonia.Styling.Setter(Layoutable.MarginProperty, new Thickness(-200, 0, 0, 0)));
        var pb = new ProgressBar { Steps = 3, Value = 50, Overrides = [new Override { Part = ProgressBar.BarPart, Style = style }] };
        using var scene = new Scene(new StackPanel { Width = 100, Children = { pb } });
        var bars = pb.Elements(ProgressBar.BarPart).Cast<Control>().ToList();
        Assert.Equal(3, bars.Count);
        Assert.All(bars, bar => Assert.True(bar.Bounds.Width >= 0));
    }

    private static CornerRadius Corners(double[] r) => new(r[0], r[1], r[2], r[3]);

    private static void AssertNear(Point expected, Point actual)
    {
        Assert.Equal(expected.X, actual.X, 3);
        Assert.Equal(expected.Y, actual.Y, 3);
    }
}
