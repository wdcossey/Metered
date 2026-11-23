using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;

namespace Metered.Controls;

/// <summary>
/// A menu to UI Automation: the list Base Web renders (<c>role="listbox"</c>), named by its <c>aria-label</c>, the
/// menu's name or "Menu". Its children are its options. The list element itself, which takes the focus, is the list's
/// peer (<see cref="MenuListAutomationPeer"/>); the menu round it adds nothing of its own to the tree.
/// </summary>
public class MenuAutomationPeer(Menu owner) : ControlAutomationPeer(owner)
{
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

    protected override bool IsControlElementCore() => false;

    protected override bool IsContentElementCore() => false;
}

/// <summary>
/// The list of a menu: a list, named as the menu is (<c>aria-label</c>, "Menu" by default), its children what Base Web's
/// listbox holds: the options, each group's header, and the empty state's message (its scroller and boxes are nothing of
/// their own).
/// </summary>
public class MenuListAutomationPeer(MenuList owner) : ControlAutomationPeer(owner)
{
    protected override IReadOnlyList<AutomationPeer>? GetChildrenCore()
    {
        var menu = ((MenuList)Owner).Owner;
        if (menu is null) return base.GetChildrenCore();
        var peers = new List<AutomationPeer>();
        foreach (var child in menu.Rendered)
        {
            if (child is Avalonia.Controls.Presenters.ContentPresenter { Child: { } content }) peers.Add(GetOrCreate(content));
            else if (child is not Border) peers.Add(GetOrCreate(child));
        }

        return peers;
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;

    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : Menu.DefaultAccessibleName;
}

/// <summary>
/// An option to UI Automation: a list item (<c>role="option"</c>), selected while it is highlighted and the menu is
/// focused (<c>aria-selected</c>), not enabled when its item is disabled (<c>aria-disabled</c>), named by its label (an
/// option with a child menu by Base Web's <c>parentMenuItemAriaLabel</c>). Selecting it is a click: the menu chooses it.
/// </summary>
public class MenuOptionAutomationPeer(MenuOptionBase owner) : ControlAutomationPeer(owner), ISelectionItemProvider
{
    private new MenuOptionBase Owner => (MenuOptionBase)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;

    protected override bool IsEnabledCore() => base.IsEnabledCore() && !Owner.Disabled;

    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : Owner.AccessibleText;

    public bool IsSelected => Owner.IsHighlighted && Owner.IsMenuFocused;

    public ISelectionProvider? SelectionContainer => null;

    public void AddToSelection() => Select();

    public void RemoveFromSelection()
    {
    }

    public void Select()
    {
        if (!IsEnabled()) throw new ElementNotEnabledException();
        Owner.Owner?.OptionClicked(Owner, tap: false);
    }
}
