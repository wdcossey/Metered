using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Interactivity;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>ProgressBar</c> (baseui 18.2.0, <c>progress-bar/progressbar.js</c>): an Avalonia <see cref="RangeBase"/>
/// with Base Web's props, its named parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>value</c>, <c>minValue</c> and <c>maxValue</c> are <c>Value</c>, <c>Minimum</c> and
/// <c>Maximum</c> (0, 0 and 100 by default, as Base Web's), which, as Base Web's, aren't clamped: a value past the
/// range is drawn as Base Web draws it and said as Base Web's label says it ("150% Complete").</para>
/// <para>The template has <c>Root</c>, <c>BarContainer</c> and <c>Label</c>; the bar makes the parts Base Web renders
/// some number of times: a <c>Bar</c> with its <c>BarProgress</c> for each of <see cref="Steps"/>, or, when
/// <see cref="Infinite"/>, two <c>InfiniteBar</c>s, the first <c>:is-left</c>.</para>
/// <para>The state pseudoclasses, set on the bar and on each part, are Base Web's <c>$</c> props: <c>:size-small</c>,
/// <c>:size-medium</c> and <c>:size-large</c>; <c>:intent-default</c>, <c>:intent-positive</c>,
/// <c>:intent-warning</c>, <c>:intent-negative</c> and <c>:intent-brand</c> (none with no intent); <c>:stepped</c>
/// (more than one step); <c>:infinite</c> (no style of Base Web's reads it, an override may). <c>:loaded</c> is set
/// once the bar is loaded, so the fill's transition doesn't run on its first appearance, as CSS's doesn't. Each step's
/// fill also has its state: <c>:step-completed</c>, <c>:step-in-progress</c> or <c>:step-awaits</c>.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(BarContainerPart, typeof(Panel))]
[TemplatePart(LabelPart, typeof(TextBlock))]
[PseudoClasses(":loaded", ":infinite", ":stepped", ":size-small", ":size-medium", ":size-large", ":intent-default",
    ":intent-positive", ":intent-warning", ":intent-negative", ":intent-brand")]
public class ProgressBar : RangeBase
{
    public const string RootPart = "Root";
    public const string BarContainerPart = "BarContainer";
    public const string BarPart = "Bar";
    public const string BarProgressPart = "BarProgress";
    public const string InfiniteBarPart = "InfiniteBar";
    public const string LabelPart = "Label";

    public static readonly StyledProperty<ProgressBarSize> SizeProperty =
        AvaloniaProperty.Register<ProgressBar, ProgressBarSize>(nameof(Size), ProgressBarSize.Medium);

    public static readonly StyledProperty<ProgressBarIntent?> IntentProperty =
        AvaloniaProperty.Register<ProgressBar, ProgressBarIntent?>(nameof(Intent));

    public static readonly StyledProperty<int> StepsProperty =
        AvaloniaProperty.Register<ProgressBar, int>(nameof(Steps), 1);

    public static readonly StyledProperty<bool> InfiniteProperty =
        AvaloniaProperty.Register<ProgressBar, bool>(nameof(Infinite));

    public static readonly StyledProperty<bool> ShowLabelProperty =
        AvaloniaProperty.Register<ProgressBar, bool>(nameof(ShowLabel));

    public static readonly StyledProperty<Func<double, double, double, string>?> GetProgressLabelProperty =
        AvaloniaProperty.Register<ProgressBar, Func<double, double, double, string>?>(nameof(GetProgressLabel));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<ProgressBar, OverrideCollection?>(nameof(Overrides));

    private static readonly string[] Intents = [":intent-default", ":intent-positive", ":intent-warning", ":intent-negative", ":intent-brand"];

    private static readonly string[] StepStates = [":step-completed", ":step-in-progress", ":step-awaits"];

    private readonly Parts _parts;

    /// <summary>The value's offset on the fill of a bar of one step, at Template priority: replaced, never stacked.</summary>
    private IDisposable? _offset;

