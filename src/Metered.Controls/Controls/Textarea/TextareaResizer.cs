using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// A browser's resize grip in a textarea's end corner, which CSS's <c>resize</c> gives it (Base Web has no part for it):
/// a drag on it, by any pointer, makes its <see cref="TextareaField"/> wider or narrower (<see cref="TextareaResize.Both"/>
/// and <see cref="TextareaResize.Horizontal"/>), taller or shorter (<c>Both</c> and <c>Vertical</c>), as a browser sets
/// the textarea's own width and height. Hidden for <see cref="TextareaResize.None"/>.
/// </summary>
/// <remarks>
/// <para>The size, the field's <see cref="TextareaField.ResizedWidth"/> and <see cref="TextareaField.ResizedHeight"/>,
/// stays within its <c>MinWidth</c>, <c>MaxWidth</c>, <c>MinHeight</c> and <c>MaxHeight</c> (Base Web's docs: an override's
/// minimum and maximum dimensions), never smaller than the grip, and no wider than the width the field is given: a browser
/// lets it overflow its parent; here the root would clip it, grip and all. Right to left, the grip is in the bottom-left
/// corner, and a drag outward, to the left, widens it.</para>
/// <para>Its look is a browser's, not Base Web's: two diagonal strokes, as Chrome draws its resizer, in
/// <see cref="Foreground"/>. A drag on it is the grip's alone: it isn't the text's press, and no scroller pans.</para>
/// <para>Nor does a press or a release on it focus the textarea, by any pointer: a desktop browser's grip leaves the focus
/// where it was (Chrome's does), and on a phone the focus would open the soft keyboard over the textarea just resized
/// (#111). The focus stays where it was, the textarea's too.</para>
/// </remarks>
public class TextareaResizer : Control, ICustomHitTest
{
    public static readonly StyledProperty<TextareaResize> DirectionProperty =
        AvaloniaProperty.Register<TextareaResizer, TextareaResize>(nameof(Direction));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        AvaloniaProperty.Register<TextareaResizer, IBrush?>(nameof(Foreground));

    private IPointer? _dragging;
    private Point _from;
    private Size _start;
    private RoutedEventArgs? _passed;

    static TextareaResizer()
    {
        AffectsRender<TextareaResizer>(ForegroundProperty);
    }

    public TextareaResizer()
    {
        AddHandler(PointerPressedEvent, OnPassedBy, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerReleasedEvent, OnPassedBy, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>Which way it drags: the field's <see cref="TextareaField.Resize"/>.</summary>
    public TextareaResize Direction { get => GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

    /// <summary>The strokes' colour.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>Whether a pointer is dragging it.</summary>
    public bool IsDragging => _dragging is not null;

    private TextareaField? _field;

    private TextareaField? Field => this.FindAncestorOfType<TextareaField>();

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == DirectionProperty)
        {
            Cursor = Direction switch
            {
                TextareaResize.Both => new Cursor(StandardCursorType.BottomRightCorner),
                TextareaResize.Horizontal => new Cursor(StandardCursorType.SizeWestEast),
                TextareaResize.Vertical => new Cursor(StandardCursorType.SizeNorthSouth),
                _ => null,
            };
        }
    }

    public override void Render(DrawingContext context)
    {
        if (Foreground is not { } brush) return;
        var pen = new Pen(brush, 1);
        var (w, h) = (Bounds.Width, Bounds.Height);
        // Chrome's: two strokes across the corner, the longer outside.
        context.DrawLine(pen, new Point(w - 11, h - 2), new Point(w - 2, h - 11));
        context.DrawLine(pen, new Point(w - 6, h - 2), new Point(w - 2, h - 6));
    }

    /// <summary>
    /// A press or a release on a grip, on its way down at an element round it (its textarea): marked handled, so that
    /// Avalonia's focus manager, whose class handler at the grip focuses the first focusable element from it up (the
    /// textarea: a mouse's on its press, a finger's or a pen's on its release), passes it by. The grip unmarks it after
    /// that handler (below), so the rest of its way is as before.
    /// </summary>
    internal static void PassFocusBy(object? sender, PointerEventArgs e)
    {
        if (e.Source is not TextareaResizer grip || grip.Direction == TextareaResize.None) return;
        grip._passed = e;
        e.Handled = true;
    }

    // An instance handler, after the class handlers at the grip: the focus manager's has passed it by.
    private void OnPassedBy(object? sender, PointerEventArgs e)
    {
        if (!ReferenceEquals(_passed, e)) return;
        _passed = null;
        e.Handled = false;
    }

    /// <summary>Its whole box takes the pointer, drawn or not.</summary>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_dragging is not null || Direction == TextareaResize.None || Field is not { } field) return;
        if (e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragging = e.Pointer;
        _field = field;
        _from = e.GetPosition(null);
        _start = field.Bounds.Size;
        e.Pointer.Capture(this);
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging != e.Pointer || _field is not { } field) return;
        e.PreventGestureRecognition();
        e.Handled = true;
        var by = e.GetPosition(null) - _from;
        if (field.FlowDirection == FlowDirection.RightToLeft) by = by.WithX(-by.X);
        // Never smaller than the grip; layout keeps the rest, the field's minimum and maximum and the width it is given.
        if (Direction is TextareaResize.Both or TextareaResize.Horizontal) field.ResizedWidth = Math.Max(_start.Width + by.X, Bounds.Width);
        if (Direction is TextareaResize.Both or TextareaResize.Vertical) field.ResizedHeight = Math.Max(_start.Height + by.Y, Bounds.Height);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragging != e.Pointer) return;
        _dragging = null;
        _field = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragging != e.Pointer) return;
        _dragging = null;
        _field = null;
    }
}
