using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

namespace Metered.Controls;

/// <summary>
/// The pointer states of Base Web's v2 controls with a backplate (radio-v2's <c>Radio</c>, checkbox-v2's
/// <c>Checkbox</c>), and of its <c>Switch</c>, whose handlers are checkbox-v2's and which has no backplate:
/// <c>:is-hovered</c>, Base Web's <c>$isHovered</c> (<c>onMouseEnter</c> on the root, a mouse's
/// only here: ADR 0001); <c>:is-active</c>, its <c>$isActive</c> (<c>onMouseDown</c>, any button or a finger, ended by
/// <c>onMouseUp</c>); and the backplate's own CSS <c>:hover</c> (a mouse over it, inside <c>@media (hover: hover)</c>)
/// and <c>:active</c> (the primary button or a finger pressed on it), which Base Web styles on that element alone; and
/// <c>:pointer-coarse</c>, Base Web's <c>@media (pointer: coarse)</c>. The control forwards its pointer events here; the
/// states it sets through <c>setState</c>, onto itself and its parts.
/// </summary>
/// <param name="owner">The control, whose bounds are the root's.</param>
/// <param name="backplate">The backplate part, when the template has one.</param>
/// <param name="setState">Sets a pseudoclass on the control and its parts.</param>
/// <param name="leaveEndsPress">Whether the pointer leaving ends <c>$isActive</c>: checkbox-v2's <c>onMouseLeave</c>
/// clears it (and the switch's), radio-v2's doesn't.</param>
internal sealed class BackplatePointer(Control owner, Func<StyledElement?> backplate, Action<string, bool> setState, bool leaveEndsPress)
{
    private bool _mouseOver;
    private bool _pressed;
    private bool _backplateHover;
    private bool _backplateActive;
    private bool? _coarse;

    /// <summary>
    /// Base Web's <c>@media (pointer: coarse)</c>: true on a phone or tablet (Android, iOS), where the root gets the
    /// padding that makes a 48 px target. Once set, it holds whatever the platform; tests set it.
    /// </summary>
    public bool Coarse
    {
        get => _coarse ?? (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
        set
        {
            _coarse = value;
            UpdateCoarse();
        }
    }

    /// <summary>Sets <c>:pointer-coarse</c> as <see cref="Coarse"/> is.</summary>
    public void UpdateCoarse() => setState(":pointer-coarse", Coarse);

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
        if (leaveEndsPress) SetPressed(false);
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
        var inside = new Rect(owner.Bounds.Size).Contains(e.GetPosition(owner));
        var mouse = e.Pointer.Type == PointerType.Mouse && owner.IsEffectivelyEnabled && inside;
        SetMouseOver(mouse);
        // Any pointer's leaving, a pen's or a finger's too, whose browser sends mouse events for them.
        if (!inside && leaveEndsPress) SetPressed(false);
        SetBackplate(mouse && OverBackplate(e), _backplateActive);
    }

    private bool OverBackplate(PointerEventArgs e) =>
        backplate() is Visual plate && new Rect(plate.Bounds.Size).Contains(e.GetPosition(plate));

    private void SetMouseOver(bool over)
    {
        if (_mouseOver == over) return;
        _mouseOver = over;
        setState(":is-hovered", over);
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
