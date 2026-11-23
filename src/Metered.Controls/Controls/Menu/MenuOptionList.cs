using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>OptionList</c> (<c>menu/option-list.js</c>), a menu's default option: its <c>ListItem</c> part (the
/// option's box, Base Web's <c>&lt;li&gt;</c>) round its label, or round its <c>ListItemAnchor</c> part, a link, for an item
/// with an <c>href</c> while <see cref="RenderHrefAsAnchor"/>.
/// </summary>
/// <remarks>
/// Its <c>Size</c>, <c>GetItemLabel</c>, <c>RenderHrefAsAnchor</c> and <c>GetChildMenu</c> are Base Web's Option props, given
/// by the menu's <c>Option</c> override (its <c>Props</c>). Its size is <c>:size-default</c> or <c>:size-compact</c> on it and
/// its parts. A click within the anchor opens its link with the platform's launcher, after the menu has chosen the item,
/// as a browser follows a link the click was on: an absolute link as it is, one without a scheme (<c>//host/path</c>, as
/// Base Web's example has them) by https; a relative one, which a page would resolve against its own address, isn't
/// opened (docs/controls/Menu).
/// </remarks>
[PseudoClasses(":size-default", ":size-compact")]
public class MenuOptionList : MenuOptionBase
{
    public const string ListItemPart = "ListItem";
    public const string ListItemAnchorPart = "ListItemAnchor";

    public static readonly StyledProperty<MenuOptionSize> SizeProperty =
        AvaloniaProperty.Register<MenuOptionList, MenuOptionSize>(nameof(Size));

    public static readonly StyledProperty<Func<object?, object?>?> GetItemLabelProperty =
        AvaloniaProperty.Register<MenuOptionList, Func<object?, object?>?>(nameof(GetItemLabel));

    public static readonly StyledProperty<bool> RenderHrefAsAnchorProperty =
        AvaloniaProperty.Register<MenuOptionList, bool>(nameof(RenderHrefAsAnchor), true);

    private ContentPresenter? _anchor;

    public MenuOptionList()
    {
        UpdateSize();
    }

    /// <summary>Base Web's <c>size</c>: the option's padding and type. Default by default.</summary>
    public MenuOptionSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>
    /// Base Web's <c>getItemLabel</c>: what the option shows for its item, any content. Null is Base Web's default, the item's
    /// <c>label</c>: a <see cref="MenuItemData"/>'s <see cref="MenuItemData.Label"/>, or the item itself.
    /// </summary>
    public Func<object?, object?>? GetItemLabel { get => GetValue(GetItemLabelProperty); set => SetValue(GetItemLabelProperty, value); }

    /// <summary>Base Web's <c>renderHrefAsAnchor</c>: an item's <c>href</c> makes its label a link. True by default.</summary>
    public bool RenderHrefAsAnchor { get => GetValue(RenderHrefAsAnchorProperty); set => SetValue(RenderHrefAsAnchorProperty, value); }

    /// <summary>The label the option shows.</summary>
    internal object? Label => GetItemLabel is { } f ? f(Item) : Item is MenuItemData d ? d.Label : Item;

    private string? Href => RenderHrefAsAnchor && Item is MenuItemData { Href: { Length: > 0 } href } ? href : null;

    protected override Type StyleKeyOverride => typeof(MenuOptionList);

    protected override string[] PartNames => [ListItemPart, ListItemAnchorPart];

    protected override Control Build(INameScope scope)
    {
        var item = Themed(new ContentPresenter(), ListItemPart, scope);
        _anchor = null;
        if (Href is not null)
        {
            _anchor = Themed(new ContentPresenter { Content = Label }, ListItemAnchorPart, scope);
            item.Content = _anchor;
        }
        else item.Content = Label;
        return item;
    }

    protected override void ReleaseParts()
    {
        if (Part(ListItemPart) is ContentPresenter item) item.Content = null;
        if (_anchor is not null) _anchor.Content = null;
        _anchor = null;
    }

    protected override IEnumerable<(string, bool)> States()
    {
        foreach (var s in base.States()) yield return s;
        yield return (":size-default", Size == MenuOptionSize.Default);
        yield return (":size-compact", Size == MenuOptionSize.Compact);
    }

    private void UpdateSize()
    {
        SetState(":size-default", Size == MenuOptionSize.Default);
        SetState(":size-compact", Size == MenuOptionSize.Compact);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == SizeProperty) UpdateSize();
        else if (change.Property == GetItemLabelProperty || change.Property == RenderHrefAsAnchorProperty) Rebuild();
    }

    /// <summary>A click within the anchor: its link opened, as a browser follows it.</summary>
    protected override void Clicked(Point at)
    {
        if (_anchor is null || Href is not { } href || this.TranslatePoint(at, _anchor) is not { } inside) return;
        if (!new Rect(_anchor.Bounds.Size).Contains(inside) || LinkOf(href) is not { } uri) return;
        _ = TopLevel.GetTopLevel(this)?.Launcher.LaunchUriAsync(uri);
    }

    /// <summary>The link an href opens: an absolute one as it is, one without a scheme by https; a relative one none.</summary>
    internal static Uri? LinkOf(string href)
    {
        if (href.StartsWith("//", StringComparison.Ordinal)) href = "https:" + href;
        return Uri.TryCreate(href, UriKind.Absolute, out var uri) && !uri.IsFile ? uri : null;
    }

    internal override string? AccessibleText =>
        ChildMenu is not null ? Menu.ParentMenuItemAriaLabel : Label switch
        {
            string s => s,
            null => null,
            var other => other.ToString(),
        };
}
