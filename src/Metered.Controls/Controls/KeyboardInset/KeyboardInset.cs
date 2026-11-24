using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Keeps its child clear of a phone's soft keyboard, as a phone's browser keeps a page clear of it, which Base Web relies
/// on (#108). While the platform's input pane (<see cref="TopLevel.InputPane"/>) is open, the child's foot is padded by
/// as much of this decorator as the keyboard covers, on top of its own <see cref="Decorator.Padding"/>, so a
/// <see cref="ScrollViewer"/> inside ends above the keyboard and can scroll its last control there. When the keyboard
/// opens, the focused control is brought into view if it is inside, and then a text box's caret. Opt-in: an app puts
/// one round the content it wants kept clear, its page or its whole view, given its height by its parent. On a platform
/// with no input pane, as Avalonia's headless one, it does nothing. A popup on a phone is an overlay in the same top
/// level, and is measured against the keyboard's rect as anything else; one in a top level of its own, as a desktop's,
/// would be measured in its own coordinates against the window's rect, and is left out.
/// <para>
/// Why an app needs it: Avalonia 12.1.4 reports the keyboard but moves nothing for it. On Android 15 and later an app
/// that targets API 35 or later is always drawn edge to edge, and such a window is never resized for the keyboard, so
/// <c>adjustResize</c> does nothing; <c>adjustPan</c> pans to the focused Android view, and Avalonia's whole UI is one
/// view, so it has nothing smaller to pan to. <see cref="TopLevel.AutoSafeAreaPadding"/> pads for the system bars, not
/// the keyboard.
/// </para>
/// <para>
/// The inset is taken from the room the child is given, never added to the decorator's own size, and a reading lays out
/// its own inset and asks for no other: a change of the decorator's bounds while it does is the reading's own. So a
/// reading can't feed its inset back into the next. The decorator is meant to be given its height (a page's or a view's
/// root, a <see cref="Grid"/> row). One whose height follows its child's stays as high as its child would have it with
/// no keyboard, if it has unbounded room (in a <see cref="StackPanel"/>, say), and its child overflows it, so it keeps
/// nothing clear; with bounded room, a child that fits itself to it (a <see cref="ScrollViewer"/>) shrinks it by the
/// inset, and the inset its reading found stays until the next reading, more than the keyboard then covers.
/// </para>
/// <para>
/// The pane is read when it says its state changed, and, once laid out, when the decorator is attached, when the
/// platform's insets change, and when the decorator moves or resizes while the keyboard is up (a rotation among them).
/// Android raises <see cref="IInputPane.StateChanged"/> only as the keyboard's insets start to animate, with its final
/// rect (<c>AndroidInsetsManager.OnStart</c>, API 31 and later); a rotation, or a keyboard hidden while the app is in the
/// background, changes the pane with no animation, and only the insets' change says so
/// (<see cref="IInsetsManager.SafeAreaChanged"/>, raised from every <c>OnApplyWindowInsets</c>). The inset is applied at
/// once, not over the keyboard's animation. A focus that moves while the keyboard is up is brought into view by the
/// <see cref="ScrollViewer"/> itself (<see cref="ScrollViewer.BringIntoViewOnFocusChange"/>).
/// </para>
/// </summary>
public class KeyboardInset : Decorator
{
    /// <summary>Defines the <see cref="Inset"/> property.</summary>
    public static readonly DirectProperty<KeyboardInset, double> InsetProperty =
        AvaloniaProperty.RegisterDirect<KeyboardInset, double>(nameof(Inset), inset => inset.Inset);

    private double _inset;
    private IPane? _pane;
    private TopLevel? _top;
    private bool _readPosted;

    /// <summary>Whether a reading is laying the decorator out: its own bounds' changes then ask for no other reading.</summary>
    private bool _applying;

    /// <summary>The room left above the keyboard at the last reading: the decorator's height less its inset.</summary>
    private double _room;

    /// <summary>
    /// What the last <see cref="IInputPane.StateChanged"/> said, while a reading it came before is still to run: that
    /// reading takes it over the pane, whose rect may still be the keyboard's at the start of its animation.
    /// </summary>
    private (bool Open, Rect Keyboard)? _said;

    static KeyboardInset()
    {
        AffectsMeasure<KeyboardInset>(InsetProperty);
    }

    /// <summary>
    /// How far the child's foot is padded for the keyboard, beyond <see cref="Decorator.Padding"/>: as much of the
    /// decorator as the open keyboard covers, or 0.
    /// </summary>
    public double Inset
    {
        get => _inset;
        private set => SetAndRaise(InsetProperty, ref _inset, value);
    }

