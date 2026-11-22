using Avalonia;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The anchoring (<see cref="Tether"/>) against popper.js itself: for each of tether.g.json's cases, the placement it
/// chose, the body's offset and the arrow's, from the margin Base Web's body has before popper.js runs; and on popper.js's
/// next updates, with the margin at the placement chosen, until it holds.
/// </summary>
public class PopoverTetherTests
{
    public static TheoryData<int> Cases() => new(Enumerable.Range(0, PopoverOracle.Value.Tether.Cases.Length));

    [Theory]
    [MemberData(nameof(Cases))]
    public void Placed_as_popper_js_places_it(int index)
    {
        var c = PopoverOracle.Value.Tether.Cases[index];
        var view = PopoverOracle.Value.Tether.View;
        // The margin before popper.js runs: Base Web's $placement is the one asked for, and auto has none.
        var margin = c.Placement == "auto" ? default : PopoverOracle.Margin(c.Placement, c.ShowArrow, c.PopoverMargin);
        var result = Tether.Compute(
            new Rect(c.X, c.Y, c.Width, c.Height), new Size(c.BodyWidth, c.BodyHeight), margin,
            c.ShowArrow ? PopoverOracle.Value.Tether.ArrowWidth : null, PopoverOracle.Placement(c.Placement),
            new Rect(0, 0, view.Width, view.Height), preventOverflow: !c.IgnoreBoundary);

        Assert.True(
            PopoverOracle.Placement(c.Result) == result.Placement && new Point(c.Left, c.Top) == result.Offset
            && (c.ArrowLeft is null ? result.ArrowOffset is null : result.ArrowOffset == new Point(c.ArrowLeft.Value, c.ArrowTop!.Value)),
            $"{c}: popper.js {c.Result} ({c.Left}, {c.Top}) arrow ({c.ArrowLeft}, {c.ArrowTop}); Metered {result.Placement} {result.Offset} arrow {result.ArrowOffset}");

        // The next updates: the margin Base Web's Body has at the placement chosen, until the placement holds.
        for (var runs = 0; runs < 4; runs++)
        {
            var name = char.ToLowerInvariant(result.Placement.ToString()[0]) + result.Placement.ToString()[1..];
            var again = Tether.Compute(
                new Rect(c.X, c.Y, c.Width, c.Height), new Size(c.BodyWidth, c.BodyHeight), PopoverOracle.Margin(name, c.ShowArrow, c.PopoverMargin),
                c.ShowArrow ? PopoverOracle.Value.Tether.ArrowWidth : null, PopoverOracle.Placement(c.Placement),
                new Rect(0, 0, view.Width, view.Height), preventOverflow: !c.IgnoreBoundary);
            var held = again.Placement == result.Placement;
            result = again;
            if (held) break;
        }

        Assert.True(
            PopoverOracle.Placement(c.SteadyResult) == result.Placement && new Point(c.SteadyLeft, c.SteadyTop) == result.Offset
            && (c.SteadyArrowLeft is null ? result.ArrowOffset is null : result.ArrowOffset == new Point(c.SteadyArrowLeft.Value, c.SteadyArrowTop!.Value)),
            $"{c}, next update: popper.js {c.SteadyResult} ({c.SteadyLeft}, {c.SteadyTop}); Metered {result.Placement} {result.Offset} arrow {result.ArrowOffset}");
    }
}
