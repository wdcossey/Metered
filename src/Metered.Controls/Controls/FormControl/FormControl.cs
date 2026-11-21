using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Metadata;
using AvaloniaLabel = Avalonia.Controls.Label;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>FormControl</c> (baseui 18.2.0, <c>form-control/form-control.js</c>): a label above its one child, and
/// a caption, an error or a positive message below it, with its props, its seven named parts and its overrides. The child
/// is the control's <see cref="ContentControl.Content"/>.
/// </summary>
/// <remarks>
/// <para>Parts, by Base Web's names: <c>LabelContainer</c> (the label's row), <c>Label</c>, <c>LabelEndEnhancer</c>,
/// <c>ControlContainer</c> (the child and the caption), <c>Caption</c>, <c>CaptionIcon</c>, <c>CaptionMessage</c>. Base
/// Web takes only a <c>component</c> for <c>CaptionIcon</c> and <c>CaptionMessage</c> (it renders them with no props, so
/// an override's style and props never reach them): here too, an override's <see cref="Override.Style"/> and
/// <see cref="Override.Props"/> for those two are ignored.</para>
/// <para>States, set on the control and every part: <c>:disabled</c> (Avalonia's, while it isn't enabled),
/// <c>:error</c> (its <see cref="Error"/>, or a counter past its maximum), <c>:positive</c>, <c>:counter-error</c>, and
/// <c>:icon-alert</c> or <c>:icon-check</c>, the icon the caption shows.</para>
/// <para>Base Web's <c>label</c>, <c>caption</c>, <c>error</c>, <c>positive</c> and <c>labelEndEnhancer</c> are content
/// or a function of the shared props (<see cref="FormControlState"/>), as <c>Func&lt;FormControlState, object?&gt;</c>;
/// a value is something when JavaScript would call it true (not null, an empty string, false, 0 or NaN). A boolean
/// <see cref="Error"/> or <see cref="Positive"/> is the state alone; anything else is also shown in the caption's place.
/// <c>{x:True}</c> in XAML is the boolean, <c>"True"</c> text.</para>
/// <para><c>disabled</c> is Avalonia's <see cref="InputElement.IsEnabled"/>, which disables the child too, as Base Web
/// gives it <c>onlyChildProps.disabled || disabled</c>. The child is given the form control's error and positive, as
/// Base Web's <c>cloneElement</c> gives them, below its own values: a child's own <c>Error</c> or <c>Positive</c> wins.
/// Base Web's Textarea has its own error, false, in its defaultProps: a Textarea isn't given the error.</para>
/// </remarks>
[TemplatePart(LabelContainerPart, typeof(StyledElement))]
[TemplatePart(LabelPart, typeof(FormControlLabel))]
[TemplatePart(LabelEndEnhancerPart, typeof(ContentPresenter))]
[TemplatePart(ControlContainerPart, typeof(StyledElement))]
[TemplatePart(CaptionPart, typeof(StyledElement))]
[TemplatePart(CaptionIconPart, typeof(StyledElement))]
[TemplatePart(CaptionMessagePart, typeof(ContentPresenter))]
[PseudoClasses(":error", ":positive", ":counter-error", ":icon-alert", ":icon-check")]
public class FormControl : ContentControl
{
    public const string LabelContainerPart = "LabelContainer";
    public const string LabelPart = "Label";
    public const string LabelEndEnhancerPart = "LabelEndEnhancer";
    public const string ControlContainerPart = "ControlContainer";
    public const string CaptionPart = "Caption";
    public const string CaptionIconPart = "CaptionIcon";
    public const string CaptionMessagePart = "CaptionMessage";

    /// <summary>Every part, by Base Web's names (types.d.ts's FormControlOverrides).</summary>
    internal static readonly string[] PartNames =
        [LabelPart, LabelEndEnhancerPart, LabelContainerPart, CaptionPart, CaptionMessagePart, CaptionIconPart, ControlContainerPart];

    /// <summary>The parts Base Web renders with no props: only an override's component reaches them.</summary>
    private static readonly string[] ComponentOnly = [CaptionMessagePart, CaptionIconPart];

    public static readonly StyledProperty<object?> LabelProperty =
        AvaloniaProperty.Register<FormControl, object?>(nameof(Label));

