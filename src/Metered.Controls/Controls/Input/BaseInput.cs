using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>BaseInput</c> (baseui 18.2.0, <c>input/base-input.js</c>), which its <c>Input</c> and <c>Textarea</c>
/// both render: an Avalonia <see cref="TextBox"/> with BaseInput's props, its parts (<c>InputContainer</c>,
/// <c>Input</c>, <c>Before</c>, <c>After</c>, <c>ClearIconContainer</c>, <c>ClearIcon</c>, and for a password
/// <c>MaskToggleButton</c>, <c>MaskToggleShowIcon</c> and <c>MaskToggleHideIcon</c>), their states and overrides, and
/// what BaseInput does: the clear icon, Escape, the mask toggle. A control on it gives its own template and its own
/// parts (Input's root and enhancers).
/// </summary>
/// <remarks>
/// <para>Base Web's <c>value</c> is <c>Text</c>, <c>placeholder</c> <c>PlaceholderText</c>, <c>readOnly</c>
/// <c>IsReadOnly</c>, <c>maxLength</c> <c>MaxLength</c>, <c>disabled</c> <c>IsEnabled</c>, <c>onChange</c>
/// <c>TextChanged</c>: the caret, selection, clipboard, undo, the IME and the soft keyboard are Avalonia's
/// <see cref="TextBox"/>'s.</para>
/// <para>The state pseudoclasses, set on the control and on each part, are Base Web's <c>$</c> props (utils.js's
/// <c>getSharedProps</c>): <c>:size-mini</c>, <c>-compact</c>, <c>-default</c>, <c>-large</c>, <c>:is-focused</c> (the
/// text box has focus, by any means), <c>:error</c>, <c>:positive</c>, <c>:is-read-only</c>; <c>:disabled</c> is
/// Avalonia's own. The clear icon and the mask toggle take focus themselves (Base Web's <c>tabIndex={0}</c> and
/// <c>&lt;button&gt;</c>), and their own <c>:focus-visible</c> is Avalonia's, on a keyboard's focus.
/// <c>:loaded</c> is set once the control is loaded, so a transition doesn't run on its first appearance.</para>
/// <para>The parts that take focus are inside the text box's template, so the text box takes no key, text, focus or
/// pointer of theirs: what they do is theirs alone, as their own elements in Base Web.</para>
/// </remarks>
[TemplatePart(InputContainerPart, typeof(StyledElement))]
[TemplatePart(InputPart, typeof(StyledElement))]
[TemplatePart(BeforePart, typeof(StyledElement))]
[TemplatePart(AfterPart, typeof(StyledElement))]
[TemplatePart(ClearIconContainerPart, typeof(StyledElement))]
[TemplatePart(ClearIconPart, typeof(InputIcon))]
[TemplatePart(MaskToggleButtonPart, typeof(Avalonia.Controls.Button))]
[TemplatePart(MaskToggleShowIconPart, typeof(StyledElement))]
[TemplatePart(MaskToggleHideIconPart, typeof(StyledElement))]
[PseudoClasses(":loaded", ":size-mini", ":size-compact", ":size-default", ":size-large", ":is-focused", ":error", ":positive",
    ":is-read-only")]
public abstract class BaseInput : TextBox
{
    public const string InputContainerPart = "InputContainer";
    public const string InputPart = "Input";
    public const string BeforePart = "Before";
    public const string AfterPart = "After";
    public const string ClearIconContainerPart = "ClearIconContainer";
    public const string ClearIconPart = "ClearIcon";
    public const string MaskToggleButtonPart = "MaskToggleButton";
    public const string MaskToggleShowIconPart = "MaskToggleShowIcon";
    public const string MaskToggleHideIconPart = "MaskToggleHideIcon";

    /// <summary>base-input.js's clear icon's <c>aria-label</c> and <c>title</c>.</summary>
    public const string ClearLabel = "Clear value";

    /// <summary>base-input.js's mask toggle's <c>aria-label</c> and <c>title</c> while the text is masked.</summary>
    public const string ShowPasswordLabel = "Show password text";

    /// <summary>The same once the toggle has shown it.</summary>
    public const string HidePasswordLabel = "Hide password text";

    /// <summary>
    /// The character a masked password is drawn in: a browser's, not Base Web's (Chrome draws U+2022 for an
    /// <c>&lt;input type="password"&gt;</c>).
    /// </summary>
    public const char MaskCharacter = '•';

    public static readonly StyledProperty<InputSize> SizeProperty =
        AvaloniaProperty.Register<BaseInput, InputSize>(nameof(Size), InputSize.Default);

