using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of a menu: the list's fill, a highlighted option's fill and a disabled option's text in Base Web's colours,
/// where the layout puts them. And, when METERED_FRAMES names a folder, a gallery of Base Web's menu examples and
/// scenarios, light and dark, saved there for docs/controls/Menu.
/// </summary>
public class MenuFrames
{
    private static MenuColourCase Case(string state, string theme) => MenuOracle.Data.Colours.Single(c => c.Theme == theme && c.State == state);

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_list_and_a_highlighted_option_are_drawn_in_Base_Webs_colours(string theme)
    {
        var items = new object[] { new MenuItemData { Label = "One" }, new MenuItemData { Label = "Two" }, new MenuItemData { Label = "Three", Disabled = true } };
        var menu = MenuKit.Make(items, initial: new MenuInitialState(1, true));
        using var scene = new Scene(new Border { Padding = new Thickness(20), Child = menu }, MenuOracle.Variant(theme));
        var frame = scene.Capture();
        var surface = MenuOracle.Data.Surfaces.Single(s => s.Theme == theme);
        Assert.Equal(Color.Parse(surface.List), At(scene, frame, menu.OptionAt(0)!, 4, 4));
        Assert.Equal(Color.Parse(Case("highlightedFocused", theme).Background), At(scene, frame, menu.OptionAt(1)!, 4, 4));
        Assert.Equal(Color.Parse(surface.List), At(scene, frame, menu.OptionAt(2)!, 4, 4));
        // The list's top padding is the list's fill, above the first option.
        Assert.Equal(Color.Parse(surface.List), At(scene, frame, (Visual)menu.Part(Menu.ListPart)!, 30, 4));
    }

    private static Color At(Scene scene, Avalonia.Media.Imaging.WriteableBitmap frame, Visual v, double x, double y)
    {
        var p = v.TranslatePoint(new Point(x, y), scene.Window)!.Value;
        return Frames.PixelAt(frame, (int)p.X, (int)p.Y);
    }

    private static OverrideCollection ListStyle(double? height = null, double width = 350, OverrideCollection? more = null)
    {
        var style = new ControlTheme(typeof(MenuList));
        style.Setters.Add(new Setter(Layoutable.WidthProperty, width));
        // A CSS height is the content box: the list's own padding is added to it (docs/controls/Menu).
        if (height is { } h) style.Setters.Add(new Setter(Layoutable.HeightProperty, h + 16));
        var all = new OverrideCollection { new Override { Part = Menu.ListPart, Style = style } };
        if (more is not null) all.AddRange(more);
        return all;
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;
        var dark = theme == "Dark";
        StackPanel Column() => new() { Width = 370, Spacing = 24, Margin = new Thickness(12) };
        var left = Column();
        var right = Column();
        Menu M(IEnumerable<object> items, OverrideCollection overrides, int highlighted = -1, bool focused = false)
        {
            var m = new Menu { ItemsSource = items.ToList(), Overrides = overrides, HorizontalAlignment = HorizontalAlignment.Left };
            m.HighlightedIndex = highlighted;
            m.IsMenuFocused = focused;
            return m;
        }

        // basic.tsx: 250 px high, 350 wide, Item Three and Four disabled; Item Two highlighted, the menu focused.
        left.Children.Add(M(MenuOracle.Items("BASIC", null), ListStyle(250), highlighted: 1, focused: true));
        // compact.tsx (its first six), dividers.tsx.
        left.Children.Add(M(MenuOracle.Items("BASIC", null).Take(6), ListStyle(more: MenuKit.OptionProps(size: "compact"))));
        right.Children.Add(M(MenuOracle.Items("DIVIDERS", null), ListStyle(), highlighted: 3));
        // grouped.tsx, labelled by id; href.tsx; menu-empty.scenario.tsx; menu-profile-menu.scenario.tsx's first two.
        right.Children.Add(M(MenuOracle.Items("GROUPED", null), ListStyle(width: 200, more: MenuKit.OptionProps(label: "id"))));
        left.Children.Add(M(MenuOracle.Items("HREF", null), ListStyle()));
        left.Children.Add(M([], ListStyle(width: 270)));
        right.Children.Add(M(MenuOracle.Items("PROFILE", null).Take(2), ListStyle(more: MenuKit.OptionProps(profile: true))));

        using var scene = new Scene(new StackPanel { Orientation = Orientation.Horizontal, Children = { left, right } },
            dark ? ThemeVariant.Dark : ThemeVariant.Light, width: 780, height: 900);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        var frame = scene.Capture();
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"menu-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
