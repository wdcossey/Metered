using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// One open layer: Base Web's <c>Layer</c> (<c>layer/layer.js</c>), its escape handler and, for a layer a click opened,
/// its document-click handler; and Metered's back handler, which Base Web has no counterpart for. Given to
/// <see cref="Layers.Add"/>; disposed to take it away.
/// </summary>
internal sealed class Layer
{
    /// <param name="onEscape">Base Web's <c>onEscape</c>: an Escape key released anywhere in the window.</param>
    /// <param name="onDocumentClick">Base Web's <c>onDocumentClick</c>, with the element pressed; null for a hover layer
    /// (<c>isHoverLayer</c>), which has none.</param>
    /// <param name="onBack">Avalonia's back request (a phone's back button or gesture, Alt+Left on desktop): true if the
    /// layer took it (it closed), which handles the request; false, or null, passes it to the layer below.</param>
    public Layer(Action? onEscape, Action<Visual?>? onDocumentClick, Func<bool>? onBack = null)
    {
        OnEscape = onEscape;
        OnDocumentClick = onDocumentClick;
        OnBack = onBack;
    }

    public Action? OnEscape { get; }

    public Action<Visual?>? OnDocumentClick { get; }

    public Func<bool>? OnBack { get; }
}

/// <summary>
/// Base Web's <c>LayersManager</c> (<c>layer/layers-manager.js</c>), one for each top level: the open layers in the
/// order they opened, the last on top. An Escape key's release (Base Web's <c>keyup</c>) goes to the top layer's escape
/// handler only, and a press in the window (Base Web's <c>mousedown</c>, on the document) to the top click layer's
/// document-click handler only, which decides whether it was outside. For everything Base Web shows in a layer (the
/// Popover here).
/// </summary>
/// <remarks>
/// Touch first: a mouse's or a pen's press is the document click, as a browser's <c>mousedown</c>; a finger's is a tap,
/// its release within the platform's tap distance of where it went down, as a phone's browser sends <c>mousedown</c>
/// only for a tap. A finger that drags (a page scrolled) clicks nothing, and the layer stays.
/// <para>Back (Metered's, #123: Base Web has none): Avalonia's back request, not yet handled when it reaches the top level,
/// goes to the layers from the top down, to the first whose back handler takes it, and is handled; none takes it, it is
/// left for the app (the sample's shell goes back).</para>
/// </remarks>
internal sealed class Layers
{
    private static readonly ConditionalWeakTable<TopLevel, Layers> All = new();

    private readonly TopLevel _top;
    private readonly List<Layer> _layers = [];
    private readonly Dictionary<long, (Point At, Visual? Source)> _touches = [];
    private bool _tookBack;

    /// <summary>The fingers down at once that are remembered; a touch cancelled by the system leaves its entry.</summary>
    private const int MaxTouches = 10;

    private Layers(TopLevel top)
    {
        _top = top;
        top.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>
    /// The back request, before an app's own handlers on its top level: a class handler runs before an element's own
    /// (Avalonia's event route), so an app that goes back in its window's <c>BackRequested</c>, added before any popover
    /// opened, still has the top popover closed first. A handler on the request's route below the top level (on the page)
    /// sees it first, and one that handles it leaves the popovers alone.
    /// </summary>
    static Layers()
    {
        TopLevel.BackRequestedEvent.AddClassHandler<TopLevel>((top, e) =>
        {
            if (All.TryGetValue(top, out var layers)) layers.OnBackRequested(top, e);
        });
    }

    /// <summary>The layers of a top level, made the first time it is asked for.</summary>
    public static Layers Of(TopLevel top) => All.GetValue(top, t => new Layers(t));

    /// <summary>Adds a layer on top; dispose the result to take it away.</summary>
    public IDisposable Add(Layer layer)
    {
        _layers.Add(layer);
        return new Removal(this, layer);
    }

    /// <summary>The layers, the top last: for tests.</summary>
    internal IReadOnlyList<Layer> Open => _layers;

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape) return;
        // layers-manager.js: the last escape handler added, whatever kind of layer it is.
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            if (_layers[i].OnEscape is { } escape)
            {
                escape();
                return;
            }
        }
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        // From the top down: a layer that doesn't take it (a popover Escape wouldn't close) lets the one below have it.
        // A copy, as a layer that closes may take itself away.
        foreach (var layer in _layers.AsEnumerable().Reverse().ToArray())
        {
            if (layer.OnBack is { } back && back())
            {
                e.Handled = true;
                // A mouse's back button: Avalonia 12.1.4 raises the request before it routes the button's press, which
                // follows in the same input; that press is the request a layer took, not a press outside as well.
                _tookBack = true;
                Dispatcher.UIThread.Post(() => _tookBack = false, DispatcherPriority.Input);
                return;
            }
        }
    }

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Touch)
        {
            // A touch the system cancels never comes up: no more fingers are down than a hand has, so older ones go.
            if (_touches.Count >= MaxTouches) _touches.Clear();
            _touches[e.Pointer.Id] = (e.GetPosition(_top), e.Source as Visual);
            return;
        }

        // A mouse's back button whose request a layer took (#123) clicks nothing more. One no layer took (a popover Escape
        // wouldn't close) is a press as any other, as a browser's mousedown is.
        if (_tookBack && e.GetCurrentPoint(_top).Properties.PointerUpdateKind == PointerUpdateKind.XButton1Pressed)
        {
            _tookBack = false;
            return;
        }

        DocumentClick(e.Source as Visual);
    }

    private void OnReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Touch || !_touches.Remove(e.Pointer.Id, out var down)) return;
        var tap = Application.Current?.PlatformSettings?.GetTapSize(PointerType.Touch) ?? new Size(10, 10);
        var at = e.GetPosition(_top);
        if (Math.Abs(at.X - down.At.X) > tap.Width / 2 || Math.Abs(at.Y - down.At.Y) > tap.Height / 2) return;
        DocumentClick(down.Source);
    }

    private void DocumentClick(Visual? source)
    {
        // layers-manager.js: the last document-click handler added; a hover layer has none.
        for (var i = _layers.Count - 1; i >= 0; i--)
        {
            if (_layers[i].OnDocumentClick is { } click)
            {
                click(source);
                return;
            }
        }
    }

    /// <summary>
    /// Whether a visual is the element or inside it: by the visual tree (a popup's content is in its host's), or by the
    /// logical tree, which leads from a popup the element's content opened (a text box's menu, a combo box's drop-down,
    /// each in a host of its own) back to it.
    /// </summary>
    public static bool IsWithin(Visual? visual, Visual? element)
    {
        if (visual is null || element is null) return false;
        if (visual == element || element.IsVisualAncestorOf(visual)) return true;
        for (var l = ((ILogical)visual).LogicalParent; l is not null; l = l.LogicalParent)
        {
            if (ReferenceEquals(l, element)) return true;
        }

        return false;
    }

    private sealed class Removal(Layers owner, Layer layer) : IDisposable
    {
        private bool _done;

        public void Dispose()
        {
            if (_done) return;
            _done = true;
            owner._layers.Remove(layer);
        }
    }
}