    public static readonly StyledProperty<bool> ErrorProperty =
        AvaloniaProperty.Register<BaseInput, bool>(nameof(Error));

    public static readonly StyledProperty<bool> PositiveProperty =
        AvaloniaProperty.Register<BaseInput, bool>(nameof(Positive));

    public static readonly StyledProperty<bool> ClearableProperty =
        AvaloniaProperty.Register<BaseInput, bool>(nameof(Clearable));

    public static readonly StyledProperty<bool> ClearOnEscapeProperty =
        AvaloniaProperty.Register<BaseInput, bool>(nameof(ClearOnEscape), true);

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<BaseInput, OverrideCollection?>(nameof(Overrides));

    public static readonly DirectProperty<BaseInput, bool> IsMaskedProperty =
        AvaloniaProperty.RegisterDirect<BaseInput, bool>(nameof(IsMasked), o => o.IsMasked);

    private readonly Parts _parts;
    private InputIcon? _clear;
    private Avalonia.Controls.Button? _toggle;
    private IPointer? _clearPress;
    private readonly Dictionary<string, (bool On, IDisposable Value)> _shown = new(StringComparer.Ordinal);
    private bool _maskState = true;
    private bool _masked;
    private bool _eatSpace;
    private IDisposable? _mask;
    private IDisposable? _maskKeyboard;
    private readonly List<IDisposable> _labels = [];
    private IDisposable[] _toggleLabel = [];

    /// <param name="component">The prefix of the parts' theme keys: <c>Input</c> for <c>Input.StyledRoot</c>.</param>
    /// <param name="parts">Every part of the control's template, by Base Web's names.</param>
    protected BaseInput(string component, string[] parts)
    {
        _parts = new Parts(this, component, parts);
        UpdateSize();
        SetState(":error", Error);
        SetState(":positive", Positive);
        SetState(":is-read-only", IsReadOnly);
        SetState(":is-focused", false);
        // Before the text box's flyout's own handler (added later, with ContextFlyout): a menu asked for on a control of
        // its own isn't the text box's.
        AddHandler(ContextRequestedEvent, OnContextRequestedFromPart);
        UpdateMask();
    }

    /// <summary>Base Web's <c>size</c>: mini, compact, default (its default) or large.</summary>
    public InputSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>error</c>. With <see cref="Positive"/> too, error is drawn, as Base Web draws it.</summary>
    public bool Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>positive</c>.</summary>
    public bool Positive { get => GetValue(PositiveProperty); set => SetValue(PositiveProperty, value); }

    /// <summary>
    /// Base Web's <c>clearable</c>: the clear icon, while there is text and the input is neither disabled nor
    /// read-only; and, with <see cref="ClearOnEscape"/>, Escape clears it.
    /// </summary>
    public bool Clearable { get => GetValue(ClearableProperty); set => SetValue(ClearableProperty, value); }

