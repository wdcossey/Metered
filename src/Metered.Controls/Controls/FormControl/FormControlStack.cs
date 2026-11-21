using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;

namespace Metered.Controls;

/// <summary>
/// A browser's block flow, for a form control's boxes: its children one under the other, full width, with CSS's margin
/// collapsing. Adjoining margins become one, the largest positive margin plus the most negative: between two children,
/// and between the first child's top margin and the stack's own, and the last child's bottom margin and the stack's own
/// (a box with no padding or border lets its children's margins out), through a stack or a content presenter inside it.
/// So the caption's bottom margin (8 px) is inside the control container's (16 px), and a child's top margin collapses
/// with the label's row's bottom margin, as on baseweb.design.
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

    protected override Size MeasureOverride(Size availableSize)
    {
        var flow = Flow();
        var width = 0.0;
        var height = 0.0;
        foreach (var child in flow)
        {
            child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
            width = Math.Max(width, child.DesiredSize.Width);
            height += child.DesiredSize.Height;
        }

        if (flow.Count == 0) return new Size(width, 0);
        height -= Lead(flow) + Trail(flow);
        for (var i = 1; i < flow.Count; i++) height -= Between(flow[i - 1], flow[i]);
        return new Size(width, Math.Max(0, height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var flow = Flow();
        if (flow.Count == 0) return finalSize;
        var y = -Lead(flow);
        for (var i = 0; i < flow.Count; i++)
        {
            if (i > 0) y -= Between(flow[i - 1], flow[i]);
            var height = flow[i].DesiredSize.Height;
            flow[i].Arrange(new Rect(0, y, finalSize.Width, height));
            y += height;
        }

        return finalSize;
    }

    private List<Control> Flow() => Children.Where(c => c.IsVisible).ToList();

    /// <summary>What the stack's top margin and its first child's collapse takes from the children's heights.</summary>
    private double Lead(List<Control> flow)
    {
        var (space, margin) = Top(flow[0]);
        return Margin.Top + space - Collapse(Margin.Top, margin);
    }

    /// <summary>What its last child's bottom margin and its own collapse takes.</summary>
    private double Trail(List<Control> flow)
    {
        var (space, margin) = Bottom(flow[^1]);
        return space + Margin.Bottom - Collapse(margin, Margin.Bottom);
    }

    /// <summary>What two neighbours' margins collapsing takes.</summary>
    private static double Between(Control above, Control below)
    {
        var (aboveSpace, aboveMargin) = Bottom(above);
        var (belowSpace, belowMargin) = Top(below);
        return aboveSpace + belowSpace - Collapse(aboveMargin, belowMargin);
    }

    /// <summary>
    /// The room above a child's content inside the box it is arranged in (its margin, and what a box with no padding or
    /// border round it lets out), and the margin CSS gives it there: a stack's own and its first child's collapsed, which
    /// the stack has already done; a content presenter's and its content's, which it hasn't.
    /// </summary>
    private static (double Space, double Margin) Top(Control child)
    {
        switch (child)
        {
            case FormControlStack stack when stack.Flow() is { Count: > 0 } flow:
            {
                var m = Collapse(stack.Margin.Top, Top(flow[0]).Margin);
                return (m, m);
            }
            case ContentPresenter { Padding.Top: 0, BorderThickness.Top: 0, Child: { IsVisible: true } inner } presenter:
            {
                var (space, margin) = Top(inner);
                return (presenter.Margin.Top + space, Collapse(presenter.Margin.Top, margin));
            }
            default:
                return (child.Margin.Top, child.Margin.Top);
        }
    }

    /// <summary>The same below a child's content.</summary>
    private static (double Space, double Margin) Bottom(Control child)
    {
        switch (child)
        {
            case FormControlStack stack when stack.Flow() is { Count: > 0 } flow:
            {
                var m = Collapse(Bottom(flow[^1]).Margin, stack.Margin.Bottom);
                return (m, m);
            }
            case ContentPresenter { Padding.Bottom: 0, BorderThickness.Bottom: 0, Child: { IsVisible: true } inner } presenter:
            {
                var (space, margin) = Bottom(inner);
                return (presenter.Margin.Bottom + space, Collapse(margin, presenter.Margin.Bottom));
            }
            default:
                return (child.Margin.Bottom, child.Margin.Bottom);
        }
    }
}
