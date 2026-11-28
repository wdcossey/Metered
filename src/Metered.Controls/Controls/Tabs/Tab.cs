using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Metered.Controls;

/// <summary>
/// Base Web's tabs-motion &lt;c&gt;Tab&lt;/c&gt; (&lt;c&gt;tab.js&lt;/c&gt;, which renders nothing itself): a tab of a &lt;see cref="Tabs"/&gt;, its
/// title, key, artwork, disabled state and overrides, and its panel's content (its XAML content). The tabs render its
/// button and its panel.
/// &lt;/summary&gt;
/// &lt;remarks&gt;
/// It is a logical child of its tabs, so its content, title and artwork, when they are controls, are its own logical
/// children and take the tabs' data context and resources; their visual parents are the parts that show them.
/// &lt;/remarks&gt;
public class Tab : StyledElement
{
    public static readonly StyledProperty<object?> TitleProperty =
        AvaloniaProperty.Register<Tab, object?>(nameof(Title));

    public static readonly StyledProperty<string?> KeyProperty =
        AvaloniaProperty.Register<Tab, string?>(nameof(Key));

    public static readonly StyledProperty<object?> ArtworkProperty =
        AvaloniaProperty.Register<Tab, object?>(nameof(Artwork));

    public static readonly StyledProperty<bool?> DisabledProperty =
        AvaloniaProperty.Register<Tab, bool?>(nameof(Disabled));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Tab, OverrideCollection?>(nameof(Overrides));

    public static readonly StyledProperty<object?> ContentProperty =
        AvaloniaProperty.Register<Tab, object?>(nameof(Content));

    /// <summary>Base Web's &lt;c&gt;title&lt;/c&gt;: the tab's label, any content. None shows its key, as &lt;c&gt;title ? title : key&lt;/c&gt;.</summary>
    public object? Title { get => GetValue(TitleProperty); set => SetValue(TitleProperty, value); }

    /// <summary>
    /// Base Web's &lt;c&gt;key&lt;/c&gt;: the tab's key, which &lt;see cref="Tabs.ActiveKey"/&gt; names. Unset is its index, as text
    /// (&lt;c&gt;child.key || String(index)&lt;/c&gt;).
    /// </summary>
    public string? Key { get => GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

    /// <summary>
    /// Base Web's &lt;c&gt;artwork&lt;/c&gt;: content before the title, an icon, in the &lt;c&gt;ArtworkContainer&lt;/c&gt; part. Base Web renders
    /// its component at size 20 and colour &lt;c&gt;contentPrimary&lt;/c&gt;: the container gives the content that colour, and the
    /// resource &lt;c&gt;Tabs.ArtworkSize&lt;/c&gt; is the size (bind an icon's width and height to it).
    /// </summary>
    public object? Artwork { get => GetValue(ArtworkProperty); set => SetValue(ArtworkProperty, value); }

    /// <summary>
    /// Base Web's &lt;c&gt;disabled&lt;/c&gt;: unset follows the tabs' &lt;see cref="Tabs.Disabled"/&gt;; set, it wins over it. The active tab
    /// is never disabled, as tabs.js renders &lt;c&gt;!isActive &amp;&amp; disabled&lt;/c&gt;.
    /// </summary>
    public bool? Disabled { get => GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }

    /// <summary>Base Web's &lt;c&gt;overrides&lt;/c&gt; of a tab, by part: &lt;c&gt;Tab&lt;/c&gt;, &lt;c&gt;ArtworkContainer&lt;/c&gt;, &lt;c&gt;TabPanel&lt;/c&gt;.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's &lt;c&gt;children&lt;/c&gt;: the panel's content, any content.</summary>
    [Content]
    public object? Content { get => GetValue(ContentProperty); set => SetValue(ContentProperty, value); }

    /// <summary>A change the tabs follow.</summary>
    internal event EventHandler<AvaloniaPropertyChangedEventArgs>? Changed;

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == TitleProperty || p == ArtworkProperty || p == ContentProperty)
        {
            if (change.OldValue is ILogical old) LogicalChildren.Remove(old);
            if (change.NewValue is ILogical now && now.LogicalParent is null) LogicalChildren.Add(now);
        }

        if (p == TitleProperty || p == KeyProperty || p == ArtworkProperty || p == DisabledProperty || p == OverridesProperty || p == ContentProperty)
        {
            Changed?.Invoke(this, change);
        }
    }
}
