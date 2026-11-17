using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Switch</c> (baseui 18.2.0, <c>switch/switch.js</c>): an Avalonia <see cref="ToggleButton"/> with Base
/// Web's props, its named parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>checked</c> is <c>IsChecked</c>, which a click, Space, or Enter on its release toggles (Base
/// Web's <c>onKeyUp</c>); <c>disabled</c> is <c>IsEnabled</c>; <c>children</c> is <c>Content</c>, the label.</para>
/// <para>The state pseudoclasses, set on the switch and on each part, are Base Web's <c>$</c> props: <c>:checked</c>,
/// <c>:size-default</c> and <c>:size-small</c>, <c>:label-placement-left</c> and <c>:label-placement-right</c>,
/// <c>:show-icon</c>, <c>:is-hovered</c> (a mouse over the switch), <c>:is-active</c> (pressed by any pointer, or Space
/// or Enter held: no style of Base Web's reads it, an override may), <c>:focus-visible</c> (focused by keyboard), and
/// <c>:pointer-coarse</c> (Base Web's <c>@media (pointer: coarse)</c>: a phone's touch screen). <c>:disabled</c> is
/// Avalonia's own. <c>:loaded</c> is set once the switch is loaded, so the knob's transition doesn't run on its first
/// appearance, as CSS's doesn't.</para>
/// <para>Base Web's <c>Input</c> part, the hidden <c>input</c>, is the switch itself.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(ToggleTrackPart, typeof(StyledElement))]
[TemplatePart(TogglePart, typeof(StyledElement))]
[TemplatePart(LabelPart, typeof(StyledElement))]
[PseudoClasses(":loaded", ":is-hovered", ":is-active", ":focus-visible", ":pointer-coarse", ":show-icon",
    ":size-default", ":size-small", ":label-placement-left", ":label-placement-right")]
public class Switch : ToggleButton
{
    public const string RootPart = "Root";
    public const string ToggleTrackPart = "ToggleTrack";
    public const string TogglePart = "Toggle";
    public const string LabelPart = "Label";

    public static readonly StyledProperty<SwitchSize> SizeProperty =
        AvaloniaProperty.Register<Switch, SwitchSize>(nameof(Size));

    public static readonly StyledProperty<SwitchLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<Switch, SwitchLabelPlacement>(nameof(LabelPlacement), SwitchLabelPlacement.Right);

    public static readonly StyledProperty<bool> ShowIconProperty =
        AvaloniaProperty.Register<Switch, bool>(nameof(ShowIcon));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Switch, OverrideCollection?>(nameof(Overrides));

    private readonly Parts _parts;
    private bool? _coarsePointer;
    private bool _mouseOver;
    private bool _pointerPressed;
    private bool _keyPressed;

    static Switch()
    {
        // Base Web's switch is on or off: no third state, whatever an app sets.
        IsThreeStateProperty.OverrideMetadata<Switch>(new StyledPropertyMetadata<bool>(false, coerce: (_, _) => false));
    }

    public Switch()
    {
        _parts = new Parts(this, "Switch", [RootPart, ToggleTrackPart, TogglePart, LabelPart]);
        SetState(":checked", IsChecked == true);
        UpdateSize();
        UpdatePlacement();
        SetState(":show-icon", ShowIcon);
        SetState(":pointer-coarse", CoarsePointer);
    }

    /// <summary>Base Web's <c>size</c>: the track, the knob and the label's type. Default by default.</summary>
    public SwitchSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>labelPlacement</c>: the label right of the track (the default) or left of it.</summary>
    public SwitchLabelPlacement LabelPlacement { get => GetValue(LabelPlacementProperty); set => SetValue(LabelPlacementProperty, value); }

