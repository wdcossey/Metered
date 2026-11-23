using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Threading;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>NestedMenus</c> (<c>menu/nested-menus.js</c>): round a menu whose options have child menus, it keeps the
/// menus that are shown, in the order they were shown (the menu, then each child as it opens), so that ArrowRight focuses
/// the next one and ArrowLeft the one before, and follows a mouse over them: a menu deeper than the one the mouse is over
/// lets its highlight go, so its own child closes; 200 ms after the mouse has left them all, every one does.
/// </summary>
/// <remarks>A menu finds it through its logical parents, the child menus in their popovers too.</remarks>
public class NestedMenus : Decorator
{
    /// <summary>nested-menus.js's mouse leave timeout.</summary>
    internal static readonly TimeSpan LeaveDelay = TimeSpan.FromMilliseconds(200);

    private readonly List<Menu> _menus = [];
    private int _hoverIndex = -1;
    private DispatcherTimer? _leave;

    /// <summary>The menus shown, in order.</summary>
    internal IReadOnlyList<Menu> Menus => _menus;

    /// <summary>The index of the menu a mouse is over, -1 for none.</summary>
    internal int HoverIndex => _hoverIndex;

    /// <summary>The nested menus a menu is in, if any.</summary>
    internal static NestedMenus? Of(Menu menu) => menu.GetLogicalAncestors().OfType<NestedMenus>().FirstOrDefault();

    /// <summary>addMenuToNesting: a menu shown.</summary>
    internal void Add(Menu menu)
    {
        if (_menus.Contains(menu)) return;
        _menus.Add(menu);
        menu.PointerEntered += OnMenuEntered;
        menu.PointerExited += OnMenuExited;
    }

    /// <summary>removeMenuFromNesting: a menu gone; with the last, the leave timer.</summary>
    internal void Remove(Menu menu)
    {
        if (!_menus.Remove(menu)) return;
        menu.PointerEntered -= OnMenuEntered;
        menu.PointerExited -= OnMenuExited;
        if (_menus.Count == 0) _leave?.Stop();
    }

    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _leave?.Stop();
    }

    /// <summary>getParentMenu: the menu shown before it.</summary>
    internal Menu? ParentOf(Menu menu) => _menus.IndexOf(menu) is var i and > 0 ? _menus[i - 1] : null;

    /// <summary>getChildMenu: the menu shown after it.</summary>
    internal Menu? ChildOf(Menu menu) => _menus.IndexOf(menu) is var i and >= 0 && i + 1 < _menus.Count ? _menus[i + 1] : null;

    /// <summary>handleMenuMouseEnter: the menu the mouse is over, the leave timer stopped.</summary>
    private void OnMenuEntered(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse || sender is not Menu menu) return;
        _leave?.Stop();
        Hover(_menus.IndexOf(menu));
    }

    /// <summary>handleMenuMouseLeave: none, 200 ms later.</summary>
    private void OnMenuExited(object? sender, PointerEventArgs e)
    {
        if (e.Pointer.Type != PointerType.Mouse) return;
        _leave ??= new DispatcherTimer(LeaveDelay, DispatcherPriority.Background, (_, _) =>
        {
            _leave!.Stop();
            Hover(-1);
        });
        _leave.Stop();
        _leave.Start();
    }

    private void Hover(int index)
    {
        if (index == _hoverIndex) return;
        _hoverIndex = index;
        // componentDidUpdate in each menu: one deeper than the hovered lets its highlight go.
        foreach (var (menu, i) in _menus.ToList().Select((m, i) => (m, i))) menu.NestedHoverChanged(index, i);
    }
}
