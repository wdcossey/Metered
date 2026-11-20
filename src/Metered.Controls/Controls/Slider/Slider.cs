using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>A slider's values, as Base Web's <c>onChange</c> and <c>onFinalChange</c> give them: <c>{ value }</c>.</summary>
public sealed class SliderChangeEventArgs(RoutedEvent routedEvent, IReadOnlyList<double> value) : RoutedEventArgs(routedEvent)
{
    /// <summary>The thumbs' values, in order.</summary>
    public IReadOnlyList<double> Value { get; } = value;
}

/// <summary>
/// Base Web's <c>Slider</c> and <c>StatefulSlider</c> (baseui 18.2.0, <c>slider/</c>), which drags and steps through
/// react-range (1.10.0, <c>react-range/lib/Range.js</c>): one thumb, or two for a range, on a line from
/// <see cref="Min"/> to <see cref="Max"/> in steps of <see cref="Step"/>, with its named parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's two components are one here: <see cref="Value"/> is the thumbs' values, which the slider sets as it
/// is dragged or stepped (as <c>StatefulSlider</c> keeps its state) and an app can bind; none is
/// <c>StatefulSlider</c>'s own start, the middle (<c>[Math.round((max - min) / 2) + min]</c>). <see cref="Change"/> and
/// <see cref="FinalChange"/> are <c>onChange</c> and <c>onFinalChange</c>, raised where react-range calls them;
/// <c>disabled</c> is <c>IsEnabled</c>.</para>
/// <para>A press on the <c>Track</c> (the mouse's primary button, a finger or a pen) takes the thumb under it, or else
/// the closest, focuses it and moves it there; the drag follows the pointer, captured, until the release. A touch that
/// starts on the track is the slider's: it never pans a <c>ScrollViewer</c>, as Base Web's <c>touchmove</c> handler
/// prevents the page's scrolling. On a focused thumb, ArrowRight, ArrowUp, <c>k</c> and PageUp step it up (PageUp ten
/// steps), ArrowLeft, ArrowDown, <c>j</c> and PageDown down.</para>
/// <para>The state pseudoclasses are Base Web's <c>$</c> props, on the slider and its parts: <c>:range</c>
/// (<c>$value.length === 2</c>) on all; <c>:is-dragged</c> on the <c>Track</c> and <c>InnerTrack</c> while any thumb is
/// dragged, and on a thumb, its <c>ThumbValue</c> and its <c>InnerThumb</c> while it is; <c>:focus-visible</c> on a
/// thumb focused by keyboard, and on the other parts while one is. <c>:disabled</c> is Avalonia's own.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(TrackPart, typeof(Control))]
[TemplatePart(InnerTrackPart, typeof(SliderInnerTrack))]
[TemplatePart(TickBarPart, typeof(StyledElement))]
[TemplatePart(MinTickName, typeof(ContentPresenter))]
[TemplatePart(MaxTickName, typeof(ContentPresenter))]
[PseudoClasses(":range", ":is-dragged", ":focus-visible")]
public class Slider : TemplatedControl
{
    public const string RootPart = "Root";
    public const string TrackPart = "Track";
    public const string InnerTrackPart = "InnerTrack";
    public const string ThumbPart = "Thumb";
    public const string InnerThumbPart = "InnerThumb";
    public const string ThumbValuePart = "ThumbValue";
    public const string TickPart = "Tick";
    public const string TickBarPart = "TickBar";
    public const string MarkPart = "Mark";

    /// <summary>The template's names for the two <c>Tick</c> parts, the minimum's and the maximum's.</summary>
    public const string MinTickName = "MinTick";
    public const string MaxTickName = "MaxTick";

    public static readonly StyledProperty<IReadOnlyList<double>> ValueProperty =
        AvaloniaProperty.Register<Slider, IReadOnlyList<double>>(nameof(Value), [], defaultBindingMode: BindingMode.TwoWay,
            validate: v => v is null || v.Count <= 2, coerce: Coerce);

    public static readonly StyledProperty<double> MinProperty =
        AvaloniaProperty.Register<Slider, double>(nameof(Min), 0, validate: double.IsFinite);

    public static readonly StyledProperty<double> MaxProperty =
        AvaloniaProperty.Register<Slider, double>(nameof(Max), 100, validate: double.IsFinite);

