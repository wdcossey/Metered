using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Metered.Controls;
using Spinner = Metered.Controls.Spinner;

namespace Metered.Tests.Controls;

/// <summary>
/// A spinner by <c>$size</c> and <c>$borderWidth</c> (the scenario's values among them: names, sizing tokens, lengths,
/// numbers, zero, negatives, text that is none of these, and unset): the ring's box and border against Base Web's
/// spinner style function (SpinnerOracle), and the size layout gives it, the border outside the box as CSS's content
/// box puts it.
/// </summary>
public class SpinnerLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SpinnerOracle.Data.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Box_and_border_match_Base_Web(int index)
    {
        var c = SpinnerOracle.Data.Layout[index];
        // Unset is given as unset, Base Web's undefined, which its default parameter makes medium.
        var spinner = new Spinner { Size = c.Size.ToLength(), BorderWidth = c.BorderWidth.ToLength() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { spinner } });
        var ring = SpinnerColourTests.Ring(spinner);

        Assert.Equal(c.Border, ring.Thickness);
        if (c.Box is { } box)
        {
            Assert.Equal(box, ring.BoxSize);
            var side = box + 2 * c.Border;
            Assert.Equal(new Size(side, side), ring.Bounds.Size);
            Assert.Equal(new Size(side, side), spinner.Bounds.Size);
        }
        else
        {
            // A box a browser drops (NaNpx, or a negative length): Metered draws no ring (docs/controls/Spinner, difference 3).
            Assert.True(double.IsNaN(ring.BoxSize), $"a box of {ring.BoxSize}");
            Assert.Equal(new Size(0, 0), ring.Bounds.Size);
        }
    }

    // display: block, with a width of its own: at the start of its line, as wide as the ring, in a parent that stretches.
    [AvaloniaFact]
    public void It_sits_at_the_start_of_its_line_as_wide_as_its_ring()
    {
        Assert.Equal("block", SpinnerOracle.Data.Display);
        var medium = SpinnerOracle.Data.Layout.Single(c => c.Size.Kind == "unset" && c.BorderWidth.Kind == "unset");
        var side = medium.Box!.Value + 2 * medium.Border;
        var spinner = new Spinner();
        var stretched = new Spinner { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        using var scene = new Scene(new StackPanel { Width = 300, Children = { spinner, stretched } });
        Assert.Equal(side, spinner.Bounds.Width);
        Assert.Equal(0, spinner.Bounds.X);
        // Stretched by the app, the spinner's box is wider, but the ring stays at its start, at its own size.
        var ring = SpinnerColourTests.Ring(stretched);
        Assert.Equal(300, stretched.Bounds.Width);
        Assert.Equal(new Rect(0, 0, side, side), ring.Bounds);
    }
}
