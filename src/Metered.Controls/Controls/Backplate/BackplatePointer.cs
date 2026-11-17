using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Metered.Controls;

/// <summary>
/// The pointer states of Base Web's v2 controls with a backplate (radio-v2's <c>Radio</c>, checkbox-v2's
/// <c>Checkbox</c>): <c>:is-hovered</c>, Base Web's <c>$isHovered</c> (<c>onMouseEnter</c> on the root, a mouse's
/// only here: ADR 0001); <c>:is-active</c>, its <c>$isActive</c> (<c>onMouseDown</c>, any button or a finger, ended by
/// <c>onMouseUp</c>); and the backplate's own CSS <c>:hover</c> (a mouse over it, inside <c>@media (hover: hover)</c>)
/// and <c>:active</c> (the primary button or a finger pressed on it), which Base Web styles on that element alone. The
/// control forwards its pointer events here; the states it sets through <c>setState</c>, onto itself and its parts.
/// </summary>
/// <param name="owner">The control, whose bounds are the root's.</param>
/// <param name="backplate">The backplate part, when the template has one.</param>
/// <param name="setState">Sets a pseudoclass on the control and its parts.</param>
/// <param name="leaveEndsPress">Whether the mouse leaving ends <c>$isActive</c>: checkbox-v2's <c>onMouseLeave</c>
/// clears it, radio-v2's doesn't.</param>
internal sealed class BackplatePointer(Control owner, Func<StyledElement?> backplate, Action<string, bool> setState, bool leaveEndsPress)
{
    private bool _mouseOver;
    private bool _pressed;
    private bool _backplateHover;
    private bool _backplateActive;

    /// <summary>Base Web's <c>@media (pointer: coarse)</c> on this platform: a phone or tablet (Android, iOS).</summary>
    public static bool PlatformIsCoarse => OperatingSystem.IsAndroid() || OperatingSystem.IsIOS();

    /// <summary>A new template's backplate: it starts with neither CSS state.</summary>
    public void TemplateApplied()
    {
        _backplateHover = _backplateActive = false;
        UpdateBackplate();
    }

    public void Entered(PointerEventArgs e) => Track(e);

    public void Moved(PointerEventArgs e) => Track(e);

    public void Exited()
    {
        SetMouseOver(false);
        SetBackplate(false, _backplateActive);
    }

    public void Pressed(PointerPressedEventArgs e)
    {
        SetPressed(true);
        SetBackplate(_backplateHover, e.GetCurrentPoint(owner).Properties.IsLeftButtonPressed && OverBackplate(e));
    }

    /// <summary>
    /// Avalonia captures the pointer on any press, a right one included, and lets it go on the release, wherever it is:
    /// the press ends there, as Base Web's <c>onMouseUp</c> ends it.
    /// </summary>
    public void CaptureLost()
    {
        SetPressed(false);
        SetBackplate(_backplateHover, false);
    }

    /// <summary>Every state ends: the control left the tree, or was disabled, and takes no pointer to see it leave.</summary>
    public void End()
    {
        SetMouseOver(false);
        SetPressed(false);
        SetBackplate(false, false);
    }

    /// <summary>Base Web's <c>setIsActive</c>, which checkbox-v2's keys call too.</summary>
    public void SetPressed(bool pressed)
    {
        if (_pressed == pressed) return;
        _pressed = pressed;
        setState(":is-active", pressed);
    }

    private void Track(PointerEventArgs e)
    {
        // Only a mouse over the control: one that captured the pointer on a press sees it move outside too, and a
        // disabled one can still see a pointer it captured before.
        var mouse = e.Pointer.Type == PointerType.Mouse && owner.IsEffectivelyEnabled && new Rect(owner.Bounds.Size).Contains(e.GetPosition(owner));
        SetMouseOver(mouse);
        SetBackplate(mouse && OverBackplate(e), _backplateActive);
    }

    private bool OverBackplate(PointerEventArgs e) =>
        backplate() is Visual plate && new Rect(plate.Bounds.Size).Contains(e.GetPosition(plate));

    private void SetMouseOver(bool over)
    {
        if (_mouseOver == over) return;
        _mouseOver = over;
        setState(":is-hovered", over);
        if (!over && leaveEndsPress) SetPressed(false);
    }

    private void SetBackplate(bool hover, bool active)
    {
        if (_backplateHover == hover && _backplateActive == active) return;
        _backplateHover = hover;
        _backplateActive = active;
        UpdateBackplate();
    }

    /// <summary>The backplate's own CSS states, which Base Web doesn't pass to the other parts.</summary>
    private void UpdateBackplate()
    {
        if (backplate() is not { } plate) return;
        var classes = (IPseudoClasses)plate.Classes;
        classes.Set(":hover", _backplateHover);
        classes.Set(":active", _backplateActive);
    }
}
