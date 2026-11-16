using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Button"/>'s <c>LoadingSpinner</c> part: Base Web's spinning
/// <c>span</c>, a circle whose border is <see cref="Background"/> but for its top quarter, in
/// <see cref="Foreground"/>. A CSS border on a round box changes colour on its diagonals, so the
/// top quarter runs from 45° left of the top to 45° right of it. The theme turns it.
/// </summary>
public class ButtonLoadingSpinner : Control
{
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<ButtonLoadingSpinner, IBrush?>(nameof(Foreground));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<ButtonLoadingSpinner, IBrush?>(nameof(Background));

    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<ButtonLoadingSpinner, double>(nameof(Thickness), 2);

    static ButtonLoadingSpinner()
    {
        AffectsRender<ButtonLoadingSpinner>(ForegroundProperty, BackgroundProperty, ThicknessProperty);
    }

    /// <summary>The top quarter's colour: Base Web's <c>borderTopColor</c>.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>The rest of the ring's colour: Base Web's other border colours.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The border's width: Base Web's <c>sizing.scale0</c>, 2.</summary>
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var t = Thickness;
        if (size <= t || t <= 0) return;

        var r = (size - t) / 2;
        var c = new Point(Bounds.Width / 2, Bounds.Height / 2);
        if (Background is { } background)
        {
            context.DrawEllipse(null, new Pen(background, t), c, r, r);
        }

        if (Foreground is { } foreground)
        {
            var geometry = new StreamGeometry();
            using (var g = geometry.Open())
            {
                var start = At(c, r, -135);
                g.BeginFigure(start, false);
                g.ArcTo(At(c, r, -45), new Size(r, r), 0, false, SweepDirection.Clockwise);
                g.EndFigure(false);
            }

            context.DrawGeometry(null, new Pen(foreground, t, lineCap: PenLineCap.Flat), geometry);
        }
    }

    private static Point At(Point c, double r, double degrees)
    {
        var a = degrees * Math.PI / 180;
        return new Point(c.X + r * Math.Cos(a), c.Y + r * Math.Sin(a));
    }
}
