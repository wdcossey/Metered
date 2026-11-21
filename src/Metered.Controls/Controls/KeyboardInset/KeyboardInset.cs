using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Keeps its child clear of a phone's soft keyboard, as a phone's browser keeps a page clear of it, which Base Web relies
/// on (#108). While the platform's input pane (<see cref="TopLevel.InputPane"/>) is open, the child's foot is padded by
/// as much of this decorator as the keyboard covers, on top of its own <see cref="Decorator.Padding"/>, so a
/// <see cref="ScrollViewer"/> inside ends above the keyboard and can scroll its last control there. When the keyboard
/// opens, the focused control is brought into view if it is inside. Opt-in: an app puts one round the content it wants
/// kept clear, its page or its whole view. On a platform with no input pane, as Avalonia's headless one, it does nothing.
/// <para>
/// Why an app needs it: Avalonia 12.1.4 reports the keyboard but moves nothing for it. On Android 15 and later an app
/// that targets API 35 or later is always drawn edge to edge, and such a window is never resized for the keyboard, so
/// <c>adjustResize</c> does nothing; <c>adjustPan</c> pans to the focused Android view, and Avalonia's whole UI is one
/// view, so it has nothing smaller to pan to. <see cref="TopLevel.AutoSafeAreaPadding"/> pads for the system bars, not
/// the keyboard.
/// </para>
/// <para>
/// The pane is read when the decorator is attached, when it says its state changed, and when its top level resizes.
/// Android raises <see cref="IInputPane.StateChanged"/> as the keyboard's insets start to animate, with its final rect
/// (<c>AndroidInsetsManager.OnStart</c>, API 31 and later), and a rotation needn't animate them. The inset is applied at
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

    static KeyboardInset()
    {
        AffectsMeasure<KeyboardInset>(InsetProperty);
    }

    /// <summary>
    /// How far the child's foot is padded for the keyboard, beyond <see cref="Decorator.Padding"/>: as much of this
    /// decorator as the open keyboard covers, or 0.
    /// </summary>
    public double Inset
    {
        get => _inset;
        private set => SetAndRaise(InsetProperty, ref _inset, value);
    }

    /// <summary>
    /// The input pane read in place of the top level's, for a test: Avalonia's headless platform has none, and Avalonia's
    /// <see cref="IInputPane"/> can't be implemented outside it. Set before the decorator is attached.
    /// </summary>
    internal IPane? InputPane { get; set; }

    /// <summary>What the decorator reads of an input pane: <see cref="IInputPane"/>'s members, which a test can stand in for.</summary>
    internal interface IPane
    {
        InputPaneState State { get; }

        Rect OccludedRect { get; }

        event EventHandler<InputPaneStateEventArgs>? StateChanged;
    }

    /// <summary>The platform's input pane, as <see cref="IPane"/>.</summary>
    private sealed class PlatformPane(IInputPane pane) : IPane
    {
        public InputPaneState State => pane.State;

        public Rect OccludedRect => pane.OccludedRect;

        public event EventHandler<InputPaneStateEventArgs>? StateChanged
        {
            add => pane.StateChanged += value;
            remove => pane.StateChanged -= value;
        }
    }

    protected override Size MeasureOverride(Size availableSize) =>
        LayoutHelper.MeasureChild(Child, availableSize, Padded);

    protected override Size ArrangeOverride(Size finalSize) =>
        LayoutHelper.ArrangeChild(Child, finalSize, Padded);

    private Thickness Padded => Padding + new Thickness(0, 0, 0, Inset);

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        _pane = InputPane ?? (_top?.InputPane is { } pane ? new PlatformPane(pane) : null);
        if (_pane is null)
            return;

        _pane.StateChanged += OnStateChanged;
        if (_top is not null)
            _top.PropertyChanged += OnTopPropertyChanged;
        // After the first layout, so that the decorator's place is known: a keyboard already up stays clear of.
        Dispatcher.UIThread.Post(Read, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_pane is not null)
            _pane.StateChanged -= OnStateChanged;
        if (_top is not null)
            _top.PropertyChanged -= OnTopPropertyChanged;
        _pane = null;
        _top = null;
        Inset = 0;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        if (_pane is null)
            return;
        var open = e.NewState == InputPaneState.Open;
        Apply(open, e.EndRect);
        if (open)
            BringFocusIntoView();
    }

    private void OnTopPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TopLevel.ClientSizeProperty)
            Dispatcher.UIThread.Post(Read, DispatcherPriority.Loaded);
    }

    /// <summary>The pane as it is now, against the decorator's place once it's laid out.</summary>
    private void Read()
    {
        if (_pane is not { } pane)
            return;
        UpdateLayout();
        Apply(pane.State == InputPaneState.Open, pane.OccludedRect);
    }

    /// <summary>
    /// The inset for a keyboard <paramref name="open"/> or not, at <paramref name="occluded"/> in the top level's
    /// coordinates: as much of the decorator as lies below the keyboard's top edge. Computed once per reading, never from
    /// a layout pass, so a decorator whose height follows its child's can't feed its own inset back into it.
    /// </summary>
    private void Apply(bool open, Rect occluded)
    {
        Inset = open && occluded.Height > 0 && _top is { } top && this.TranslatePoint(default, top) is { } origin
            ? Math.Clamp(origin.Y + Bounds.Height - occluded.Y, 0, Bounds.Height)
            : 0;
    }

    /// <summary>
    /// The focused control, if it is inside, brought into view once the inset is laid out, as a phone's browser scrolls
    /// the focused field into the space above the keyboard.
    /// </summary>
    private void BringFocusIntoView()
    {
        if (_top?.FocusManager?.GetFocusedElement() is not Control focused || !this.IsVisualAncestorOf(focused))
            return;
        UpdateLayout();
        focused.BringIntoView();
    }
}
