using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Radio"/>'s <c>RadioMarkOuter</c> and <c>RadioMarkInner</c> parts: a box like a
/// <see cref="Border"/> (<see cref="Background"/>, <see cref="BorderBrush"/>, <see cref="BorderThickness"/>,
/// <see cref="CornerRadius"/>) that also draws two things Base Web's marks have and a border can't take from a theme by
/// token. <see cref="Overlay"/> is the inner mark's hover and press tint, Base Web's
/// <c>box-shadow: inset 999px 999px 0px</c>, which fills the box inside the border over its background.
/// <see cref="OutlineBrush"/>, <see cref="OutlineThickness"/> and <see cref="OutlineOffset"/> are CSS's
/// <c>outline</c> and <c>outline-offset</c>, the outer mark's focus ring: a line that follows the box's corners outside
/// it, beyond a gap, taking no room in layout.
/// </summary>
/// <remarks>A radius larger than half the box is drawn as half the box, as CSS draws it.</remarks>
public class RadioMark : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<RadioMark>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        Border.BorderBrushProperty.AddOwner<RadioMark>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        Border.BorderThicknessProperty.AddOwner<RadioMark>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<RadioMark>();

    public static readonly StyledProperty<IBrush?> OverlayProperty =
        AvaloniaProperty.Register<RadioMark, IBrush?>(nameof(Overlay));

    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        AvaloniaProperty.Register<RadioMark, IBrush?>(nameof(OutlineBrush));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<RadioMark, double>(nameof(OutlineThickness));

    public static readonly StyledProperty<double> OutlineOffsetProperty =
        AvaloniaProperty.Register<RadioMark, double>(nameof(OutlineOffset));

    static RadioMark()
    {
        AffectsRender<RadioMark>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty,
            OverlayProperty, OutlineBrushProperty, OutlineThicknessProperty, OutlineOffsetProperty);
        AffectsMeasure<RadioMark>(BorderThicknessProperty);
    }

    private ImmutablePen? _borderPen;
    private ImmutablePen? _outlinePen;

    /// <summary>The fill, under the border as CSS's is.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The border's colour.</summary>
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }

    /// <summary>The border's width, inside the box.</summary>
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }

    /// <summary>The box's corners.</summary>
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>A tint over the background, inside the border; none when null.</summary>
    public IBrush? Overlay { get => GetValue(OverlayProperty); set => SetValue(OverlayProperty, value); }

    /// <summary>The outline's colour; none when null.</summary>
    public IBrush? OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }

    /// <summary>The outline's width; none when 0.</summary>
    public double OutlineThickness { get => GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }

    /// <summary>The gap between the box and its outline.</summary>
    public double OutlineOffset { get => GetValue(OutlineOffsetProperty); set => SetValue(OutlineOffsetProperty, value); }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == BorderBrushProperty || change.Property == BorderThicknessProperty) _borderPen = null;
        else if (change.Property == OutlineBrushProperty || change.Property == OutlineThicknessProperty) _outlinePen = null;
    }

    protected override Size MeasureOverride(Size availableSize) =>
        LayoutHelper.MeasureChild(Child, availableSize, Padding, BorderThickness);

    protected override Size ArrangeOverride(Size finalSize) =>
        LayoutHelper.ArrangeChild(Child, finalSize, Padding, BorderThickness);

    public override void Render(DrawingContext context)
    {
        var box = new Rect(Bounds.Size);
        var outer = Clamp(CornerRadius, box.Size);
        var b = BorderThickness;
        var inside = box.Deflate(b);
        // The padding box's corners: the border box's, less the border.
        var inner = Clamp(new CornerRadius(
            Math.Max(0, outer.TopLeft - Math.Max(b.Left, b.Top)), Math.Max(0, outer.TopRight - Math.Max(b.Right, b.Top)),
            Math.Max(0, outer.BottomRight - Math.Max(b.Right, b.Bottom)), Math.Max(0, outer.BottomLeft - Math.Max(b.Left, b.Bottom))), inside.Size);

        if (Background is { } background) context.DrawRectangle(background, null, new RoundedRect(box, outer));

        if (BorderBrush is { } border && b != default)
        {
            if (b.IsUniform)
            {
                // A uniform border: a line along the middle of the border's width.
                var half = b.Left / 2;
                var r = new CornerRadius(Math.Max(0, outer.TopLeft - half), Math.Max(0, outer.TopRight - half),
                    Math.Max(0, outer.BottomRight - half), Math.Max(0, outer.BottomLeft - half));
                _borderPen ??= new ImmutablePen(border.ToImmutable(), b.Left);
                context.DrawRectangle(null, _borderPen, new RoundedRect(box.Deflate(half), r));
            }
            else
            {
                // Sides of different widths: the box less its padding box.
                context.DrawGeometry(border, null, new CombinedGeometry(GeometryCombineMode.Exclude, Box(box, outer), Box(inside, inner)));
            }
        }

        if (Overlay is { } overlay && inside.Width > 0 && inside.Height > 0)
        {
            context.DrawRectangle(overlay, null, new RoundedRect(inside, inner));
        }

        if (OutlineBrush is { } brush && OutlineThickness > 0)
        {
            // The line's centre: past the offset and half the line's width, its corners grown as much.
            var grow = OutlineOffset + OutlineThickness / 2;
            var radii = new CornerRadius(outer.TopLeft + grow, outer.TopRight + grow, outer.BottomRight + grow, outer.BottomLeft + grow);
            _outlinePen ??= new ImmutablePen(brush.ToImmutable(), OutlineThickness);
            context.DrawRectangle(null, _outlinePen, new RoundedRect(box.Inflate(grow), radii));
        }
    }

    /// <summary>A box with rounded corners, as a geometry.</summary>
    private static StreamGeometry Box(Rect r, CornerRadius c)
    {
        var geometry = new StreamGeometry();
        using var g = geometry.Open();
        void Corner(Point to, double radius)
        {
            if (radius > 0) g.ArcTo(to, new Size(radius, radius), 0, false, SweepDirection.Clockwise);
            else g.LineTo(to);
        }

        g.BeginFigure(new Point(r.Left + c.TopLeft, r.Top), true);
        g.LineTo(new Point(r.Right - c.TopRight, r.Top));
        Corner(new Point(r.Right, r.Top + c.TopRight), c.TopRight);
        g.LineTo(new Point(r.Right, r.Bottom - c.BottomRight));
        Corner(new Point(r.Right - c.BottomRight, r.Bottom), c.BottomRight);
        g.LineTo(new Point(r.Left + c.BottomLeft, r.Bottom));
        Corner(new Point(r.Left, r.Bottom - c.BottomLeft), c.BottomLeft);
        g.LineTo(new Point(r.Left, r.Top + c.TopLeft));
        Corner(new Point(r.Left + c.TopLeft, r.Top), c.TopLeft);
        g.EndFigure(true);
        return geometry;
    }

    /// <summary>A radius no larger than half the box, as CSS draws a larger one.</summary>
    private static CornerRadius Clamp(CornerRadius r, Size size)
    {
        var most = Math.Max(0, Math.Min(size.Width, size.Height) / 2);
        return new CornerRadius(Math.Min(r.TopLeft, most), Math.Min(r.TopRight, most), Math.Min(r.BottomRight, most), Math.Min(r.BottomLeft, most));
    }
}