    /// <summary>Base Web's <c>clearOnEscape</c>, true by default: Escape in a clearable input that isn't read-only clears it.</summary>
    public bool ClearOnEscape { get => GetValue(ClearOnEscapeProperty); set => SetValue(ClearOnEscapeProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>
    /// base-input.js's <c>isMasked</c>: a password's text is drawn masked. Base Web starts it as whether the type is a
    /// password, and the toggle alone changes it; here it is whether the type is a password until the toggle is clicked,
    /// then the toggle's (docs/controls/Input, known differences).
    /// </summary>
    public bool IsMasked
    {
        get => _masked;
        private set => SetAndRaise(IsMaskedProperty, ref _masked, value);
    }

    /// <summary>Whether Base Web's <c>type</c> is <c>"password"</c>: the text is masked, and the toggle shown.</summary>
    protected virtual bool IsPassword => false;

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>Sets a state pseudoclass on the control and on every part.</summary>
    protected void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    /// <summary>
    /// Shows or hides a part, as Base Web renders it or doesn't: at the template's priority, so that an override's
    /// <c>IsVisible</c> (Base Web's <c>MaskToggleButton: () =&gt; null</c>) still hides it.
    /// </summary>
    /// <remarks>
    /// One value for each part, held and let go when it changes: a value set at a priority below the local one is a frame
    /// of its own, and frames would pile up on every keystroke.
    /// </remarks>
    protected void Show(string part, bool on)
    {
        if (_parts[part] is not { } element) return;
        if (_shown.TryGetValue(part, out var held))
        {
            if (held.On == on) return;
            held.Value.Dispose();
        }

        _shown[part] = (on, element.SetValue(IsVisibleProperty, on, BindingPriority.Template)!);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Release();
        foreach (var held in _shown.Values) held.Value.Dispose();
        _shown.Clear();
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        _clear = _parts[ClearIconPart] as InputIcon;
        _toggle = _parts[MaskToggleButtonPart] as Avalonia.Controls.Button;
        if (_clear is not null)
        {
            _clear.AddHandler(PointerPressedEvent, OnClearPressed);
            _clear.AddHandler(PointerReleasedEvent, OnClearReleased);
            _clear.AddHandler(PointerCaptureLostEvent, OnClearCaptureLost);
            _clear.AddHandler(KeyDownEvent, OnClearKeyDown);
            _clear.AddHandler(TextInputMethodClientRequestedEvent, OnPartTextInputClient);
            _clear.Invoked = () =>
            {
                // Only what a click can do: the icon is there.
                if (CanClear) OnClearIconClick();
            };
            // base-input.js's aria-label and title, which an override's props replace: at the template's priority.
            _labels.Add(_clear.SetValue(AutomationProperties.NameProperty, ClearLabel, BindingPriority.Template)!);
            _labels.Add(_clear.SetValue(ToolTip.TipProperty, ClearLabel, BindingPriority.Template)!);
        }

        if (_toggle is not null)
        {
            _toggle.Click += OnToggleClick;
            _toggle.AddHandler(TextInputMethodClientRequestedEvent, OnPartTextInputClient);
        }

        UpdateClear();
        UpdateMask();
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
        if (p == SizeProperty) UpdateSize();
        else if (p == ErrorProperty) SetState(":error", Error);
        else if (p == PositiveProperty) SetState(":positive", Positive);
        else if (p == IsFocusedProperty) SetState(":is-focused", IsFocused);
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        else if (p == IsReadOnlyProperty)
        {
            SetState(":is-read-only", IsReadOnly);
            UpdateClear();
        }
        else if (p == TextProperty || p == ClearableProperty || p == IsEffectivelyEnabledProperty) UpdateClear();
    }

    // base-input.js's onInputKeyDown, a listener on the input alone, added when it is clearable: Escape clears a value
    // that isn't read-only, and goes no further (stopPropagation). A key on a part of the template that takes focus (the
    // clear icon, the toggle) isn't the text box's.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Source != this) return;
        // Another key: the clearing Space's text, if a platform sent one, has come.
        _eatSpace = false;
        if (e.Key == Key.Escape && Clearable && ClearOnEscape && !IsReadOnly)
        {
            Clear();
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (e.Key == Key.Space) _eatSpace = false;
        if (e.Source != this) return;
        base.OnKeyUp(e);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        if (e.Source != this) return;
        // The Space that cleared from the clear icon focused the text box on its key down: its text, which a platform sends
        // after the key, isn't typed (Base Web's preventDefault).
        if (_eatSpace && e.Text == " ")
        {
            _eatSpace = false;
            e.Handled = true;
            return;
        }

        base.OnTextInput(e);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        if (e.Source != this) return;
        base.OnGotFocus(e);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        if (e.Source != this) return;
        _eatSpace = false;
        base.OnLostFocus(e);
    }

    // The text box's pointer: a press moves its caret and takes the pointer (and so its release), a held finger selects a
    // word. Neither is a control of its own's.
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (FromOwnControl(e.Source)) return;
        base.OnPointerPressed(e);
    }

    protected override void OnHolding(HoldingRoutedEventArgs e)
    {
        if (FromOwnControl(e.Source)) return;
        base.OnHolding(e);
    }

    /// <summary>
    /// Whether an event comes from a control of its own inside the template: an element that takes focus (the clear
    /// icon, the toggle, an app's button in an enhancer), which the text box leaves alone.
    /// </summary>
    private bool FromOwnControl(object? source)
    {
        for (var v = source as Visual; v is not null && v != this; v = v.GetVisualParent())
        {
            if (v is InputElement { Focusable: true }) return true;
        }

        return false;
    }

    private void OnContextRequestedFromPart(object? sender, ContextRequestedEventArgs e)
    {
        if (FromOwnControl(e.Source)) e.Handled = true;
    }

    // A part that takes focus isn't a text box: the IME and the soft keyboard aren't the text box's while it has focus.
    private static void OnPartTextInputClient(object? sender, TextInputMethodClientRequestedEventArgs e) => e.Handled = true;

    // The clear icon's click: Base Web's onClick on the svg, a press and a release on it, by any pointer.
    private void OnClearPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_clear is null) return;
        var point = e.GetCurrentPoint(_clear);
        if (e.Pointer.Type == PointerType.Mouse && !point.Properties.IsLeftButtonPressed) return;
        e.Handled = true;
        // One pointer's click at a time: another finger on it meanwhile isn't a second.
        if (_clearPress is not null) return;
        _clearPress = e.Pointer;
        e.Pointer.Capture(_clear);
        e.Handled = true;
    }

    private void OnClearReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_clear is null || _clearPress != e.Pointer) return;
        _clearPress = null;
        e.Pointer.Capture(null);
        e.Handled = true;
        if (new Rect(_clear.Bounds.Size).Contains(e.GetPosition(_clear))) OnClearIconClick();
    }

    private void OnClearCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_clearPress == e.Pointer) _clearPress = null;
    }

    // The clear icon's onKeyDown: Enter or Space clears (preventDefault: Space doesn't scroll).
    private void OnClearKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Enter or Key.Space)) return;
        e.Handled = true;
        _eatSpace = e.Key == Key.Space;
        OnClearIconClick();
    }

    // base-input.js's onClearIconClick: clear, then focus the input.
    private void OnClearIconClick()
    {
        Clear();
        Focus();
    }

    private void OnToggleClick(object? sender, RoutedEventArgs e)
    {
        _maskState = !IsMasked;
        UpdateMask();
    }

    /// <summary>The handlers of the parts of the template going.</summary>
    private void Release()
    {
        if (_clear is not null)
        {
            _clear.RemoveHandler(PointerPressedEvent, OnClearPressed);
            _clear.RemoveHandler(PointerReleasedEvent, OnClearReleased);
            _clear.RemoveHandler(PointerCaptureLostEvent, OnClearCaptureLost);
            _clear.RemoveHandler(KeyDownEvent, OnClearKeyDown);
            _clear.RemoveHandler(TextInputMethodClientRequestedEvent, OnPartTextInputClient);
            _clear.Invoked = null;
        }

        if (_toggle is not null)
        {
            _toggle.Click -= OnToggleClick;
            _toggle.RemoveHandler(TextInputMethodClientRequestedEvent, OnPartTextInputClient);
        }

        _clear = null;
        _toggle = null;
        _clearPress = null;
        foreach (var label in _labels) label.Dispose();
        _labels.Clear();
        foreach (var label in _toggleLabel) label.Dispose();
        _toggleLabel = [];
    }

    private void UpdateSize()
    {
        SetState(":size-mini", Size == InputSize.Mini);
        SetState(":size-compact", Size == InputSize.Compact);
        SetState(":size-default", Size == InputSize.Default);
        SetState(":size-large", Size == InputSize.Large);
    }

    /// <summary>base-input.js's renderClear: the icon only for a clearable input with a value, not disabled or read-only.</summary>
    private void UpdateClear()
    {
        Show(ClearIconContainerPart, CanClear);
        Show(ClearIconPart, CanClear);
    }

    private bool CanClear => Clearable && IsEffectivelyEnabled && !IsReadOnly && !string.IsNullOrEmpty(Text);

    /// <summary>
    /// base-input.js's getInputType and renderMaskToggle: a password masked until the toggle shows it, the toggle and its
    /// label, the show icon while masked and the hide icon after. The text is masked as a browser masks it; the soft
    /// keyboard is told it's a password while masked, as a browser tells it of <c>type="password"</c>.
    /// </summary>
    protected void UpdateMask()
    {
        var password = IsPassword;
        IsMasked = password && _maskState;
        var masked = IsMasked;
        // At the template's priority, held to be taken away: an app's own value (a local PasswordChar) still wins.
        if (masked && _mask is null)
        {
            _mask = SetValue(PasswordCharProperty, MaskCharacter, BindingPriority.Template);
            _maskKeyboard = SetValue(TextInputOptions.ContentTypeProperty, TextInputContentType.Password, BindingPriority.Template);
        }
        else if (!masked && _mask is not null)
        {
            _mask.Dispose();
            _maskKeyboard!.Dispose();
            _mask = _maskKeyboard = null;
        }

        Show(MaskToggleButtonPart, password);
        Show(MaskToggleShowIconPart, masked);
        Show(MaskToggleHideIconPart, password && !masked);
        if (_toggle is not null)
        {
            var label = IsMasked ? ShowPasswordLabel : HidePasswordLabel;
            foreach (var held in _toggleLabel) held.Dispose();
            _toggleLabel =
            [
                _toggle.SetValue(AutomationProperties.NameProperty, label, BindingPriority.Template)!,
                _toggle.SetValue(ToolTip.TipProperty, label, BindingPriority.Template)!,
            ];
        }
    }
}
