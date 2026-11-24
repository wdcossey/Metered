using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Metered.Controls;

/// <summary>
/// The element of a select's <c>ValueContainer</c> part, Base Web's <c>StyledValueContainer</c>: a flex row inside its
/// padding (<c>display: flex; align-items: center</c>), wrapping for a multi select (<c>flex-wrap: wrap</c>), as a browser
/// lays it out. Its children are the values (a single value, or a tag for each), the input's box and the placeholder.
/// </summary>
/// <remarks>
/// <para>Each child is at its own width (its margins included), at most the row's (<c>max-width: 100%</c>), and centred in
/// its line's height. In a row that doesn't wrap, children that are too wide shrink as CSS's <c>flex-shrink: 1</c>
/// shrinks them, in proportion to their widths: those whose overflow is hidden (the single value, the placeholder, with
/// their ellipsis), as CSS gives them no minimum, and never the input's box, whose minimum is its content's.</para>
/// <para>A wrapping row breaks a child that doesn't fit onto a new line; the lines stack, each as tall as its tallest
/// child.</para>
/// </remarks>
public class SelectValueContainer : Panel
{
    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<SelectValueContainer>();

    public static readonly StyledProperty<bool> WrapProperty =
        AvaloniaProperty.Register<SelectValueContainer, bool>(nameof(Wrap));

    static SelectValueContainer()
    {
        AffectsMeasure<SelectValueContainer>(PaddingProperty, WrapProperty);
    }

    /// <summary>The padding inside the box.</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    /// <summary>CSS's <c>flex-wrap: wrap</c>: children that don't fit go to a new line.</summary>
    public bool Wrap { get => GetValue(WrapProperty); set => SetValue(WrapProperty, value); }

    /// <summary>Whether a child can shrink below its width: one whose overflow is hidden (not the input's box).</summary>
    private static bool Shrinks(Control child) => child is not SelectInputContainer;

    private List<(Control Child, Rect Slot)> _slots = [];

    protected override Size MeasureOverride(Size availableSize)
    {
        var inner = availableSize.Deflate(Padding);
        var width = inner.Width;
        _slots = Layout(width, measure: true, out var size);
        return size.Inflate(Padding);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var inner = finalSize.Deflate(Padding);
        // A taller box: a single line's items centred in it (align-items); wrapped lines share it (align-content's stretch).
        _slots = Layout(inner.Width, measure: false, out _, inner.Height);
        foreach (var (child, slot) in _slots) child.Arrange(slot.Translate(new Vector(Padding.Left, Padding.Top)));
        return finalSize;
    }

    /// <summary>The children's slots, line by line, for a row this wide; the lines stretched to a height, when given.</summary>
    private List<(Control, Rect)> Layout(double width, bool measure, out Size size, double? stretch = null)
    {
        var shown = Children.Where(c => c.IsVisible).ToList();
        var lines = new List<List<(Control Child, double Width, double Height)>>();
        var line = new List<(Control, double, double)>();
        double used = 0;
        foreach (var child in shown)
        {
            if (measure) child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var w = Math.Min(child.DesiredSize.Width, double.IsInfinity(width) ? double.PositiveInfinity : width);
            var h = child.DesiredSize.Height;
            if (Wrap && line.Count > 0 && used + w > width + 0.01)
            {
                lines.Add(line);
                line = [];
                used = 0;
            }

            line.Add((child, w, h));
            used += w;
        }

        if (line.Count > 0) lines.Add(line);
        var slots = new List<(Control, Rect)>();
        double y = 0, widest = 0;
        var natural = 0.0;
        var placed = new List<(List<(Control Child, double Width, double Height)> Line, double[] Widths)>();
        foreach (var l in lines)
        {
            var total = l.Sum(x => x.Item2);
            var widths = l.Select(x => x.Item2).ToArray();
            if (!Wrap && !double.IsInfinity(width) && total > width)
            {
                // flex-shrink: 1, by base size, among those that can shrink.
                var over = total - width;
                var shrinkable = l.Where(x => Shrinks(x.Item1)).Sum(x => x.Item2);
                for (var i = 0; i < l.Count; i++)
                {
                    if (!Shrinks(l[i].Item1) || shrinkable <= 0) continue;
                    widths[i] = Math.Max(0, l[i].Item2 - over * l[i].Item2 / shrinkable);
                }
            }

            // A child shrunk is measured again at its width: its height then (a label that wraps) is the line's.
            for (var i = 0; i < l.Count; i++)
            {
                if (measure && widths[i] < l[i].Item1.DesiredSize.Width) l[i].Item1.Measure(new Size(widths[i], double.PositiveInfinity));
            }

            natural += l.Count == 0 ? 0 : l.Max(x => x.Item1.DesiredSize.Height);
            placed.Add((l, widths));
        }

        var extra = stretch is { } h0 && placed.Count > 0 ? Math.Max(0, h0 - natural) / placed.Count : 0;
        foreach (var (l, widths) in placed)
        {
            var height = (l.Count == 0 ? 0 : l.Max(x => x.Item1.DesiredSize.Height)) + extra;
            double x0 = 0;
            for (var i = 0; i < l.Count; i++)
            {
                var child = l[i].Item1;
                var childHeight = child.DesiredSize.Height;
                slots.Add((child, new Rect(x0, y + (height - childHeight) / 2, widths[i], childHeight)));
                x0 += widths[i];
            }

            widest = Math.Max(widest, x0);
            y += height;
        }

        size = new Size(double.IsInfinity(width) ? widest : width, y);
        return slots;
    }
}
