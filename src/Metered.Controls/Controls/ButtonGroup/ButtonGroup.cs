using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Metered.Controls;

/// <summary>Base Web's ButtonGroup <c>MODE</c>; <see cref="None"/> is a group of plain clickable buttons.</summary>
public enum ButtonGroupMode
{
    None,
    Radio,
    Checkbox,
}

/// <summary>Base Web's ButtonGroup <c>PADDING</c>.</summary>
public enum ButtonGroupPadding
{
    /// <summary>No horizontal padding (Base Web's default).</summary>
    None,

    /// <summary>16 each side: the theme's <c>sizing.scale600</c>.</summary>
    Default,

    /// <summary>The theme sets none: the app's own <c>Root</c> override does.</summary>
    Custom,
}

/// <summary>A click on a group's button: Base Web's <c>onClick(event, index)</c>.</summary>
public sealed class ButtonGroupClickEventArgs(RoutedEvent routedEvent, int index) : RoutedEventArgs(routedEvent)
{
    /// <summary>The button's index in the group.</summary>
    public int Index { get; } = index;
}

/// <summary>
/// Base Web's <c>ButtonGroup</c> and <c>StatefulButtonGroup</c> (baseui 18.2.0,
/// <c>button-group/</c>): <see cref="Button"/>s in a row, given the group's
/// <see cref="Kind"/>, <see cref="Size"/> and <see cref="Shape"/>, and in radio or checkbox
/// <see cref="Mode"/> selected by index.
/// </summary>
/// <remarks>
/// <para>Base Web's two components are one here. <see cref="Selected"/> is the selection: set it or
/// bind it (Base Web's controlled <c>selected</c>), and a click in radio or checkbox mode changes
/// it as <c>StatefulButtonGroup</c>'s container does. <c>disabled</c> is <c>IsEnabled</c>, which
/// Avalonia passes to the buttons.</para>
/// <para>As Base Web clones each child: the group's kind, size and shape replace the button's
/// own; a button's own <c>IsSelected</c> of true is kept, and otherwise the group sets it from
/// <see cref="Selected"/>, except in a group of plain buttons (no mode, no selection), which
/// leaves it alone. In radio mode only the selected button (or the first, with none) is a tab
/// stop, and the arrow keys move the selection and the focus, wrapping. <see cref="Selected"/> is by place, as Base
/// Web's index is: when a button comes or goes before another, that one is selected, and a tab stop, by its new
/// place.</para>
/// <para>As <c>cloneElement</c> replaces a child's props without changing the child, the group holds its values
/// over the button's own, which stay beneath them (Avalonia's <c>Animation</c> priority, the one above a local
/// value): a button that leaves has its own kind, size, shape, selection, tab stop and role again, its bindings and
/// styles with them, and whatever was set on it while it was in the group.</para>
/// <para>Its one part is <c>Root</c>, the row.</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[PseudoClasses(":wrap", ":nowrap", ":padding-default", ":padding-none", ":padding-custom")]
public class ButtonGroup : ItemsControl
{
    public const string RootPart = "Root";

    public static readonly StyledProperty<ButtonGroupMode> ModeProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonGroupMode>(nameof(Mode));

