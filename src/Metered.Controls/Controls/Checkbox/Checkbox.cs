using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaCheckBox = Avalonia.Controls.CheckBox;

namespace Metered.Controls;

/// <summary>
/// Base Web's checkbox-v2 <c>LABEL_PLACEMENT</c>: where the label sits beside the mark. checkbox-v2 has right and
/// left only: its <c>constants.js</c> deprecates top and bottom.
/// </summary>
public enum CheckboxLabelPlacement
{
    /// <summary>Base Web's default.</summary>
    Right,

    Left,
}

/// <summary>
/// Base Web's checkbox-v2 <c>Checkbox</c> (baseui 18.2.0, <c>checkbox-v2/checkbox.js</c>): an Avalonia
/// <see cref="AvaloniaCheckBox"/> with Base Web's props, its named parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>checked</c> is <c>IsChecked</c>; <c>disabled</c> is <c>IsEnabled</c>; <c>children</c> is
/// <c>Content</c>, the label; <c>onChange</c> is <c>IsCheckedChanged</c> (and <c>Click</c>, <c>Command</c>). A click
/// or Space toggles <c>IsChecked</c>, as on Base Web's native input, and Enter toggles it when it is let go
/// (checkbox.js's <c>onKeyUp</c>). <see cref="IsIndeterminate"/> is Base Web's <c>isIndeterminate</c>, left as the app
/// set it by a click (Base Web's own stateful checkbox leaves it too); Avalonia's <c>IsChecked = null</c> is drawn
/// indeterminate as well.</para>
/// <para>The state pseudoclasses, set on the checkbox and on each part, are Base Web's <c>$</c> props:
/// <c>:checked</c>, <c>:is-indeterminate</c>, <c>:error</c>, <c>:label-placement-right</c> and <c>-left</c>,
/// <c>:is-hovered</c> (a mouse over the checkbox), <c>:is-active</c> (pressed by any pointer until it is let go or the
/// mouse leaves, or Space or Enter held), <c>:focus-visible</c> (focused by keyboard) and <c>:pointer-coarse</c>
/// (Base Web's <c>@media (pointer: coarse)</c>: a phone's touch screen). <c>:disabled</c> is Avalonia's own. The
/// <c>CheckmarkContainer</c> part alone also takes CSS's <c>:hover</c> (a mouse over it) and <c>:active</c> (the
/// primary button or a finger pressed on it), which Base Web styles on that element. <c>:loaded</c> is set once the
/// checkbox is loaded, so the mark's transition doesn't run on its first appearance, as CSS's doesn't.</para>
/// <para>Base Web's <c>Input</c> part, the hidden <c>input</c>, is the checkbox itself.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(CheckmarkContainerPart, typeof(StyledElement))]
[TemplatePart(CheckmarkPart, typeof(StyledElement))]
[TemplatePart(LabelPart, typeof(StyledElement))]
[PseudoClasses(":loaded", ":is-indeterminate", ":error", ":is-hovered", ":is-active", ":focus-visible", ":pointer-coarse",
    ":label-placement-right", ":label-placement-left")]
public class Checkbox : AvaloniaCheckBox
{
    public const string RootPart = "Root";
    public const string CheckmarkContainerPart = "CheckmarkContainer";
    public const string CheckmarkPart = "Checkmark";
    public const string LabelPart = "Label";

    public static readonly StyledProperty<CheckboxLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<Checkbox, CheckboxLabelPlacement>(nameof(LabelPlacement));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<Checkbox, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<bool> ErrorProperty =
        AvaloniaProperty.Register<Checkbox, bool>(nameof(Error));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Checkbox, OverrideCollection?>(nameof(Overrides));

    private readonly Parts _parts;
    private readonly BackplatePointer _pointer;
    private bool _enterDown;