    public static readonly StyledProperty<double> StepProperty =
        AvaloniaProperty.Register<Slider, double>(nameof(Step), 1, validate: v => double.IsFinite(v) && v > 0);

    public static readonly StyledProperty<bool> MarksProperty =
        AvaloniaProperty.Register<Slider, bool>(nameof(Marks));

    public static readonly StyledProperty<bool> PersistentThumbProperty =
        AvaloniaProperty.Register<Slider, bool>(nameof(PersistentThumb));

    public static readonly StyledProperty<Func<double, object?>?> ValueToLabelProperty =
        AvaloniaProperty.Register<Slider, Func<double, object?>?>(nameof(ValueToLabel));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Slider, OverrideCollection?>(nameof(Overrides));

    public static readonly RoutedEvent<SliderChangeEventArgs> ChangeEvent =
        RoutedEvent.Register<Slider, SliderChangeEventArgs>(nameof(Change), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<SliderChangeEventArgs> FinalChangeEvent =
        RoutedEvent.Register<Slider, SliderChangeEventArgs>(nameof(FinalChange), RoutingStrategies.Bubble);

    private readonly Parts _parts;
    private readonly List<SliderThumb> _thumbs = [];
    private readonly List<(ContentPresenter Value, Border Inner)> _labels = [];
    private readonly List<bool> _hovered = [];
    private readonly List<Border> _marks = [];
    private Control? _track;
    private SliderInnerTrack? _inner;
    private ContentPresenter? _minTick;
    private ContentPresenter? _maxTick;
    private int _dragged = -1;
    private bool _isChanged;
    private IPointer? _pointer;
    private int _focused = -1;
    private bool _focusVisible;

    public Slider()
    {
        _parts = new Parts(this, "Slider", [RootPart, TrackPart, InnerTrackPart, ThumbPart, InnerThumbPart, ThumbValuePart, TickPart, TickBarPart, MarkPart]);
        CoerceValue(ValueProperty);
        Sync();
        RebuildMarks();
    }

    /// <summary>
    /// Base Web's <c>value</c>: the thumbs' places, one or two, in order. More than two is refused, as Base Web throws;
    /// none is <c>StatefulSlider</c>'s start, the middle. A value outside <see cref="Min"/> and <see cref="Max"/> is held
    /// at the nearer end, and two out of order are put in order (react-range throws on both).
    /// </summary>
    public IReadOnlyList<double> Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Base Web's <c>min</c>: 0 by default.</summary>
    public double Min { get => GetValue(MinProperty); set => SetValue(MinProperty, value); }

    /// <summary>Base Web's <c>max</c>: 100 by default.</summary>
    public double Max { get => GetValue(MaxProperty); set => SetValue(MaxProperty, value); }

    /// <summary>Base Web's <c>step</c>: what a drag rounds to and a key moves by; 1 by default.</summary>
    public double Step { get => GetValue(StepProperty); set => SetValue(StepProperty, value); }

    /// <summary>Base Web's <c>marks</c>: a mark on the line at each step.</summary>
    public bool Marks { get => GetValue(MarksProperty); set => SetValue(MarksProperty, value); }

    /// <summary>Base Web's <c>persistentThumb</c>: each thumb's value always shown, not only under a mouse or while dragged.</summary>
    public bool PersistentThumb { get => GetValue(PersistentThumbProperty); set => SetValue(PersistentThumbProperty, value); }

    /// <summary>
    /// Base Web's <c>valueToLabel</c>: what a thumb's value and the two ticks show for a number. None shows the number,
    /// as a browser writes it.
    /// </summary>
    public Func<double, object?>? ValueToLabel { get => GetValue(ValueToLabelProperty); set => SetValue(ValueToLabelProperty, value); }

    /// <summary>
    /// Base Web's <c>overrides</c>, by part: <c>Root</c>, <c>Track</c>, <c>InnerTrack</c>, <c>Thumb</c>,
    /// <c>InnerThumb</c>, <c>ThumbValue</c>, <c>Tick</c>, <c>TickBar</c>, <c>Mark</c>. A part there is more than one of
    /// (a thumb, a tick, a mark) takes its override on each.
    /// </summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's <c>onChange</c>: a drag or a key moved a thumb, or would have (react-range calls it then too).</summary>
    public event EventHandler<SliderChangeEventArgs>? Change
    {
        add => AddHandler(ChangeEvent, value);
        remove => RemoveHandler(ChangeEvent, value);
    }

    /// <summary>Base Web's <c>onFinalChange</c>: a drag ended, or a key that moved a thumb was let go.</summary>
    public event EventHandler<SliderChangeEventArgs>? FinalChange
    {
        add => AddHandler(FinalChangeEvent, value);
        remove => RemoveHandler(FinalChangeEvent, value);
    }

    /// <summary>A part of the current template by its Base Web name (the first, for a part there are several of), for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>Every element of a part, for tests and tools.</summary>
    internal IReadOnlyList<StyledElement> Elements(string name) => _parts.Elements(name).ToList();

    /// <summary>The thumbs, in the values' order.</summary>
    internal IReadOnlyList<SliderThumb> Thumbs => _thumbs;

    /// <summary>The thumb being dragged, by a pointer or a held key: react-range's <c>draggedThumbIndex</c>, or -1.</summary>
    internal int DraggedThumb => _dragged;

    protected override Type StyleKeyOverride => typeof(Slider);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Detach();
        _parts.Attach(e.NameScope);
        _track = e.NameScope.Find<Control>(TrackPart);
        _inner = e.NameScope.Find<SliderInnerTrack>(InnerTrackPart);
        _minTick = e.NameScope.Find<ContentPresenter>(MinTickName);
        _maxTick = e.NameScope.Find<ContentPresenter>(MaxTickName);
        if (_minTick is not null) _parts.Add(TickPart, _minTick);
        if (_maxTick is not null) _parts.Add(TickPart, _maxTick);
        if (_track is not null)
        {
            // Before any gesture recogniser sees the press: a touch that starts on the track is the slider's.
            _track.AddHandler(PointerPressedEvent, OnTrackPressing, RoutingStrategies.Tunnel, handledEventsToo: true);
            _track.AddHandler(PointerPressedEvent, OnTrackPressed, RoutingStrategies.Bubble, handledEventsToo: true);
            _track.AddHandler(PointerMovedEvent, OnTrackMoved, RoutingStrategies.Bubble, handledEventsToo: true);
            _track.AddHandler(PointerReleasedEvent, OnTrackReleased, RoutingStrategies.Bubble, handledEventsToo: true);
            _track.AddHandler(PointerCaptureLostEvent, OnTrackCaptureLost, RoutingStrategies.Direct | RoutingStrategies.Bubble, handledEventsToo: true);
        }

        if (_inner is not null)
        {
            foreach (var mark in _marks) _inner.Children.Add(mark);
            foreach (var thumb in _thumbs) _inner.Children.Add(thumb);
        }

        Sync();
        RebuildMarks();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ValueProperty)
        {
            if (Value.Count == _thumbs.Count) Moved();
            else Sync();
        }
        else if (p == MinProperty || p == MaxProperty)
        {
            CoerceValue(ValueProperty);
            Sync();
            RebuildMarks();
        }
        else if (p == StepProperty || p == MarksProperty) RebuildMarks();
        else if (p == PersistentThumbProperty) UpdateLabels();
        else if (p == ValueToLabelProperty) UpdateText();
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        else if (p == IsEffectivelyEnabledProperty)
        {
            if (!IsEffectivelyEnabled) EndAll();
            UpdateLabels();
            foreach (var thumb in _thumbs) thumb.Peer?.Update();
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        EndAll();
    }

