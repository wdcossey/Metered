using Avalonia;
using Avalonia.Data;

namespace Metered.Controls;

/// <summary>
/// Base Web's controlled Popover, as a child menu (maybe-child-menu.js) and Select (select-component.js) give it: its
/// <c>isOpen</c> is its owner's state, held there (coerced), so neither a click on its anchor nor a dismissal changes
/// it, as a controlled Base Web popover calls its handlers and changes nothing. Its look and template are the Popover's.
/// </summary>
/// <remarks>Moved here from the Menu's option (#97) so that Select's dropdown uses it too.</remarks>
internal sealed class ControlledPopover : Popover
{
    static ControlledPopover()
    {
        IsOpenProperty.OverrideMetadata<ControlledPopover>(new StyledPropertyMetadata<bool>(
            defaultBindingMode: BindingMode.TwoWay, coerce: (o, _) => ((ControlledPopover)o).Held));
    }

    /// <summary>Whether it is open: its owner's state.</summary>
    internal bool Held { get; set; }

    /// <summary>Open or closed as its owner's state is.</summary>
    internal void Hold(bool open)
    {
        Held = open;
        CoerceValue(IsOpenProperty);
    }
}
