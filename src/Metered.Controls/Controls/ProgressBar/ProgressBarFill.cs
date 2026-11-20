using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="ProgressBar"/>'s <c>BarProgress</c> part, a bar's fill: a box of the bar's whole size
/// (<see cref="Background"/>, <see cref="CornerRadius"/>) drawn moved along by <see cref="Offset"/>, CSS's
/// <c>transform: translateX(…%)</c>, a percentage of its own width that takes no room in layout. The bar clips it.
/// </summary>
/// <remarks>
/// Avalonia's transforms take pixels, and a theme can't hold a length relative to the element's own width, so the fill
/// keeps CSS's percentage and moves itself as it draws. In a bar of one step the <see cref="ProgressBar"/> sets the
/// value's offset at Template priority, above the theme's plain setter and below its state styles and an override.
/// </remarks>
public class ProgressBarFill : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<ProgressBarFill>();

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<ProgressBarFill>();

    public static readonly StyledProperty<double> OffsetProperty =
        AvaloniaProperty.Register<ProgressBarFill, double>(nameof(Offset));

    static ProgressBarFill()
    {
        AffectsRender<ProgressBarFill>(BackgroundProperty, CornerRadiusProperty, OffsetProperty);
    }

    /// <summary>The fill's colour.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The box's corners; a radius larger than half the box is drawn as half, as CSS draws it.</summary>
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>CSS's <c>translateX</c>: how far along the box is drawn, in percent of its own width (−100 is a box's width to the left).</summary>
    public double Offset { get => GetValue(OffsetProperty); set => SetValue(OffsetProperty, value); }

    public override void Render(DrawingContext context)
    {
        if (Background is not { } background) return;
        var size = Bounds.Size;
        var box = new Rect(new Point(Offset / 100 * size.Width, 0), size);
        context.DrawRectangle(background, null, new RoundedRect(box, Clamp(CornerRadius, size)));
    }

    internal static CornerRadius Clamp(CornerRadius r, Size size)
    {
        var most = Math.Max(0, Math.Min(size.Width, size.Height) / 2);
        return new CornerRadius(Math.Min(r.TopLeft, most), Math.Min(r.TopRight, most), Math.Min(r.BottomRight, most), Math.Min(r.BottomLeft, most));
    }
}
