using Avalonia;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Checkbox"/>'s <c>Checkmark</c> part: radio-v2's mark (<see cref="RadioMark"/>: a box,
/// its border, and CSS's <c>outline</c> beyond its offset), which checkbox-v2's is too, that also draws Base Web's
/// background image, the tick or the dash. <see cref="Glyph"/> is its SVG's one path, in the coordinates of its
/// <see cref="GlyphViewBox"/>, drawn at <see cref="GlyphSize"/> (the SVG's own width and height: Base Web sets no
/// <c>background-size</c>) in <see cref="Foreground"/>, centred on the box inside the border
/// (<c>background-position: center</c> in the padding box), and clipped to the box, as CSS clips a background to its
/// border box.
/// </summary>
public class CheckboxMark : RadioMark
{
    public static readonly StyledProperty<Geometry?> GlyphProperty =
        AvaloniaProperty.Register<CheckboxMark, Geometry?>(nameof(Glyph));

    public static readonly StyledProperty<Size> GlyphSizeProperty =
        AvaloniaProperty.Register<CheckboxMark, Size>(nameof(GlyphSize));

    public static readonly StyledProperty<Rect> GlyphViewBoxProperty =
        AvaloniaProperty.Register<CheckboxMark, Rect>(nameof(GlyphViewBox));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<CheckboxMark, IBrush?>(nameof(Foreground));

    static CheckboxMark()
    {
        AffectsRender<CheckboxMark>(GlyphProperty, GlyphSizeProperty, GlyphViewBoxProperty, ForegroundProperty);
    }

    /// <summary>The glyph's path, in the coordinates of <see cref="GlyphViewBox"/>; none when null.</summary>
    public Geometry? Glyph { get => GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

    /// <summary>The size the glyph is drawn at: its SVG's width and height.</summary>
    public Size GlyphSize { get => GetValue(GlyphSizeProperty); set => SetValue(GlyphSizeProperty, value); }

    /// <summary>The glyph's coordinates: its SVG's <c>viewBox</c>, which <see cref="GlyphSize"/> shows whole.</summary>
    public Rect GlyphViewBox { get => GetValue(GlyphViewBoxProperty); set => SetValue(GlyphViewBoxProperty, value); }

    /// <summary>The glyph's fill.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>Where the glyph is drawn: <see cref="GlyphSize"/>, centred on the box inside the border.</summary>
    internal Rect GlyphRect()
    {
        var inner = new Rect(Bounds.Size).Deflate(BorderThickness);
        var size = GlyphSize;
        return new Rect(inner.Center.X - size.Width / 2, inner.Center.Y - size.Height / 2, size.Width, size.Height);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var view = GlyphViewBox;
        if (Glyph is not { } glyph || Foreground is not { } fill || view.Width <= 0 || view.Height <= 0) return;
        var at = GlyphRect();
        if (at.Width <= 0 || at.Height <= 0) return;
        // Drawn over the border, which is transparent wherever a glyph is (checked or indeterminate) or, disabled, the
        // fill's colour, and which the glyph doesn't reach: as CSS draws the image under it.
        using (context.PushClip(new RoundedRect(new Rect(Bounds.Size), Clamp(CornerRadius, Bounds.Size))))
        using (context.PushTransform(Matrix.CreateTranslation(-view.X, -view.Y)
            * Matrix.CreateScale(at.Width / view.Width, at.Height / view.Height)
            * Matrix.CreateTranslation(at.X, at.Y)))
        {
            context.DrawGeometry(fill, null, glyph);
        }
    }
}