    // react-range's onKeyDown, on a thumb: the increase and decrease keys move it a step (PageUp and PageDown ten) and make
    // it the dragged one until the key's release; Tab ends that, and any other key ends a change the keys made.
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (IndexOf(e.Source) is not (>= 0 and var index) || index >= Value.Count || !HasRange) return;
        var step = Step;
        if (Increases(e))
        {
            e.Handled = true;
            Drag(index, keyed: true);
            Move(index, Value[index] + (e.Key == Key.PageUp ? step * 10 : step));
        }
        else if (Decreases(e))
        {
            e.Handled = true;
            Drag(index, keyed: true);
            Move(index, Value[index] - (e.Key == Key.PageDown ? step * 10 : step));
        }
        else if (e.Key == Key.Tab)
        {
            Drag(-1);
            if (_isChanged) FireFinalChange();
        }
        else if (_isChanged)
        {
            FireFinalChange();
        }
    }

    // react-range's onKeyUp: the thumb is no longer dragged, and a change the keys made is final.
    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        if (IndexOf(e.Source) < 0) return;
        var changed = _isChanged;
        Drag(-1);
        if (changed) FireFinalChange();
    }

    /// <summary>react-range's INCREASE_KEYS: ArrowRight, ArrowUp, "k" and PageUp, by the key's own name ("K" is not "k").</summary>
    private static bool Increases(KeyEventArgs e) =>
        e.Key is Key.Right or Key.Up or Key.PageUp || (e.Key == Key.K && Symbol(e, "k") == "k");

    /// <summary>react-range's DECREASE_KEYS: ArrowLeft, ArrowDown, "j" and PageDown.</summary>
    private static bool Decreases(KeyEventArgs e) =>
        e.Key is Key.Left or Key.Down or Key.PageDown || (e.Key == Key.J && Symbol(e, "j") == "j");

    /// <summary>What the key types, as a browser's <c>key</c>; with none given, the letter, upper case under Shift.</summary>
    private static string Symbol(KeyEventArgs e, string letter) =>
        e.KeySymbol ?? ((e.KeyModifiers & KeyModifiers.Shift) != 0 ? letter.ToUpperInvariant() : letter);

    /// <summary>Base Web's StatefulSlider's start, react-range's checks as a coercion, and Base Web's limit of two.</summary>
    private static IReadOnlyList<double> Coerce(AvaloniaObject o, IReadOnlyList<double> value)
    {
        var s = (Slider)o;
        double min = s.Min, max = s.Max;
        if (value is null || value.Count == 0) return [SliderInnerTrack.JsRound((max - min) / 2) + min];
        var lo = Math.Min(min, max);
        var hi = Math.Max(min, max);
        var held = value.Select(v => double.IsNaN(v) ? lo : Math.Clamp(v, lo, hi)).ToArray();
        if (held.Length == 2 && held[0] > held[1]) (held[0], held[1]) = (held[1], held[0]);
        return held.SequenceEqual(value) ? value : held;
    }

    // --- The pointer: react-range's onMouseDownTrack and onTouchStartTrack, then onMouseOrTouchStart, onMove and onEnd.

    // Only a press the slider takes: one it ignores (a second finger, a slider with no range) can still pan the page.
    private void OnTrackPressing(object? sender, PointerPressedEventArgs e)
    {
        if (Takes(e)) e.PreventGestureRecognition();
    }

    /// <summary>Whether the slider takes a press on its track: react-range's onMouseDownTrack and onTouchStartTrack.</summary>
    private bool Takes(PointerPressedEventArgs e) =>
        IsEffectivelyEnabled && _inner is not null && _track is not null && _pointer is null && _thumbs.Count > 0 && HasRange
        // A mouse's primary button (react-range ignores the others), a finger or a pen's tip.
        && e.GetCurrentPoint(_inner).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed;

    private void OnTrackPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!Takes(e) || _inner is null || _track is null) return;
        var point = e.GetCurrentPoint(_inner);
        e.Handled = true;
        var x = point.Position.X;

        // onMouseDownTrack: the closest thumb is focused, dragged and moved to the pointer.
        var closest = Closest(x);
        FocusThumb(closest);
        Drag(closest);
        MoveTo(x);

        // Then onMouseOrTouchStart, on the document: a press on a thumb drags that one.
        if (IndexOf(e.Source) is >= 0 and var target && target != closest) Drag(target);

        // react-range follows the pointer on the document until its release: here, by capturing it.
        e.Pointer.Capture(_track);
        _pointer = e.Pointer;
    }

    private void OnTrackMoved(object? sender, PointerEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer || _inner is null) return;
        MoveTo(e.GetPosition(_inner).X);
    }

    private void OnTrackReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer) return;
        e.Handled = true;
        End();
    }

    // touchcancel, or anything else that takes the pointer: the drag ends where it is, as onEnd ends it.
    private void OnTrackCaptureLost(object? sender, PointerCaptureLostEventArgs e)
    {
        if (_pointer is null || e.Pointer != _pointer || e.Source != _track) return;
        End();
    }

    /// <summary>react-range's onEnd: the drag ends, and its value is final.</summary>
    private void End()
    {
        var pointer = _pointer;
        _pointer = null;
        if (pointer is not null && _track is not null && pointer.Captured == _track) pointer.Capture(null);
        if (_dragged == -1) return;
        Drag(-1);
        FireFinalChange();
    }

    /// <summary>
    /// The slider is disabled or leaves the tree: a drag ends, a pointer's or a held key's, whose release it won't see,
    /// and no thumb is hovered.
    /// </summary>
    private void EndAll()
    {
        if (_pointer is not null) End();
        EndKeyDrag();
        for (var i = 0; i < _hovered.Count; i++) _hovered[i] = false;
        UpdateLabels();
    }

    /// <summary>
    /// react-range's onMove, for a dragged thumb: the pointer's place along the line as a value, taken only when it is at
    /// least half a step from the thumb's.
    /// </summary>
    private void MoveTo(double x)
    {
        if (_dragged < 0 || _dragged >= Value.Count || _inner is null) return;
        var length = _inner.Bounds.Width;
        if (length <= 0) return;
        var value = x / length * (Max - Min) + Min;
        if (Math.Abs(Value[_dragged] - value) >= Step / 2) Move(_dragged, value);
    }

    /// <summary>A thumb moved toward a value: react-range's normalizeValue, then onChange.</summary>
    private void Move(int index, double value)
    {
        var next = Value.ToArray();
        next[index] = Normalize(value, index, Min, Max, Step, Value);
        // StatefulSliderContainer's onChange: its state first, then the app's handler.
        SetCurrentValue(ValueProperty, next);
        RaiseEvent(new SliderChangeEventArgs(ChangeEvent, Value));
    }

    /// <summary>
    /// Whether the minimum is below the maximum. react-range throws otherwise (checkBoundaries); here, as Min and Max are
    /// set one at a time, such a slider is drawn and takes no input until they are in order.
    /// </summary>
    private bool HasRange => Min < Max;

    /// <summary>Automation's set: the value normalised, as a step's, and final at once.</summary>
    internal void SetFromAutomation(int index, double value)
    {
        if (index < 0 || index >= Value.Count || !HasRange) return;
        Move(index, value);
        FireFinalChange();
    }

    /// <summary>A drag a held key started ends, and its change is final: the key's release won't come to the thumb.</summary>
    private void EndKeyDrag()
    {
        if (_pointer is not null || _dragged < 0) return;
        var changed = _isChanged;
        Drag(-1);
        if (changed) FireFinalChange();
    }

    /// <summary>react-range's fireOnFinalChange.</summary>
    private void FireFinalChange()
    {
        _isChanged = false;
        RaiseEvent(new SliderChangeEventArgs(FinalChangeEvent, Value));
    }

    /// <summary>
    /// react-range's normalizeValue (react-range/lib/utils.js): a thumb stays between its neighbours and inside min and
    /// max, and lands on a step from min, rounded to the step's decimals. Here a neighbour holds the thumb whatever its
    /// value; react-range tests one by its truthiness, so a neighbour at 0 doesn't, and the two thumbs can cross
    /// (docs/controls/Slider/, known differences).
    /// </summary>
    internal static double Normalize(double value, int index, double min, double max, double step, IReadOnlyList<double> values)
    {
        const double Big = 10e10;
        value = SliderInnerTrack.JsRound(value * Big) / Big;
        if (index > 0 && values[index - 1] > value) return values[index - 1];
        if (index + 1 < values.Count && values[index + 1] < value) return values[index + 1];
        if (value > max) return max;
        if (value < min) return min;
        var remainder = Math.Floor(value * Big - min * Big) % Math.Floor(step * Big);
        var closestLower = Math.Floor(value * Big - Math.Abs(remainder));
        var rounded = remainder == 0 ? value : closestLower / Big;
        var result = Math.Abs(remainder / Big) < step / 2 ? rounded : rounded + step;
        return Math.Round(result, Math.Min(15, StepDecimals(step)), MidpointRounding.AwayFromZero);
    }

    /// <summary>react-range's getStepDecimals: the digits after the point of the step, as JavaScript writes it.</summary>
    private static int StepDecimals(double step)
    {
        var text = JsNumber(step);
        var dot = text.IndexOf('.');
        return dot < 0 ? 0 : text.Length - dot - 1;
    }

    /// <summary>
    /// A number as JavaScript writes it (<c>String(n)</c>, ECMAScript's Number::toString): the shortest digits that read
    /// back, in plain notation from 1e-6 up to 1e21, and as <c>1.5e-7</c> or <c>1e+21</c> beyond.
    /// </summary>
    internal static string JsNumber(double value)
    {
        if (value == 0) return "0";
        if (double.IsNaN(value)) return "NaN";
        if (double.IsInfinity(value)) return value > 0 ? "Infinity" : "-Infinity";
        // .NET's shortest round-trip digits ("R"), as ECMAScript's digits and n, where the point goes after them.
        var (digits, n) = Digits(Math.Abs(value).ToString("R", CultureInfo.InvariantCulture));
        var k = digits.Length;
        string text;
        if (k <= n && n <= 21) text = digits + new string('0', n - k);
        else if (0 < n && n <= 21) text = digits[..n] + "." + digits[n..];
        else if (-6 < n && n <= 0) text = "0." + new string('0', -n) + digits;
        else
        {
            var exponent = n - 1;
            text = digits[..1] + (k > 1 ? "." + digits[1..] : "") + "e" + (exponent < 0 ? "-" : "+") + Math.Abs(exponent).ToString(CultureInfo.InvariantCulture);
        }

        return value < 0 ? "-" + text : text;
    }

    /// <summary>A shortest round-trip text, plain (<c>0.00123</c>, <c>4500</c>) or with an exponent (<c>1.5E-07</c>), as its significant digits and ECMAScript's n.</summary>
    private static (string Digits, int N) Digits(string text)
    {
        var at = text.IndexOf('E');
        var mantissa = at < 0 ? text : text[..at];
        var exponent = at < 0 ? 0 : int.Parse(text[(at + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
        var dot = mantissa.IndexOf('.');
        var whole = dot < 0 ? mantissa : mantissa[..dot];
        var all = whole + (dot < 0 ? "" : mantissa[(dot + 1)..]);
        var n = whole.Length + exponent;
        var trimmed = all.TrimStart('0');
        n -= all.Length - trimmed.Length;
        return (trimmed.TrimEnd('0'), n);
    }

    /// <summary>react-range's getClosestThumbIndex: the thumb whose centre is nearest along the line, the first on a tie.</summary>
    private int Closest(double x)
    {
        var best = 0;
        var distance = double.PositiveInfinity;
        for (var i = 0; i < _thumbs.Count; i++)
        {
            var b = _thumbs[i].Bounds;
            var d = Math.Abs(x - (b.X + b.Width / 2));
            if (d < distance)
            {
                distance = d;
                best = i;
            }
        }

        return best;
    }

    /// <summary>The thumb an event came from, or one of its children, by index; -1 for none.</summary>
    private int IndexOf(object? source)
    {
        for (var v = source as Visual; v is not null && v != this; v = v.GetVisualParent())
        {
            if (v is SliderThumb thumb && thumb.Owner == this) return thumb.Index;
        }

        return -1;
    }

    /// <summary>react-range's thumb.focus(): a pointer's focus, which shows no ring.</summary>
    private void FocusThumb(int index)
    {
        if (index < _thumbs.Count && !_thumbs[index].IsFocused) _thumbs[index].Focus(NavigationMethod.Pointer);
    }

    /// <summary>react-range's draggedThumbIndex, which a key's press sets too (and with it isChanged).</summary>
    private void Drag(int index, bool keyed = false)
    {
        if (keyed) _isChanged = true;
        if (_dragged == index) return;
        _dragged = index;
        UpdateDragged();
        UpdateLabels();
    }

    // --- The thumbs, their labels and the marks.

    /// <summary>A thumb for each value, each placed, labelled and in its state.</summary>
    private void Sync()
    {
        var count = Value.Count;
        // The dragged thumb taken away: its drag ends, final, and the pointer is let go.
        if (_dragged >= count)
        {
            if (_pointer is not null) End();
            else Drag(-1);
        }

        while (_thumbs.Count > count) RemoveThumb();
        while (_thumbs.Count < count) AddThumb();
        if (_focused >= count) _focused = -1;
        SetState(":range", count == 2);
        UpdateText();
        UpdateDragged();
        UpdateFocus();
        UpdateLabels();
        Moved();
    }

    /// <summary>
    /// The thumbs' places, their labels' text and their peers: all a change of value moves when the thumbs stay the same,
    /// as on each step of a drag.
    /// </summary>
    private void Moved()
    {
        var range = Max - Min;
        if (_inner is not null) _inner.Positions = Value.Select(v => range == 0 ? 0 : (v - Min) / range).ToArray();
        for (var i = 0; i < _labels.Count && i < Value.Count; i++) _labels[i].Value.Content = Label(Value[i]);
        foreach (var thumb in _thumbs) thumb.Peer?.Update();
    }

    private void AddThumb()
    {
        var index = _thumbs.Count;
        var thumb = new SliderThumb { Owner = this, Index = index };
        Themed(thumb, ThumbPart);
        thumb.PointerEntered += OnThumbPointer;
        thumb.PointerMoved += OnThumbPointer;
        thumb.PointerExited += OnThumbExited;
        thumb.GotFocus += OnThumbGotFocus;
        thumb.LostFocus += OnThumbLostFocus;
        var value = new ContentPresenter();
        Themed(value, ThumbValuePart);
        var inner = new Border();
        Themed(inner, InnerThumbPart);
        _thumbs.Add(thumb);
        _labels.Add((value, inner));
        _hovered.Add(false);
        _parts.Add(ThumbPart, thumb);
        _parts.Add(ThumbValuePart, value);
        _parts.Add(InnerThumbPart, inner);
        _inner?.Children.Add(thumb);
    }

    private void RemoveThumb()
    {
        var index = _thumbs.Count - 1;
        var thumb = _thumbs[index];
        var (value, inner) = _labels[index];
        if (_focused == index)
        {
            _focused = -1;
            _focusVisible = false;
        }
        thumb.PointerEntered -= OnThumbPointer;
        thumb.PointerMoved -= OnThumbPointer;
        thumb.PointerExited -= OnThumbExited;
        thumb.GotFocus -= OnThumbGotFocus;
        thumb.LostFocus -= OnThumbLostFocus;
        thumb.Children.Clear();
        _inner?.Children.Remove(thumb);
        _parts.Remove(thumb);
        _parts.Remove(value);
        _parts.Remove(inner);
        thumb.Owner = null;
        _thumbs.RemoveAt(index);
        _labels.RemoveAt(index);
        _hovered.RemoveAt(index);
    }

    /// <summary>
    /// A mark at each step (react-range's numOfMarks + 1), when <see cref="Marks"/> is on: the marks there are kept, and
    /// only the difference made or let go.
    /// </summary>
    private void RebuildMarks()
    {
        var steps = (Max - Min) / Step;
        var count = Marks && steps > 0 && steps < int.MaxValue - 1 ? (int)Math.Ceiling(steps + 1) : 0;
        while (_marks.Count > count)
        {
            var mark = _marks[^1];
            _marks.RemoveAt(_marks.Count - 1);
            _inner?.Children.Remove(mark);
            _parts.Remove(mark);
        }

        while (_marks.Count < count)
        {
            var mark = new Border();
            Themed(mark, MarkPart);
            _inner?.Children.Insert(_marks.Count, mark);
            _marks.Add(mark);
            _parts.Add(MarkPart, mark);
        }

        _inner?.SetMarks(count, steps);
    }

    /// <summary>Base Web renders a thumb's ThumbValue and InnerThumb only while it shows its label.</summary>
    private void UpdateLabels()
    {
        for (var i = 0; i < _thumbs.Count; i++)
        {
            // slider.js: persistentThumb, or (hovered or dragged) and not disabled.
            var show = PersistentThumb || ((_hovered[i] || _dragged == i) && IsEffectivelyEnabled);
            var thumb = _thumbs[i];
            var (value, inner) = _labels[i];
            if (show && thumb.Children.Count == 0)
            {
                thumb.Children.Add(value);
                thumb.Children.Add(inner);
            }
            else if (!show && thumb.Children.Count > 0)
            {
                thumb.Children.Clear();
            }
        }
    }

    /// <summary>The thumbs' labels and the ticks: <see cref="ValueToLabel"/>, or the number.</summary>
    private void UpdateText()
    {
        for (var i = 0; i < _labels.Count && i < Value.Count; i++) _labels[i].Value.Content = Label(Value[i]);
        if (_minTick is not null) _minTick.Content = Label(Min);
        if (_maxTick is not null) _maxTick.Content = Label(Max);
    }

    private object? Label(double value) => ValueToLabel is { } f ? f(value) : JsNumber(value);

    /// <summary>$isDragged: the Track's and InnerTrack's for any thumb, each thumb's and its label's for its own.</summary>
    private void UpdateDragged()
    {
        var any = _dragged >= 0;
        PseudoClasses.Set(":is-dragged", any);
        if (_parts[TrackPart] is { } track) Set(track, ":is-dragged", any);
        if (_parts[InnerTrackPart] is { } inner) Set(inner, ":is-dragged", any);
        for (var i = 0; i < _thumbs.Count; i++)
        {
            var own = _dragged == i;
            Set(_thumbs[i], ":is-dragged", own);
            Set(_labels[i].Value, ":is-dragged", own);
            Set(_labels[i].Inner, ":is-dragged", own);
        }
    }

    /// <summary>$isFocusVisible: the slider's on every part, and on each thumb, its own (slider.js's Thumb props).</summary>
    private void UpdateFocus()
    {
        PseudoClasses.Set(":focus-visible", _focusVisible);
        _parts.SetState(":focus-visible", _focusVisible, except: ThumbPart);
        for (var i = 0; i < _thumbs.Count; i++) Set(_thumbs[i], ":focus-visible", _focusVisible && _focused == i);
    }

    // slider.js's onMouseEnter and onMouseLeave on a thumb: Metered's hover is a mouse's (ADR 0001).
    private void OnThumbPointer(object? sender, PointerEventArgs e)
    {
        if (sender is not SliderThumb thumb || e.Pointer.Type != PointerType.Mouse || thumb.Index >= _hovered.Count) return;
        // Over the thumb or its label, which overflows it: mouseenter and mouseleave count a thumb's children as the thumb.
        var over = IsEffectivelyEnabled && thumb.InputHitTest(e.GetPosition(thumb)) is not null;
        SetHovered(thumb.Index, over);
    }

    private void OnThumbExited(object? sender, PointerEventArgs e)
    {
        if (sender is SliderThumb thumb && e.Pointer.Type == PointerType.Mouse && thumb.Index < _hovered.Count) SetHovered(thumb.Index, false);
    }

    private void SetHovered(int index, bool over)
    {
        if (_hovered[index] == over) return;
        _hovered[index] = over;
        UpdateLabels();
    }

    // slider.js's handleFocus and handleBlur, on the root: isFocusVisible when the focus came by keyboard.
    private void OnThumbGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not SliderThumb thumb || e.Source != thumb) return;
        _focused = thumb.Index;
        _focusVisible = e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional;
        UpdateFocus();
    }

    private void OnThumbLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is not SliderThumb thumb || e.Source != thumb) return;
        _focusVisible = false;
        _focused = -1;
        UpdateFocus();
        // A key held on the thumb: its release goes elsewhere now.
        if (_dragged == thumb.Index) EndKeyDrag();
    }

    /// <summary>
    /// The part's own theme for an element the slider makes, as a template's <c>{DynamicResource}</c> gives one, beneath
    /// an override's (<see cref="Parts"/>).
    /// </summary>
    /// <remarks>The element's own resource lookup, as a template's is, so that an element let go of holds nothing of the slider's.</remarks>
    private void Themed(StyledElement element, string part) =>
        element.Bind(ThemeProperty, element.GetResourceObservable(_parts.ThemeKey(part)), BindingPriority.Template);

    private void Detach()
    {
        if (_track is not null)
        {
            _track.RemoveHandler(PointerPressedEvent, OnTrackPressing);
            _track.RemoveHandler(PointerPressedEvent, OnTrackPressed);
            _track.RemoveHandler(PointerMovedEvent, OnTrackMoved);
            _track.RemoveHandler(PointerReleasedEvent, OnTrackReleased);
            _track.RemoveHandler(PointerCaptureLostEvent, OnTrackCaptureLost);
        }

        if (_pointer is not null) End();
        _inner?.Children.Clear();
        if (_minTick is not null) _parts.Remove(_minTick);
        if (_maxTick is not null) _parts.Remove(_maxTick);
        _track = null;
        _inner = null;
        _minTick = _maxTick = null;
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    private static void Set(StyledElement element, string pseudoClass, bool on) =>
        ((IPseudoClasses)element.Classes).Set(pseudoClass, on);
}
