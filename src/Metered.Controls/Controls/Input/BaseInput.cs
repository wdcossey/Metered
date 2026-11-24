using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
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
/// <para>A finger's or a pen's drag on it scrolls, as on a phone's browser, and never moves the caret, by its moves or
/// where it lifts (Avalonia's text box would move it, and keep the page from panning): a tap does. The drag scrolls the
/// text where it can (an input's long value sideways, a textarea's lines), focusing nothing, and what is round it
/// otherwise; once it has moved the text it stays the text's until the finger lifts, as a browser latches a touch's
/// scroll. After a hold, that pointer's drag extends the hold's selection. A press still focuses it, and a mouse's drag
/// selects.</para>
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
[TemplatePart(ScrollViewerPart, typeof(ScrollViewer))]
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

    /// <summary>The text box's own text scroller (Avalonia's TextBox's part): not one of Base Web's parts.</summary>
    internal const string ScrollViewerPart = "PART_ScrollViewer";

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
    private IPointer? _held;
    private (IPointer Pointer, Point At)? _press;
    private IDisposable? _mask;
    private IDisposable? _maskKeyboard;
    private readonly List<IDisposable> _labels = [];
    private IDisposable[] _toggleLabel = [];
    private ScrollViewer? _scroller;
    private readonly HashSet<int> _latched = [];
    private TextPresenter? _textPresenter;
    private IPointer? _unfocused;

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
        AddHandler(PointerReleasedEvent, OnPointerReleasedTunnel, RoutingStrategies.Tunnel);
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
        _scroller = e.NameScope.Find<ScrollViewer>(ScrollViewerPart);
        _scroller?.AddHandler(ScrollGestureEvent, OnScrollGesture, handledEventsToo: true);
        _scroller?.AddHandler(ScrollGestureEndedEvent, OnScrollGestureEnded, handledEventsToo: true);
        _textPresenter = e.NameScope.Find<TextPresenter>("PART_TextPresenter");
        _textPresenter?.AddHandler(PointerReleasedEvent, OnPresenterReleasedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
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

    // The text box's pointer: a press moves its caret and takes the pointer (and so its release). Not a control of its
    // own's. (A finger's tap and hold on one still reach the text box: neither can be seen headless, #90.)
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        // A pointer's new press ends what its last one left (a release a scroller took never came here).
        if (_held == e.Pointer) _held = null;
        if (_press?.Pointer == e.Pointer) _press = null;
        if (FromOwnControl(e.Source)) return;
        // Where the finger is on the window, not on the input, which a page's scroll moves.
        if (e.Pointer.Type != PointerType.Mouse) _press = (e.Pointer, e.GetPosition(null));
        base.OnPointerPressed(e);
    }

    // A finger's or a pen's drag on the text is a scroll's, the page's (or a textarea's own), as in a browser on a phone,
    // where the caret moves by a tap or its handle (and Avalonia's scroll gesture takes a pen's as a finger's). Avalonia's
    // text box would take the drag to move its caret, and keep any scroller round it from panning (#94). After a hold, that
    // pointer's drag extends the selection the hold made, as the text box's does. A mouse's drag selects, as before.
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse && e.Pointer != _held) return;
        base.OnPointerMoved(e);
    }

    // The text box puts the caret where a finger lifts. Only where it lifts from a tap: a drag that nothing scrolled
    // (sideways, or in a page that can't move) isn't the caret's either, nor is a release whose press this didn't see
    // last (another finger pressed since). The text box's release does nothing else for a finger that wasn't held: its
    // touch modes are a hold's, and a press resets them.
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        Point? pressedAt = null;
        if (_press is { } press && press.Pointer == e.Pointer)
        {
            pressedAt = press.At;
            _press = null;
        }

        var held = _held == e.Pointer;
        if (held) _held = null;
        if (e.Pointer.Type != PointerType.Mouse && !held && !(pressedAt is { } at && IsTap(e, at))) return;
        base.OnPointerReleased(e);
    }

    // Nor does that drag focus it: a drag in a phone's browser focuses nothing, and Chrome's sideways scroll of a long value
    // leaves it as it was (#117). Avalonia's focus manager focuses a finger's text box on its release, wherever it moved,
    // from the text presenter the text box's press captured the pointer to: the release is marked handled on its way down,
    // so the focus manager passes it by there, and unmarked at the presenter after it (below), so the rest of its way is as
    // before: the text box's own release does nothing for it (above), and what is round it sees it. Only the text box's
    // own capture: a control inside with a press of its own (a resize grip) keeps its release, and a drag that a scroller
    // took (the page, or the text itself) has gone to the scroll gesture.
    private void OnPointerReleasedTunnel(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Mouse || _held == e.Pointer) return;
        if (_textPresenter is null || e.Pointer.Captured != _textPresenter) return;
        if (_press is not { } press || press.Pointer != e.Pointer || IsTap(e, press.At)) return;
        _unfocused = e.Pointer;
        e.Handled = true;
    }

    // After the focus manager's class handler at the presenter: the release goes on unhandled.
    private void OnPresenterReleasedTunnel(object? sender, PointerReleasedEventArgs e)
    {
        if (_unfocused != e.Pointer) return;
        _unfocused = null;
        e.Handled = false;
    }

    protected override void OnHolding(HoldingRoutedEventArgs e)
    {
        base.OnHolding(e);
        // Avalonia's hold is the last press's: here the text's own (a hold on a control of its own's had no press here).
        if (e.HoldingState == HoldingState.Started && !FromOwnControl(e.Source) && _press is { } press
            && press.Pointer.Type == e.PointerType)
        {
            _held = press.Pointer;
        }
    }

    // A browser's scroll latching: a touch's scroll that has moved the text (a textarea's lines, an input's long value
    // sideways) stays the text's until the finger lifts, and goes no further at its end. One that hasn't moved it (it was
    // at its end that way) goes on to the page. The presenter has already moved what it could: handled, by Avalonia's
    // ScrollContentPresenter, if it moved. A fling that has carried it to its end ends there (the recognizer reads
    // ShouldEndScrollGesture only while it flings), rather than going on unseen until it slows.
    private void OnScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        if (_scroller?.Presenter is not { } presenter || e.Source != presenter) return;
        if (e.Handled) _latched.Add(e.Id);
        else if (_latched.Contains(e.Id))
        {
            e.Handled = true;
            e.ShouldEndScrollGesture = true;
        }
    }

    private void OnScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e) => _latched.Remove(e.Id);

    // Avalonia's tap (Gestures): a release within the platform's tap size of the press, here on the window.
    private static bool IsTap(PointerReleasedEventArgs e, Point pressedAt)
    {
        var size = Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type) ?? new Size(4, 4);
        return new Rect(pressedAt, default(Size)).Inflate(new Thickness(size.Width, size.Height)).ContainsExclusive(e.GetPosition(null));
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
        // One pointer's click at a time: another finger on it meanwhile isn't a second. The text box leaves the press
        // alone (FromOwnControl), as it goes on up as Base Web's does.
        if (_clearPress is not null) return;
        _clearPress = e.Pointer;
        e.Pointer.Capture(_clear);
    }

    private void OnClearReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_clear is null || _clearPress != e.Pointer) return;
        _clearPress = null;
        e.Pointer.Capture(null);
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

        _scroller?.RemoveHandler(ScrollGestureEvent, OnScrollGesture);
        _scroller?.RemoveHandler(ScrollGestureEndedEvent, OnScrollGestureEnded);
        _scroller = null;
        _latched.Clear();
        _textPresenter?.RemoveHandler(PointerReleasedEvent, OnPresenterReleasedTunnel);
        _textPresenter = null;
        _unfocused = null;
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
