using System.Collections;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>StatefulMenu</c> and <c>Menu</c> (baseui 18.2.0, <c>menu/stateful-menu.js</c>, <c>menu/menu.js</c>,
/// <c>menu/stateful-container.js</c>) in one: a list of options with its keyboard highlight and scrolling, type-ahead,
/// groups, dividers, its empty state, a child menu for an option (with <see cref="NestedMenus"/>), its parts and overrides.
/// </summary>
/// <remarks>
/// <para>Its items are Base Web's <c>items</c>: <see cref="Items"/> (the XAML content) or <see cref="ItemsSource"/>, any
/// objects (a <see cref="MenuItemData"/> has Base Web's <c>label</c>, <c>disabled</c>, <c>divider</c> and <c>href</c>), with
/// a <see cref="MenuGroup"/> for each of Base Web's grouped keys.</para>
/// <para>Parts, by Base Web's names: <c>List</c> (the template's: <see cref="MenuList"/>), and the elements the menu makes:
/// an <c>OptgroupHeader</c> for each group, an <c>Option</c> for each item (<see cref="MenuOptionList"/>, or the type its
/// override's theme targets: <see cref="MenuOptionProfile"/>), a <c>MenuDivider</c> for each divider, and
/// <c>EmptyState</c> when no group has an item. An option's own parts take the menu's overrides too: <c>ListItem</c>,
/// <c>ListItemAnchor</c>, <c>ChildMenuPopover</c>, and the profile's.</para>
/// <para>Controlled and stateful are one, as Popover's and ButtonGroup's are: <see cref="HighlightedIndex"/> and
/// <see cref="IsMenuFocused"/> are Base Web's state, set by the keys, the pointer and focus through
/// <see cref="StateReducer"/>, whatever bound them, and their first values are Base Web's <c>initialState</c>.
/// <see cref="ItemSelect"/> is <c>onItemSelect</c>.</para>
/// <para>Touch first: a finger's drag over a long menu scrolls it and highlights nothing; a tap highlights the option and
/// chooses it, as a phone's browser's mouse events after a tap do (docs/controls/Menu).</para>
/// </remarks>
[TemplatePart(ListPart, typeof(MenuList))]
[TemplatePart(ItemsPart, typeof(Panel))]
public class Menu : TemplatedControl
{
    public const string ListPart = "List";
    public const string OptionPart = "Option";
    public const string OptgroupHeaderPart = "OptgroupHeader";
    public const string MenuDividerPart = "MenuDivider";
    public const string EmptyStatePart = "EmptyState";
    public const string ItemsPart = "PART_Items";

    /// <summary>Base Web's locale's <c>noResultsMsg</c>, the empty state's default.</summary>
    public const string NoResultsMsgDefault = "No results";

    /// <summary>Base Web's locale's <c>parentMenuItemAriaLabel</c>: the name of an option with a child menu.</summary>
    public const string ParentMenuItemAriaLabel =
        "You are currently at an item that opens a nested listbox. Press right arrow to enter that element and left arrow to return.";

    /// <summary>Base Web's <c>aria-label</c> default.</summary>
    internal const string DefaultAccessibleName = "Menu";

    /// <summary>Type-ahead's letters are let go this long after the last (stateful-container.js's setTimeout).</summary>
    internal static readonly TimeSpan TypeAheadTimeout = TimeSpan.FromMilliseconds(500);

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<Menu, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<int> HighlightedIndexProperty =
        AvaloniaProperty.Register<Menu, int>(nameof(HighlightedIndex), -1, defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsMenuFocusedProperty =
        AvaloniaProperty.Register<Menu, bool>(nameof(IsMenuFocused), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> TypeAheadProperty =
        AvaloniaProperty.Register<Menu, bool>(nameof(TypeAhead), true);

    public static readonly StyledProperty<bool> ForceHighlightProperty =
        AvaloniaProperty.Register<Menu, bool>(nameof(ForceHighlight));

    public static readonly StyledProperty<Func<MenuStateChangeType, MenuState, MenuState, MenuState>?> StateReducerProperty =
        AvaloniaProperty.Register<Menu, Func<MenuStateChangeType, MenuState, MenuState, MenuState>?>(nameof(StateReducer));

    public static readonly StyledProperty<object?> NoResultsMsgProperty =
        AvaloniaProperty.Register<Menu, object?>(nameof(NoResultsMsg));

    public static readonly StyledProperty<InputElement?> KeyboardControlNodeProperty =
        AvaloniaProperty.Register<Menu, InputElement?>(nameof(KeyboardControlNode));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Menu, OverrideCollection?>(nameof(Overrides));

    public static readonly RoutedEvent<MenuItemSelectEventArgs> ItemSelectEvent =
        RoutedEvent.Register<Menu, MenuItemSelectEventArgs>(nameof(ItemSelect), RoutingStrategies.Bubble);

    private readonly Parts _parts;
    private readonly List<object> _flat = [];
    private readonly List<MenuOptionBase?> _options = [];
    private readonly HashSet<INotifyCollectionChanged> _watched = [];
    private MenuList? _list;
    private Panel? _host;
    private string _typeAhead = "";
    private DispatcherTimer? _typeAheadTimer;
    private InputElement? _keyboardNode;
    private NestedMenus? _nesting;
    private bool _updating;

    static Menu()
    {
        // A templated control clips to its bounds: the list's shadow and focus outline are drawn outside them.
        ClipToBoundsProperty.OverrideDefaultValue<Menu>(false);
    }

    public Menu()
    {
        _parts = new Parts(this, "Menu", [ListPart]);
        Items.CollectionChanged += (_, _) => ItemsChanged(update: true);
    }

    /// <summary>The menu's items, its XAML content: any objects, <see cref="MenuItemData"/> and <see cref="MenuGroup"/>s among them.</summary>
    [Content]
    public AvaloniaList<object> Items { get; } = [];

    /// <summary>Base Web's <c>items</c>, from code or a binding: used in place of <see cref="Items"/> when set.</summary>
    public IEnumerable? ItemsSource { get => GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }

    /// <summary>
    /// Base Web's state's <c>highlightedIndex</c>: the highlighted item's index among the items, dividers counted and groups
    /// flattened, or -1 for none. Its first value is <c>initialState.highlightedIndex</c>.
    /// </summary>
    public int HighlightedIndex { get => GetValue(HighlightedIndexProperty); set => SetValue(HighlightedIndexProperty, value); }

    /// <summary>
    /// Base Web's state's <c>isFocused</c>: the menu takes the keys and shows its highlight as focused. Set when its list
    /// is focused or a mouse comes over it, cleared when its list loses focus. Its first value is <c>initialState.isFocused</c>.
    /// </summary>
    public bool IsMenuFocused { get => GetValue(IsMenuFocusedProperty); set => SetValue(IsMenuFocusedProperty, value); }

    /// <summary>Base Web's <c>typeAhead</c>: letters typed highlight the first option whose label starts with them. True by default.</summary>
    public bool TypeAhead { get => GetValue(TypeAheadProperty); set => SetValue(TypeAheadProperty, value); }

    /// <summary>Base Web's <c>forceHighlight</c>: once the state changes with nothing highlighted, the first item is.</summary>
    public bool ForceHighlight { get => GetValue(ForceHighlightProperty); set => SetValue(ForceHighlightProperty, value); }

    /// <summary>
    /// Base Web's <c>stateReducer</c>: given what changed the state, the state it would become and the state it is, the state
    /// it becomes. Null is Base Web's default, which keeps the change.
    /// </summary>
    public Func<MenuStateChangeType, MenuState, MenuState, MenuState>? StateReducer { get => GetValue(StateReducerProperty); set => SetValue(StateReducerProperty, value); }

    /// <summary>Base Web's <c>noResultsMsg</c>: the empty state's content, any content. Null is "No results".</summary>
    public object? NoResultsMsg { get => GetValue(NoResultsMsgProperty); set => SetValue(NoResultsMsgProperty, value); }

    /// <summary>
    /// Base Web's <c>keyboardControlNode</c>: an element whose keys the menu takes while it is focused, in place of its
    /// list's (Select's input keeps the focus while its menu follows the keys).
    /// </summary>
    public InputElement? KeyboardControlNode { get => GetValue(KeyboardControlNodeProperty); set => SetValue(KeyboardControlNodeProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>List</c>, <c>Option</c>, <c>OptgroupHeader</c>, <c>MenuDivider</c>, <c>EmptyState</c>, and the options' own.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's <c>onItemSelect</c>: an option chosen, by a click, a tap or Enter.</summary>
    public event EventHandler<MenuItemSelectEventArgs>? ItemSelect { add => AddHandler(ItemSelectEvent, value); remove => RemoveHandler(ItemSelectEvent, value); }

    /// <summary>The state, as Base Web's container holds it.</summary>
    public MenuState State => new(HighlightedIndex, IsMenuFocused);

    /// <summary>The items, groups flattened, dividers counted: Base Web's <c>getItems()</c>.</summary>
    internal IReadOnlyList<object> FlatItems => _flat;

    /// <summary>The option of an item by its index, null for a divider.</summary>
    internal MenuOptionBase? OptionAt(int index) => index >= 0 && index < _options.Count ? _options[index] : null;

    /// <summary>A part of the current template by its Base Web name (the first, for a part it makes many of), for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>Every element of a part: the template's one, or each the menu made.</summary>
    internal IReadOnlyList<StyledElement> Elements(string name) => _parts.All(name);

    /// <summary>The list, the menu's focusable element.</summary>
    internal MenuList? List => _list;

    /// <summary>The elements the menu made, in order.</summary>
    internal IReadOnlyList<Control> Rendered => _host?.Children ?? (IReadOnlyList<Control>)[];

    protected override Type StyleKeyOverride => typeof(Menu);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_list is not null)
        {
            _list.Owner = null;
            _list.GotFocus -= OnListGotFocus;
            _list.LostFocus -= OnListLostFocus;
            _list.PointerEntered -= OnListPointer;
            _list.PointerMoved -= OnListPointer;
            _list.PointerExited -= OnListExited;
        }

        _parts.Attach(e.NameScope);
        _parts.SetOverrides(Overrides);
        _list = e.NameScope.Find<MenuList>(ListPart);
        _host = e.NameScope.Find<Panel>(ItemsPart);
        if (_list is not null)
        {
            _list.Owner = this;
            _list.GotFocus += OnListGotFocus;
            _list.LostFocus += OnListLostFocus;
            _list.PointerEntered += OnListPointer;
            _list.PointerMoved += OnListPointer;
            _list.PointerExited += OnListExited;
            UpdateName();
        }

        Build();
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        // Mounted: React renders the items it has, and runs componentDidUpdate only after a change.
        ItemsChanged(update: false);
    }

    /// <summary>Out of the tree, it holds on to no collection of the app's, which would keep it.</summary>
    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        foreach (var w in _watched) w.CollectionChanged -= OnSourceChanged;
        _watched.Clear();
    }

    /// <summary>The state it had when it was first shown: its initialState, which a menu shown again starts from.</summary>
    private MenuState? _initial;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _initial ??= State;
        // addMenuToNesting: a menu shown (its offsetHeight not 0), not one hidden.
        _nesting = IsEffectivelyVisible ? NestedMenus.Of(this) : null;
        _nesting?.Add(this);
        WatchKeyboardNode();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _nesting?.Remove(this);
        _nesting = null;
        _keyboardNode?.RemoveHandler(KeyDownEvent, OnKeyboardNodeKeyDown);
        _keyboardNode = null;
        _typeAheadTimer?.Stop();
        _typeAhead = "";
        // Base Web unmounts a menu that is no longer shown (a child menu's closed popover): shown again, it is a new one, at
        // its initialState. Not through the reducer: it is a new container, not a change.
        if (_initial is { } initial) Apply(initial);
    }

    /// <summary>componentDidMount: the highlighted option, if any, scrolled to the list's centre, once laid out.</summary>
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (HighlightedIndex > -1) ScrollIntoView(HighlightedIndex, center: true);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ItemsSourceProperty) ItemsChanged(update: true);
        else if (p == HighlightedIndexProperty)
        {
            UpdateHighlight(change.GetOldValue<int>(), change.GetNewValue<int>());
            if (!_updating) Updated();
        }
        else if (p == IsMenuFocusedProperty)
        {
            foreach (var option in _options) option?.SetValue(MenuOptionBase.IsMenuFocusedProperty, IsMenuFocused, BindingPriority.Template);
            WatchKeyboardNode();
            if (!_updating) Updated();
        }
        else if (p == ForceHighlightProperty) Updated();
        else if (p == OverridesProperty)
        {
            _parts.SetOverrides(Overrides);
            Build();
        }
        else if (p == NoResultsMsgProperty) Build();
        else if (p == KeyboardControlNodeProperty) WatchKeyboardNode();
        else if (p == AutomationProperties.NameProperty) UpdateName();
    }

    private void UpdateName()
    {
        if (_list is null) return;
        AutomationProperties.SetName(_list, AutomationProperties.GetName(this) is { Length: > 0 } name ? name : DefaultAccessibleName);
    }

    // ---- The items ----

    private IEnumerable Source => ItemsSource ?? Items;

    /// <summary>The items changed: watched again, the parts made again, the range checked (componentDidUpdate) if asked.</summary>
    private void ItemsChanged(bool update)
    {
        foreach (var w in _watched) w.CollectionChanged -= OnSourceChanged;
        _watched.Clear();
        Watch(Source);
        foreach (var group in Source.OfType<MenuGroup>()) Watch(group.Items);
        Build();
        if (update) Updated();
    }

    /// <summary>The app's collection followed, only while the menu is in the tree: out of it, nothing of the app's holds it.</summary>
    private void Watch(IEnumerable source)
    {
        if (!((Avalonia.LogicalTree.ILogical)this).IsAttachedToLogicalTree) return;
        if (source is INotifyCollectionChanged c && source != Items && _watched.Add(c)) c.CollectionChanged += OnSourceChanged;
    }

    private void OnSourceChanged(object? sender, NotifyCollectionChangedEventArgs e) => ItemsChanged(update: true);

    /// <summary>The sections menu.js renders: each group with its header, the ungrouped items with none.</summary>
    private List<(string? Header, List<object> Items)> Sections()
    {
        var sections = new List<(string?, List<object>)>();
        List<object>? loose = null;
        foreach (var entry in Source)
        {
            if (entry is MenuGroup group)
            {
                loose = null;
                sections.Add((group.Label, [.. group.Items]));
            }
            else
            {
                if (loose is null) sections.Add((null, loose = []));
                loose.Add(entry);
            }
        }

        return sections;
    }

    /// <summary>The type of the menu's options: the one its <c>Option</c> override's theme targets, else Base Web's OptionList.</summary>
    private Type OptionType()
    {
        var entry = Overrides?.FirstOrDefault(o => o.Part == OptionPart);
        foreach (var theme in new[] { entry?.Component, entry?.Style, entry?.Props })
        {
            if (theme?.TargetType is { IsAbstract: false } type && typeof(MenuOptionBase).IsAssignableFrom(type)) return type;
        }

        return typeof(MenuOptionList);
    }

    /// <summary>menu.js's render: a header for each group, an option or a divider for each item, or the empty state.</summary>
    private void Build()
    {
        var sections = Sections();
        _flat.Clear();
        foreach (var (_, items) in sections) _flat.AddRange(items);
        // The options and the empty state made before let their content go: the new ones take the same items' and message.
        foreach (var old in _options) old?.Release();
        foreach (var old in _parts.All(EmptyStatePart)) ((ContentPresenter)old).Content = null;
        _options.Clear();
        if (_host is null) return;
        _host.Children.Clear();
        var headers = new List<StyledElement>();
        var options = new List<StyledElement>();
        var dividers = new List<StyledElement>();
        var empty = new List<StyledElement>();
        var children = new List<Control>();
        if (sections.All(s => s.Items.Count == 0))
        {
            var state = new ContentPresenter { Content = NoResultsMsg ?? NoResultsMsgDefault };
            // aria-live="polite", aria-atomic: said when it appears.
            AutomationProperties.SetLiveSetting(state, AutomationLiveSetting.Polite);
            empty.Add(state);
            children.Add(state);
        }
        else
        {
            var type = OptionType();
            var index = -1;
            foreach (var (header, items) in sections)
            {
                if (header is not null)
                {
                    var h = new TextBlock { Text = header };
                    headers.Add(h);
                    children.Add(h);
                }

                foreach (var item in items)
                {
                    index++;
                    if (item is MenuItemData { Divider: true })
                    {
                        var d = new Border();
                        dividers.Add(d);
                        children.Add(d);
                        _options.Add(null);
                        continue;
                    }

                    var option = (MenuOptionBase)Activator.CreateInstance(type)!;
                    option.Owner = this;
                    option.Index = index;
                    option.SetValue(MenuOptionBase.ItemProperty, item, BindingPriority.Template);
                    option.SetValue(MenuOptionBase.DisabledProperty, item is MenuItemData { Disabled: true }, BindingPriority.Template);
                    option.SetValue(MenuOptionBase.IsHighlightedProperty, index == HighlightedIndex, BindingPriority.Template);
                    option.SetValue(MenuOptionBase.IsMenuFocusedProperty, IsMenuFocused, BindingPriority.Template);
                    option.SetValue(MenuOptionBase.OverridesProperty, Overrides, BindingPriority.Template);
                    options.Add(option);
                    children.Add(option);
                    _options.Add(option);
                }
            }
        }

        _parts.Place((OptgroupHeaderPart, headers), (OptionPart, options), (MenuDividerPart, dividers), (EmptyStatePart, empty));
        foreach (var child in children) _host.Children.Add(child);
    }

    private void UpdateHighlight(int old, int now)
    {
        OptionAt(old)?.SetValue(MenuOptionBase.IsHighlightedProperty, false, BindingPriority.Template);
        OptionAt(now)?.SetValue(MenuOptionBase.IsHighlightedProperty, true, BindingPriority.Template);
    }

    // ---- The state: stateful-container.js ----

    /// <summary>internalSetState: the change through the reducer, then componentDidUpdate's checks.</summary>
    internal void SetState(MenuStateChangeType type, int? highlightedIndex = null, bool? isFocused = null)
    {
        var current = State;
        var changes = new MenuState(highlightedIndex ?? current.HighlightedIndex, isFocused ?? current.IsFocused);
        var next = StateReducer is { } reducer ? reducer(type, changes, current) : changes;
        Apply(next);
        Updated();
    }

    private void Apply(MenuState next)
    {
        var was = _updating;
        _updating = true;
        try
        {
            SetCurrentValue(IsMenuFocusedProperty, next.IsFocused);
            SetCurrentValue(HighlightedIndexProperty, next.HighlightedIndex);
        }
        finally
        {
            _updating = was;
        }
    }

    /// <summary>
    /// componentDidUpdate: with nothing highlighted, the first item under forceHighlight; nothing in an empty menu; the
    /// first item when the highlight is past the end. Each a change of its own, through the reducer.
    /// </summary>
    private void Updated()
    {
        // Before it is mounted (XAML still setting its values and adding its items) there is no update.
        if (_updating || !((Avalonia.LogicalTree.ILogical)this).IsAttachedToLogicalTree) return;
        var range = _flat.Count;
        if (ForceHighlight && HighlightedIndex == -1 && range > 0) SetState(MenuStateChangeType.Enter, highlightedIndex: 0);
        else if (range == 0 && HighlightedIndex != -1) SetState(MenuStateChangeType.Enter, highlightedIndex: -1);
        else if (HighlightedIndex >= range && range > 0) SetState(MenuStateChangeType.Enter, highlightedIndex: 0);
    }

    /// <summary>The nested menus' hover moved: a deeper menu than the one hovered lets its highlight go (componentDidUpdate).</summary>
    internal void NestedHoverChanged(int hovered, int self)
    {
        if (self > hovered && !ForceHighlight)
        {
            // Base Web's setState here, not internalSetState: no reducer.
            Apply(new MenuState(-1, IsMenuFocused));
        }
    }

    /// <summary>resetMenu: unfocused, nothing highlighted (a child menu's popover's Escape).</summary>
    internal void ResetMenu() => SetState(MenuStateChangeType.Reset, highlightedIndex: -1, isFocused: false);

    private bool _childClosing;

    /// <summary>
    /// A child menu's Escape (or the back request, #131): resetMenu. Its popover closes, and if the focus was in the child,
    /// Avalonia's popup gives it back to the list, where a browser leaves it on the page (the child's list, focused, is
    /// gone): that focus isn't the menu's, and is let go. With the focus elsewhere, none comes back, and none is let go.
    /// </summary>
    internal void ResetFromChild(Visual? child)
    {
        // Only ever set here, so a second call with the focus elsewhere doesn't clear a first one's.
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is Visual focused && Layers.IsWithin(focused, child))
        {
            _childClosing = true;
        }

        ResetMenu();
        // The popup gives the focus back as it closes; if it doesn't come, the next focus is the menu's again.
        DispatcherTimer.RunOnce(() => _childClosing = false, ChildClosingTime);
    }

    /// <summary>How long a child's closing may take to give the focus back.</summary>
    private static readonly TimeSpan ChildClosingTime = TimeSpan.FromMilliseconds(300);

    /// <summary>focusMenu: focused, the first item highlighted if none is, the list focused.</summary>
    private void FocusMenu(NavigationMethod how)
    {
        if (IsMenuFocused) return;
        if (HighlightedIndex < 0) SetState(MenuStateChangeType.Focus, highlightedIndex: 0, isFocused: true);
        else SetState(MenuStateChangeType.Focus, isFocused: true);
        if (_list is { IsFocused: false } list) list.Focus(how);
    }

    /// <summary>
    /// onFocus: focusMenu. (A finger's press doesn't focus the list in Avalonia: as a browser focuses nothing for a drag, the
    /// tap's own mouse events come as it lifts: OptionClicked.)
    /// </summary>
    private void OnListGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source != _list) return;
        if (_childClosing)
        {
            // The focus a closing child's popup gives back: a browser leaves it on the page, and moves nothing (#132).
            _childClosing = false;
            _list?.QuietFocus();
            Dispatcher.UIThread.Post(() =>
            {
                if (_list is { IsFocused: true } && !IsMenuFocused) TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
            });
            return;
        }

        FocusMenu(e.NavigationMethod);
    }

    /// <summary>onBlur: unfocusMenu.</summary>
    private void OnListLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source != _list) return;
        SetState(MenuStateChangeType.Focus, isFocused: false);
    }

    /// <summary>The element a mouse was last over in the list: mouseover fires as it comes onto another.</summary>
    private object? _over;

    /// <summary>onMouseEnter and onMouseOver: focusMenu, a mouse's only, as it comes onto the list and onto each element in it.</summary>
    private void OnListPointer(object? sender, PointerEventArgs e)
    {
        // A child menu's popup routes its pointer events through its option to this list: they aren't this list's.
        if (e.Source is not Visual source || (source != _list && !_list!.IsVisualAncestorOf(source))) return;
        if (e.Pointer.Type != PointerType.Mouse || e.Pointer.Captured is { } c && c != _list && c is Visual v && !_list!.IsVisualAncestorOf(v)) return;
        if (e.RoutedEvent == PointerMovedEvent && source == _over) return;
        _over = source;
        FocusMenu(NavigationMethod.Pointer);
    }

    private void OnListExited(object? sender, PointerEventArgs e) => _over = null;

    /// <summary>
    /// A mouse onto an option: the mouseover that bubbles to the list first (focusMenu), then the option's mouseenter,
    /// handleMouseEnter, which highlights it if it isn't disabled.
    /// </summary>
    internal void OptionMouseEnter(MenuOptionBase option)
    {
        if (option.Owner != this) return;
        FocusMenu(NavigationMethod.Pointer);
        if (option.Disabled || HighlightedIndex == option.Index) return;
        SetState(MenuStateChangeType.MouseEnter, highlightedIndex: option.Index);
    }

    /// <summary>
    /// handleItemClick: an option that isn't disabled chosen, then highlighted. A tap is a phone browser's mouse events as the
    /// finger lifts: mouseover (focusMenu), mouseenter (the highlight), then the click.
    /// </summary>
    internal void OptionClicked(MenuOptionBase option, bool tap)
    {
        if (option.Owner != this) return;
        if (tap)
        {
            FocusMenu(NavigationMethod.Pointer);
            OptionMouseEnter(option);
        }

        if (option.Disabled) return;
        Select(_flat[option.Index]);
        SetState(MenuStateChangeType.Click, highlightedIndex: option.Index);
    }

    private void Select(object item) => RaiseEvent(new MenuItemSelectEventArgs(ItemSelectEvent, this, item));

    // ---- The keys ----

    /// <summary>menu.js's onKeyDown on the list: the container's, while focused, unless a keyboard control node takes them.</summary>
    internal void ListKeyDown(KeyEventArgs e)
    {
        if (e.Source != _list || KeyboardControlNode is not null || !IsMenuFocused) return;
        HandleKey(e);
    }

    private void WatchKeyboardNode()
    {
        var node = IsMenuFocused && this.IsAttachedToVisualTree() ? KeyboardControlNode : null;
        if (node == _keyboardNode) return;
        _keyboardNode?.RemoveHandler(KeyDownEvent, OnKeyboardNodeKeyDown);
        _keyboardNode = node;
        // addEventListener('keydown'): every key on the node, whatever else took it, before the node's own handling, so a
        // key the menu takes (its preventDefault) is the node's no more: a text box's caret doesn't move with the highlight.
        _keyboardNode?.AddHandler(KeyDownEvent, OnKeyboardNodeKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    private void OnKeyboardNodeKeyDown(object? sender, KeyEventArgs e)
    {
        // Its own keys and its descendants', as a keydown listener hears them.
        if (IsMenuFocused) HandleKey(e);
    }

    /// <summary>stateful-container.js's onKeyDown, by the key's DOM name.</summary>
    private void HandleKey(KeyEventArgs e)
    {
        var key = MenuKeys.Name(e);
        switch (key)
        {
            case "ArrowUp":
            case "ArrowDown":
            case "ArrowLeft":
            case "ArrowRight":
            case "Home":
            case "End":
                ArrowKey(key, e);
                break;
            case "Enter":
                EnterKey(e);
                break;
            default:
                if (TypeAhead)
                {
                    _typeAheadTimer?.Stop();
                    AlphaDown(key);
                }

                break;
        }
    }

    private void ArrowKey(string key, KeyEventArgs e)
    {
        var prev = HighlightedIndex;
        var next = prev;
        switch (key)
        {
            case "ArrowUp":
                e.Handled = true;
                next = Math.Max(0, prev - 1);
                SetState(MenuStateChangeType.MoveUp, highlightedIndex: next);
                break;
            case "ArrowDown":
                e.Handled = true;
                next = Math.Min(prev + 1, _flat.Count - 1);
                SetState(MenuStateChangeType.MoveDown, highlightedIndex: next);
                break;
            case "Home":
                e.Handled = true;
                next = 0;
                SetState(MenuStateChangeType.MoveUp, highlightedIndex: next);
                break;
            case "End":
                e.Handled = true;
                next = _flat.Count - 1;
                SetState(MenuStateChangeType.MoveDown, highlightedIndex: next);
                break;
            case "ArrowLeft":
                if (_nesting?.ParentOf(this)?.List is { } parent)
                {
                    parent.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                }

                break;
            case "ArrowRight":
                if (_nesting?.ChildOf(this)?.List is { } child)
                {
                    child.Focus(NavigationMethod.Directional);
                    e.Handled = true;
                }

                break;
        }

        ScrollIntoView(next);
    }

    private void EnterKey(KeyEventArgs e)
    {
        var i = HighlightedIndex;
        if (i < 0 || i >= _flat.Count || _flat[i] is MenuItemData { Disabled: true }) return;
        e.Handled = true;
        Select(_flat[i]);
    }

    /// <summary>handleAlphaDown: the key added to the letters typed, the first label that starts with them highlighted, else one that has them inside.</summary>
    private void AlphaDown(string key)
    {
        _typeAhead += key;
        _typeAheadTimer ??= new DispatcherTimer(TypeAheadTimeout, DispatcherPriority.Background, (_, _) =>
        {
            _typeAheadTimer!.Stop();
            _typeAhead = "";
        });
        _typeAheadTimer.Start();
        var next = HighlightedIndex;
        if (_flat.Count == 0 || !MenuKeys.HasLabel(_flat[0])) return;
        var chars = _typeAhead.ToUpperInvariant();
        var match = false;
        for (var n = 0; n < _flat.Count; n++)
        {
            if (MenuKeys.Label(_flat[n]) is { } label && label.ToUpperInvariant().IndexOf(chars, StringComparison.Ordinal) == 0)
            {
                next = n;
                match = true;
                break;
            }
        }

        if (!match)
        {
            for (var n = 0; n < _flat.Count; n++)
            {
                if (MenuKeys.Label(_flat[n]) is { } label && label.ToUpperInvariant().IndexOf(chars, StringComparison.Ordinal) > 0)
                {
                    next = n;
                    break;
                }
            }
        }

        SetState(MenuStateChangeType.Character, highlightedIndex: next);
        ScrollIntoView(next);
    }

    // ---- Scrolling: utils.js's scrollItemIntoView ----

    /// <summary>The option's top in the list's scrolled content, its padding included (its offsetTop), and its height.</summary>
    private (double Top, double Height)? Geometry(int index)
    {
        if (OptionAt(index) is not { } option || _list?.ContentPart is not { } presenter || presenter.Child is not Visual content) return null;
        if (option.TranslatePoint(default, content) is not { } at) return null;
        var top = at.Y + presenter.Padding.Top;
        return (top, option.Bounds.Height);
    }

    /// <summary>JavaScript's Math.round: the nearest integer, a half towards +∞.</summary>
    private static double JsRound(double x) => Math.Floor(x + 0.5);

    /// <summary>
    /// scrollItemIntoView: below the view, the list scrolled so that it is at the bottom (the last option: to the end), or
    /// centred; above the view, at the top (the first: to the start), or centred. A divider's index scrolls nothing.
    /// </summary>
    internal void ScrollIntoView(int index, bool center = false)
    {
        if (_list is null || index < 0) return;
        if (!_list.IsArrangeValid) _list.UpdateLayout();
        if (Geometry(index) is not var (top, height)) return;
        var scrollTop = _list.Offset.Y;
        var view = _list.Viewport.Height;
        var childTop = top - scrollTop;
        var childBottom = childTop + height;
        var isFirst = index == 0;
        var isLast = index == _flat.Count - 1;
        double? to = null;
        if (childBottom > view)
        {
            if (isLast) to = _list.Extent.Height - view;
            else to = top + height - (center ? JsRound((view + height) / 2) : view);
        }
        else if (childTop < 0)
        {
            if (isFirst) to = 0;
            else to = top - (center ? JsRound((view - height) / 2) : 0);
        }

        if (to is { } y) _list.Offset = new Vector(_list.Offset.X, Math.Max(0, Math.Min(y, Math.Max(0, _list.Extent.Height - view))));
    }

    protected override Avalonia.Automation.Peers.AutomationPeer OnCreateAutomationPeer() => new MenuAutomationPeer(this);
}

/// <summary>Base Web's <c>onItemSelect</c>'s argument: the item chosen.</summary>
public class MenuItemSelectEventArgs(RoutedEvent routedEvent, object source, object item) : RoutedEventArgs(routedEvent, source)
{
    /// <summary>The item chosen.</summary>
    public object Item { get; } = item;
}
