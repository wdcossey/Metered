using Avalonia;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Metered.Controls;

/// <summary>
/// The input's sideways scroll gesture (#117): Avalonia's scroll gesture, taken only by a drag that is more sideways than
/// up or down when it has gone the gesture's start distance, so that a drag mostly up or down is the page's, as a browser
/// gives a touch's scroll to the scroller that moves the way it goes, even when its first move has a sideways part.
/// </summary>
/// <remarks>
/// Avalonia's recognizer, scrolling only sideways, starts on the first move that has gone its start distance sideways,
/// whatever it has gone up or down; and the text's scroller is inside the page's, so it sees each move first and keeps the
/// page's from starting at all.
/// </remarks>
internal sealed class InputScrollGestureRecognizer : ScrollGestureRecognizer
{
    private IPointer? _pointer;
    private Point _pressedAt;
    private bool? _sideways;

    protected override void PointerPressed(PointerPressedEventArgs e)
    {
        base.PointerPressed(e);
        // The pointers Avalonia's gesture takes: a finger's or a pen's.
        if (e.Pointer.Type is not (PointerType.Touch or PointerType.Pen)) return;
        _pointer = e.Pointer;
        _pressedAt = e.GetPosition(null);
        _sideways = null;
    }

    protected override void PointerMoved(PointerEventArgs e)
    {
        if (e.Pointer == _pointer && _sideways is null)
        {
            var moved = e.GetPosition(null) - _pressedAt;
            if (Math.Max(Math.Abs(moved.X), Math.Abs(moved.Y)) > ScrollStartDistance)
                _sideways = Math.Abs(moved.X) > Math.Abs(moved.Y);
        }

        // Not this drag's way: the rest of it is the page's.
        if (e.Pointer == _pointer && _sideways == false) return;
        base.PointerMoved(e);
    }
}
