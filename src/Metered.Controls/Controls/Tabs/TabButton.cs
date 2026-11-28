using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaButton = Avalonia.Controls.Button;

namespace Metered.Controls;

/// <summary>
/// The element of a tab's &lt;c&gt;Tab&lt;/c&gt; part, Base Web's &lt;c&gt;&amp;lt;button type="button" role="tab"&amp;gt;&lt;/c&gt; (&lt;c&gt;StyledTab&lt;/c&gt;):
/// an Avalonia &lt;see cref="AvaloniaButton"/&gt;, clicked by a mouse, a tap, Enter or Space, that also draws CSS's
/// &lt;c&gt;outline&lt;/c&gt; (&lt;see cref="OutlineBrush"/&gt;, &lt;see cref="OutlineThickness"/&gt;, &lt;see cref="OutlineOffset"/&gt;; Base Web's is
/// inset, a negative offset), is a flex item of its line (&lt;see cref="FlexGrow"/&gt;, &lt;see cref="FlexBasis"/&gt;), and is
/// &lt;c&gt;:is-hovered&lt;/c&gt; while a mouse is over it, CSS's &lt;c&gt;:hover&lt;/c&gt;.
/// &lt;/summary&gt;
/// &lt;remarks&gt;
/// &lt;para&gt;Its template is its own, by default (a theme's &lt;c&gt;Template&lt;/c&gt; replaces it): radio-v2's mark
/// (&lt;see cref="RadioMark"/&gt;) with the button's background, padding and outline, round its content, which the
/// &lt;see cref="Tabs"/&gt; gives it (the artwork's container and the title). Its look is the part's theme, &lt;c&gt;Tabs.StyledTab&lt;/c&gt;.&lt;/para&gt;
/// &lt;para&gt;Touch first: a finger or a pen that moves past the platform's tap size before it lifts doesn't click it, as a
/// browser gives no click for a drag; a drag a scroller takes (the tab list's, sideways; the page's) takes its press
/// as Avalonia's button lets it.&lt;/para&gt;
/// &lt;/remarks&gt;
public class TabButton : AvaloniaButton
{
    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        RadioMark.OutlineBrushProperty.AddOwner<TabButton>();

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        RadioMark.OutlineThicknessProperty.AddOwner<TabButton>();

    public static readonly StyledProperty<double> OutlineOffsetProperty =
        RadioMark.OutlineOffsetProperty.AddOwner<TabButton>();

    public static readonly StyledProperty<double> FlexGrowProperty =
        AvaloniaProperty.Register<TabButton, double>(nameof(FlexGrow));

    public static readonly StyledProperty<double> FlexBasisProperty =
        AvaloniaProperty.Register<TabButton, double>(nameof(FlexBasis), double.NaN);

    private Point? _pressedAt;
    private bool _hovered;

    static TabButton()
    {
        TemplateProperty.OverrideDefaultValue<TabButton>(new FuncControlTemplate<TabButton>((button, scope) =>
        {
            var content = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = button[!ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = button[!ContentTemplateProperty],
                [!HorizontalAlignmentProperty] = button[!HorizontalContentAlignmentProperty],
                [!VerticalAlignmentProperty] = button[!VerticalContentAlignmentProperty],
            };
            scope.Register(content.Name, content);
            return new RadioMark
            {
                Name = "PART_Box",
                [!RadioMark.BackgroundProperty] = button[!BackgroundProperty],
                [!RadioMark.BorderBrushProperty] = button[!BorderBrushProperty],
                [!RadioMark.BorderThicknessProperty] = button[!BorderThicknessProperty],
                [!RadioMark.CornerRadiusProperty] = button[!CornerRadiusProperty],
                [!PaddingProperty] = button[!PaddingProperty],
                [!RadioMark.OutlineBrushProperty] = button[!OutlineBrushProperty],
                [!RadioMark.OutlineThicknessProperty] = button[!OutlineThicknessProperty],
                [!RadioMark.OutlineOffsetProperty] = button[!OutlineOffsetProperty],
                Child = content,
            };
        }));
    }

