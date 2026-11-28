using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Where the tooltip's body and arrow are drawn: popper.js's placement and offsets at the tooltip's popoverMargin, 0, without
/// the arrow, and at 8 with it, stateful-tooltip.test.tsx's (PopoverOracle's tether cases, popper.js itself: tether.g.json
/// has no arrow at a margin of 0), Body's margin there (getPopoverMarginStyles), and the arrow where
/// getArrowPositionStyles puts it; and that its body never moves as it fades in and out
/// (tooltip/styled-components.js's Body: its transform getEndPosition in every state).
/// </summary>
public class TooltipLayoutTests
{
    /// <summary>
    /// The tether cases drawn, the 120 × 60 body round the anchors in the middle and in a corner it must flip from: at the
    /// tooltip's margin, 0 (no arrow), and at 8 with the arrow.
    /// </summary>
    public static TheoryData<int> Drawn()
    {
        var data = new TheoryData<int>();
        var cases = PopoverOracle.Value.Tether.Cases;
        for (var i = 0; i < cases.Length; i++)
        {
            var c = cases[i];
            if (!c.IgnoreBoundary && c.BodyWidth == 120 && c.BodyHeight == 60 && c.Anchor is "middle" or "bottomRight"
                && (c.PopoverMargin == TooltipOracle.Value.Defaults.Stateful.PopoverMargin || (c.PopoverMargin == 8 && c.ShowArrow))) data.Add(i);
        }

        return data;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Drawn))]
    public void The_body_and_arrow_are_drawn_where_popper_js_and_Base_Web_put_them(int index)
    {
        var c = PopoverOracle.Value.Tether.Cases[index];
        // The content's size for a body of the case's: Inner's padding round it.
        var pad = TooltipOracle.Value.Colours[0].Inner.Padding;
        var (tooltip, anchor, _) = TooltipTests.Fixed(c.X, c.Y, c.Width, c.Height, c.BodyWidth - pad[0] - pad[2], c.BodyHeight - pad[1] - pad[3]);
        tooltip.Placement = PopoverOracle.Placement(c.Placement);
        tooltip.ShowArrow = c.ShowArrow;
        tooltip.PopoverMargin = c.PopoverMargin;
        using var scene = PopoverTests.Show(tooltip);
        tooltip.IsOpen = true;
        PopoverTests.Settle(scene, 180);

        Assert.Equal(new Rect(c.X, c.Y, c.Width, c.Height), PopoverTests.BoxOf(anchor, scene));
        Assert.Equal(PopoverOracle.Placement(c.SteadyResult), tooltip.ActualPlacement);
        var m = TooltipOracle.Value.Body.Single(b => b.Placement == c.SteadyResult && b.ShowArrow == c.ShowArrow && b.PopoverMargin == c.PopoverMargin).Margin;
        var body = (Visual)tooltip.Part(Popover.BodyPart)!;
        Assert.Equal(new Rect(c.SteadyLeft + m[0], c.SteadyTop + m[1], c.BodyWidth, c.BodyHeight), PopoverTests.BoxOf(body, scene));

        var arrow = (Control)tooltip.Part(Popover.ArrowPart)!;
        Assert.Equal(c.ShowArrow, arrow.IsEffectivelyVisible);
        if (!c.ShowArrow) return;
        var at = PopoverOracle.Value.Arrow.Cases.Single(a => a.Placement == c.SteadyResult);
        var side = c.SteadyResult.StartsWith("top") || c.SteadyResult.StartsWith("bottom") ? "left" : "top";
        var w = PopoverOracle.Value.Arrow.Width;
        var h = PopoverOracle.Value.Arrow.Height;
        double x = side == "left" ? c.SteadyArrowLeft!.Value : at.Left ?? (at.Right is { } r ? c.BodyWidth - r - w : 0);
        double y = side == "top" ? c.SteadyArrowTop!.Value : at.Top ?? (at.Bottom is { } b ? c.BodyHeight - b - h : 0);
        Assert.Equal(new Rect(x, y, w, h), arrow.Bounds);
    }

    // A text content wraps at the room it has (CSS's white-space, normal, a browser's own): a long one is kept inside the
    // window, on more than one line, its lines Inner's line height (font150's) each.
    [AvaloniaFact]
    public void A_long_text_wraps_inside_the_window()
    {
        var (tooltip, _, _) = TooltipTests.Fixed(150, 400);
        tooltip.Content = string.Join(" ", Enumerable.Repeat("Tooltips display short messages.", 6));
        tooltip.Placement = PopoverPlacement.Bottom;
        tooltip.IsOpen = true;
        using var scene = PopoverTests.Show(tooltip);
        PopoverTests.Settle(scene);
        var c = TooltipOracle.Value.Colours[0].Inner;
        var body = PopoverTests.BoxOf((Visual)tooltip.Part(Popover.BodyPart)!, scene);
        Assert.True(body.Left >= 0 && body.Right <= Scene.Width, $"{body}");
        var text = ((Visual)tooltip.Part(Popover.InnerPart)!).GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(TextWrapping.Wrap, text.TextWrapping);
        var lines = (body.Height - c.Padding[1] - c.Padding[3]) / c.Font.LineHeight;
        Assert.True(lines >= 2 && lines == Math.Floor(lines), $"{lines} lines");
    }

    // Body's margin is the popover's getPopoverMarginStyles; its transform is the same closed as open: no start position.
    [Fact]
    public void Body_margin_is_getPopoverMarginStyles_and_its_start_is_its_end()
    {
        foreach (var c in TooltipOracle.Value.Body)
        {
            var placement = PopoverOracle.Placement(c.Placement);
            var arrowSize = c.ShowArrow ? TooltipOracle.Value.Constants.ArrowSize : 0;
            Assert.True(new Thickness(c.Margin[0], c.Margin[1], c.Margin[2], c.Margin[3]) == Popover.MarginFor(placement, arrowSize, c.PopoverMargin), $"{c} margin");
            Assert.Equal(new[] { 0.0, 0.0 }, c.Start);
            Assert.Equal(new[] { 0.0, 0.0 }, c.End);
        }
    }

    // The body is drawn where it is placed from the moment it is shown, through its fade in and its fade out, as Base Web's
    // tooltip body, whose transform is getEndPosition in every state; a popover's starts towards its anchor.
    [AvaloniaTheory]
    [InlineData(false, 0)]
    [InlineData(true, 0)]
    [InlineData(true, 20)]
    public void Its_body_never_moves_as_it_fades_in_and_out(bool showArrow, double popoverMargin)
    {
        var (tooltip, _, _) = TooltipTests.Fixed(150, 400);
        tooltip.Placement = PopoverPlacement.Bottom;
        tooltip.ShowArrow = showArrow;
        tooltip.PopoverMargin = popoverMargin;
        tooltip.AnimateOutTime = 300;
        using var scene = PopoverTests.Show(tooltip);
        foreach (var l in tooltip.GetSelfAndVisualDescendants().OfType<Layoutable>()) l.UseLayoutRounding = false;
        tooltip.IsOpen = true;
        scene.Pump();
        var body = (Visual)tooltip.Part(Popover.BodyPart)!;
        Assert.Equal(0, body.Opacity);
        Assert.DoesNotContain(":is-animating", tooltip.Classes);
        var placed = PopoverTests.BoxOf(body, scene);
        Assert.Equal(Matrix.Identity, body.RenderTransform?.Value ?? Matrix.Identity);

        PopoverTests.Settle(scene, 25);
        Assert.Contains(":is-animating", tooltip.Classes);
        Assert.Equal(placed, PopoverTests.BoxOf(body, scene));
        PopoverTests.Settle(scene, 200);
        Assert.Equal(1, body.Opacity);
        Assert.Equal(placed, PopoverTests.BoxOf(body, scene));

        tooltip.IsOpen = false;
        PopoverTests.Settle(scene, 100);
        Assert.InRange(body.Opacity, 0.01, 0.99);
        Assert.Equal(placed, PopoverTests.BoxOf(body, scene));
        Assert.Equal(Matrix.Identity, body.RenderTransform?.Value ?? Matrix.Identity);

        // A popover's body at the same props starts towards its anchor (getStartPosition), unless that is 0 (no arrow, no margin).
        if (!showArrow && popoverMargin == 0) return;
        var (popover, _, _) = PopoverTests.Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        popover.ShowArrow = showArrow;
        popover.PopoverMargin = popoverMargin;
        using var popoverScene = PopoverTests.Show(popover);
        popover.IsOpen = true;
        popoverScene.Pump();
        Assert.NotEqual(Matrix.Identity, ((Visual)popover.Part(Popover.BodyPart)!).RenderTransform!.Value);
    }
}
