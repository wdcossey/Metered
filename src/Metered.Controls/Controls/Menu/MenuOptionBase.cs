using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// The element of a menu's <c>Option</c> part: one option, Base Web's <c>OptionList</c> (<see cref="MenuOptionList"/>,
/// the default) or <c>OptionProfile</c> (<see cref="MenuOptionProfile"/>), as the menu's <c>Option</c> override chooses
/// by the type its theme targets, as Base Web's <c>overrides.Option.component</c> does.
/// </summary>
/// <remarks>
/// <para>The menu gives each option its item and the states menu.js gives it (<see cref="IsHighlighted"/>,
/// <see cref="IsMenuFocused"/>, <see cref="Disabled"/>), at Template priority, so an <c>Option</c> override's props win, as
/// Base Web's spread them last. The option mirrors them onto its parts as <c>:is-highlighted</c>, <c>:is-focused</c> and
/// <c>:is-disabled</c>, and <c>:loaded</c> once it is loaded, so a transition doesn't run on its first look.</para>
/// <para>Its template is its own, made in code (a theme's <c>Template</c> replaces it), as its parts depend on its item:
/// each subclass's parts, inside a <see cref="Popover"/>, the <c>ChildMenuPopover</c> part, when
/// <see cref="GetChildMenu"/> gives the item a child menu (maybe-child-menu.js).</para>
/// <para>The pointer, as option-list.js and stateful-container.js take it, touch first: a mouse over it highlights it (not a
/// disabled one); a click (a press and a release on it, the pointer not taken from it in between) chooses it, as a tap
/// does, which highlights it too (a phone's browser sends mouseover, mouseenter and click as the finger lifts); a finger's
/// drag is the scroller's, which takes the pointer from it, and does nothing here.</para>
/// </remarks>
[PseudoClasses(":is-highlighted", ":is-focused", ":is-disabled", ":loaded")]
public abstract class MenuOptionBase : TemplatedControl
{
    public const string ChildMenuPopoverPart = "ChildMenuPopover";

    public static readonly StyledProperty<object?> ItemProperty =
        AvaloniaProperty.Register<MenuOptionBase, object?>(nameof(Item));

    public static readonly StyledProperty<bool> IsHighlightedProperty =
        AvaloniaProperty.Register<MenuOptionBase, bool>(nameof(IsHighlighted));

    public static readonly StyledProperty<bool> IsMenuFocusedProperty =
        AvaloniaProperty.Register<MenuOptionBase, bool>(nameof(IsMenuFocused));

    public static readonly StyledProperty<bool> DisabledProperty =
        AvaloniaProperty.Register<MenuOptionBase, bool>(nameof(Disabled));

    public static readonly StyledProperty<Func<object, object?>?> GetChildMenuProperty =
        AvaloniaProperty.Register<MenuOptionBase, Func<object, object?>?>(nameof(GetChildMenu));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<MenuOptionBase, OverrideCollection?>(nameof(Overrides));

    private Parts? _parts;
    private IPointer? _pressed;
    private Popover? _popover;
    private object? _childMenu;

    static MenuOptionBase()
    {
        FocusableProperty.OverrideDefaultValue<MenuOptionBase>(false);
        TemplateProperty.OverrideDefaultValue<MenuOptionBase>(new FuncControlTemplate<MenuOptionBase>((option, scope) => option.MakeTemplate(scope)));
    }

    /// <summary>The item the option shows.</summary>
    public object? Item { get => GetValue(ItemProperty); set => SetValue(ItemProperty, value); }

    /// <summary>Base Web's <c>$isHighlighted</c>: the menu's highlighted option. Its child menu, if it has one, is open while it is.</summary>
    public bool IsHighlighted { get => GetValue(IsHighlightedProperty); set => SetValue(IsHighlightedProperty, value); }

    /// <summary>Base Web's <c>$isFocused</c>: the menu is focused (its state's <c>isFocused</c>), the same for every option.</summary>
    public bool IsMenuFocused { get => GetValue(IsMenuFocusedProperty); set => SetValue(IsMenuFocusedProperty, value); }

    /// <summary>Base Web's <c>$disabled</c>: the item's <c>disabled</c>. A mouse doesn't highlight it, nor a click choose it.</summary>
    public bool Disabled { get => GetValue(DisabledProperty); set => SetValue(DisabledProperty, value); }

