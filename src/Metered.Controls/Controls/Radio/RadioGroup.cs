using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Metered.Controls;

/// <summary>
/// Base Web's radio-v2 <c>RadioGroup</c> and <c>StatefulRadioGroup</c> (baseui 18.2.0, <c>radio-v2/radiogroup.js</c>,
/// <c>stateful-radiogroup.js</c>): <see cref="Radio"/>s in a column or a wrapping row, one of them checked, by
/// <see cref="Value"/>.
/// </summary>
/// <remarks>
/// <para>Base Web's two components are one here. <see cref="Value"/> is the checked radio's value: set it or bind it
/// (Base Web's controlled <c>value</c>), and a click or an arrow key changes it as <c>StatefulRadioGroup</c>'s
/// container does. <c>disabled</c> is <c>IsEnabled</c>, which Avalonia passes to the radios; a radio's own stays.</para>
/// <para>As Base Web's group context: each radio draws the group's <see cref="Align"/>, <see cref="LabelPlacement"/>
/// and <see cref="Error"/> in place of its own, which it keeps for when it leaves; the radio whose value is the
/// group's is checked; only that radio (or the first, when the group has no value) is a tab stop. The arrow keys move
/// the selection and the focus to the next or previous radio that is enabled, wrapping, as a browser does for radios
/// of one name. It is announced as a group (Base Web's <c>role="radiogroup"</c>; Avalonia has no radio group).</para>
/// <para>As Base Web's context stands in for a radio's <c>checked</c> and <c>tabIndex</c> without changing its props,
/// the group holds whether each radio is checked and a tab stop over the radio's own, which stay beneath
/// (<see cref="Hold"/>, at Avalonia's <c>Animation</c> priority, the one above a local value): a radio that leaves has
/// its own again, its bindings and styles with them, and whatever was set on it while it was in the group.</para>
/// <para>Its one part is <c>RadioGroupRoot</c>.</para>
/// </remarks>
[TemplatePart(RadioGroupRootPart, typeof(StyledElement))]
[PseudoClasses(":error", ":align-vertical", ":align-horizontal",
    ":label-placement-top", ":label-placement-right", ":label-placement-bottom", ":label-placement-left")]
public class RadioGroup : ItemsControl
{
    public const string RadioGroupRootPart = "RadioGroupRoot";

