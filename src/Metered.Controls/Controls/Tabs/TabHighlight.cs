using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a &lt;see cref="Tabs"/&gt;' &lt;c&gt;TabHighlight&lt;/c&gt; part, Base Web's absolutely placed &lt;c&gt;StyledTabHighlight&lt;/c&gt;:
/// a bar of &lt;see cref="Background"/&gt;, &lt;see cref="Length"/&gt; long (its &lt;c&gt;$length&lt;/c&gt;, the active tab's width, or height
/// when vertical) and &lt;see cref="Distance"/&gt; along the tab list (its &lt;c&gt;$distance&lt;/c&gt;, the tab's &lt;c&gt;translateX&lt;/c&gt;, or
/// &lt;c&gt;translateY&lt;/c&gt;), as thick as its &lt;c&gt;Height&lt;/c&gt; (its &lt;c&gt;Width&lt;/c&gt;), at the side its alignment gives (&lt;c&gt;bottom: 0&lt;/c&gt;,
/// &lt;c&gt;right: 0&lt;/c&gt;). The &lt;see cref="TabListPanel"/&gt; it is in places it, over the tabs' line and its padding.
/// &lt;/summary&gt;
public class TabHighlight : Control
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        Border.BackgroundProperty.AddOwner<TabHighlight>();

    public static readonly StyledProperty<double> LengthProperty =
        AvaloniaProperty.Register<TabHighlight, double>(nameof(Length));

    public static readonly StyledProperty<double> DistanceProperty =
        AvaloniaProperty.Register<TabHighlight, double>(nameof(Distance));

    static TabHighlight()
    {
        AffectsRender<TabHighlight>(BackgroundProperty);
        AffectsMeasure<TabHighlight>(LengthProperty);
        IsHitTestVisibleProperty.OverrideDefaultValue<TabHighlight>(false);
    }

    /// <summary>The bar's colour.</summary>
    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }

    /// <summary>Base Web's &lt;c&gt;$length&lt;/c&gt;: the bar's length along the tab list, in pixels.</summary>
    public double Length { get => GetValue(LengthProperty); set => SetValue(LengthProperty, value); }

    /// <summary>Base Web's &lt;c&gt;$distance&lt;/c&gt;: the bar's start along the tab list, in pixels.</summary>
    public double Distance { get => GetValue(DistanceProperty); set => SetValue(DistanceProperty, value); }

    /// <summary>As long as its length either way: its thickness is its own &lt;c&gt;Width&lt;/c&gt; or &lt;c&gt;Height&lt;/c&gt;, which wins.</summary>
    protected override Size MeasureOverride(Size availableSize) => new(Math.Max(0, Length), Math.Max(0, Length));

    /// <summary>Its length and distance are its line's to place it by: a change lays the line out again.</summary>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == LengthProperty) (Parent as TabListPanel)?.InvalidateMeasure();
        else if (change.Property == DistanceProperty) (Parent as TabListPanel)?.InvalidateArrange();
    }

    public override void Render(DrawingContext context)
    {
        if (Background is { } brush) context.FillRectangle(brush, new Rect(Bounds.Size));
    }
}
