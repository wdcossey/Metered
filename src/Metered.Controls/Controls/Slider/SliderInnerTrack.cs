using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Slider"/>'s <c>InnerTrack</c> part: Base Web's 2 px line and the box react-range places
/// the marks and the thumbs in. It draws react-range's <c>getTrackBackground</c>, a gradient with a hard stop at each
/// thumb: <see cref="Fill"/>, the selection, from the start to one thumb or between two, and
/// <see cref="Background"/> for the rest, inside its rounded corners (<see cref="CornerRadius"/>).
/// </summary>
/// <remarks>
/// Its children are the slider's marks and then its thumbs, placed as react-range places them
/// (<c>react-range/lib/Range.js</c>): a thumb centred on its value along the line and on the line across it
/// (<c>getOffsets</c>: the line's width × the value's place − half the thumb); a mark at each step, at
/// <c>calculateMarkOffsets</c>' whole pixels (the line's width, truncated, ÷ the steps × the mark's index, less half the
/// mark, rounded). Both overflow the line, which doesn't clip them.
/// </remarks>
public class SliderInnerTrack : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Panel.BackgroundProperty.AddOwner<SliderInnerTrack>();

    public static readonly StyledProperty<IBrush?> FillProperty =
        AvaloniaProperty.Register<SliderInnerTrack, IBrush?>(nameof(Fill));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        Border.CornerRadiusProperty.AddOwner<SliderInnerTrack>();

    private IReadOnlyList<double> _positions = [];
    private int _marks;
    private double _steps;

    public SliderInnerTrack()
    {
        SliderChildren.Follow(Children, LogicalChildren, VisualChildren, InvalidateMeasure);
    }

    static SliderInnerTrack()
    {
        AffectsRender<SliderInnerTrack>(BackgroundProperty, FillProperty, CornerRadiusProperty);
    }

    /// <summary>The marks and thumbs (the line's), or the label's parts (a thumb's), in the order they are drawn.</summary>
    public Avalonia.Controls.Controls Children { get; } = new();

    /// <summary>The fill: the line's rest, or the handle's.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>The selection's colour.</summary>
    public IBrush? Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>The line's corners; a radius larger than half its height is drawn as half, as CSS draws it.</summary>
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }

    /// <summary>Each thumb's place along the line, from 0 (the minimum) to 1 (the maximum), in the value's order.</summary>
    internal IReadOnlyList<double> Positions
    {
        get => _positions;
        set
        {
            _positions = value;
            InvalidateArrange();
            InvalidateVisual();
        }
    }

    /// <summary>How many of the children, from the first, are marks; and react-range's <c>numOfMarks</c>, the steps they divide the line into.</summary>
    internal void SetMarks(int count, double steps)
    {
        _marks = count;
        _steps = steps;
        InvalidateArrange();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        foreach (var child in Children) child.Measure(Size.Infinity);
        return default;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var width = finalSize.Width;
        var height = finalSize.Height;
        for (var i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            var size = child.DesiredSize;
            if (i < _marks)
            {
                // calculateMarkOffsets: parseInt of the computed width and height, then Math.round.
                var x = JsRound(Math.Floor(width) / _steps * i - size.Width / 2);
                var y = -JsRound((size.Height - Math.Floor(height)) / 2);
                child.Arrange(new Rect(x, y, size.Width, size.Height));
            }
            else
            {
                var index = i - _marks;
                var at = index < _positions.Count ? _positions[index] : 0;
                // getOffsets, Direction.Right: the line has no padding, the thumb no margin.
                child.Arrange(new Rect(width * at - size.Width / 2, -(size.Height - height) / 2, size.Width, size.Height));
            }
        }

        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var box = new Rect(Bounds.Size);
        if (box.Width <= 0 || box.Height <= 0) return;
        var most = Math.Min(box.Width, box.Height) / 2;
        var r = CornerRadius;
        var line = new RoundedRect(box, Math.Min(r.TopLeft, most), Math.Min(r.TopRight, most), Math.Min(r.BottomRight, most), Math.Min(r.BottomLeft, most));
        using (context.PushClip(line))
        {
            if (Background is { } rest) context.DrawRectangle(rest, null, box);
            if (Fill is not { } fill || _positions.Count == 0) return;
            // getTrackBackground sorts the values: one thumb selects from the start to it, two between them.
            var sorted = _positions.Order().ToArray();
            var from = sorted.Length == 1 ? 0 : sorted[0];
            var to = sorted[^1];
            if (to > from) context.DrawRectangle(fill, null, new Rect(box.Width * from, 0, box.Width * (to - from), box.Height));
        }
    }

    /// <summary>JavaScript's Math.round: halves go up.</summary>
    internal static double JsRound(double value) => Math.Floor(value + 0.5);
}
