using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="ProgressBar"/>'s <c>BarContainer</c> part: Base Web's flex row, whose children are each
/// <c>flex: 1</c>. As CSS lays them out (a basis of 0, an equal share of the free space), each child is as wide as the
/// others, the row's width less every child's horizontal margins shared out equally; each is as high as the row, which
/// is as high as its highest child. When they need more room than the row has, their margins overflow it, as CSS's do.
/// </summary>
public class ProgressBarContainer : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        var share = Share(availableSize.Width);
        double width = 0, height = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var m = child.Margin;
            child.Measure(new Size(Math.Max(0, share + m.Left + m.Right), availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var share = Share(finalSize.Width);
        // With layout rounding, each edge is rounded where it falls, so the children fill the row exactly, each within
        // a device pixel of its share, as a browser snaps a fraction it lays out to the pixels it paints.
        var scale = UseLayoutRounding ? LayoutHelper.GetLayoutScale(this) : 0;
        double Edge(double at) => scale > 0 ? Math.Round(at * scale) / scale : at;
        double x = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var m = child.Margin;
            var slot = share + m.Left + m.Right;
            var left = Edge(x);
            // A negative margin (an override's) overlaps the bars, as CSS's does; the slot is never less than nothing.
            child.Arrange(new Rect(left, 0, Math.Max(0, Edge(x + slot) - left), finalSize.Height));
            x += slot;
        }

        return finalSize;
    }

    /// <summary>Each child's width inside its margins: the row's width less the margins, in equal parts, none below 0.</summary>
    private double Share(double width)
    {
        double margins = 0;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            margins += child.Margin.Left + child.Margin.Right;
            count++;
        }

        if (count == 0) return 0;
        return double.IsInfinity(width) ? width : Math.Max(0, (width - margins) / count);
    }
}