    static ProgressBar()
    {
        // Base Web clamps nothing: a value past the range, or a maximum under the minimum, is kept as given. A value
        // that isn't finite is refused, as RangeBase refuses it. A progress bar shows its value: one way by default.
        ValueProperty.OverrideMetadata<ProgressBar>(new StyledPropertyMetadata<double>(defaultBindingMode: BindingMode.OneWay, coerce: KeepFinite(ValueProperty)));
        MaximumProperty.OverrideMetadata<ProgressBar>(new StyledPropertyMetadata<double>(100, coerce: KeepFinite(MaximumProperty)));
    }

    public ProgressBar()
    {
        _parts = new Parts(this, "ProgressBar", [RootPart, BarContainerPart, LabelPart]);
        UpdateSize();
        UpdateIntent();
        SetState(":stepped", Steps > 1);
        SetState(":infinite", Infinite);
    }

    /// <summary>Base Web's <c>size</c>: the bars' thickness and the label's type. Medium by default.</summary>
    public ProgressBarSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>intent</c>: the fill's colour. None (null) by default, which is <c>backgroundAccent</c>.</summary>
    public ProgressBarIntent? Intent { get => GetValue(IntentProperty); set => SetValue(IntentProperty, value); }

    /// <summary>Base Web's <c>steps</c>: a bar for each, 8 apart, each filled when its share of the range is reached. 1 by default.</summary>
    public int Steps { get => GetValue(StepsProperty); set => SetValue(StepsProperty, value); }

    /// <summary>Base Web's <c>infinite</c>: no value, two bars sweeping from the middle outwards, endlessly.</summary>
    public bool Infinite { get => GetValue(InfiniteProperty); set => SetValue(InfiniteProperty, value); }

    /// <summary>Base Web's <c>showLabel</c>: the label under the bar.</summary>
    public bool ShowLabel { get => GetValue(ShowLabelProperty); set => SetValue(ShowLabelProperty, value); }

