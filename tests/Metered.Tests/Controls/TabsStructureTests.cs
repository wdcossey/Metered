using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// What Base Web's Tabs renders for each case (TabsOracle: tabs.js and stateful-tabs.js rendered, each part's props
/// recorded), against what Metered's Tabs makes of the same case: the parts in Base Web's order, each tab's text (its
/// title or its key), its artwork, whether it is selected (UI Automation's selection), its tab stop (tabIndex 0 only for
/// the active tab), disabled; each panel hidden but the active one's, its content only then or with renderAll; the end
/// enhancer horizontal only; the list's role and orientation; the highlight hidden from UI Automation.
/// </summary>
public class TabsStructureTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, TabsOracle.Data.Structure.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Renders_what_Base_Web_renders(int index)
    {
        var c = TabsOracle.Data.Structure[index];
        var tabs = TabsOracle.Make(c.Input);
        using var scene = new Scene(tabs);

        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        Assert.Equal(c.Tabs.Length, buttons.Count);
        for (var i = 0; i < c.Tabs.Length; i++)
        {
            var e = c.Tabs[i];
            var b = buttons[i];
            Assert.Equal(e.Key, tabs.Keys[i]);
            Assert.Equal(e.Text, tabs.TextOf(b));
            Assert.Equal(e.Text, ControlAutomationPeer.CreatePeerForElement(b).GetName());
            var peer = (TabButtonAutomationPeer)ControlAutomationPeer.CreatePeerForElement(b);
            Assert.Equal(AutomationControlType.TabItem, peer.GetAutomationControlType());
            Assert.Equal(e.Selected, peer.IsSelected);
            Assert.Equal(e.Selected, b.Classes.Contains(":is-active"));
            Assert.Equal(e.TabStop, b.IsTabStop);
            Assert.Equal(e.Disabled, !b.IsEnabled);
            Assert.Equal(e.Artwork, tabs.Elements(Tabs.ArtworkContainerPart).Any(a => b.IsVisualAncestorOf((Visual)a)));
            Assert.True(e.ControlsItsPanel);
        }

        Assert.Equal(c.Tabs.Count(t => t.Artwork), tabs.Elements(Tabs.ArtworkContainerPart).Count);
        if (c.ArtworkProps is { } art)
        {
            Assert.True(scene.Window.TryFindResource("Tabs.ArtworkSize", out var size));
            Assert.Equal(art.Size, (double)size!);
        }

        var panels = tabs.Elements(Tabs.TabPanelPart).Cast<ContentPresenter>().ToList();
        Assert.Equal(c.Panels.Length, panels.Count);
        for (var i = 0; i < c.Panels.Length; i++)
        {
            Assert.Equal(c.Panels[i].Hidden, !panels[i].IsVisible);
            Assert.Equal(c.Panels[i].Content, panels[i].Content as string);
            // aria-labelledby its tab: named as its tab is.
            Assert.True(c.Panels[i].LabelledByItsTab);
            Assert.Equal(c.Tabs[i].Text, AutomationProperties.GetName(panels[i]));
        }

        // The list: role="tablist", aria-orientation.
        Assert.Equal("tablist", c.TabList.Role);
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        Assert.Equal(AutomationControlType.Tab, ControlAutomationPeer.CreatePeerForElement(list).GetAutomationControlType());
        Assert.Equal(c.TabList.Orientation == "vertical" ? Orientation.Vertical : Orientation.Horizontal, list.Orientation);

        // The bar's $hasEndEnhancer, and the enhancer's container rendered (horizontal only).
        Assert.Equal(c.HasEndEnhancer, tabs.Part(Tabs.TabBarPart)!.Classes.Contains(":has-end-enhancer"));
        Assert.Equal(c.EndEnhancerShown, ((Control)tabs.Part(Tabs.EndEnhancerContainerPart)!).IsVisible);

        // The highlight: aria-hidden, role="presentation", not animated on mount.
        var highlight = (TabHighlight)tabs.Part(Tabs.TabHighlightPart)!;
        Assert.True(c.Highlight.Hidden);
        Assert.Equal(AccessibilityView.Raw, AutomationProperties.GetAccessibilityView(highlight));
        Assert.Equal(c.Highlight.Animate, highlight.Classes.Contains(":animate"));

        // The parts in Base Web's order: the root, the bar, the list, its tabs, the highlight, (the enhancer,) the border,
        // the panels.
        var named = new[] { Tabs.RootPart, Tabs.TabBarPart, Tabs.TabListPart, Tabs.TabHighlightPart, Tabs.EndEnhancerContainerPart, Tabs.TabBorderPart };
        var order = new List<string>();
        foreach (var v in tabs.GetVisualDescendants())
        {
            if (v is TabButton) order.Add(Tabs.TabPart);
            else if (v is Control { Name: { } n } ctl && named.Contains(n) && ctl.IsVisible) order.Add(n);
            else if (panels.Contains(v)) order.Add(Tabs.TabPanelPart);
            else if (v is ContentPresenter p && tabs.Elements(Tabs.ArtworkContainerPart).Contains(p)) order.Add(Tabs.ArtworkContainerPart);
        }

        Assert.Equal(c.Parts, order);
    }

    // StatefulTabs' initial state is the first tab's key: set on the property, as the state is (stateful-tabs.js).
    [AvaloniaFact]
    public void An_unset_key_becomes_the_first_tabs()
    {
        var c = TabsOracle.Data.Structure.Single(s => s.Name == "stateful own keys");
        var tabs = TabsOracle.Make(c.Input);
        Assert.Null(tabs.ActiveKey);
        using var scene = new Scene(tabs);
        Assert.Equal(c.Tabs.Single(t => t.Selected).Key, tabs.ActiveKey);
    }
}
