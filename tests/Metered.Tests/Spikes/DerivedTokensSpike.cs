using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;

namespace Metered.Tests.Spikes;

/// <summary>
/// Spike 1 (research §8.3): Base Web's createTheme recomputes the derived colour layers from the
/// foundation (backgroundAccent follows accent). Does an Avalonia brush resource whose Color is a
/// DynamicResource to another key follow that key when it is overridden, app-wide or in a subtree?
/// </summary>
public class DerivedTokensSpike
{
    private static ResourceInclude Dictionary() =>
        new(new Uri("avares://Metered.Tests/")) { Source = new Uri("avares://Metered.Tests/Spikes/DerivedTokens.axaml") };

    private static Color? BackgroundOf(Border border) => (border.Background as ISolidColorBrush)?.Color;

    [AvaloniaFact]
    public void A_derived_brush_follows_its_foundation_where_the_dictionary_is()
    {
        var border = new Border { Width = 10, Height = 10 };
        border[!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("spike.backgroundAccent");
        using var scene = new Scene(border);
        scene.Window.Resources.MergedDictionaries.Add(Dictionary());
        scene.Pump();
        Assert.Equal(Colors.Red, BackgroundOf(border));

        // Overridden beside the dictionary, at the window: followed.
        scene.Window.Resources["spike.accent"] = Colors.Blue;
        scene.Pump();
        Assert.Equal(Colors.Blue, BackgroundOf(border));
    }

    [AvaloniaFact]
    public void A_derived_brush_ignores_its_foundation_overridden_in_a_subtree()
    {
        var border = new Border { Width = 10, Height = 10 };
        border[!Border.BackgroundProperty] = new Avalonia.Markup.Xaml.MarkupExtensions.DynamicResourceExtension("spike.backgroundAccent");
        var subtree = new Panel { Children = { border } };
        using var scene = new Scene(subtree);
        scene.Window.Resources.MergedDictionaries.Add(Dictionary());
        subtree.Resources["spike.accent"] = Colors.Green;
        scene.Pump();

        // The brush is one object, resolved where its dictionary is (the window), not where it is
        // used: a subtree's accent doesn't reach it. A subtree theme (Base Web's ThemeProvider with
        // createTheme) must give every derived key, not only the foundation (docs/spikes).
        Assert.Equal(Colors.Red, BackgroundOf(border));
    }
}
