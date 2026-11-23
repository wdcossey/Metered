using System.Text.Json;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>Building the oracle's cases in Metered, and driving a menu as the oracle's steps do.</summary>
internal static class MenuKit
{
    /// <summary>The Option override a case's Option props make: Base Web's <c>overrides.Option.props</c>.</summary>
    public static OverrideCollection? OptionProps(string? size = null, string? label = null, bool renderHrefAsAnchor = true, bool profile = false)
    {
        if (profile)
        {
            var theme = new ControlTheme(typeof(MenuOptionProfile));
            // menu-profile-menu.scenario.tsx's: ({ title, subtitle, body }) => ({ title, subtitle, body }), no image, the title as its text.
            theme.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemLabelsProperty,
                (Func<object?, MenuProfileLabels?>)(item => item is Person p ? new MenuProfileLabels(p.Title, p.Subtitle, p.Body) : null)));
            theme.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemImgProperty, (Func<object?, object?>)(_ => null)));
            theme.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemImgTextProperty, (Func<object?, string?>)(item => (item as Person)?.Title)));
            return [new Override { Part = Menu.OptionPart, Props = theme }];
        }

        if (size is null && label is null && renderHrefAsAnchor) return null;
        var props = new ControlTheme(typeof(MenuOptionList));
        if (size is not null) props.Setters.Add(new Setter(MenuOptionList.SizeProperty, Enum.Parse<MenuOptionSize>(size, ignoreCase: true)));
        // grouped.tsx's getItemLabel: item => item.id.
        if (label == "id") props.Setters.Add(new Setter(MenuOptionList.GetItemLabelProperty, (Func<object?, object?>)(item => (item as Swatch)?.Id)));
        if (!renderHrefAsAnchor) props.Setters.Add(new Setter(MenuOptionList.RenderHrefAsAnchorProperty, false));
        return [new Override { Part = Menu.OptionPart, Props = props }];
    }

    /// <summary>A menu for a case: its items, Option props, first state, message and name.</summary>
    public static Menu Make(IEnumerable<object> items, OverrideCollection? overrides = null, MenuInitialState? initial = null)
    {
        var menu = new Menu { ItemsSource = items.ToList(), Overrides = overrides, Width = 350, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        if (initial?.HighlightedIndex is { } h) menu.HighlightedIndex = h;
        if (initial?.IsFocused is { } f) menu.IsMenuFocused = f;
        return menu;
    }

    public static Menu Make(MenuStructureCase c)
    {
        var menu = Make(MenuOracle.Items(c.Items, c.ItemsGiven), OptionProps(c.Size, c.Label, c.RenderHrefAsAnchor, c.Profile), c.InitialState);
        if (c.NoResultsMsg is not null) menu.NoResultsMsg = c.NoResultsMsg;
        if (c.AriaLabelGiven is not null) Avalonia.Automation.AutomationProperties.SetName(menu, c.AriaLabelGiven);
        return menu;
    }

    /// <summary>The menu's elements, in order, as the parts they are.</summary>
    public static IReadOnlyList<Control> Rendered(Menu menu) => ((Panel)((Visual)menu.Part(Menu.ListPart)!).GetVisualDescendants().OfType<StackPanel>().First(p => p.Name == Menu.ItemsPart)).Children;

    /// <summary>The DOM's key name as Avalonia's key and the symbol it prints.</summary>
    public static (Key Key, string? Symbol) KeyOf(string dom) => dom switch
    {
        "ArrowUp" => (Key.Up, null),
        "ArrowDown" => (Key.Down, null),
        "ArrowLeft" => (Key.Left, null),
        "ArrowRight" => (Key.Right, null),
        "Home" => (Key.Home, null),
        "End" => (Key.End, null),
        "Enter" => (Key.Enter, null),
        "Escape" => (Key.Escape, null),
        "Backspace" => (Key.Back, null),
        "Shift" => (Key.LeftShift, null),
        "Control" => (Key.LeftCtrl, null),
        " " => (Key.Space, " "),
        _ when dom.Length == 1 && char.IsLetter(dom[0]) => (Key.A + (char.ToLowerInvariant(dom[0]) - 'a'), dom),
        _ => throw new ArgumentException($"a key the kit doesn't press: {dom}"),
    };

    /// <summary>A real key pressed and released, on whatever has the focus.</summary>
    public static void Press(Scene scene, string dom)
    {
        var (key, symbol) = KeyOf(dom);
        scene.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, symbol);
        scene.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, symbol);
        scene.Pump();
    }

    /// <summary>The dispatcher's loop for a while, so its timers fire (type-ahead's 500 ms, the nested menus' 200 ms).</summary>
    public static void Wait(Scene scene, int ms) => PopoverTests.Settle(scene, ms);

    /// <summary>An option's centre, scrolled into the list's view first.</summary>
    public static Point CentreOf(Scene scene, Menu menu, int index)
    {
        var option = menu.OptionAt(index)!;
        option.BringIntoView();
        scene.Pump();
        return scene.CentreOf(option);
    }

    public static AutomationPeer Peer(Control c) => ControlAutomationPeer.CreatePeerForElement(c);
}
