using Avalonia.Collections;
using Avalonia.Interactivity;
using Avalonia.Metadata;

namespace Metered.Controls;

/// <summary>
/// An option of a <see cref="Select"/>, as Base Web's select reads one (<c>select-component.js</c>, <c>dropdown.js</c>):
/// its <c>id</c> (the <c>valueKey</c>), <c>label</c> (the <c>labelKey</c>), <c>disabled</c> and <c>clearableValue</c>. It is a
/// <see cref="MenuItemData"/>, as the dropdown's menu shows it.
/// </summary>
/// <remarks>
/// Base Web's options are any objects, read by <c>valueKey</c> and <c>labelKey</c>. A select here takes any object as an
/// option too: one that isn't a <see cref="SelectOption"/> is its own label (shown as content), its own value, and is never
/// disabled (docs/controls/Select).
/// </remarks>
public class SelectOption : MenuItemData
{
    public SelectOption()
    {
    }

    public SelectOption(object id, object? label = null)
    {
        Id = id;
        Label = label ?? id;
    }

    /// <summary>Base Web's <c>id</c>, the default <c>valueKey</c>: what tells options apart. Null is its label.</summary>
    public object? Id { get; set; }

    /// <summary>
    /// Base Web's <c>clearableValue</c>: false keeps it in a multi select's value whatever clears or removes values (the
    /// clear icon, Backspace, Delete).
    /// </summary>
    public bool ClearableValue { get; set; } = true;

    /// <summary>Base Web's <c>isCreatable</c>: the option a creatable select offers for the text typed.</summary>
    public bool IsCreatable { get; set; }

    public override string ToString() => Label?.ToString() ?? Id?.ToString() ?? "";
}

/// <summary>
/// A group of a <see cref="Select"/>'s options under a header: one key of Base Web's grouped options
/// (<c>{ __ungrouped: [...], Blueish: [...] }</c>), its <see cref="Label"/> the key. Options outside any group are
/// <c>__ungrouped</c>, which has no header.
/// </summary>
public class SelectGroup
{
    /// <summary>The group's key, its header's text.</summary>
    public string Label { get; set; } = "";

    /// <summary>The group's options.</summary>
    [Content]
    public AvaloniaList<object> Items { get; } = [];

    public override string ToString() => Label;
}

/// <summary>Base Web's <c>STATE_CHANGE_TYPE</c>: why the value changed.</summary>
public enum SelectChangeType
{
    /// <summary>An option chosen (or, multi, added).</summary>
    Select,

    /// <summary>A value removed: chosen again in a multi select, its tag's action, Backspace or Delete.</summary>
    Remove,

    /// <summary>The clear icon, or Escape while closed.</summary>
    Clear,
}

/// <summary>Base Web's select <c>SIZE</c> (the input's): the padding, the font and the icons' sizes.</summary>
public enum SelectSize
{
    Mini,
    Compact,

    /// <summary>Base Web's default.</summary>
    Default,

    Large,
}

/// <summary>Base Web's <c>TYPE</c>: a select's arrow, or a search's magnifying glass at its start and no arrow.</summary>
public enum SelectType
{
    /// <summary>Base Web's default.</summary>
    Select,

    Search,
}

/// <summary>Base Web's <c>onChange</c>'s argument: the new value, the option that changed it (none for a clear), and why.</summary>
public class SelectChangeEventArgs(RoutedEvent routedEvent, object source, IReadOnlyList<object> value, object? option, SelectChangeType type)
    : RoutedEventArgs(routedEvent, source)
{
    /// <summary>The value: every option chosen, in order.</summary>
    public IReadOnlyList<object> Value { get; } = value;

    /// <summary>The option chosen or removed; null for a clear.</summary>
    public object? Option { get; } = option;

    /// <summary>Why it changed.</summary>
    public SelectChangeType Type { get; } = type;
}

/// <summary>Base Web's <c>onInputChange</c>'s argument: the text typed.</summary>
public class SelectInputChangeEventArgs(RoutedEvent routedEvent, object source, string text) : RoutedEventArgs(routedEvent, source)
{
    /// <summary>The input's text.</summary>
    public string Text { get; } = text;
}

/// <summary>
/// Base Web's <c>getOptionLabel</c>'s <c>optionState</c>: what <c>dropdown.js</c> knows of an option as it labels it.
/// </summary>
public readonly record struct SelectOptionState(bool Selected, bool Disabled, bool IsHighlighted);
