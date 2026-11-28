using System.Collections.Specialized;
using System.Globalization;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Base Web's &lt;c&gt;Tabs&lt;/c&gt; and &lt;c&gt;StatefulTabs&lt;/c&gt; (baseui 18.2.0, &lt;c&gt;tabs-motion/tabs.js&lt;/c&gt;,
/// &lt;c&gt;tabs-motion/stateful-tabs.js&lt;/c&gt;) in one: a line of tabs with its moving highlight and its border, and the active
/// tab's panel. Base Web's tabs-motion is its current tabs (its &lt;c&gt;tabs&lt;/c&gt; is deprecated in its favour).
/// &lt;/summary&gt;
/// &lt;remarks&gt;
/// &lt;para&gt;Its tabs are its XAML content, &lt;see cref="Tab"/&gt;s (&lt;see cref="Items"/&gt;). Parts, by Base Web's names: &lt;c&gt;Root&lt;/c&gt;
/// (&lt;see cref="TabsRootPanel"/&gt;), &lt;c&gt;TabBar&lt;/c&gt;, &lt;c&gt;TabList&lt;/c&gt; (&lt;see cref="TabList"/&gt;), &lt;c&gt;TabHighlight&lt;/c&gt;
/// (&lt;see cref="TabHighlight"/&gt;), &lt;c&gt;TabBorder&lt;/c&gt; and &lt;c&gt;EndEnhancerContainer&lt;/c&gt;, the template's, which &lt;see cref="Overrides"/&gt;
/// reaches; and for each tab the elements it makes: &lt;c&gt;Tab&lt;/c&gt; (&lt;see cref="TabButton"/&gt;), &lt;c&gt;ArtworkContainer&lt;/c&gt; (only with
/// an artwork) and &lt;c&gt;TabPanel&lt;/c&gt;, which the tab's own &lt;see cref="Tab.Overrides"/&gt; reaches, as Base Web's.&lt;/para&gt;
/// &lt;para&gt;Controlled and stateful are one, as Menu's and Popover's are: &lt;see cref="ActiveKey"/&gt; is StatefulTabs' state, set by
/// a click, a tap and the keys through &lt;see cref="StateReducer"/&gt; whatever bound it; unset, it becomes the first tab's key
/// (StatefulTabs' initial state). &lt;see cref="Change"/&gt; is &lt;c&gt;onChange&lt;/c&gt;.&lt;/para&gt;
/// &lt;para&gt;Touch first: a finger's drag sideways along tabs that overflow scrolls them and chooses none; a tap chooses one; a
/// drag mostly up or down is the page's (docs/controls/Tabs).&lt;/para&gt;
/// &lt;/remarks&gt;
[TemplatePart(RootPart, typeof(TabsRootPanel))]
[TemplatePart(TabBarPart, typeof(Grid))]
[TemplatePart(TabListPart, typeof(TabList))]
[TemplatePart(TabHighlightPart, typeof(TabHighlight))]
[TemplatePart(TabBorderPart, typeof(Control))]
[TemplatePart(EndEnhancerContainerPart, typeof(ContentPresenter))]
[TemplatePart(TabsPart, typeof(TabListPanel))]
[TemplatePart(PanelsPart, typeof(Panel))]
public class Tabs : TemplatedControl
{
    public const string RootPart = "Root";
    public const string TabBarPart = "TabBar";
    public const string TabListPart = "TabList";
    public const string TabHighlightPart = "TabHighlight";
    public const string TabBorderPart = "TabBorder";
    public const string EndEnhancerContainerPart = "EndEnhancerContainer";
    public const string TabPart = "Tab";
    public const string ArtworkContainerPart = "ArtworkContainer";
    public const string TabPanelPart = "TabPanel";
    public const string TabsPart = "PART_Tabs";
    public const string PanelsPart = "PART_Panels";

