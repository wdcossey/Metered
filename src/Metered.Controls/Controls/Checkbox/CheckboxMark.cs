using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// A box as CSS draws one (its background reaching under its border, as <c>background-clip:
/// border-box</c> has it, and its box-shadows), that also draws what Base Web draws on its
/// Checkbox's marks with CSS:
/// a <see cref="Glyph"/> (the tick or the dash, Base Web's background image: its SVG's path, laid
/// out at <see cref="GlyphSize"/>, contained in the box inside its border and centred, as
/// <c>background-size: contain</c> and <c>background-position: center</c> place it), and an
/// outline of <see cref="OutlineThickness"/> around the box, outside it, following its corners
/// (CSS's <c>outline</c>, and a <c>0 0 0 3px</c> box-shadow ring), which takes a brush, so it
/// follows the theme as a box-shadow's colour can't. The Checkbox's <c>Checkmark</c> and
/// <c>Toggle</c> parts; its look is their themes'.
/// </summary>
public class CheckboxMark : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty = Border.BackgroundProperty.AddOwner<CheckboxMark>();

    public static readonly StyledProperty<IBrush?> BorderBrushProperty = Border.BorderBrushProperty.AddOwner<CheckboxMark>();

    public static readonly StyledProperty<Thickness> BorderThicknessProperty = Border.BorderThicknessProperty.AddOwner<CheckboxMark>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner<CheckboxMark>();

    public static readonly StyledProperty<BoxShadows> BoxShadowProperty = Border.BoxShadowProperty.AddOwner<CheckboxMark>();

    public static readonly StyledProperty<Geometry?> GlyphProperty =
        AvaloniaProperty.Register<CheckboxMark, Geometry?>(nameof(Glyph));

    public static readonly StyledProperty<Size> GlyphSizeProperty =
        AvaloniaProperty.Register<CheckboxMark, Size>(nameof(GlyphSize));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CheckboxMark, IBrush?>(nameof(Foreground));

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        AvaloniaProperty.Register<CheckboxMark, double>(nameof(OutlineThickness));

    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        AvaloniaProperty.Register<CheckboxMark, IBrush?>(nameof(OutlineBrush));

    static CheckboxMark()
    {
        AffectsRender<CheckboxMark>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, BoxShadowProperty, GlyphProperty, GlyphSizeProperty, ForegroundProperty, OutlineThicknessProperty, OutlineBrushProperty);
    }

    /// <summary>The fill, under the border too.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The border's colour.</summary>
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }

    /// <summary>The border's width: Base Web's are the same on every side, and the left one is drawn on all four.</summary>
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }

    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    public BoxShadows BoxShadow { get => GetValue(BoxShadowProperty); set => SetValue(BoxShadowProperty, value); }

    /// <summary>The glyph's path, in the coordinates of a box of <see cref="GlyphSize"/> (the SVG's viewBox).</summary>
    public Geometry? Glyph { get => GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    /// <summary>The glyph's own size, the SVG's width and height.</summary>
    public Size GlyphSize { get => GetValue(GlyphSizeProperty); set => SetValue(GlyphSizeProperty, value); }

    /// <summary>The glyph's fill.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>The outline's width, outside the box; 0 for none.</summary>
    public double OutlineThickness { get => GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }

    /// <summary>The outline's colour.</summary>
    public IBrush? OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }

    /// <summary>Where the glyph is drawn: its size scaled to fit inside the border, centred.</summary>
    internal Rect GlyphRect()
    {
        var inner = new Rect(Bounds.Size).Deflate(BorderThickness);
        var size = GlyphSize;
        if (size.Width <= 0 || size.Height <= 0 || inner.Width <= 0 || inner.Height <= 0) return default;
        var scale = Math.Min(inner.Width / size.Width, inner.Height / size.Height);
        var drawn = size * scale;
        return new Rect(inner.Center.X - drawn.Width / 2, inner.Center.Y - drawn.Height / 2, drawn.Width, drawn.Height);
    }

    public override void Render(DrawingContext context)
    {
        var box = new Rect(Bounds.Size);
        var r = CornerRadius;
        context.DrawRectangle(Background, null, new RoundedRect(box, r), BoxShadow);
        var width = BorderThickness.Left;
        if (width > 0 && BorderBrush is { } border)
        {
            // The border's stroke runs along the middle of its width, inside the box.
            var mid = width / 2;
            static double Shrink(double radius, double by) => Math.Max(0, radius - by);
            context.DrawRectangle(null, new Pen(border, width),
                new RoundedRect(box.Deflate(mid), Shrink(r.TopLeft, mid), Shrink(r.TopRight, mid), Shrink(r.BottomRight, mid), Shrink(r.BottomLeft, mid)));
        }

        if (Glyph is { } glyph && Foreground is { } fill)
        {
            var at = GlyphRect();
            if (at.Width > 0)
            {
                var scale = at.Width / GlyphSize.Width;
                using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(at.X, at.Y)))
                {
                    context.DrawGeometry(fill, null, glyph);
                }
            }
        }

        if (OutlineThickness > 0 && OutlineBrush is { } brush)
        {
            // A stroke centred half its width outside the box; a rounded corner's radius grows by as
            // much (CSS's spread), a square corner stays square.
            var half = OutlineThickness / 2;
            static double Grow(double radius, double by) => radius > 0 ? radius + by : 0;
            var ring = new RoundedRect(new Rect(Bounds.Size).Inflate(half),
                Grow(r.TopLeft, half), Grow(r.TopRight, half), Grow(r.BottomRight, half), Grow(r.BottomLeft, half));
            var pen = new Pen(brush, OutlineThickness, lineJoin: PenLineJoin.Miter);
            context.DrawRectangle(null, pen, ring);
        }
    }
}
