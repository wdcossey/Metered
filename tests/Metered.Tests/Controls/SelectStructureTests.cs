using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// Which parts a select shows for each of Base Web's cases, their text, sizes and ARIA as UI Automation's, and the props it
/// gives its dropdown's popover (SelectOracle's structure); and what the open dropdown lists: each option, its label's
/// states, which is highlighted first, the groups' headers, the empty state, its height (SelectOracle's dropdown).
/// </summary>
public class SelectStructureTests
{
    public static TheoryData<string> Structure => new(SelectOracle.Data.Structure.Select(c => c.Name));

    public static TheoryData<string> Dropdowns => new(SelectOracle.Data.Dropdown.Select(c => c.Name));

    private static readonly string[] Shown = [Select.SingleValuePart, Select.InputContainerPart, Select.InputPart, Select.PlaceholderPart,
        Select.LoadingIndicatorPart, Select.ClearIconPart, Select.SelectArrowPart, Select.SearchIconContainerPart, Select.SearchIconPart];

    [AvaloniaTheory]
    [MemberData(nameof(Structure))]
    public void The_parts_shown_and_their_ARIA_are_Base_Webs(string name)
    {
        var c = SelectOracle.Data.Structure.Single(x => x.Name == name);
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select
        {
            Options = options,
            Value = SelectOracle.Value(options, c.Value, c.Unclearable),
            Clearable = c.Clearable ?? true,
            IsEnabled = c.Disabled != true,
            IsLoading = c.IsLoading == true,
            Searchable = c.Searchable ?? true,
            Type = c.Type == "search" ? SelectType.Search : SelectType.Select,
            Multi = c.Multi == true,
            Placeholder = c.Placeholder,
            Required = c.Required == true,
        };
        if (c.AriaLabelGiven is { } label) AutomationProperties.SetName(select, label);
        using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Children = { select } });

        // The parts Base Web renders, as many as it renders them (a hidden input is the InputContainer's: not its Input part).
        var want = c.Elements.Select(e => e.Part).Where(p => Shown.Contains(p)).OrderBy(p => p).ToList();
        var got = Shown.Where(select.Shown).OrderBy(p => p).ToList();
        Assert.Equal(want, got);
        Assert.Equal(c.Elements.Count(e => e.Part == "Tag"), select.Tags.Count);

        foreach (var e in c.Elements)
        {
            switch (e.Part)
            {
                case "SingleValue":
                    Assert.Equal(e.Text, select.P<ContentPresenter>(Select.SingleValuePart).Content);
                    break;
                case "Placeholder":
                    Assert.Equal(e.Text, select.P<TextBlock>(Select.PlaceholderPart).Text);
                    break;
                case "Tag":
                    var tag = select.Tags.Single(t => Select.LabelText(t.Value) == e.Text);
                    Assert.Equal(e.Text, tag.Content);
                    Assert.Equal(e.Disabled, !tag.IsEffectivelyEnabled);
                    // tag.js: a button, "Remove …", tabIndex 0, while it isn't disabled.
                    var peer = ControlAutomationPeer.CreatePeerForElement(tag);
                    Assert.Equal(e.Role == "button", peer.GetAutomationControlType() == AutomationControlType.Button);
                    if (e.AriaLabel is not null) Assert.Equal(e.AriaLabel, peer.GetName());
                    Assert.Equal(e.TabIndex == 0, tag.Focusable && tag.IsEffectivelyEnabled);
                    Assert.Equal(e.Action, tag.Part(SelectTag.ActionPart) is Visual { IsVisible: true });
                    Assert.Equal(e.ActionIconSize, ((Control)tag.Part(SelectTag.ActionIconPart)!).Bounds.Width);
                    break;
                case "ClearIcon":
                    var clear = select.P<InputIcon>(Select.ClearIconPart);
                    Assert.Equal(e.Size, clear.Bounds.Width);
                    Assert.Equal(e.AriaLabel, AutomationProperties.GetName(clear));
                    break;
                case "SelectArrow":
                    Assert.Equal(e.Size, select.P<InputIcon>(Select.SelectArrowPart).Bounds.Width);
                    break;
                case "SearchIcon":
                    Assert.Equal(e.Size, select.P<InputIcon>(Select.SearchIconPart).Bounds.Width);
                    break;
                case "Input" or "InputContainer" when e.AriaLabel is not null || e.Role is not null:
                    // The focusable element: the input, or its box when it isn't searchable (its hidden input has no part).
                    var target = (Control)select.FocusTarget!;
                    Assert.Equal(e.Part == "Input" ? Select.InputPart : Select.InputContainerPart, target.Name);
                    var combo = ControlAutomationPeer.CreatePeerForElement(target);
                    Assert.Equal(AutomationControlType.ComboBox, combo.GetAutomationControlType());
                    Assert.Equal(e.AriaLabel, combo.GetName());
                    Assert.Equal(ExpandCollapseState.Collapsed, ((IExpandCollapseProvider)combo).ExpandCollapseState);
                    Assert.Equal(e.TabIndex == 0, target.Focusable && target.IsTabStop && target.IsEffectivelyEnabled || c.Disabled == true);
                    break;
            }
        }

        Assert.Equal(c.Required == true, AutomationProperties.GetIsRequiredForForm(select));

        // The props select-component.js gives its Popover.
        var popover = select.DropdownPopover!;
        Assert.Equal(Enum.Parse<PopoverPlacement>(c.Popover.Placement, ignoreCase: true), popover.Placement);
        Assert.Equal(c.Popover.PopoverMargin, popover.PopoverMargin);
        Assert.Equal(c.Popover.AutoFocus, popover.AutoFocus);
        Assert.Equal(c.Popover.FocusLock, popover.FocusLock);
        Assert.Equal(Enum.Parse<PopoverAccessibilityType>(c.Popover.AccessibilityType, ignoreCase: true), popover.AccessibilityType);
    }

    [AvaloniaTheory]
    [MemberData(nameof(Dropdowns))]
    public void The_dropdown_lists_what_Base_Webs_does(string name)
    {
        var c = SelectOracle.Data.Dropdown.Single(x => x.Name == name);
        var options = c.Options is { } list ? SelectOracle.Options(list) : SelectOracle.Options(c.OptionsGiven!.Value);
        var select = new Select
        {
            Options = options,
            Value = SelectOracle.Value(options, c.Value),
            Multi = c.Multi == true,
            FilterOutSelected = c.FilterOutSelected ?? true,
            NoResultsMsg = c.NoResultsMsg,
            Size = c.Size is { } s ? SelectOracle.Size(s) : SelectSize.Default,
            MaxDropdownHeight = c.MaxDropdownHeight ?? Select.MaxDropdownHeightDefault,
        };
        var (scene, _) = SelectKit.Page(select);
        using var _s = scene;
        select.SetDropdownOpen(true);
        PopoverTests.Settle(scene);
        var menu = select.DropdownMenu!;
        Assert.True(menu.IsEffectivelyVisible && menu.IsAttachedToVisualTree());
        var rendered = menu.Rendered.ToList();
        var options2 = c.Elements.Where(e => e.Part == "DropdownListItem").ToList();
        var contents = c.Elements.Where(e => e.Part == "OptionContent").ToList();
        var made = rendered.OfType<MenuOptionList>().ToList();
        Assert.Equal(options2.Count, made.Count);
        for (var i = 0; i < made.Count; i++)
        {
            var content = (SelectOptionContent)((ContentPresenter)made[i].Part(MenuOptionList.ListItemPart)!).Content!;
            Assert.Equal(contents[i].Text, content.Text);
            Assert.Equal(options2[i].Highlighted, made[i].IsHighlighted);
            Assert.Equal(options2[i].Disabled, made[i].Disabled);
            Assert.Equal(contents[i].Selected, content.Selected);
            Assert.Equal(contents[i].Disabled, content.Disabled);
            Assert.Equal(contents[i].Highlighted, content.IsHighlighted);
            Assert.Equal(contents[i].Text, ControlAutomationPeer.CreatePeerForElement(made[i]).GetName());
        }

        // The groups' headers and the empty state, in order among the options.
        var order = c.Elements.Where(e => e.Part is "DropdownListItem" or "OptgroupHeader" or "EmptyState").Select(e => e.Part == "DropdownListItem" ? "option:" + e.Text : e.Part + ":" + e.Text).ToList();
        var got = rendered.Select(r => r switch
        {
            MenuOptionList o => "option:" + ((SelectOptionContent)((ContentPresenter)o.Part(MenuOptionList.ListItemPart)!).Content!).Text,
            TextBlock t => "OptgroupHeader:" + t.Text,
            ContentPresenter p => "EmptyState:" + p.Content,
            _ => r.GetType().Name,
        }).ToList();
        Assert.Equal(order, got);

        // Its height: maxDropdownHeight, 40vh of the window's height by default; as wide as the select.
        var listPart = (MenuList)menu.Part(Menu.ListPart)!;
        var max = c.MaxHeight!.EndsWith("vh", StringComparison.Ordinal) ? scene.Window.ClientSize.Height * double.Parse(c.MaxHeight[..^2], System.Globalization.CultureInfo.InvariantCulture) / 100
            : double.Parse(c.MaxHeight[..^2], System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal(max, listPart.MaxHeight, 3);
        Assert.True(listPart.Bounds.Height <= max + 0.01);
        Assert.Equal("anchor", c.ContainerWidth);
        Assert.Equal(select.P<Control>(Select.RootPart).Bounds.Width, ((Control)select.Part(Select.DropdownContainerPart)!).Bounds.Width);
        Assert.Equal(c.Elements.Single(e => e.Part == "Dropdown").Role == "listbox", ControlAutomationPeer.CreatePeerForElement(listPart).GetAutomationControlType() == AutomationControlType.List);
        // Placed below the select, at its left: popoverMargin 0, placement bottom.
        var root = select.P<Control>(Select.RootPart);
        var below = root.TranslatePoint(new Point(0, root.Bounds.Height), scene.Window)!.Value;
        var top = ((Visual)select.Part(Select.DropdownContainerPart)!).TranslatePoint(default, scene.Window)!.Value;
        Assert.Equal(below.X, top.X, 3);
        Assert.Equal(below.Y, top.Y, 3);
    }
}
