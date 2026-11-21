using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Where the popover's body and arrow are drawn, in a 390 × 844 window as tether.g.json's page: popper.js's placement and
/// offsets (PopoverOracle's tether cases, popper.js itself) with the margin Base Web's Body has at the placement chosen,
/// and the arrow where getArrowPositionStyles puts it; Body's margin and its entrance's start by placement, arrow and
/// popoverMargin (getPopoverMarginStyles, getStartPosition).
/// </summary>
public class PopoverLayoutTests
{
    /// <summary>The cases drawn: the anchors in the middle, in a corner it must flip from, and at a fractional place.</summary>
    public static TheoryData<int> Drawn()
    {
        var data = new TheoryData<int>();
        var cases = PopoverOracle.Value.Tether.Cases;
        for (var i = 0; i < cases.Length; i++)
        {
            var c = cases[i];
            if (c.BodyWidth == 120 && c.BodyHeight == 60 && c.Anchor is "middle" or "bottomRight" or "fractional" or "right") data.Add(i);
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Drawn))]
    public void The_body_and_arrow_are_drawn_where_popper_js_and_Base_Web_put_them(int index)
    {
        var c = PopoverOracle.Value.Tether.Cases[index];
        var (popover, anchor, _) = PopoverTests.Fixed(c.X, c.Y, c.Width, c.Height, c.BodyWidth, c.BodyHeight);
        popover.Placement = PopoverOracle.Placement(c.Placement);
        popover.ShowArrow = c.ShowArrow;
        popover.PopoverMargin = c.PopoverMargin;
        popover.IgnoreBoundary = c.IgnoreBoundary;
        using var scene = PopoverTests.Show(popover);
        // A fractional anchor, as a browser lays it out: no rounding to whole pixels (Avalonia's layout rounding).
        foreach (var l in popover.GetSelfAndVisualDescendants().OfType<Layoutable>()) l.UseLayoutRounding = false;
        popover.InvalidateMeasure();
        popover.IsOpen = true;
        PopoverTests.Settle(scene, 180);

        Assert.Equal(new Rect(c.X, c.Y, c.Width, c.Height), PopoverTests.BoxOf(anchor, scene));
        Assert.Equal(PopoverOracle.Placement(c.Result), popover.ActualPlacement);
        var margin = PopoverOracle.Margin(c.Result, c.ShowArrow, c.PopoverMargin);
        var body = (Visual)popover.Part(Popover.BodyPart)!;
        var box = PopoverTests.BoxOf(body, scene);
        Assert.Equal(new Rect(c.Left + margin.Left, c.Top + margin.Top, c.BodyWidth, c.BodyHeight), box);

        var arrow = (Control)popover.Part(Popover.ArrowPart)!;
        Assert.Equal(c.ShowArrow, arrow.IsEffectivelyVisible);
        if (!c.ShowArrow) return;
        // getArrowPositionStyles: popper.js's offset on the side along the placement's side, ARROW_SIZE - 2 beyond the other.
        var at = PopoverOracle.Value.Arrow.Cases.Single(a => a.Placement == c.Result);
        var side = c.Result.StartsWith("top") || c.Result.StartsWith("bottom") ? "left" : "top";
        var w = PopoverOracle.Value.Arrow.Width;
        var h = PopoverOracle.Value.Arrow.Height;
        double x = side == "left" ? c.ArrowLeft!.Value : at.Left ?? (at.Right is { } r ? c.BodyWidth - r - w : 0);
        double y = side == "top" ? c.ArrowTop!.Value : at.Top ?? (at.Bottom is { } b ? c.BodyHeight - b - h : 0);
        // The arrow's box, before its turn: its bounds in the body (the panel fills it: the body has no padding).
        Assert.Equal(new Rect(x, y, w, h), arrow.Bounds);
    }

    [Fact]
    public void Body_margin_and_start_are_getPopoverMarginStyles_and_getStartPosition()
    {
        foreach (var c in PopoverOracle.Value.Body)
        {
            var placement = PopoverOracle.Placement(c.Placement);
            var arrowSize = c.ShowArrow ? PopoverOracle.Value.Constants.ArrowSize : 0;
            Assert.True(new Thickness(c.Margin[0], c.Margin[1], c.Margin[2], c.Margin[3]) == Popover.MarginFor(placement, arrowSize, c.PopoverMargin), $"{c} margin");
            Assert.True(new Point(c.Start[0], c.Start[1]) == Popover.StartFor(placement, arrowSize, c.PopoverMargin), $"{c} start");
            Assert.Equal(new[] { 0.0, 0.0 }, c.End);
        }
    }

    // The control gives the body its margin at the placement chosen, and its entrance starts from getStartPosition's.
    [AvaloniaFact]
    public void The_body_is_given_its_margin_at_the_placement_chosen()
    {
        foreach (var placement in new[] { PopoverPlacement.Top, PopoverPlacement.RightBottom, PopoverPlacement.BottomLeft, PopoverPlacement.Left })
        {
            var (popover, _, _) = PopoverTests.Fixed(150, 400);
            popover.Placement = placement;
            popover.ShowArrow = true;
            popover.PopoverMargin = 13;
            popover.IsOpen = true;
            using var scene = PopoverTests.Show(popover);
            PopoverTests.Settle(scene);
            var name = char.ToLowerInvariant(placement.ToString()[0]) + placement.ToString()[1..];
            Assert.Equal(PopoverOracle.Margin(name, true, 13), ((Layoutable)popover.Part(Popover.BodyPart)!).Margin);
        }
    }
}
