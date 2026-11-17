using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using AvaloniaCheckBox = Avalonia.Controls.CheckBox;

namespace Metered.Controls;

/// <summary>Base Web's <c>STYLE_TYPE</c>, the Checkbox's <c>checkmarkType</c>.</summary>
public enum CheckboxStyleType
{
    /// <summary>A box with a tick.</summary>
    Default,

    /// <summary>A knob on a track. Base Web's <c>toggle_round</c> is the same value.</summary>
    Toggle,
}

/// <summary>
/// Base Web's <c>Checkbox</c> (baseui 18.2.0, <c>checkbox/checkbox.tsx</c>, the one baseweb.design
/// shows as Checkbox): an Avalonia <see cref="AvaloniaCheckBox"/> with Base Web's props, its named
/// parts and its overrides, as a box with a tick or, with <see cref="CheckmarkType"/> Toggle, a knob
/// on a track.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>checked</c> is <c>IsChecked</c>; <c>disabled</c> is <c>IsEnabled</c>;
/// <c>children</c> is <c>Content</c>, the label; <c>onChange</c> is <c>IsCheckedChanged</c> (and
/// <c>Click</c>, <c>Command</c>). A click or Space toggles <c>IsChecked</c>; Enter doesn't, as on
/// Base Web's native input. <see cref="IsIndeterminate"/> is Base Web's <c>isIndeterminate</c>,
/// left as the app set it by a click (Base Web's own stateful checkbox leaves it too); Avalonia's
/// <c>IsChecked = null</c> is drawn indeterminate as well.</para>
/// <para>The state pseudoclasses, set on the checkbox and on each part, are Base Web's <c>$</c>
/// props: <c>:checked</c>, <c>:is-indeterminate</c>, <c>:error</c>, <c>:label-top</c> (and right,
/// bottom, left), CSS's <c>:hover</c> (a mouse over it), <c>:active</c> (a mouse pressed on it, as Base Web's
/// <c>isActive</c> is a mouse's) and
/// <c>:focus-visible</c> (focus from the keyboard). <c>:disabled</c> is Avalonia's own.
/// <c>:loaded</c> is set once loaded, so that no transition runs on its first appearance.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(LabelPlacementPanel))]
[TemplatePart(CheckmarkPart, typeof(CheckboxMark))]
[TemplatePart(LabelPart, typeof(StyledElement))]
[TemplatePart(ToggleTrackPart, typeof(StyledElement))]
[TemplatePart(TogglePart, typeof(CheckboxMark))]
[PseudoClasses(":loaded", ":hover", ":active", ":focus-visible", ":is-indeterminate", ":error", ":toggle",
    ":label-top", ":label-right", ":label-bottom", ":label-left")]
public class Checkbox : AvaloniaCheckBox
{
    public const string RootPart = "Root";
    public const string CheckmarkPart = "Checkmark";
    public const string LabelPart = "Label";
    public const string ToggleTrackPart = "ToggleTrack";
    public const string TogglePart = "Toggle";

    public static readonly StyledProperty<CheckboxStyleType> CheckmarkTypeProperty =
        AvaloniaProperty.Register<Checkbox, CheckboxStyleType>(nameof(CheckmarkType), CheckboxStyleType.Default);

    public static readonly StyledProperty<LabelPlacement?> LabelPlacementProperty =
        AvaloniaProperty.Register<Checkbox, LabelPlacement?>(nameof(LabelPlacement));

    public static readonly StyledProperty<bool> IsIndeterminateProperty =
        AvaloniaProperty.Register<Checkbox, bool>(nameof(IsIndeterminate));

    public static readonly StyledProperty<bool> ErrorProperty =
        AvaloniaProperty.Register<Checkbox, bool>(nameof(Error));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Checkbox, OverrideCollection?>(nameof(Overrides));

    public static readonly DirectProperty<Checkbox, LabelPlacement> ActualLabelPlacementProperty =
        AvaloniaProperty.RegisterDirect<Checkbox, LabelPlacement>(nameof(ActualLabelPlacement), c => c.ActualLabelPlacement);

    private static readonly string[] Placements = [":label-top", ":label-right", ":label-bottom", ":label-left"];

    private readonly Parts _parts;
    private LabelPlacement _actualLabelPlacement = Controls.LabelPlacement.Right;
    private bool _mouseOver;
    private bool _mouseDown;
    private CheckboxAutomationPeer? _peer;

    public Checkbox()
    {
        _parts = new Parts(this, "Checkbox", [RootPart, CheckmarkPart, LabelPart, ToggleTrackPart, TogglePart]);
        UpdateAllStates();
    }

    /// <summary>Base Web's <c>checkmarkType</c>: a box with a tick, or a toggle.</summary>
    public CheckboxStyleType CheckmarkType { get => GetValue(CheckmarkTypeProperty); set => SetValue(CheckmarkTypeProperty, value); }

