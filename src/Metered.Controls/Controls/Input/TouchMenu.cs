using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Primitives.PopupPositioning;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// The cut, copy and paste menu's place on a coarse pointer (a phone, #110): above the selection, clear of it, as a phone's
/// browser places the platform's floating text toolbar. Base Web leaves the menu to the browser; Chrome on Android shows
/// Android's, so this is Android's placement (<c>aosp-mirror/platform_frameworks_base</c> at <c>1cdfff555f</c>), with
/// Chrome's content rect.
/// </summary>
/// <remarks>
/// <para>The content rect is Chrome's (<c>SelectionPopupControllerImpl</c>, chromium <c>main</c>): the selection's bounds,
/// their bottom extended by the touch handles' height so the menu below them doesn't cover them; a caret's line where
/// nothing is selected. Here only the part of the selection the text's box shows, as Blink clips an input's selection;
/// then kept inside the page as <c>FloatingActionMode.repositionToolbar</c> keeps it inside its view, its bottom allowed
/// <see cref="BottomClipAllowance"/> past it.</para>
/// <para>The place is <c>LocalFloatingToolbarPopup.refreshCoordinatesAndOverflowDirection</c>'s for a toolbar with no
/// overflow (<see cref="Place"/>), in the viewport Android gives it (<c>getWindowVisibleDisplayFrame</c>): the top level's
/// safe area above the soft keyboard. Avalonia's own place for a touch's menu (<c>TextSelectionHandleCanvas.ShowFlyout</c>)
/// hangs it over the selection (docs/avalonia-issues.md, 2).</para>
/// </remarks>
internal static class TouchMenu
{
    /// <summary>Android's <c>floating_toolbar_vertical_margin</c> (<c>core/res/res/values/dimens.xml</c>): 8 dp, above and below the menu.</summary>
    internal const double VerticalMargin = 8;

    /// <summary>Android's <c>floating_toolbar_horizontal_margin</c>: 16 dp, either side of the menu.</summary>
    internal const double HorizontalMargin = 16;

    /// <summary>Android's <c>content_rect_bottom_clip_allowance</c>: 20 dp the content rect may reach below its view.</summary>
    internal const double BottomClipAllowance = 20;

    private static readonly ConditionalWeakTable<TopLevel, Func<Rect?>> Keyboards = [];

    /// <summary>
    /// The soft keyboard read in place of a top level's input pane, for a test: Avalonia.Headless has none, and Avalonia's
    /// <see cref="IInputPane"/> can't be implemented outside it. The function gives the keyboard's rect in the top level's
    /// coordinates while it is open, or null; null gives the platform's back.
    /// </summary>
    internal static void FakeKeyboard(TopLevel top, Func<Rect?>? keyboard)
    {
        if (keyboard is null) Keyboards.Remove(top);
        else Keyboards.AddOrUpdate(top, keyboard);
    }