    /// <summary>
    /// Base Web's <c>getChildMenu</c>: the child menu for an item (usually another <see cref="Menu"/>), or null for none.
    /// The child is shown at the option's right, at its top, while the option is highlighted (maybe-child-menu.js).
    /// </summary>
    public Func<object, object?>? GetChildMenu { get => GetValue(GetChildMenuProperty); set => SetValue(GetChildMenuProperty, value); }

    /// <summary>The option's own overrides: by default the menu's (menu.js gives it <c>overrides: props.overrides</c>).</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>The menu the option is in.</summary>
    internal Menu? Owner { get; set; }

    /// <summary>Its index among the menu's items, dividers counted, as Base Web's.</summary>
    internal int Index { get; set; }

    /// <summary>Its child menu, when <see cref="GetChildMenu"/> gave the item one.</summary>
    internal object? ChildMenu => _childMenu;

    /// <summary>The popover its child menu is in.</summary>
    internal Popover? ChildMenuPopover => _popover;

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts?[name];

    /// <summary>The option's parts, by Base Web's names.</summary>
    protected abstract string[] PartNames { get; }

    /// <summary>The option's parts: their elements, registered in the scope, the root returned.</summary>
    protected abstract Control Build(INameScope scope);

    /// <summary>A part's element with its theme, <c>Menu.Styled{Part}</c>, as a template's <c>DynamicResource</c> gives it.</summary>
    protected T Themed<T>(T element, string part, INameScope scope) where T : StyledElement
    {
        element.Name = part;
        element.Bind(ThemeProperty, this.GetResourceObservable("Menu.Styled" + part, v => v as ControlTheme), BindingPriority.Template);
        scope.Register(part, element);
        return element;
    }

    private Control MakeTemplate(INameScope scope)
    {
        var root = Build(scope);
        _childMenu = Item is { } item ? GetChildMenu?.Invoke(item) : null;
        if (_childMenu is null)
        {
            _popover = null;
            return root;
        }

        // maybe-child-menu.js: a Popover round the option, open while it is highlighted, at its right, its top. A click on
        // the option is the option's: the anchor's tap doesn't toggle it.
        root.AddHandler(TappedEvent, (_, e) => e.Handled = true);
        root.AddHandler(DoubleTappedEvent, (_, e) => e.Handled = true);
        var popover = new Popover { Name = ChildMenuPopoverPart, Anchor = root, Content = _childMenu };
        popover.SetValue(Popover.PlacementProperty, PopoverPlacement.RightTop, BindingPriority.Template);
        popover.SetValue(Popover.IgnoreBoundaryProperty, true, BindingPriority.Template);
        popover.SetValue(Popover.FocusLockProperty, false, BindingPriority.Template);
        popover.SetValue(Popover.AutoFocusProperty, false, BindingPriority.Template);
        popover.SetValue(Popover.MouseEnterDelayProperty, 30, BindingPriority.Template);
        popover.SetValue(Popover.MouseLeaveDelayProperty, 30, BindingPriority.Template);
        // Controlled: Base Web gives it no onClickOutside, and Escape is onEsc, which resets the parent menu.
        popover.SetValue(Popover.DismissOnClickOutsideProperty, false, BindingPriority.Template);
        popover.SetValue(Popover.DismissOnEscProperty, false, BindingPriority.Template);
        popover.IsOpen = IsHighlighted;
        popover.Esc += (_, _) => Owner?.ResetMenu();
        popover.PropertyChanged += (_, e) =>
        {
            // Held to the highlight, whatever else set it.
            if (e.Property == Popover.IsOpenProperty && popover.IsOpen != IsHighlighted) popover.IsOpen = IsHighlighted;
        };
        // maybe-child-menu.js's Body onKeyDown: Tab stays in the child menu.
        if (_childMenu is InputElement content) content.AddHandler(KeyDownEvent, (_, e) => { if (e.Key == Key.Tab) e.Handled = true; });
        scope.Register(ChildMenuPopoverPart, popover);
        _popover = popover;
        return popover;
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts = new Parts(this, "Menu", [.. PartNames, ChildMenuPopoverPart]);
        _parts.Attach(e.NameScope);
        _parts.SetOverrides(Overrides);
        foreach (var (pseudo, on) in States()) if (Mirrors(pseudo)) _parts.SetState(pseudo, on);
        Templated();
    }

