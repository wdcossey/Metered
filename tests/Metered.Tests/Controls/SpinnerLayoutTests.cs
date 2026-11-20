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
        var spinner = new Spinner { BorderWidth = c.BorderWidth.ToLength() };
        if (c.Size.Kind != "unset") spinner.Size = c.Size.ToLength();
        else spinner.ClearValue(Spinner.SizeProperty);
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
        var spinner = new Spinner();
        var holder = new Border { Width = 300, Child = spinner };
        using var scene = new Scene(new StackPanel { Children = { holder } });
        Assert.Equal(40, spinner.Bounds.Width);
        Assert.Equal(0, spinner.Bounds.X);
    }
}
