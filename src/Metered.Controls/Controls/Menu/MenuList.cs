using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Layout;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a menu's <c>List</c> part, Base Web's <c>&lt;ul role="listbox" tabIndex="0"&gt;</c>
/// (<c>StyledList</c>): the focusable box the options are in, scrolled (CSS's <c>overflow: auto</c>) by the wheel, by a
/// finger's drag and by the menu's keys, with its fill, radius and shadow (<see cref="Background"/>,
/// <see cref="TemplatedControl.CornerRadius"/>, <see cref="BoxShadow"/>), its padding inside the scroller, as an overflow box's padding
/// scrolls with its content (<see cref="TemplatedControl.Padding"/>), and CSS's <c>outline</c>
/// (<see cref="OutlineBrush"/>, <see cref="OutlineThickness"/>), its focus ring on keyboard focus.
/// </summary>
/// <remarks>
/// Its template is its own, by default (a theme's <c>Template</c> replaces it): the shadow's box round the fill's, which
/// clips the options to its round corners, the scroller in it, and the outline over them. It draws no scroll bars, as
/// Metered's other scrollers. The keys are the menu's: it gives none to the scroller but Page Up and Page Down, which a
/// browser's own scrolls a focused box by, as Base Web lets them through (docs/controls/Menu).
/// </remarks>
public class MenuList : ScrollViewer
{
    public static readonly StyledProperty<BoxShadows> BoxShadowProperty =
        Border.BoxShadowProperty.AddOwner<MenuList>();

    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        RadioMark.OutlineBrushProperty.AddOwner<MenuList>();

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        RadioMark.OutlineThicknessProperty.AddOwner<MenuList>();

    static MenuList()
    {
        FocusableProperty.OverrideDefaultValue<MenuList>(true);
        IsTabStopProperty.OverrideDefaultValue<MenuList>(true);
        ClipToBoundsProperty.OverrideDefaultValue<MenuList>(false);
        HorizontalScrollBarVisibilityProperty.OverrideDefaultValue<MenuList>(Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        VerticalScrollBarVisibilityProperty.OverrideDefaultValue<MenuList>(Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden);
        TemplateProperty.OverrideDefaultValue<MenuList>(new FuncControlTemplate<MenuList>((list, scope) =>
        {
            var presenter = new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = list[!ContentProperty],
                [!ContentPresenter.PaddingProperty] = list[!PaddingProperty],
                [!ScrollViewer.IsScrollInertiaEnabledProperty] = list[!IsScrollInertiaEnabledProperty],
            };
            // A finger's drag scrolls it, as Avalonia's own scroller's template has it.
            presenter.GestureRecognizers.Add(new ScrollGestureRecognizer
            {
                [!ScrollGestureRecognizer.CanHorizontallyScrollProperty] = presenter[!ScrollContentPresenter.CanHorizontallyScrollProperty],
                [!ScrollGestureRecognizer.CanVerticallyScrollProperty] = presenter[!ScrollContentPresenter.CanVerticallyScrollProperty],
                [!ScrollGestureRecognizer.IsScrollInertiaEnabledProperty] = presenter[!ScrollViewer.IsScrollInertiaEnabledProperty],
                [!ScrollGestureRecognizer.OffsetProperty] = presenter[!ScrollContentPresenter.OffsetProperty],
                [!ScrollGestureRecognizer.ViewportProperty] = presenter[!ScrollContentPresenter.ViewportProperty],
                [!ScrollGestureRecognizer.ExtentProperty] = presenter[!ScrollContentPresenter.ExtentProperty],
            });
            scope.Register(presenter.Name, presenter);
            var fill = new Border
            {
                Name = "PART_Fill",
                ClipToBounds = true,
                [!Border.BackgroundProperty] = list[!BackgroundProperty],
                [!Border.CornerRadiusProperty] = list[!CornerRadiusProperty],
                Child = presenter,
            };
            var shadow = new Border
            {
                Name = "PART_Shadow",
                [!Border.BoxShadowProperty] = list[!BoxShadowProperty],
                [!Border.CornerRadiusProperty] = list[!CornerRadiusProperty],
                Child = fill,
            };
            var outline = new RadioMark
            {
                Name = "PART_Outline",
                IsHitTestVisible = false,
                [!RadioMark.CornerRadiusProperty] = list[!CornerRadiusProperty],
                [!RadioMark.OutlineBrushProperty] = list[!OutlineBrushProperty],
                [!RadioMark.OutlineThicknessProperty] = list[!OutlineThicknessProperty],
            };
            return new Panel { Children = { shadow, outline } };
        }));
    }

    /// <summary>The shadow round the box; none by default.</summary>
    public BoxShadows BoxShadow { get => GetValue(BoxShadowProperty); set => SetValue(BoxShadowProperty, value); }

    /// <summary>The outline's colour; none when null.</summary>
    public IBrush? OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }

    /// <summary>The outline's width, outside the box; none when 0.</summary>
    public double OutlineThickness { get => GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }

    /// <summary>The menu whose list this is, which takes its keys.</summary>
    internal Menu? Owner { get; set; }

    /// <summary>The template's scroller, whose content the options are.</summary>
    internal ScrollContentPresenter? ContentPart { get; private set; }

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        ContentPart = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter");
    }

    protected override Type StyleKeyOverride => typeof(MenuList);

    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new MenuListAutomationPeer(this);

    /// <summary>
    /// The menu's keys first (menu.js's onKeyDown on the list). A browser scrolls a focused box by Page Up, Page Down and
    /// Space (Shift and Space up), which Base Web doesn't prevent: those scroll it a page after the menu has had them
    /// (Space is a letter of type-ahead's too). Its arrows, Home and End are the menu's, never the scroller's, whether the
    /// menu took them or not (a key before the menu is focused does nothing, as Base Web's).
    /// </summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        Owner?.ListKeyDown(e);
        if (e.Handled) return;
        if (e.Key is Key.PageUp or Key.PageDown) base.OnKeyDown(e);
        else if (e.Key == Key.Space)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift)) PageUp();
            else PageDown();
            e.Handled = true;
        }
    }
}
