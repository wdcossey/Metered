using System.Collections;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Select</c> and <c>StatefulSelect</c> (baseui 18.2.0, <c>select/select-component.js</c>, <c>select/select.js</c>,
/// <c>select/stateful-select.js</c>) in one: single and multi, searchable and creatable, clearable, its tags for a multi
/// value, its sizes, error and positive, loading, and its dropdown of options, Metered's <see cref="Popover"/> round
/// Metered's <see cref="Menu"/>, as <c>dropdown.js</c> shows them.
/// </summary>
/// <remarks>
/// <para>Its options are Base Web's <c>options</c>: <see cref="Options"/>, any objects (a <see cref="SelectOption"/> has
/// Base Web's <c>id</c>, <c>label</c>, <c>disabled</c> and <c>clearableValue</c>), a <see cref="SelectGroup"/> for each of
/// Base Web's grouped keys. Its value is Base Web's <c>value</c>, always a list: <see cref="Value"/>.</para>
/// <para>Controlled and stateful are one, as Popover's and Menu's are: <see cref="Value"/>, <see cref="IsOpen"/> and
/// <see cref="InputValue"/> are set by the select as Base Web's StatefulSelect and its own state set them, whatever bound
/// them; <see cref="Change"/> is <c>onChange</c>. Its state runs through Base Web's own handlers, ported: a click and a tap
/// on the control, focus and blur, the keys, a click or a tap outside, typing, a choice in the dropdown, the clear icon, a
/// tag's action, and Escape's release (the dropdown popover's <c>onEsc</c>).</para>
/// <para>Parts, by Base Web's names: <c>Popover</c>, <c>Root</c>, <c>ControlContainer</c>, <c>ValueContainer</c>,
/// <c>SingleValue</c>, <c>InputContainer</c>, <c>Input</c>, <c>Placeholder</c>, <c>IconsContainer</c>,
/// <c>LoadingIndicator</c>, <c>ClearIcon</c>, <c>SelectArrow</c>, <c>SearchIconContainer</c>, <c>SearchIcon</c> (the
/// template's); <c>Tag</c> (<c>MultiValue</c> too) for each multi value, <c>DropdownContainer</c>, <c>StatefulMenu</c> and
/// an <c>OptionContent</c> for each option (the ones it makes); and <c>Dropdown</c>, <c>DropdownOption</c> and
/// <c>DropdownListItem</c>, which are the menu's <c>List</c>, <c>Option</c> and <c>ListItem</c> and go to it as its
/// overrides, as dropdown.js gives them. States, on it and every part (getSharedProps): <c>:size-*</c>, <c>:is-focused</c>,
/// <c>:is-pseudo-focused</c>, <c>:is-open</c>, <c>:error</c>, <c>:positive</c>, <c>:multi</c>, <c>:has-value</c>,
/// <c>:searchable</c>, <c>:type-search</c>, <c>:is-loading</c>, <c>:clearable</c>, and Avalonia's <c>:disabled</c>.</para>
/// <para>Touch first: a finger's tap on the control is its click; a drag over it (the page scrolled) opens nothing; a
/// finger down outside closes the dropdown, as Base Web's touchstart listener does; the dropdown is the menu's, a drag in it
/// scrolling it and a tap choosing (docs/controls/Select).</para>
/// </remarks>
[TemplatePart(PopoverPart, typeof(Popover))]
[TemplatePart(RootPart, typeof(Control))]
[TemplatePart(ControlContainerPart, typeof(Control))]
[TemplatePart(ValueContainerPart, typeof(SelectValueContainer))]
[TemplatePart(SingleValuePart, typeof(ContentPresenter))]
[TemplatePart(InputContainerPart, typeof(SelectInputContainer))]
[TemplatePart(InputPart, typeof(SelectInput))]
[TemplatePart(PlaceholderPart, typeof(TextBlock))]
[TemplatePart(IconsContainerPart, typeof(Control))]
[TemplatePart(LoadingIndicatorPart, typeof(Control))]
[TemplatePart(ClearIconPart, typeof(Control))]
[TemplatePart(SelectArrowPart, typeof(Control))]
[TemplatePart(SearchIconContainerPart, typeof(Control))]
[TemplatePart(SearchIconPart, typeof(Control))]
[PseudoClasses(":size-mini", ":size-compact", ":size-default", ":size-large", ":is-focused", ":is-pseudo-focused", ":is-open", ":error",
    ":positive", ":multi", ":has-value", ":searchable", ":type-search", ":is-loading", ":clearable", ":loaded")]
public class Select : TemplatedControl
{
    public const string PopoverPart = "Popover";
    public const string RootPart = "Root";
    public const string ControlContainerPart = "ControlContainer";
    public const string ValueContainerPart = "ValueContainer";
    public const string SingleValuePart = "SingleValue";
    public const string TagPart = "Tag";
    public const string MultiValuePart = "MultiValue";
    public const string InputContainerPart = "InputContainer";
    public const string InputPart = "Input";
    public const string PlaceholderPart = "Placeholder";
    public const string IconsContainerPart = "IconsContainer";
    public const string LoadingIndicatorPart = "LoadingIndicator";
    public const string ClearIconPart = "ClearIcon";
    public const string SelectArrowPart = "SelectArrow";
    public const string SearchIconContainerPart = "SearchIconContainer";
    public const string SearchIconPart = "SearchIcon";
    public const string DropdownContainerPart = "DropdownContainer";
    public const string DropdownPart = "Dropdown";
    public const string DropdownOptionPart = "DropdownOption";
    public const string DropdownListItemPart = "DropdownListItem";
    public const string OptionContentPart = "OptionContent";
    public const string StatefulMenuPart = "StatefulMenu";

    /// <summary>Base Web's locale's <c>placeholder</c>.</summary>
    public const string PlaceholderDefault = "Select...";

    /// <summary>Base Web's locale's <c>create</c>: a creatable option's label is <c>Create “text”</c>.</summary>
    public const string CreateLabel = "Create";

    /// <summary>Base Web's <c>maxDropdownHeight</c> default.</summary>
    public const string MaxDropdownHeightDefault = "40vh";

    private static readonly string[] TemplateParts = [PopoverPart, RootPart, ControlContainerPart, ValueContainerPart, SingleValuePart,
        InputContainerPart, InputPart, PlaceholderPart, IconsContainerPart, LoadingIndicatorPart, ClearIconPart, SelectArrowPart,
        SearchIconContainerPart, SearchIconPart];

    private static readonly string[] MadeParts = [TagPart, DropdownContainerPart, StatefulMenuPart, OptionContentPart];

    public static readonly StyledProperty<IEnumerable?> OptionsProperty =
        AvaloniaProperty.Register<Select, IEnumerable?>(nameof(Options));

    public static readonly StyledProperty<IReadOnlyList<object>?> ValueProperty =
        AvaloniaProperty.Register<Select, IReadOnlyList<object>?>(nameof(Value), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<Select, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<string> InputValueProperty =
        AvaloniaProperty.Register<Select, string>(nameof(InputValue), "", defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> MultiProperty = AvaloniaProperty.Register<Select, bool>(nameof(Multi));
    public static readonly StyledProperty<bool> SearchableProperty = AvaloniaProperty.Register<Select, bool>(nameof(Searchable), true);
    public static readonly StyledProperty<bool> CreatableProperty = AvaloniaProperty.Register<Select, bool>(nameof(Creatable));
    public static readonly StyledProperty<bool> ClearableProperty = AvaloniaProperty.Register<Select, bool>(nameof(Clearable), true);
    public static readonly StyledProperty<bool> ErrorProperty = AvaloniaProperty.Register<Select, bool>(nameof(Error));
    public static readonly StyledProperty<bool> PositiveProperty = AvaloniaProperty.Register<Select, bool>(nameof(Positive));
    public static readonly StyledProperty<bool> IsLoadingProperty = AvaloniaProperty.Register<Select, bool>(nameof(IsLoading));
    public static readonly StyledProperty<bool> RequiredProperty = AvaloniaProperty.Register<Select, bool>(nameof(Required));
    public static readonly StyledProperty<SelectSize> SizeProperty = AvaloniaProperty.Register<Select, SelectSize>(nameof(Size), SelectSize.Default);
    public static readonly StyledProperty<SelectType> TypeProperty = AvaloniaProperty.Register<Select, SelectType>(nameof(Type));
    public static readonly StyledProperty<string?> PlaceholderProperty = AvaloniaProperty.Register<Select, string?>(nameof(Placeholder));
    public static readonly StyledProperty<object?> NoResultsMsgProperty = AvaloniaProperty.Register<Select, object?>(nameof(NoResultsMsg));
    public static readonly StyledProperty<string> MaxDropdownHeightProperty =
        AvaloniaProperty.Register<Select, string>(nameof(MaxDropdownHeight), MaxDropdownHeightDefault);
    public static readonly StyledProperty<bool> CloseOnSelectProperty = AvaloniaProperty.Register<Select, bool>(nameof(CloseOnSelect), true);
    public static readonly StyledProperty<bool> BackspaceRemovesProperty = AvaloniaProperty.Register<Select, bool>(nameof(BackspaceRemoves), true);
    public static readonly StyledProperty<bool> BackspaceClearsInputValueProperty = AvaloniaProperty.Register<Select, bool>(nameof(BackspaceClearsInputValue));
    public static readonly StyledProperty<bool> DeleteRemovesProperty = AvaloniaProperty.Register<Select, bool>(nameof(DeleteRemoves), true);
    public static readonly StyledProperty<bool> EscapeClearsValueProperty = AvaloniaProperty.Register<Select, bool>(nameof(EscapeClearsValue), true);
    public static readonly StyledProperty<bool> FilterOutSelectedProperty = AvaloniaProperty.Register<Select, bool>(nameof(FilterOutSelected), true);
    public static readonly StyledProperty<bool> IgnoreCaseProperty = AvaloniaProperty.Register<Select, bool>(nameof(IgnoreCase), true);
    public static readonly StyledProperty<bool> OnBlurResetsInputProperty = AvaloniaProperty.Register<Select, bool>(nameof(OnBlurResetsInput), true);
    public static readonly StyledProperty<bool> OnCloseResetsInputProperty = AvaloniaProperty.Register<Select, bool>(nameof(OnCloseResetsInput), true);
    public static readonly StyledProperty<bool> OnSelectResetsInputProperty = AvaloniaProperty.Register<Select, bool>(nameof(OnSelectResetsInput), true);
    public static readonly StyledProperty<bool> OpenOnClickProperty = AvaloniaProperty.Register<Select, bool>(nameof(OpenOnClick), true);
    public static readonly StyledProperty<bool> AutoFocusProperty = AvaloniaProperty.Register<Select, bool>(nameof(AutoFocus));

    public static readonly StyledProperty<Func<object, SelectOptionState, object?>?> GetOptionLabelProperty =
        AvaloniaProperty.Register<Select, Func<object, SelectOptionState, object?>?>(nameof(GetOptionLabel));

    public static readonly StyledProperty<Func<object, object?>?> GetValueLabelProperty =
        AvaloniaProperty.Register<Select, Func<object, object?>?>(nameof(GetValueLabel));

    public static readonly StyledProperty<Func<IReadOnlyList<object>, string, IReadOnlyList<object>?, IReadOnlyList<object>>?> FilterOptionsProperty =
        AvaloniaProperty.Register<Select, Func<IReadOnlyList<object>, string, IReadOnlyList<object>?, IReadOnlyList<object>>?>(nameof(FilterOptions));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Select, OverrideCollection?>(nameof(Overrides));

    public static readonly RoutedEvent<SelectChangeEventArgs> ChangeEvent =
        RoutedEvent.Register<Select, SelectChangeEventArgs>(nameof(Change), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<SelectInputChangeEventArgs> InputChangeEvent =
        RoutedEvent.Register<Select, SelectInputChangeEventArgs>(nameof(InputChange), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> OpenEvent =
        RoutedEvent.Register<Select, RoutedEventArgs>(nameof(Open), RoutingStrategies.Bubble);

    public static readonly RoutedEvent<RoutedEventArgs> CloseEvent =
        RoutedEvent.Register<Select, RoutedEventArgs>(nameof(Close), RoutingStrategies.Bubble);

    private readonly Parts _parts;
    private readonly Dictionary<object, SelectOptionContent> _contents = new(ReferenceEqualityComparer.Instance);
    private readonly List<SelectTag> _tags = [];
    private readonly HashSet<INotifyCollectionChanged> _watched = [];
    private readonly ControlTheme _optionProps;
    private ControlledPopover? _popover;
    private Control? _root, _control, _clearIcon;
    private SelectValueContainer? _values;
    private ContentPresenter? _single;
    private SelectInputContainer? _inputContainer;
    private SelectInput? _input;
    private Border? _dropdown;
    private Menu? _menu;
    private TopLevel? _top;
    private OverrideCollection? _appOverrides;
    private bool _overridesWatched;
    private SelectOption? _created;
    private bool _relabel;

    // Base Web's state beyond its two properties, and its instance flags.
    private bool _isFocused, _isPseudoFocused;
    private bool _focusAfterClear, _openAfterFocus, _justSelected;
    private bool _focusOwed;
    private bool _listening;
    private string? _shownText;
    private bool _syncing;
    private IReadOnlyList<object>? _renderedValue;

    public Select()
    {
        _parts = new Parts(this, "Select", [.. TemplateParts, .. MadeParts], new Dictionary<string, string> { [MultiValuePart] = TagPart });
        _optionProps = new ControlTheme(typeof(MenuOptionList))
        {
            Setters = { new Setter(MenuOptionList.GetItemLabelProperty, (Func<object?, object?>)ItemLabel) },
        };
        // select-component.js's onKeyDown is the control container's: the keys of what is focused in it, before the input
        // has them (its default action comes after the handlers), whatever handled them.
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        AddHandler(PointerPressedEvent, OnPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    /// <summary>Base Web's <c>options</c>: any objects, <see cref="SelectOption"/>s and <see cref="SelectGroup"/>s among them.</summary>
    public IEnumerable? Options { get => GetValue(OptionsProperty); set => SetValue(OptionsProperty, value); }

    /// <summary>Base Web's <c>value</c>: the options chosen, always a list (one for a single select). Set by the select (StatefulSelect's).</summary>
    public IReadOnlyList<object>? Value { get => GetValue(ValueProperty); set => SetValue(ValueProperty, value); }

    /// <summary>Base Web's state's <c>isOpen</c>: the dropdown shown. Its first value is <c>startOpen</c>. <c>setDropdownOpen</c> too.</summary>
    public bool IsOpen { get => GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

    /// <summary>Base Web's state's <c>inputValue</c>: the text typed, which filters the options. <c>setInputValue</c> too.</summary>
    public string InputValue { get => GetValue(InputValueProperty); set => SetValue(InputValueProperty, value); }

    /// <summary>Base Web's <c>multi</c>: any number of values, each a tag.</summary>
    public bool Multi { get => GetValue(MultiProperty); set => SetValue(MultiProperty, value); }

    /// <summary>Base Web's <c>searchable</c>: text typed filters the options. True by default.</summary>
    public bool Searchable { get => GetValue(SearchableProperty); set => SetValue(SearchableProperty, value); }

    /// <summary>Base Web's <c>creatable</c>: the text typed is offered as an option when none matches it.</summary>
    public bool Creatable { get => GetValue(CreatableProperty); set => SetValue(CreatableProperty, value); }

    /// <summary>Base Web's <c>clearable</c>: the clear icon, Escape and Backspace clear values. True by default.</summary>
    public bool Clearable { get => GetValue(ClearableProperty); set => SetValue(ClearableProperty, value); }

    /// <summary>Base Web's <c>error</c>.</summary>
    public bool Error { get => GetValue(ErrorProperty); set => SetValue(ErrorProperty, value); }

    /// <summary>Base Web's <c>positive</c>; <see cref="Error"/> wins.</summary>
    public bool Positive { get => GetValue(PositiveProperty); set => SetValue(PositiveProperty, value); }

    /// <summary>Base Web's <c>isLoading</c>: the loading ring in place of the clear icon.</summary>
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    /// <summary>Base Web's <c>required</c>: said to UI Automation.</summary>
    public bool Required { get => GetValue(RequiredProperty); set => SetValue(RequiredProperty, value); }

    /// <summary>Base Web's <c>size</c>.</summary>
    public SelectSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>type</c>: select (an arrow) or search (a magnifying glass, no arrow).</summary>
    public SelectType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Base Web's <c>placeholder</c>; null is <c>Select...</c>.</summary>
    public string? Placeholder { get => GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }

    /// <summary>Base Web's <c>noResultsMsg</c>: the dropdown's empty state, any content; null is the menu's, <c>No results</c> (dropdown.js passes the prop on as it is).</summary>
    public object? NoResultsMsg { get => GetValue(NoResultsMsgProperty); set => SetValue(NoResultsMsgProperty, value); }

    /// <summary>Base Web's <c>maxDropdownHeight</c>, a CSS length: <c>40vh</c> (of the window's height) by default, or pixels.</summary>
    public string MaxDropdownHeight { get => GetValue(MaxDropdownHeightProperty); set => SetValue(MaxDropdownHeightProperty, value); }

    /// <summary>Base Web's <c>closeOnSelect</c>. True by default.</summary>
    public bool CloseOnSelect { get => GetValue(CloseOnSelectProperty); set => SetValue(CloseOnSelectProperty, value); }

    /// <summary>Base Web's <c>backspaceRemoves</c>: Backspace with no text removes the last value. True by default.</summary>
    public bool BackspaceRemoves { get => GetValue(BackspaceRemovesProperty); set => SetValue(BackspaceRemovesProperty, value); }

    /// <summary>Base Web's <c>backspaceClearsInputValue</c>: Backspace's value doesn't come back as text.</summary>
    public bool BackspaceClearsInputValue { get => GetValue(BackspaceClearsInputValueProperty); set => SetValue(BackspaceClearsInputValueProperty, value); }

    /// <summary>Base Web's <c>deleteRemoves</c>: Delete with no text removes the last value. True by default.</summary>
    public bool DeleteRemoves { get => GetValue(DeleteRemovesProperty); set => SetValue(DeleteRemovesProperty, value); }

    /// <summary>Base Web's <c>escapeClearsValue</c>: Escape while closed clears. True by default.</summary>
    public bool EscapeClearsValue { get => GetValue(EscapeClearsValueProperty); set => SetValue(EscapeClearsValueProperty, value); }

    /// <summary>Base Web's <c>filterOutSelected</c>: a multi select's dropdown leaves out what is chosen. True by default.</summary>
    public bool FilterOutSelected { get => GetValue(FilterOutSelectedProperty); set => SetValue(FilterOutSelectedProperty, value); }

    /// <summary>Base Web's <c>ignoreCase</c>, for creatable's match. True by default.</summary>
    public bool IgnoreCase { get => GetValue(IgnoreCaseProperty); set => SetValue(IgnoreCaseProperty, value); }

    /// <summary>Base Web's <c>onBlurResetsInput</c>. True by default.</summary>
    public bool OnBlurResetsInput { get => GetValue(OnBlurResetsInputProperty); set => SetValue(OnBlurResetsInputProperty, value); }

    /// <summary>Base Web's <c>onCloseResetsInput</c>. True by default.</summary>
    public bool OnCloseResetsInput { get => GetValue(OnCloseResetsInputProperty); set => SetValue(OnCloseResetsInputProperty, value); }

    /// <summary>Base Web's <c>onSelectResetsInput</c>. True by default.</summary>
    public bool OnSelectResetsInput { get => GetValue(OnSelectResetsInputProperty); set => SetValue(OnSelectResetsInputProperty, value); }

    /// <summary>Base Web's <c>openOnClick</c>: a click that focuses it opens it. True by default.</summary>
    public bool OpenOnClick { get => GetValue(OpenOnClickProperty); set => SetValue(OpenOnClickProperty, value); }

    /// <summary>Base Web's <c>autoFocus</c>: the input focused once the select is shown (componentDidMount).</summary>
    public bool AutoFocus { get => GetValue(AutoFocusProperty); set => SetValue(AutoFocusProperty, value); }

    /// <summary>Base Web's <c>getOptionLabel</c>: what the dropdown shows for an option, any content. Null is its label, or <c>Create “…”</c>.</summary>
    public Func<object, SelectOptionState, object?>? GetOptionLabel { get => GetValue(GetOptionLabelProperty); set => SetValue(GetOptionLabelProperty, value); }

    /// <summary>Base Web's <c>getValueLabel</c>: what a value shows (the single value, a tag), any content. Null is its label.</summary>
    public Func<object, object?>? GetValueLabel { get => GetValue(GetValueLabelProperty); set => SetValue(GetValueLabelProperty, value); }

    /// <summary>
    /// Base Web's <c>filterOptions</c>: the options, the text typed (trimmed) and the values to leave out (a multi select's,
    /// with <see cref="FilterOutSelected"/>), to the options shown. Null is Base Web's <c>defaultFilterOptions</c>.
    /// </summary>
    public Func<IReadOnlyList<object>, string, IReadOnlyList<object>?, IReadOnlyList<object>>? FilterOptions
    {
        get => GetValue(FilterOptionsProperty);
        set => SetValue(FilterOptionsProperty, value);
    }

    /// <summary>Base Web's <c>overrides</c>, by part (the class remarks list them).</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's <c>onChange</c>: the value changed.</summary>
    public event EventHandler<SelectChangeEventArgs>? Change { add => AddHandler(ChangeEvent, value); remove => RemoveHandler(ChangeEvent, value); }

    /// <summary>Base Web's <c>onInputChange</c>: text typed.</summary>
    public event EventHandler<SelectInputChangeEventArgs>? InputChange { add => AddHandler(InputChangeEvent, value); remove => RemoveHandler(InputChangeEvent, value); }

    /// <summary>Base Web's <c>onOpen</c>.</summary>
    public event EventHandler<RoutedEventArgs>? Open { add => AddHandler(OpenEvent, value); remove => RemoveHandler(OpenEvent, value); }

    /// <summary>Base Web's <c>onClose</c>.</summary>
    public event EventHandler<RoutedEventArgs>? Close { add => AddHandler(CloseEvent, value); remove => RemoveHandler(CloseEvent, value); }

    // ---- controlRef ----

    /// <summary><c>controlRef.setDropdownOpen</c>.</summary>
    public void SetDropdownOpen(bool open) => SetState(s => s with { IsOpen = open });

    /// <summary><c>controlRef.setInputValue</c>.</summary>
    public void SetInputValue(string text) => SetState(s => s with { InputValue = text ?? "" });

    /// <summary><c>controlRef.setInputFocus</c>: the input focused.</summary>
    public void SetInputFocus() => FocusTarget?.Focus(NavigationMethod.Unspecified);

    /// <summary><c>controlRef.setInputBlur</c>: the input's focus let go.</summary>
    public void SetInputBlur()
    {
        if (FocusTarget is { IsFocused: true }) TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
    }

    // ---- For tests and tools ----

    /// <summary>A part by its Base Web name (the first, for a part it makes many of).</summary>
    internal StyledElement? Part(string name) => _parts[name];

    /// <summary>Every element of a part.</summary>
    internal IReadOnlyList<StyledElement> Elements(string name) => _parts.All(name);

    /// <summary>The dropdown's menu while it is open.</summary>
    internal Menu? DropdownMenu => _menu;

    /// <summary>The popover the dropdown is in.</summary>
    internal Popover? DropdownPopover => _popover;

    /// <summary>The tags, in order.</summary>
    internal IReadOnlyList<SelectTag> Tags => _tags;

    /// <summary>Base Web's <c>isFocused</c>.</summary>
    internal bool IsSelectFocused => _isFocused;

    /// <summary>Base Web's <c>isPseudoFocused</c>.</summary>
    internal bool IsPseudoFocused => _isPseudoFocused;

    /// <summary>The options the dropdown lists now: filtered, a creatable one added.</summary>
    internal IReadOnlyList<object> ShownOptions => Filtered();

    /// <summary>The element that takes the focus: the input, or the input's box when it isn't searchable.</summary>
    internal InputElement? FocusTarget => Searchable ? _input : _inputContainer;

    /// <summary>What UI Automation calls it: Base Web's aria-label, <c>Selected …. </c> and the app's name.</summary>
    internal string AccessibleName
    {
        get
        {
            var selected = string.Join(", ", ValueArray().Select(LabelText));
            return (selected.Length > 0 ? "Selected " + selected + ". " : "") + (AutomationProperties.GetName(this) ?? "");
        }
    }

    protected override Type StyleKeyOverride => typeof(Select);

    // ---- The state: select-component.js's, its setState batched as React batches it ----

    private readonly record struct State(bool IsOpen, bool IsFocused, bool IsPseudoFocused, string InputValue);

    private State Current => new(IsOpen, _isFocused, _isPseudoFocused, InputValue ?? "");

    private int _batch;
    private List<(Func<State, State> Update, Action? Then)> _pending = [];
    private bool _committing;

    /// <summary>A handler: its setState calls, and those of a handler it sets off (focus() inside a click), applied together after it.</summary>
    private void Batch(Action handler)
    {
        _batch++;
        try
        {
            handler();
        }
        finally
        {
            _batch--;
        }

        if (_batch == 0) Flush();
    }

    private void SetState(Func<State, State> update, Action? then = null)
    {
        _pending.Add((update, then));
        if (_batch == 0) Flush();
    }

    private void Flush()
    {
        for (var guard = 0; _pending.Count > 0; guard++)
        {
            // An app's handlers that set the state from its own changes, round and round: theirs to fix, said at once.
            if (guard > 100)
            {
                _pending.Clear();
                throw new InvalidOperationException("The select's state does not settle: a handler sets it again from each change.");
            }

            var list = _pending;
            _pending = [];
            var prev = Current;
            var next = prev;
            foreach (var (update, _) in list) next = update(next);
            Commit(prev, next);
            _batch++;
            try
            {
                foreach (var (_, then) in list) then?.Invoke();
            }
            finally
            {
                _batch--;
            }
        }
    }

    /// <summary>The state applied, then componentDidUpdate: onOpen and onClose, and the document's click listener after the first focus.</summary>
    private void Commit(State prev, State next)
    {
        _committing = true;
        try
        {
            _isFocused = next.IsFocused;
            _isPseudoFocused = next.IsPseudoFocused;
            SetCurrentValue(InputValueProperty, next.InputValue);
            SetCurrentValue(IsOpenProperty, next.IsOpen);
        }
        finally
        {
            _committing = false;
        }

        Render();
        if (prev.IsOpen != next.IsOpen) OpenChanged(next.IsOpen);
        // setTimeout(() => document.addEventListener('click', ...), 0): after the click that focused it.
        if (!prev.IsFocused && next.IsFocused && !_listening) Dispatcher.UIThread.Post(Listen);
    }

    private void OpenChanged(bool open)
    {
        RaiseEvent(new RoutedEventArgs(open ? OpenEvent : CloseEvent, this));
        if (FocusTarget is Control target && Avalonia.Automation.Peers.ControlAutomationPeer.FromElement(target) is SelectComboAutomationPeer peer) peer.Changed(open);
    }

    // ---- The template ----

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        Unhook();
        _parts.Attach(e.NameScope);
        _popover = e.NameScope.Find<ControlledPopover>(PopoverPart);
        _root = e.NameScope.Find<Control>(RootPart);
        _control = e.NameScope.Find<Control>(ControlContainerPart);
        _values = e.NameScope.Find<SelectValueContainer>(ValueContainerPart);
        _single = e.NameScope.Find<ContentPresenter>(SingleValuePart);
        _inputContainer = e.NameScope.Find<SelectInputContainer>(InputContainerPart);
        _input = e.NameScope.Find<SelectInput>(InputPart);
        _clearIcon = e.NameScope.Find<Control>(ClearIconPart);
        _tags.Clear();
        _renderedValue = null;
        _shownText = null;
        // A new template is a new popover: the dropdown is made again for it.
        CloseDropdown();
        if (_popover is not null)
        {
            _popover.EscOnBack = true;
            _popover.Esc += OnPopoverEsc;
        }

        // The control container's onClick, a click each time, as a browser's: a second within the double-click time is
        // Avalonia's DoubleTapped. On the root, which holds the container and the search icon over its start (in the
        // container's DOM in Base Web, absolutely placed).
        _root?.AddHandler(InputElement.TappedEvent, OnControlTapped, handledEventsToo: true);
        _root?.AddHandler(InputElement.DoubleTappedEvent, OnControlTapped, handledEventsToo: true);
        _clearIcon?.AddHandler(InputElement.TappedEvent, OnClearTapped, handledEventsToo: true);
        _clearIcon?.AddHandler(InputElement.DoubleTappedEvent, OnClearTapped, handledEventsToo: true);
        if (_root is not null) _root.PropertyChanged += OnRootChanged;
        if (_input is not null)
        {
            _input.Owner = this;
            _input.PropertyChanged += OnInputChanged;
            _input.GotFocus += OnTargetGotFocus;
            _input.LostFocus += OnTargetLostFocus;
        }

        if (_inputContainer is not null)
        {
            _inputContainer.Owner = this;
            _inputContainer.GotFocus += OnTargetGotFocus;
            _inputContainer.LostFocus += OnTargetLostFocus;
        }

        ApplyOverrides();
        Render();
    }

    private void Unhook()
    {
        if (_popover is not null) _popover.Esc -= OnPopoverEsc;
        _root?.RemoveHandler(InputElement.TappedEvent, OnControlTapped);
        _root?.RemoveHandler(InputElement.DoubleTappedEvent, OnControlTapped);
        _clearIcon?.RemoveHandler(InputElement.TappedEvent, OnClearTapped);
        _clearIcon?.RemoveHandler(InputElement.DoubleTappedEvent, OnClearTapped);
        if (_root is not null) _root.PropertyChanged -= OnRootChanged;
        if (_input is not null)
        {
            _input.PropertyChanged -= OnInputChanged;
            _input.GotFocus -= OnTargetGotFocus;
            _input.LostFocus -= OnTargetLostFocus;
        }

        if (_inputContainer is not null)
        {
            _inputContainer.GotFocus -= OnTargetGotFocus;
            _inputContainer.LostFocus -= OnTargetLostFocus;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        if (_isFocused) Listen();
        if (_top is not null)
        {
            _top.AddHandler(PointerPressedEvent, OnTopPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
            _top.PropertyChanged += OnTopChanged;
        }

        // What changed while it was out of the tree: its dropdown held as its state says, its height of this window.
        ApplyMenuOverrides();
        Render();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Unlisten();
        if (_top is not null)
        {
            _top.RemoveHandler(PointerPressedEvent, OnTopPressed);
            _top.PropertyChanged -= OnTopChanged;
        }

        _top = null;
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        Watch();
        WatchOverrides();
        ApplyOverrides();
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromLogicalTree(e);
        foreach (var w in _watched) w.CollectionChanged -= OnCollectionChanged;
        _watched.Clear();
        UnwatchOverrides();
    }

    /// <summary>The app's overrides followed while the select is in the tree: out of it, nothing of the app's holds it.</summary>
    private void WatchOverrides()
    {
        UnwatchOverrides();
        if (!((ILogical)this).IsAttachedToLogicalTree || Overrides is not { } o) return;
        _appOverrides = o;
        o.CollectionChanged += OnOverridesChanged;
        _overridesWatched = true;
    }

    private void UnwatchOverrides()
    {
        if (_overridesWatched && _appOverrides is not null) _appOverrides.CollectionChanged -= OnOverridesChanged;
        _overridesWatched = false;
        _appOverrides = null;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetPseudo(":loaded", true);
        if (IsOpen) OpenDropdown();
        if (AutoFocus) FocusTarget?.Focus(NavigationMethod.Unspecified);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        SetPseudo(":loaded", false);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == OverridesProperty)
        {
            WatchOverrides();
            ApplyOverrides();
        }
        else if (p == IsOpenProperty)
        {
            if (!_committing)
            {
                // The app's (setDropdownOpen, a binding, startOpen): a change of state of its own.
                var was = change.GetOldValue<bool>();
                Render();
                if (was != IsOpen) OpenChanged(IsOpen);
            }
        }
        else if (p == InputValueProperty)
        {
            if (!_committing) Render();
        }
        else if (p == OptionsProperty || p == ValueProperty)
        {
            Watch();
            Render();
        }
        else if (p == IsEffectivelyEnabledProperty)
        {
            if (!IsEffectivelyEnabled && IsOpen) SetState(s => s with { IsOpen = false });
            Render();
        }
        else if (p == MultiProperty || p == SearchableProperty || p == CreatableProperty || p == ClearableProperty || p == ErrorProperty
            || p == PositiveProperty || p == IsLoadingProperty || p == SizeProperty || p == TypeProperty || p == PlaceholderProperty
            || p == NoResultsMsgProperty || p == GetOptionLabelProperty || p == GetValueLabelProperty || p == FilterOptionsProperty
            || p == FilterOutSelectedProperty || p == IgnoreCaseProperty || p == RequiredProperty || p == MaxDropdownHeightProperty)
        {
            if (p == GetOptionLabelProperty)
            {
                // Every option labelled again, as Base Web renders them again with the new function.
                _contents.Clear();
                _relabel = true;
            }
            if (p == GetValueLabelProperty || p == MultiProperty) _renderedValue = null;
            if (p == MaxDropdownHeightProperty) ApplyMenuOverrides();
            Render();
        }
        else if (p == AutomationProperties.NameProperty) Render();
    }

    private void OnRootChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // The dropdown is as wide as the anchor (dropdown.js's $width, the anchor's clientWidth).
        if (e.Property == BoundsProperty && _dropdown is not null) _dropdown.Width = _root!.Bounds.Width;
    }

    private void OnTopChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        // A height in vh follows the window's.
        if (e.Property == TopLevel.ClientSizeProperty && _menu is not null) ApplyMenuOverrides();
    }

    private void SetPseudo(string pseudo, bool on)
    {
        PseudoClasses.Set(pseudo, on);
        _parts.SetState(pseudo, on);
    }

    // ---- Options and values ----

    private void Watch()
    {
        foreach (var w in _watched) w.CollectionChanged -= OnCollectionChanged;
        _watched.Clear();
        if (!((ILogical)this).IsAttachedToLogicalTree) return;
        void Add(object? source)
        {
            if (source is INotifyCollectionChanged c && _watched.Add(c)) c.CollectionChanged += OnCollectionChanged;
        }

        Add(Options);
        foreach (var group in (Options ?? Array.Empty<object>()).OfType<SelectGroup>()) Add(group.Items);
        Add(Value);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        Watch();
        _renderedValue = null;
        Render();
    }

    /// <summary>normalizeOptions: the options flattened, each with its group (Base Web's <c>__optgroup</c>), in order.</summary>
    private List<(object Option, string? Group)> Normalized()
    {
        var all = new List<(object, string?)>();
        foreach (var entry in Options ?? Array.Empty<object>())
        {
            if (entry is SelectGroup g) all.AddRange(g.Items.Select(o => (o, (string?)g.Label)));
            else if (entry is not null) all.Add((entry, null));
        }

        return all;
    }

    /// <summary>Base Web's <c>valueKey</c> of an option: a <see cref="SelectOption"/>'s id (its label if it has none); any other object itself.</summary>
    internal static object? KeyOf(object? option) => option is SelectOption s ? s.Id ?? s.Label : option;

    /// <summary>Base Web's <c>labelKey</c> of an option, as text.</summary>
    internal static string LabelText(object? option) => option is SelectOption s ? s.Label?.ToString() ?? "" : option?.ToString() ?? "";

    /// <summary>An option's label as content: a <see cref="SelectOption"/>'s <c>label</c>, any other object itself.</summary>
    private static object? LabelOf(object option) => option is SelectOption s ? s.Label : option;

    private static bool SameKey(object? a, object? b) => Equals(KeyOf(a), KeyOf(b));

    /// <summary>getValueArray: the value, each expanded to its option (expandValue, by its id as text).</summary>
    private List<object> ValueArray()
    {
        var options = Normalized();
        var list = new List<object>();
        foreach (var v in Value ?? Array.Empty<object>())
        {
            var key = KeyOf(v)?.ToString();
            var found = options.FirstOrDefault(o => KeyOf(o.Option)?.ToString() == key).Option;
            list.Add(found ?? v);
        }

        return list;
    }

    /// <summary>filterOptions: the options for the text typed, the creatable one added when nothing matches it exactly.</summary>
    private List<object> Filtered()
    {
        var filter = (InputValue ?? "").Trim();
        var values = ValueArray();
        var exclude = Multi && FilterOutSelected ? values : null;
        var options = Normalized().Select(o => o.Option).ToList();
        var shown = (FilterOptions is { } f ? f(options, filter, exclude) : DefaultFilterOptions(options, filter, exclude)).ToList();
        bool Differs(object o) => IgnoreCase
            ? !string.Equals(LabelText(o).ToLowerInvariant(), filter.ToLowerInvariant().Trim(), StringComparison.Ordinal)
            : LabelText(o) != filter.Trim();
        if (filter.Length > 0 && Creatable && shown.Concat(values).All(Differs))
        {
            // One creatable option for the text, made again only when the text changes.
            if (_created is null || (string?)_created.Label != filter) _created = new SelectOption { Id = filter, Label = filter, IsCreatable = true };
            shown.Add(_created);
        }

        return shown;
    }

    /// <summary>
    /// Base Web's <c>defaultFilterOptions</c> (<c>select/utils/default-filter-options.js</c>), with select-component.js's
    /// arguments: the excluded values left out, then the options whose value or label has the text anywhere, case ignored.
    /// </summary>
    public static IReadOnlyList<object> DefaultFilterOptions(IReadOnlyList<object> options, string filterValue, IReadOnlyList<object>? exclude)
        => DefaultFilterOptions(options, filterValue, exclude, ignoreCase: true);

    internal static IReadOnlyList<object> DefaultFilterOptions(IReadOnlyList<object> options, string filterValue, IReadOnlyList<object>? exclude, bool ignoreCase)
    {
        var filter = ignoreCase ? filterValue.ToLowerInvariant() : filterValue;
        filter = filter.Trim();
        var excluded = new HashSet<object?>((exclude ?? []).Select(KeyOf));
        var re = new Regex(Regex.Escape(filter), ignoreCase ? RegexOptions.IgnoreCase | RegexOptions.CultureInvariant : RegexOptions.None);
        return options.Where(option =>
        {
            if (excluded.Contains(KeyOf(option))) return false;
            if (filter.Length == 0) return true;
            // isValid: neither undefined, null nor ''. A plain object has no id of its own (Base Web's option[valueKey]).
            var value = option is SelectOption s ? s.Id?.ToString() : null;
            var label = option is SelectOption so ? so.Label?.ToString() : option?.ToString();
            var hasValue = !string.IsNullOrEmpty(value);
            var hasLabel = !string.IsNullOrEmpty(label);
            if (!hasValue && !hasLabel) return false;
            return (hasValue && re.IsMatch(value!)) || (hasLabel && re.IsMatch(label!));
        }).ToList();
    }

    // ---- Rendering: select-component.js's render ----

    private bool ShouldShowPlaceholder(int values) => !((InputValue ?? "").Length > 0 || values > 0);

    private bool ShouldShowValue => (InputValue ?? "").Length == 0;

    private object? ValueLabel(object option) => GetValueLabel is { } f ? f(option) : LabelOf(option);

    private void Show(Control? element, bool shown) => element?.SetValue(IsVisibleProperty, shown, BindingPriority.Template);

    private void Render()
    {
        var values = ValueArray();
        var size = Size switch { SelectSize.Mini => "mini", SelectSize.Compact => "compact", SelectSize.Large => "large", _ => "default" };
        foreach (var s in new[] { "mini", "compact", "default", "large" }) SetPseudo(":size-" + s, s == size);
        SetPseudo(":is-focused", _isFocused);
        SetPseudo(":is-pseudo-focused", _isPseudoFocused);
        SetPseudo(":is-open", IsOpen);
        SetPseudo(":error", Error);
        SetPseudo(":positive", Positive);
        SetPseudo(":multi", Multi);
        SetPseudo(":has-value", values.Count > 0);
        SetPseudo(":searchable", Searchable);
        SetPseudo(":type-search", Type == SelectType.Search);
        SetPseudo(":is-loading", IsLoading);
        SetPseudo(":clearable", Clearable);

        // The values: a tag for each (multi), or the single value while there is no text.
        RenderValues(values);
        Show(_input, Searchable);
        if (_input is not null && _shownText != (InputValue ?? ""))
        {
            // React sets a controlled input's value when its state's changes, and only then.
            _shownText = InputValue ?? "";
            _syncing = true;
            try
            {
                _input.Text = _shownText;
                _input.CaretIndex = _shownText.Length;
            }
            finally
            {
                _syncing = false;
            }
        }

        if (_inputContainer is not null)
        {
            _inputContainer.SetValue(FocusableProperty, !Searchable, BindingPriority.Template);
            _inputContainer.SetValue(IsTabStopProperty, !Searchable, BindingPriority.Template);
        }

        if (_parts[PlaceholderPart] is TextBlock placeholder)
        {
            placeholder.Text = Placeholder ?? PlaceholderDefault;
            Show(placeholder, ShouldShowPlaceholder(values.Count));
        }

        Show(_parts[LoadingIndicatorPart] as Control, IsLoading);
        var entered = values.Count > 0 || (InputValue ?? "").Length > 0;
        Show(_clearIcon, Clearable && IsEffectivelyEnabled && !IsLoading && entered);
        if (_clearIcon is not null) AutomationProperties.SetName(_clearIcon, Multi ? "Clear all" : "Clear value");
        Show(_parts[SelectArrowPart] as Control, Type == SelectType.Select);
        Show(_parts[SearchIconContainerPart] as Control, Type == SelectType.Search);
        AutomationProperties.SetIsRequiredForForm(this, Required);
        _popover?.Hold(IsOpen && IsEffectivelyEnabled && _top is not null);
        if (IsOpen && IsEffectivelyEnabled) OpenDropdown();
        else CloseDropdown();
    }

    private void RenderValues(List<object> values)
    {
        if (_values is null) return;
        var same = _renderedValue is not null && _renderedValue.Count == values.Count && _renderedValue.Zip(values).All(x => ReferenceEquals(x.First, x.Second));
        if (_single is not null)
        {
            var single = !Multi && values.Count > 0 && ShouldShowValue;
            if (!same || _single.Content is null) _single.Content = !Multi && values.Count > 0 ? ValueLabel(values[0]) : null;
            Show(_single, single);
        }

        if (same && _tags.Count == (Multi ? values.Count : 0))
        {
            foreach (var t in _tags) t.IsEnabled = IsEffectivelyEnabled && (t.Value is not SelectOption { ClearableValue: false });
            return;
        }

        _renderedValue = values;
        foreach (var t in _tags) _values.Children.Remove(t);
        _tags.Clear();
        if (Multi)
        {
            var at = 0;
            foreach (var v in values)
            {
                var value = v;
                // multi-value.js: the tag disabled when the select is, or the value isn't clearable.
                var tag = new SelectTag
                {
                    Content = ValueLabel(value),
                    Value = value,
                    Remove = () => Batch(() => RemoveValue(value)),
                    IsEnabled = value is not SelectOption { ClearableValue: false },
                };
                _tags.Add(tag);
                _values.Children.Insert(at++, tag);
            }
        }

        _parts.Place((TagPart, _tags));
    }

    // ---- The dropdown: dropdown.js ----

    private void OpenDropdown()
    {
        if (_popover is null) return;
        if (_menu is null)
        {
            _contents.Clear();
            // A new menu each time it opens, as Base Web mounts the dropdown: its initialState the first value's index.
            _menu = new Menu
            {
                TypeAhead = false,
                ForceHighlight = true,
                KeyboardControlNode = this,
                IsMenuFocused = true,
            };
            _menu.ItemSelect += OnItemSelect;
            _dropdown = new Border { Child = _menu };
            if (_root is not null) _dropdown.Width = _root.Bounds.Width;
            ApplyMenuOverrides();
            var first = Grouped(Filtered());
            _menu.HighlightedIndex = HighlightedIndex(Flat(first));
            _menu.ItemsSource = first;
            _menu.NoResultsMsg = NoResultsMsg;
            _menu.TemplateApplied += OnMenuTemplated;
            _parts.Place((DropdownContainerPart, [_dropdown]), (StatefulMenuPart, [_menu]));
            _popover.Content = _dropdown;
            return;
        }

        var shown = Filtered();
        var grouped = Grouped(shown);
        // The labels of options no longer shown let go.
        foreach (var gone in _contents.Keys.Where(k => !shown.Contains(k, ReferenceEqualityComparer.Instance)).ToList()) _contents.Remove(gone);
        if (_relabel || _menu.ItemsSource is not IList<object> old || !old.SequenceEqual(grouped, ItemComparer.Instance))
        {
            _relabel = false;
            _menu.ItemsSource = grouped;
            _parts.Place((OptionContentPart, [.. _contents.Values]));
        }
        _menu.NoResultsMsg = NoResultsMsg;
        UpdateContents();
    }

    private void OnMenuTemplated(object? sender, TemplateAppliedEventArgs e)
    {
        // The list never takes the focus from the input: a press on it leaves it where it is (a browser's, given back to
        // the input by selectValue's focus(); docs/controls/Select).
        if (_menu?.List is { } list)
        {
            list.SetValue(FocusableProperty, false, BindingPriority.Template);
            list.SetValue(IsTabStopProperty, false, BindingPriority.Template);
            AutomationProperties.SetName(list, AutomationProperties.GetName(this) is { Length: > 0 } n ? n : "Menu");
        }
    }

    private void CloseDropdown()
    {
        if (_menu is null) return;
        _menu.ItemSelect -= OnItemSelect;
        _menu.TemplateApplied -= OnMenuTemplated;
        _menu = null;
        _dropdown = null;
        // The popover keeps showing its content as it closes (its exit); a new menu comes with the next opening.
        _parts.Place((DropdownContainerPart, []), (StatefulMenuPart, []), (OptionContentPart, []));
        _contents.Clear();
    }

    /// <summary>The menu's items in its order: the ungrouped, then each group's.</summary>
    private static List<object> Flat(List<object> grouped) => [.. grouped.SelectMany(g => g is MenuGroup m ? m.Items : [g])];

    /// <summary>
    /// getHighlightedIndex: the first value's index among the options shown, or the first; in the menu's order, which is
    /// Base Web's whenever its ungrouped options come first, as its <c>__ungrouped</c> key conventionally does.
    /// </summary>
    private int HighlightedIndex(List<object> options)
    {
        var values = ValueArray();
        if (values.Count == 0) return 0;
        var i = options.FindIndex(o => SameKey(o, values[0]));
        return i == -1 ? 0 : i;
    }

    /// <summary>groupOptions: the ungrouped options first, then each group in the order its first option came.</summary>
    private List<object> Grouped(List<object> shown)
    {
        var groupOf = Normalized().GroupBy(o => o.Option, ReferenceEqualityComparer.Instance).ToDictionary(g => g.Key!, g => g.First().Group, ReferenceEqualityComparer.Instance);
        var loose = new List<object>();
        var groups = new List<MenuGroup>();
        foreach (var o in shown)
        {
            var g = groupOf.TryGetValue(o, out var name) ? name : null;
            if (g is null) loose.Add(o);
            else
            {
                var group = groups.FirstOrDefault(x => x.Label == g);
                if (group is null) groups.Add(group = new MenuGroup { Label = g });
                group.Items.Add(o);
            }
        }

        return [.. loose, .. groups];
    }

    /// <summary>dropdown.js's getItemLabel: the option's content, OptionContent round its label.</summary>
    private object? ItemLabel(object? item)
    {
        if (item is null) return null;
        if (!_contents.TryGetValue(item, out var content))
        {
            var option = item;
            content = new SelectOptionContent
            {
                Option = option,
                Label = state => GetOptionLabel is { } f ? f(option, state)
                    : option is SelectOption { IsCreatable: true } ? CreateLabel + " “" + LabelText(option) + "”" : LabelOf(option),
            };
            _contents[item] = content;
            _parts.Place((OptionContentPart, [.. _contents.Values]));
        }

        var values = ValueArray();
        content.SetState(values.Any(v => SameKey(v, item)), item is MenuItemData { Disabled: true });
        AutomationProperties.SetName(content, content.Text);
        return content;
    }

    private void UpdateContents()
    {
        var values = ValueArray();
        foreach (var (item, content) in _contents) content.SetState(values.Any(v => SameKey(v, item)), item is MenuItemData { Disabled: true });
    }

    private void OnItemSelect(object? sender, MenuItemSelectEventArgs e) => Batch(() => SelectValue(e.Item));

    private sealed class ItemComparer : IEqualityComparer<object>
    {
        public static readonly ItemComparer Instance = new();

        public new bool Equals(object? x, object? y) =>
            x is MenuGroup a && y is MenuGroup b ? a.Label == b.Label && a.Items.SequenceEqual(b.Items, this) : ReferenceEquals(x, y);

        public int GetHashCode(object obj) => obj is MenuGroup g ? g.Label.GetHashCode() : RuntimeHelpers.GetHashCode(obj);
    }

    // ---- Overrides ----

    private void OnOverridesChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyOverrides();

    private Override? Find(string part) => Overrides?.FirstOrDefault(o => o.Part == part);

    /// <summary>
    /// The select's own overrides, and its Popover's: select-component.js's props for it (placement bottom, no margin, no
    /// auto focus or focus lock, accessibility none; its dismissals the select's), as a style an app's Popover override's
    /// props are merged over, as Base Web spreads them after its own.
    /// </summary>
    private void ApplyOverrides()
    {
        var all = new OverrideCollection();
        foreach (var o in Overrides ?? [])
        {
            if (o.Part is PopoverPart or DropdownPart or DropdownOptionPart or DropdownListItemPart) continue;
            all.Add(o);
        }

        var app = Find(PopoverPart);
        all.Add(new Override { Part = PopoverPart, Component = app?.Component, Style = Combine(PopoverStyle, app?.Style), Props = app?.Props });
        _parts.SetOverrides(all);
        ApplyMenuOverrides();
    }

    private static readonly ControlTheme PopoverStyle = new(typeof(Popover))
    {
        Setters =
        {
            new Setter(Popover.PlacementProperty, PopoverPlacement.Bottom),
            new Setter(Popover.PopoverMarginProperty, 0.0),
            new Setter(Popover.AutoFocusProperty, false),
            new Setter(Popover.FocusLockProperty, false),
            new Setter(Popover.AccessibilityTypeProperty, PopoverAccessibilityType.None),
            // Controlled: Base Web gives it no onClickOutside; its onEsc closes the menu (the select's own handler).
            new Setter(Popover.DismissOnClickOutsideProperty, false),
            new Setter(Popover.DismissOnEscProperty, false),
        },
    };

    private static readonly ConditionalWeakTable<ControlTheme, Dictionary<ControlTheme, ControlTheme>> Combined = new();

    /// <summary>Two themes as one: the first's setters, then the second's (which win), its state styles after.</summary>
    private static ControlTheme Combine(ControlTheme first, ControlTheme? second)
    {
        if (second is null) return first;
        var cache = Combined.GetOrCreateValue(first);
        lock (cache)
        {
            if (cache.TryGetValue(second, out var both)) return both;
            // Its base the app's, which Parts then uses as it is, as it uses an app's theme with a base of its own.
            both = new ControlTheme(second.TargetType ?? first.TargetType!) { BasedOn = second.BasedOn };
            // One setter for a property: the second's wins.
            var theirs = second.Setters.OfType<Setter>().Select(s => s.Property).ToHashSet();
            foreach (var s in first.Setters) if (s is not Setter { Property: { } p } || !theirs.Contains(p)) both.Setters.Add(s);
            foreach (var s in second.Setters) both.Setters.Add(s);
            foreach (var c in first.Children) both.Children.Add(c);
            foreach (var c in second.Children) both.Children.Add(c);
            cache[second] = both;
            return both;
        }
    }

    private (double Height, ControlTheme Theme)? _listStyle;

    /// <summary>
    /// The menu's overrides, dropdown.js's: its List with the dropdown's max height under an app's Dropdown, its Option with
    /// getItemLabel under an app's DropdownOption, its ListItem an app's DropdownListItem.
    /// </summary>
    private void ApplyMenuOverrides()
    {
        if (_menu is null) return;
        var height = DropdownMaxHeight();
        // One theme for the height now, made again when it changes (a window resized), the last let go.
        if (_listStyle is not { } current || current.Height != height)
        {
            _listStyle = (height, new ControlTheme(typeof(MenuList)) { Setters = { new Setter(MaxHeightProperty, height) } });
        }

        var list = _listStyle.Value.Theme;

        var dropdown = Find(DropdownPart);
        var option = Find(DropdownOptionPart);
        var item = Find(DropdownListItemPart);
        var overrides = new OverrideCollection
        {
            new Override { Part = Menu.ListPart, Component = dropdown?.Component, Style = Combine(list, dropdown?.Style), Props = dropdown?.Props },
            new Override { Part = Menu.OptionPart, Component = option?.Component, Style = option?.Style, Props = Combine(_optionProps, option?.Props) },
        };
        if (item is not null) overrides.Add(new Override { Part = MenuOptionList.ListItemPart, Component = item.Component, Style = item.Style, Props = item.Props });
        _menu.Overrides = overrides;
    }

    /// <summary>maxDropdownHeight in pixels: a CSS length in vh (of the window's height) or px; anything else none.</summary>
    private double DropdownMaxHeight()
    {
        var v = (MaxDropdownHeight ?? "").Trim();
        if (v.EndsWith("vh", StringComparison.Ordinal) && double.TryParse(v[..^2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var vh))
            return (_top?.ClientSize.Height ?? 0) * vh / 100;
        if (v.EndsWith("px", StringComparison.Ordinal)) v = v[..^2];
        return double.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var px) ? px : double.PositiveInfinity;
    }

    // ---- The handlers: select-component.js's ----

    /// <summary>A press: where a browser's focus would go. Anywhere in the control but the input, the input's focus is let go (to the page), as a mousedown does.</summary>
    private void OnPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;
        _focusOwed = source is not null && !Within(source, _input) && !_tags.Any(t => Within(source, t));
    }

    private static bool Within(Visual? source, Visual? container) => source is not null && container is not null && (source == container || container.IsVisualAncestorOf(source));

    private bool InAnchor(Visual? v) => Within(v, _root);

    private bool InDropdown(Visual? v) => Within(v, _dropdown) || (_popover?.Part(Popover.BodyPart) is Visual body && Within(v, body));

    /// <summary>focus(): the input focused, its focus event too where a browser's press had let it go.</summary>
    private void FocusInput()
    {
        if (FocusTarget is not { } target) return;
        if (target.IsFocused)
        {
            if (_focusOwed)
            {
                _focusOwed = false;
                HandleInputFocus();
            }

            return;
        }

        _focusOwed = false;
        target.Focus(NavigationMethod.Pointer);
    }

    /// <summary>A click or a tap on the control (its onClick; a tap is a phone's click, a drag none).</summary>
    private void OnControlTapped(object? sender, TappedEventArgs e)
    {
        var source = e.Source as Visual;
        if (_tags.Any(t => t.Clicked))
        {
            // A tag's action: its click went no further (stopPropagation).
            foreach (var t in _tags) t.Clicked = false;
            return;
        }

        Batch(() => HandleClick(source));
        _focusOwed = false;
    }

    private void HandleClick(Visual? target)
    {
        if (!IsEffectivelyEnabled) return;
        if (Searchable && Within(target, _input))
        {
            if (!_isFocused)
            {
                _openAfterFocus = OpenOnClick;
                FocusInput();
            }

            if (!Current.IsOpen) SetState(s => s with { IsOpen = true, IsFocused = true, IsPseudoFocused = false });
            return;
        }

        // isInteractive: a role="button" (the clear icon, a tag) takes its own click.
        if (Within(target, _clearIcon) || _tags.Any(t => Within(target, t))) return;
        if (!Searchable)
        {
            FocusInput();
            if (Current.IsOpen) SetState(s => s with { IsOpen = false, IsFocused = false });
            else SetState(s => s with { IsOpen = true, IsFocused = true });
            return;
        }

        if (_isFocused)
        {
            FocusInput();
            // this.input.value = '': React writes the state's text back as it renders again, so the caret is at its end
            // (what the comment there wants: a click places it at the end rather than selecting the text).
            if (_input is not null) _input.ClearSelection();
            if (_input is not null) _input.CaretIndex = (_input.Text ?? "").Length;

            var afterClear = _focusAfterClear;
            SetState(s => s with { IsOpen = !afterClear && !s.IsOpen, IsPseudoFocused = false });
            _focusAfterClear = false;
        }
        else
        {
            _focusAfterClear = false;
            _openAfterFocus = OpenOnClick;
            FocusInput();
        }
    }

    private void OnTargetGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source != sender) return;
        Batch(HandleInputFocus);
    }

    private void HandleInputFocus()
    {
        if (!IsEffectivelyEnabled) return;
        var toOpen = Current.IsOpen || _openAfterFocus;
        toOpen = !_focusAfterClear && toOpen;
        SetState(s => s with { IsFocused = true, IsOpen = toOpen });
        _focusAfterClear = false;
        _openAfterFocus = false;
    }

    /// <summary>handleBlur on the root's blur: focus gone to the anchor or the dropdown, or to the page (no related target: the input is in the anchor), is let go.</summary>
    private void OnTargetLostFocus(object? sender, FocusChangedEventArgs e)
    {
        if (e.Source != sender) return;
        var next = e.NewFocusedElement as Visual;
        if (next is null or TopLevel) return;
        if (InAnchor(next) || InDropdown(next)) return;
        Batch(Blur);
    }

    private void Blur()
    {
        var reset = OnBlurResetsInput;
        SetState(s => s with { IsFocused = false, IsOpen = false, IsPseudoFocused = false, InputValue = reset ? "" : s.InputValue });
    }

    // The document's click listener, from the first focus.
    private void Listen()
    {
        if (_listening || _top is null) return;
        _listening = true;
        _top.AddHandler(PointerReleasedEvent, OnTopReleased, RoutingStrategies.Bubble, handledEventsToo: true);
    }

    private void Unlisten()
    {
        if (!_listening) return;
        _listening = false;
        _top?.RemoveHandler(PointerReleasedEvent, OnTopReleased);
    }

    private (IPointer Pointer, Point At, Visual? Source, bool InAnchor, bool InDropdown, bool OnTagAction)? _down;

    /// <summary>
    /// A finger down outside: handleTouchOutside (touchstart, while open). Any press: the click it may become, where it was
    /// decided now, while what was pressed is in the tree (a tag's action takes its tag away before the release).
    /// </summary>
    private void OnTopPressed(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Visual;
        _down = (e.Pointer, e.GetPosition(_top), source, InAnchor(source), InDropdown(source),
            _tags.Any(t => t.Part(SelectTag.ActionPart) is Visual action && Within(source, action)));
        if (e.Pointer.Type == PointerType.Touch && IsOpen && !InDropdown(source) && !InAnchor(source)) Batch(CloseMenu);
    }

    /// <summary>A click on the page (a press and its release, a finger's within the tap distance): handleClickOutside.</summary>
    private void OnTopReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_down is not { } down || down.Pointer != e.Pointer || _top is null) return;
        _down = null;
        if (e.InitialPressMouseButton != MouseButton.Left && e.Pointer.Type == PointerType.Mouse) return;
        var tap = Application.Current?.PlatformSettings?.GetTapSize(e.Pointer.Type) ?? new Size(10, 10);
        var at = e.GetPosition(_top);
        if (e.Pointer.Type != PointerType.Mouse && (Math.Abs(at.X - down.At.X) > tap.Width / 2 || Math.Abs(at.Y - down.At.Y) > tap.Height / 2)) return;
        // A tag's action stops its click's propagation: the document never hears it.
        if (down.OnTagAction && e.Handled) return;
        Batch(() => HandleClickOutside(down.InAnchor, down.InDropdown));
    }

    private void HandleClickOutside(bool inAnchor, bool inDropdown)
    {
        if (_justSelected)
        {
            _justSelected = false;
            return;
        }

        if (inDropdown) return;
        if ((_isFocused || _isPseudoFocused) && !inAnchor)
        {
            // A browser's press on the page has taken the focus from the input.
            if (FocusTarget is { IsFocused: true }) TopLevel.GetTopLevel(this)?.FocusManager?.Focus(null);
            Blur();
        }
    }

    private void OnInputChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != TextBox.TextProperty || _syncing) return;
        var text = _input?.Text ?? "";
        _shownText = text;
        Batch(() =>
        {
            SetState(s => s with { InputValue = text, IsOpen = true, IsPseudoFocused = false });
            RaiseEvent(new SelectInputChangeEventArgs(InputChangeEvent, this, text));
        });
    }

    private void OnClearTapped(object? sender, TappedEventArgs e) => Batch(ClearValue);

    private void OnPopoverEsc(object? sender, RoutedEventArgs e) => Batch(CloseMenu);

    /// <summary>handleKeyDown, by the key's keyCode, for the keys of the input (or its box): a tag's are its own.</summary>
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!IsEffectivelyEnabled || e.Source is not Visual source || (!Within(source, _input) && !Within(source, _inputContainer))) return;
        Batch(() => HandleKeyDown(e));
    }

    private void HandleKeyDown(KeyEventArgs e)
    {
        var state = Current;
        var shift = e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        void Prevent() => e.Handled = true;
        void Open()
        {
            if (!Current.IsOpen) SetState(s => s with { IsOpen = true });
        }

        switch (e.Key)
        {
            case Key.Back:
                if (state.InputValue.Length == 0 && Clearable && BackspaceRemoves)
                {
                    Prevent();
                    BackspaceValue();
                }

                break;
            case Key.Tab:
                var keep = !OnCloseResetsInput || !OnBlurResetsInput;
                SetState(s => s with { IsPseudoFocused = false, IsFocused = false, IsOpen = false, InputValue = keep ? s.InputValue : "" });
                break;
            case Key.Escape:
                if (!state.IsOpen && Clearable && EscapeClearsValue)
                {
                    ClearValue();
                    SetState(s => s with { IsFocused = false, IsPseudoFocused = false });
                }

                break;
            case Key.Space:
                if (Searchable) break;
                Prevent();
                Open();
                break;
            case Key.Up or Key.Down or Key.PageUp or Key.PageDown:
                Prevent();
                Open();
                break;
            case Key.End or Key.Home:
                if (shift) break;
                Prevent();
                Open();
                break;
            case Key.Delete:
                if (state.InputValue.Length == 0 && DeleteRemoves)
                {
                    Prevent();
                    PopValue();
                }

                break;
        }
    }

    private void SelectValue(object item)
    {
        if (item is MenuItemData { Disabled: true }) return;
        _justSelected = true;
        var updated = OnSelectResetsInput ? "" : Current.InputValue;
        var close = CloseOnSelect;
        if (Multi)
        {
            SetState(s => s with { InputValue = updated, IsOpen = !close }, () =>
            {
                if ((Value ?? []).Any(v => SameKey(v, item))) RemoveValue(item);
                else AddValue(item);
            });
        }
        else
        {
            FocusInput();
            SetState(s => s with { InputValue = updated, IsOpen = !close, IsFocused = true, IsPseudoFocused = false },
                () => SetValue([item], item, SelectChangeType.Select));
        }
    }

    private void AddValue(object item) => SetValue([.. Value ?? [], item], item, SelectChangeType.Select);

    private object? PopValue()
    {
        var values = (Value ?? []).ToList();
        if (values.Count == 0) return null;
        if (values[^1] is SelectOption { ClearableValue: false }) return null;
        var item = values[^1];
        values.RemoveAt(values.Count - 1);
        SetValue(values, item, SelectChangeType.Remove);
        return item;
    }

    private void BackspaceValue()
    {
        if (PopValue() is not { } item) return;
        var label = ValueLabel(item);
        if (!BackspaceClearsInputValue && label is string text)
        {
            var remaining = text.Length > 0 ? text[..^1] : "";
            SetState(s => s with { InputValue = remaining, IsOpen = true });
        }
    }

    private void RemoveValue(object item)
    {
        SetValue([.. (Value ?? []).Where(v => !SameKey(v, item))], item, SelectChangeType.Remove);
        FocusInput();
    }

    private void ClearValue()
    {
        if (Value is { } value)
        {
            var reset = value.Where(v => v is SelectOption { ClearableValue: false }).ToList();
            SetValue(reset, null, SelectChangeType.Clear);
        }

        SetState(s => s with { InputValue = "", IsOpen = false });
        FocusInput();
        _focusAfterClear = true;
    }

    private void CloseMenu()
    {
        var pseudo = _isFocused && !Multi;
        if (OnCloseResetsInput) SetState(s => s with { InputValue = "", IsOpen = false, IsPseudoFocused = pseudo });
        else SetState(s => s with { IsOpen = false, IsPseudoFocused = pseudo });
    }

    /// <summary>setValue: onChange, and the value StatefulSelect keeps.</summary>
    private void SetValue(IReadOnlyList<object> value, object? option, SelectChangeType type)
    {
        RaiseEvent(new SelectChangeEventArgs(ChangeEvent, this, value, option, type));
        SetCurrentValue(ValueProperty, value);
    }
}
