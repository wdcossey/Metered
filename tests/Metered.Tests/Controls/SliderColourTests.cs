using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Slider = Metered.Controls.Slider;

namespace Metered.Tests.Controls;

/// <summary>
/// A slider of one thumb or two, at rest, under a mouse, pressed by a finger (which doesn't hover), focused by keyboard,
/// with its labels always shown, and disabled, light and dark: each part's colour, the inner track's selection and rest,
/// the thumb's shadow or ring, and whether the thumb's label shows, against Base Web's slider style functions
/// (SliderOracle) and slider.js. The states are real input.
/// </summary>
public class SliderColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SliderOracle.Data.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = SliderOracle.Data.Colours[index];
        var slider = new Slider
        {
            Value = c.Value,
            Step = 10,
            Marks = true,
            PersistentThumb = c.State is "persistent" or "disabled",
            IsEnabled = c.State != "disabled",
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20, 80, 20, 20), Children = { slider } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var thumb = slider.Thumbs[0];
        IDisposable? touch = null;
        switch (c.State)
        {
            case "hover":
                scene.Hover(thumb);
                break;
            case "touch":
                touch = scene.Window.TouchBegin(scene.CentreOf(thumb), RawInputModifiers.None);
                scene.Pump();
                Assert.Equal(0, slider.DraggedThumb);
                break;
            case "focusVisible":
                scene.Key(Key.Tab);
                Assert.True(thumb.IsFocused);
                break;
        }

        using var _ = touch;
        Assert.Equal(c.Value, slider.Value);
        Assert.Equal(c.Track, Colour(((Border)slider.Part(Slider.TrackPart)!).Background));

        // The inner track's runs: one thumb, the selection then the rest; two, the rest, the selection, the rest.
        var inner = (SliderInnerTrack)slider.Part(Slider.InnerTrackPart)!;
        var selection = c.Value.Length == 1 ? 0 : 1;
        Assert.Equal(c.Value.Length + 1, c.InnerTrack.Length);
        for (var i = 0; i < c.InnerTrack.Length; i++)
        {
            Assert.Equal(c.InnerTrack[i].Colour, Colour(i == selection ? inner.Fill : inner.Background));
            if (i < c.Value.Length) Assert.Equal(c.Value[i], c.InnerTrack[i].To);
        }

        Assert.All(slider.Elements(Slider.MarkPart), mark => Assert.Equal(c.Mark, Colour(((Border)mark).Background)));
        Assert.Equal(c.Thumb, Colour(thumb.Background));
        if (c.Ring is { } ring)
        {
            Assert.Equal(ring.Colour, Colour(thumb.OutlineBrush));
            Assert.Equal(ring.Thickness, thumb.OutlineThickness);
            Assert.Equal(0, thumb.OutlineOffset);
            Assert.Equal(0, thumb.BoxShadow.Count);
        }
        else
        {
            Assert.True(thumb.OutlineBrush is null || thumb.OutlineThickness == 0, $"a ring of {thumb.OutlineThickness}");
            Assert.Equal(BoxShadows.Parse(c.BoxShadow!), thumb.BoxShadow);
        }

        // slider.js: the label shows while hovered or dragged and enabled, or always with persistentThumb.
        var shown = c.State is "hover" or "touch" or "persistent" or "disabled";
        Assert.Equal(shown, thumb.Children.Count > 0);
        if (shown)
        {
            var value = (ContentPresenter)slider.Elements(Slider.ThumbValuePart)[0];
            Assert.Same(thumb, value.Parent);
            Assert.Equal(c.ThumbValue, Colour(value.Background));
            Assert.Equal(c.ThumbValueText, Colour(value.Foreground));
            Assert.Equal(c.InnerThumb, Colour(((Border)slider.Elements(Slider.InnerThumbPart)[0]).Background));
        }

        Assert.All(slider.Elements(Slider.TickPart), tick => Assert.Equal(c.Tick, Colour(((ContentPresenter)tick).Foreground)));
    }

    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);
}
