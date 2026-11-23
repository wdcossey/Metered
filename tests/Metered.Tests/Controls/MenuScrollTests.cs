using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// Keyboard scrolling against Base Web's own scrollItemIntoView (utils.js) on the geometry its styles give
/// (MenuOracle.Scroll): basic.tsx's 250 px list stepped down to its end and back, a long compact list's End and Home, and
/// a first highlight scrolled to the list's centre as componentDidMount does (menu-stateful.scenario.tsx's, and one out
/// of view). The list's offset after each key is Base Web's scrollTop.
/// </summary>
public class MenuScrollTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, MenuOracle.Data.Scroll.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void The_keys_scroll_the_highlight_into_view_as_Base_Webs(int index)
    {
        var c = MenuOracle.Data.Scroll[index];
        var items = Enumerable.Range(1, c.Count).Select(i => (object)new MenuItemData { Label = "Item " + i }).ToList();
        var height = new ControlTheme(typeof(MenuList));
        height.Setters.Add(new Setter(Layoutable.HeightProperty, c.BoxHeight));
        var overrides = MenuKit.OptionProps(size: c.Size) ?? [];
        overrides.Add(new Override { Part = Menu.ListPart, Style = height });
        // Focused from the start, so the focus doesn't highlight the first item: the steps start from Base Web's.
        var menu = MenuKit.Make(items, overrides, new MenuInitialState(c.Start, true));
        using var scene = new Scene(menu);
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        Assert.Equal(c.BoxHeight, list.Bounds.Height);
        Assert.Equal(c.Item, menu.OptionAt(0)!.Bounds.Height);
        Assert.Equal(c.InitialScrollTop, list.Offset.Y);
        list.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.Equal(c.Start, menu.HighlightedIndex);
        foreach (var step in c.Steps)
        {
            MenuKit.Press(scene, step.Key);
            Assert.Equal(step.HighlightedIndex, menu.HighlightedIndex);
            Assert.True(step.ScrollTop == list.Offset.Y, $"after {step.Key} to {step.HighlightedIndex}: scrolled to {list.Offset.Y}, Base Web's {step.ScrollTop}");
        }
    }
}
