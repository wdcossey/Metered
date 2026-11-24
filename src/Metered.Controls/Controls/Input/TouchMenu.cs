using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// The cut, copy and paste menu on a coarse pointer (a phone, #110): without the shortcuts the theme's menu shows on a
/// desktop, and above the selection, clear of it, as a phone's browser places the platform's floating text toolbar. Base
/// Web leaves the menu to the browser; Chrome on Android shows Android's, so this is Android's placement
/// (<c>aosp-mirror/platform_frameworks_base</c> at <c>1cdfff555f</c>), with Chrome's content rect.
/// </summary>
/// <remarks>
/// <para>The content rect is Chrome's (<c>SelectionPopupControllerImpl</c>, chromium <c>main</c>): the selection's bounds,
/// their bottom extended by the touch handles' height so the menu below them doesn't cover them; a caret's line where
/// nothing is selected. Here only the part of the selection the text's box shows, as Blink clips an input's selection, and
/// <c>FloatingActionMode.repositionToolbar</c> keeps the rect inside its view; a selection the box doesn't show at all
/// gives the box itself.</para>
/// <para>The place is <c>LocalFloatingToolbarPopup.refreshCoordinatesAndOverflowDirection</c>'s for a toolbar with no
/// overflow (<see cref="Place"/>), in the viewport Android gives it (<c>getWindowVisibleDisplayFrame</c>): the top level's
/// safe area above the soft keyboard. It is placed again while it is open as the input moves (a page's scroll, the keyboard
/// making room) and as the keyboard or the safe area changes, as <c>FloatingActionMode</c> moves the toolbar with its
/// content. Avalonia's own place for a touch's menu (<c>TextSelectionHandleCanvas.ShowFlyout</c>) hangs it over the
/// selection (docs/avalonia-issues.md, 2).</para>
/// </remarks>
internal static class TouchMenu
{
    /// <summary>Android's <c>floating_toolbar_vertical_margin</c> (<c>core/res/res/values/dimens.xml</c>): 8 dp, above and below the menu.</summary>
    internal const double VerticalMargin = 8;

    /// <summary>Android's <c>floating_toolbar_horizontal_margin</c>: 16 dp, either side of the menu.</summary>
    internal const double HorizontalMargin = 16;

    /// <summary>The theme's cut, copy and paste menu (Input.axaml), the one whose shortcuts a phone's has not.</summary>
    internal const string ThemeMenuKey = "Input.ContextFlyout";

    private static readonly ConditionalWeakTable<TopLevel, IKeyboard> Keyboards = [];

    /// <summary>What the menu reads of the soft keyboard: where it covers the top level while it is open, and when that changes.</summary>
    internal interface IKeyboard
    {
        /// <summary>The keyboard's rect in the top level's coordinates while it is open; null while it is closed.</summary>
        Rect? Occluded { get; }

        /// <summary>It has opened, closed or changed its height.</summary>
        event EventHandler? Changed;
    }

    /// <summary>
    /// The keyboard read in place of a top level's input pane, for a test: Avalonia.Headless has none, and Avalonia's
    /// <see cref="IInputPane"/> can't be implemented outside it. Null gives the platform's back.
    /// </summary>
    internal static void FakeKeyboard(TopLevel top, IKeyboard? keyboard)
    {
        if (keyboard is null) Keyboards.Remove(top);
        else Keyboards.AddOrUpdate(top, keyboard);
    }