    public static readonly StyledProperty<IReadOnlyList<int>?> SelectedProperty =
        AvaloniaProperty.Register<ButtonGroup, IReadOnlyList<int>?>(nameof(Selected), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<ButtonKind> KindProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonKind>(nameof(Kind), ButtonKind.Secondary);

    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonSize>(nameof(Size), ButtonSize.Default);

    public static readonly StyledProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonShape>(nameof(Shape), ButtonShape.Default);

    public static readonly StyledProperty<ButtonGroupPadding> PaddingTypeProperty =
        AvaloniaProperty.Register<ButtonGroup, ButtonGroupPadding>(nameof(PaddingType), ButtonGroupPadding.None);

    public static readonly StyledProperty<bool?> WrapProperty =
        AvaloniaProperty.Register<ButtonGroup, bool?>(nameof(Wrap));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<ButtonGroup, OverrideCollection?>(nameof(Overrides));

    public static readonly RoutedEvent<ButtonGroupClickEventArgs> ClickEvent =
        RoutedEvent.Register<ButtonGroup, ButtonGroupClickEventArgs>(nameof(Click), RoutingStrategies.Bubble);

    private readonly Parts _parts;

    /// <summary>
    /// The buttons the group has taken, so that each is given back when it leaves, however it leaves, and what the
    /// group holds on each.
    /// </summary>
    private readonly Dictionary<Button, Hold> _members = [];

    /// <summary>
    /// The values the group holds on a button, above the button's own, which stay beneath them: Base Web's
    /// <c>cloneElement</c>, which replaces a child's props and leaves the child unchanged. Let go, the button's own
    /// values are its again, local, bound or styled.
    /// </summary>
    private sealed class Hold(Button button)
    {
        private readonly Dictionary<AvaloniaProperty, (object? Value, IDisposable? Undo)> _held = [];

        /// <summary>Holds <paramref name="value"/> over the button's own.</summary>
        public void Set<T>(StyledProperty<T> property, T value)
        {
            if (_held.TryGetValue(property, out var held) && Equals(held.Value, value)) return;
            var undo = button.SetValue(property, value, BindingPriority.Animation);
            // Let go of the old value after the new one is held, so that the button's own doesn't show between.
            held.Undo?.Dispose();
            _held[property] = (value, undo);
        }

        /// <summary>The button's own value shows again.</summary>
        public void Let(AvaloniaProperty property)
        {
            if (_held.Remove(property, out var held)) held.Undo?.Dispose();
        }

        public void LetAll()
        {
            foreach (var property in _held.Keys.ToList()) Let(property);
        }
    }

    public ButtonGroup()
    {
        _parts = new Parts(this, "ButtonGroup", [RootPart]);
        UpdateStates();
        AddHandler(KeyDownEvent, OnKeyDownForRadio, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
        Items.CollectionChanged += OnItemsChanged;
    }

    private void OnItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // A button that is its own container isn't cleared by ItemsControl when it leaves; nor on a reset (an
        // ObservableCollection's Clear), which lists no old items, so then every button no longer in the group is given
        // back.
        var gone = e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset
            ? _members.Keys.Where(button => !Items.Contains(button)).ToList()
            : e.OldItems?.OfType<Button>().Where(button => !Items.Contains(button)).ToList() ?? [];
        foreach (var button in gone) Release(button);
    }

    /// <summary>Base Web's <c>mode</c>.</summary>
    public ButtonGroupMode Mode { get => GetValue(ModeProperty); set => SetValue(ModeProperty, value); }

    /// <summary>Base Web's <c>selected</c>: the selected buttons' indices. Null is Base Web's undefined.</summary>
    public IReadOnlyList<int>? Selected { get => GetValue(SelectedProperty); set => SetValue(SelectedProperty, value); }

    /// <summary>Base Web's <c>kind</c>, given to every button. Secondary by default.</summary>
    public ButtonKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }

    /// <summary>Base Web's <c>size</c>, given to every button. Default (medium) by default.</summary>
    public ButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>shape</c>, given to every button. Default by default.</summary>
    public ButtonShape Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>Base Web's <c>padding</c>: the row's horizontal padding.</summary>
    public ButtonGroupPadding PaddingType { get => GetValue(PaddingTypeProperty); set => SetValue(PaddingTypeProperty, value); }

    /// <summary>Base Web's <c>wrap</c>: true wraps the buttons onto more rows; false scrolls them sideways with no bar; null does neither.</summary>
    public bool? Wrap { get => GetValue(WrapProperty); set => SetValue(WrapProperty, value); }

    /// <summary>Base Web's <c>overrides</c>: its one part, <c>Root</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's <c>onClick(event, index)</c>, raised after the button's own Click.</summary>
    public event EventHandler<ButtonGroupClickEventArgs>? Click
    {
        add => AddHandler(ClickEvent, value);
        remove => RemoveHandler(ClickEvent, value);
    }

    internal StyledElement? Part(string name) => _parts[name];

