using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Metered.Controls;

/// <summary>
/// Base Web's radio-v2 <c>Radio</c> (baseui 18.2.0, <c>radio-v2/radio.js</c>): an Avalonia <see cref="RadioButton"/>
/// with Base Web's props, its named parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>checked</c> is <c>IsChecked</c>, which a click sets, as an Avalonia radio button's;
/// <c>disabled</c> is <c>IsEnabled</c>; <c>children</c> is <c>Content</c>, the label; <c>name</c> is
/// <c>GroupName</c>. In a <see cref="RadioGroup"/>, the group's value, align, label placement and error replace the
/// radio's own, as Base Web's group context does.</para>
/// <para>The state pseudoclasses, set on the radio and on each part, are Base Web's <c>$</c> props:
/// <c>:checked</c>, <c>:error</c>, <c>:align-*</c>, <c>:label-placement-*</c>, <c>:is-hovered</c> (a mouse over
/// the radio), <c>:is-active</c> (pressed by any pointer), <c>:focus-visible</c> (focused by keyboard, in a group:
/// a radio on its own takes it from its app in Base Web, and never shows it here), and <c>:pointer-coarse</c>
/// (Base Web's <c>@media (pointer: coarse)</c>: a phone's touch screen). <c>:disabled</c> is Avalonia's own.
/// The <c>RadioBackplate</c> part alone also takes CSS's <c>:hover</c> (a mouse over the backplate) and
/// <c>:active</c> (pressed on it), which Base Web styles on that element. <c>:loaded</c> is set once the radio is
/// loaded, so the mark's transition doesn't run on its first appearance, as CSS's doesn't.</para>
/// <para>Base Web's <c>Input</c> part, the hidden <c>input</c>, is the radio itself.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(RadioBackplatePart, typeof(StyledElement))]
[TemplatePart(RadioMarkOuterPart, typeof(StyledElement))]
[TemplatePart(RadioMarkInnerPart, typeof(StyledElement))]
[TemplatePart(LabelWrapperPart, typeof(StyledElement))]
[TemplatePart(LabelPart, typeof(StyledElement))]
[TemplatePart(DescriptionPart, typeof(StyledElement))]
[PseudoClasses(":loaded", ":error", ":is-hovered", ":is-active", ":focus-visible", ":pointer-coarse",
    ":align-vertical", ":align-horizontal",
    ":label-placement-top", ":label-placement-right", ":label-placement-bottom", ":label-placement-left")]
public class Radio : RadioButton
{
    public const string RootPart = "Root";
    public const string RadioBackplatePart = "RadioBackplate";
    public const string RadioMarkOuterPart = "RadioMarkOuter";
    public const string RadioMarkInnerPart = "RadioMarkInner";
    public const string LabelWrapperPart = "LabelWrapper";
    public const string LabelPart = "Label";
    public const string DescriptionPart = "Description";

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<Radio, string?>(nameof(Value));

    public static readonly StyledProperty<string?> DescriptionProperty =
        AvaloniaProperty.Register<Radio, string?>(nameof(Description));

    public static readonly StyledProperty<RadioAlign> AlignProperty =
        AvaloniaProperty.Register<Radio, RadioAlign>(nameof(Align));

    public static readonly StyledProperty<RadioLabelPlacement> LabelPlacementProperty =
        AvaloniaProperty.Register<Radio, RadioLabelPlacement>(nameof(LabelPlacement), RadioLabelPlacement.Right);

    public static readonly StyledProperty<bool> ErrorProperty =
        AvaloniaProperty.Register<Radio, bool>(nameof(Error));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Radio, OverrideCollection?>(nameof(Overrides));

    private static readonly string[] Placements = [":label-placement-top", ":label-placement-right", ":label-placement-bottom", ":label-placement-left"];

    private readonly Parts _parts;
    private bool? _coarsePointer;
    private bool _mouseOver;
    private bool _pressed;
    private bool _backplateHover;
    private bool _backplateActive;