    /// <summary>
    /// Opens <paramref name="menu"/> for <paramref name="input"/> above its selection, without the theme's shortcuts, as an
    /// Avalonia flyout is shown on a phone (without taking the focus, so the keyboard stays). False only when it was
    /// already open for it, which Avalonia's flyout handler leaves as it is too; a menu whose opening was cancelled stays
    /// shut.
    /// </summary>
    internal static bool Show(BaseInput input, PopupFlyoutBase menu)
    {
        if (menu.IsOpen && menu.Target == input) return false;
        var shown = new Shown(input, menu);
        // Above the values Avalonia's touch path sets for its own place while it asks for the menu, and given back after:
        // the popup keeps the callback, which places it again as its size changes.
        using (menu.SetValue(PopupFlyoutBase.PlacementProperty, PlacementMode.Custom, BindingPriority.Animation))
        using (menu.SetValue(PopupFlyoutBase.CustomPopupPlacementCallbackProperty,
                   (CustomPopupPlacementCallback)shown.Position, BindingPriority.Animation))
        using (menu.SetValue(PopupFlyoutBase.ShowModeProperty, FlyoutShowMode.Transient, BindingPriority.Animation))
        {
            menu.ShowAt(input);
        }

        if (menu.IsOpen && menu.Target == input) shown.Follow();
        else shown.Release();
        return true;
    }

    /// <summary>
    /// The menu's top left in the viewport, for a menu of <paramref name="menu"/>'s size and a selection whose content rect
    /// is <paramref name="content"/>: <c>refreshCoordinatesAndOverflowDirection</c>'s with no overflow, its popup window
    /// the menu with <see cref="HorizontalMargin"/> either side and <see cref="VerticalMargin"/> above and below.
    /// </summary>
    internal static Point Place(Size menu, Rect content, Rect viewport)
    {
        var width = menu.Width + 2 * HorizontalMargin;
        var height = menu.Height + 2 * VerticalMargin;
        // Not enough room: as far left as it goes. Else centred on the content, kept inside.
        var x = width > viewport.Width
            ? viewport.Left
            : Math.Clamp(content.Center.X - width / 2, viewport.Left, viewport.Right - width);
        var above = content.Top - viewport.Top;
        var below = viewport.Bottom - content.Bottom;
        double y;
        if (above >= height) y = content.Top - height;
        else if (below >= height) y = content.Bottom;
        // Just room for the menu with no margins.
        else if (below >= menu.Height) y = content.Bottom - VerticalMargin;
        // No room: as high as it goes.
        else y = Math.Max(viewport.Top, content.Top - height);
        return new Point(x + HorizontalMargin, y + VerticalMargin);
    }

    /// <summary>
    /// The theme's own menu shows no shortcuts while it is open for a phone's input, as a phone's browser's toolbar has
    /// none; given back when it closes. An app's menu is its own.
    /// </summary>
    private static IDisposable? HideShortcuts(BaseInput input, PopupFlyoutBase menu)
    {
        if (menu is not MenuFlyout flyout || !input.TryFindResource(ThemeMenuKey, out var theme) || theme != menu)
            return null;
        var held = new List<IDisposable>();
        foreach (var item in flyout.Items.OfType<MenuItem>())
        {
            if (item.SetValue(MenuItem.InputGestureProperty, null, BindingPriority.Animation) is { } value) held.Add(value);
        }

        return new Release(held);
    }

    private static IKeyboard? KeyboardOf(TopLevel top) =>
        Keyboards.TryGetValue(top, out var fake) ? fake : top.InputPane is { } pane ? new Platform(pane) : null;

    // Android's Rect.intersects, with the edges that touch: FloatingActionMode.intersectsClosed.
    private static bool IntersectsClosed(Rect a, Rect b) =>
        a.Left <= b.Right && b.Left <= a.Right && a.Top <= b.Bottom && b.Top <= a.Bottom;

    // Each edge kept inside, as FloatingActionMode sets the content rect from its view's.
    private static Rect Clamp(Rect content, Rect inside)
    {
        var left = Math.Max(content.Left, inside.Left);
        var topEdge = Math.Max(content.Top, inside.Top);
        var right = Math.Min(content.Right, inside.Right);
        var bottomEdge = Math.Min(content.Bottom, inside.Bottom);
        return new Rect(left, topEdge, Math.Max(0, right - left), Math.Max(0, bottomEdge - topEdge));
    }