    public Checkbox()
    {
        _parts = new Parts(this, "Checkbox", [RootPart, CheckmarkContainerPart, CheckmarkPart, LabelPart]);
        _pointer = new BackplatePointer(this, () => _parts[CheckmarkContainerPart], SetState, leaveEndsPress: true);
        UpdateChecked();
        UpdatePlacement();
        SetState(":error", Error);
        _pointer.UpdateCoarse();
    }

    /// <summary>Base Web's <c>labelPlacement</c>: right (its default) or left.</summary>
    public CheckboxLabelPlacement LabelPlacement { get => GetValue(LabelPlacementProperty); set => SetValue(LabelPlacementProperty, value); }

    /// <summary>Base Web's <c>isIndeterminate</c>: drawn half checked, whatever <c>IsChecked</c> is.</summary>
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

    /// <summary>Base Web's <c>error</c>.</summary>
    public bool Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>Root</c>, <c>CheckmarkContainer</c>, <c>Checkmark</c>, <c>Label</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>
    /// Base Web's <c>@media (pointer: coarse)</c>: true on a phone or tablet (Android, iOS), where the root gets the
    /// padding that makes a 48 px target. Null follows the platform; tests set it.
    /// </summary>
    internal bool CoarsePointer
    {
        get => _pointer.Coarse;
        set => _pointer.Coarse = value;
    }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override Type StyleKeyOverride => typeof(Checkbox);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        _pointer.TemplateApplied();
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
        _pointer.End();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsCheckedProperty || p == IsIndeterminateProperty) UpdateChecked();
        else if (p == ErrorProperty) SetState(":error", Error);
        else if (p == LabelPlacementProperty) UpdatePlacement();
        else if (p == ContentProperty) UpdateLabel();
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        // A disabled checkbox takes no pointer or key: a hover or a press it had would come back stale when it is enabled.
        else if (p == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) _pointer.End();
    }

    // checkbox.js's onKeyDown, on its input alone (a key in a control in the label isn't the checkbox's): Space or Enter
    // sets isActive. Space is the native input's own toggle, on its release, as Avalonia's; Enter's is checkbox.js's
    // onKeyUp, so its press doesn't click here. The checkbox takes the Enter it acts on: it doesn't reach a default
    // button too.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source != this) return;
        if (e.Key is Key.Space or Key.Enter) _pointer.SetPressed(true);
        if (e.Key == Key.Enter)
        {
            _enterDown = true;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    // checkbox.js's onKeyUp: Space or Enter ends isActive, and Enter toggles, as onChange with !checked: here only an
    // Enter pressed on the checkbox, not one pressed elsewhere whose release finds it focused.
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Source != this) return;
        if (e.Key is Key.Space or Key.Enter) _pointer.SetPressed(false);
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

    // Base Web's $isHovered and $isActive, and the CheckmarkContainer's CSS :hover and :active (BackplatePointer).
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

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        // Only the checkbox's own focus: GotFocus also bubbles from a control in its label, which Base Web's input never sees.
        if (e.Source == this) SetState(":focus-visible", e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        if (e.Source != this) return;
        SetState(":focus-visible", false);
        // A key held as the focus left is never let go here.
        _pointer.SetPressed(false);
        _enterDown = false;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new CheckboxAutomationPeer(this);

    /// <summary>A click, for the automation peer: it toggles, and raises <c>Click</c> and runs <c>Command</c>.</summary>
    internal void ClickFromAutomation() => OnClick();

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    private void UpdateChecked()
    {
        SetState(":checked", IsChecked == true);
        SetState(":is-indeterminate", IsIndeterminate || IsChecked is null);
    }

    private void UpdatePlacement()
    {
        SetState(":label-placement-right", LabelPlacement == CheckboxLabelPlacement.Right);
        SetState(":label-placement-left", LabelPlacement == CheckboxLabelPlacement.Left);
    }

    /// <summary>Base Web renders the label only for children that are something: not for none or an empty string.</summary>
    private void UpdateLabel()
    {
        if (_parts[LabelPart] is Visual label) label.IsVisible = Content is not (null or "");
    }
}