    public static readonly StyledProperty<object?> CaptionProperty =
        AvaloniaProperty.Register<FormControl, object?>(nameof(Caption));

    public static readonly StyledProperty<object?> ErrorProperty =
        AvaloniaProperty.Register<FormControl, object?>(nameof(Error));

    public static readonly StyledProperty<object?> PositiveProperty =
        AvaloniaProperty.Register<FormControl, object?>(nameof(Positive));

    public static readonly StyledProperty<object?> LabelEndEnhancerProperty =
        AvaloniaProperty.Register<FormControl, object?>(nameof(LabelEndEnhancer));

    public static readonly StyledProperty<FormControlCounter?> CounterProperty =
        AvaloniaProperty.Register<FormControl, FormControlCounter?>(nameof(Counter));

    public static readonly StyledProperty<IInputElement?> HtmlForProperty =
        AvaloniaProperty.Register<FormControl, IInputElement?>(nameof(HtmlFor));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<FormControl, OverrideCollection?>(nameof(Overrides));

    public static readonly DirectProperty<FormControl, FormControlState> StateProperty =
        AvaloniaProperty.RegisterDirect<FormControl, FormControlState>(nameof(State), o => o.State);

    private readonly Parts _parts;
    private readonly Held _held = new();
    private readonly OverrideCollection _effective = [];
    private OverrideCollection? _followed;
    private AvaloniaObject? _child;
    private FormControlState _state = new(false, false, false);

    public FormControl()
    {
        _parts = new Parts(this, "FormControl", PartNames);
        _parts.SetOverrides(_effective);
    }

    /// <summary>Base Web's <c>label</c>: rendered above the child, with its row, when it is something.</summary>
    public object? Label { get => GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

    /// <summary>Base Web's <c>caption</c>: rendered below the child unless an error or a positive message is.</summary>
    public object? Caption { get => GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

    /// <summary>Base Web's <c>error</c>: the error state, and unless a boolean the message shown in the caption's place.</summary>
    public object? Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>positive</c>: the positive state, and unless a boolean the message shown (after an error).</summary>
    public object? Positive { get => GetValue(PositiveProperty); set => SetValue(PositiveProperty, value); }

    /// <summary>Base Web's <c>labelEndEnhancer</c>: shown at the end of the label's row; the counter's text when unset.</summary>
    public object? LabelEndEnhancer { get => GetValue(LabelEndEnhancerProperty); set => SetValue(LabelEndEnhancerProperty, value); }

    /// <summary>Base Web's <c>counter</c>: none (null, Base Web's <c>false</c>) or a counter of the child's text.</summary>
    public FormControlCounter? Counter { get => GetValue(CounterProperty); set => SetValue(CounterProperty, value); }

    /// <summary>
    /// Base Web's <c>htmlFor</c>: the element the label labels, focused by a click or tap on it, by name in XAML as
    /// Avalonia's <c>Label.Target</c>. When unset, the child: Base Web's default is the child's <c>id</c>, which Avalonia
    /// hasn't (docs/controls/FormControl).
    /// </summary>
    [ResolveByName]
    public IInputElement? HtmlFor { get => GetValue(HtmlForProperty); set => SetValue(HtmlForProperty, value); }

    /// <summary>The parts' overrides: Base Web's <c>overrides</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>The props Base Web gives the parts now (form-control.js's <c>sharedProps</c>).</summary>
    public FormControlState State { get => _state; private set => SetAndRaise(StateProperty, ref _state, value); }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>The element the label labels: <see cref="HtmlFor"/>, or the child.</summary>
    internal IInputElement? LabelTarget => HtmlFor ?? Content as IInputElement;

    protected override Type StyleKeyOverride => typeof(FormControl);

    /// <summary>Renders once its props are all set (XAML's, or a tree's that takes it), as Base Web renders with them all.</summary>
    protected override void OnInitialized()
    {
        base.OnInitialized();
        Update();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        _held.Release(part => part is StyledElement s && _parts.Names.Any(n => _parts[n] == s));
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        Update();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ContentProperty) Follow(change.NewValue as AvaloniaObject);
        if (p == OverridesProperty) FollowOverrides(Overrides);
        if (p == LabelProperty || p == CaptionProperty || p == ErrorProperty || p == PositiveProperty || p == LabelEndEnhancerProperty
            || p == CounterProperty || p == HtmlForProperty || p == ContentProperty || p == IsEffectivelyEnabledProperty)
        {
            Update();
        }
    }

