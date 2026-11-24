using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Metered.Controls;

/// <summary>
/// The element of a multi select's <c>Tag</c> part (Base Web's <c>MultiValue</c> too): one value, as <c>multi-value.js</c>
/// renders Base Web's Tag for it: gray, primary, small, closeable, its Root's margins multi-value.js's. Only what the
/// select uses of Base Web's Tag (<c>tag/tag.js</c>), until Metered has the Tag control.
/// </summary>
/// <remarks>
/// <para>Parts, by Base Web's tag's names: <c>Root</c> (its box, which draws its overlay and its focus ring), <c>Text</c>,
/// <c>Action</c> and <c>ActionIcon</c>; their themes <c>SelectTag.Styled{Part}</c>, the tag's own <see cref="Overrides"/>
/// merged over. States on it and its parts: <c>:disabled</c>, <c>:focus-visible</c>, and its root's own CSS
/// <c>:hover</c> (a mouse's) and <c>:active</c> (pressed), which draw its overlay.</para>
/// <para>As tag.js: while it isn't disabled it is a focusable button, <c>Remove …</c> to UI Automation; a click or a tap on its
/// action removes it, and Backspace or Delete while it is focused.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(RadioMark))]
[TemplatePart(TextPart, typeof(TextBlock))]
[TemplatePart(ActionPart, typeof(Control))]
[TemplatePart(ActionIconPart, typeof(InputIcon))]
[PseudoClasses(":is-hovered", ":is-active")]
public class SelectTag : TemplatedControl
{
    public const string RootPart = "Root";
    public const string TextPart = "Text";
    public const string ActionPart = "Action";
    public const string ActionIconPart = "ActionIcon";

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<SelectTag, object?>(nameof(Content));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<SelectTag, OverrideCollection?>(nameof(Overrides));

    private readonly Parts _parts;
    private readonly BackplatePointer _pointer;
    private Control? _action;
    private IPointer? _actionPress;

    static SelectTag()
    {
        FocusableProperty.OverrideDefaultValue<SelectTag>(true);
        // Its focus ring is Base Web's box-shadow, drawn by its root, not Avalonia's adorner.
        FocusAdornerProperty.OverrideDefaultValue<SelectTag>(null);
    }

    public SelectTag()
    {
        _parts = new Parts(this, "SelectTag", [RootPart, TextPart, ActionPart, ActionIconPart]);
        _pointer = new BackplatePointer(this, () => _parts[RootPart], SetState, leaveEndsPress: false);
    }

    /// <summary>The tag's label: the select's value label for its option.</summary>
    [Content]
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>Base Web's Tag's <c>overrides</c>: <c>Root</c>, <c>Text</c>, <c>Action</c>, <c>ActionIcon</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>The option the tag shows.</summary>
    internal object? Value { get; set; }

    /// <summary>Base Web's <c>onActionClick</c>: the select's removeValue.</summary>
    internal Action? Remove { get; set; }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override Type StyleKeyOverride => typeof(SelectTag);

    private void SetState(string pseudo, bool on)
    {
        PseudoClasses.Set(pseudo, on);
        _parts.SetState(pseudo, on);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_action is not null)
        {
            _action.RemoveHandler(PointerPressedEvent, OnActionPressed);
            _action.RemoveHandler(PointerReleasedEvent, OnActionReleased);
        }

        _parts.Attach(e.NameScope);
        _parts.SetOverrides(Overrides);
        _parts.SetState(":focus-visible", IsFocused && PseudoClasses.Contains(":focus-visible"));
        _pointer.TemplateApplied();
        _action = e.NameScope.Find<Control>(ActionPart);
        _action?.AddHandler(PointerPressedEvent, OnActionPressed, handledEventsToo: true);
        _action?.AddHandler(PointerReleasedEvent, OnActionReleased, handledEventsToo: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == OverridesProperty) _parts.SetOverrides(Overrides);
        else if (change.Property == IsEffectivelyEnabledProperty)
        {
            UpdateName();
            if (!IsEffectivelyEnabled) _pointer.End();
        }
        else if (change.Property == ContentProperty) UpdateName();
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        _parts.SetState(":focus-visible", PseudoClasses.Contains(":focus-visible"));
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        _parts.SetState(":focus-visible", false);
    }

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
        if (IsEffectivelyEnabled) _pointer.Pressed(e);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _pointer.CaptureLost();
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _pointer.CaptureLost();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _pointer.End();
        _actionPress = null;
    }

    private void OnActionPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsEffectivelyEnabled || e.GetCurrentPoint(_action).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;
        _actionPress = e.Pointer;
    }

    /// <summary>The action's click (a press and a release on it, a finger's tap too): onActionClick, the click going no further (stopPropagation).</summary>
    private void OnActionReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _actionPress || _action is null) return;
        _actionPress = null;
        if (!new Rect(_action.Bounds.Size).Contains(e.GetPosition(_action))) return;
        e.Handled = true;
        Clicked = true;
        Remove?.Invoke();
    }

    /// <summary>Whether its action was just clicked: the select's control takes no click from it (stopPropagation).</summary>
    internal bool Clicked { get; set; }

    /// <summary>tag.js's handleKeyDown: Backspace or Delete on the focused tag removes it.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Source != this || !IsEffectivelyEnabled) return;
        if (e.Key is Key.Back or Key.Delete)
        {
            e.Handled = true;
            Remove?.Invoke();
        }
    }

    private void UpdateName()
    {
        // tag.js's aria-label: "Remove" and its text, while it is a button (closeable, not disabled).
        AutomationProperties.SetName(this, IsEffectivelyEnabled ? "Remove" + (Content is string s ? " " + s : "") : Content?.ToString());
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SelectTagAutomationPeer(this);
}

/// <summary>A tag to UI Automation: tag.js's <c>role="button"</c> while it isn't disabled, invoked as its action's click.</summary>
internal sealed class SelectTagAutomationPeer(SelectTag owner) : ControlAutomationPeer(owner), IInvokeProvider
{
    private new SelectTag Owner => (SelectTag)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner.IsEffectivelyEnabled ? AutomationControlType.Button : AutomationControlType.Text;

    protected override bool IsControlElementCore() => Owner.IsVisible;

    protected override bool IsContentElementCore() => Owner.IsVisible;

    public void Invoke()
    {
        if (!IsEnabled()) throw new ElementNotEnabledException();
        Owner.Remove?.Invoke();
    }
}
