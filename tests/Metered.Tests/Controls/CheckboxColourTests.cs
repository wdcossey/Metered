using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A checkbox checked or not, indeterminate or not, in error or not, disabled or not, at rest, under a mouse (over the
/// CheckmarkContainer, and over the label only), pressed by a mouse and by a finger, focused by keyboard, and with Space
/// held, light and dark: each part's colour, the glyph and the focus outline against Base Web's checkbox-v2 style
/// functions (CheckboxOracle). The states are real input.
/// </summary>
public class CheckboxColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, CheckboxOracle.Value.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = CheckboxOracle.Value.Colours[index];
        var box = new Checkbox { Content = "Label", IsChecked = c.Checked, IsIndeterminate = c.Indeterminate, Error = c.Error, IsEnabled = !c.Disabled };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { box } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var container = (Border)box.Part(Checkbox.CheckmarkContainerPart)!;

        switch (c.State)
        {
            case "hover":
                scene.Hover(container);
                break;
            case "hoverLabel":
                scene.Hover((Visual)box.Part(Checkbox.LabelPart)!);
                break;
            case "active":
                scene.Press(container);
                break;
            case "touchActive":
                RadioColourTests.Touch(scene, container);
                break;
            case "focusVisible":
                scene.Key(Key.Tab);
                Assert.True(box.IsFocused);
                break;
            case "keyActive":
                scene.Key(Key.Tab);
                Assert.True(box.IsFocused);
                scene.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, " ");
                scene.Pump();
                break;
        }

        var mark = (CheckboxMark)box.Part(Checkbox.CheckmarkPart)!;
        Assert.Equal(c.Background, Colour(Base(mark, CheckboxMark.BackgroundProperty)));
        Assert.Equal(c.Border, Colour(Base(mark, CheckboxMark.BorderBrushProperty)));
        if (c.Glyph is null)
        {
            Assert.Null(mark.Glyph);
        }
        else
        {
            var facts = CheckboxOracle.Value.Glyphs[c.Glyph];
            Assert.NotNull(mark.Glyph);
            Assert.Equal(Geometry.Parse(facts.D).Bounds, mark.Glyph.Bounds);
            Assert.Equal(new Size(facts.Width, facts.Height), mark.GlyphSize);
            Assert.Equal(new Rect(facts.ViewBox[0], facts.ViewBox[1], facts.ViewBox[2], facts.ViewBox[3]), mark.GlyphViewBox);
            Assert.Equal(c.Tick, Colour(mark.Foreground));
        }

        if (c.Outline is { } outline)
        {
            Assert.Equal(outline.Colour, Colour(mark.OutlineBrush));
            Assert.Equal(outline.Thickness, mark.OutlineThickness);
            Assert.Equal(outline.Offset, mark.OutlineOffset);
        }
        else
        {
            Assert.True(mark.OutlineBrush is null || mark.OutlineThickness == 0, $"an outline of {mark.OutlineThickness} in {Colour(mark.OutlineBrush)}");
        }

        Assert.Equal(c.Container, Colour(container.Background));
        Assert.Equal(c.Label, Colour(((ContentPresenter)box.Part(Checkbox.LabelPart)!).Foreground));
    }

    /// <summary>A brush's colour as #AARRGGBB; any colour with no alpha is CSS's transparent, #00000000 (Avalonia's is white's).</summary>
    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);

    /// <summary>The value styles give, before any transition.</summary>
    private static T Base<T>(AvaloniaObject o, StyledProperty<T> property) => o.GetBaseValue(property).GetValueOrDefault()!;
}
