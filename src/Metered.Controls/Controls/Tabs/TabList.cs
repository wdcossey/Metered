using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input.GestureRecognizers;
using Avalonia.Layout;

namespace Metered.Controls;

/// <summary>
/// The element of a &lt;see cref="Tabs"/&gt;' &lt;c&gt;TabList&lt;/c&gt; part, Base Web's &lt;c&gt;&amp;lt;div role="tablist"&amp;gt;&lt;/c&gt;
/// (&lt;c&gt;StyledTabList&lt;/c&gt;): the scroller of the tabs' line (&lt;c&gt;overflow-x: scroll&lt;/c&gt;, or &lt;c&gt;overflow-y&lt;/c&gt; vertically,
/// with no bar drawn, while the fill is intrinsic: its scroll bars' visibility), its content a &lt;see cref="TabListPanel"/&gt;
/// laid out along &lt;see cref="Orientation"/&gt; (&lt;c&gt;flex-direction&lt;/c&gt;) inside its &lt;see cref="TemplatedControl.Padding"/&gt;,
/// which scrolls with the tabs as an overflow box's padding does.
/// &lt;/summary&gt;
/// &lt;remarks&gt;
/// Its template is its own, by default (a theme's &lt;c&gt;Template&lt;/c&gt; replaces it): a scroller whose content the line is.
/// Touch first: a finger's drag more sideways than up or down scrolls a horizontal list, which has room to move that way,
/// and the rest of a drag mostly up or down is the page's (the input's recogniser, #117); a vertical list's drag is
/// Avalonia's own. The wheel's vertical turn over a horizontal list goes on to the page, as a browser's does.
/// &lt;/remarks&gt;
public class TabList : ScrollViewer
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<TabList>();

    static TabList()
    {
        ClipToBoundsProperty.OverrideDefaultValue<TabList>(true);
        HorizontalScrollBarVisibilityProperty.OverrideDefaultValue<TabList>(Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden);
        VerticalScrollBarVisibilityProperty.OverrideDefaultValue<TabList>(Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        TemplateProperty.OverrideDefaultValue<TabList>(new FuncControlTemplate<TabList>((list, scope) =>
        {
            var presenter = new ScrollContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = list[!ContentProperty],
                [!ScrollViewer.IsScrollInertiaEnabledProperty] = list[!IsScrollInertiaEnabledProperty],
            };
            presenter.GestureRecognizers.Add(new InputScrollGestureRecognizer
            {
                CanVerticallyScroll = false,
                [!ScrollGestureRecognizer.CanHorizontallyScrollProperty] = presenter[!ScrollContentPresenter.CanHorizontallyScrollProperty],
                [!ScrollGestureRecognizer.IsScrollInertiaEnabledProperty] = presenter[!ScrollViewer.IsScrollInertiaEnabledProperty],
                [!ScrollGestureRecognizer.OffsetProperty] = presenter[!ScrollContentPresenter.OffsetProperty],
                [!ScrollGestureRecognizer.ViewportProperty] = presenter[!ScrollContentPresenter.ViewportProperty],
                [!ScrollGestureRecognizer.ExtentProperty] = presenter[!ScrollContentPresenter.ExtentProperty],
            });
            presenter.GestureRecognizers.Add(new ScrollGestureRecognizer
            {
                CanHorizontallyScroll = false,
                [!ScrollGestureRecognizer.CanVerticallyScrollProperty] = presenter[!ScrollContentPresenter.CanVerticallyScrollProperty],
                [!ScrollGestureRecognizer.IsScrollInertiaEnabledProperty] = presenter[!ScrollViewer.IsScrollInertiaEnabledProperty],
                [!ScrollGestureRecognizer.OffsetProperty] = presenter[!ScrollContentPresenter.OffsetProperty],
                [!ScrollGestureRecognizer.ViewportProperty] = presenter[!ScrollContentPresenter.ViewportProperty],
                [!ScrollGestureRecognizer.ExtentProperty] = presenter[!ScrollContentPresenter.ExtentProperty],
            });
            scope.Register(presenter.Name, presenter);
            return presenter;
        }));
    }

    /// <summary>The line's direction: horizontal (a row) or vertical (a column).</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>The width it was given to lay out in, the room its line flexes in (its scroller gives the line none).</summary>
    internal double? Room { get; private set; }

    /// <summary>The template's scroller.</summary>
    internal ScrollContentPresenter? ScrollPresenter { get; private set; }

    protected override Type StyleKeyOverride => typeof(TabList);

    protected override void OnApplyTemplate(Avalonia.Controls.Primitives.TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        ScrollPresenter = e.NameScope.Find<ScrollContentPresenter>("PART_ContentPresenter");
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ContentProperty)
        {
            if (change.OldValue is TabListPanel old && old.Owner == this) old.Owner = null;
            if (change.NewValue is TabListPanel line) line.Owner = this;
        }

        if (change.Property == OrientationProperty || change.Property == PaddingProperty) (Content as Layoutable)?.InvalidateMeasure();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var room = double.IsInfinity(availableSize.Width) ? (double?)null : availableSize.Width;
        if (room != Room)
        {
            Room = room;
            (Content as Layoutable)?.InvalidateMeasure();
        }

        return base.MeasureOverride(availableSize);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TabListAutomationPeer(this);
}

/// <summary>A tab list to UI Automation: a tab control (&lt;c&gt;role="tablist"&lt;/c&gt;), its children the tabs.</summary>
public class TabListAutomationPeer(TabList owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;
}
