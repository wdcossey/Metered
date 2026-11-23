using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// What a menu renders, against Base Web's StatefulMenu rendered for the same case (MenuOracle.Structure): its scenarios and
/// examples (basic, compact, dividers, grouped, empty, href, profile) and the states they show. The parts in order, each
/// option's label, size, its parts' states (<c>:is-highlighted</c>, <c>:is-focused</c>, <c>:is-disabled</c>, the props
/// Base Web gave the option's box), its link, and what UI Automation reads for its role, <c>aria-selected</c> and
/// <c>aria-disabled</c>; the list's role, name and tab stop.
/// </summary>
public class MenuStructureTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, MenuOracle.Data.Structure.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void It_renders_what_Base_Web_renders(int index)
    {
        var c = MenuOracle.Data.Structure[index];
        var menu = MenuKit.Make(c);
        using var scene = new Scene(menu);
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        var listPeer = MenuKit.Peer(list);
        Assert.Equal(AutomationControlType.List, listPeer.GetAutomationControlType());
        Assert.Equal("listbox", c.Role);
        Assert.Equal(c.AriaLabel, listPeer.GetName());
        Assert.Equal(0, c.TabIndex);
        Assert.True(list.Focusable && list.IsTabStop);

        var rendered = MenuKit.Rendered(menu);
        Assert.Equal(c.Elements.Length, rendered.Count);
        for (var i = 0; i < rendered.Count; i++)
        {
            var e = c.Elements[i];
            var element = rendered[i];
            switch (e.Part)
            {
                case "OptgroupHeader":
                    Assert.Same(element, menu.Elements(Menu.OptgroupHeaderPart).Single(h => h == element));
                    Assert.Equal(e.Text, ((TextBlock)element).Text);
                    break;
                case "MenuDivider":
                    Assert.Contains(element, menu.Elements(Menu.MenuDividerPart));
                    break;
                case "EmptyState":
                    Assert.Contains(element, menu.Elements(Menu.EmptyStatePart));
                    Assert.Equal(e.Text, ((ContentPresenter)element).Content);
                    Assert.Equal("polite", e.AriaLive);
                    Assert.Equal(Avalonia.Automation.AutomationLiveSetting.Polite, Avalonia.Automation.AutomationProperties.GetLiveSetting(element));
                    break;
                case "ListItem":
                case "ListItemProfile":
                    Option(e, Assert.IsAssignableFrom<MenuOptionBase>(element), menu, c.Profile);
                    break;
                default:
                    throw new InvalidOperationException(e.Part);
            }
        }

        if (c.Profile)
        {
            var texts = menu.Elements(Menu.OptionPart).Cast<MenuOptionProfile>().SelectMany(o =>
                new[] { MenuOptionProfile.ProfileTitlePart, MenuOptionProfile.ProfileSubtitlePart, MenuOptionProfile.ProfileBodyPart }
                    .Select(p => (p, ((TextBlock?)o.Part(p))?.Text))).ToList();
            Assert.Equal(c.ProfileText.Select(t => (t.Part, (string?)t.Text)), texts);
        }
    }

    private static void Option(MenuElement e, MenuOptionBase option, Menu menu, bool profile)
    {
        Assert.Contains(option, menu.Elements(Menu.OptionPart));
        var box = (StyledElement)option.Part(profile ? MenuOptionProfile.ListItemProfilePart : MenuOptionList.ListItemPart)!;
        Assert.Equal(e.Highlighted, box.Classes.Contains(":is-highlighted"));
        Assert.Equal(e.Focused, box.Classes.Contains(":is-focused"));
        Assert.Equal(e.Disabled, box.Classes.Contains(":is-disabled"));
        var peer = MenuKit.Peer(option);
        Assert.Equal("option", e.Role);
        Assert.Equal(AutomationControlType.ListItem, peer.GetAutomationControlType());
        Assert.Equal(e.AriaSelected, peer.GetProvider<ISelectionItemProvider>()!.IsSelected);
        Assert.Equal(e.AriaDisabled, !peer.IsEnabled());
        if (profile)
        {
            Assert.Null(e.Size);
            var labels = ((MenuOptionProfile)option).Labels;
            Assert.Equal(e.Text, labels.Title + labels.Subtitle + labels.Body);
            return;
        }

        var list = (MenuOptionList)option;
        Assert.Equal(e.Size, list.Size.ToString().ToLowerInvariant());
        Assert.Equal(e.Size == "compact", box.Classes.Contains(":size-compact"));
        var anchor = list.Part(MenuOptionList.ListItemAnchorPart);
        Assert.Equal(e.Href is not null, anchor is not null);
        if (anchor is not null)
        {
            Assert.Same(anchor, ((ContentPresenter)box).Content);
            Assert.Equal(e.Href, ((MenuItemData)list.Item!).Href);
            Assert.Equal(e.Text, ((ContentPresenter)anchor).Content);
        }
        else Assert.Equal(e.Text, ((ContentPresenter)box).Content);
        Assert.Equal(e.Text, peer.GetName());
    }
}
