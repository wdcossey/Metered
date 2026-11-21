using Avalonia;
using Avalonia.Controls;

namespace Metered.Controls;

/// <summary>
/// A browser's block flow, for a form control's boxes: its children one under the other, full width, with CSS's margin
/// collapsing. Adjoining margins become one, the largest positive margin plus the most negative: between two children,
/// and between the first child's top margin and the stack's own, and the last child's bottom margin and the stack's own
/// (a box with no padding or border lets its children's margins out). So the caption's bottom margin (8 px) is inside the
/// control container's (16 px), as on baseweb.design.
/// </summary>
/// <remarks>
/// It is the form control's root, where Base Web renders a fragment (its label container and control container), and
/// the <c>ControlContainer</c> part (the child and the caption). A hidden child isn't in the flow, as a box Base Web
/// doesn't render. Its own margins are laid out by its parent, which collapses nothing: only what is inside one form
/// control collapses (Avalonia's layout has no margin collapsing between controls).
/// </remarks>
public class FormControlStack : Panel
{
    /// <summary>CSS's collapsed margin of adjoining margins.</summary>
    internal static double Collapse(double a, double b) => Math.Max(0, Math.Max(a, b)) + Math.Min(0, Math.Min(a, b));

    /// <summary>How much two adjoining margins lose by collapsing: their sum less the one margin they become.</summary>
    private static double Overlap(double a, double b) => a + b - Collapse(a, b);

    private IEnumerable<Control> Flow => Children.Where(c => c.IsVisible);

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = 0.0;
        var height = 0.0;
        foreach (var child in Flow)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        return new Size(width, Math.Max(0, height - Overlaps().Sum()));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var flow = Flow.ToList();
        if (flow.Count == 0) return finalSize;
        var y = -Overlap(Margin.Top, flow[0].Margin.Top);
        for (var i = 0; i < flow.Count; i++)
        {
            if (i > 0) y -= Overlap(flow[i - 1].Margin.Bottom, flow[i].Margin.Top);
            var height = flow[i].DesiredSize.Height;
            flow[i].Arrange(new Rect(0, y, finalSize.Width, height));
            y += height;
        }

        return finalSize;
    }

    /// <summary>What each collapse takes from the children's heights: at the top, between each two, at the bottom.</summary>
    private IEnumerable<double> Overlaps()
    {
        var flow = Flow.ToList();
        if (flow.Count == 0) yield break;
        yield return Overlap(Margin.Top, flow[0].Margin.Top);
        for (var i = 1; i < flow.Count; i++) yield return Overlap(flow[i - 1].Margin.Bottom, flow[i].Margin.Top);
        yield return Overlap(flow[^1].Margin.Bottom, Margin.Bottom);
    }
}
