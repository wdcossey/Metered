using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The tooltip's parts' themes against Base Web's style functions (TooltipOracle, tooltip/styled-components.js): Body's,
/// Arrow's and Inner's fill, radius, shadow, text colour, Inner's padding and font, light and dark; the body's opacity
/// and its fade by state, opacity alone; a hover body's first 0.1 s.
/// </summary>
public class TooltipColourTests
{
    private static (Tooltip Tooltip, Scene Scene) Open(string theme, Action<Tooltip>? set = null)
    {
        var (tooltip, _, _) = TooltipTests.Fixed(150, 400);
        tooltip.ShowArrow = true;
        set?.Invoke(tooltip);
        tooltip.IsOpen = true;
        var scene = PopoverTests.Show(tooltip, theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        PopoverTests.Settle(scene);
        return (tooltip, scene);
    }

    private static string Hex(IBrush? b) => $"#{((ISolidColorBrush)b!).Color.ToUInt32():X8}";

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void Body_Arrow_and_Inner_are_Base_Webs(string theme)
    {
        var c = TooltipOracle.Value.Colours.Single(x => x.Theme == theme);
        var (tooltip, scene) = Open(theme);
        using var _ = scene;
        var body = (Border)tooltip.Part(Popover.BodyPart)!;
        var arrow = (Border)tooltip.Part(Popover.ArrowPart)!;
        var inner = (ContentPresenter)tooltip.Part(Popover.InnerPart)!;

        Assert.Equal(c.Body.Background, Hex(body.Background));
        Assert.Equal(new CornerRadius(c.Body.Radius!.Value), body.CornerRadius);
        Assert.Equal(BoxShadows.Parse(c.Body.BoxShadow!), body.BoxShadow);
        Assert.Equal(c.Body.Foreground, Hex(body.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.Arrow.Background, Hex(arrow.Background));
        Assert.Equal(BoxShadows.Parse(c.Arrow.BoxShadow!), arrow.BoxShadow);
        Assert.Equal(c.Inner.Background, Hex(inner.Background));
        Assert.Equal(new CornerRadius(c.Inner.Radius), inner.CornerRadius);
        Assert.Equal(c.Inner.Foreground, Hex(inner.Foreground));
        Assert.Equal(new Thickness(c.Inner.Padding[0], c.Inner.Padding[1], c.Inner.Padding[2], c.Inner.Padding[3]), inner.Padding);
        Assert.Equal(c.Inner.Font.FontSize, inner.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(c.Inner.Font.FontWeight), inner.FontWeight);
        Assert.Equal(c.Inner.Font.LineHeight, inner.LineHeight);
        Assert.Equal(FontFamily.Parse(c.Inner.Font.Family), inner.FontFamily);

        // Its arrow: the popover's ARROW_WIDTH square, turned 45°.
        var a = PopoverOracle.Value.Arrow;
        Assert.Equal(TooltipOracle.Value.Constants.ArrowWidth, a.Width);
        Assert.Equal(a.Width, arrow.Width);
        Assert.Equal(a.Height, arrow.Height);
        Assert.Equal(Matrix.CreateRotation(Math.PI * a.Rotate / 180).M12, arrow.RenderTransform!.Value.M12, 6);
    }

    // Its text, a string's, is shown in Inner's font and colour.
    [AvaloniaFact]
    public void Its_text_takes_Inners_font_and_colour()
    {
        var c = TooltipOracle.Value.Colours.Single(x => x.Theme == "Light");
        var (tooltip, scene) = Open("Light", t => t.Content = "Tooltips display short messages.");
        using var _ = scene;
        var text = Avalonia.VisualTree.VisualExtensions.GetVisualDescendants((Visual)tooltip.Part(Popover.InnerPart)!).OfType<TextBlock>().Single();
        Assert.Equal("Tooltips display short messages.", text.Text);
        Assert.Equal(c.Inner.Foreground, Hex(text.Foreground));
        Assert.Equal(c.Inner.Font.FontSize, text.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(c.Inner.Font.FontWeight), text.FontWeight);
        Assert.Equal(c.Inner.Font.LineHeight, text.LineHeight);
    }

    // getBodyStyles' opacity, with the tooltip's transitionProperty: opacity alone, over 0.1 s on easeOutCurve while open
    // and animating; before it, none; at animateOutTime 0 closed at once.
    [AvaloniaFact]
    public void The_body_fades_in_over_Base_Webs_transition_of_its_opacity_alone()
    {
        var m = TooltipOracle.Value.Motion;
        var entering = m.Single(x => x.IsOpen && x.IsAnimating && x.AnimateOutTime == 0 && x.IsHoverTrigger);
        var placed = m.Single(x => x.IsOpen && !x.IsAnimating && x.AnimateOutTime == 0 && x.IsHoverTrigger);
        var gone = m.Single(x => !x.IsOpen && x.IsAnimating && x.AnimateOutTime == 0 && x.IsHoverTrigger);
        Assert.All(m, x => Assert.Equal(["opacity"], x.Properties));
        var (tooltip, _, _) = TooltipTests.Fixed(150, 400);
        using var scene = PopoverTests.Show(tooltip);
        var body = (Border)tooltip.Part(Popover.BodyPart)!;

        tooltip.IsOpen = true;
        scene.Pump();
        Assert.Equal(placed.Opacity, body.Opacity);
        Assert.Equal(0, placed.Seconds);
        Assert.True(body.Transitions is null || body.Transitions.Count == 0);

        PopoverTests.Settle(scene);
        Assert.Equal(entering.Opacity, body.Opacity);
        var t = Assert.Single(body.Transitions!.Cast<TransitionBase>());
        Assert.Equal("Opacity", t.Property!.Name);
        Assert.Equal(TimeSpan.FromSeconds(entering.Seconds), t.Duration);
        var e = (SplineEasing)t.Easing;
        Assert.Equal(entering.Easing, new[] { e.X1, e.Y1, e.X2, e.Y2 });

        tooltip.IsOpen = false;
        scene.Pump();
        Assert.Equal(gone.Opacity, body.Opacity);
        Assert.Equal(0, gone.Seconds);
        Assert.True(body.Transitions is null || body.Transitions.Count == 0);
    }

    // At an animateOutTime, its exit is its opacity's alone, over that time on easeInCurve.
    [AvaloniaFact]
    public void Its_exit_fades_its_opacity_alone()
    {
        var leaving = TooltipOracle.Value.Motion.Single(x => !x.IsOpen && x.IsAnimating && x.AnimateOutTime == 300 && x.IsHoverTrigger);
        var (tooltip, scene) = Open("Light", t => t.AnimateOutTime = 300);
        using var _ = scene;
        var body = (Border)tooltip.Part(Popover.BodyPart)!;
        tooltip.IsOpen = false;
        scene.Pump();
        var t = Assert.Single(body.Transitions!.Cast<TransitionBase>());
        Assert.Equal("Opacity", t.Property!.Name);
        Assert.Equal(TimeSpan.FromSeconds(leaving.Seconds), t.Duration);
        var e = (SplineEasing)t.Easing;
        Assert.Equal(leaving.Easing, new[] { e.X1, e.Y1, e.X2, e.Y2 });
    }

    // getBodyStyles' animationName for a hover trigger, a tooltip's default: no pointer to 99% of 0.1 s, then the pointer.
    [AvaloniaFact]
    public void Its_body_takes_no_pointer_for_its_first_tenth_of_a_second()
    {
        var m = TooltipOracle.Value.Motion.First(x => x.IsHoverTrigger && x.HitTest is not null).HitTest!;
        Assert.Equal([false, false, true], m.Frames.Select(f => f[1].GetBoolean()));
        var (tooltip, scene) = Open("Light");
        using var _ = scene;
        var body = (Border)tooltip.Part(Popover.BodyPart)!;
        tooltip.IsOpen = false;
        PopoverTests.Settle(scene, 100);
        tooltip.IsOpen = true;
        scene.Pump();
        Assert.False(body.IsHitTestVisible);
        PopoverTests.Settle(scene, (int)(m.Seconds * 1000) + 150);
        Assert.True(body.IsHitTestVisible);
    }
}
