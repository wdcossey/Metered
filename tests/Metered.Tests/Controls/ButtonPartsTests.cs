using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>The loading spinner, the enhancers, and the Button's behaviour.</summary>
public class ButtonPartsTests
{
    public static TheoryData<int> SpinnerCases => new(Enumerable.Range(0, Oracle.Button.Spinner.Length));

    [AvaloniaTheory]
    [MemberData(nameof(SpinnerCases))]
    public void Spinner_matches_Base_Web(int index)
    {
        var c = Oracle.Button.Spinner[index];
        var button = new Button
        {
            Content = "Button",
            IsLoading = true,
            Kind = Enum.Parse<ButtonKind>(c.Kind, ignoreCase: true),
            Size = Enum.Parse<ButtonSize>(c.Size, ignoreCase: true),
            IsEnabled = !c.Disabled,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        var spinner = (ButtonLoadingSpinner)button.Part(Button.LoadingSpinnerPart)!;
        var container = (Panel)button.Part(Button.LoadingSpinnerContainerPart)!;
        Assert.True(container.IsVisible);
        Assert.Equal(c.Diameter, spinner.Bounds.Width, 3);
        Assert.Equal(c.Diameter, spinner.Bounds.Height, 3);
        Assert.Equal(c.Thickness, spinner.Thickness);
        Assert.Equal(c.Foreground, ButtonColourTests.Colour(spinner.Foreground));
        Assert.Equal(c.Background, ButtonColourTests.Colour(spinner.Background));
        Assert.Equal(new Thickness(c.ContainerMargin[0], c.ContainerMargin[1], c.ContainerMargin[2], c.ContainerMargin[3]), container.Margin);
    }

    [AvaloniaTheory]
    [InlineData(ButtonSize.XSmall)]
    [InlineData(ButtonSize.Small)]
    [InlineData(ButtonSize.Medium)]
    [InlineData(ButtonSize.Large)]
    public void Loading_keeps_the_button_size_and_hides_the_content(ButtonSize size)
    {
        var plain = new Button { Content = "Loading", Size = size };
        var loading = new Button { Content = "Loading", Size = size, IsLoading = true };
        using var scene = new Scene(new StackPanel { Children = { plain, loading } });
        Assert.Equal(plain.Bounds.Size, loading.Bounds.Size);
        Assert.False(((Panel)plain.Part(Button.LoadingSpinnerContainerPart)!).IsVisible);
        var content = loading.GetVisualDescendants().OfType<DockPanel>().Single(d => d.Name == "PART_Content");
        Assert.Equal(0, content.Opacity);
    }

    [AvaloniaFact]
    public void A_loading_button_doesnt_click()
    {
        var clicks = 0;
        var button = new Button { Content = "Go", IsLoading = true };
        button.Click += (_, _) => clicks++;
        using var scene = new Scene(new StackPanel { Children = { button } });
        scene.Click(button);
        Assert.Equal(0, clicks);
        button.IsLoading = false;
        scene.Click(button);
        Assert.Equal(1, clicks);
    }

    // The ring's turn, Base Web's LoadingSpinner (button/styled-components.js): rotate(0deg) to rotate(360deg), on
    // Avalonia's clock, here the test's. Its angle moves between two frames 150 ms apart while the button loads.
    [AvaloniaFact]
    public void The_loading_ring_turns()
    {
        var (ring, scene) = Loading();
        using var _ = scene;
        var first = Angle(ring, scene);
        // 150 ms on the test's clock: a turn that never runs stays at its first angle.
        scene.Advance(150);
        var moved = Angle(ring, scene) != first;

        Assert.True(moved, $"the ring stayed at {first} degrees");
    }

    // :metered-still, which PreviewGallery sets before a picture: the turn stops, the ring at 0deg, Base Web's from
    // (button/styled-components.js, LoadingSpinner's animationName).
    [AvaloniaFact]
    public void A_still_loading_ring_stands_at_its_start()
    {
        const double from = 0;
        var (ring, scene) = Loading();
        using var _ = scene;
        // Turning first, so standing at 0 below is the hold's doing.
        scene.Advance(150);
        var turned = Angle(ring, scene) != from;

        Assert.True(turned, "the ring never turned");
        SpinnerColourTests.Still(ring);
        Assert.Equal(from, Angle(ring, scene));
        scene.Advance(150);
        Assert.Equal(from, Angle(ring, scene));
    }

    // Base Web's LoadingSpinner (button/styled-components.js) sets no transform at rest, only its animation's (#77), in
    // none of its sizes, kinds or states.
    [AvaloniaFact]
    public void The_loading_ring_sets_no_transform_at_rest()
    {
        using var scene = new Scene(new StackPanel());
        var theme = RingTheme(scene);
        Assert.DoesNotContain(Setters(theme.Setters, theme.Children), s => s.Property == Visual.RenderTransformProperty);
    }

    // Avalonia 12.1.4's TransformAnimator ignores a turn when the RenderTransform is a TransformOperations, as a setter's
    // rotate(0deg) is. Within the theme, the look and the turn attach in one styling batch, before any value is set, so
    // the turn wins there whatever the theme sets. Here the ring's look at rest (the theme's setters for its state)
    // lands first, in a style of its own, and the theme with its turn comes after: the ring must still turn.
    [AvaloniaFact]
    public void The_loading_ring_turns_when_its_at_rest_look_lands_first()
    {
        string[] states = [":is-loading", ":size-medium", ":kind-primary"];
        var ring = new ButtonLoadingSpinner();
        foreach (var state in states) ((IPseudoClasses)ring.Classes).Set(state, true);
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { ring } });
        var theme = RingTheme(scene);
        // The root's setters and those of each nested style that only asks for some of the ring's states and turns nothing.
        var rest = new Style(x => x.OfType<ButtonLoadingSpinner>());
        var matching = theme.Children.OfType<Style>().Where(s => s.Animations.Count == 0
            && System.Text.RegularExpressions.Regex.Match(s.Selector?.ToString() ?? "", @"^\^((?::[a-z-]+)+)$") is { Success: true } m
            && m.Groups[1].Value.Split(':', StringSplitOptions.RemoveEmptyEntries).All(p => states.Contains(":" + p)));
        foreach (var setter in Setters(theme.Setters, matching)) rest.Setters.Add(new Setter(setter.Property!, setter.Value!));
        Assert.NotEmpty(rest.Setters);
        scene.Window.Styles.Add(rest);
        scene.Pump();