    /// <summary>
    /// The input pane read in place of the top level's, for a test: Avalonia's headless platform has none, and a top
    /// level's <see cref="TopLevel.InputPane"/> can't be set, so a test needs a way in. An interface of its own, as C#
    /// can't implement <see cref="IInputPane"/> (its reference assembly is <c>[NotClientImplementable]</c>, with an
    /// internal member), and it carries the insets' change too. A pane made at runtime by <c>DispatchProxy</c>, as Avonic's
    /// tests make one, would also do, with an <see cref="IInsetsManager"/> made the same way. Set before the decorator is
    /// attached.
    /// </summary>
    internal IPane? InputPane { get; set; }

    /// <summary>What the decorator reads of the platform: an input pane's members, and when the insets change.</summary>
    internal interface IPane
    {
        InputPaneState State { get; }

        Rect OccludedRect { get; }

        event EventHandler<InputPaneStateEventArgs>? StateChanged;

        /// <summary>The platform's insets have changed, the keyboard's among them, whether or not it says so itself.</summary>
        event EventHandler? InsetsChanged;
    }

    /// <summary>The child measured in the room left above the keyboard; the decorator wants what it would without one.</summary>
    protected override Size MeasureOverride(Size availableSize)
    {
        var desired = LayoutHelper.MeasureChild(Child, availableSize, Padded);
        return desired.WithHeight(Math.Max(0, desired.Height - Inset));
    }

    protected override Size ArrangeOverride(Size finalSize) => LayoutHelper.ArrangeChild(Child, finalSize, Padded);

    private Thickness Padded => Padding + new Thickness(0, 0, 0, Inset);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        _pane = InputPane ?? (_top?.InputPane is { } pane ? new PlatformPane(pane, _top.InsetsManager) : null);
        if (_pane is null)
            return;