    public static readonly StyledProperty<string?> ActiveKeyProperty =
        AvaloniaProperty.Register<Tabs, string?>(nameof(ActiveKey), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> DisabledProperty =
        AvaloniaProperty.Register<Tabs, bool>(nameof(Disabled));

    public static readonly StyledProperty<TabsFill> FillProperty =
        AvaloniaProperty.Register<Tabs, TabsFill>(nameof(Fill));

    public static readonly StyledProperty<Orientation> OrientationProperty =
        StackPanel.OrientationProperty.AddOwner<Tabs>(new StyledPropertyMetadata<Orientation>(Orientation.Horizontal));

    public static readonly StyledProperty<bool> ActivateOnFocusProperty =
        AvaloniaProperty.Register<Tabs, bool>(nameof(ActivateOnFocus), true);

    public static readonly StyledProperty<bool> RenderAllProperty =
        AvaloniaProperty.Register<Tabs, bool>(nameof(RenderAll));

    public static readonly StyledProperty<object?> EndEnhancerProperty =
        AvaloniaProperty.Register<Tabs, object?>(nameof(EndEnhancer));

    public static readonly StyledProperty<Func<TabsState, TabsAction, TabsState>?> StateReducerProperty =
        AvaloniaProperty.Register<Tabs, Func<TabsState, TabsAction, TabsState>?>(nameof(StateReducer));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Tabs, OverrideCollection?>(nameof(Overrides));

    public static readonly RoutedEvent<TabsChangeEventArgs> ChangeEvent =
        RoutedEvent.Register<Tabs, TabsChangeEventArgs>(nameof(Change), RoutingStrategies.Bubble);

    private readonly Parts _parts;
    private readonly List<Built> _built = [];
    private TabListPanel? _line;
    private Panel? _panels;
    private TabList? _list;
    private ContentPresenter? _endEnhancer;
    private TabHighlight? _highlight;
    private int _keyUpdated;
    private bool _mounted;
    private bool _reanimate;

    /// <summary>What it made of a tab: its button, its artwork's container, its title's presenter, its panel, their parts.</summary>
    private sealed class Built(Tab tab, TabButton button, Grid content, ContentPresenter title, ContentPresenter panel, Parts parts)
    {
        public Tab Tab { get; } = tab;

        public string Key { get; set; } = "";

        public TabButton Button { get; } = button;

        public Grid Content { get; } = content;

        public ContentPresenter? Artwork { get; set; }

        public ContentPresenter Title { get; } = title;

        public ContentPresenter Panel { get; } = panel;

        public Parts Parts { get; } = parts;
    }

    static Tabs()
    {
        // A templated control clips to its bounds: the tab list's margin puts the highlight over the border, inside them.
        ClipToBoundsProperty.OverrideDefaultValue<Tabs>(false);
    }

    public Tabs()
    {
        _parts = new Parts(this, "Tabs", [RootPart, TabBarPart, TabListPart, TabHighlightPart, TabBorderPart, EndEnhancerContainerPart]);
        Items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>The tabs, its XAML content: Base Web's &lt;c&gt;children&lt;/c&gt;.</summary>
    [Content]
    public AvaloniaList<Tab> Items { get; } = [];

    /// <summary>
    /// Base Web's &lt;c&gt;activeKey&lt;/c&gt; (StatefulTabs' state): the active tab's key, compared with each tab's as text (tabs.js's
    /// &lt;c&gt;key == activeKey&lt;/c&gt;). Set by a click, a tap and the keys; unset (or empty), the first tab's, as StatefulTabs'
    /// initial state. A key no tab has leaves none active, as Base Web's.
    /// </summary>
    public string? ActiveKey { get => GetValue(ActiveKeyProperty); set => SetValue(ActiveKeyProperty, value); }

    /// <summary>Base Web's &lt;c&gt;disabled&lt;/c&gt;: every tab but the active one disabled, unless the tab's own &lt;see cref="Tab.Disabled"/&gt; says otherwise.</summary>
    public bool Disabled { get => GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }

    /// <summary>Base Web's &lt;c&gt;fill&lt;/c&gt;: intrinsic (each tab its own length, scrolling when they don't fit) or fixed (equal shares).</summary>
    public TabsFill Fill { get => GetValue(FillProperty); set => SetValue(FillProperty, value); }

    /// <summary>Base Web's &lt;c&gt;orientation&lt;/c&gt;: horizontal (the default) or vertical.</summary>
    public Orientation Orientation { get => GetValue(OrientationProperty); set => SetValue(OrientationProperty, value); }

    /// <summary>Base Web's &lt;c&gt;activateOnFocus&lt;/c&gt;: an arrow key that moves the focus to a tab chooses it. True by default.</summary>
    public bool ActivateOnFocus { get => GetValue(ActivateOnFocusProperty); set => SetValue(ActivateOnFocusProperty, value); }

    /// <summary>Base Web's &lt;c&gt;renderAll&lt;/c&gt;: every panel holds its content, the inactive ones hidden.</summary>
    public bool RenderAll { get => GetValue(RenderAllProperty); set => SetValue(RenderAllProperty, value); }

    /// <summary>Base Web's &lt;c&gt;endEnhancer&lt;/c&gt;: content after the tab list, horizontal only (ignored when vertical, as Base Web).</summary>
    public object? EndEnhancer { get => GetValue(EndEnhancerProperty); set => SetValue(EndEnhancerProperty, value); }

    /// <summary>
    /// StatefulTabs' &lt;c&gt;stateReducer&lt;/c&gt;: given the state and the change, the state it becomes. Null is Base Web's default,
    /// which takes the change's key.
    /// </summary>
    public Func<TabsState, TabsAction, TabsState>? StateReducer { get => GetValue(StateReducerProperty); set => SetValue(StateReducerProperty, value); }

    /// <summary>Base Web's &lt;c&gt;overrides&lt;/c&gt;, by part: &lt;c&gt;Root&lt;/c&gt;, &lt;c&gt;TabBar&lt;/c&gt;, &lt;c&gt;TabList&lt;/c&gt;, &lt;c&gt;TabHighlight&lt;/c&gt;, &lt;c&gt;TabBorder&lt;/c&gt;, &lt;c&gt;EndEnhancerContainer&lt;/c&gt;.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's &lt;c&gt;onChange&lt;/c&gt;: a tab chosen, its key, after the state has taken it.</summary>
    public event EventHandler<TabsChangeEventArgs>? Change { add => AddHandler(ChangeEvent, value); remove => RemoveHandler(ChangeEvent, value); }

    /// <summary>A part of the current template by its Base Web name (the first, for a part it makes many of), for tests and tools.</summary>
    internal StyledElement? Part(string name) => name switch
    {
        TabPart or ArtworkContainerPart or TabPanelPart => Elements(name).FirstOrDefault(),
        _ => _parts[name],
    };

    /// <summary>Every element of a part: the template's one, or the ones it made, in its tabs' order.</summary>
    internal IReadOnlyList<StyledElement> Elements(string name) => name switch
    {
        TabPart => _built.Select(b => (StyledElement)b.Button).ToList(),
        ArtworkContainerPart => _built.Where(b => b.Artwork is not null).Select(b => (StyledElement)b.Artwork!).ToList(),
        TabPanelPart => _built.Select(b => (StyledElement)b.Panel).ToList(),
        _ => _parts.All(name),
    };

    /// <summary>The key of each tab, in order (Base Web's &lt;c&gt;child.key || String(index)&lt;/c&gt;).</summary>
    internal IReadOnlyList<string> Keys => _built.Select(b => b.Key).ToList();

    /// <summary>The button of the tab with that key, if it has one.</summary>
    internal TabButton? ButtonOf(string key) => _built.FirstOrDefault(b => b.Key == key)?.Button;

    /// <summary>The tab's text, its name: its title's when it is text, else its key's, as Base Web renders &lt;c&gt;title ? title : key&lt;/c&gt;.</summary>
    internal string? TextOf(TabButton button)
    {
        var b = _built.FirstOrDefault(x => x.Button == button);
        if (b is null) return null;
        return IsBlank(b.Tab.Title) ? b.Key : b.Tab.Title switch
        {
            string s => s,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            // A control's text, as a button's text content names it: its own peer's name (a text block's text).
            Control c => ControlAutomationPeer.CreatePeerForElement(c).GetName() is { Length: > 0 } name ? name : b.Key,
            var other => other?.ToString() ?? b.Key,
        };
    }

    protected override Type StyleKeyOverride => typeof(Tabs);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Release();
        _parts.Attach(e.NameScope);
        _parts.SetOverrides(Overrides);
        _line = e.NameScope.Find<TabListPanel>(TabsPart);
        _panels = e.NameScope.Find<Panel>(PanelsPart);
        _list = e.NameScope.Find<TabList>(TabListPart);
        _endEnhancer = e.NameScope.Find<ContentPresenter>(EndEnhancerContainerPart);
        _highlight = e.NameScope.Find<TabHighlight>(TabHighlightPart);
        // Once the key has changed, a new template's highlight moves too: once it has been placed, so it doesn't slide in.
        _reanimate = _keyUpdated > 0;
        UpdateShared();
        Build();
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Initial();
    }

    /// <summary>
    /// StatefulTabs' getInitialState: the first tab's key when none is given (an empty key is none: JavaScript's falsy).
    /// </summary>
    private void Initial()
    {
        if (string.IsNullOrEmpty(ActiveKey) && Items.Count > 0) SetCurrentValue(ActiveKeyProperty, KeyAt(0));
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        // On mount: the highlight placed, not moved there; the active tab centred in a list that overflows.
        UpdateHighlight();
        if (!_mounted)
        {
            _mounted = true;
            CentreActive();
        }
    }

    /// <summary>Unmounted: shown again, it is mounted again, as Base Web's: centred, its highlight placed, not moved.</summary>
    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _mounted = false;
        _keyUpdated = 0;
        _reanimate = false;
        if (_highlight is not null) ((IPseudoClasses)_highlight.Classes).Set(":animate", false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ActiveKeyProperty)
        {
            if (string.IsNullOrEmpty(ActiveKey) && Items.Count > 0 && ((ILogical)this).IsAttachedToLogicalTree)
            {
                // The first tab's key, whose own change updates the parts.
                Initial();
                return;
            }

            // keyUpdated: from the first change after mount on, the highlight moves to its place ($animate).
            if (_mounted) _keyUpdated++;
            if (_highlight is not null) ((IPseudoClasses)_highlight.Classes).Set(":animate", _keyUpdated > 0);
            UpdateTabs();
            UpdateHighlight();
            if (_mounted) ScrollActiveIntoView();
        }
        else if (p == DisabledProperty || p == RenderAllProperty) UpdateTabs();
        else if (p == OrientationProperty || p == FillProperty || p == EndEnhancerProperty)
        {
            UpdateShared();
            UpdateTabs();
            UpdateHighlight();
        }
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    // ---- The tabs ----

    private void OnItemsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (Tab tab in e.OldItems)
            {
                tab.Changed -= OnTabChanged;
                LogicalChildren.Remove(tab);
            }
        }

        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            foreach (var tab in LogicalChildren.OfType<Tab>().ToList())
            {
                tab.Changed -= OnTabChanged;
                LogicalChildren.Remove(tab);
            }
        }

        if (e.NewItems is not null)
        {
            foreach (Tab tab in e.NewItems)
            {
                // A tab's content can be shown by one panel only: a tab is in one tabs, once.
                if (Items.Count(t => t == tab) > 1) throw new InvalidOperationException("A Tab can be in a Tabs' items only once.");
                if (tab.Parent is { } owner && owner != this) throw new InvalidOperationException("The Tab is already another control's.");
                if (tab.Parent is null) LogicalChildren.Add(tab);
                tab.Changed -= OnTabChanged;
                tab.Changed += OnTabChanged;
            }
        }

        if (((ILogical)this).IsAttachedToLogicalTree) Initial();
        Build();
        UpdateHighlight();
    }

    /// <summary>A tab's change, its own parts updated in place, as React renders the same elements again: the focus stays.</summary>
    private void OnTabChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (_built.FirstOrDefault(x => x.Tab == sender) is not { } b) return;
        if (e.Property == Tab.OverridesProperty) b.Parts.SetOverrides(b.Tab.Overrides);
        else if (e.Property == Tab.ArtworkProperty)
        {
            SetArtwork(b);
            Place(b);
        }
        else if (e.Property == Tab.KeyProperty)
        {
            for (var i = 0; i < _built.Count; i++) _built[i].Key = KeyAt(i);
        }

        UpdateTabs();
        UpdateHighlight();
    }

    private string KeyAt(int index) => Items[index].Key is { Length: > 0 } key ? key : index.ToString(CultureInfo.InvariantCulture);

    private static bool IsBlank(object? title) => title is null || title is string { Length: 0 };

    /// <summary>A tab's parts let go, its content given back, so the tab's content can be shown again by new ones.</summary>
    private static void Release(Built b)
    {
        b.Title.Content = null;
        if (b.Artwork is not null) b.Artwork.Content = null;
        b.Panel.Content = null;
        b.Parts.Detach();
    }

    /// <summary>Every part it made let go (a new template makes them again).</summary>
    private void Release()
    {
        foreach (var b in _built)
        {
            b.Button.PropertyChanged -= OnButtonChanged;
            Release(b);
        }

        _built.Clear();
    }

    /// <summary>
    /// tabs.js's render: for each tab its button (its artwork's container and its title) and its panel. A tab already shown
    /// keeps its parts, as React keeps an element it renders again, so a focused tab stays focused; a new one is made, a
    /// tab gone lets its parts go.
    /// </summary>
    private void Build()
    {
        if (_line is null || _panels is null) return;
        var kept = _built.ToDictionary(b => b.Tab);
        var next = new List<Built>();
        foreach (var tab in Items)
        {
            if (kept.Remove(tab, out var b))
            {
                next.Add(b);
                continue;
            }

            var title = new ContentPresenter { VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(title, 1);
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Children = { title } };
            var button = new TabButton { Owner = this, Tab = tab, TitlePresenter = title, Content = grid };
            var parts = new Parts(this, "Tabs", []);
            parts.SetOverrides(tab.Overrides);
            b = new Built(tab, button, grid, title, new ContentPresenter(), parts);
            SetArtwork(b);
            Place(b);
            button.PropertyChanged += OnButtonChanged;
            next.Add(b);
        }

        foreach (var gone in kept.Values)
        {
            gone.Button.PropertyChanged -= OnButtonChanged;
            Release(gone);
        }

        _built.Clear();
        _built.AddRange(next);
        for (var i = 0; i < _built.Count; i++) _built[i].Key = KeyAt(i);
        Order(_line.Children, _built.Select(b => (Control)b.Button).ToList(), c => c is TabButton);
        Order(_panels.Children, _built.Select(b => (Control)b.Panel).ToList(), _ => true);
        UpdateTabs();
    }

    /// <summary>The children in this order, at the start, those that stay left where they are, so they keep their focus.</summary>
    private static void Order(Avalonia.Controls.Controls children, List<Control> wanted, Func<Control, bool> ours)
    {
        foreach (var c in children.Where(ours).Where(c => !wanted.Contains(c)).ToList()) children.Remove(c);
        for (var i = 0; i < wanted.Count; i++)
        {
            if (i < children.Count && children[i] == wanted[i]) continue;
            children.Remove(wanted[i]);
            children.Insert(i, wanted[i]);
        }
    }

    /// <summary>The artwork's container, only with an artwork, before the title (tabs.js renders it only then).</summary>
    private static void SetArtwork(Built b)
    {
        if (b.Tab.Artwork is null)
        {
            if (b.Artwork is null) return;
            b.Artwork.Content = null;
            b.Content.Children.Remove(b.Artwork);
            b.Artwork = null;
            return;
        }

        if (b.Artwork is null)
        {
            b.Artwork = new ContentPresenter { VerticalAlignment = VerticalAlignment.Center };
            b.Content.Children.Insert(0, b.Artwork);
        }

        b.Artwork.Content = b.Tab.Artwork;
    }

    private void Place(Built b)
    {
        b.Parts.SetState(":orientation-vertical", Orientation == Orientation.Vertical);
        b.Parts.SetState(":fill-fixed", Fill == TabsFill.Fixed);
        b.Parts.Place((TabPart, [b.Button]), (ArtworkContainerPart, b.Artwork is null ? [] : [b.Artwork]), (TabPanelPart, [b.Panel]));
    }

    /// <summary>The shared styling props ($orientation, $fill) on every part, the bar's $hasEndEnhancer, the enhancer shown.</summary>
    private void UpdateShared()
    {
        var vertical = Orientation == Orientation.Vertical;
        _parts.SetState(":orientation-vertical", vertical);
        _parts.SetState(":fill-fixed", Fill == TabsFill.Fixed);
        if (_parts[TabBarPart] is { } bar) ((IPseudoClasses)bar.Classes).Set(":has-end-enhancer", EndEnhancer is not null);
        if (_endEnhancer is not null) _endEnhancer.IsVisible = !vertical && EndEnhancer is not null;
        foreach (var b in _built)
        {
            b.Parts.SetState(":orientation-vertical", vertical);
            b.Parts.SetState(":fill-fixed", Fill == TabsFill.Fixed);
        }
    }

    /// <summary>
    /// Each tab's state as InternalTab renders it: active (&lt;c&gt;key == activeKey&lt;/c&gt;), its tab stop (only the active tab's
    /// &lt;c&gt;tabIndex&lt;/c&gt; is 0), disabled (&lt;c&gt;!isActive &amp;&amp; disabled&lt;/c&gt;, its own disabled or else the tabs'), its title (its key
    /// without one); each panel shown only for the active tab, holding its content only then, or with RenderAll.
    /// </summary>
    private void UpdateTabs()
    {
        foreach (var b in _built)
        {
            var active = b.Key == ActiveKey;
            b.Button.SetActive(active);
            b.Parts.SetState(":is-active", active);
            b.Button.IsTabStop = active;
            // InternalTab takes the tab's own disabled over the tabs' (its props spread after them), then renders
            // !isActive && disabled: the active tab is never disabled.
            b.Button.IsEnabled = active || !(b.Tab.Disabled ?? Disabled);
            b.Title.Content = IsBlank(b.Tab.Title) ? b.Key : b.Tab.Title;
            b.Panel.IsVisible = active;
            b.Panel.Content = active || RenderAll ? b.Tab.Content : null;
            AutomationProperties.SetName(b.Panel, TextOf(b.Button));
            AutomationProperties.SetControlTypeOverride(b.Panel, AutomationControlType.Pane);
        }

        // A hovered tab enabled or disabled: the line's cursor follows.
        _line?.RefreshHover();
    }

    // ---- The highlight, and scrolling ----

    private TabButton? Active => _built.FirstOrDefault(b => b.Key == ActiveKey)?.Button;

    /// <summary>The ResizeObserver: a tab's size or place changed, the highlight placed again.</summary>
    private void OnButtonChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == BoundsProperty) UpdateHighlight();
    }

    /// <summary>JavaScript's Math.round: the nearest integer, a half towards +∞.</summary>
    private static double JsRound(double x) => Math.Floor(x + 0.5);

    /// <summary>
    /// updateHighlight: the highlight at the active tab, by getLayoutParams: its &lt;c&gt;clientWidth&lt;/c&gt; and &lt;c&gt;offsetLeft&lt;/c&gt;
    /// (vertically its &lt;c&gt;clientHeight&lt;/c&gt; and &lt;c&gt;offsetTop&lt;/c&gt;), whole pixels, in the tab list's box. No active tab leaves it
    /// where it is.
    /// </summary>
    private void UpdateHighlight()
    {
        if (_highlight is null || _line is null || Active is not { } tab || !tab.IsArrangeValid && tab.Bounds == default) return;
        var vertical = Orientation == Orientation.Vertical;
        var r = tab.Bounds;
        _highlight.SetValue(TabHighlight.LengthProperty, JsRound(vertical ? r.Height : r.Width), BindingPriority.Template);
        _highlight.SetValue(TabHighlight.DistanceProperty, JsRound(vertical ? r.Y : r.X), BindingPriority.Template);
        if (_reanimate)
        {
            _reanimate = false;
            ((IPseudoClasses)_highlight.Classes).Set(":animate", true);
        }
    }

    /// <summary>scrollParentToCentreTarget: on mount, a list that overflows scrolled so the active tab's centre is its centre.</summary>
    private void CentreActive()
    {
        if (_list is null || Active is not { } tab) return;
        if (!_list.IsArrangeValid) _list.UpdateLayout();
        var vertical = Orientation == Orientation.Vertical;
        var extent = vertical ? _list.Extent.Height : _list.Extent.Width;
        var view = vertical ? _list.Viewport.Height : _list.Viewport.Width;
        if (extent <= view) return;
        var r = tab.Bounds;
        var target = vertical ? r.Y + r.Height / 2 - view / 2 : r.X + r.Width / 2 - view / 2;
        // Out of bounds is the browser's to manage: it clamps.
        target = Math.Max(0, Math.Min(target, extent - view));
        _list.Offset = vertical ? new Vector(_list.Offset.X, target) : new Vector(target, _list.Offset.Y);
    }

    /// <summary>
    /// A changed key in a list that overflows: scrollIntoView({block: 'nearest', inline: 'nearest'}) on the active tab, the list
    /// and the page round it scrolled as little as shows it (at once: Base Web's is smooth).
    /// </summary>
    private void ScrollActiveIntoView()
    {
        if (_list is null || Active is not { } tab || !this.IsAttachedToVisualTree()) return;
        var vertical = Orientation == Orientation.Vertical;
        var overflows = vertical ? _list.Extent.Height > _list.Viewport.Height : _list.Extent.Width > _list.Viewport.Width;
        if (overflows) tab.BringIntoView();
    }

    // ---- The keys and the click ----

    /// <summary>
    /// InternalTab's handleKeyDown: the tabs that aren't disabled, and with more than one, an arrow along the line (Left and
    /// Right, or Up and Down vertically) moves the focus to the previous or next, wrapping at the ends, and with
    /// activateOnFocus clicks it; vertically the key is taken (preventDefault), so the page doesn't scroll.
    /// </summary>
    internal void TabKeyDown(TabButton source, KeyEventArgs e)
    {
        var available = _built.Select(b => b.Button).Where(b => b.IsEnabled).ToList();
        if (available.Count == 1) return;
        var vertical = Orientation == Orientation.Vertical;
        bool? previous = (vertical, e.Key) switch
        {
            (false, Key.Left) => true,
            (false, Key.Right) => false,
            (true, Key.Up) => true,
            (true, Key.Down) => false,
            _ => null,
        };
        if (previous is not { } back) return;
        var i = available.IndexOf(source);
        TabButton? next;
        if (back) next = i - 1 >= 0 && i - 1 < available.Count ? available[i - 1] : available.LastOrDefault();
        else next = i + 1 >= 0 && i + 1 < available.Count ? available[i + 1] : available.FirstOrDefault();
        if (next is not null)
        {
            next.Focus(NavigationMethod.Directional);
            // focus() scrolls it into view.
            next.BringIntoView();
            if (ActivateOnFocus) TabClicked(next);
        }

        if (vertical) e.Handled = true;
    }

    /// <summary>The tab's onClick: onChange with its key: StatefulTabs' dispatch through the reducer, then the app's onChange.</summary>
    internal void TabClicked(TabButton button)
    {
        var b = _built.FirstOrDefault(x => x.Button == button);
        if (b is null) return;
        var state = new TabsState(ActiveKey);
        var action = new TabsAction(TabsStateChangeType.Change, b.Key);
        var next = StateReducer is { } reducer ? reducer(state, action) : new TabsState(action.Payload);
        SetCurrentValue(ActiveKeyProperty, next.ActiveKey);
        RaiseEvent(new TabsChangeEventArgs(ChangeEvent, this, b.Key));
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new TabsAutomationPeer(this);
}

/// <summary>Base Web's &lt;c&gt;onChange&lt;/c&gt;'s argument: &lt;c&gt;{activeKey}&lt;/c&gt;, the key of the tab chosen.</summary>
public class TabsChangeEventArgs(RoutedEvent routedEvent, object source, string activeKey) : RoutedEventArgs(routedEvent, source)
{
    /// <summary>The key of the tab chosen.</summary>
    public string ActiveKey { get; } = activeKey;
}

/// <summary>The tabs to UI Automation: nothing of their own; the tab list and the panels are.</summary>
public class TabsAutomationPeer(Tabs owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override bool IsControlElementCore() => false;

    protected override bool IsContentElementCore() => false;
}
