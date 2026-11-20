using Avalonia;
using Avalonia.Animation;
using Avalonia.Animation.Easings;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Metered.Controls;
using Spinner = Metered.Controls.Spinner;

namespace Metered.Tests.Controls;

/// <summary>
/// The spinner's behaviour: its turn (at its end values, since a frame-by-frame test needs #13's test clock), which
/// runs unless the ring is <c>:metered-still</c>; its cursor; its colour following <c>Color</c>; its lengths by name,
/// token and number, and the text it reads them from; its overrides; its automation peer.
/// </summary>
public class SpinnerTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    private static (Spinner Spinner, SpinnerRing Ring, Scene Scene) Make(Spinner spinner)
    {
        var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { spinner } });
        return (spinner, SpinnerColourTests.Ring(spinner), scene);
    }

    // spin: rotate(0deg) to rotate(360deg), timing1000, linear, infinite.
    [AvaloniaFact]
    public void The_turn_is_Base_Webs_spin()
    {
        var m = SpinnerOracle.Data.Motion;
        Assert.Equal(("linear", "infinite"), (m.Timing, m.Iterations));
        var (_, ring, scene) = Make(new Spinner());
        using var _ = scene;

        var animation = Assert.Single(Animations(ring.Theme!));
        Assert.Equal(TimeSpan.FromSeconds(m.Seconds), animation.Duration);
        Assert.Equal(IterationCount.Infinite, animation.IterationCount);
        Assert.IsType<LinearEasing>(animation.Easing);
        Assert.Collection(animation.Children,
            first =>
            {
                Assert.Equal(0, first.Cue.CueValue);
                var setter = Assert.IsType<Setter>(Assert.Single(first.Setters));
                Assert.Equal(RotateTransform.AngleProperty, setter.Property);
                Assert.Equal(m.From, Convert.ToDouble(setter.Value));
            },
            last =>
            {
                Assert.Equal(1, last.Cue.CueValue);
                var setter = Assert.IsType<Setter>(Assert.Single(last.Setters));
                Assert.Equal(RotateTransform.AngleProperty, setter.Property);
                Assert.Equal(m.To, Convert.ToDouble(setter.Value));
            });
    }

    // The turn runs on Avalonia's clock, real time: the ring's angle moves between two frames 150 ms apart.
    [AvaloniaFact]
    public void The_ring_turns()
    {
        var (_, ring, scene) = Make(new Spinner());
        using var _ = scene;
        var first = Angle(ring, scene);
        // A few tries, so a slow runner's frame can't make it flaky: a turn that never runs stays at its first angle.
        var moved = false;
        for (var i = 0; i < 5 && !moved; i++)
        {
            Thread.Sleep(150);
            moved = Angle(ring, scene) != first;
        }

        Assert.True(moved, $"the ring stayed at {first} degrees");
    }

    // :metered-still, which PreviewGallery sets before a picture: the turn stops, the ring at 0deg, Base Web's from.
    [AvaloniaFact]
    public void A_still_ring_stands_at_its_start()
    {
        var (_, ring, scene) = Make(new Spinner());
        using var _ = scene;
        Thread.Sleep(150);
        Assert.NotEqual(SpinnerOracle.Data.Motion.From, Angle(ring, scene));
        SpinnerColourTests.Still(ring);
        Assert.Equal(SpinnerOracle.Data.Motion.From, Angle(ring, scene));
        Thread.Sleep(150);
        Assert.Equal(SpinnerOracle.Data.Motion.From, Angle(ring, scene));
    }

    [AvaloniaFact]
    public void Its_cursor_is_wait()
    {
        Assert.Equal("wait", SpinnerOracle.Data.Cursor);
        var (spinner, ring, scene) = Make(new Spinner());
        using var _ = scene;
        Assert.Equal(new Cursor(StandardCursorType.Wait).ToString(), ring.Cursor?.ToString());
        // Over all of the round box, as the browser's: its hollow middle takes the pointer too.
        Assert.Same(ring, scene.Window.InputHitTest(scene.CentreOf(ring)));
    }

    // $color: the top of the ring is the app's brush while it is given, and contentAccent again when it isn't.
    [AvaloniaFact]
    public void The_top_follows_Color()
    {
        var (spinner, ring, scene) = Make(new Spinner());
        using var _ = scene;
        var accent = SpinnerColourTests.Colour(ring.Foreground);
        spinner.Color = Red;
        Assert.Same(Red, ring.Foreground);
        var blue = new ImmutableSolidColorBrush(Colors.Blue);
        spinner.Color = blue;
        Assert.Same(blue, ring.Foreground);
        spinner.Color = null;
        Assert.DoesNotContain(":has-color", ring.Classes);
        Assert.Equal(accent, SpinnerColourTests.Colour(ring.Foreground));
    }

    // One state per prop at a time: a change takes the old one off.
    [AvaloniaFact]
    public void A_length_change_moves_the_state()
    {
        var (spinner, ring, scene) = Make(new Spinner { Size = SpinnerSize.Large });
        using var _ = scene;
        Assert.Contains(":size-large", ring.Classes);
        Assert.Contains(":border-width-large", ring.Classes);
        spinner.Size = new SpinnerLength("scale1000");
        Assert.Contains(":size-custom", ring.Classes);
        Assert.DoesNotContain(":size-large", ring.Classes);
        Assert.Equal(Token("scale1000", scene), ring.BoxSize);
        // A size that isn't a name: an unset border is NaNpx, which a browser drops.
        Assert.Contains(":border-width-invalid", ring.Classes);
        Assert.DoesNotContain(":border-width-large", ring.Classes);
        spinner.Size = new SpinnerLength("scale9999");
        Assert.Contains(":size-invalid", ring.Classes);
        Assert.DoesNotContain(":size-custom", ring.Classes);
        spinner.Size = 50;
        spinner.BorderWidth = 6;
        scene.Pump();
        Assert.Equal((50d, 6d), (ring.BoxSize, ring.Thickness));
        Assert.Equal(new Size(50 + 2 * 6, 50 + 2 * 6), ring.Bounds.Size);
        Assert.Single(ring.Classes, c => c.StartsWith(":size-"));
        Assert.Single(ring.Classes, c => c.StartsWith(":border-width-"));
    }

    // $theme.sizing[$size]: the theme's token, whatever the theme holds: one Base Web lacks, or another value for one it has.
    [AvaloniaFact]
    public void A_sizing_token_is_the_themes()
    {
        var (_, ring, scene) = Make(new Spinner { Size = new SpinnerLength("scale9999"), BorderWidth = new SpinnerLength("scale1000") });
        using var _ = scene;
        Assert.Contains(":size-invalid", ring.Classes);
        scene.Window.Resources["sizing.scale9999"] = 50d;
        scene.Window.Resources["sizing.scale1000"] = 7d;
        scene.Pump();
        Assert.Contains(":size-custom", ring.Classes);
        Assert.Equal((50d, 7d), (ring.BoxSize, ring.Thickness));
    }

    // The text XAML gives, read as Base Web reads $size: a size by its name, a number, text (parseInt's length), unset.
    [AvaloniaFact]
    public void Lengths_are_read_from_text_as_Base_Web_reads_them()
    {
        Assert.Equal(SpinnerSize.Large, SpinnerLength.Parse("Large").Size);
        Assert.Equal(SpinnerSize.Small, SpinnerLength.Parse("small").Size);
        // Base Web's SIZE.medium is the string 'medium', which its lookup finds; its lookup is case-sensitive.
        Assert.Equal(SpinnerSize.Medium, new SpinnerLength("medium").Size);
        Assert.Null(new SpinnerLength("Medium").Size);
        Assert.Equal(1, SpinnerLength.Parse("+1").Number);
        Assert.True(double.IsNaN(SpinnerLength.Parse("Small,Large").Pixels));
        Assert.Equal(16, SpinnerLength.Parse("0x10px").Pixels);
        Assert.Equal(60, SpinnerLength.Parse("60").Number);
        Assert.Equal(0, SpinnerLength.Parse("0").Number);
        Assert.Equal("60px", SpinnerLength.Parse("60px").Text);
        Assert.Equal(60, SpinnerLength.Parse("60.7px").Pixels);
        Assert.Equal(60, new SpinnerLength(60.7).Pixels);
        Assert.Equal(12, SpinnerLength.Parse("  12px").Pixels);
        Assert.Equal(-5, SpinnerLength.Parse("-5").Pixels);
        Assert.True(double.IsNaN(SpinnerLength.Parse("abc").Pixels));
        Assert.Equal("scale1000", SpinnerLength.Parse("scale1000").Text);
        Assert.False(SpinnerLength.Parse("").IsSet);
        Assert.False(default(SpinnerLength).IsSet);

        // XAML reads it through its type converter.
        var converter = System.ComponentModel.TypeDescriptor.GetConverter(typeof(SpinnerLength));
        Assert.IsType<SpinnerLengthConverter>(converter);
        Assert.Equal(new SpinnerLength("scale1000"), converter.ConvertFrom("scale1000"));
        Assert.Equal(new SpinnerLength(SpinnerSize.Large), converter.ConvertFrom("Large"));
    }

    // ADR 0001: an override's plain setter wins over the part theme's state styles and its bindings, as Base Web's
    // withStyle merges its style over the style function's.
    [AvaloniaFact]
    public void An_override_wins_over_the_props()
    {
        var style = new ControlTheme(typeof(SpinnerRing));
        style.Setters.Add(new Setter(SpinnerRing.BoxSizeProperty, 96d));
        style.Setters.Add(new Setter(SpinnerRing.ThicknessProperty, 12d));
        style.Setters.Add(new Setter(SpinnerRing.ForegroundProperty, Red));
        var (spinner, ring, scene) = Make(new Spinner
        {
            Size = SpinnerSize.Large,
            BorderWidth = 20,
            Color = Brushes.Blue,
            Overrides = [new Override { Part = Spinner.SpinnerPart, Style = style }],
        });
        using var _ = scene;
        Assert.Equal((96d, 12d), (ring.BoxSize, ring.Thickness));
        Assert.Same(Red, ring.Foreground);
        Assert.Equal(new Size(120, 120), spinner.Bounds.Size);
        // The theme's turn is still there, from the theme the override is merged over.
        Assert.Single(Animations(ring.Theme!.BasedOn!));
    }

    // Base Web's role="progressbar" and aria-label="loading"; an app's own label wins, as its props come after.
    [AvaloniaFact]
    public void It_is_a_progress_bar_called_loading()
    {
        var (spinner, _, scene) = Make(new Spinner());
        using var _ = scene;
        var peer = ControlAutomationPeer.CreatePeerForElement(spinner);
        Assert.IsType<SpinnerAutomationPeer>(peer);
        Assert.Equal(AutomationControlType.ProgressBar, peer.GetAutomationControlType());
        Assert.Equal("loading", peer.GetName());
        Assert.True(peer.IsControlElement());
        Assert.True(peer.IsContentElement());
        AutomationProperties.SetName(spinner, "Fetching the route");
        Assert.Equal("Fetching the route", peer.GetName());
    }

    // Button's ring shares the drawing, not the box: its own Width and Height, no box of its own.
    [AvaloniaFact]
    public void Buttons_ring_keeps_its_border_box()
    {
        var button = new Metered.Controls.Button { IsLoading = true, Content = "Loading" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var ring = (ButtonLoadingSpinner)button.Part(Metered.Controls.Button.LoadingSpinnerPart)!;
        Assert.IsAssignableFrom<SpinnerRing>(ring);
        Assert.True(double.IsNaN(ring.BoxSize));
        Assert.Equal(new Size(ring.Width, ring.Height), ring.Bounds.Size);
    }

    private static double Token(string name, Scene scene) =>
        scene.Window.TryFindResource("sizing." + name, scene.Window.ActualThemeVariant, out var value) ? (double)value! : double.NaN;

    /// <summary>The ring's angle, as the turn's animation sets it on the rotation Avalonia puts in its render transform.</summary>
    private static double Angle(SpinnerRing ring, Scene scene)
    {
        scene.Pump();
        var group = Assert.IsType<TransformGroup>(ring.RenderTransform);
        return Assert.Single(group.Children.OfType<RotateTransform>()).Angle;
    }

    private static IEnumerable<Animation> Animations(ControlTheme theme) =>
        theme.Children.OfType<Style>().SelectMany(s => s.Animations.OfType<Animation>());
}