    private bool IsSimpleClickable => Mode == ButtonGroupMode.None && Selected is null;

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Button)
        {
            recycleKey = null;
            return false;
        }

        return base.NeedsContainerOverride(item, index, out recycleKey);
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is not Button button) return;
        if (_members.TryAdd(button, new Hold(button)))
        {
            button.Click += OnButtonClick;
            button.PropertyChanged += OnButtonPropertyChanged;
        }

        Apply(button, index);
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is Button button) Release(button);
    }

    /// <summary>A button moved, as another came or went before it: Base Web's index is its place, so its selection and tab stop are its new place's.</summary>
    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        if (container is Button button && _members.ContainsKey(button)) Apply(button, newIndex);
    }

    /// <summary>Gives a button that leaves the group its own values back.</summary>
    private void Release(Button button)
    {
        if (!_members.Remove(button, out var hold)) return;
        button.Click -= OnButtonClick;
        button.PropertyChanged -= OnButtonPropertyChanged;
        hold.LetAll();
    }

    /// <summary>
    /// The button's own IsSelected changed: Base Web keeps an own true, so the button is applied again. Avalonia tells
    /// of no change to a value beneath one the group holds, so this sees an own value only while the group holds none
    /// over it (an own true, or a group of plain buttons); otherwise the own value is read at the group's next apply.
    /// </summary>
    private void OnButtonPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Button.IsSelectedProperty || e.Priority == BindingPriority.Animation) return;
        if (sender is not Button button || !_members.ContainsKey(button)) return;
        var index = IndexFromContainer(button);
        if (index >= 0) Apply(button, index);
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == ModeProperty || p == SelectedProperty || p == KindProperty || p == SizeProperty || p == ShapeProperty)
        {
            ApplyAll();
        }

        if (p == SizeProperty || p == PaddingTypeProperty || p == WrapProperty) UpdateStates();
        if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    private void ApplyAll()
    {
        foreach (var container in GetRealizedContainers())
        {
            if (container is Button button && _members.ContainsKey(button)) Apply(button, IndexFromContainer(button));
        }
    }

    private void Apply(Button button, int index)
    {
        var hold = _members[button];
        // Base Web's cloneElement: the group's kind, shape and size replace the child's.
        hold.Set(Button.KindProperty, Kind);
        hold.Set(Button.ShapeProperty, Shape);
        hold.Set(Button.SizeProperty, Size);

        // child.props.isSelected of true is kept: the button's own, beneath what the group holds.
        var ownSelected = button.GetBaseValue(Button.IsSelectedProperty) is { HasValue: true, Value: true };
        bool? isSelected = ownSelected ? true
            : IsSimpleClickable ? null
            : Selected?.Contains(index) == true;
        if (isSelected is { } selected && !ownSelected) hold.Set(Button.IsSelectedProperty, selected);
        else hold.Let(Button.IsSelectedProperty);

        // Base Web's role and tabIndex.
        var isRadio = Mode == ButtonGroupMode.Radio;
        var noneSelected = Selected is null || Selected.Count == 0;
        hold.Set(IsTabStopProperty, !isRadio || isSelected == true || (noneSelected && index == 0));
        if (isRadio) hold.Set(AutomationProperties.ControlTypeOverrideProperty, AutomationControlType.RadioButton);
        else if (!IsSimpleClickable) hold.Set(AutomationProperties.ControlTypeOverrideProperty, AutomationControlType.CheckBox);
        else hold.Let(AutomationProperties.ControlTypeOverrideProperty);
    }

    private void OnButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !IsEffectivelyEnabled) return;
        var index = IndexFromContainer(button);
        if (index < 0) return;
        Select(index);
        RaiseEvent(new ButtonGroupClickEventArgs(ClickEvent, index));
    }

    /// <summary>StatefulButtonGroup's onClick: a radio selects the index, or clears it if it is the one selected; a checkbox toggles it.</summary>
    private void Select(int index)
    {
        var current = Selected ?? [];
        if (Mode == ButtonGroupMode.Radio)
        {
            SetCurrentValue(SelectedProperty, current.Count == 0 || current[0] != index ? new[] { index } : Array.Empty<int>());
        }
        else if (Mode == ButtonGroupMode.Checkbox)
        {
            SetCurrentValue(SelectedProperty, current.Contains(index)
                ? current.Where(i => i != index).ToArray()
                : current.Append(index).ToArray());
        }
    }

    private void OnKeyDownForRadio(object? sender, KeyEventArgs e)
    {
        if (Mode != ButtonGroupMode.Radio) return;
        var count = ItemCount;
        if (count == 0) return;

        // Base Web reads the current index as Number(selected), which is 0 for none.
        var value = Selected is { Count: > 0 } s ? s[0] : 0;
        int next;
        if (e.Key is Key.Up or Key.Left) next = value - 1 < 0 ? count - 1 : value - 1;
        else if (e.Key is Key.Down or Key.Right) next = value + 1 > count - 1 ? 0 : value + 1;
        else return;

        e.Handled = true;
        Select(next);
        RaiseEvent(new ButtonGroupClickEventArgs(ClickEvent, next));
        ContainerFromIndex(next)?.Focus(NavigationMethod.Directional);
    }

    private void UpdateStates()
    {
        SetState(":wrap", Wrap == true);
        SetState(":nowrap", Wrap == false);
        SetState(":padding-default", PaddingType == ButtonGroupPadding.Default);
        SetState(":padding-none", PaddingType == ButtonGroupPadding.None);
        SetState(":padding-custom", PaddingType == ButtonGroupPadding.Custom);
        var size = Button.Canonical(Size);
        SetState(":size-xsmall", size == ButtonSize.XSmall);
        SetState(":size-small", size == ButtonSize.Small);
        SetState(":size-medium", size == ButtonSize.Medium);
        SetState(":size-large", size == ButtonSize.Large);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }
}
