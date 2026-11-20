using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Slider"/>'s <c>Thumb</c> part, one for each of its values: a round handle that takes the
/// focus (react-range gives each thumb <c>tabIndex=0</c>), drawn as Base Web's: <see cref="Background"/> in its
/// corners (<see cref="CornerRadius"/>) over its <see cref="BoxShadow"/>, and the focus ring, Base Web's
/// <c>0 0 0 3px</c> shadow, as a line round the box (<see cref="OutlineBrush"/>, <see cref="OutlineThickness"/>,
/// <see cref="OutlineOffset"/>), which a spread-only shadow on a round box is and which can take its colour by token.
/// </summary>
/// <remarks>
/// Its children, the thumb's <c>ThumbValue</c> and <c>InnerThumb</c> while its label shows, are CSS's absolutely placed
/// children of a flex box that centres them: centred across the thumb, at <see cref="Canvas.TopProperty"/> from its top
/// (CSS's <c>top</c>), or centred down it when that is unset. They overflow the thumb, which doesn't clip them.
/// </remarks>
public class SliderThumb : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Panel.BackgroundProperty.AddOwner<SliderThumb>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<SliderThumb>();

    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        Border.BoxShadowProperty.AddOwner<SliderThumb>();

    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        AvaloniaProperty.Register<SliderThumb, IBrush?>(nameof(OutlineBrush));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<SliderThumb, double>(nameof(OutlineThickness));

    public static readonly StyledProperty<double> OutlineOffsetProperty =
        AvaloniaProperty.Register<SliderThumb, double>(nameof(OutlineOffset));

    private ImmutablePen? _outlinePen;

    public SliderThumb()
    {
        SliderChildren.Follow(Children, LogicalChildren, VisualChildren, InvalidateMeasure);
    }

    static SliderThumb()
    {
        FocusableProperty.OverrideDefaultValue<SliderThumb>(true);
        AffectsRender<SliderThumb>(BackgroundProperty, CornerRadiusProperty, BoxShadowProperty, OutlineBrushProperty, OutlineThicknessProperty, OutlineOffsetProperty);
    }

    /// <summary>The marks and thumbs (the line's), or the label's parts (a thumb's), in the order they are drawn.</summary>
    public Avalonia.Controls.Controls Children { get; } = new();

    /// <summary>The fill: the line's rest, or the handle's.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The slider whose thumb this is, and the value it holds.</summary>
    internal Slider? Owner { get; set; }

    /// <summary>Which of the slider's values the thumb holds: react-range's index.</summary>
    internal int Index { get; set; }

    /// <summary>The handle's corners; a radius larger than half the box is drawn as half, as CSS draws it.</summary>
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>CSS's box-shadow, drawn under the fill.</summary>
    public BoxShadows BoxShadow { get => GetValue(BoxShadowProperty); set => SetValue(BoxShadowProperty, value); }

    /// <summary>The focus ring's colour; none when null.</summary>
    public IBrush? OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }

    /// <summary>The focus ring's width; none when 0.</summary>
    public double OutlineThickness { get => GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }

    /// <summary>The gap between the box and its ring.</summary>
    public double OutlineOffset { get => GetValue(OutlineOffsetProperty); set => SetValue(OutlineOffsetProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OutlineBrushProperty || change.Property == OutlineThicknessProperty) _outlinePen = null;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SliderThumbAutomationPeer(this);

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(Size.Infinity);
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (var child in Children)
        {
            var size = child.DesiredSize;
            var top = Canvas.GetTop(child);
            var y = double.IsNaN(top) ? (finalSize.Height - size.Height) / 2 : top;
            child.Arrange(new Rect((finalSize.Width - size.Width) / 2, y, size.Width, size.Height));
        }

        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var box = new Rect(Bounds.Size);
        var most = Math.Max(0, Math.Min(box.Width, box.Height) / 2);
        var r = CornerRadius;
        var c = new CornerRadius(Math.Min(r.TopLeft, most), Math.Min(r.TopRight, most), Math.Min(r.BottomRight, most), Math.Min(r.BottomLeft, most));
        var shape = new RoundedRect(box, c);
        if (Background is not null || BoxShadow.Count > 0) context.DrawRectangle(Background, null, shape, BoxShadow);

        if (OutlineBrush is { } brush && OutlineThickness > 0)
        {
            // The line's centre: past the offset and half the line's width, its corners grown as much.
            var grow = OutlineOffset + OutlineThickness / 2;
            var radii = new CornerRadius(c.TopLeft + grow, c.TopRight + grow, c.BottomRight + grow, c.BottomLeft + grow);
            _outlinePen ??= new ImmutablePen(brush.ToImmutable(), OutlineThickness);
            context.DrawRectangle(null, _outlinePen, new RoundedRect(box.Inflate(grow), radii));
        }
    }
}
