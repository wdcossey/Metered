using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="ProgressBar"/>'s <c>InfiniteBar</c> part, one half of the infinite bar: Base Web's
/// <c>background-image</c>, a linear gradient from transparent to <see cref="Fill"/> at <see cref="FillStart"/> percent,
/// <see cref="Fill"/> to <see cref="FillEnd"/>, and transparent again; <see cref="ImageSize"/> percent of the bar's width
/// wide (<c>background-size</c>), placed at <see cref="Position"/> (<c>background-position-x</c>: the image's point at
/// that percentage on the bar's point at that percentage), drawn once (<c>no-repeat</c>), clipped to the bar's corners.
/// The theme's animation moves <see cref="Position"/>.
/// </summary>
/// <remarks>
/// CSS interpolates a gradient's colours premultiplied, so its transparent ends fade the colour out, never through
/// black: here they are the fill's colour with no alpha. Base Web's gradient is symmetric (checked by the generator),
/// so the angle it gives each half (90 and 270 degrees) draws the same.
/// </remarks>
public class ProgressBarInfiniteBar : Control
{
    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<ProgressBarInfiniteBar, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<ProgressBarInfiniteBar>();

    public static readonly StyledProperty<double> ImageSizeProperty =
        AvaloniaProperty.Register<ProgressBarInfiniteBar, double>(nameof(ImageSize), 100);

    public static readonly StyledProperty<double> PositionProperty =
        AvaloniaProperty.Register<ProgressBarInfiniteBar, double>(nameof(Position));

    public static readonly StyledProperty<double> FillStartProperty =
        AvaloniaProperty.Register<ProgressBarInfiniteBar, double>(nameof(FillStart));

    public static readonly StyledProperty<double> FillEndProperty =
        AvaloniaProperty.Register<ProgressBarInfiniteBar, double>(nameof(FillEnd), 100);

    static ProgressBarInfiniteBar()
    {
        AffectsRender<ProgressBarInfiniteBar>(FillProperty, CornerRadiusProperty, ImageSizeProperty, PositionProperty,
            FillStartProperty, FillEndProperty);
    }

    /// <summary>The gradient's colour (a solid colour brush); nothing is drawn with none or another brush.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The box's corners, which clip the image.</summary>
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>The image's width, in percent of the bar's: CSS's <c>background-size</c>.</summary>
    public double ImageSize { get => GetValue(ImageSizeProperty); set => SetValue(ImageSizeProperty, value); }

    /// <summary>CSS's <c>background-position-x</c>, in percent.</summary>
    public double Position { get => GetValue(PositionProperty); set => SetValue(PositionProperty, value); }

    /// <summary>Where along the image the colour is whole, from: a percentage of its width.</summary>
    public double FillStart { get => GetValue(FillStartProperty); set => SetValue(FillStartProperty, value); }

    /// <summary>Where along the image the colour is whole, to: a percentage of its width.</summary>
    public double FillEnd { get => GetValue(FillEndProperty); set => SetValue(FillEndProperty, value); }

    /// <summary>The image's left edge, in the bar's coordinates: CSS's <c>(box − image) × position</c>.</summary>
    internal double ImageLeft => (Bounds.Width - ImageWidth) * Position / 100;

    internal double ImageWidth => Bounds.Width * ImageSize / 100;

    public override void Render(DrawingContext context)
    {
        if (Fill is not ISolidColorBrush fill || ImageWidth <= 0) return;
        var c = fill.Color;
        var colour = new Color((byte)Math.Round(c.A * fill.Opacity), c.R, c.G, c.B);
        var key = (colour, FillStart, FillEnd, ImageWidth);
        // The animation moves the position on every frame: the brush is made again only when what it is made of changes.
        if (_brush is null || _brushKey != key)
        {
            var clear = new Color(0, c.R, c.G, c.B);
            _brush = new ImmutableLinearGradientBrush(
                [
                    new ImmutableGradientStop(0, clear),
                    new ImmutableGradientStop(FillStart / 100, colour),
                    new ImmutableGradientStop(FillEnd / 100, colour),
                    new ImmutableGradientStop(1, clear),
                ],
                startPoint: new RelativePoint(0, 0, RelativeUnit.Absolute),
                endPoint: new RelativePoint(ImageWidth, 0, RelativeUnit.Absolute));
            _brushKey = key;
        }

        // The image from its left edge: the box drawn in the image's coordinates.
        var left = ImageLeft;
        var size = Bounds.Size;
        using (context.PushTransform(Matrix.CreateTranslation(left, 0)))
        {
            context.DrawRectangle(_brush, null, new RoundedRect(new Rect(-left, 0, size.Width, size.Height), RadioMark.Clamp(CornerRadius, size)));
        }
    }

    private ImmutableLinearGradientBrush? _brush;
    private (Color, double, double, double) _brushKey;
}