    /// <summary>
    /// One opening of the menu for an input: what it is placed by, read from the input's current parts, and the sources it
    /// follows while it is open, kept so that a change they report is the one it is placed by.
    /// </summary>
    private sealed class Shown
    {
        private readonly BaseInput _input;
        private readonly PopupFlyoutBase _menu;
        private readonly TopLevel? _top;
        private readonly IKeyboard? _keyboard;
        private readonly SafeArea.ISource? _safe;
        private readonly TextSelectorLayer? _layer;
        private IDisposable? _shortcuts;
        private double _handles;

        public Shown(BaseInput input, PopupFlyoutBase menu)
        {
            _input = input;
            _menu = menu;
            _top = TopLevel.GetTopLevel(input);
            _keyboard = _top is null ? null : KeyboardOf(_top);
            _safe = _top is null ? null : SafeArea.Of(_top);
            _layer = TextSelectorLayer.GetTextSelectorLayer(input);
            _shortcuts = HideShortcuts(input, menu);
        }

        /// <summary>
        /// Placed again while it is open as what it is placed by changes: the input's place on the window (a page's scroll,
        /// the keyboard making room), its selection or caret, the handles' height once they are laid out, the keyboard and
        /// the bars. Let go when it closes.
        /// </summary>
        public void Follow()
        {
            _input.EffectiveViewportChanged += OnMoved;
            _input.PropertyChanged += OnInputChanged;
            if (_layer is not null) _layer.LayoutUpdated += OnHandlesLaidOut;
            if (_keyboard is not null) _keyboard.Changed += OnChanged;
            if (_safe is not null) _safe.Changed += OnChanged;
            _menu.Closed += OnClosed;
        }

        public void Release()
        {
            _shortcuts?.Dispose();
            _shortcuts = null;
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            _menu.Closed -= OnClosed;
            _input.EffectiveViewportChanged -= OnMoved;
            _input.PropertyChanged -= OnInputChanged;
            if (_layer is not null) _layer.LayoutUpdated -= OnHandlesLaidOut;
            if (_keyboard is not null) _keyboard.Changed -= OnChanged;
            if (_safe is not null) _safe.Changed -= OnChanged;
            Release();
        }

        private void OnMoved(object? sender, EffectiveViewportChangedEventArgs e) => PlaceAgain();

        private void OnChanged(object? sender, EventArgs e) => PlaceAgain();

