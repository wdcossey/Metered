using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Metered.Controls;
using Metered.Themes;

namespace Metered.Tests.Controls;

/// <summary>
/// Drawn pixels of the tooltip: its body and its arrow's tip in Base Web's tooltipBackground, light and dark, where the
/// placement puts them, over the page; its text in tooltipText. And, when METERED_FRAMES names a folder, a gallery of
/// tooltips at Base Web's placements, with and without the arrow, light and dark, saved there for docs/controls/Tooltip.
/// </summary>
public class TooltipFrames
{
    private static Scene Show(Control content, ThemeVariant variant, double width = Scene.Width, double height = Scene.Height) =>
        new(content, variant, width, height, [new SimpleTheme(), new BaseTheme()]);

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_body_its_arrow_and_its_text_are_drawn_in_Base_Webs_colours(string theme)
    {
        var c = TooltipOracle.Value.Colours.Single(x => x.Theme == theme);
        var (tooltip, _, _) = TooltipTests.Fixed(150, 400);
        // A solid block of text colour in place of glyphs, so its pixel is the colour exactly.
        tooltip.Content = new Border { Width = 120, Height = 30, Child = new Border { Width = 40, Height = 10, [!Border.BackgroundProperty] = new Avalonia.Data.Binding("Foreground") { RelativeSource = new Avalonia.Data.RelativeSource { Mode = Avalonia.Data.RelativeSourceMode.FindAncestor, AncestorType = typeof(Avalonia.Controls.Presenters.ContentPresenter) } } } };
        tooltip.Placement = PopoverPlacement.Bottom;
        tooltip.ShowArrow = true;
        tooltip.IsOpen = true;
        using var scene = Show(tooltip, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        scene.Window.Background = theme == "Dark" ? Brushes.Black : Brushes.White;
        PopoverTests.Settle(scene);
        var frame = scene.Capture();
        var body = PopoverTests.BoxOf((Visual)tooltip.Part(Popover.BodyPart)!, scene);
        // Inside its padding, off the content: the fill.
        Assert.Equal(Color.Parse(c.Body.Background), Frames.PixelAt(frame, (int)body.Left + 6, (int)body.Center.Y));
        // The block in the content's middle: Inner's text colour.
        Assert.Equal(Color.Parse(c.Inner.Foreground), Frames.PixelAt(frame, (int)body.Center.X, (int)body.Center.Y));
        // The arrow's tip, above the body's top edge, under the anchor's centre (195): its fill, over the page.
        Assert.Equal(Color.Parse(c.Arrow.Background), Frames.PixelAt(frame, 195, (int)body.Top - 3));
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

        var grid = new Canvas { Width = 780, Height = 600 };
        var places = new (PopoverPlacement Placement, double X, double Y)[]
        {
            (PopoverPlacement.TopLeft, 40, 80), (PopoverPlacement.Top, 300, 80), (PopoverPlacement.TopRight, 560, 80),
            (PopoverPlacement.LeftTop, 220, 250), (PopoverPlacement.Right, 420, 250),
            (PopoverPlacement.BottomLeft, 40, 420), (PopoverPlacement.Bottom, 300, 420), (PopoverPlacement.BottomRight, 560, 420),
        };
        var i = 0;
        foreach (var (placement, x, y) in places)
        {
            var tooltip = new Tooltip
            {
                Anchor = new Metered.Controls.Button { Content = placement.ToString(), Size = ButtonSize.Compact, Kind = ButtonKind.Secondary },
                Content = "Tooltips display short messages.",
                Placement = placement, ShowArrow = i++ % 2 == 1, IsOpen = true,
            };
            Canvas.SetLeft(tooltip, x);
            Canvas.SetTop(tooltip, y);
            grid.Children.Add(tooltip);
        }

        using var scene = Show(grid, dark ? ThemeVariant.Dark : ThemeVariant.Light, 780, 600);
        scene.Window.Background = dark ? Brushes.Black : Brushes.White;
        PopoverTests.Settle(scene, 300);
        var frame = scene.Capture();
        Directory.CreateDirectory(folder);
        frame.Save(Path.Combine(folder, $"tooltip-gallery-{theme.ToLowerInvariant()}.png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
    }
}