    /// <summary>JavaScript's truth: not null, an empty string, false, 0 or NaN.</summary>
    internal static bool IsTruthy(object? value) => value switch
    {
        null => false,
        string s => s.Length > 0,
        bool b => b,
        double d => d != 0 && !double.IsNaN(d),
        float f => f != 0 && !float.IsNaN(f),
        int i => i != 0,
        long l => l != 0,
        _ => true,
    };

    /// <summary>A value, or what a function of the shared props gives for them.</summary>
    private static object? Resolve(object? value, FormControlState state) =>
        value is Func<FormControlState, object?> f ? f(state) : value;

    /// <summary>
    /// form-control.js's render: the shared props, the hint and its icon (chooseRenderedHint, chooseRenderedHintIcon),
    /// the counter, which parts render and what they show, and what the child and the label's target are given.
    /// </summary>
    private void Update()
    {
        if (!IsInitialized) return;
        var error = Error;
        var positive = Positive;
        var state = new FormControlState(!IsEffectivelyEnabled, IsTruthy(error), IsTruthy(positive));

        // The hint is chosen before the counter changes the shared props: its function sees them as they were.
        object? hint = null;
        if (IsTruthy(error) && error is not bool) hint = Resolve(error, state);
        else if (IsTruthy(positive) && positive is not bool) hint = Resolve(positive, state);
        else if (IsTruthy(Caption)) hint = Resolve(Caption, state);
        var hasHint = IsTruthy(hint);
        string? icon = null;
        if (hasHint && IsTruthy(error)) icon = error is Delegate ? null : "alert";
        else if (hasHint && IsTruthy(positive)) icon = positive is Delegate ? null : "check";

        var endEnhancer = LabelEndEnhancer;
        if (Counter is { } counter)
        {
            // Inferred values are preferred, but the counter's own win (form-control.js).
            var text = _child as TextBox;
            int? maxLength = counter.MaxLength is { } m && m != 0 ? m : text is { MaxLength: > 0 } ? text.MaxLength : null;
            var length = counter.Length ?? text?.Text?.Length ?? 0;
            var counterError = counter.Error;
            if (!IsTruthy(endEnhancer))
            {
                endEnhancer = maxLength is { } max
                    ? $"{length.ToString(CultureInfo.InvariantCulture)}/{max.ToString(CultureInfo.InvariantCulture)}"
                    : length.ToString(CultureInfo.InvariantCulture);
            }

            if (maxLength is { } most && length > most && counterError is null) counterError = true;
            state = state with { Length = length, MaxLength = maxLength is null ? null : length };
            if (counterError == true) state = state with { Error = true, CounterError = true };
        }

        State = state;
        SetState(":error", state.Error);
        SetState(":positive", state.Positive);
        SetState(":counter-error", state.CounterError);
        SetState(":icon-alert", icon == "alert");
        SetState(":icon-check", icon == "check");

        var hasLabel = IsTruthy(Label);
        var hasEnd = hasLabel && IsTruthy(endEnhancer);
        Show(LabelContainerPart, hasLabel);
        Show(LabelEndEnhancerPart, hasEnd);
        Show(CaptionPart, hasHint);
        Show(CaptionIconPart, icon is not null);
        Give(_parts[LabelPart], ContentProperty, hasLabel ? Resolve(Label, state) : null);
        Give(_parts[LabelEndEnhancerPart], ContentPresenter.ContentProperty, hasEnd ? Resolve(endEnhancer, state) : null);
        Give(_parts[CaptionMessagePart], ContentPresenter.ContentProperty, hasHint ? hint : null);

        // The label's htmlFor, and the name it gives what it labels.
        var target = LabelTarget;
        var label = _parts[LabelPart] as FormControlLabel;
        Give(label, AvaloniaLabel.TargetProperty, target);
        _held.Release(o => o != target, AutomationProperties.LabeledByProperty);
        if (target is AvaloniaObject labelled) Give(labelled, AutomationProperties.LabeledByProperty, hasLabel ? label : null);

        // What cloneElement gives the child: error and positive, when the form control's are on; aria-describedby (a
        // caption or a positive) and aria-errormessage (an error) name the caption: its text is the child's help text.
        _held.Release(o => o != _child && !IsPart(o) && o != target);
        if (_child is { } child)
        {
            // Base Web's Textarea has error: false in its defaultProps, so its own error is always set.
            if (child is not Textarea) GiveFlag(child, "Error", state.Error);
            GiveFlag(child, "Positive", state.Positive);
            var described = IsTruthy(Caption) || IsTruthy(positive) || IsTruthy(error);
            Give(child, AutomationProperties.HelpTextProperty, described && hasHint ? TextOf(hint) : null);
        }
    }