    /// <summary>Base Web's <c>labelPlacement</c>. Unset, it is right, or left for a toggle (<see cref="ActualLabelPlacement"/>).</summary>
    public LabelPlacement? LabelPlacement { get => GetValue(LabelPlacementProperty); set => SetValue(LabelPlacementProperty, value); }

    /// <summary>Where the label is: <see cref="LabelPlacement"/>, or Base Web's default for the <see cref="CheckmarkType"/>.</summary>
    public LabelPlacement ActualLabelPlacement { get => _actualLabelPlacement; private set => SetAndRaise(ActualLabelPlacementProperty, ref _actualLabelPlacement, value); }

    /// <summary>Base Web's <c>isIndeterminate</c>: drawn half checked, whatever <c>IsChecked</c> is.</summary>
    public bool IsIndeterminate { get => GetValue(IsIndeterminateProperty); set => SetValue(IsIndeterminateProperty, value); }

    /// <summary>Base Web's <c>error</c>.</summary>
    public bool Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>Root</c>, <c>Checkmark</c>, <c>Label</c>, <c>ToggleTrack</c>, <c>Toggle</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override Type StyleKeyOverride => typeof(Checkbox);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        UpdateStructure();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetState(":loaded", true);
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        SetState(":loaded", false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsCheckedProperty) UpdateChecked();
        else if (p == IsIndeterminateProperty) UpdateChecked();
        else if (p == ErrorProperty) SetState(":error", Error);
        else if (p == CheckmarkTypeProperty || p == LabelPlacementProperty) UpdatePlacement();
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        else if (p == ContentProperty) UpdateStructure();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // A native checkbox toggles on Space, not on Enter: Enter is left to bubble.
        if (e.Key == Key.Enter) return;
        base.OnKeyDown(e);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        SetMouseOver(e.Pointer.Type == PointerType.Mouse);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        SetMouseOver(e.Pointer.Type == PointerType.Mouse);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetMouseOver(false);
        // Base Web's onMouseLeave clears isActive too, though the button is still held.
        SetMouseDown(false);
    }

    // Base Web's isActive is a mouse's: onMouseDown and onMouseUp on the Root. Space on its input, and a
    // phone's tap (whose compatibility mousedown and mouseup come together after it), never show it.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (e.Pointer.Type == PointerType.Mouse) SetMouseDown(true);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        SetMouseDown(false);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        SetMouseDown(false);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => _peer = new CheckboxAutomationPeer(this);

    /// <summary>Avalonia's toggle, for the automation peer.</summary>
    internal void ToggleFromAutomation() => Toggle();

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        SetState(":focus-visible", e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetState(":focus-visible", false);
    }

    private void SetMouseDown(bool down)
    {
        if (_mouseDown == down) return;
        _mouseDown = down;
        SetState(":active", down);
    }

    private void SetMouseOver(bool over)
    {
        if (_mouseOver == over) return;
        _mouseOver = over;
        SetState(":hover", over);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    private void UpdateAllStates()
    {
        UpdateChecked();
        SetState(":error", Error);
        UpdatePlacement();
    }

    private void UpdateChecked()
    {
        SetState(":checked", IsChecked == true);
        var indeterminate = IsIndeterminate || IsChecked is null;
        SetState(":is-indeterminate", indeterminate);
        _peer?.StateChanged();
    }

    private void UpdatePlacement()
    {
        var toggle = CheckmarkType == CheckboxStyleType.Toggle;
        // checkbox.js: labelPlacement = checkmarkType === toggle ? 'left' : 'right' when unset.
        ActualLabelPlacement = LabelPlacement ?? (toggle ? Controls.LabelPlacement.Left : Controls.LabelPlacement.Right);
        // The toggle's template (Checkbox.axaml): Base Web renders the track or the checkmark, never both.
        PseudoClasses.Set(":toggle", toggle);
        var placement = ActualLabelPlacement switch
        {
            Controls.LabelPlacement.Top => ":label-top",
            Controls.LabelPlacement.Bottom => ":label-bottom",
            Controls.LabelPlacement.Left => ":label-left",
            _ => ":label-right",
        };
        foreach (var p in Placements) SetState(p, p == placement);
        UpdateStructure();
    }

    /// <summary>
    /// What Base Web renders: the label only when there is one, before the mark for the top and left
    /// placements. Structure, not look: set here, not by a theme.
    /// </summary>
    private void UpdateStructure()
    {
        if (_parts[LabelPart] is Visual label) label.IsVisible = Content is not (null or "");
        if (_parts[RootPart] is LabelPlacementPanel root)
        {
            root.LabelFirst = ActualLabelPlacement is Controls.LabelPlacement.Top or Controls.LabelPlacement.Left;
        }
    }
}
