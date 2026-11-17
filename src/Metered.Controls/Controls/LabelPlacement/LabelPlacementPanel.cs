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

    /// <summary>The visible children in the order they are laid out.</summary>
    private List<Control> Line()
    {
        var line = Children.Where(c => c.IsVisible).ToList();
        if (LabelFirst && line.Count > 1)
        {
            var label = line[^1];
            line.RemoveAt(line.Count - 1);
            line.Insert(0, label);
        }

        return line;
    }

    private Control? Label() => Children.LastOrDefault(c => c.IsVisible);

    protected override Size MeasureOverride(Size availableSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var label = Label();
        double main = 0, cross = 0;
        // The marks first, at their own size; then the label in what is left along the line.
        foreach (var child in Children.Where(c => c.IsVisible && c != label))
        {
            child.Measure(horizontal ? new Size(double.PositiveInfinity, availableSize.Height) : new Size(availableSize.Width, double.PositiveInfinity));
            main += horizontal ? child.DesiredSize.Width : child.DesiredSize.Height;
            cross = Math.Max(cross, horizontal ? child.DesiredSize.Height : child.DesiredSize.Width);
        }

        if (label is not null)
        {
            label.Measure(horizontal
                ? new Size(Math.Max(0, availableSize.Width - main), availableSize.Height)
                : new Size(availableSize.Width, double.PositiveInfinity));
            main += horizontal ? label.DesiredSize.Width : label.DesiredSize.Height;
            cross = Math.Max(cross, horizontal ? label.DesiredSize.Height : label.DesiredSize.Width);
        }

        return horizontal ? new Size(main, cross) : new Size(cross, main);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var horizontal = Orientation == Orientation.Horizontal;
        var lineCross = horizontal ? finalSize.Height : finalSize.Width;
        double at = 0;
        foreach (var child in Line())
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

        return finalSize;
    }
}