    /// <summary>
    /// Opens <paramref name="menu"/> for <paramref name="input"/>, placed above its selection. False when it was already
    /// open for it (Avalonia's flyout leaves it so) or didn't open.
    /// </summary>
    internal static bool Show(BaseInput input, PopupFlyoutBase menu, TextPresenter? presenter, ScrollViewer? scroller)
    {
        if (menu.IsOpen && menu.Target == input) return false;
        // Above the values Avalonia's touch path sets for its own place while it asks for the menu, and given back after:
        // the popup keeps the callback, which places it again as its size changes.
        using (menu.SetValue(PopupFlyoutBase.PlacementProperty, PlacementMode.Custom, BindingPriority.Animation))
        using (menu.SetValue(PopupFlyoutBase.CustomPopupPlacementCallbackProperty,
                   (CustomPopupPlacementCallback)(p => Position(p, input, presenter, scroller)), BindingPriority.Animation))
        {
            menu.ShowAt(input);
        }

        return menu.IsOpen && menu.Target == input;
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

    private static void Position(CustomPopupPlacement placement, BaseInput input, TextPresenter? presenter, ScrollViewer? scroller)
    {
        if (TopLevel.GetTopLevel(input) is not { } top || input.TranslatePoint(default, top) is not { } origin
            || presenter is null || Content(input, presenter, scroller, top) is not { } content)
        {
            // Nothing to place it by: above the input.
            placement.Anchor = PopupAnchor.Top;
            placement.Gravity = PopupGravity.Top;
            return;
        }

        var at = Place(placement.PopupSize.Deflate(placement.Deflate), content, Viewport(top));
        // The positioner's coordinates are its root's, where the anchor rectangle it gives is the input's box; the menu's
        // own box (its size less what it deflates by) goes at the anchor's corner.
        var shift = placement.AnchorRectangle.TopLeft - origin;
        placement.AnchorRectangle = new Rect(at + shift, default(Size));
        placement.Anchor = PopupAnchor.TopLeft;
        placement.Gravity = PopupGravity.BottomRight;
        placement.ConstraintAdjustment = PopupPositionerConstraintAdjustment.None;
        placement.Offset = default;
    }

    /// <summary>Chrome's content rect, in the top level's coordinates.</summary>
    private static Rect? Content(BaseInput input, TextPresenter presenter, ScrollViewer? scroller, TopLevel top)
    {
        if (presenter.TransformToVisual(top) is not { } toTop) return null;
        var layout = presenter.TextLayout;
        var start = Math.Min(input.SelectionStart, input.SelectionEnd);
        var end = Math.Max(input.SelectionStart, input.SelectionEnd);
        Rect? box = null;
        if (start != end)
        {
            foreach (var line in layout.HitTestTextRange(start, end - start)) box = box?.Union(line) ?? line;
        }

        if (box is null)
        {
            var caret = layout.HitTestTextPosition(input.CaretIndex);
            box = new Rect(caret.X, caret.Y, 0, caret.Height);
        }

        var content = box.Value.TransformToAABB(toTop);
        // The part the text's box shows.
        if (scroller?.TransformToVisual(top) is { } scrollerToTop)
        {
            var shown = new Rect(scroller.Bounds.Size).TransformToAABB(scrollerToTop);
            if (IntersectsClosed(content, shown)) content = Clamp(content, shown, shown.Bottom);
        }

        // Clear of the handles below it.
        content = content.WithHeight(content.Height + HandleHeight(input));
        // Inside the page, its bottom allowed a little past it.
        var page = new Rect(top.ClientSize);
        if (IntersectsClosed(content, page)) content = Clamp(content, page, page.Bottom + BottomClipAllowance);
        return content;
    }

    /// <summary>The safe area, above the soft keyboard while it is open: Android's visible display frame.</summary>
    private static Rect Viewport(TopLevel top)
    {
        var area = SafeArea.Area(top.ClientSize, SafeArea.Of(top)?.Padding ?? default);
        var keyboard = Keyboards.TryGetValue(top, out var fake)
            ? fake()
            : top.InputPane is { State: InputPaneState.Open } pane ? pane.OccludedRect : null;
        if (keyboard is { Height: > 0 } k && k.Top < area.Bottom)
            area = area.WithHeight(Math.Max(0, k.Top - area.Top));
        return area;
    }

    /// <summary>
    /// The touch handles' height, as Chrome adds it below the selection: the tallest shown in the top level's text selector
    /// layer, where only the focused text box's are shown. None shown, none added.
    /// </summary>
    private static double HandleHeight(Visual input)
    {
        if (TextSelectorLayer.GetTextSelectorLayer(input) is not { } layer) return 0;
        var height = 0.0;
        foreach (var handle in layer.GetVisualDescendants().OfType<TextSelectionHandle>())
        {
            if (handle.IsEffectivelyVisible) height = Math.Max(height, handle.Bounds.Height);
        }

        return height;
    }

    // Android's Rect.intersects, with the edges that touch: FloatingActionMode.intersectsClosed.
    private static bool IntersectsClosed(Rect a, Rect b) =>
        a.Left <= b.Right && b.Left <= a.Right && a.Top <= b.Bottom && b.Top <= a.Bottom;

    // Each edge kept inside, as FloatingActionMode sets the content rect from its view's.
    private static Rect Clamp(Rect content, Rect inside, double bottom)
    {
        var left = Math.Max(content.Left, inside.Left);
        var topEdge = Math.Max(content.Top, inside.Top);
        var right = Math.Min(content.Right, inside.Right);
        var bottomEdge = Math.Min(content.Bottom, bottom);
        return new Rect(left, topEdge, Math.Max(0, right - left), Math.Max(0, bottomEdge - topEdge));
    }
}
