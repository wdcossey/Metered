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
/// <c>GroupName</c>. In a <see cref="RadioGroup"/>, the group's value checks it, and the group's align, label
/// placement and error are drawn in place of the radio's own, which stay as they are, as Base Web's
/// <c>groupContext?.align ?? align</c>.</para>
/// <para>The state pseudoclasses, set on the radio and on each part, are Base Web's <c>$</c> props:
/// <c>:checked</c>, <c>:error</c>, <c>:align-*</c>, <c>:label-placement-*</c>, <c>:is-hovered</c> (a mouse over
/// the radio), <c>:is-active</c> (pressed by any pointer), <c>:focus-visible</c> (focused by keyboard, in a group:
/// a radio on its own takes it from its app in Base Web, and never shows it here), and <c>:pointer-coarse</c>
/// (Base Web's <c>@media (pointer: coarse)</c>: a phone's touch screen). <c>:disabled</c> is Avalonia's own.
/// The <c>RadioBackplate</c> part alone also takes CSS's <c>:hover</c> (a mouse over the backplate) and
/// <c>:active</c> (the primary button or a finger pressed on it), which Base Web styles on that element.
/// <c>:in-group</c> is set while a <see cref="RadioGroup"/> holds the radio. <c>:loaded</c> is set once the radio is loaded, so the mark's transition doesn't run on its first appearance, as
/// CSS's doesn't.</para>
/// <para>Base Web's <c>Input</c> part, the hidden <c>input</c>, is the radio itself.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(RadioBackplatePart, typeof(StyledElement))]
[TemplatePart(RadioMarkOuterPart, typeof(StyledElement))]
[TemplatePart(RadioMarkInnerPart, typeof(StyledElement))]
[TemplatePart(LabelWrapperPart, typeof(StyledElement))]
[TemplatePart(LabelPart, typeof(StyledElement))]
[TemplatePart(DescriptionPart, typeof(StyledElement))]
[PseudoClasses(":loaded", ":in-group", ":error", ":is-hovered", ":is-active", ":focus-visible", ":pointer-coarse",
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
    private RadioGroup? _group;
    private readonly BackplatePointer _pointer;

    public Radio()
    {
        _parts = new Parts(this, "Radio",
            [RootPart, RadioBackplatePart, RadioMarkOuterPart, RadioMarkInnerPart, LabelWrapperPart, LabelPart, DescriptionPart]);
        _pointer = new BackplatePointer(this, () => _parts[RadioBackplatePart], SetState, leaveEndsPress: false);
        UpdateContext();
        SetState(":checked", IsChecked == true);
        _pointer.UpdateCoarse();
    }

    /// <summary>Base Web's <c>value</c>: what a <see cref="RadioGroup"/>'s value is when this radio is checked.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Base Web's <c>description</c>: text under the label; none when null or empty.</summary>
    public string? Description { get => GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

    /// <summary>Base Web's <c>align</c>. Vertical by default; horizontal holds the label and description to 240 wide
    /// and three lines. In a group, the group's is drawn.</summary>
    public RadioAlign Align { get => GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

    /// <summary>Base Web's <c>labelPlacement</c>. Right by default. In a group, the group's is drawn.</summary>
    public RadioLabelPlacement LabelPlacement { get => GetValue(LabelPlacementProperty); set => SetValue(LabelPlacementProperty, value); }

    /// <summary>Base Web's <c>error</c>. In a group, the group's is drawn.</summary>
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
        get => _pointer.Coarse;
        set => _pointer.Coarse = value;
    }

    /// <summary>The group the radio is in, whose context it draws and which sets its focus ring; null on its own.</summary>
    internal RadioGroup? Group
    {
        get => _group;
        set
        {
            _group = value;
            PseudoClasses.Set(":in-group", value is not null);
            UpdateContext();
        }
    }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override Type StyleKeyOverride => typeof(Radio);

    /// <summary>
    /// The align, label placement and error drawn: the group's, in a group, or the radio's own (Base Web's
    /// <c>groupContext?.align ?? alignProp</c>, whose group always gives one).
    /// </summary>
    internal void UpdateContext()
    {
        SetContextStates(SetState, _group?.Align ?? Align, _group?.LabelPlacement ?? LabelPlacement, _group?.Error ?? Error);
    }

    /// <summary>The pseudoclasses of an align, a label placement and an error, a radio's or a group's.</summary>
    internal static void SetContextStates(Action<string, bool> set, RadioAlign align, RadioLabelPlacement placement, bool error)
    {
        set(":align-vertical", align == RadioAlign.Vertical);
        set(":align-horizontal", align == RadioAlign.Horizontal);
        var on = Placements[(int)placement];
        foreach (var p in Placements) set(p, p == on);
        set(":error", error);
    }

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
        if (p == IsCheckedProperty) SetState(":checked", IsChecked == true);
        else if (p == AlignProperty || p == LabelPlacementProperty || p == ErrorProperty) UpdateContext();
        else if (p == ValueProperty) _group?.Refresh(this);
        else if (p == ContentProperty) UpdateLabel();
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        // A disabled radio takes no pointer: a hover or a press it had would come back stale when it is enabled.
        else if (p == IsEffectivelyEnabledProperty && !IsEffectivelyEnabled) _pointer.End();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        // A browser's radio doesn't check on Enter (it submits the form); Space checks it, as Avalonia's does.
        if (e.Key == Key.Enter) return;
        base.OnKeyDown(e);
    }

    // Base Web's $isHovered and $isActive, and the backplate's CSS :hover and :active (BackplatePointer).
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
        // Only the radio's own focus: GotFocus also bubbles from a control in its label, which Base Web's input never sees.
        if (e.Source == this) SetState(":focus-visible", _group is not null && e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetState(":focus-visible", false);
    }

    /// <summary>Base Web renders the label wrapper only for children that are something (<c>!!children</c>): not for none or an empty string.</summary>
    private void UpdateLabel()
    {
        if (_parts[LabelWrapperPart] is Visual wrapper) wrapper.IsVisible = Content is not (null or "");
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }
}