        private void OnInputChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty
                || e.Property == TextBox.CaretIndexProperty)
                PlaceAgain();
        }

        // Only when their height has changed: a layout pass of its own is no reason.
        private void OnHandlesLaidOut(object? sender, EventArgs e)
        {
            if (HandleHeight() != _handles) PlaceAgain();
        }

        /// <summary>
        /// The popup places itself again (through the callback it keeps) when one of its offsets changes: the place sets the
        /// offset itself, so the one it is given is no matter.
        /// </summary>
        private void PlaceAgain()
        {
            if (!_menu.IsOpen || _menu.Target != _input) return;
            var popup = _menu.Popup;
            var offset = popup.HorizontalOffset;
            popup.HorizontalOffset = offset + 1;
            popup.HorizontalOffset = offset;
        }

        public void Position(CustomPopupPlacement placement)
        {
            if (_top is null || Content() is not { } content)
            {
                // Nothing to place it by: above the input.
                placement.Anchor = PopupAnchor.Top;
                placement.Gravity = PopupGravity.Top;
                return;
            }

            // The positioner's coordinates are its root's, and the anchor rectangle it gives is the popup's placement rect
            // (the input's box unless an app set one) within the input's box there: the same corner in the top level's
            // gives the shift from one to the other.
            var box = new Rect(_input.Bounds.Size);
            var corner = (_menu.Popup.PlacementRect ?? box).Intersect(box).TopLeft;
            if (_input.TranslatePoint(corner, _top) is not { } cornerOnTop) return;
            var at = Place(placement.PopupSize.Deflate(placement.Deflate), content, Viewport());
            // The menu's own box (its size less what it deflates by) goes at the anchor's corner.
            placement.AnchorRectangle = new Rect(at + (placement.AnchorRectangle.TopLeft - cornerOnTop), default(Size));
            placement.Anchor = PopupAnchor.TopLeft;
            placement.Gravity = PopupGravity.BottomRight;
            placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
            placement.Offset = default;
        }

        /// <summary>
        /// Chrome's content rect, in the top level's coordinates. The selection's indices are the text presenter's as it
        /// draws the selection (<c>TextPresenter.Render</c>), so the menu goes where the selection is drawn.
        /// </summary>
        private Rect? Content()
        {
            if (_input.TextPresenterPart is not { } presenter || presenter.TransformToVisual(_top!) is not { } toTop) return null;
            var layout = presenter.TextLayout;
            var start = Math.Min(_input.SelectionStart, _input.SelectionEnd);
            var end = Math.Max(_input.SelectionStart, _input.SelectionEnd);
            Rect? box = null;
            if (start != end)
            {
                foreach (var line in layout.HitTestTextRange(start, end - start)) box = box?.Union(line) ?? line;
            }

            if (box is null)
            {
                var caret = layout.HitTestTextPosition(_input.CaretIndex);
                box = new Rect(caret.X, caret.Y, 0, caret.Height);
            }

            var content = box.Value.TransformToAABB(toTop);
            // The part the text's box shows; none of it, the box.
            if (_input.TextScrollerPart?.TransformToVisual(_top!) is { } scrollerToTop)
            {
                var shown = new Rect(_input.TextScrollerPart.Bounds.Size).TransformToAABB(scrollerToTop);
                content = IntersectsClosed(content, shown) ? Clamp(content, shown) : shown;
            }

            // Clear of the handles below it.
            _handles = HandleHeight();
            return content.WithHeight(content.Height + _handles);
        }

        /// <summary>The safe area, above the soft keyboard while it is open: Android's visible display frame.</summary>
        private Rect Viewport()
        {
            var area = SafeArea.Area(_top!.ClientSize, _safe?.Padding ?? default);
            if (_keyboard?.Occluded is { Height: > 0 } keyboard && keyboard.Top < area.Bottom)
                area = area.WithHeight(Math.Max(0, keyboard.Top - area.Top));
            return area;
        }

        /// <summary>
        /// The touch handles' height, as Chrome adds it below the selection: the tallest shown in the top level's text
        /// selector layer, where only the focused text box's are shown. None shown, none added.
        /// </summary>
        private double HandleHeight()
        {
            if (_layer is null) return 0;
            var height = 0.0;
            foreach (var handle in _layer.GetVisualDescendants().OfType<TextSelectionHandle>())
            {
                if (handle.IsEffectivelyVisible) height = Math.Max(height, handle.Bounds.Height);
            }

            return height;
        }
    }

    private sealed class Release(List<IDisposable> held) : IDisposable
    {
        public void Dispose()
        {
            foreach (var value in held) value.Dispose();
            held.Clear();
        }
    }

    /// <summary>
    /// The platform's input pane, as <see cref="IKeyboard"/>: its rect as its last change gave it (Android raises the change
    /// as the keyboard starts to move, with where it ends), or as it reads before any.
    /// </summary>
    private sealed class Platform(IInputPane pane) : IKeyboard
    {
        private EventHandler? _changed;
        private (bool Open, Rect Rect)? _said;

        public Rect? Occluded => _said is { } said
            ? said.Open ? said.Rect : null
            : pane.State == InputPaneState.Open ? pane.OccludedRect : null;

        public event EventHandler? Changed
        {
            add
            {
                if (_changed is null) pane.StateChanged += OnStateChanged;
                _changed += value;
            }
            remove
            {
                _changed -= value;
                if (_changed is null) pane.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(object? sender, InputPaneStateEventArgs e)
        {
            _said = (e.NewState == InputPaneState.Open, e.EndRect);
            _changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
