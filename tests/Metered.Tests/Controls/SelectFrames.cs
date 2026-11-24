using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of a select: the control's fill at rest and its border focused, a tag's fill, the dropdown's highlighted
/// option, in Base Web's colours where the layout puts them. And, when METERED_FRAMES names a folder, a gallery of Base
/// Web's select examples, light and dark, saved there for docs/controls/Select.
/// </summary>
public class SelectFrames
{
    private static Color At(Scene scene, Avalonia.Media.Imaging.WriteableBitmap frame, Visual v, double x, double y)
    {
        var p = v.TranslatePoint(new Point(x, y), scene.Window)!.Value;
        return Frames.PixelAt(frame, (int)p.X, (int)p.Y);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_control_a_tag_and_a_highlighted_option_are_drawn_in_Base_Webs_colours(string theme)
    {
        var rest = SelectOracle.Data.Colours.Single(c => c.Theme == theme && c.State == "rest");
        var focused = SelectOracle.Data.Colours.Single(c => c.Theme == theme && c.State == "focused");
        var surface = SelectOracle.Data.Surfaces.Single(s => s.Theme == theme);
        var options = SelectOracle.Options("OPTIONS");
        var single = new Select { Options = options };
        var multi = new Select { Options = options, Multi = true, Value = SelectOracle.Value(options, ["Aqua"]) };
        using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Spacing = 20, Children = { multi, single } }, SelectColourTests.Variant(theme));
        var frame = scene.Capture();
        var control = single.P<Border>(Select.ControlContainerPart);
        Assert.Equal(Color.Parse(rest.Background), At(scene, frame, control, control.Bounds.Width * 0.7, control.Bounds.Height / 2));
        var tag = (Visual)multi.Tags[0].Part(SelectTag.RootPart)!;
        Assert.Equal(Color.Parse(surface.Tag.Background), At(scene, frame, tag, 3, tag.Bounds.Height / 2));

        // Opened: the border borderSelected, the first option highlighted (forceHighlight) in the menu's colours.
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, single));
        PopoverTests.Settle(scene, 400);
        frame = scene.Capture();
        Assert.Equal(Color.Parse(focused.Border), At(scene, frame, control, control.Bounds.Width / 2, 1));
        var highlighted = MenuOracle.Data.Colours.Single(c => c.Theme == theme && c.State == "highlightedFocused");
        var first = single.DropdownMenu!.OptionAt(0)!;
        Assert.Equal(Color.Parse(highlighted.Background), At(scene, frame, first, first.Bounds.Width - 6, 6));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;
        var dark = theme == "Dark";
        List<object> O() => SelectOracle.Options("OPTIONS");
        Select S(Action<Select> set, params string[] value)
        {
            var options = O();
            var s = new Select { Options = options, Value = SelectOracle.Value(options, value) };
            set(s);
            return s;
        }

        StackPanel Column() => new() { Width = 360, Spacing = 16, Margin = new Thickness(12) };
        var left = Column();
        var right = Column();
        // controlled.tsx, sizes.tsx's four, search-single-pick.tsx, search-multi-pick.tsx.
        left.Children.Add(S(_ => { }, "Azure"));
        foreach (var size in new[] { SelectSize.Mini, SelectSize.Compact, SelectSize.Default, SelectSize.Large }) left.Children.Add(S(s => s.Size = size, "Aqua"));
        left.Children.Add(S(s => s.Type = SelectType.Search));
        left.Children.Add(S(s => { s.Multi = true; s.Type = SelectType.Search; }, "AliceBlue", "Aqua", "Azure", "Bisque"));
        // select-states.scenario.tsx: error, positive, disabled, loading.
        right.Children.Add(S(s => s.Error = true, "Aqua"));
        right.Children.Add(S(s => s.Positive = true, "Aqua"));
        right.Children.Add(S(s => s.IsEnabled = false, "Aqua"));
        right.Children.Add(S(s => { s.Multi = true; s.IsEnabled = false; }, "Aqua", "Azure"));
        right.Children.Add(S(s => s.IsLoading = true, "Aqua"));
        right.Children.Add(S(s => s.Placeholder = "Choose a colour"));
        // Open, its value highlighted: select-open.scenario.tsx.
        var open = S(s => s.MaxDropdownHeight = "220px", "Aquamarine");
        right.Children.Add(open);
        right.Children.Add(new Border { Height = 230 });

        using var scene = SelectKit.Show(new StackPanel { Orientation = Orientation.Horizontal, Children = { left, right } },
            dark ? ThemeVariant.Dark : ThemeVariant.Light);
        scene.Window.Width = 760;
        scene.Window.Height = 900;
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        scene.Pump();
        open.SetInputFocus();
        open.SetDropdownOpen(true);
        PopoverTests.Settle(scene, 400);
        var frame = scene.Capture();
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"select-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
