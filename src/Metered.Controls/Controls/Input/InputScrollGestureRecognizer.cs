using Avalonia;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;

namespace Metered.Controls;

/// <summary>
/// The input's sideways scroll gesture (#117): Avalonia's scroll gesture, taken only by a drag that, when it has gone the
/// gesture's start distance, is more sideways than up or down and goes a way the text can move, so that a drag mostly up
/// or down is the page's, as a browser gives a touch's scroll to the scroller that moves the way it goes, even when its
/// first move has a sideways part; and one that set off the way the text can't go stays off it when it turns back, as a
/// browser latches a touch's scroll when it starts.
/// </summary>
/// <remarks>
/// Avalonia's recognizer, scrolling only sideways, starts on the first move that has gone its start distance sideways,
/// whatever it has gone up or down, and on any later move that has gone that far from the press the way the text can move;
/// and the text's scroller is inside the page's, so it sees each move first and keeps the page's from starting at all. It
/// also follows its pointer after the release until the next press, which would let a pen's hover scroll the text.
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
        if (e.Pointer == _pointer)
        {
            if (_sideways is null)
            {
                var moved = e.GetPosition(null) - _pressedAt;
                if (Math.Max(Math.Abs(moved.X), Math.Abs(moved.Y)) > ScrollStartDistance)
                    _sideways = Math.Abs(moved.X) > Math.Abs(moved.Y) && CanScrollToward(moved.X);
            }

            // Not the text's: the rest of this drag is the page's.
            if (_sideways == false) return;
        }

        base.PointerMoved(e);
    }

    protected override void PointerReleased(PointerReleasedEventArgs e)
    {
        base.PointerReleased(e);
        // Lifted: nothing of it is the text's until it presses again (a fling goes on by itself).
        if (e.Pointer == _pointer) _sideways = false;
    }

    // Whether the text can move as a finger going this way along the line takes it: back towards its start for a finger
    // going right, on towards its end for one going left (Avalonia's recognizer starts only then).
    private bool CanScrollToward(double fingerX)
    {
        if (Offset is not { } offset || Viewport is not { } viewport || Extent is not { } extent) return false;
        return fingerX > 0 ? offset.X > 0 : offset.X + viewport.Width < extent.Width;
    }
}
