using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Metered.Controls;

/// <summary>
/// The element of a &lt;see cref="Tabs"/&gt;' &lt;c&gt;Root&lt;/c&gt; part, Base Web's &lt;c&gt;StyledRoot&lt;/c&gt;: a block, its children (the bar, the
/// border, the panels) stacked, each as wide as it is (&lt;see cref="Orientation"/&gt; vertical); or, vertical tabs' flex row
/// (horizontal), its children side by side, each but the last at its own width, the last (the panels, &lt;c&gt;flex-grow: 1&lt;/c&gt;)
/// the rest, all as tall as the row (&lt;c&gt;align-items: stretch&lt;/c&gt;).
/// &lt;/summary&gt;
public class TabsRootPanel : Panel
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<TabsRootPanel>();

    static TabsRootPanel()
    {
        AffectsMeasure<TabsRootPanel>(OrientationProperty);
    }

    /// <summary>Vertical: a block's stack. Horizontal: a flex row, the last child taking the rest.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    private List<Control> Shown => Children.Where(c => c.IsVisible).ToList();

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = Shown;
        double width = 0, height = 0;
        if (Orientation == Orientation.Vertical)
        {
            foreach (var child in children)
            {
                child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                width = Math.Max(width, child.DesiredSize.Width);
                height += child.DesiredSize.Height;
            }

            return new Size(width, height);
        }

        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var last = i == children.Count - 1;
            var room = last ? Math.Max(0, availableSize.Width - width) : double.PositiveInfinity;
            child.Measure(new Size(room, availableSize.Height));
            width += child.DesiredSize.Width;
            height = Math.Max(height, child.DesiredSize.Height);
        }

        return new Size(width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = Shown;
        if (Orientation == Orientation.Vertical)
        {
            double y = 0;
            foreach (var child in children)
            {
                var h = child.DesiredSize.Height;
                child.Arrange(new Rect(0, y, finalSize.Width, Math.Max(0, h)));
                y += h;
            }

            return finalSize;
        }

        double x = 0;
        for (var i = 0; i < children.Count; i++)
        {
            var child = children[i];
            var w = i == children.Count - 1 ? Math.Max(0, finalSize.Width - x) : child.DesiredSize.Width;
            child.Arrange(new Rect(x, 0, Math.Max(0, w), finalSize.Height));
            x += w;
        }

        return finalSize;
    }
}
