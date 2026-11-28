using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using MeteredButton = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>
/// The tooltip's behaviour against Base Web's (tooltip/default-props.js, tooltip.js, and the popover's popover.js and
/// stateful-container.js it renders): its defaults; a mouse's hover with its delays, a finger's tap toggling it (#123),
/// keyboard focus and the blur, Escape and Back; no focus taken nor locked; UI Automation; its template, themes and
/// overrides. What it shares with the Popover unchanged is held by PopoverTests.
/// </summary>
public class TooltipTests
{
    private static TooltipDefaults Defaults => TooltipOracle.Value.Defaults.Stateful;

    /// <summary>A tooltip round a fixed box at a place in the window, with content of a fixed size.</summary>
    internal static (Tooltip Tooltip, Border Anchor, Border Content) Fixed(double x, double y, double width = 90, double height = 40,
        double contentWidth = 120, double contentHeight = 60)
    {
        var anchor = new Border { Width = width, Height = height, Background = Brushes.Transparent };
        var content = new Border { Width = contentWidth, Height = contentHeight };
        var tooltip = new Tooltip
        {
            Anchor = anchor, Content = content, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(x, y, 0, 0),
        };
        return (tooltip, anchor, content);
    }

    private static Visual Body(Popover p) => (Visual)p.Part(Popover.BodyPart)!;

    private static bool Shown(Popover p) => p.PopupHost!.IsOpen && Body(p).Opacity > 0;

    private static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    private static string Camel(PopoverPlacement p) => char.ToLowerInvariant(p.ToString()[0]) + p.ToString()[1..];

    // ---- Defaults ----

    // StatefulTooltip's, as React resolves them: tooltip/default-props.js, then StatefulPopover's container's (the
    // dismissals), then Popover's (ignoreBoundary). A Tooltip's own are the same where it has them.
    [AvaloniaFact]
    public void Its_defaults_are_Base_Webs_tooltips()
    {
        var t = new Tooltip();
        Assert.Equal(Defaults.Placement, Camel(t.Placement));
        Assert.Equal(Defaults.TriggerType, t.TriggerType.ToString().ToLowerInvariant());
        Assert.Equal(Defaults.AccessibilityType, t.AccessibilityType.ToString().ToLowerInvariant());
        Assert.Equal(Defaults.ShowArrow, t.ShowArrow);
        Assert.Equal(Defaults.FocusLock, t.FocusLock);
        Assert.Equal(Defaults.AutoFocus, t.AutoFocus);
        Assert.Equal(Defaults.ReturnFocus, t.ReturnFocus);
        Assert.Equal(Defaults.IgnoreBoundary, t.IgnoreBoundary);
        Assert.Equal(Defaults.OnMouseEnterDelay, t.MouseEnterDelay);
        Assert.Equal(Defaults.OnMouseLeaveDelay, t.MouseLeaveDelay);
        Assert.Equal(Defaults.DismissOnClickOutside, t.DismissOnClickOutside);
        Assert.Equal(Defaults.DismissOnEsc, t.DismissOnEsc);
        Assert.Equal(Defaults.PopoverMargin, t.PopoverMargin);
        // animateOutTime unset: popover.js's ANIMATE_OUT_TIME.
        Assert.Null(Defaults.AnimateOutTime);
        Assert.Equal(PopoverOracle.Value.Constants.AnimateOutTime, t.AnimateOutTime);
        Assert.False(t.IsOpen);

        var controlled = TooltipOracle.Value.Defaults.Tooltip;
        Assert.Equal(Defaults with { DismissOnClickOutside = null, DismissOnEsc = null }, controlled);

        // A popover's are its own still.
        var p = new Popover();
        Assert.Equal(PopoverOracle.Value.Defaults.TriggerType, p.TriggerType.ToString().ToLowerInvariant());
        Assert.Equal(PopoverOracle.Value.Defaults.PopoverMargin, p.PopoverMargin);
        Assert.Equal(PopoverOracle.Value.Defaults.AutoFocus, p.AutoFocus);
    }

    // ---- Its template and themes ----

