using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The tabs' line inside a &lt;see cref="TabList"/&gt;, Base Web's &lt;c&gt;StyledTabList&lt;/c&gt; flex box (&lt;c&gt;flex-wrap: nowrap&lt;/c&gt;, its
/// direction the list's &lt;see cref="TabList.Orientation"/&gt;) inside its padding (the list's &lt;see cref="TemplatedControl.Padding"/&gt;,
/// which scrolls with it), and the &lt;see cref="TabHighlight"/&gt;, placed absolutely over the line and the padding.
/// &lt;/summary&gt;
/// &lt;remarks&gt;
/// &lt;para&gt;Each tab is a flex item: its &lt;see cref="TabButton.FlexGrow"/&gt; and &lt;see cref="TabButton.FlexBasis"/&gt; (auto, its
/// max-content length, when NaN), a shrink of 1 and a minimum of its min-content length (CSS's &lt;c&gt;min-width: auto&lt;/c&gt;),
/// their lengths resolved as CSS Flexbox's §9.7 resolves them against the room the list has. A horizontal list that
/// scrolls is given no width by its scroller, so the room is the list's own width, which it reads from the list
/// (&lt;see cref="TabList.Room"/&gt;): tabs that need more overflow it and scroll. Across the line, each tab is as tall (wide)
/// as the line (&lt;c&gt;align-items: stretch&lt;/c&gt;), which is as tall as its tallest.&lt;/para&gt;
/// &lt;para&gt;A mouse over a tab, disabled or not, is its CSS &lt;c&gt;:hover&lt;/c&gt; (&lt;see cref="TabButton.IsHovered"/&gt;): the line
/// follows the mouse, since a disabled button takes no pointer events of its own in Avalonia.&lt;/para&gt;
/// &lt;/remarks&gt;
public class TabListPanel : Panel
{
    private readonly List<(TabButton Tab, double Basis, double Min)> _items = [];
    private TabButton? _hovered;

    public TabListPanel()
    {
        // Hit over its whole box, so the mouse over a disabled tab is the line's.
        Background = Brushes.Transparent;
        AddHandler(PointerMovedEvent, OnPointer, handledEventsToo: true);
        AddHandler(PointerEnteredEvent, OnPointer, handledEventsToo: true);
        AddHandler(PointerExitedEvent, OnExited, handledEventsToo: true);
    }

    /// <summary>The list it is the line of: its direction, padding and room.</summary>
    internal TabList? Owner { get; set; }

    private bool Horizontal => (Owner?.Orientation ?? Orientation.Horizontal) == Orientation.Horizontal;

    private Thickness Pad => Owner?.Padding ?? default;

    private IEnumerable<TabButton> Tabs => Children.OfType<TabButton>().Where(t => t.IsVisible);

    private TabHighlight? Highlight => Children.OfType<TabHighlight>().FirstOrDefault(h => h.IsVisible);

    /// <summary>Along the line or across it: a size's main and cross lengths.</summary>
    private (double Main, double Cross) Split(Size s) => Horizontal ? (s.Width, s.Height) : (s.Height, s.Width);

    private Size Join(double main, double cross) => Horizontal ? new Size(main, cross) : new Size(cross, main);

    protected override Size MeasureOverride(Size availableSize)
    {
        var pad = Pad;
        var (padMain, padCross) = Horizontal ? (pad.Left + pad.Right, pad.Top + pad.Bottom) : (pad.Top + pad.Bottom, pad.Left + pad.Right);
        var (availMain, availCross) = Split(availableSize);
        if (Horizontal && Owner?.Room is { } room && double.IsInfinity(availMain)) availMain = room;
        var crossRoom = Math.Max(0, availCross - padCross);
        var mainRoom = Math.Max(0, availMain - padMain);

        _items.Clear();
        foreach (var tab in Tabs)
        {
            // Its max-content length, then its min-content length (each word of its title on a line of its own, horizontally).
            tab.Measure(Join(double.PositiveInfinity, crossRoom));
            var max = Split(tab.DesiredSize).Main;
            var min = Horizontal ? tab.MinContentWidth() : max;

            var basis = double.IsNaN(tab.FlexBasis) ? max : tab.FlexBasis;
            _items.Add((tab, basis, min));
        }

        var lengths = Resolve(mainRoom);
        double main = 0, cross = 0;
        for (var i = 0; i < _items.Count; i++)
        {
            var tab = _items[i].Tab;
            tab.Measure(Join(lengths[i], crossRoom));
            main += lengths[i];
            cross = Math.Max(cross, Split(tab.DesiredSize).Cross);
        }

        Highlight?.Measure(Size.Infinity);
        return Join(main + padMain, cross + padCross);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var pad = Pad;
        var (finalMain, finalCross) = Split(finalSize);
        var (startMain, startCross) = Horizontal ? (pad.Left, pad.Top) : (pad.Top, pad.Left);
        var (padMain, padCross) = Horizontal ? (pad.Left + pad.Right, pad.Top + pad.Bottom) : (pad.Top + pad.Bottom, pad.Left + pad.Right);
        var lengths = Resolve(Math.Max(0, finalMain - padMain));
        var cross = Math.Max(0, finalCross - padCross);
        // With layout rounding, each edge is rounded where it falls, so the tabs fill their room exactly, each within a
        // device pixel of its length, as a browser snaps the fractions it lays out to the pixels it paints.
        var scale = UseLayoutRounding ? LayoutHelper.GetLayoutScale(this) : 0;
        double Edge(double x) => scale > 0 ? Math.Round(x * scale) / scale : x;
        var at = startMain;
        for (var i = 0; i < _items.Count; i++)
        {
            var from = Edge(at);
            var length = Edge(at + lengths[i]) - from;
            var rect = Horizontal ? new Rect(from, startCross, length, cross) : new Rect(startCross, from, cross, length);
            _items[i].Tab.Arrange(rect);
            at += lengths[i];
        }

        // Absolute, in the list's padding box: along the line by its distance and length, across it the whole box, its
        // thickness and alignment (bottom: 0, right: 0) placing it there.
        if (Highlight is { } h)
        {
            var length = Math.Max(0, h.Length);
            h.Arrange(Horizontal ? new Rect(h.Distance, 0, length, finalSize.Height) : new Rect(0, h.Distance, finalSize.Width, length));
        }

        return finalSize;
    }

    /// <summary>
    /// CSS Flexbox's §9.7, resolving flexible lengths on one line: each item's flex base size, then the free space grown
    /// into by the items' grow factors, or shrunk out of by their shrink factors (1) scaled by their base sizes, each item
    /// that would go below its minimum frozen there and the rest resolved again. With no room given (infinite), each item
    /// is its base size, no less than its minimum.
    /// </summary>
    private double[] Resolve(double room)
    {
        var n = _items.Count;
        var target = new double[n];
        for (var i = 0; i < n; i++) target[i] = Math.Max(_items[i].Basis, _items[i].Min);
        if (n == 0 || double.IsInfinity(room) || double.IsNaN(room)) return target;

        var hypothetical = target.Sum();
        var grow = hypothetical < room;
        var frozen = new bool[n];
        for (var i = 0; i < n; i++)
        {
            var factor = grow ? _items[i].Tab.FlexGrow : 1;
            // An item that can't flex, or whose base is already past where it would flex to, keeps its hypothetical length.
            if (factor <= 0 || (grow && _items[i].Basis > target[i]) || (!grow && _items[i].Basis < target[i])) frozen[i] = true;
            else target[i] = _items[i].Basis;
        }

        for (var guard = 0; guard <= n; guard++)
        {
            if (frozen.All(f => f)) break;
            var used = 0.0;
            var factors = 0.0;
            for (var i = 0; i < n; i++)
            {
                if (frozen[i]) used += target[i];
                else
                {
                    used += _items[i].Basis;
                    factors += grow ? _items[i].Tab.FlexGrow : _items[i].Basis;
                }
            }

            var free = room - used;
            for (var i = 0; i < n; i++)
            {
                if (frozen[i]) continue;
                var share = factors <= 0 ? 0 : (grow ? _items[i].Tab.FlexGrow : _items[i].Basis) / factors;
                target[i] = _items[i].Basis + free * share;
            }

            // Fix the minimum violations, freezing them; no violation ends it.
            var violated = false;
            for (var i = 0; i < n; i++)
            {
                if (frozen[i] || target[i] >= _items[i].Min) continue;
                target[i] = _items[i].Min;
                frozen[i] = true;
                violated = true;
            }

            if (!violated) break;
        }

        return target;
    }

    private void OnPointer(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse)
        {
            Hover(null);
            return;
        }

        var at = e.GetPosition(this);
        Hover(Tabs.FirstOrDefault(t => t.Bounds.Contains(at)));
    }

    private void OnExited(object? sender, PointerEventArgs e)
    {
        if (e.Source == this || !this.IsPointerOver) Hover(null);
    }

    private void Hover(TabButton? tab)
    {
        if (tab == _hovered) return;
        if (_hovered is not null) _hovered.IsHovered = false;
        _hovered = tab;
        if (tab is not null) tab.IsHovered = true;
    }

    /// <summary>A tab taken out of the line loses its hover.</summary>
    protected override void ChildrenChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        base.ChildrenChanged(sender, e);
        if (_hovered is not null && !Children.Contains(_hovered))
        {
            _hovered.IsHovered = false;
            _hovered = null;
        }
    }
}
