using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Menu, as its documentation's examples: basic (basic.tsx), compact (compact.tsx), profile (profile.tsx), a
/// child menu (child.tsx), grouped (grouped.tsx), a long list (virtual-list.tsx), links (href.tsx), dividers
/// (dividers.tsx), and its empty state (menu-empty.scenario.tsx).
/// </summary>
[SamplePage("Menu", Order = 107)]
public partial class MenuPage : UserControl
{
    /// <summary>child.tsx's options with a child menu.</summary>
    public const string OpenRecent = "Open Recent ->";

    public const string NewBreakpoint = "New Breakpoint ->";

    /// <summary>The long list's items: virtual-list.tsx's 1500, here 100.</summary>
    public const int LongCount = 100;

    public MenuPage()
    {
        InitializeComponent();

        // profile.tsx: four of David Smith, by OptionProfile (its third line Metered's own, PROCESS §2: no Uber mark), the picture a box of the theme's tertiary background.
        Profile.ItemsSource = Enumerable.Range(0, 4).Select(_ => (object)new Person("David Smith", "Senior Engineering Manager", "Design Systems")).ToList();
        var profile = new ControlTheme(typeof(MenuOptionProfile));
        profile.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemLabelsProperty,
            (Func<object?, MenuProfileLabels?>)(item => item is Person p ? new MenuProfileLabels(p.Title, p.Subtitle, p.Body) : null)));
        profile.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemImgProperty, (Func<object?, object?>)(_ => Picture())));
        profile.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemImgTextProperty, (Func<object?, string?>)(item => (item as Person)?.Title)));
        Profile.Overrides = [new Override { Part = Menu.OptionPart, Props = profile }];

        // child.tsx: the file menu, compact, its two options with a child menu each.
        var recent = ChildMenu(["Reopen Closed Editor", "~/workspace/baseui", "~/workspace/styletron", "~/workspace/my-project", "More...", "Clear Recently Opened"]);
        var breakpoints = ChildMenu(["Conditional Breakpoint...", "Inline Breakpoint", "Function Breakpoint...", "Logpoint..."]);
        Child.ItemsSource = new[]
        {
            "New File", "New Window", "Open...", "Open Workspace...", OpenRecent, "Add Folder to Workspace...", "Save", "Save As...",
            "Toggle Breakpoint", NewBreakpoint, "Close Folder", "Close Window",
        }.Select(l => (object)new MenuItemData { Label = l }).ToList();
        var parent = CompactOverrides(width: 180);
        parent[1].Props!.Setters.Add(new Setter(MenuOptionBase.GetChildMenuProperty, (Func<object, object?>)(item => (item as MenuItemData)?.Label switch
        {
            OpenRecent => recent,
            NewBreakpoint => breakpoints,
            _ => null,
        })));
        Child.Overrides = parent;

        // virtual-list.tsx: "item number: n".
        Long.ItemsSource = Enumerable.Range(1, LongCount).Select(i => (object)new MenuItemData { Label = $"item number: {i}" }).ToList();
    }

    /// <summary>A child menu of child.tsx's: compact, narrower than its 200 px, to fit a phone beside its parent.</summary>
    private static Menu ChildMenu(string[] labels) => new()
    {
        ItemsSource = labels.Select(l => (object)new MenuItemData { Label = l }).ToList(),
        Overrides = CompactOverrides(width: 170),
    };

    /// <summary>child.tsx's overrides: the list's width, the options compact.</summary>
    private static OverrideCollection CompactOverrides(double width)
    {
        var list = new ControlTheme(typeof(MenuList));
        list.Setters.Add(new Setter(Layoutable.WidthProperty, width));
        var option = new ControlTheme(typeof(MenuOptionList));
        option.Setters.Add(new Setter(MenuOptionList.SizeProperty, MenuOptionSize.Compact));
        return [new Override { Part = Menu.ListPart, Style = list }, new Override { Part = Menu.OptionPart, Props = option }];
    }

    /// <summary>The profile's picture: a round box of the theme's tertiary background, where the example's placeholder image is.</summary>
    private static Control Picture()
    {
        var box = new Border { Width = 60, Height = 60, CornerRadius = new CornerRadius(30) };
        box.Bind(Border.BackgroundProperty, box.GetResourceObservable("colors.backgroundTertiary"));
        return box;
    }

    /// <summary>profile.tsx's item.</summary>
    public sealed record Person(string Title, string Subtitle, string Body);
}
