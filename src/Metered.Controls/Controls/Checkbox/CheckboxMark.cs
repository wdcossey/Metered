using Avalonia;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Checkbox"/>'s <c>Checkmark</c> part: radio-v2's mark (<see cref="RadioMark"/>: a box,
/// its border, and CSS's <c>outline</c> beyond its offset), which checkbox-v2's is too, that also draws Base Web's
/// background image, the tick or the dash. <see cref="Glyph"/> is its SVG's one path, in the coordinates of its
/// <see cref="GlyphViewBox"/>, drawn at <see cref="GlyphSize"/> (the SVG's own width and height: Base Web sets no
/// <c>background-size</c>) in <see cref="Foreground"/>, centred on the box inside the border
/// (<c>background-position: center</c> in the padding box), clipped to the box and under the border, as CSS draws a
/// background (RadioMark's <c>RenderImage</c>).
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
    internal Rect GlyphRect() => Centred(new Rect(Bounds.Size).Deflate(BorderThickness));

    private Rect Centred(Rect padding)
    {
        var size = GlyphSize;
        return new Rect(padding.Center.X - size.Width / 2, padding.Center.Y - size.Height / 2, size.Width, size.Height);
    }

    // Base Web's background image: over the fill and under the border, clipped to the border box, as CSS draws it.
    protected override void RenderImage(DrawingContext context, RoundedRect border, Rect padding)
    {
        var view = GlyphViewBox;
        if (Glyph is not { } glyph || Foreground is not { } fill || view.Width <= 0 || view.Height <= 0) return;
        var at = Centred(padding);
        if (at.Width <= 0 || at.Height <= 0) return;
        using (context.PushClip(border))
        using (context.PushTransform(Matrix.CreateTranslation(-view.X, -view.Y)
            * Matrix.CreateScale(at.Width / view.Width, at.Height / view.Height)
            * Matrix.CreateTranslation(at.X, at.Y)))
        {
            context.DrawGeometry(fill, null, glyph);
        }
    }
}
