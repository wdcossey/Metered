using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Metered.Controls;
using Metered.Themes;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of the popover: its body and its arrow's tip in Base Web's fill, where the placement puts them, over the
/// page. And, when METERED_FRAMES names a folder, a gallery of Base Web's documented placements, with and without the
/// arrow, light and dark, saved there for docs/controls/Popover.
/// </summary>
public class PopoverFrames
{
    private static Scene Show(Control content, ThemeVariant variant, double width = Scene.Width, double height = Scene.Height) =>
        new(content, variant, width, height, [new SimpleTheme(), new BaseTheme()]);

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_body_and_the_arrow_are_drawn_in_Base_Webs_fill_against_the_anchor(string theme)
    {
        var c = PopoverOracle.Value.Colours.Single(x => x.Theme == theme);
        var (popover, anchor, _) = PopoverTests.Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        popover.ShowArrow = true;
        popover.IsOpen = true;
        using var scene = Show(popover, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        scene.Window.Background = theme == "Dark" ? Brushes.Black : Brushes.White;
        PopoverTests.Settle(scene);
        var frame = scene.Capture();
        var body = PopoverTests.BoxOf((Visual)popover.Part(Popover.BodyPart)!, scene);
        Assert.Equal(Color.Parse(c.Body.Background), Frames.PixelAt(frame, (int)body.Center.X, (int)body.Center.Y));
        // The arrow's tip, above the body's top edge, under the anchor's centre (195): its fill, drawn over the page.
        var tip = Frames.PixelAt(frame, 195, (int)body.Top - 3);
        Assert.Equal(Color.Parse(c.Arrow.Background), tip);
        // Beyond the tip, the page.
        Assert.NotEqual(Color.Parse(c.Arrow.Background), Frames.PixelAt(frame, 195, (int)body.Top - 8));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Gallery(string theme)
    {
        var folder = Environment.GetEnvironmentVariable(ButtonFrames.Variable);
        if (string.IsNullOrEmpty(folder)) return;
        var dark = theme == "Dark";

        // Base Web's documentation's placements example: each placement round its own button, with and without the arrow.
        var grid = new Canvas { Width = 780, Height = 860 };
        var places = new (PopoverPlacement Placement, double X, double Y)[]
        {
            (PopoverPlacement.TopLeft, 40, 110), (PopoverPlacement.Top, 300, 110), (PopoverPlacement.TopRight, 560, 110),
            (PopoverPlacement.LeftTop, 200, 300), (PopoverPlacement.Right, 420, 300),
            (PopoverPlacement.Left, 200, 420), (PopoverPlacement.RightBottom, 420, 420),
            (PopoverPlacement.BottomLeft, 40, 560), (PopoverPlacement.Bottom, 300, 560), (PopoverPlacement.BottomRight, 560, 560),
        };
        var i = 0;
        foreach (var (placement, x, y) in places)
        {
            var popover = new Popover
            {
                Anchor = new Metered.Controls.Button { Content = placement.ToString(), Size = ButtonSize.Compact, Kind = ButtonKind.Secondary },
                Content = new Border { Theme = null, Padding = new Thickness(12), Child = new TextBlock { Text = "Content" } },
                Placement = placement, ShowArrow = i++ % 2 == 0, IsOpen = true, AutoFocus = false,
            };
            Canvas.SetLeft(popover, x);
            Canvas.SetTop(popover, y);
            grid.Children.Add(popover);
        }

        using var scene = Show(grid, dark ? ThemeVariant.Dark : ThemeVariant.Light, 780, 860);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        PopoverTests.Settle(scene, 300);
        var frame = scene.Capture();
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"popover-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
