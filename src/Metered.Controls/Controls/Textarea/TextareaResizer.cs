using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
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
/// <para>The size stays within the field's <c>MinWidth</c>, <c>MaxWidth</c>, <c>MinHeight</c> and <c>MaxHeight</c> (Base
/// Web's docs: an override's minimum and maximum dimensions), never smaller than the grip, and no wider than the room the
/// textarea has: a browser lets it overflow its parent; here the root would clip it, grip and all.</para>
/// <para>Its look is a browser's, not Base Web's: two diagonal strokes, as Chrome draws its resizer, in
/// <see cref="Foreground"/>. A drag on it is the grip's alone: it isn't the text's press, and no scroller pans.</para>
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
    private double _room;

    static TextareaResizer()
    {
        AffectsRender<TextareaResizer>(ForegroundProperty);
    }

    /// <summary>Which way it drags: the field's <see cref="TextareaField.Resize"/>.</summary>
    public TextareaResize Direction { get => GetValue(DirectionProperty); set => SetValue(DirectionProperty, value); }

    /// <summary>The strokes' colour.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>Whether a pointer is dragging it.</summary>
    public bool IsDragging => _dragging is not null;

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

    /// <summary>Its whole box takes the pointer, drawn or not.</summary>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (_dragging is not null || Direction == TextareaResize.None || Field is not { } field) return;
        if (e.Pointer.Type == PointerType.Mouse && !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) return;
        _dragging = e.Pointer;
        _from = e.GetPosition(null);
        _start = field.Bounds.Size;
        _room = Room(field);
        e.Pointer.Capture(this);
        e.PreventGestureRecognition();
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_dragging != e.Pointer || Field is not { } field) return;
        e.PreventGestureRecognition();
        e.Handled = true;
        var by = e.GetPosition(null) - _from;
        if (Direction is TextareaResize.Both or TextareaResize.Horizontal)
        {
            var most = Math.Min(field.MaxWidth, _start.Width + _room);
            field.Width = Math.Clamp(_start.Width + by.X, Math.Min(Math.Max(field.MinWidth, Bounds.Width), most), most);
        }

        if (Direction is TextareaResize.Both or TextareaResize.Vertical)
        {
            var least = Math.Max(field.MinHeight, Bounds.Height);
            field.Height = Math.Clamp(_start.Height + by.Y, Math.Min(least, field.MaxHeight), field.MaxHeight);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_dragging != e.Pointer) return;
        _dragging = null;
        e.Pointer.Capture(null);
        e.Handled = true;
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (_dragging == e.Pointer) _dragging = null;
    }

    /// <summary>How much wider the field can be: the textarea's width its root doesn't fill.</summary>
    private static double Room(TextareaField field)
    {
        if (field.FindAncestorOfType<Textarea>() is not { } textarea || textarea.Part(Textarea.RootPart) is not Visual root) return 0;
        return Math.Max(0, textarea.Bounds.Width - root.Bounds.Width);
    }
}
