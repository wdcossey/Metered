using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A switch on or off, disabled or not, with the checkmark or not, at rest, under a mouse, pressed by a finger (which
/// doesn't hover) and focused by keyboard, light and dark: each part's colour, the track's border, the overlays, the
/// focus outline and the checkmark against Base Web's switch style functions (SwitchOracle). The states are real input.
/// </summary>
public class SwitchColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SwitchOracle.Data.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = SwitchOracle.Data.Colours[index];
        var sw = new Switch { Content = "Label", IsChecked = c.Checked, IsEnabled = !c.Disabled, ShowIcon = c.ShowIcon };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { sw } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var track = (RadioMark)sw.Part(Switch.ToggleTrackPart)!;
        var toggle = (SwitchToggle)sw.Part(Switch.TogglePart)!;

        switch (c.State)
        {
            case "hover":
                scene.Hover(sw);
                break;
            case "touch":
                RadioColourTests.Touch(scene, track);
                Assert.Contains(":is-active", sw.Classes);
                break;
            case "focusVisible":
                scene.Key(Key.Tab);
                Assert.True(sw.IsFocused);
                break;
        }

        Assert.Equal(c.Checked, sw.IsChecked);
        Assert.Equal(c.TrackBackground, Colour(track.Background));
        Assert.Equal(c.TrackBorder, Colour(track.BorderBrush));
        Assert.Equal(new Thickness(c.TrackBorderWidth), track.BorderThickness);
        Assert.Equal(c.TrackOverlay, track.Overlay is null ? null : Colour(track.Overlay));
        if (c.Outline is { } outline)
        {
            Assert.Equal(outline.Colour, Colour(track.OutlineBrush));
            Assert.Equal(outline.Thickness, track.OutlineThickness);
            Assert.Equal(outline.Offset, track.OutlineOffset);
        }
        else
        {
            Assert.True(track.OutlineBrush is null || track.OutlineThickness == 0, $"an outline of {track.OutlineThickness} in {Colour(track.OutlineBrush)}");
        }

        Assert.Equal(c.ToggleBackground, Colour(toggle.Background));
        Assert.Equal(c.ToggleOverlay, toggle.Overlay is null ? null : Colour(toggle.Overlay));
        if (c.Icon is { } icon)
        {
            Assert.Equal(icon.Colour, Colour(toggle.IconBrush));
            Assert.Equal(icon.Size, toggle.IconSize);
            Assert.Equal(Geometry.Parse(icon.Path).Bounds, toggle.Icon!.Bounds);
        }
        else
        {
            Assert.True(toggle.Icon is null || toggle.IconBrush is null || toggle.IconSize == 0, "a checkmark");
        }

        Assert.Equal(c.Label, Colour(sw.Part(Switch.LabelPart)!.GetValue(TextElement.ForegroundProperty)));
    }

    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);
}