    /// <summary>The template's parts are in: a subclass fills them in.</summary>
    protected virtual void Templated()
    {
    }

    /// <summary>The states the option mirrors onto its parts.</summary>
    protected virtual IEnumerable<(string, bool)> States()
    {
        yield return (":is-highlighted", IsHighlighted);
        yield return (":is-focused", IsMenuFocused);
        yield return (":is-disabled", Disabled);
        yield return (":loaded", IsLoaded);
    }

    /// <summary>Sets a state on the option and, if it mirrors it, its parts.</summary>
    protected void SetState(string pseudo, bool on)
    {
        PseudoClasses.Set(pseudo, on);
        if (Mirrors(pseudo)) _parts?.SetState(pseudo, on);
    }

    /// <summary>Whether a state goes to the option's parts too, as Base Web gives them its prop.</summary>
    protected virtual bool Mirrors(string pseudo) => true;

    /// <summary>The template made again, as its parts depend on what changed.</summary>
    protected void Rebuild()
    {
        if (Template is null) return;
        // The same template object would be kept: a fresh one makes the parts again.
        var template = Template;
        Template = null;
        Template = template;
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == IsHighlightedProperty)
        {
            SetState(":is-highlighted", IsHighlighted);
            if (_popover is not null) _popover.IsOpen = IsHighlighted;
        }
        else if (p == IsMenuFocusedProperty) SetState(":is-focused", IsMenuFocused);
        else if (p == DisabledProperty) SetState(":is-disabled", Disabled);
        else if (p == OverridesProperty) _parts?.SetOverrides(Overrides);
        else if (p == ItemProperty || p == GetChildMenuProperty) Rebuild();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetState(":loaded", true);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        SetState(":loaded", false);
        _pressed = null;
    }

    /// <summary>Whether a pointer event is this option's own, not one from its child menu, whose popup routes it here.</summary>
    private bool Own(RoutedEventArgs e) => e.Source is Visual v && (v == this || this.IsVisualAncestorOf(v));

    /// <summary>A mouse over the option: option-list.js's onMouseEnter, the menu's handleMouseEnter (not on a disabled one).</summary>
    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        if (e.Pointer.Type != PointerType.Mouse || !Own(e)) return;
        Owner?.OptionMouseEnter(this);
        MouseOver(true);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        // A mouse that came in pressed, captured elsewhere, enters when it moves over it.
        if (e.Pointer.Type != PointerType.Mouse || !Own(e) || e.Pointer.Captured is not null) return;
        if (new Rect(Bounds.Size).Contains(e.GetPosition(this))) Owner?.OptionMouseEnter(this);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        if (e.Pointer.Type == PointerType.Mouse) MouseOver(false);
    }

    /// <summary>A mouse over the option or not: the profile option's own <c>:hover</c>.</summary>
    protected virtual void MouseOver(bool over)
    {
    }

    /// <summary>A press: noted, the pointer taken, so that a scroller that takes it for a drag takes the click with it.</summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (!Own(e) || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;
        if (_pressed is not null && _pressed != e.Pointer) return;
        _pressed = e.Pointer;
        _pressed.Capture(this);
    }

    /// <summary>The click: the release of the main button, a finger or a pen's tip that pressed it, on it.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.Pointer != _pressed || e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;
        _pressed = null;
        var at = e.GetPosition(this);
        if (!new Rect(Bounds.Size).Contains(at)) return;
        e.Handled = true;
        Owner?.OptionClicked(this, e.Pointer.Type != PointerType.Mouse);
        Clicked(at);
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (e.Pointer == _pressed) _pressed = null;
    }

    /// <summary>A click on the option at that point, after the menu has had it: an href option's anchor opens its link.</summary>
    protected virtual void Clicked(Point at)
    {
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new MenuOptionAutomationPeer(this);

    /// <summary>What the option says to UI Automation: its label's text, or Base Web's aria-label for an option with a child menu.</summary>
    internal abstract string? AccessibleText { get; }
}