    public static readonly StyledProperty<string?> ValueProperty =
        AvaloniaProperty.Register<RadioGroup, string?>(nameof(Value), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<RadioAlign> AlignProperty =
        Radio.AlignProperty.AddOwner<RadioGroup>();

    public static readonly StyledProperty<RadioLabelPlacement> LabelPlacementProperty =
        Radio.LabelPlacementProperty.AddOwner<RadioGroup>();

    public static readonly StyledProperty<bool> ErrorProperty =
        Radio.ErrorProperty.AddOwner<RadioGroup>();

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<RadioGroup, OverrideCollection?>(nameof(Overrides));

    private readonly Parts _parts;

    /// <summary>
    /// The radios the group has taken, so that each is given back when it leaves, however it leaves, and what the
    /// group holds on each over the radio's own (<see cref="Hold"/>): whether it is checked (Base Web's own
    /// <c>checked</c>, which the group's value stands in for) and its tab stop.
    /// </summary>
    private readonly Dictionary<Radio, Hold> _members = [];

    static RadioGroup()
    {
        AutomationProperties.ControlTypeOverrideProperty.OverrideDefaultValue<RadioGroup>(AutomationControlType.Group);
    }

    public RadioGroup()
    {
        _parts = new Parts(this, "RadioGroup", [RadioGroupRootPart]);
        UpdateStates();
        AddHandler(KeyDownEvent, OnKeyDownForArrows, RoutingStrategies.Tunnel);
        Items.CollectionChanged += OnItemsChanged;
    }

    /// <summary>Base Web's <c>value</c>: the checked radio's <see cref="Radio.Value"/>. Null or empty checks none.</summary>
    public string? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Base Web's <c>align</c>, drawn by every radio: a column (vertical, the default) or a wrapping row.</summary>
    public RadioAlign Align { get => GetValue(AlignProperty); set => SetValue(AlignProperty, value); }

    /// <summary>Base Web's <c>labelPlacement</c>, drawn by every radio. Right by default.</summary>
    public RadioLabelPlacement LabelPlacement { get => GetValue(LabelPlacementProperty); set => SetValue(LabelPlacementProperty, value); }

    /// <summary>Base Web's <c>error</c>, drawn by every radio.</summary>
    public bool Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>overrides</c>: its one part, <c>RadioGroupRoot</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    internal StyledElement? Part(string name) => _parts[name];

    protected override bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
    {
        if (item is Radio)
        {
            recycleKey = null;
            return false;
        }

        return base.NeedsContainerOverride(item, index, out recycleKey);
    }

    protected override void PrepareContainerForItemOverride(Control container, object? item, int index)
    {
        base.PrepareContainerForItemOverride(container, item, index);
        if (container is not Radio radio) return;
        if (_members.TryAdd(radio, new Hold(radio)))
        {
            radio.IsCheckedChanged += OnRadioCheckedChanged;
            radio.Group = this;
        }

        Apply(radio, index);
    }

    protected override void ClearContainerForItemOverride(Control container)
    {
        base.ClearContainerForItemOverride(container);
        if (container is Radio radio) Release(radio);
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
        if (p == ValueProperty) ApplyAll();
        if (p == AlignProperty || p == LabelPlacementProperty || p == ErrorProperty)
        {
            UpdateStates();
            foreach (var radio in _members.Keys) radio.UpdateContext();
        }

        if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    /// <summary>A radio moved, as another came or went before it: the first may be another, the tab stop of a group with no value.</summary>
    protected override void ContainerIndexChangedOverride(Control container, int oldIndex, int newIndex)
    {
        base.ContainerIndexChangedOverride(container, oldIndex, newIndex);
        if (container is Radio radio && _members.ContainsKey(radio)) Apply(radio, newIndex);
    }

    /// <summary>A radio's value changed: whether it is checked, and a tab stop, again.</summary>
    internal void Refresh(Radio radio)
    {
        var index = IndexFromContainer(radio);
        if (index >= 0) Apply(radio, index);
    }

    private void OnItemsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        // A radio that is its own container isn't cleared by ItemsControl when it leaves; nor on a reset (Clear), which
        // lists no old items, so then every radio no longer in the group is given back.
        var gone = e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset
            ? _members.Keys.Where(radio => !Items.Contains(radio)).ToList()
            : e.OldItems?.OfType<Radio>().Where(radio => !Items.Contains(radio)).ToList() ?? [];
        foreach (var radio in gone) Release(radio);
    }

    /// <summary>Gives a radio that leaves the group its own values back: the group lets go of what it holds.</summary>
    private void Release(Radio radio)
    {
        if (!_members.Remove(radio, out var hold)) return;
        radio.IsCheckedChanged -= OnRadioCheckedChanged;
        radio.Group = null;
        hold.LetAll();
    }

    private void ApplyAll()
    {
        foreach (var container in GetRealizedContainers())
        {
            if (container is Radio radio && _members.ContainsKey(radio)) Apply(radio, IndexFromContainer(radio));
        }
    }

    private void Apply(Radio radio, int index)
    {
        var hold = _members[radio];
        // Base Web's checked = groupContext.selectedValue === value, whatever the radio's own.
        var none = string.IsNullOrEmpty(Value);
        var isChecked = !none && radio.Value == Value;
        hold.Set(ToggleButton.IsCheckedProperty, (bool?)isChecked);
        // Base Web's tabIndex: the checked radio, or the first when the group has no value.
        hold.Set(IsTabStopProperty, isChecked || (none && index == 0));
    }

    private void OnRadioCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is Radio { IsChecked: true } radio && radio.Value != Value) SetCurrentValue(ValueProperty, radio.Value);
    }

    private void OnKeyDownForArrows(object? sender, KeyEventArgs e)
    {
        int step;
        if (e.Key is Key.Up or Key.Left) step = -1;
        else if (e.Key is Key.Down or Key.Right) step = 1;
        else return;

        // A shortcut with an arrow (Alt+Left is a back gesture) isn't the radios' to take.
        if ((e.KeyModifiers & (KeyModifiers.Alt | KeyModifiers.Control | KeyModifiers.Meta)) != 0) return;
        // Only from a radio of this group itself, not a control in a radio's label.
        if (e.Source is not Radio from || !_members.ContainsKey(from)) return;
        var index = IndexFromContainer(from);
        var count = ItemCount;
        if (index < 0 || count == 0) return;
        // A browser's radios take the arrow, even with no other to move to: it doesn't scroll the page.
        e.Handled = true;

        for (var i = 1; i < count; i++)
        {
            var next = ContainerFromIndex(((index + step * i) % count + count) % count);
            if (next is not Radio radio || !radio.IsEffectivelyEnabled || !radio.IsEffectivelyVisible) continue;
            SetCurrentValue(ValueProperty, radio.Value);
            radio.Focus(NavigationMethod.Directional);
            return;
        }
    }

    private void UpdateStates() => Radio.SetContextStates(SetState, Align, LabelPlacement, Error);

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }
}
