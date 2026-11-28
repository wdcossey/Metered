namespace Metered.Controls;

/// <summary>Base Web's tabs-motion &lt;c&gt;FILL&lt;/c&gt;: how the tabs fill the tab list.</summary>
public enum TabsFill
{
    /// <summary>Each tab as wide as its title needs; tabs that don't fit scroll (Base Web's default).</summary>
    Intrinsic,

    /// <summary>The tab list's room shared out equally between the tabs.</summary>
    Fixed,
}

/// <summary>Base Web's tabs-motion &lt;c&gt;STATE_CHANGE_TYPE&lt;/c&gt;: what changed a &lt;see cref="Tabs"/&gt;' state.</summary>
public enum TabsStateChangeType
{
    /// <summary>A tab chosen: a click, a tap, Enter or Space, or an arrow key that activates on focus.</summary>
    Change,
}

/// <summary>Base Web's StatefulTabs' state: the active tab's key.</summary>
public sealed record TabsState(string? ActiveKey);

/// <summary>Base Web's StatefulTabs' action: what changed the state and the key it gives.</summary>
public sealed record TabsAction(TabsStateChangeType Type, string? Payload);