    public Radio()
    {
        _parts = new Parts(this, "Radio",
            [RootPart, RadioBackplatePart, RadioMarkOuterPart, RadioMarkInnerPart, LabelWrapperPart, LabelPart, DescriptionPart]);
        UpdateAlign();
        UpdateLabelPlacement();
        SetState(":error", Error);
        SetState(":checked", IsChecked == true);
        SetState(":pointer-coarse", CoarsePointer);
    }

    /// <summary>Base Web's <c>value</c>: what a <see cref="RadioGroup"/>'s value is when this radio is checked.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Base Web's <c>description</c>: text under the label; none when null or empty.</summary>
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Base Web's <c>align</c>. Vertical by default; horizontal holds the label and description to 240 wide and three lines.</summary>
    public RadioAlign Align { get => GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

    /// <summary>Base Web's <c>labelPlacement</c>. Right by default.</summary>
    public RadioLabelPlacement LabelPlacement { get => GetValue(LabelPlacementProperty); set => SetValue(LabelPlacementProperty, value); }

    /// <summary>Base Web's <c>error</c>.</summary>
    public bool Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>Root</c>, <c>RadioBackplate</c>, <c>RadioMarkOuter</c>,
    /// <c>RadioMarkInner</c>, <c>LabelWrapper</c>, <c>Label</c>, <c>Description</c>.</summary>
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

    /// <summary>The group the radio is in, which sets its focus ring; null on its own.</summary>
    internal RadioGroup? Group { get; set; }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override Type StyleKeyOverride => typeof(Radio);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        _backplateHover = _backplateActive = false;
        UpdateBackplate();
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsCheckedProperty) SetState(":checked", IsChecked == true);
        else if (p == AlignProperty) UpdateAlign();
        else if (p == LabelPlacementProperty) UpdateLabelPlacement();
        else if (p == ErrorProperty) SetState(":error", Error);
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // A browser's radio doesn't check on Enter (it submits the form); Space checks it, as Avalonia's does.
        if (e.Key == Key.Enter) return;
        base.OnKeyDown(e);
    }

    // Base Web's $isHovered and the backplate's CSS :hover: a mouse's only (the backplate's is inside
    // @media (hover: hover); Metered's hover is a mouse's throughout, ADR 0001).
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
        SetMouseOver(false);
        _backplateHover = false;
        UpdateBackplate();
    }

    // Base Web's $isActive (a press on the root) and the backplate's CSS :active (a press on the backplate), by any pointer.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        SetPressed(true);
        _backplateActive = OverBackplate(e);
        UpdateBackplate();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        EndPress();
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        EndPress();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        SetState(":focus-visible", Group is not null && e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetState(":focus-visible", false);
    }

    private void TrackMouse(PointerEventArgs e)
    {
        var mouse = e.Pointer.Type == PointerType.Mouse;
        SetMouseOver(mouse);
        _backplateHover = mouse && OverBackplate(e);
        UpdateBackplate();
    }

    private bool OverBackplate(PointerEventArgs e) =>
        _parts[RadioBackplatePart] is Visual backplate && new Rect(backplate.Bounds.Size).Contains(e.GetPosition(backplate));

    private void EndPress()
    {
        SetPressed(false);
        _backplateActive = false;
        UpdateBackplate();
    }

    private void SetMouseOver(bool over)
    {
        if (_mouseOver == over) return;
        _mouseOver = over;
        SetState(":is-hovered", over);
    }

    private void SetPressed(bool pressed)
    {
        if (_pressed == pressed) return;
        _pressed = pressed;
        SetState(":is-active", pressed);
    }

    /// <summary>The backplate's own CSS states, which Base Web doesn't pass to the other parts.</summary>
    private void UpdateBackplate()
    {
        if (_parts[RadioBackplatePart] is not { } backplate) return;
        var classes = (IPseudoClasses)backplate.Classes;
        classes.Set(":hover", _backplateHover);
        classes.Set(":active", _backplateActive);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    private void UpdateAlign()
    {
        SetState(":align-vertical", Align == RadioAlign.Vertical);
        SetState(":align-horizontal", Align == RadioAlign.Horizontal);
    }

    private void UpdateLabelPlacement()
    {
        var placement = Placements[(int)LabelPlacement];
        foreach (var p in Placements) SetState(p, p == placement);
    }
}
