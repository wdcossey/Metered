using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// A menu's lengths against Base Web's style functions (MenuOracle.Layout): an option's padding and type by size and the
/// box they make, the list's padding inside its scroller and its radius, a header's, a divider's and the empty state's
/// boxes, the profile option's, and the option's colour and background transition.
/// </summary>
public class MenuLayoutTests
{
    private static MenuLayout L => MenuOracle.Data.Layout;

    private static Thickness Pad(double[] p) => new(p[0], p[1], p[2], p[3]);

    private static FontWeight Weight(string w) => Enum.Parse<FontWeight>(w);

    public static TheoryData<string> Sizes => new(L.Options.Select(o => o.Size));

    [AvaloniaTheory]
    [MemberData(nameof(Sizes))]
    public void An_options_box_is_its_padding_round_a_line(string size)
    {
        var o = L.Options.Single(x => x.Size == size);
        var items = Enumerable.Range(0, 3).Select(i => (object)new MenuItemData { Label = "Item " + i }).ToList();
        var menu = MenuKit.Make(items, MenuKit.OptionProps(size: size));
        using var scene = new Scene(menu);
        var option = (MenuOptionList)menu.OptionAt(1)!;
        var box = (ContentPresenter)option.Part(MenuOptionList.ListItemPart)!;
        Assert.Equal(Pad(o.Padding), box.Padding);
        Assert.Equal(o.Font.FontSize, box.FontSize);
        Assert.Equal(Weight(o.Font.FontWeight), box.FontWeight);
        Assert.Equal(o.Font.LineHeight, box.LineHeight);
        // A block: as wide as the list, as tall as its padding and one line.
        Assert.Equal(o.Padding[1] + o.Font.LineHeight + o.Padding[3], option.Bounds.Height);
        Assert.Equal(menu.Bounds.Width, option.Bounds.Width);

        // The list's padding is inside its scroller, before the first option and after the last.
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        Assert.Equal(Pad(L.List.Padding), list.Padding);
        Assert.Equal(new CornerRadius(L.List.Radius), list.CornerRadius);
        Assert.Equal(L.List.Padding[1], menu.OptionAt(0)!.TranslatePoint(default, list)!.Value.Y);
        var height = L.List.Padding[1] + 3 * option.Bounds.Height + L.List.Padding[3];
        Assert.Equal(height, list.Bounds.Height);
        Assert.Equal(height, list.Extent.Height);
    }

    [AvaloniaFact]
    public void A_header_a_divider_and_the_empty_state_are_Base_Webs_boxes()
    {
        var group = new MenuGroup { Label = "Group", Items = { new MenuItemData { Label = "a" }, new MenuItemData { Divider = true }, new MenuItemData { Label = "b" } } };
        var menu = MenuKit.Make([group]);
        var empty = MenuKit.Make([]);
        using var scene = new Scene(new StackPanel { Spacing = 20, Children = { menu, empty } });

        var header = (TextBlock)menu.Part(Menu.OptgroupHeaderPart)!;
        Assert.Equal(Pad(L.Header.Padding), header.Padding);
        Assert.Equal(L.Header.Font.FontSize, header.FontSize);
        Assert.Equal(Weight(L.Header.Font.FontWeight), header.FontWeight);
        Assert.Equal(L.Header.Font.LineHeight, header.LineHeight);
        Assert.Equal(L.Header.Padding[1] + L.Header.Font.LineHeight + L.Header.Padding[3], header.Bounds.Height);

        // Its height 0, its border below it: a line of the border's width, inset by its margins.
        var divider = (Border)menu.Part(Menu.MenuDividerPart)!;
        Assert.Equal(L.Divider.Height + L.Divider.BorderWidth, divider.Bounds.Height);
        Assert.Equal(new Thickness(0, 0, 0, L.Divider.BorderWidth), divider.BorderThickness);
        Assert.Equal(Pad(L.Divider.Margin), divider.Margin);
        var a = menu.OptionAt(0)!;
        var b = menu.OptionAt(2)!;
        Assert.Equal(a.Bounds.Bottom + L.Divider.Margin[1], divider.Bounds.Top);
        Assert.Equal(divider.Bounds.Bottom + L.Divider.Margin[3], b.Bounds.Top);
        Assert.Equal(L.Divider.Margin[0], divider.Bounds.Left);
        Assert.Equal(menu.Bounds.Width - L.Divider.Margin[0] - L.Divider.Margin[2], divider.Bounds.Width);

        var state = (ContentPresenter)empty.Part(Menu.EmptyStatePart)!;
        Assert.Equal(Pad(L.Empty.Padding), state.Padding);
        Assert.Equal(L.Empty.Font.FontSize, state.FontSize);
        Assert.Equal(L.Empty.Font.LineHeight, state.LineHeight);
        Assert.Equal("center", L.Empty.TextAlign);
        Assert.Equal(TextAlignment.Center, state.TextAlignment);
        Assert.Equal(L.Empty.Padding[1] + L.Empty.Font.LineHeight + L.Empty.Padding[3], state.Bounds.Height);
        var text = state.Child!;
        Assert.Equal(state.Bounds.Width / 2, text.Bounds.Center.X, 0.5);
    }