    /// <summary>
    /// Base Web's <c>getProgressLabel</c>: the label for a value, a maximum and a minimum, in that order, as Base Web
    /// calls it. Null is Base Web's default, the percentage, rounded: "20% Complete".
    /// </summary>
    public Func<double, double, double, string>? GetProgressLabel { get => GetValue(GetProgressLabelProperty); set => SetValue(GetProgressLabelProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>Root</c>, <c>BarContainer</c>, <c>Bar</c>, <c>BarProgress</c>, <c>InfiniteBar</c>, <c>Label</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>The label Base Web shows: <see cref="GetProgressLabel"/>'s, or its default.</summary>
    internal string ProgressLabel => GetProgressLabel?.Invoke(Value, Maximum, Minimum) ?? DefaultLabel(Value, Maximum, Minimum);

    /// <summary>
    /// The name Base Web gives assistive technology when the app gives none (<c>aria-label</c>): "Loading" when infinite,
    /// "Step 3 of 4" with steps, otherwise the label.
    /// </summary>
    internal string DefaultAccessibleName => Infinite ? "Loading" : Steps > 1 ? StepLabel(Value, Maximum, Minimum, Steps) : ProgressLabel;

    /// <summary>A part of the current template by its Base Web name (the first, for a part it makes many of), for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>Every element of a part: the template's one, or each the bar made.</summary>
    internal IReadOnlyList<StyledElement> Elements(string name) => _parts.All(name);

    protected override Type StyleKeyOverride => typeof(ProgressBar);

    protected override AutomationPeer OnCreateAutomationPeer() => new MeteredProgressBarAutomationPeer(this);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        Build();
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

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ValueProperty || p == MinimumProperty || p == MaximumProperty)
        {
            UpdateFills();
            UpdateLabel();
        }
        else if (p == StepsProperty || p == InfiniteProperty)
        {
            SetState(":stepped", Steps > 1);
            SetState(":infinite", Infinite);
            Build();
        }
        else if (p == SizeProperty) UpdateSize();
        else if (p == IntentProperty) UpdateIntent();
        else if (p == ShowLabelProperty || p == GetProgressLabelProperty) UpdateLabel();
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    /// <summary>Base Web's default label: <c>`${Math.round((value - minValue) / (maxValue - minValue) * 100)}% Complete`</c>.</summary>
    internal static string DefaultLabel(double value, double maxValue, double minValue) =>
        $"{Js(JsRound((value - minValue) / (maxValue - minValue) * 100))}% Complete";

    /// <summary>Base Web's step label: <c>`Step ${Math.ceil((value - minValue) / (maxValue - minValue) * steps)} of ${steps}`</c>.</summary>
    internal static string StepLabel(double value, double maxValue, double minValue, int steps) =>
        $"Step {Js(Math.Ceiling((value - minValue) / (maxValue - minValue) * steps))} of {steps.ToString(CultureInfo.InvariantCulture)}";

    /// <summary>
    /// Base Web's fill in a bar of one step (styled-components.js, StyledBarProgress): <c>translateX(-${100 - (value -
    /// min) * 100 / (max - min)}%)</c>. Past the maximum the width is negative and the text <c>translateX(--50%)</c>
    /// isn't CSS: a browser drops it and draws no transform, 0; so it does for a width that isn't a number.
    /// </summary>
    internal static double Offset(double value, double minValue, double maxValue)
    {
        var width = 100 - (value - minValue) * 100 / (maxValue - minValue);
        return double.IsFinite(width) && width >= 0 ? -width : 0;
    }

    /// <summary>Base Web's step states (StyledBarProgress): the steps whose share of the range is reached are completed, the next in progress.</summary>
    internal static string StepState(double value, double minValue, double maxValue, int steps, int index)
    {
        var stepValue = (maxValue - minValue) / steps;
        var currentValue = (value - minValue) / (maxValue - minValue) * 100;
        var completedSteps = Math.Floor(currentValue / stepValue);
        return index < completedSteps ? ":step-completed" : index == completedSteps ? ":step-in-progress" : ":step-awaits";
    }

    /// <summary>JavaScript's Math.round: the nearest integer, a half towards +∞.</summary>
    private static double JsRound(double x)
    {
        if (!double.IsFinite(x)) return x;
        var f = Math.Floor(x);
        return x - f >= 0.5 ? f + 1 : f;
    }

    /// <summary>
    /// An integer, a NaN or an infinity as JavaScript writes it in a template literal: every digit below 10²¹, then the
    /// shortest exponent form ("1e+21"); −0 as 0.
    /// </summary>
    private static string Js(double x) =>
        double.IsNaN(x) ? "NaN"
        : double.IsInfinity(x) ? (x > 0 ? "Infinity" : "-Infinity")
        : Math.Abs(x) < 1e21 ? (x + 0.0).ToString("F0", CultureInfo.InvariantCulture)
        : x.ToString("R", CultureInfo.InvariantCulture).ToLowerInvariant();

    /// <summary>
    /// The bars Base Web renders: a Bar and its BarProgress for each step, or the two InfiniteBars. Each new element has
    /// its state, its offset and its theme before it joins the tree, so its first look isn't transitioned to, as a
    /// browser doesn't transition an element's first style.
    /// </summary>
    private void Build()
    {
        if (_parts[BarContainerPart] is not Panel container) return;
        container.Children.Clear();
        _offset?.Dispose();
        _offset = null;
        var bars = new List<StyledElement>();
        var fills = new List<StyledElement>();
        var infinite = new List<StyledElement>();
        if (Infinite)
        {
            for (var i = 0; i < 2; i++)
            {
                var half = new ProgressBarInfiniteBar();
                ((IPseudoClasses)half.Classes).Set(":is-left", i == 0);
                infinite.Add(half);
            }
        }
        else
        {
            for (var i = 0; i < Steps; i++)
            {
                var fill = new ProgressBarFill();
                UpdateFill(fill, i);
                bars.Add(new Border { Child = fill });
                fills.Add(fill);
            }
        }

        _parts.Place((BarPart, bars), (BarProgressPart, fills), (InfiniteBarPart, infinite));
        foreach (var element in bars.Concat(infinite)) container.Children.Add((Control)element);
    }

    /// <summary>Each fill's step state, or in a bar of one step its offset from the value.</summary>
    private void UpdateFills()
    {
        var fills = _parts.All(BarProgressPart);
        for (var i = 0; i < fills.Count; i++) UpdateFill(fills[i], i);
    }

    private void UpdateFill(StyledElement fill, int index)
    {
        var classes = (IPseudoClasses)fill.Classes;
        var state = Steps > 1 ? StepState(Value, Minimum, Maximum, Steps, index) : null;
        foreach (var s in StepStates) classes.Set(s, s == state);
        if (state is not null) return;

        // A bar of one step: the value's offset. Set before the last is let go, so the fill never sees the theme's own
        // in between (its transition runs from the last value to this one); kept, a frame each would pile up.
        var next = fill.SetValue(ProgressBarFill.OffsetProperty, Offset(Value, Minimum, Maximum), BindingPriority.Template);
        _offset?.Dispose();
        _offset = next;
    }

    /// <summary>
    /// Base Web renders the label only with showLabel: without it, nothing shows it; with it, the part's theme and its
    /// override decide, as an override's style does on Base Web's.
    /// </summary>
    private void UpdateLabel()
    {
        if (_parts[LabelPart] is not TextBlock label) return;
        if (ShowLabel) label.ClearValue(Visual.IsVisibleProperty);
        else label.IsVisible = false;
        label.Text = ShowLabel ? ProgressLabel : null;
    }

    private void UpdateSize()
    {
        SetState(":size-small", Size == ProgressBarSize.Small);
        SetState(":size-medium", Size == ProgressBarSize.Medium);
        SetState(":size-large", Size == ProgressBarSize.Large);
    }

    private void UpdateIntent()
    {
        for (var i = 0; i < Intents.Length; i++) SetState(Intents[i], Intent is { } intent && (int)intent == i);
    }

    private static Func<AvaloniaObject, double, double> KeepFinite(StyledProperty<double> property) =>
        (sender, value) => double.IsFinite(value) ? value : sender.GetValue(property);

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }
}

