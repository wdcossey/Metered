using Avalonia.Collections;
using Avalonia.Metadata;

namespace Metered.Controls;

/// <summary>
/// An item of a <see cref="Menu"/>, as Base Web's menu reads one (`menu.js`, `option-list.js`, `stateful-container.js`):
/// its <c>label</c>, <c>disabled</c>, <c>divider</c> and <c>href</c>.
/// </summary>
/// <remarks>
/// Base Web's items are any objects; a menu reads these four keys of them. A menu here takes any object as an item too: one
/// that isn't a <see cref="MenuItemData"/> is its own label (shown as content, by its data template if it has one), is
/// never disabled, a divider or a link, and a string is its label for type-ahead (docs/controls/Menu).
/// </remarks>
public class MenuItemData
{
    /// <summary>Base Web's <c>label</c>: what the option shows (any content), and, a string, what type-ahead matches.</summary>
    public object? Label { get; set; }

    /// <summary>Base Web's <c>disabled</c>: the option can be highlighted by the keys, but not chosen.</summary>
    public bool Disabled { get; set; }

    /// <summary>Base Web's <c>divider: true</c>: a line in place of an option, which still takes an index.</summary>
    public bool Divider { get; set; }

    /// <summary>Base Web's <c>href</c>: the option's label is a link, opened when it is chosen.</summary>
    public string? Href { get; set; }

    public override string ToString() => Label?.ToString() ?? (Divider ? "divider" : "");
}

/// <summary>
/// A group of a <see cref="Menu"/>'s items under a header: one key of Base Web's grouped items
/// (<c>{ __ungrouped: [...], Blueish: [...] }</c>), its <see cref="Label"/> the key. Items outside any group are Base Web's
/// <c>__ungrouped</c>, which has no header.
/// </summary>
public class MenuGroup
{
    /// <summary>The group's key, its header's text.</summary>
    public string Label { get; set; } = "";

    /// <summary>The group's items.</summary>
    [Content]
    public AvaloniaList<object> Items { get; } = [];

    public override string ToString() => Label;
}

/// <summary>Base Web's <c>getProfileItemLabels</c>' result: a profile option's title, subtitle and body, each shown when it isn't empty.</summary>
public sealed record MenuProfileLabels(string? Title = null, string? Subtitle = null, string? Body = null);

/// <summary>A menu's state (Base Web's <c>StatefulContainerState</c>): the highlighted option's index, and whether the menu is focused.</summary>
public readonly record struct MenuState(int HighlightedIndex, bool IsFocused);

/// <summary>Base Web's <c>STATE_CHANGE_TYPES</c>: what changed the state, as a menu's <c>StateReducer</c> is told.</summary>
public enum MenuStateChangeType
{
    Click,
    MoveUp,
    MoveDown,
    MouseEnter,
    MouseLeave,
    Focus,
    Reset,
    Character,
    Enter,
}

/// <summary>Base Web's <c>OPTION_LIST_SIZE</c>: an option's padding and type.</summary>
public enum MenuOptionSize
{
    Default,
    Compact,
}