    /// <summary>The outline's colour; none when null.</summary>
    public IBrush? OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }

    /// <summary>The outline's width; none when 0.</summary>
    public double OutlineThickness { get => GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }

    /// <summary>The gap between the tab and its outline; a negative gap draws it inside the tab.</summary>
    public double OutlineOffset { get => GetValue(OutlineOffsetProperty); set => SetValue(OutlineOffsetProperty, value); }

    /// <summary>CSS's &lt;c&gt;flex-grow&lt;/c&gt;: its share of the line's free room. 0 by default.</summary>
    public double FlexGrow { get => GetValue(FlexGrowProperty); set => SetValue(FlexGrowProperty, value); }

    /// <summary>CSS's &lt;c&gt;flex-basis&lt;/c&gt;, in pixels; NaN (the default) is &lt;c&gt;auto&lt;/c&gt;, its content's length.</summary>
    public double FlexBasis { get => GetValue(FlexBasisProperty); set => SetValue(FlexBasisProperty, value); }

    /// <summary>The presenter of its title, which its min-content width is read from.</summary>
    internal ContentPresenter? TitlePresenter { get; set; }

    /// <summary>
    /// Its min-content width, measured at its max-content width first (CSS's &lt;c&gt;min-width: auto&lt;/c&gt; of a flex item): for a
    /// title that is text, the widest word of it in its font, as a line breaks it at its spaces, with what is round it
    /// (padding, the artwork); for other content its own width. Avalonia measures nothing narrower than it is given, so
    /// the width is read, not measured (as &lt;see cref="FormControlLabelContainer"/&gt; reads its end enhancer's).
    /// </summary>
    internal double MinContentWidth()
    {
        var max = DesiredSize.Width;
        if (TitlePresenter is not { } presenter || presenter.Child is not TextBlock { Text: { Length: > 0 } text } block) return max;
        var typeface = new Typeface(block.FontFamily, block.FontStyle, block.FontWeight, block.FontStretch);
        var widest = 0.0;
        foreach (var word in text.Split(Breaks, StringSplitOptions.RemoveEmptyEntries))
        {
            using var layout = new Avalonia.Media.TextFormatting.TextLayout(word, typeface, block.FontSize, null, letterSpacing: block.LetterSpacing);
            widest = Math.Max(widest, layout.WidthIncludingTrailingWhitespace);
        }

        var round = block.Padding.Left + block.Padding.Right + block.Margin.Left + block.Margin.Right;
        var scale = Avalonia.Layout.LayoutHelper.GetLayoutScale(this);
        var min = max - presenter.DesiredSize.Width + Math.Ceiling((widest + round) * scale) / scale;
        return Math.Min(max, Math.Max(0, min));
    }

    /// <summary>The white space a line breaks at: all but the no-break spaces (U+00A0, U+2007, U+202F).</summary>
    private static readonly char[] Breaks = Enumerable.Range(0, 0x3001).Select(c => (char)c)
        .Where(c => char.IsWhiteSpace(c) && c is not ((char)0xA0 or (char)0x2007 or (char)0x202F)).ToArray();

    /// <summary>The tabs whose tab this is: they take its keys and its click.</summary>
    internal Tabs? Owner { get; set; }

    /// <summary>The tab it is the button of.</summary>
    internal Tab? Tab { get; set; }

    /// <summary>Base Web's &lt;c&gt;$isActive&lt;/c&gt;: the active tab's (&lt;c&gt;:is-active&lt;/c&gt;).</summary>
    internal bool IsActive { get; private set; }

    internal void SetActive(bool on)
    {
        IsActive = on;
        PseudoClasses.Set(":is-active", on);
    }

    /// <summary>A mouse over it, CSS's &lt;c&gt;:hover&lt;/c&gt; (&lt;c&gt;:is-hovered&lt;/c&gt;), which its line sets, disabled or not.</summary>
    internal bool IsHovered
    {
        get => _hovered;
        set
        {
            _hovered = value;
            PseudoClasses.Set(":is-hovered", value);
        }
    }

    protected override Type StyleKeyOverride => typeof(TabButton);

    /// <summary>Its growth and basis are its line's to lay it out by: a change lays the line out again.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == FlexGrowProperty || change.Property == FlexBasisProperty) (Parent as TabListPanel)?.InvalidateMeasure();
    }

    /// <summary>tabs.js's onKeyDown first, then the button's own Enter and Space, a browser's activation of a button.</summary>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        Owner?.TabKeyDown(this, e);
        if (!e.Handled) base.OnKeyDown(e);
    }

    protected override void OnClick()
    {
        base.OnClick();
        Owner?.TabClicked(this);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _pressedAt = e.Pointer.Type is PointerType.Touch or PointerType.Pen ? e.GetPosition(null) : null;
        base.OnPointerPressed(e);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        if (_pressedAt is { } at)
        {
            _pressedAt = null;
            var tap = Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type) ?? new Size(10, 10);
            var moved = e.GetPosition(null) - at;
            // A drag, not a tap: let the press go, so the button doesn't click.
            if (Math.Abs(moved.X) > tap.Width / 2 || Math.Abs(moved.Y) > tap.Height / 2) e.Pointer.Capture(null);
        }

        base.OnPointerReleased(e);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TabButtonAutomationPeer(this);
}

/// <summary>
/// A tab to UI Automation: a tab item (&lt;c&gt;role="tab"&lt;/c&gt;), selected while it is the active tab (&lt;c&gt;aria-selected&lt;/c&gt;),
/// named by its title, or its key when it has none (its text). Selecting it is its click.
/// </summary>
public class TabButtonAutomationPeer(TabButton owner) : ButtonAutomationPeer(owner), ISelectionItemProvider
{
    private new TabButton Owner => (TabButton)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

    // An app's own name first; else its text (its content is the tabs' grid, not text).
    protected override string? GetNameCore() =>
        Avalonia.Automation.AutomationProperties.GetName(Owner) is { Length: > 0 } name ? name : Owner.Owner?.TextOf(Owner);

    public bool IsSelected => Owner.IsActive;

    public ISelectionProvider? SelectionContainer => null;

    public void AddToSelection() => Select();

    public void RemoveFromSelection()
    {
    }

    public void Select()
    {
        if (Owner.IsEffectivelyEnabled) Owner.Owner?.TabClicked(Owner);
    }
}