    // The Popover's parts, each with the tooltip's theme, Tooltip.Styled{Part}; a style for Popovers isn't a tooltip's.
    [AvaloniaFact]
    public void Its_parts_are_the_popovers_with_the_tooltips_themes()
    {
        var (tooltip, _, _) = Fixed(150, 400);
        tooltip.ShowArrow = true;
        tooltip.IsOpen = true;
        var page = new Panel { Children = { tooltip } };
        page.Styles.Add(new Style(x => x.OfType<Popover>()) { Setters = { new Setter(Popover.ShowArrowProperty, false) } });
        using var scene = PopoverTests.Show(page);
        PopoverTests.Settle(scene);
        foreach (var part in new[] { Popover.BodyPart, Popover.ArrowPart, Popover.InnerPart })
        {
            var element = tooltip.Part(part);
            Assert.NotNull(element);
            Assert.Same(tooltip.FindResource($"Tooltip.Styled{part}"), element.Theme);
        }

        Assert.True(tooltip.ShowArrow);
        Assert.Contains(":is-open", tooltip.Classes);
        Assert.Contains(":is-hover-trigger", tooltip.Part(Popover.BodyPart)!.Classes);
    }

    [AvaloniaFact]
    public void Overrides_of_Body_Arrow_and_Inner_win_over_its_themes()
    {
        var (tooltip, _, _) = Fixed(150, 400);
        tooltip.ShowArrow = true;
        var warning = new SolidColorBrush(Colors.Orange);
        tooltip.Overrides =
        [
            new Override
            {
                Part = "Body",
                Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Border.BackgroundProperty, warning), new Setter(Border.CornerRadiusProperty, new CornerRadius(2)) } },
            },
            new Override { Part = "Arrow", Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Border.BackgroundProperty, warning) } } },
            new Override { Part = "Inner", Style = new ControlTheme(typeof(ContentPresenter)) { Setters = { new Setter(ContentPresenter.PaddingProperty, new Thickness(3)) } } },
        ];
        tooltip.IsOpen = true;
        using var scene = PopoverTests.Show(tooltip);
        PopoverTests.Settle(scene);
        var c = TooltipOracle.Value.Colours.Single(x => x.Theme == "Light");
        Assert.Same(warning, ((Border)tooltip.Part("Body")!).Background);
        Assert.Equal(new CornerRadius(2), ((Border)tooltip.Part("Body")!).CornerRadius);
        Assert.Same(warning, ((Border)tooltip.Part("Arrow")!).Background);
        var inner = (ContentPresenter)tooltip.Part("Inner")!;
        Assert.Equal(new Thickness(3), inner.Padding);
        // What the override leaves is the tooltip's: Inner's font, the body's opacity state.
        Assert.Equal(c.Inner.Font.FontSize, inner.FontSize);
        Assert.Equal(1, Body(tooltip).Opacity);
    }

    // ---- What opens and closes it ----

    // popover.js's hover trigger at the tooltip's delays: a mouse resting on the anchor opens it after onMouseEnterDelay,
    // and leaving closes it after onMouseLeaveDelay.
    [AvaloniaFact]
    public void A_mouse_opens_it_after_the_enter_delay_and_closes_it_after_the_leave_delay()
    {
        var (tooltip, anchor, _) = Fixed(150, 400);
        using var scene = PopoverTests.Show(tooltip);
        scene.Hover(anchor);
        PopoverTests.Settle(scene, Defaults.OnMouseEnterDelay - 20);
        Assert.False(tooltip.IsOpen);
        PopoverTests.Settle(scene, 40);
        Assert.True(tooltip.IsOpen);
        PopoverTests.Settle(scene, 200);
        Assert.True(Shown(tooltip));

        scene.Window.MouseMove(new Point(20, 20));
        scene.Pump();
        PopoverTests.Settle(scene, Defaults.OnMouseLeaveDelay - 20);
        Assert.True(tooltip.IsOpen);
        PopoverTests.Settle(scene, 40);
        Assert.False(tooltip.IsOpen);
    }

    // A click on the anchor is a click trigger's, which a tooltip isn't: a mouse's press and release open nothing at once.
    [AvaloniaFact]
    public void A_click_on_the_anchor_doesnt_toggle_it()
    {
        var button = new MeteredButton { Content = "Button" };
        var tooltip = new Tooltip { Anchor = button, Content = "Tooltips display short messages.", Margin = new Thickness(100), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = PopoverTests.Show(tooltip);
        scene.Window.MouseMove(scene.CentreOf(button));
        PopoverTests.Settle(scene, 400);
        Assert.True(tooltip.IsOpen);
        scene.Click(button);
        PopoverTests.Settle(scene, 50);
        Assert.True(tooltip.IsOpen);
    }

    // A finger never hovers: a tap on the anchor toggles it, and a tap elsewhere closes one a tap opened (#123).
    [AvaloniaFact]
    public void A_tap_on_the_anchor_toggles_it_and_a_tap_elsewhere_closes_it()
    {
        var (tooltip, anchor, _) = Fixed(150, 400);
        using var scene = PopoverTests.Show(tooltip);
        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        Assert.True(Shown(tooltip));
        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene, 50);
        Assert.False(tooltip.IsOpen);

        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        Tap(scene, new Point(20, 700));
        Assert.False(tooltip.IsOpen);
    }

    // stateful-container.js: keyboard focus on the anchor opens it; with the tooltip's autoFocus false, focus leaving the
    // anchor closes it.
    [AvaloniaFact]
    public void Keyboard_focus_on_the_anchor_opens_it_and_leaving_it_closes_it()
    {
        var button = new MeteredButton { Content = "Button" };
        var other = new MeteredButton { Content = "Other" };
        var tooltip = new Tooltip { Anchor = button, Content = "Tooltips display short messages." };
        using var scene = PopoverTests.Show(new StackPanel { Margin = new Thickness(40), Spacing = 200, Children = { tooltip, other } });
        button.Focus(NavigationMethod.Pointer);
        scene.Pump();
        Assert.False(tooltip.IsOpen);
        other.Focus(NavigationMethod.Pointer);
        button.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(tooltip.IsOpen);
        other.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.False(tooltip.IsOpen);
    }

    // StatefulPopover's dismissOnEsc, which a StatefulTooltip keeps: Escape closes it; and Avalonia's back request (#123).
    [AvaloniaFact]
    public void Escape_and_Back_close_it()
    {
        var (tooltip, anchor, _) = Fixed(150, 400);
        using var scene = PopoverTests.Show(tooltip);
        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        scene.Key(Key.Escape);
        Assert.False(tooltip.IsOpen);
        PopoverTests.Settle(scene, 100);

        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        Assert.True(Metered.Tests.Sample.ShellFrames.Back(scene.Window));
        Assert.False(tooltip.IsOpen);
    }

    // ---- Focus ----

    // popover.js: neither autoFocus nor focusLock is a tooltip's, whatever they say: focus stays where it was, and Tab
    // isn't kept inside.
    [AvaloniaFact]
    public void It_never_takes_focus_nor_locks_it()
    {
        var field = new TextBox();
        var (tooltip, anchor, _) = Fixed(150, 400);
        tooltip.Content = new StackPanel { Children = { new TextBlock { Text = "Name" }, field } };
        tooltip.AutoFocus = true;
        tooltip.FocusLock = true;
        using var scene = PopoverTests.Show(tooltip);
        Tap(scene, scene.CentreOf(anchor));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        Assert.False(field.IsFocused);
        Assert.NotEqual(KeyboardNavigationMode.Cycle, KeyboardNavigation.GetTabNavigation((InputElement)Body(tooltip)));

        // A popover with the same props takes it, as Base Web's.
        var popoverField = new TextBox();
        var (popover, popoverAnchor, _) = PopoverTests.Fixed(150, 400);
        popover.Content = new StackPanel { Children = { popoverField } };
        popover.FocusLock = true;
        using var popoverScene = PopoverTests.Show(popover);
        popoverScene.Click(popoverAnchor);
        PopoverTests.Settle(popoverScene);
        Assert.True(popoverField.IsFocused);
    }

    // ---- UI Automation ----

    // popover.js's getPopoverBodyProps and getAnchorProps for a tooltip: the body's role is tooltip, and the anchor is
    // described by it while it is open (aria-describedby).
    [AvaloniaFact]
    public void Its_body_is_a_tooltip_that_describes_its_anchor_while_open()
    {
        var button = new MeteredButton { Content = "Button" };
        var tooltip = new Tooltip { Anchor = button, Content = "Tooltips display short messages.", Margin = new Thickness(100), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
        using var scene = PopoverTests.Show(tooltip);
        Assert.Null(AutomationProperties.GetHelpText(button));
        Tap(scene, scene.CentreOf(button));
        PopoverTests.Settle(scene);
        Assert.True(tooltip.IsOpen);
        var body = (Control)Body(tooltip);
        Assert.Equal(AutomationControlType.ToolTip, ControlAutomationPeer.CreatePeerForElement(body).GetAutomationControlType());
        Assert.Equal("Tooltips display short messages.", AutomationProperties.GetHelpText(button));
        Tap(scene, scene.CentreOf(button));
        PopoverTests.Settle(scene);
        Assert.False(tooltip.IsOpen);
        Assert.Null(AutomationProperties.GetHelpText(button));
    }
}