    [AvaloniaFact]
    public void A_profile_option_is_its_picture_box_and_its_labels_in_a_row()
    {
        var menu = MenuKit.Make([new Person("David Smith", "Senior Engineering Manager", "Uber Everything")], MenuKit.OptionProps(profile: true));
        using var scene = new Scene(menu);
        var option = (MenuOptionProfile)menu.OptionAt(0)!;
        var box = (Border)option.Part(MenuOptionProfile.ListItemProfilePart)!;
        var picture = (Border)option.Part(MenuOptionProfile.ProfileImgContainerPart)!;
        var labels = (StackPanel)option.Part(MenuOptionProfile.ProfileLabelsContainerPart)!;
        var p = L.Profile;
        Assert.Equal(Pad(p.Padding), box.Padding);
        Assert.Equal(new Size(p.ImgContainer[0], p.ImgContainer[1]), picture.Bounds.Size);
        Assert.Equal(p.LabelsMarginLeft, labels.Margin.Left);
        Assert.Equal(VerticalAlignment.Top, labels.VerticalAlignment);
        foreach (var (part, font) in new[] { (MenuOptionProfile.ProfileTitlePart, p.Title), (MenuOptionProfile.ProfileSubtitlePart, p.Subtitle), (MenuOptionProfile.ProfileBodyPart, p.Body) })
        {
            var t = (TextBlock)option.Part(part)!;
            Assert.Equal(font.FontSize, t.FontSize);
            Assert.Equal(Weight(font.FontWeight), t.FontWeight);
            Assert.Equal(font.LineHeight, t.LineHeight);
        }

        // The row's height is its tallest child's (the labels' three lines, the picture's 60) and the padding.
        var lines = p.Title.LineHeight + p.Subtitle.LineHeight + p.Body.LineHeight;
        Assert.Equal(p.Padding[1] + Math.Max(lines, p.ImgContainer[1]) + p.Padding[3], option.Bounds.Height);
        Assert.Equal(p.Padding[0], picture.TranslatePoint(default, box)!.Value.X);
        Assert.Equal(p.Padding[0] + p.ImgContainer[0] + p.LabelsMarginLeft, labels.TranslatePoint(default, box)!.Value.X);
        Assert.Equal("50%", p.ImgRadius);
    }

    [AvaloniaFact]
    public void An_options_colour_and_background_move_as_Base_Webs_once_it_is_loaded()
    {
        var menu = MenuKit.Make([new MenuItemData { Label = "a" }]);
        using var scene = new Scene(menu);
        var box = (ContentPresenter)((MenuOptionList)menu.OptionAt(0)!).Part(MenuOptionList.ListItemPart)!;
        Assert.Contains(":loaded", box.Classes);
        var t = L.Transition;
        Assert.Equal("color, background-color", t.Property);
        var transitions = box.Transitions!.OfType<BrushTransition>().ToList();
        Assert.Equal(new[] { "Foreground", "Background" }, transitions.Select(x => x.Property!.Name));
        Assert.All(transitions, x =>
        {
            Assert.Equal(TimeSpan.FromSeconds(t.Seconds), x.Duration);
            var e = Assert.IsType<Avalonia.Animation.Easings.SplineEasing>(x.Easing);
            Assert.Equal(t.Curve, new[] { e.X1, e.Y1, e.X2, e.Y2 });
        });
    }
}