    private bool IsPart(AvaloniaObject o) => o is StyledElement s && _parts.Names.Any(n => _parts[n] == s);

    /// <summary>The text a hint shows: a string's, or a text block's.</summary>
    private static string? TextOf(object? hint) => hint switch
    {
        string s => s,
        TextBlock t => t.Text,
        _ => null,
    };

    /// <summary>Gives the child its form control's flag, by the property of that name it has (Base Web's prop), if on.</summary>
    private void GiveFlag(AvaloniaObject child, string name, bool on)
    {
        if (AvaloniaPropertyRegistry.Instance.FindRegistered(child, name) is not { } property || property.PropertyType != typeof(bool)) return;
        Give(child, property, on ? true : null);
    }

    /// <summary>
    /// Gives a value at the template's priority, below an element's own (a local value, a style trigger), as Base Web's
    /// props are below an override's; null lets it go.
    /// </summary>
    private void Give(AvaloniaObject? target, AvaloniaProperty property, object? value)
    {
        if (target is null) return;
        _held.Set(target, property, value);
    }

    /// <summary>Shows or hides a part, as Base Web renders it or doesn't, at the template's priority (an override's wins).</summary>
    private void Show(string part, bool on)
    {
        if (_parts[part] is { } element) _held.Set(element, IsVisibleProperty, on);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    /// <summary>Follows the child's text and maximum, which the counter reads, as Base Web re-renders with its props.</summary>
    private void Follow(AvaloniaObject? child)
    {
        if (_child is not null)
        {
            _child.PropertyChanged -= OnChildChanged;
            _held.Release(o => o == _child);
        }

        _child = child;
        if (_child is not null) _child.PropertyChanged += OnChildChanged;
    }

    private void OnChildChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (Counter is not null && (e.Property == TextBox.TextProperty || e.Property == TextBox.MaxLengthProperty)) Update();
    }

    /// <summary>The app's overrides, less a style and props for the parts Base Web gives only a component.</summary>
    private void FollowOverrides(OverrideCollection? overrides)
    {
        if (_followed is not null) _followed.CollectionChanged -= OnOverridesChanged;
        _followed = overrides;
        if (_followed is not null) _followed.CollectionChanged += OnOverridesChanged;
        OnOverridesChanged(null, null);
    }

    private void OnOverridesChanged(object? sender, NotifyCollectionChangedEventArgs? e)
    {
        _effective.Clear();
        foreach (var entry in _followed ?? [])
        {
            _effective.Add(ComponentOnly.Contains(entry.Part) ? new Override { Part = entry.Part, Component = entry.Component } : entry);
        }
    }

    /// <summary>
    /// The values given at the template's priority, one for each element and property, held and let go when it changes: a
    /// value set below the local one is a frame of its own, and frames would pile up on every keystroke.
    /// </summary>
    private sealed class Held
    {
        private readonly Dictionary<(AvaloniaObject, AvaloniaProperty), (object? Value, IDisposable? Frame)> _held = [];

        public void Set(AvaloniaObject target, AvaloniaProperty property, object? value)
        {
            var key = (target, property);
            if (_held.TryGetValue(key, out var held))
            {
                if (Equals(held.Value, value)) return;
                held.Frame?.Dispose();
                _held.Remove(key);
            }

            if (value is null) return;
            _held[key] = (value, target.SetValue(property, value, BindingPriority.Template));
        }

        /// <summary>Lets go of every value on the elements that match (of that property, if one is given).</summary>
        public void Release(Func<AvaloniaObject, bool> which, AvaloniaProperty? property = null)
        {
            foreach (var key in _held.Keys.Where(k => which(k.Item1) && (property is null || k.Item2 == property)).ToList())
            {
                _held[key].Frame?.Dispose();
                _held.Remove(key);
            }
        }
    }
}
