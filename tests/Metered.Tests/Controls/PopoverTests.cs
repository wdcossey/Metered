using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Metered.Controls;
using Metered.Themes;

namespace Metered.Tests.Controls;

/// <summary>The popover's behaviour against Base Web's: opening, placement, motion, dismissal, focus, by real input.</summary>
public class PopoverTests
{
    /// <summary>A window with a template (SimpleTheme's): the overlay layer a popover is shown in, as an app's window has.</summary>
    internal static Scene Show(Control content) => new(content, themes: [new SimpleTheme(), new BaseTheme()]);

    /// <summary>A popover round a fixed box at a place in the window, with content of a fixed size.</summary>
    internal static (Popover Popover, Border Anchor, Border Content) Fixed(double x, double y, double width = 90, double height = 40,
        double contentWidth = 120, double contentHeight = 60)
    {
        var anchor = new Border { Width = width, Height = height, Background = Avalonia.Media.Brushes.Transparent };
        var content = new Border { Width = contentWidth, Height = contentHeight };
        var popover = new Popover
        {
            Anchor = anchor, Content = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(x, y, 0, 0),
        };
        return (popover, anchor, content);
    }

    /// <summary>
    /// Runs the dispatcher's loop for a while, so its timers fire (the entrance's 20 ms, the hover delays) and transitions
    /// run: headless, a timer fires only in the loop, not from RunJobs.
    /// </summary>
    internal static void Settle(Scene scene, int ms = 200)
    {
        Dispatcher.UIThread.MainLoop(new CancellationTokenSource(ms).Token);
        scene.Pump();
    }

    internal static Rect BoxOf(Visual v, Scene scene) => new(v.TranslatePoint(default, scene.Window)!.Value, v.Bounds.Size);

    [AvaloniaFact]
    public void A_click_on_the_anchor_opens_it_below_where_popper_puts_it()
    {
        var (popover, anchor, _) = Fixed(150, 400);
        popover.Placement = PopoverPlacement.Bottom;
        using var scene = Show(popover);
        scene.Click(anchor);
        Settle(scene);
        Assert.True(popover.IsOpen);
        var body = (Visual)popover.Part(Popover.BodyPart)!;
        // popper.js: bottom of a 90 × 40 anchor at (150, 400), a 120 × 60 body with margin-top 8: (135, 440), then the margin.
        Assert.Equal(new Rect(135, 448, 120, 60), BoxOf(body, scene));
        Assert.Equal(1, body.Opacity);
    }
}
