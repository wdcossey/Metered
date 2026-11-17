using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>labelPlacement</c>: where a control's label sits beside its mark (Checkbox's
/// <c>LABEL_PLACEMENT</c>, the same four as Radio's).
/// </summary>
public enum LabelPlacement
{
    Top,
    Right,
    Bottom,
    Left,
}

/// <summary>CSS's <c>align-items</c>: where a flex line puts each item across it.</summary>
public enum AlignItems
{
    /// <summary><c>flex-start</c>.</summary>
    Start,

    /// <summary><c>center</c>.</summary>
    Center,

    /// <summary><c>flex-end</c>.</summary>
    End,

    /// <summary><c>stretch</c>.</summary>
    Stretch,
}

/// <summary>
/// The flex line of Base Web's labelled controls' <c>Root</c> (a <c>label</c> with <c>display: flex</c>):
/// the control's mark and its label, one after the other from the start, along
/// <see cref="Orientation"/> (<c>flexDirection</c>), each placed across the line by
/// <see cref="AlignItems"/>. The last visible child is the label: it alone shrinks (and so wraps)
/// to the room the others leave, as a flex item with the default <c>flex-shrink: 1</c> beside
/// Base Web's marks' <c>flex: 0 0 auto</c>. <see cref="LabelFirst"/> puts the label before the
/// rest, as Base Web renders it for the top and left placements; the line still starts at the start.
/// </summary>
public class LabelPlacementPanel : Panel
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<LabelPlacementPanel, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<AlignItems> AlignItemsProperty =
        AvaloniaProperty.Register<LabelPlacementPanel, AlignItems>(nameof(AlignItems), AlignItems.Start);

    public static readonly StyledProperty<bool> LabelFirstProperty =
        AvaloniaProperty.Register<LabelPlacementPanel, bool>(nameof(LabelFirst));

    static LabelPlacementPanel()
    {
        AffectsMeasure<LabelPlacementPanel>(OrientationProperty, LabelFirstProperty);
        AffectsArrange<LabelPlacementPanel>(AlignItemsProperty);
    }

    /// <summary>CSS's <c>flex-direction</c>: Horizontal is <c>row</c>, Vertical <c>column</c>.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>CSS's <c>align-items</c>.</summary>
    public AlignItems AlignItems { get => GetValue(AlignItemsProperty); set => SetValue(AlignItemsProperty, value); }

    /// <summary>The label (the last visible child) comes first: Base Web's top and left placements.</summary>
    public bool LabelFirst { get => GetValue(LabelFirstProperty); set => SetValue(LabelFirstProperty, value); }

    /// <summary>The index of the label, the last visible child; -1 if none is visible.</summary>
    private int LabelIndex()
    {
        for (var i = Children.Count - 1; i >= 0; i--)
        {
            if (Children[i].IsVisible) return i;
        }

        return -1;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var label = LabelIndex();
        double main = 0, cross = 0;
        // The marks first, at their own size; then the label in what is left along the line.
        var free = horizontal ? new Size(double.PositiveInfinity, availableSize.Height) : new Size(availableSize.Width, double.PositiveInfinity);
        for (var i = 0; i < label; i++)
        {
            var child = Children[i];
            if (!child.IsVisible) continue;
            child.Measure(free);
            main += horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            cross = Math.Max(cross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }

        if (label >= 0)
        {
            var child = Children[label];
            child.Measure(horizontal ? new Size(Math.Max(0, availableSize.Width - main), availableSize.Height) : free);
            main += horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            cross = Math.Max(cross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }

        return horizontal ? new Size(main, cross) : new Size(cross, main);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var lineCross = horizontal ? finalSize.Height : finalSize.Width;
        var label = LabelIndex();
        double at = 0;

        void Place(Control child)
        {
            var size = child.DesiredSize;
            var along = horizontal ? size.Width : size.Height;
            var across = AlignItems == AlignItems.Stretch ? lineCross : (horizontal ? size.Height : size.Width);
            var offset = AlignItems switch
            {
                AlignItems.Center => (lineCross - across) / 2,
                AlignItems.End => lineCross - across,
                _ => 0,
            };
            child.Arrange(horizontal ? new Rect(at, offset, along, across) : new Rect(offset, at, across, along));
            at += along;
        }

        var labelFirst = LabelFirst && label > 0;
        if (labelFirst) Place(Children[label]);
        for (var i = 0; i < Children.Count; i++)
        {
            if (Children[i].IsVisible && !(labelFirst && i == label)) Place(Children[i]);
        }

        return finalSize;
    }
}
