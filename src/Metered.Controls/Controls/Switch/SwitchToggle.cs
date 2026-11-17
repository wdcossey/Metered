using Avalonia;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Switch"/>'s <c>Toggle</c> part, the knob: a <see cref="RadioMark"/> (its fill, corners
/// and hover overlay) that also draws Base Web's checkmark, the toggle's <c>background-image</c> with
/// <c>showIcon</c>: <see cref="Icon"/>, a path in Base Web's 24 × 24 view box, filled with <see cref="IconBrush"/>,
/// drawn <see cref="IconSize"/> wide and high in the middle of the knob's padding box (<c>background-position:
/// center</c>), clipped to its round corners, over the fill and under the overlay, as CSS paints a background image
/// under an inset shadow.
/// </summary>
public class SwitchToggle : RadioMark
{
    /// <summary>The side of Base Web's icon's view box, <c>viewBox="0 0 24 24"</c> (switch/styled-components.js).</summary>
    public const double IconViewBox = 24;

    public static readonly StyledProperty<Geometry?> IconProperty =
        AvaloniaProperty.Register<SwitchToggle, Geometry?>(nameof(Icon));

    public static readonly StyledProperty<IBrush?> IconBrushProperty =
        AvaloniaProperty.Register<SwitchToggle, IBrush?>(nameof(IconBrush));

    public static readonly StyledProperty<double> IconSizeProperty =
        AvaloniaProperty.Register<SwitchToggle, double>(nameof(IconSize));

    static SwitchToggle()
    {
        AffectsRender<SwitchToggle>(IconProperty, IconBrushProperty, IconSizeProperty);
    }

    /// <summary>The checkmark's path, in a 24 × 24 box; none when null.</summary>
    public Geometry? Icon { get => GetValue(IconProperty); set => SetValue(IconProperty, value); }

    /// <summary>The checkmark's fill; none when null.</summary>
    public IBrush? IconBrush { get => GetValue(IconBrushProperty); set => SetValue(IconBrushProperty, value); }

    /// <summary>The checkmark's drawn width and height; none when 0.</summary>
    public double IconSize { get => GetValue(IconSizeProperty); set => SetValue(IconSizeProperty, value); }

    protected override void RenderImage(DrawingContext context, RoundedRect border, Rect padding)
    {
        if (Icon is not { } icon || IconBrush is not { } brush || IconSize <= 0) return;
        var scale = IconSize / IconViewBox;
        var at = new Point(padding.X + (padding.Width - IconSize) / 2, padding.Y + (padding.Height - IconSize) / 2);
        using (context.PushClip(border))
        using (context.PushTransform(Matrix.CreateScale(scale, scale) * Matrix.CreateTranslation(at.X, at.Y)))
        {
            context.DrawGeometry(brush, null, icon);
        }
    }
}