        _pane.StateChanged += OnStateChanged;
        _pane.InsetsChanged += OnInsetsChanged;
        // Also how a keyboard already up is read: Avalonia raises the effective viewport's first change once the decorator
        // is laid out, attached again in the same place or not.
        EffectiveViewportChanged += OnEffectiveViewportChanged;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_pane is not null)
        {
            _pane.StateChanged -= OnStateChanged;
            _pane.InsetsChanged -= OnInsetsChanged;
            EffectiveViewportChanged -= OnEffectiveViewportChanged;
        }
        _pane = null;
        _top = null;
        _said = null;
        Inset = 0;
        _room = 0;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        // Resized under an open keyboard: the part it covers may have changed. A change while a reading lays it out is
        // that reading's, and asks for no other.
        if (change.Property == BoundsProperty)
            OnMoved();
    }

    /// <summary>
    /// Moved under an open keyboard, its own bounds the same: a parent moved (a window resized with no insets event, a
    /// decorator of a fixed height centred in it). <see cref="Visual.Bounds"/> is its parent's; its effective viewport,
    /// in its own coordinates, moves with it against the top level.
    /// </summary>
    private void OnEffectiveViewportChanged(object? sender, EffectiveViewportChangedEventArgs e) => OnMoved();

    private void OnMoved()
    {
        if (!_applying && _pane?.State == InputPaneState.Open)
            PostRead();
    }

    private void OnStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        var open = e.NewState == InputPaneState.Open;
        // Android posts a reading (SafeAreaChanged, from OnApplyWindowInsets) just before it raises this.
        if (_readPosted)
            _said = (open, e.EndRect);
        Apply(open, e.EndRect);
        if (open)
            BringFocusIntoView();
    }

    private void OnInsetsChanged(object? sender, EventArgs e) => PostRead();

    /// <summary>A reading after the layout and render under way, once however many ask for it before it runs.</summary>
    private void PostRead()
    {
        if (_readPosted)
            return;
        _readPosted = true;
        Dispatcher.UIThread.Post(Read, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The pane as it is now; if that changes the room left above an open keyboard (its inset, or the decorator's
    /// height, as on a rotation), the focused control brought into view again, as a browser keeps the focused field in
    /// view as its visual viewport changes.
    /// </summary>
    private void Read()
    {
        _readPosted = false;
        var said = _said;
        _said = null;
        if (_pane is not { } pane)
            return;
        var (open, keyboard) = said ?? (pane.State == InputPaneState.Open, pane.OccludedRect);
        if (!open && Inset == 0)
            return;
        var before = _room;
        Apply(open, keyboard);
        if (open && _room != before)
            BringFocusIntoView();
    }

    /// <summary>
    /// The inset for a keyboard <paramref name="open"/> or not, at <paramref name="occluded"/> in the top level's
    /// coordinates: where the keyboard's rect overlaps the decorator's, on both axes, as much of the decorator as lies
    /// below the top of that overlap, as it is laid out now; nothing where they don't overlap (a keyboard beside it, as
    /// Windows' touch keyboard floating beside the window, or above or below it). Laid out first, so that a decorator
    /// inside another that has just made room is read in that room, and pads nothing more.
    /// </summary>
    private void Apply(bool open, Rect occluded)
    {
        // Laid out before, and with the inset after, as one reading: a change of its own bounds in either pass is this
        // reading's, not a move to read again, so a layout that disagreed with its inset could never loop. With no
        // keyboard to keep clear of, laid out with none, so that a focus brought into view after is brought into the
        // room as it now is.
        _applying = true;
        try
        {
            if (!open || occluded.Height <= 0 || _top is not { } top)
            {
                _room = 0;
                if (Inset != 0)
                {
                    Inset = 0;
                    UpdateLayout();
                }
                return;
            }

            UpdateLayout();
            Inset = this.TranslatePoint(default, top) is { } origin ? Covered(new Rect(origin, Bounds.Size), occluded) : 0;
            _room = Bounds.Height - Inset;
            UpdateLayout();
        }
        finally
        {
            _applying = false;
        }
    }

    /// <summary>
    /// How much of <paramref name="decorator"/> the keyboard at <paramref name="occluded"/> keeps its child out of, both in
    /// the top level's coordinates: from the top of their overlap to the decorator's foot, the room below the keyboard's
    /// top being lost as a whole; 0 where they don't overlap. An empty intersection is Avalonia's default rect.
    /// </summary>
    private static double Covered(Rect decorator, Rect occluded)
    {
        var overlap = decorator.Intersect(occluded);
        return overlap.Width > 0 && overlap.Height > 0 ? decorator.Bottom - overlap.Top : 0;
    }

    /// <summary>
    /// The focused control, if it is inside, brought into view once the inset is laid out, then its caret if it takes
    /// text, as a phone's browser scrolls the focused field, and the caret in it, into the space above the keyboard. The
    /// caret matters in a text taller than that space: the control alone would be brought in by its top.
    /// </summary>
    private void BringFocusIntoView()
    {
        if (_top?.FocusManager?.GetFocusedElement() is not Control focused || !this.IsVisualAncestorOf(focused))
            return;
        focused.BringIntoView();
        if (CaretOf(focused) is { } caret)
            focused.BringIntoView(caret);
    }

    /// <summary>
    /// A text box's caret, in the text box's own coordinates, where its text's scroller shows it; else null. Read from its
    /// presenter's layout, read-only or not; and asked of the text box itself, not its presenter, so that the page's
    /// scroller takes it, not the text's own.
    /// </summary>
    private static Rect? CaretOf(Control focused)
    {
        if (focused is not TextBox || focused.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault() is not { } presenter
            || presenter.TransformToVisual(focused) is not { } toFocused)
            return null;
        // Its lines only: a caret has no width, and a rect with none intersects nothing.
        var caret = presenter.TextLayout.HitTestTextPosition(presenter.CaretIndex).TransformToAABB(toFocused);
        double top = Math.Max(caret.Top, 0), bottom = Math.Min(caret.Bottom, focused.Bounds.Height);
        return bottom > top ? new Rect(0, top, focused.Bounds.Width, bottom - top) : null;
    }

    /// <summary>The platform's input pane and insets manager, as <see cref="IPane"/>.</summary>
    private sealed class PlatformPane(IInputPane pane, IInsetsManager? insets) : IPane
    {
        private EventHandler? _insetsChanged;

        public InputPaneState State => pane.State;

        public Rect OccludedRect => pane.OccludedRect;

        public event EventHandler<InputPaneStateEventArgs>? StateChanged
        {
            add => pane.StateChanged += value;
            remove => pane.StateChanged -= value;
        }

        public event EventHandler? InsetsChanged
        {
            add
            {
                if (_insetsChanged is null && insets is not null)
                    insets.SafeAreaChanged += OnSafeAreaChanged;
                _insetsChanged += value;
            }
            remove
            {
                _insetsChanged -= value;
                if (_insetsChanged is null && insets is not null)
                    insets.SafeAreaChanged -= OnSafeAreaChanged;
            }
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e) => _insetsChanged?.Invoke(this, EventArgs.Empty);
    }
}
