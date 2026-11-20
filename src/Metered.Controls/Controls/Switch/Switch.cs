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
/// Web's <c>onKeyUp</c>): here only an Enter pressed on the switch, which it takes, as the checkbox does; <c>disabled</c>
/// is <c>IsEnabled</c>; <c>children</c> is <c>Content</c>, the label.</para>
/// <para>The state pseudoclasses, set on the switch and on each part, are Base Web's <c>$</c> props: <c>:checked</c>,
/// <c>:size-default</c> and <c>:size-small</c>, <c>:label-placement-left</c> and <c>:label-placement-right</c>,
/// <c>:show-icon</c>, <c>:is-hovered</c> (a mouse over the switch), <c>:is-active</c> (pressed by any pointer until it is
/// let go or the pointer leaves, or Space or Enter held, as the checkbox's: no style of Base Web's reads it; Metered's
/// pressed tint does, #68), <c>:focus-visible</c> (focused by keyboard), and <c>:pointer-coarse</c> (Base Web's
/// <c>@media (pointer: coarse)</c>: a phone's touch screen). The pointer states are <see cref="BackplatePointer"/>'s, as
/// the checkbox's are, with no backplate. <c>:disabled</c> is
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
    private readonly BackplatePointer _pointer;
    private bool _enterDown;
    private bool _keyDown;

    static Switch()
    {
        // Base Web's switch is on or off: no third state, whatever an app sets. A null IsChecked is off.
        IsThreeStateProperty.OverrideMetadata<Switch>(new StyledPropertyMetadata<bool>(false, coerce: (_, _) => false));
        IsCheckedProperty.OverrideMetadata<Switch>(new StyledPropertyMetadata<bool?>(false, coerce: (_, v) => v ?? false));
    }

    public Switch()
    {
        _parts = new Parts(this, "Switch", [RootPart, ToggleTrackPart, TogglePart, LabelPart]);
        // switch.js's mouse handlers are checkbox.js's: onMouseLeave ends isActive too. The switch has no backplate.
        _pointer = new BackplatePointer(this, () => null, SetState, leaveEndsPress: true);
        SetState(":checked", IsChecked == true);
        UpdateSize();
        UpdatePlacement();
        SetState(":show-icon", ShowIcon);
        _pointer.UpdateCoarse();
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
    /// padding that makes a 48 px target: the platform's until set; tests set it.
    /// </summary>
    internal bool CoarsePointer
    {
        get => _pointer.Coarse;
        set => _pointer.Coarse = value;
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
        EndPress();
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
        // A disabled switch takes no pointer or key: a hover or a press it had would come back stale when it is enabled.
        else if (p == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) EndPress();
    }

    // Base Web's onKeyDown: Enter and Space make the switch active. Enter toggles it on its release (onKeyUp), not here
    // as an Avalonia button's does; Space is a checkbox's, which toggles on its release too. Only the switch's own keys:
    // a key in a control in its label bubbles here, which Base Web's input never sees. The switch takes the Enter it
    // acts on, as the checkbox does: it doesn't reach a default button too.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source != this) return;
        if (e.Key is Key.Enter or Key.Space)
        {
            _keyDown = true;
            _pointer.SetPressed(true);
        }

        if (e.Key == Key.Enter)
        {
            _enterDown = true;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    // Base Web's onKeyUp: Space or Enter ends isActive, and Enter toggles, as onChange with !checked: here only an Enter
    // pressed on the switch, not one pressed elsewhere whose release finds it focused (the checkbox's rule, #49).
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Source != this) return;
        if (e.Key is Key.Enter or Key.Space)
        {
            _keyDown = false;
            _pointer.SetPressed(false);
        }

        if (e.Key == Key.Enter)
        {
            if (!_enterDown) return;
            _enterDown = false;
            e.Handled = true;
            OnClick();
            return;
        }

        base.OnKeyUp(e);
    }

    // Base Web's $isHovered (onMouseEnter and onMouseLeave on the root: a mouse's here, ADR 0001) and $isActive
    // (onMouseDown, any button or a finger; onMouseUp and onMouseLeave end it), as the checkbox's (BackplatePointer).
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _pointer.Entered(e);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        _pointer.Moved(e);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _pointer.Exited();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        _pointer.Pressed(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointer.CaptureLost();
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
        // Only the switch's own focus, as in OnGotFocus: LostFocus also bubbles from a control in its label.
        if (e.Source != this) return;
        SetState(":focus-visible", false);
        // A key held as the focus left is never let go here: its release, wherever it lands, isn't the switch's. A
        // pointer's press goes on.
        if (_keyDown) _pointer.SetPressed(false);
        _keyDown = _enterDown = false;
    }

    /// <summary>Every press ends, a key's too: the switch left the tree or was disabled.</summary>
    private void EndPress()
    {
        _pointer.End();
        _keyDown = _enterDown = false;
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