    /// <summary>Base Web's <c>showIcon</c>: a checkmark on the knob while the switch is on.</summary>
    public bool ShowIcon { get => GetValue(ShowIconProperty); set => SetValue(ShowIconProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>Root</c>, <c>ToggleTrack</c>, <c>Toggle</c>, <c>Label</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>
    /// Base Web's <c>@media (pointer: coarse)</c>: true on a phone or tablet (Android, iOS), where the root gets the
    /// padding that makes a 48 px target. Null follows the platform; tests set it.
    /// </summary>
    internal bool CoarsePointer
    {
        get => _coarsePointer ?? (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS());
        set
        {
            _coarsePointer = value;
            SetState(":pointer-coarse", CoarsePointer);
        }
    }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override Type StyleKeyOverride => typeof(Switch);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        UpdateLabel();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetState(":loaded", true);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        SetState(":loaded", false);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        EndInput();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsCheckedProperty) SetState(":checked", IsChecked == true);
        else if (p == SizeProperty) UpdateSize();
        else if (p == LabelPlacementProperty) UpdatePlacement();
        else if (p == ShowIconProperty) SetState(":show-icon", ShowIcon);
        else if (p == ContentProperty) UpdateLabel();
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        // A disabled switch takes no input: a hover or a press it had would come back stale when it is enabled.
        else if (p == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) EndInput();
    }

    // Base Web's onKeyDown: Enter and Space make the switch active. Enter toggles it on its release (onKeyUp), not here
    // as an Avalonia button's does; Space is a checkbox's, which toggles on its release too.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space) SetKeyPressed(true);
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space) SetKeyPressed(false);
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            OnClick();
            return;
        }

        base.OnKeyUp(e);
    }

    // Base Web's $isHovered: onMouseEnter and onMouseLeave on the root. Metered's hover is a mouse's (ADR 0001).
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        TrackMouse(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        TrackMouse(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (e.Pointer.Type == PointerType.Mouse) LeaveMouse();
    }

    // Base Web's $isActive: onMouseDown on the root, any button or a finger; onMouseUp and onMouseLeave end it.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        SetPointerPressed(true);
    }

    // Avalonia captures the pointer on any press and lets it go on the release, wherever it is: the press ends there.
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        SetPointerPressed(false);
    }

    // Base Web's onFocus: isFocusVisible when the focus came by keyboard; onBlur ends it.
    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        // Only the switch's own focus: GotFocus also bubbles from a control in its label, which Base Web's input never sees.
        if (e.Source == this) SetState(":focus-visible", e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetState(":focus-visible", false);
        SetKeyPressed(false);
    }

    private void TrackMouse(PointerEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse) return;
        // Only a mouse over the switch: one that captured the pointer on a press sees it move outside too, and a
        // disabled one can still see a pointer it captured before.
        if (IsEffectivelyEnabled && new Rect(Bounds.Size).Contains(e.GetPosition(this))) SetMouseOver(true);
        else LeaveMouse();
    }

    /// <summary>
    /// Base Web's onMouseLeave: the hover ends, and so does a press. A mouse press always follows a hover here: Avalonia
    /// raises PointerEntered again when a switch under the mouse is enabled.
    /// </summary>
    private void LeaveMouse()
    {
        if (!_mouseOver) return;
        SetMouseOver(false);
        SetPointerPressed(false);
    }

    private void EndInput()
    {
        SetMouseOver(false);
        SetPointerPressed(false);
        SetKeyPressed(false);
    }

    private void SetMouseOver(bool over)
    {
        if (_mouseOver == over) return;
        _mouseOver = over;
        SetState(":is-hovered", over);
    }

    private void SetPointerPressed(bool pressed)
    {
        _pointerPressed = pressed;
        SetState(":is-active", _pointerPressed || _keyPressed);
    }

    private void SetKeyPressed(bool pressed)
    {
        _keyPressed = pressed;
        SetState(":is-active", _pointerPressed || _keyPressed);
    }

    private void UpdateSize()
    {
        SetState(":size-default", Size == SwitchSize.Default);
        SetState(":size-small", Size == SwitchSize.Small);
    }

    private void UpdatePlacement()
    {
        SetState(":label-placement-left", LabelPlacement == SwitchLabelPlacement.Left);
        SetState(":label-placement-right", LabelPlacement == SwitchLabelPlacement.Right);
    }

    /// <summary>Base Web renders the label only for children that are something (<c>children &amp;&amp;</c>): not for none or an empty string.</summary>
    private void UpdateLabel()
    {
        if (_parts[LabelPart] is Visual label) label.IsVisible = Content is not (null or "");
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }
}
