using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Radio"/>'s <c>Root</c> part: Base Web's <c>label</c>, an inline flex box of one line,
/// as its style function lays it out. Its children go in a row or a column (<see cref="Orientation"/>, CSS's
/// <c>flex-direction</c>), in their order or, with <see cref="LabelFirst"/>, the other way round (radio.js renders the
/// label wrapper before the backplate for the top and left placements), <see cref="Spacing"/> apart (the gap along the
/// direction), aligned across it by <see cref="AlignItems"/>, inside <see cref="Padding"/>. It hugs its children.
/// </summary>
/// <remarks>
/// The children are measured in their own order, so the first (the backplate, which keeps its size) is measured
/// before the second, which takes what is left of the line, as a flex line shrinks a label rather than the backplate,
/// whose minimum is its own size.
/// </remarks>
public class RadioRootPanel : Panel
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<RadioRootPanel>(new StyledPropertyMetadata<Orientation>(Orientation.Horizontal));

    public static readonly StyledProperty<bool> LabelFirstProperty =
        AvaloniaProperty.Register<RadioRootPanel, bool>(nameof(LabelFirst));

    public static readonly StyledProperty<RadioRootAlignment> AlignItemsProperty =
        AvaloniaProperty.Register<RadioRootPanel, RadioRootAlignment>(nameof(AlignItems));

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<RadioRootPanel, double>(nameof(Spacing));

    public static readonly StyledProperty<Thickness> PaddingProperty =
        Decorator.PaddingProperty.AddOwner<RadioRootPanel>();

    static RadioRootPanel()
    {
        AffectsMeasure<RadioRootPanel>(OrientationProperty, SpacingProperty, PaddingProperty);
        AffectsArrange<RadioRootPanel>(LabelFirstProperty, AlignItemsProperty);
    }

    /// <summary>A row (horizontal) or a column (vertical).</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Whether the children are laid out last first: the label before the mark.</summary>
    public bool LabelFirst { get => GetValue(LabelFirstProperty); set => SetValue(LabelFirstProperty, value); }

    /// <summary>The children's alignment across the direction.</summary>
    public RadioRootAlignment AlignItems { get => GetValue(AlignItemsProperty); set => SetValue(AlignItemsProperty, value); }

    /// <summary>The gap between two children along the direction.</summary>
    public double Spacing { get => GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>The room inside the panel's edges, which takes the pointer as the children do.</summary>
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var row = Orientation == Orientation.Horizontal;
        var inner = availableSize.Deflate(Padding);
        var availableMain = row ? inner.Width : inner.Height;
        var availableCross = row ? inner.Height : inner.Width;
        double main = 0, cross = 0;
        var count = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var gap = count > 0 ? Spacing : 0;
            var left = Math.Max(0, availableMain - main - gap);
            child.Measure(row ? new Size(left, availableCross) : new Size(availableCross, left));
            var desired = child.DesiredSize;
            main += gap + (row ? desired.Width : desired.Height);
            cross = Math.Max(cross, row ? desired.Height : desired.Width);
            count++;
        }

        return (row ? new Size(main, cross) : new Size(cross, main)).Inflate(Padding);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var row = Orientation == Orientation.Horizontal;
        var inner = new Rect(finalSize).Deflate(Padding);
        var crossSize = row ? inner.Height : inner.Width;
        double position = 0;
        var count = 0;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[LabelFirst ? Children.Count - 1 - i : i];
            if (!child.IsVisible) continue;
            if (count++ > 0) position += Spacing;
            var desired = child.DesiredSize;
            var main = row ? desired.Width : desired.Height;
            var cross = row ? desired.Height : desired.Width;
            var offset = AlignItems == RadioRootAlignment.Center ? Math.Max(0, (crossSize - cross) / 2) : 0;
            child.Arrange(row
                ? new Rect(inner.X + position, inner.Y + offset, main, cross)
                : new Rect(inner.X + offset, inner.Y + position, cross, main));
            position += main;
        }

        return finalSize;
    }
}
