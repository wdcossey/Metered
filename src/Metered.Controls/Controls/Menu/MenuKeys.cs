using Avalonia.Input;

namespace Metered.Controls;

/// <summary>
/// The keys as Base Web's menu reads them: by the DOM's <c>KeyboardEvent.key</c>, a printable key's character and any
/// other's name ("ArrowDown", "Shift", "Tab"), as type-ahead adds every key that isn't an arrow, Home, End or Enter to its
/// letters, and labels as its type-ahead matches them.
/// </summary>
internal static class MenuKeys
{
    /// <summary>The key's DOM name: its symbol when it prints one, else the name of the key.</summary>
    public static string Name(KeyEventArgs e)
    {
        if (e.KeySymbol is { Length: > 0 } symbol && !char.IsControl(symbol[0])) return symbol;
        return e.Key switch
        {
            Key.Up => "ArrowUp",
            Key.Down => "ArrowDown",
            Key.Left => "ArrowLeft",
            Key.Right => "ArrowRight",
            Key.Home => "Home",
            Key.End => "End",
            Key.Enter => "Enter",
            Key.Space => " ",
            Key.Escape => "Escape",
            Key.Tab => "Tab",
            Key.Back => "Backspace",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.LeftShift or Key.RightShift => "Shift",
            Key.LeftCtrl or Key.RightCtrl => "Control",
            Key.LeftAlt or Key.RightAlt => "Alt",
            Key.LWin or Key.RWin => "Meta",
            Key.CapsLock => "CapsLock",
            Key.NumLock => "NumLock",
            Key.Scroll => "ScrollLock",
            Key.Pause => "Pause",
            Key.PrintScreen => "PrintScreen",
            Key.Apps => "ContextMenu",
            // A key an input method takes, its Enter that commits a composition too: the DOM's "Process" (keyCode 229),
            // which stateful-container.js never takes for Enter.
            Key.ImeProcessed => "Process",
            >= Key.F1 and <= Key.F24 => "F" + (e.Key - Key.F1 + 1),
            >= Key.A and <= Key.Z => ((char)('a' + (e.Key - Key.A))).ToString(),
            >= Key.D0 and <= Key.D9 => ((char)('0' + (e.Key - Key.D0))).ToString(),
            _ => "Unidentified",
        };
    }

    /// <summary>
    /// Base Web's <c>'label' in list[0]</c>: whether the items have labels for type-ahead. A <see cref="MenuItemData"/> with a
    /// label has one, and a string is its own; any other object has none.
    /// </summary>
    public static bool HasLabel(object item) => item is MenuItemData { Label: not null } or string;

    /// <summary>The label type-ahead matches: a string label (Base Web's <c>label.toUpperCase</c> exists), or none.</summary>
    public static string? Label(object item) => item switch
    {
        MenuItemData { Label: string label } => label,
        string text => text,
        _ => null,
    };
}
