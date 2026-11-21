using System.Collections.Specialized;
using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Transformation;
using Avalonia.Metadata;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Popover</c> and <c>StatefulPopover</c> (baseui 18.2.0, <c>popover/popover.js</c>,
/// <c>popover/stateful-container.js</c>) in one: content shown in a layer against its anchor, opened by a click or a
/// hover, placed as popper.js places it (<see cref="Tether"/>), dismissed by a click outside and Escape
/// (<see cref="Layers"/>), with its entrance, its focus handling, its three named parts and their overrides.
/// </summary>
/// <remarks>
/// <para>The anchor is Base Web's <c>children</c>: <see cref="Anchor"/>, the XAML content. The popover's content is
/// Base Web's <c>content</c>: <see cref="Content"/>, any content (text, a control, data with
/// <see cref="ContentTemplate"/>), or a <c>Func&lt;Action, object?&gt;</c> called with a function that closes it, as
/// StatefulPopover's render prop is; it is shown by the <c>Inner</c> part.</para>
/// <para>Parts, by Base Web's names: <c>Body</c> (the box placed against the anchor), <c>Arrow</c>, <c>Inner</c>. States,
/// set on the control and each part (popover.js's sharedProps): <c>:is-open</c>, <c>:is-animating</c>,
/// <c>:show-arrow</c>, <c>:is-hover-trigger</c>, and <c>:placement-*</c>, the placement popper.js chose
/// (<c>:placement-bottom-left</c>).</para>
/// <para>Controlled and stateful are one, as ButtonGroup's and RadioGroup's are: <see cref="IsOpen"/> is set by a click
/// on the anchor (or a hover), and cleared by a click outside and Escape (<see cref="DismissOnClickOutside"/>,
/// <see cref="DismissOnEsc"/>), whatever bound it. <see cref="Open"/> and <see cref="Close"/> are raised as it changes,
/// <see cref="ClickOutside"/> and <see cref="Esc"/> as Base Web's <c>onClickOutside</c> and <c>onEsc</c> are.</para>
/// <para>Touch first: a finger never hovers, so a hover popover isn't opened by a tap; a mouse press outside closes a
/// popover, a finger's tap outside does, and a finger's drag (the page scrolled) doesn't (docs/controls/Popover).</para>
/// </remarks>
[TemplatePart(AnchorPresenterPart, typeof(ContentPresenter))]
[TemplatePart(PopupPart, typeof(Popup))]
[TemplatePart(BodyPart, typeof(StyledElement))]
[TemplatePart(ArrowPart, typeof(StyledElement))]
[TemplatePart(InnerPart, typeof(ContentPresenter))]
[PseudoClasses(":is-open", ":is-animating", ":show-arrow", ":is-hover-trigger", ":placement-top-left", ":placement-top", ":placement-top-right",
    ":placement-right-top", ":placement-right", ":placement-right-bottom", ":placement-bottom-right", ":placement-bottom", ":placement-bottom-left",
    ":placement-left-bottom", ":placement-left", ":placement-left-top")]
public class Popover : TemplatedControl
{
    public const string AnchorPresenterPart = "PART_AnchorPresenter";
    public const string PopupPart = "PART_Popup";
    public const string BodyPart = "Body";
    public const string ArrowPart = "Arrow";
    public const string InnerPart = "Inner";

    /// <summary>Base Web's override keys (types.d.ts's PopoverOverrides).</summary>
    internal static readonly string[] PartNames = [BodyPart, ArrowPart, InnerPart];

    /// <summary>Base Web's <c>ANIMATE_IN_TIME</c>: the entrance starts this long after the body is placed.</summary>
    internal const int AnimateInTime = 20;

    /// <summary>Base Web's <c>ARROW_SIZE</c>, the arrow's reach, which the body's margin and entrance allow for.</summary>
    internal const double ArrowSize = 6;

    public static readonly StyledProperty<object?> AnchorProperty =
        AvaloniaProperty.Register<Popover, object?>(nameof(Anchor));

    public static readonly StyledProperty<IDataTemplate?> AnchorTemplateProperty =
        AvaloniaProperty.Register<Popover, IDataTemplate?>(nameof(AnchorTemplate));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<Popover, object?>(nameof(Content));

    public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
        AvaloniaProperty.Register<Popover, IDataTemplate?>(nameof(ContentTemplate));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<PopoverTriggerType> TriggerTypeProperty =
        AvaloniaProperty.Register<Popover, PopoverTriggerType>(nameof(TriggerType));

    public static readonly StyledProperty<PopoverPlacement> PlacementProperty =
        AvaloniaProperty.Register<Popover, PopoverPlacement>(nameof(Placement));

    public static readonly StyledProperty<bool> ShowArrowProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(ShowArrow));

    public static readonly StyledProperty<double> PopoverMarginProperty =
        AvaloniaProperty.Register<Popover, double>(nameof(PopoverMargin), 8);

    public static readonly StyledProperty<int> MouseEnterDelayProperty =
        AvaloniaProperty.Register<Popover, int>(nameof(MouseEnterDelay), 200);

    public static readonly StyledProperty<int> MouseLeaveDelayProperty =
        AvaloniaProperty.Register<Popover, int>(nameof(MouseLeaveDelay), 200);

    public static readonly StyledProperty<int> AnimateOutTimeProperty =
        AvaloniaProperty.Register<Popover, int>(nameof(AnimateOutTime));

    public static readonly StyledProperty<bool> AutoFocusProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(AutoFocus), true);

    public static readonly StyledProperty<bool> FocusLockProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(FocusLock));

    public static readonly StyledProperty<bool> ReturnFocusProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(ReturnFocus), true);

    public static readonly StyledProperty<PopoverAccessibilityType> AccessibilityTypeProperty =
        AvaloniaProperty.Register<Popover, PopoverAccessibilityType>(nameof(AccessibilityType), PopoverAccessibilityType.Menu);

    public static readonly StyledProperty<bool> IgnoreBoundaryProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(IgnoreBoundary));

    public static readonly StyledProperty<bool> DismissOnClickOutsideProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(DismissOnClickOutside), true);

    public static readonly StyledProperty<bool> DismissOnEscProperty =
        AvaloniaProperty.Register<Popover, bool>(nameof(DismissOnEsc), true);

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Popover, OverrideCollection?>(nameof(Overrides));

    public static readonly DirectProperty<Popover, PopoverPlacement> ActualPlacementProperty =
        AvaloniaProperty.RegisterDirect<Popover, PopoverPlacement>(nameof(ActualPlacement), o => o.ActualPlacement);

    public static readonly RoutedEvent<RoutedEventArgs> OpenEvent =
        RoutedEvent.Register<Popover, RoutedEventArgs>(nameof(Open), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> CloseEvent =
        RoutedEvent.Register<Popover, RoutedEventArgs>(nameof(Close), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> ClickOutsideEvent =
        RoutedEvent.Register<Popover, RoutedEventArgs>(nameof(ClickOutside), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> EscEvent =
        RoutedEvent.Register<Popover, RoutedEventArgs>(nameof(Esc), RoutingStrategies.Bubble);

    private static readonly PopoverPlacement[] Placements = Enum.GetValues<PopoverPlacement>().Where(p => p != PopoverPlacement.Auto).ToArray();

    /// <summary>Each placement's pseudoclass, <c>:placement-bottom-left</c>.</summary>
    private static readonly string[] PlacementClasses = Placements.Select(p => ":placement-" + Kebab(p)).ToArray();

    private readonly Parts _parts;
    private readonly Dictionary<(AvaloniaObject, AvaloniaProperty), (object Value, IDisposable Frame)> _given = [];
    private ContentPresenter? _anchorPresenter;
    private Popup? _popup;
    private TopLevel? _top;
    private IDisposable? _layer;
    private bool _mounted;
    private bool _animating;
    private PopoverPlacement _actualPlacement;
    private (Rect Anchor, Size Body, Size View, Rect? Bounds, Thickness Margin, double? Arrow)? _tethered;
    private ScrollContentPresenter? _scrollParent;
    private IDisposable? _animateIn, _animateOut, _animateOutComplete, _enter, _leave;
    private (int Time, Easing? Easing, Transitions Transitions)? _exit;
    private Animatable? _exiting;
    private (Point At, TransformOperations Operations)? _translate;
    private bool _unclipped;

    public Popover()
    {
        _parts = new Parts(this, "Popover", PartNames);
    }

    /// <summary>Base Web's <c>children</c>: what the popover is anchored to, and what a click or hover on opens it.</summary>
    [Content]
    public object? Anchor { get => GetValue(AnchorProperty); set => SetValue(AnchorProperty, value); }

    /// <summary>The template the anchor is shown with, as a content control's.</summary>
    public IDataTemplate? AnchorTemplate { get => GetValue(AnchorTemplateProperty); set => SetValue(AnchorTemplateProperty, value); }

    /// <summary>
    /// Base Web's <c>content</c>: what the popover shows, any content; or a <c>Func&lt;Action, object?&gt;</c>, called
    /// each time it opens with a function that closes it.
    /// </summary>
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>The template the content is shown with, as a content control's.</summary>
    public IDataTemplate? ContentTemplate { get => GetValue(ContentTemplateProperty); set => SetValue(ContentTemplateProperty, value); }

    /// <summary>Base Web's <c>isOpen</c>, and StatefulPopover's state: set by the anchor and the dismissals whatever bound it.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Base Web's <c>triggerType</c>: a click (its default) or a mouse's hover opens it.</summary>
    public PopoverTriggerType TriggerType { get => GetValue(TriggerTypeProperty); set => SetValue(TriggerTypeProperty, value); }

    /// <summary>Base Web's <c>placement</c>: where it goes against the anchor, <see cref="PopoverPlacement.Auto"/> by default.</summary>
    public PopoverPlacement Placement { get => GetValue(PlacementProperty); set => SetValue(PlacementProperty, value); }

    /// <summary>Base Web's <c>showArrow</c>.</summary>
    public bool ShowArrow { get => GetValue(ShowArrowProperty); set => SetValue(ShowArrowProperty, value); }

    /// <summary>Base Web's <c>popoverMargin</c>: the gap between the anchor and the body, 8 by default.</summary>
    public double PopoverMargin { get => GetValue(PopoverMarginProperty); set => SetValue(PopoverMarginProperty, value); }

    /// <summary>Base Web's <c>onMouseEnterDelay</c>, in milliseconds: how long a mouse rests on the anchor before it opens.</summary>
    public int MouseEnterDelay { get => GetValue(MouseEnterDelayProperty); set => SetValue(MouseEnterDelayProperty, value); }

    /// <summary>Base Web's <c>onMouseLeaveDelay</c>, in milliseconds: how long after the mouse leaves it closes.</summary>
    public int MouseLeaveDelay { get => GetValue(MouseLeaveDelayProperty); set => SetValue(MouseLeaveDelayProperty, value); }

    /// <summary>Base Web's <c>animateOutTime</c>, in milliseconds: how long it fades out; 0, at once, by default.</summary>
    public int AnimateOutTime { get => GetValue(AnimateOutTimeProperty); set => SetValue(AnimateOutTimeProperty, value); }

    /// <summary>Base Web's <c>autoFocus</c>: focus goes to the first focusable element inside once it is placed.</summary>
    public bool AutoFocus { get => GetValue(AutoFocusProperty); set => SetValue(AutoFocusProperty, value); }

    /// <summary>Base Web's <c>focusLock</c>: Tab stays inside, and focus goes back when it closes (<see cref="ReturnFocus"/>).</summary>
    public bool FocusLock { get => GetValue(FocusLockProperty); set => SetValue(FocusLockProperty, value); }

    /// <summary>Base Web's <c>returnFocus</c>. Avalonia's popup gives focus back to what had it when it opened whatever this says (docs/controls/Popover, Known differences 4); with <see cref="FocusLock"/>, focus goes back to where it was when it closes.</summary>
    public bool ReturnFocus { get => GetValue(ReturnFocusProperty); set => SetValue(ReturnFocusProperty, value); }

    /// <summary>Base Web's <c>accessibilityType</c>.</summary>
    public PopoverAccessibilityType AccessibilityType { get => GetValue(AccessibilityTypeProperty); set => SetValue(AccessibilityTypeProperty, value); }

    /// <summary>Base Web's <c>ignoreBoundary</c>: popper.js's <c>preventOverflow</c> off, so it may leave the window.</summary>
    public bool IgnoreBoundary { get => GetValue(IgnoreBoundaryProperty); set => SetValue(IgnoreBoundaryProperty, value); }

    /// <summary>StatefulPopover's <c>dismissOnClickOutside</c>: a click or tap outside the anchor and the body closes it.</summary>
    public bool DismissOnClickOutside { get => GetValue(DismissOnClickOutsideProperty); set => SetValue(DismissOnClickOutsideProperty, value); }

    /// <summary>StatefulPopover's <c>dismissOnEsc</c>: Escape closes it.</summary>
    public bool DismissOnEsc { get => GetValue(DismissOnEscProperty); set => SetValue(DismissOnEscProperty, value); }

    /// <summary>The parts' overrides: Base Web's <c>overrides</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>The placement popper.js chose while it is shown (Base Web's state's <c>placement</c>); the one asked for otherwise.</summary>
    public PopoverPlacement ActualPlacement { get => _actualPlacement; private set => SetAndRaise(ActualPlacementProperty, ref _actualPlacement, value); }

    /// <summary>StatefulPopover's <c>onOpen</c>: raised as it opens.</summary>
    public event EventHandler<RoutedEventArgs>? Open { add => AddHandler(OpenEvent, value); remove => RemoveHandler(OpenEvent, value); }

    /// <summary>StatefulPopover's <c>onClose</c>: raised as it closes.</summary>
    public event EventHandler<RoutedEventArgs>? Close { add => AddHandler(CloseEvent, value); remove => RemoveHandler(CloseEvent, value); }

    /// <summary>Base Web's <c>onClickOutside</c>: a press (a finger's tap) outside the anchor and the body while open.</summary>
    public event EventHandler<RoutedEventArgs>? ClickOutside { add => AddHandler(ClickOutsideEvent, value); remove => RemoveHandler(ClickOutsideEvent, value); }

    /// <summary>Base Web's <c>onEsc</c>: Escape released while it is the top layer.</summary>
    public event EventHandler<RoutedEventArgs>? Esc { add => AddHandler(EscEvent, value); remove => RemoveHandler(EscEvent, value); }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>The popup the body is shown in, for tests.</summary>
    internal Popup? PopupHost => _popup;

    protected override Type StyleKeyOverride => typeof(Popover);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        Unmount();
        if (_anchorPresenter is not null) Unhook(_anchorPresenter);
        if (_parts[BodyPart] is InputElement oldBody)
        {
            oldBody.PointerEntered -= OnBodyEntered;
            oldBody.PointerExited -= OnBodyExited;
        }

        foreach (var given in _given.Values) given.Frame.Dispose();
        _given.Clear();
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        _anchorPresenter = e.NameScope.Find<ContentPresenter>(AnchorPresenterPart);
        _popup = e.NameScope.Find<Popup>(PopupPart);
        if (_anchorPresenter is not null) Hook(_anchorPresenter);
        if (_parts[BodyPart] is InputElement body)
        {
            body.PointerEntered += OnBodyEntered;
            body.PointerExited += OnBodyExited;
        }

        ActualPlacement = Placement;
        UpdateStates();
        UpdateParts();
        if (IsOpen && _top is not null) Mount();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        if (IsOpen) Dispatcher.UIThread.Post(() => { if (IsOpen && !_mounted && _top is not null) Mount(); }, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        // Leaving the tree, its popup is closed once the tree has been taken down: closing it here, while it is (a window
        // closing with a popover in another's content), would change the overlay layer's children as Avalonia walks them.
        // Avalonia's Popup closes itself only on leaving the logical tree; a host that keeps it there (a tab not shown)
        // would leave it open.
        Unmount(closePopup: false);
        var popup = _popup;
        Dispatcher.UIThread.Post(() => { if (!_mounted && popup is { IsOpen: true }) popup.IsOpen = false; });
        ClearTimers();
        _top = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsOpenProperty) OnIsOpenChanged(change.GetNewValue<bool>());
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
        else if (p == ContentProperty || p == ContentTemplateProperty) GiveContent();
        else if (p == ShowArrowProperty || p == TriggerTypeProperty || p == PopoverMarginProperty || p == FocusLockProperty
            || p == AccessibilityTypeProperty || p == AnimateOutTimeProperty)
        {
            if (p == TriggerTypeProperty) Relayer();
            UpdateStates();
            UpdateParts();
            // The arrow and the margin are in what placing reads (Retether's key): placed again if they changed.
            Retether();
        }
        else if (p == AnchorProperty)
        {
            // What was given the old anchor goes with it.
            if (change.OldValue is AvaloniaObject old) Give(old, AutomationProperties.HelpTextProperty, null);
            if (_mounted) Dispatcher.UIThread.Post(FindScrollParent, DispatcherPriority.Loaded);
            _tethered = null;
            GiveAnchorHelp();
            Retether();
        }
        else if (p == PlacementProperty || p == IgnoreBoundaryProperty)
        {
            if (!_mounted)
            {
                ActualPlacement = Placement;
                UpdateStates();
                UpdateParts();
            }

            _tethered = null;
            Retether();
        }
    }

    // ---- Opening and closing: popover.js's lifecycle ----

    private void OnIsOpenChanged(bool open)
    {
        if (open)
        {
            // popover.js's init: opening while the layer is mounted clears the exit's timers.
            ClearTimers();
            EndExit();
            if (_mounted)
            {
                UpdateStates();
                UpdateParts();
                Retether();
            }
            else if (_top is not null && _popup is not null)
            {
                Mount();
            }

            RaiseEvent(new RoutedEventArgs(OpenEvent, this));
            return;
        }

        _animateIn?.Dispose();
        _animateIn = null;
        _enter?.Dispose();
        _enter = null;
        RaiseEvent(new RoutedEventArgs(CloseEvent, this));
        if (!_mounted) return;
        if (!_animating)
        {
            // Not yet shown: render() unmounts it at once (mountedAndOpen is false).
            Unmount();
            return;
        }

        // Shown: $isOpen goes false at once, its exit over animateOutTime; it is unmounted animateOutTime after
        // animateOut, 20 ms on (popover.js's init and animateOut). The exit's transitions are the body's before the state
        // changes, so its opacity and transform go over them: as the body's own value, above the theme's entrance (a state
        // style), for the exit alone.
        if (AnimateOutTime > 0 && _parts[BodyPart] is Animatable exiting)
        {
            exiting.SetValue(Animatable.TransitionsProperty, ExitTransitions());
            _exiting = exiting;
        }

        UpdateStates();
        UpdateParts();
        _animateOut = DispatcherTimer.RunOnce(() =>
        {
            _animateOut = null;
            _animateOutComplete = DispatcherTimer.RunOnce(() =>
            {
                _animateOutComplete = null;
                Unmount();
            }, TimeSpan.FromMilliseconds(Math.Max(0, AnimateOutTime)));
        }, TimeSpan.FromMilliseconds(AnimateInTime));
    }

    /// <summary>Base Web's Layer mounted: the body shown (still hidden: opacity 0), the layer added.</summary>
    private void Mount()
    {
        // Base Web's layer is in the page: here the window's overlay layer, which every theme's window template has. A
        // window without one (no theme's template) shows no popover rather than failing.
        if (_mounted || _popup is null || _top is null || OverlayLayer.GetOverlayLayer(this) is null) return;
        _mounted = true;
        _animating = false;
        _tethered = null;
        _unclipped = false;
        ActualPlacement = Placement;
        GiveContent();
        UpdateStates();
        UpdateParts();
        // Placed in the window's coordinates, popper.js's offset its own: the anchor moving (a page scrolled) moves the
        // offset, which places it again.
        _popup.PlacementTarget = _top;
        _popup.IsOpen = true;
        Relayer();
        FindScrollParent();
        _top.LayoutUpdated += OnTopLayoutUpdated;
        if (_top is Window window) window.Closing += OnWindowClosing;
        Retether();
    }

    /// <summary>
    /// Base Web's layer for the trigger type: a hover layer has no document-click handler. A new trigger type while it is
    /// shown is a new layer (Base Web's Layer is rendered again with its new handlers), on top.
    /// </summary>
    private void Relayer()
    {
        if (!_mounted || _top is null) return;
        _layer?.Dispose();
        _layer = Layers.Of(_top).Add(new Layer(OnLayerEscape, TriggerType == PopoverTriggerType.Hover ? null : OnDocumentClick));
    }

    /// <summary>
    /// Base Web's layer doesn't clip the body, whose shadow reaches past its box: the popup's host and its template's
    /// elements, up to the overlay layer, clip to their bounds, so that is turned off on them (a host is made for each
    /// opening, its template applied by the time it is laid out and placed: done once it is found). And its host lets Tab out.
    /// </summary>
    private void Unclip()
    {
        for (var v = (_parts[BodyPart] as Visual)?.GetVisualParent(); v is not null and not OverlayLayer; v = v.GetVisualParent())
        {
            if (v is OverlayPopupHost) _unclipped = true;
            v.ClipToBounds = false;
            // Base Web's layer is in the page's tab order: without a focus lock, Tab leaves it. Avalonia's popup host
            // cycles Tab inside itself; Continue lets it out (with the lock, the body cycles: UpdateParts).
            if (v is OverlayPopupHost host) KeyboardNavigation.SetTabNavigation(host, KeyboardNavigationMode.Continue);
        }
    }

    /// <summary>The layer unmounted: hidden, the layer taken away, Base Web's state's placement back to the one asked for.</summary>
    private void Unmount(bool closePopup = true)
    {
        if (!_mounted) return;
        _mounted = false;
        _animating = false;
        EndExit();
        _tethered = null;
        _animateIn?.Dispose();
        _animateIn = null;
        _layer?.Dispose();
        _layer = null;
        if (_top is not null) _top.LayoutUpdated -= OnTopLayoutUpdated;
        if (_top is Window window) window.Closing -= OnWindowClosing;
        if (closePopup && _popup is not null) _popup.IsOpen = false;

        ActualPlacement = Placement;
        UpdateStates();
        UpdateParts();
    }

    /// <summary>
    /// Closed before its window closes: Avalonia 12.1.4 fails taking down a window with an overlay popup open inside
    /// another's (a popover in a popover's content), each closing itself as the overlay layer's children are walked.
    /// </summary>
    /// <remarks>A close a handler cancels leaves the window: the popover, still open, is shown again once the close has
    /// been decided (a closed window has taken it out of the tree by then).</remarks>
    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        Unmount();
        Dispatcher.UIThread.Post(() => { if (IsOpen && !_mounted && _top is not null) Mount(); });
    }

    /// <summary>The exit's transitions taken off the body: it has gone, or opened again.</summary>
    private void EndExit()
    {
        _exiting?.ClearValue(Animatable.TransitionsProperty);
        _exiting = null;
    }

    private void ClearTimers()
    {
        foreach (var t in new[] { _animateIn, _animateOut, _animateOutComplete, _enter, _leave }) t?.Dispose();
        _animateIn = _animateOut = _animateOutComplete = _enter = _leave = null;
    }

    // ---- Placement: TetherBehavior and popper.js ----

    private void OnTopLayoutUpdated(object? sender, EventArgs e) => Retether();

    /// <summary>
    /// popper.js's update: when it opens, and when the anchor or the body changes its box or the window its size (Tether's
    /// componentDidUpdate, popper.js's scroll and resize listeners). The margin is read as popper.js reads it, the body's
    /// own (Base Web's, at the placement its state has then). A new margin (the placement chosen gives the body its own)
    /// is popper.js's next update too: Base Web's runs at the next scroll or resize, its first leaving a body that opens
    /// above or to the left of its anchor with no gap; Metered's runs at once, so it never shows that (docs/controls/Popover).
    /// </summary>
    private void Retether()
    {
        if (!_mounted || _top is null || _popup is null || _parts[BodyPart] is not Avalonia.Layout.Layoutable body) return;
        var anchor = Anchor is Visual v && v.IsAttachedToVisualTree() ? v : _anchorPresenter as Visual ?? this;
        if (anchor.TranslatePoint(default, _top) is not { } anchorAt) return;
        var size = body.Bounds.Size;
        if (!body.IsMeasureValid || (size.Width <= 0 && size.Height <= 0)) return;
        // The arrow's outer size, as popper.js's getOuterSizes: its box once laid out, its width before (just shown).
        double? arrow = ShowArrow && _parts[ArrowPart] is Avalonia.Layout.Layoutable a
            ? (a.Bounds.Width > 0 ? a.Bounds.Width : double.IsNaN(a.Width) ? 0 : a.Width)
            : null;
        var key = (new Rect(anchorAt, anchor.Bounds.Size), size, _top.ClientSize, ScrollParent(), body.Margin, arrow);
        if (_tethered == key) return;
        _tethered = key;
        if (!_unclipped) Unclip();

        var result = Tether.Compute(key.Item1, size, key.Margin, arrow, Placement, new Rect(_top.ClientSize), !IgnoreBoundary, key.Item4);
        ActualPlacement = result.Placement;
        if (_parts[ArrowPart] is { } arrowPart && result.ArrowOffset is { } ao)
        {
            Give(arrowPart, MarginProperty, new Thickness(ao.X, ao.Y, 0, 0));
        }

        UpdateStates();
        UpdateParts();
        // popper.js's offset is the margin box's; Avalonia's popup puts its child's border box at its offset, its margin
        // round it (the popup's deflate), so the body's margin, as it is now, is added.
        _popup.HorizontalOffset = result.Offset.X + body.Margin.Left;
        _popup.VerticalOffset = result.Offset.Y + body.Margin.Top;
        // onPopperUpdate: the entrance 20 ms after it is placed.
        if (IsOpen && !_animating && _animateIn is null)
        {
            _animateIn = DispatcherTimer.RunOnce(AnimateIn, TimeSpan.FromMilliseconds(AnimateInTime));
        }
    }

    /// <summary>
    /// popper.js's <c>preventOverflow</c> boundaries: the anchor's scroll parent, the box its nearest scroll viewer shows
    /// (popper.js's nearest element whose overflow scrolls), found when it opens; none (the window) without one.
    /// </summary>
    private Rect? ScrollParent() =>
        _scrollParent is { } shown && _top is not null && shown.TranslatePoint(default, _top) is { } at ? new Rect(at, shown.Bounds.Size) : null;

    private void FindScrollParent()
    {
        var anchor = Anchor is Visual v && v.IsAttachedToVisualTree() ? v : _anchorPresenter as Visual ?? this;
        _scrollParent = anchor.GetVisualAncestors().OfType<ScrollContentPresenter>().FirstOrDefault();
    }

    private void AnimateIn()
    {
        _animateIn = null;
        if (!IsOpen || !_mounted) return;
        _animating = true;
        UpdateStates();
        UpdateParts();
        MoveFocusInside();
    }

    // ---- States and the values the parts are given ----

    private void UpdateStates()
    {
        SetState(":is-open", IsOpen && _mounted);
        SetState(":is-animating", _animating);
        SetState(":show-arrow", ShowArrow);
        SetState(":is-hover-trigger", TriggerType == PopoverTriggerType.Hover);
        for (var i = 0; i < Placements.Length; i++) SetState(PlacementClasses[i], _actualPlacement == Placements[i]);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    private static string Kebab(PopoverPlacement p)
    {
        var name = p.ToString();
        var sb = new System.Text.StringBuilder();
        foreach (var c in name)
        {
            if (char.IsUpper(c) && sb.Length > 0) sb.Append('-');
            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString();
    }

    /// <summary>
    /// What the parts are given beyond their themes, at the template's priority, below an override (ADR 0001): the body's
    /// margin and transform (getPopoverMarginStyles and getStartPosition of popoverMargin, a number), its exit's
    /// transition (animateOutTime), the arrow shown or not, and what focus and UI Automation need.
    /// </summary>
    private void UpdateParts()
    {
        var showArrow = ShowArrow;
        var arrowSize = showArrow ? ArrowSize : 0;
        if (_parts[BodyPart] is { } body)
        {
            Give(body, MarginProperty, MarginFor(_actualPlacement, arrowSize, PopoverMargin));
            var open = IsOpen && _mounted;
            var start = open && _animating ? new Point(0, 0) : StartFor(_actualPlacement, arrowSize, PopoverMargin);
            Give(body, RenderTransformProperty, Translate(start));
            var tooltip = AccessibilityType == PopoverAccessibilityType.Tooltip;
            Give(body, KeyboardNavigation.TabNavigationProperty, FocusLock && !tooltip ? KeyboardNavigationMode.Cycle : null);
            Give(body, AutomationProperties.ControlTypeOverrideProperty, tooltip ? AutomationControlType.ToolTip : null);
        }

        if (_parts[ArrowPart] is { } arrow) Give(arrow, IsVisibleProperty, showArrow);
        GiveAnchorHelp();
    }

    /// <summary>A translation as CSS's <c>translate()</c>, one instance for each point, so an unchanged one is the same value.</summary>
    private TransformOperations Translate(Point at)
    {
        if (_translate is { } made && made.At == at) return made.Operations;
        var operations = TransformOperations.Parse(FormattableString.Invariant($"translate({at.X}px, {at.Y}px)"));
        _translate = (at, operations);
        return operations;
    }

    /// <summary>Base Web's <c>getPopoverMarginStyles</c>: the gap on the side facing the anchor.</summary>
    internal static Thickness MarginFor(PopoverPlacement placement, double arrowSize, double popoverMargin)
    {
        if (placement == PopoverPlacement.Auto) return default;
        var gap = arrowSize + popoverMargin;
        return Tether.SideOf(placement) switch
        {
            Tether.Side.Top => new Thickness(0, 0, 0, gap),
            Tether.Side.Bottom => new Thickness(0, gap, 0, 0),
            Tether.Side.Left => new Thickness(0, 0, gap, 0),
            _ => new Thickness(gap, 0, 0, 0),
        };
    }

    /// <summary>Base Web's <c>getStartPosition</c> at an offset of 0: the entrance starts this far towards the anchor.</summary>
    internal static Point StartFor(PopoverPlacement placement, double arrowSize, double popoverMargin)
    {
        var margin = (arrowSize > 0 ? arrowSize : popoverMargin) * 2;
        // splitPlacement('auto') is 'auto', not vertical and not 'left'.
        if (placement == PopoverPlacement.Auto) return new Point(-margin, 0);
        return Tether.SideOf(placement) switch
        {
            Tether.Side.Top => new Point(0, margin),
            Tether.Side.Bottom => new Point(0, -margin),
            Tether.Side.Left => new Point(margin, 0),
            _ => new Point(-margin, 0),
        };
    }

    /// <summary>The body's exit: opacity and transform over animateOutTime on easeInCurve (getBodyStyles).</summary>
    private Transitions ExitTransitions()
    {
        // The curve as the theme has it now (a subtree's own theme may give another).
        var themed = this.TryFindResource("animation.easeInCurve", out var e) ? e as Easing : null;
        if (_exit is { } made && made.Time == AnimateOutTime && ReferenceEquals(made.Easing, themed)) return made.Transitions;
        var duration = TimeSpan.FromMilliseconds(AnimateOutTime);
        var easing = themed ?? new LinearEasing();
        Transitions transitions =
        [
            new DoubleTransition { Property = OpacityProperty, Duration = duration, Easing = easing },
            new TransformOperationsTransition { Property = RenderTransformProperty, Duration = duration, Easing = easing },
        ];
        _exit = (AnimateOutTime, themed, transitions);
        return transitions;
    }

    private void GiveContent()
    {
        if (_parts[InnerPart] is not { } inner) return;
        var content = Content is Func<Action, object?> render ? (_mounted ? render(CloseFromContent) : null) : Content;
        Give(inner, ContentPresenter.ContentProperty, content);
    }

    private void CloseFromContent() => SetCurrentValue(IsOpenProperty, false);

    /// <summary>A tooltip describes its anchor (Base Web's <c>aria-describedby</c>): its text is the anchor's help text while open.</summary>
    private void GiveAnchorHelp()
    {
        if (Anchor is not AvaloniaObject anchor) return;
        var text = AccessibilityType == PopoverAccessibilityType.Tooltip && IsOpen && _mounted ? TextOf(_parts[InnerPart] is ContentPresenter p ? p.Content : null) : null;
        Give(anchor, AutomationProperties.HelpTextProperty, text);
    }

    private static string? TextOf(object? content) => content switch
    {
        string s => s,
        TextBlock t => t.Text,
        _ => null,
    };

    /// <summary>
    /// Gives a value at the template's priority, below an override's (Base Web's props are below an override's style); null
    /// lets it go. The same value again is left as it is: a new frame would raise a change for nothing.
    /// </summary>
    private void Give(AvaloniaObject target, AvaloniaProperty property, object? value)
    {
        var key = (target, property);
        var had = _given.TryGetValue(key, out var old);
        if (had && Equals(old.Value, value)) return;
        // The new value before the old one goes, so the property never passes through its default between them.
        if (value is not null && target.SetValue(property, value, BindingPriority.Template) is { } frame) _given[key] = (value, frame);
        else _given.Remove(key);
        if (had) old.Frame.Dispose();
    }

    // ---- Focus: react-focus-lock's MoveFocusInside and FocusLock ----

    /// <summary>
    /// Once placed, focus goes to the first focusable element inside (popover.js: autoFocus, not for a tooltip), unless it
    /// is already inside.
    /// </summary>
    private void MoveFocusInside()
    {
        if (!AutoFocus || AccessibilityType == PopoverAccessibilityType.Tooltip || _parts[BodyPart] is not Visual body || _top is null) return;
        var focused = _top.FocusManager?.GetFocusedElement() as Visual;
        if (Layers.IsWithin(focused, body)) return;
        // The first tab stop inside, in Tab's order (TabIndex, then the tree), as react-focus-lock's.
        var first = body.GetVisualDescendants().OfType<InputElement>()
            .Where(x => x.Focusable && x.IsEffectivelyEnabled && x.IsEffectivelyVisible && KeyboardNavigation.GetIsTabStop(x))
            .Select((x, i) => (x, i)).OrderBy(t => KeyboardNavigation.GetTabIndex(t.x)).ThenBy(t => t.i).Select(t => t.x).FirstOrDefault();
        if (first is null) return;
        var keyboard = focused is StyledElement s && s.Classes.Contains(":focus-visible");
        first.Focus(keyboard ? NavigationMethod.Tab : NavigationMethod.Unspecified);
    }

    // ---- The anchor: popover.js's getAnchorProps and stateful-container.js ----

    private void Hook(ContentPresenter anchor)
    {
        anchor.AddHandler(Button.ClickEvent, OnAnchorButtonClick);
        // Every click that reaches the anchor, as Base Web's onClick on it: one its content handled too.
        anchor.AddHandler(InputElement.TappedEvent, OnAnchorTapped, handledEventsToo: true);
        anchor.AddHandler(InputElement.DoubleTappedEvent, OnAnchorTapped, handledEventsToo: true);
        anchor.PointerEntered += OnAnchorEntered;
        anchor.PointerExited += OnAnchorExited;
        anchor.GotFocus += OnAnchorGotFocus;
        anchor.LostFocus += OnAnchorLostFocus;
    }

    private void Unhook(ContentPresenter anchor)
    {
        anchor.RemoveHandler(Button.ClickEvent, OnAnchorButtonClick);
        anchor.RemoveHandler(InputElement.TappedEvent, OnAnchorTapped);
        anchor.RemoveHandler(InputElement.DoubleTappedEvent, OnAnchorTapped);
        anchor.PointerEntered -= OnAnchorEntered;
        anchor.PointerExited -= OnAnchorExited;
        anchor.GotFocus -= OnAnchorGotFocus;
        anchor.LostFocus -= OnAnchorLostFocus;
    }

    /// <summary>A button anchor's click (a press and release, a tap, Enter or Space): Base Web's onClick on the anchor.</summary>
    private void OnAnchorButtonClick(object? sender, RoutedEventArgs e) => AnchorClick();

    /// <summary>
    /// Any other anchor's click or tap; a button's comes as its Click instead. A second click soon after the first is
    /// Avalonia's double tap, and a browser's click all the same.
    /// </summary>
    private void OnAnchorTapped(object? sender, TappedEventArgs e)
    {
        if (e.Source is Visual source && source.GetSelfAndVisualAncestors().TakeWhile(x => x != _anchorPresenter).OfType<Avalonia.Controls.Button>().Any()) return;
        AnchorClick();
    }

    /// <summary>stateful-container.js's onClick: a click trigger's toggle.</summary>
    private void AnchorClick()
    {
        if (TriggerType != PopoverTriggerType.Click) return;
        SetCurrentValue(IsOpenProperty, !IsOpen);
    }

    /// <summary>A mouse over the anchor (a finger never hovers): open after the enter delay, the leave timer cleared.</summary>
    private void OnAnchorEntered(object? sender, PointerEventArgs e)
    {
        if (TriggerType != PopoverTriggerType.Hover || e.Pointer.Type != PointerType.Mouse) return;
        _leave?.Dispose();
        _leave = null;
        _enter?.Dispose();
        _enter = After(MouseEnterDelay, () => { _enter = null; SetCurrentValue(IsOpenProperty, true); });
    }

    private void OnAnchorExited(object? sender, PointerEventArgs e)
    {
        if (TriggerType != PopoverTriggerType.Hover || e.Pointer.Type != PointerType.Mouse) return;
        _enter?.Dispose();
        _enter = null;
        LeaveLater();
    }

    private void OnBodyEntered(object? sender, PointerEventArgs e)
    {
        if (TriggerType != PopoverTriggerType.Hover || e.Pointer.Type != PointerType.Mouse) return;
        _leave?.Dispose();
        _leave = null;
    }

    private void OnBodyExited(object? sender, PointerEventArgs e)
    {
        if (TriggerType != PopoverTriggerType.Hover || e.Pointer.Type != PointerType.Mouse) return;
        LeaveLater();
    }

    private void LeaveLater()
    {
        _leave?.Dispose();
        _leave = After(MouseLeaveDelay, () => { _leave = null; SetCurrentValue(IsOpenProperty, false); });
    }

    /// <summary>A delay of 0 is none: popover.js calls at once (triggerOnMouseEnterWithDelay).</summary>
    private static IDisposable? After(int ms, Action action)
    {
        if (ms <= 0)
        {
            action();
            return null;
        }

        return DispatcherTimer.RunOnce(action, TimeSpan.FromMilliseconds(ms));
    }

    /// <summary>stateful-container.js's onFocus: a hover trigger opens on keyboard focus (focus-visible).</summary>
    private void OnAnchorGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (TriggerType != PopoverTriggerType.Hover) return;
        if (e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional) SetCurrentValue(IsOpenProperty, true);
    }

    /// <summary>stateful-container.js's onBlur: a hover trigger closes when focus leaves the anchor, unless focusLock or autoFocus.</summary>
    private void OnAnchorLostFocus(object? sender, RoutedEventArgs e)
    {
        if (TriggerType != PopoverTriggerType.Hover || FocusLock || AutoFocus) return;
        if (_top?.FocusManager?.GetFocusedElement() is Visual now && Layers.IsWithin(now, _anchorPresenter)) return;
        SetCurrentValue(IsOpenProperty, false);
    }

    // ---- Dismissal: Base Web's Layer ----

    private void OnLayerEscape()
    {
        RaiseEvent(new RoutedEventArgs(EscEvent, this));
        if (DismissOnEsc) SetCurrentValue(IsOpenProperty, false);
    }

    /// <summary>popover.js's onDocumentClick: a press in the body or the anchor isn't outside.</summary>
    private void OnDocumentClick(Visual? source)
    {
        if (Layers.IsWithin(source, _parts[BodyPart] as Visual) || Layers.IsWithin(source, _anchorPresenter)) return;
        RaiseEvent(new RoutedEventArgs(ClickOutsideEvent, this));
        if (DismissOnClickOutside) SetCurrentValue(IsOpenProperty, false);
    }
}
