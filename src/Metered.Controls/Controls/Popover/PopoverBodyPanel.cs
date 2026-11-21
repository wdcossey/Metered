using Avalonia;
using Avalonia.Controls;

namespace Metered.Controls;

/// <summary>
/// The popover's body as CSS lays it out: its children in flow (Base Web's <c>Inner</c>, <c>position: relative</c>) fill
/// it and size it, and a child marked <see cref="IsAbsoluteProperty"/> (the <c>Arrow</c>, <c>position: absolute</c>) is
/// placed by <see cref="Canvas.LeftProperty"/>, <see cref="Canvas.TopProperty"/>, <see cref="Canvas.RightProperty"/> and
/// <see cref="Canvas.BottomProperty"/> against the body's padding box, as CSS's <c>left</c>, <c>top</c>, <c>right</c> and
/// <c>bottom</c>, its margin added, and takes no room. Later children are drawn over earlier ones, as positioned boxes in
/// CSS are.
/// </summary>
public class PopoverBodyPanel : Panel
{
    public static readonly AttachedProperty<bool> IsAbsoluteProperty =
        AvaloniaProperty.RegisterAttached<PopoverBodyPanel, Control, bool>("IsAbsolute");

    static PopoverBodyPanel()
    {
        AffectsParentArrange<PopoverBodyPanel>(Canvas.LeftProperty, Canvas.TopProperty, Canvas.RightProperty, Canvas.BottomProperty, IsAbsoluteProperty);
    }

    public static bool GetIsAbsolute(Control element) => element.GetValue(IsAbsoluteProperty);

    public static void SetIsAbsolute(Control element, bool value) => element.SetValue(IsAbsoluteProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var size = default(Size);
        foreach (var child in Children)
        {
            if (GetIsAbsolute(child))
            {
                child.Measure(Size.Infinity);
                continue;
            }

            child.Measure(availableSize);
            size = new Size(Math.Max(size.Width, child.DesiredSize.Width), Math.Max(size.Height, child.DesiredSize.Height));
        }

        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            if (!GetIsAbsolute(child))
            {
                child.Arrange(new Rect(finalSize));
                continue;
            }

            // The margin box: DesiredSize holds the margins, which Arrange takes off again.
            var box = child.DesiredSize;
            var left = Canvas.GetLeft(child);
            var top = Canvas.GetTop(child);
            var right = Canvas.GetRight(child);
            var bottom = Canvas.GetBottom(child);
            var x = !double.IsNaN(left) ? left : !double.IsNaN(right) ? finalSize.Width - right - box.Width : 0;
            var y = !double.IsNaN(top) ? top : !double.IsNaN(bottom) ? finalSize.Height - bottom - box.Height : 0;
            child.Arrange(new Rect(new Point(x, y), box));
        }

        return finalSize;
    }
}