/// <summary>
/// The ProgressBar's automation peer: Avalonia's progress bar peer (role progressbar, its range read only), named as
/// Base Web names it when the app gives no name: "Loading", "Step 3 of 4" or the label.
/// </summary>
internal sealed class MeteredProgressBarAutomationPeer : Avalonia.Controls.Automation.Peers.ProgressBarAutomationPeer
{
    private string? _name;

    public MeteredProgressBarAutomationPeer(ProgressBar owner)
        : base(owner)
    {
        _name = GetName();
        owner.PropertyChanged += OnOwnerPropertyChanged;
    }

    /// <summary>
    /// Base Web's <c>aria-label</c> changes with the value, and its <c>aria-live="polite"</c> region says it: the name's
    /// change is raised whenever what it is made of changes.
    /// </summary>
    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        var p = e.Property;
        if (p != RangeBase.ValueProperty && p != RangeBase.MinimumProperty && p != RangeBase.MaximumProperty && p != ProgressBar.StepsProperty
            && p != ProgressBar.InfiniteProperty && p != ProgressBar.GetProgressLabelProperty && p != AutomationProperties.NameProperty) return;
        var name = GetName();
        if (name == _name) return;
        RaisePropertyChangedEvent(AutomationElementIdentifiers.NameProperty, _name, name);
        _name = name;
    }

    protected override string? GetNameCore()
    {
        var name = base.GetNameCore();
        return string.IsNullOrEmpty(name) ? ((ProgressBar)Owner).DefaultAccessibleName : name;
    }
}
