using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// Base Web's spinning ring, as both its spinners draw it: a round box whose top border is <see cref="Foreground"/>
/// and whose other three are <see cref="Background"/>. A CSS border on a round box changes colour on its diagonals,
/// so the top quarter runs from 45° left of the top to 45° right of it. The element of a <see cref="Spinner"/>'s
/// <c>Spinner</c> part, and the base of a <see cref="Button"/>'s <see cref="ButtonLoadingSpinner"/>. Its theme turns it.
/// </summary>
/// <remarks>
/// Base Web's two style functions differ in their box: Button's <c>LoadingSpinner</c> is <c>box-sizing:
/// border-box</c>, its <c>width</c> the whole ring's, which is this element's own <c>Width</c> and <c>Height</c>; the
/// spinner's <c>StyledSpinner</c> sets no <c>box-sizing</c>, so CSS's content box puts its border outside its
/// <c>width</c>: that is <see cref="BoxSize"/>, and the ring measures <c>BoxSize + 2 × Thickness</c> across.
/// </remarks>
public class SpinnerRing : Control
{
    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<SpinnerRing, IBrush?>(nameof(Foreground));

    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<SpinnerRing, IBrush?>(nameof(Background));

    public static readonly StyledProperty<double> ThicknessProperty =
        AvaloniaProperty.Register<SpinnerRing, double>(nameof(Thickness), 2);

    public static readonly StyledProperty<double> BoxSizeProperty =
        AvaloniaProperty.Register<SpinnerRing, double>(nameof(BoxSize), double.NaN);

    static SpinnerRing()
    {
        AffectsRender<SpinnerRing>(ForegroundProperty, BackgroundProperty, ThicknessProperty);
        AffectsMeasure<SpinnerRing>(ThicknessProperty, BoxSizeProperty);
    }

    /// <summary>The top quarter's colour: Base Web's <c>borderTopColor</c>.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>The rest of the ring's colour: Base Web's other border colours.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The border's width.</summary>
    public double Thickness { get => GetValue(ThicknessProperty); set => SetValue(ThicknessProperty, value); }

    /// <summary>
    /// CSS's content box, inside the border: the spinner's <c>width</c> and <c>height</c>. When it is a number, the ring
    /// measures <c>BoxSize + 2 × Thickness</c> each way; unset (NaN, the default), it measures nothing of its own and
    /// takes its <c>Width</c> and <c>Height</c>, as Button's ring does.
    /// </summary>
    public double BoxSize { get => GetValue(BoxSizeProperty); set => SetValue(BoxSizeProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var box = BoxSize;
        if (double.IsNaN(box) || box < 0) return base.MeasureOverride(availableSize);
        var side = box + 2 * Math.Max(0, Thickness);
        return new Size(side, side);
    }

    public override void Render(DrawingContext context)
    {
        var size = Math.Min(Bounds.Width, Bounds.Height);
        var t = Thickness;
        var c = new Point(Bounds.Width / 2, Bounds.Height / 2);
        // The spinner's ring (a box of its own) takes the pointer over its whole disc, its hollow middle too, as the round
        // box Base Web draws its border on does (cursor: wait over all of it); a transparent fill draws nothing. Button's
        // ring, inside the button, is as it was.
        if (!double.IsNaN(BoxSize) && size > 0) context.DrawEllipse(Brushes.Transparent, null, c, size / 2, size / 2);
        if (size <= t || t <= 0) return;

        var r = (size - t) / 2;
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
