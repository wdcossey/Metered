using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The popover's parts' themes against Base Web's style functions (PopoverOracle): Body's, Arrow's and Inner's fill, radius,
/// shadow and text colour, light and dark; the body's opacity and transitions by state; a hover body's first 0.1 s.
/// </summary>
public class PopoverColourTests
{
    private static (Popover Popover, Scene Scene) Open(string theme, Action<Popover>? set = null)
    {
        var (popover, _, _) = PopoverTests.Fixed(150, 400);
        popover.ShowArrow = true;
        set?.Invoke(popover);
        popover.IsOpen = true;
        var scene = PopoverTests.Show(popover, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        PopoverTests.Settle(scene);
        return (popover, scene);
    }

    private static string Hex(IBrush? b) => $"#{((ISolidColorBrush)b!).Color.ToUInt32():X8}";

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Body_Arrow_and_Inner_are_Base_Webs(string theme)
    {
        var c = PopoverOracle.Value.Colours.Single(x => x.Theme == theme);
        var (popover, scene) = Open(theme);
        using var _ = scene;
        var body = (Border)popover.Part(Popover.BodyPart)!;
        var arrow = (Border)popover.Part(Popover.ArrowPart)!;
        var inner = (ContentPresenter)popover.Part(Popover.InnerPart)!;

        Assert.Equal(c.Body.Background, Hex(body.Background));
        Assert.Equal(new CornerRadius(c.Body.Radius!.Value), body.CornerRadius);
        Assert.Equal(BoxShadows.Parse(c.Body.BoxShadow!), body.BoxShadow);
        Assert.Equal(c.Arrow.Background, Hex(arrow.Background));
        Assert.Equal(BoxShadows.Parse(c.Arrow.BoxShadow!), arrow.BoxShadow);
        Assert.Equal(c.Inner.Background, Hex(inner.Background));
        Assert.Equal(new CornerRadius(c.Inner.Radius!.Value), inner.CornerRadius);
        Assert.Equal(c.Inner.Foreground, Hex(inner.Foreground));

        // The arrow: ARROW_WIDTH square, turned 45°.
        var a = PopoverOracle.Value.Arrow;
        Assert.Equal(a.Width, arrow.Width);
        Assert.Equal(a.Height, arrow.Height);
        var turned = arrow.RenderTransform!.Value;
        Assert.Equal(Matrix.CreateRotation(Math.PI * a.Rotate / 180).M12, turned.M12, 6);
    }

    // Base Web's StyledPadding, a theme for a Border.
    [AvaloniaFact]
    public void Padding_is_Base_Webs_StyledPadding()
    {
        var p = PopoverOracle.Value.Padding;
        var border = new Border();
        using var scene = PopoverTests.Show(border);
        border.Theme = (ControlTheme)border.FindResource("Popover.StyledPadding")!;
        scene.Pump();
        Assert.Equal(new Thickness(p[0], p[1], p[2], p[3]), border.Padding);
    }

    // getBodyStyles: opacity 1 and the entrance's transition only while open and animating; before it, none.
    [AvaloniaFact]
    public void The_body_enters_over_Base_Webs_transition_and_has_none_before()
    {
        var entering = PopoverOracle.Value.Motion.Single(x => x.IsOpen && x.IsAnimating && x.AnimateOutTime == 0 && !x.IsHoverTrigger);
        var placed = PopoverOracle.Value.Motion.Single(x => x.IsOpen && !x.IsAnimating && x.AnimateOutTime == 0 && !x.IsHoverTrigger);
        var gone = PopoverOracle.Value.Motion.Single(x => !x.IsOpen && x.IsAnimating && x.AnimateOutTime == 0 && !x.IsHoverTrigger);
        var (popover, anchor, _) = PopoverTests.Fixed(150, 400);
        using var scene = PopoverTests.Show(popover);
        var body = (Border)popover.Part(Popover.BodyPart)!;

        scene.Click(anchor);
        Assert.Equal(placed.Opacity, body.Opacity);
        Assert.Equal(0, placed.Seconds);
        Assert.True(body.Transitions is null || body.Transitions.Count == 0);

        PopoverTests.Settle(scene);
        Assert.Equal(entering.Opacity, body.Opacity);
        var transitions = body.Transitions!;
        Assert.Equal(["Opacity", "RenderTransform"], transitions.Select(t => ((TransitionBase)t).Property!.Name).Order());
        foreach (var t in transitions.Cast<TransitionBase>())
        {
            Assert.Equal(TimeSpan.FromSeconds(entering.Seconds), t.Duration);
            var e = (SplineEasing)t.Easing;
            Assert.Equal(entering.Easing, new[] { e.X1, e.Y1, e.X2, e.Y2 });
        }

        // Closing at animateOutTime 0: at once, no transition.
        popover.IsOpen = false;
        scene.Pump();
        Assert.Equal(gone.Opacity, body.Opacity);
        Assert.Equal(0, gone.Seconds);
        Assert.True(body.Transitions is null || body.Transitions.Count == 0);
    }

    // getBodyStyles' animationName for a hover trigger: no pointer to 99% of 0.1 s, then the pointer.
    [AvaloniaFact]
    public void A_hover_body_takes_no_pointer_for_its_first_tenth_of_a_second()
    {
        var m = PopoverOracle.Value.Motion.First(x => x.IsHoverTrigger && x.HitTest is not null).HitTest!;
        Assert.Equal(0.1, m.Seconds);
        Assert.Equal([false, false, true], m.Frames.Select(f => f[1].GetBoolean()));
        var (popover, scene) = Open("Light", p => p.TriggerType = PopoverTriggerType.Hover);
        using var _ = scene;
        var body = (Border)popover.Part(Popover.BodyPart)!;
        Assert.True(body.IsHitTestVisible);
        popover.IsOpen = false;
        PopoverTests.Settle(scene, 100);
        popover.IsOpen = true;
        scene.Pump();
        Assert.False(body.IsHitTestVisible);
        PopoverTests.Settle(scene, (int)(m.Seconds * 1000) + 150);
        Assert.True(body.IsHitTestVisible);

        // A click popover's body takes the pointer at once.
        var (click, clickScene) = Open("Light");
        using var __ = clickScene;
        click.IsOpen = false;
        PopoverTests.Settle(clickScene, 100);
        click.IsOpen = true;
        clickScene.Pump();
        Assert.True(((Border)click.Part(Popover.BodyPart)!).IsHitTestVisible);
    }
}
