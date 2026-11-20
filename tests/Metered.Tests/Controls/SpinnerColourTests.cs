using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Metered.Controls;
using Spinner = Metered.Controls.Spinner;

namespace Metered.Tests.Controls;

/// <summary>
/// A spinner light and dark, with no <c>$color</c> and with one: the ring's top and the rest against Base Web's spinner
/// style function (SpinnerOracle), and the pixels it draws.
/// </summary>
public class SpinnerColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SpinnerOracle.Data.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void The_ring_matches_Base_Web(int index)
    {
        var c = SpinnerOracle.Data.Colours[index];
        var spinner = new Spinner();
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { spinner } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        // $color={theme.colors.negative}, as the scenario gives it: the app's brush, here the colour the oracle read.
        if (c.Color is not null) spinner.Color = new ImmutableSolidColorBrush(Color.Parse(c.Foreground));
        scene.Pump();
        var ring = Ring(spinner);

        Assert.Equal(c.Color is not null, ring.Classes.Contains(":has-color"));
        Assert.Equal(c.Foreground, Colour(ring.Foreground));
        Assert.Equal(c.Background, Colour(ring.Background));

        // Drawn at rest (0deg): the middle of the top border in the top's colour, of the bottom in the rest's.
        Still(ring);
        var frame = scene.Capture();
        var at = ring.TranslatePoint(new Point(0, 0), scene.Window)!.Value;
        var side = ring.Bounds.Width;
        var t = ring.Thickness;
        Assert.Equal(c.Foreground, Frames.PixelAt(frame, (int)(at.X + side / 2), (int)(at.Y + t / 2)).ToString().ToUpperInvariant());
        Assert.Equal(c.Background, Frames.PixelAt(frame, (int)(at.X + side / 2), (int)(at.Y + side - t / 2)).ToString().ToUpperInvariant());
        Assert.Equal(c.Background, Frames.PixelAt(frame, (int)(at.X + t / 2), (int)(at.Y + side / 2)).ToString().ToUpperInvariant());
    }

    internal static SpinnerRing Ring(Spinner spinner) => (SpinnerRing)spinner.Part(Spinner.SpinnerPart)!;

    /// <summary>What PreviewGallery does before a picture: the turn stops, and the ring stands at 0deg.</summary>
    internal static void Still(StyledElement ring, bool still = true) => ((IPseudoClasses)ring.Classes).Set(":metered-still", still);

    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);
}
