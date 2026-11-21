using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// One open layer: Base Web's <c>Layer</c> (<c>layer/layer.js</c>), its escape handler and, for a layer a click opened,
/// its document-click handler. Given to <see cref="Layers.Add"/>; disposed to take it away.
/// </summary>
internal sealed class Layer
{
    /// <param name="onEscape">Base Web's <c>onEscape</c>: an Escape key released anywhere in the window.</param>
    /// <param name="onDocumentClick">Base Web's <c>onDocumentClick</c>, with the element pressed; null for a hover layer
    /// (<c>isHoverLayer</c>), which has none.</param>
    public Layer(Action? onEscape, Action<Visual?>? onDocumentClick)
    {
        OnEscape = onEscape;
        OnDocumentClick = onDocumentClick;
    }

    public Action? OnEscape { get; }

    public Action<Visual?>? OnDocumentClick { get; }
}

/// <summary>
/// Base Web's <c>LayersManager</c> (<c>layer/layers-manager.js</c>), one for each top level: the open layers in the
/// order they opened, the last on top. An Escape key's release (Base Web's <c>keyup</c>) goes to the top layer's escape
/// handler only, and a press in the window (Base Web's <c>mousedown</c>, on the document) to the top click layer's
/// document-click handler only, which decides whether it was outside. Shared by everything Base Web shows in a layer
/// (Popover, and Select's and Menu's dropdowns through it).
/// </summary>
/// <remarks>
/// Touch first: a mouse's or a pen's press is the document click, as a browser's <c>mousedown</c>; a finger's is a tap,
/// its release within the platform's tap distance of where it went down, as a phone's browser sends <c>mousedown</c>
/// only for a tap. A finger that drags (a page scrolled) clicks nothing, and the layer stays.
/// </remarks>
internal sealed class Layers
{
    private static readonly ConditionalWeakTable<TopLevel, Layers> All = new();

    private readonly TopLevel _top;
    private readonly List<Layer> _layers = [];
    private readonly Dictionary<long, (Point At, Visual? Source)> _touches = [];

    private Layers(TopLevel top)
    {
        _top = top;
        top.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        top.AddHandler(InputElement.PointerReleasedEvent, OnReleased, RoutingStrategies.Tunnel, handledEventsToo: true);
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

    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Pointer.Type == PointerType.Touch)
        {
            _touches[e.Pointer.Id] = (e.GetPosition(_top), e.Source as Visual);
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

    /// <summary>Whether a visual is the element or inside it, by the visual tree (a popup's content is in its host's).</summary>
    public static bool IsWithin(Visual? visual, Visual? element)
    {
        if (visual is null || element is null) return false;
        return visual == element || element.IsVisualAncestorOf(visual);
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
