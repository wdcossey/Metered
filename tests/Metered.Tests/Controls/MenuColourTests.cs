using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// A menu's colours, light and dark, against Base Web's (MenuOracle): an option at rest, highlighted, highlighted with the
/// menu focused, disabled, and disabled and highlighted (by the keys) with and without focus, its text, fill and cursor,
/// and its href label's colour (always the default: option-list.js gives the anchor none of the option's states); the
/// list's fill, shadow and keyboard focus outline; a header's, a divider's and the empty state's colours; the profile
/// option's hover fill and text.
/// </summary>
public class MenuColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, MenuOracle.Data.Colours.Length));

    public static TheoryData<int> Surfaces => new(Enumerable.Range(0, MenuOracle.Data.Surfaces.Length));

    /// <summary>menu.js's item index for each state (the oracle's): A (an href) for the highlight, B at rest, C disabled.</summary>
    private static (int Index, bool Highlighted, bool Focused) StateOf(string state) => state switch
    {
        "rest" => (1, false, false),
        "highlighted" => (0, true, false),
        "highlightedFocused" => (0, true, true),
        "disabled" => (2, false, false),
        "disabledHighlighted" => (2, true, false),
        "disabledHighlightedFocused" => (2, true, true),
        _ => throw new ArgumentException(state),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Options_match_Base_Web(int index)
    {
        var c = MenuOracle.Data.Colours[index];
        var (i, highlighted, focused) = StateOf(c.State);
        var items = new object[]
        {
            new MenuItemData { Label = "A", Href = "#a" }, new MenuItemData { Label = "B" }, new MenuItemData { Label = "C", Disabled = true },
        };
        var menu = MenuKit.Make(items, initial: new MenuInitialState(highlighted ? i : -1, focused));
        using var scene = new Scene(menu, MenuOracle.Variant(c.Theme));
        var option = (MenuOptionList)menu.OptionAt(i)!;
        var box = (ContentPresenter)option.Part(MenuOptionList.ListItemPart)!;
        Assert.Equal(c.Foreground, Colour(box.Foreground));
        Assert.Equal(c.Background, Colour(box.Background));
        Assert.Equal(InputColourTests.Cursor(c.Cursor), box.Cursor?.ToString());
        if (c.Anchor is { } anchor)
        {
            var link = (ContentPresenter)((MenuOptionList)menu.OptionAt(0)!).Part(MenuOptionList.ListItemAnchorPart)!;
            Assert.Equal(anchor, Colour(link.Foreground));
            // A browser's own stylesheet underlines a link; the text inherits it.
            var text = link.GetVisualDescendants().OfType<TextBlock>().Single();
            Assert.Equal(TextDecorations.Underline, text.TextDecorations);
        }
    }

    [AvaloniaTheory]
    [MemberData(nameof(Surfaces))]
    public void The_list_the_headers_dividers_empty_state_and_profile_match_Base_Web(int index)
    {
        var c = MenuOracle.Data.Surfaces[index];
        var group = new MenuGroup { Label = "G", Items = { new MenuItemData { Label = "a" }, new MenuItemData { Divider = true } } };
        var menu = MenuKit.Make([group]);
        var empty = MenuKit.Make([]);
        var profile = MenuKit.Make([new Person("David Smith", "Senior Engineering Manager", "Uber Everything")], MenuKit.OptionProps(profile: true));
        using var scene = new Scene(new StackPanel { Spacing = 20, Margin = new Thickness(10), Children = { menu, empty, profile } }, MenuOracle.Variant(c.Theme));
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        Assert.Equal(c.List, Colour(list.Background));
        Assert.Equal(BoxShadows.Parse(c.ListShadow), list.BoxShadow);
        Assert.Equal(c.Header, Colour(((TextBlock)menu.Part(Menu.OptgroupHeaderPart)!).Foreground));
        Assert.Equal(c.Divider, Colour(((Border)menu.Part(Menu.MenuDividerPart)!).BorderBrush));
        Assert.Equal(c.Empty, Colour(((ContentPresenter)empty.Part(Menu.EmptyStatePart)!).Foreground));

        // The outline: on keyboard focus, Base Web's $isFocusVisible.
        Assert.Null(list.OutlineBrush);
        scene.Key(Avalonia.Input.Key.Tab);
        Assert.True(list.IsFocused);
        Assert.Contains(":focus-visible", list.Classes);
        Assert.Equal(c.ListFocusOutline, Colour(list.OutlineBrush));
        Assert.Equal(MenuOracle.Data.Layout.List.FocusOutlineWidth, list.OutlineThickness);

        var option = (MenuOptionProfile)profile.OptionAt(0)!;
        var box = (Border)option.Part(MenuOptionProfile.ListItemProfilePart)!;
        Assert.Equal("#00000000", Colour(box.Background));
        scene.Hover(box);
        PopoverTests.Settle(scene, 300);
        Assert.Contains(":hover", box.Classes);
        Assert.Equal(c.ProfileHover, Colour(box.Background));
        Assert.Equal(c.ProfileTitle, Colour(((TextBlock)option.Part(MenuOptionProfile.ProfileTitlePart)!).Foreground));
        Assert.Equal(c.ProfileSubtitle, Colour(((TextBlock)option.Part(MenuOptionProfile.ProfileSubtitlePart)!).Foreground));
        Assert.Equal(c.ProfileBody, Colour(((TextBlock)option.Part(MenuOptionProfile.ProfileBodyPart)!).Foreground));
    }

    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);
}