        ring.Theme = theme;
        var first = Angle(ring, scene);
        // 150 ms on the test's clock: a turn that never runs stays at its first angle.
        scene.Advance(150);
        var moved = Angle(ring, scene) != first;

        Assert.True(moved, $"the ring stayed at {first} degrees");
    }

    private static ControlTheme RingTheme(Scene scene)
    {
        Assert.True(scene.Window.TryFindResource("Button.StyledLoadingSpinner", out var found));
        return Assert.IsType<ControlTheme>(found);
    }

    /// <summary>The setters given, and those of the styles given and of every style nested in them.</summary>
    private static IEnumerable<Setter> Setters(IEnumerable<SetterBase> setters, IEnumerable<IStyle> styles) =>
        setters.OfType<Setter>().Concat(styles.OfType<StyleBase>().SelectMany(s => Setters(s.Setters, s.Children)));

    private static (ButtonLoadingSpinner Ring, Scene Scene) Loading()
    {
        var button = new Button { Content = "Button", IsLoading = true };
        var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        return ((ButtonLoadingSpinner)button.Part(Button.LoadingSpinnerPart)!, scene);
    }

    /// <summary>The ring's angle, read as the Spinner's tests read it: its ring is the same <see cref="SpinnerRing"/>.</summary>
    private static double Angle(ButtonLoadingSpinner ring, Scene scene) => SpinnerTests.Angle(ring, scene);

    [AvaloniaFact]
    public void Enhancers_sit_beside_the_content_with_Base_Webs_margins()
    {
        var start = new Border { Width = 20, Height = 20, Background = Brushes.Red };
        var end = new Border { Width = 20, Height = 20, Background = Brushes.Blue };
        var label = new TextBlock { Text = "Label" };
        var button = new Button { Content = label, StartEnhancer = start, EndEnhancer = end };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });

        var s = start.TranslatePoint(default, button)!.Value;
        var l = label.TranslatePoint(default, button)!.Value;
        var e = end.TranslatePoint(default, button)!.Value;
        Assert.Equal(Oracle.Button.Enhancers.StartMarginRight, l.X - (s.X + 20), 3);
        Assert.Equal(Oracle.Button.Enhancers.EndMarginLeft, e.X - (l.X + label.Bounds.Width), 3);
        Assert.Equal(16, s.X, 3); // the medium button's left padding
    }

    [AvaloniaFact]
    public void Absent_enhancers_take_no_room()
    {
        var with = new Button { Content = "Label", StartEnhancer = new Border { Width = 0 } };
        var without = new Button { Content = "Label" };
        using var scene = new Scene(new StackPanel { Children = { with, without } });
        Assert.Equal(without.Bounds.Width + Oracle.Button.Enhancers.StartMarginRight, with.Bounds.Width, 3);
    }

    // The label at its own width, which the font sets, and at a set width whose centre is a whole
    // pixel whatever the font, where the check is exact.
    [AvaloniaTheory]
    [InlineData(double.NaN)]
    [InlineData(40.0)]
    public void Filled_the_end_enhancer_sits_at_the_end_and_the_content_in_the_middle(double width)
    {
        var end = new Border { Width = 20, Height = 20 };
        var label = new TextBlock { Text = "Label", Width = width };
        var button = new Button { Content = label, EndEnhancer = end, WidthType = ButtonWidthType.Fill };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var e = end.TranslatePoint(default, button)!.Value;
        Assert.Equal(button.Bounds.Width - 16 - 20, e.X, 3);
        var l = label.TranslatePoint(default, button)!.Value;
        var room = button.Bounds.Width - 16 - 16 - 20 - Oracle.Button.Enhancers.EndMarginLeft;
        // Centred, as CSS centres it, to Avalonia's layout rounding: at scaling 1 the label sits on a
        // whole pixel, so a half-pixel centre (an odd room left by the label's width) lands on the
        // pixel either side of it, and a whole-pixel centre exactly on it.
        var centre = 16 + (room - label.Bounds.Width) / 2;
        if (!double.IsNaN(width))
        {
            Assert.Equal(Math.Round(centre), centre, 3);
        }

        Assert.Equal(Math.Round(l.X), l.X, 3);
        Assert.InRange(l.X, Math.Floor(centre) - 0.001, Math.Ceiling(centre) + 0.001);
    }

    [AvaloniaTheory]
    [InlineData(ButtonSize.XSmall, "xSmall")]
    [InlineData(ButtonSize.Small, "small")]
    [InlineData(ButtonSize.Medium, "medium")]
    [InlineData(ButtonSize.Large, "large")]
    public void Artwork_size_is_Base_Webs(ButtonSize size, string name)
    {
        var button = new Button { Content = "A", Size = size };
        using var scene = new Scene(button);
        Assert.Equal(Oracle.Button.Enhancers.ArtworkSize[name], button.ArtworkSize);
    }

    [AvaloniaFact]
    public void Touch_never_hovers()
    {
        // Base Web's hover styles are inside @media (hover: hover): a finger doesn't hover.
        var button = new Button { Content = "Button" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        button.RaiseEvent(new PointerEventArgs(InputElement.PointerEnteredEvent, button, touch, scene.Window,
            scene.CentreOf(button), 0, new PointerPointProperties(), KeyModifiers.None));
        scene.Pump();
        Assert.DoesNotContain(":hover", button.Classes);
        Assert.Equal(BoxShadows.Parse("none"), ((Border)button.Part("BaseButton")!).BoxShadow);

        scene.Hover(button);
        Assert.Contains(":hover", button.Classes);
    }

    [AvaloniaFact]
    public void Tap_hit_area_takes_touches_above_and_below_a_small_button()
    {
        var clicks = 0;
        var button = new Button { Content = "Go", Size = ButtonSize.XSmall, MinHitArea = ButtonMinHitArea.Tap };
        button.Click += (_, _) => clicks++;
        var next = new Border { Height = 40 };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(40), Children = { button, next } });
        // The band takes no room: the button is 28 tall and the next control starts below it.
        Assert.Equal(28, ((Border)button.Part("BaseButton")!).Bounds.Height, 3);
        Assert.Equal(28, next.Bounds.Y, 3);
        var centre = scene.CentreOf(button);
        var below = centre + new Point(0, 22); // outside the 28 tall button, inside the 48 tall area
        scene.Window.MouseMove(below);
        scene.Window.MouseDown(below, MouseButton.Left);
        scene.Window.MouseUp(below, MouseButton.Left);
        scene.Pump();
        Assert.Equal(1, clicks);

        button.MinHitArea = ButtonMinHitArea.Click;
        scene.Pump();
        scene.Window.MouseDown(below, MouseButton.Left);
        scene.Window.MouseUp(below, MouseButton.Left);
        scene.Pump();
        Assert.Equal(1, clicks);
    }

    [AvaloniaFact]
    public void Every_part_takes_the_buttons_state()
    {
        var button = new Button { Content = "A", Kind = ButtonKind.Tertiary, Size = ButtonSize.Large, IsSelected = true };
        using var scene = new Scene(button);
        foreach (var name in new[] { "BaseButton", "StartEnhancer", "EndEnhancer", "StartEnhancerButtonContentContainer", "LoadingSpinnerContainer", "LoadingSpinner" })
        {
            var part = button.Part(name)!;
            Assert.Contains(":kind-tertiary", part.Classes);
            Assert.Contains(":size-large", part.Classes);
            Assert.Contains(":is-selected", part.Classes);
        }

        button.Kind = ButtonKind.Primary;
        Assert.Contains(":kind-primary", button.Part("LoadingSpinner")!.Classes);
        Assert.DoesNotContain(":kind-tertiary", button.Part("LoadingSpinner")!.Classes);
    }
}
