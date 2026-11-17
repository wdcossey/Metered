using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Every state of Base Web's Checkbox against its own style functions (CheckboxOracle), light and dark:
/// the default's Checkmark (fill, border, glyph and its colour, the focus outline) and the toggle's
/// Toggle and ToggleTrack (fill, box-shadow or focus ring, the knob's travel), with the label's colour.
/// Hover, press and keyboard focus are real input.
/// </summary>
public class CheckboxColourTests
{
    public static TheoryData<int> CheckmarkCases => new(Enumerable.Range(0, CheckboxOracle.Value.Checkmark.Length));

    public static TheoryData<int> ToggleCases => new(Enumerable.Range(0, CheckboxOracle.Value.Toggle.Length));

    [AvaloniaTheory]
    [MemberData(nameof(CheckmarkCases))]
    public void Checkmark_matches_Base_Web(int index)
    {
        var c = CheckboxOracle.Value.Checkmark[index];
        var box = new Checkbox { Content = "Label", IsChecked = c.Checked, IsIndeterminate = c.Indeterminate, Error = c.Error, IsEnabled = !c.Disabled };
        using var scene = Show(box, c.Theme, c.State);

        var mark = (CheckboxMark)box.Part(Checkbox.CheckmarkPart)!;
        Assert.Equal(c.Background, Colour(Base(mark, CheckboxMark.BackgroundProperty)));
        Assert.Equal(c.Border, Colour(Base(mark, CheckboxMark.BorderBrushProperty)));
        var glyph = Base(mark, CheckboxMark.GlyphProperty);
        if (c.Glyph is null)
        {
            Assert.Null(glyph);
        }
        else
        {
            var facts = CheckboxOracle.Value.Glyphs[c.Glyph];
            Assert.NotNull(glyph);
            Assert.Equal(Geometry.Parse(facts.D).Bounds, glyph.Bounds);
            Assert.Equal(new Size(facts.Width, facts.Height), Base(mark, CheckboxMark.GlyphSizeProperty));
            Assert.Equal(c.Tick, Colour(Base(mark, CheckboxMark.ForegroundProperty)));
        }

        Assert.Equal(c.OutlineWidth, Base(mark, CheckboxMark.OutlineThicknessProperty));
        if (c.Outline is not null) Assert.Equal(c.Outline, Colour(Base(mark, CheckboxMark.OutlineBrushProperty)));
        AssertLabel(box, c.Label);
    }

    [AvaloniaTheory]
    [MemberData(nameof(ToggleCases))]
    public void Toggle_matches_Base_Web(int index)
    {
        var c = CheckboxOracle.Value.Toggle[index];
        var box = new Checkbox { Content = "Label", CheckmarkType = CheckboxStyleType.Toggle, IsChecked = c.Checked, Error = c.Error, IsEnabled = !c.Disabled };
        using var scene = Show(box, c.Theme, c.State);

        var knob = (CheckboxMark)box.Part(Checkbox.TogglePart)!;
        Assert.Equal(c.Background, Colour(Base(knob, CheckboxMark.BackgroundProperty)));
        // Base Web's focus ring, a 0 0 0 3px box-shadow, is the knob's outline, which takes the theme's colour.
        var shadow = BoxShadows.Parse(c.BoxShadow);
        if (shadow.Count == 1 && shadow[0] is { OffsetX: 0, OffsetY: 0, Blur: 0, IsInset: false } ring && ring.Spread > 0)
        {
            Assert.Equal(BoxShadows.Parse("none"), Base(knob, CheckboxMark.BoxShadowProperty));
            Assert.Equal(ring.Spread, Base(knob, CheckboxMark.OutlineThicknessProperty));
            Assert.Equal("#" + ring.Color.ToUInt32().ToString("X8"), Colour(Base(knob, CheckboxMark.OutlineBrushProperty)));
        }
        else
        {
            Assert.Equal(shadow, Base(knob, CheckboxMark.BoxShadowProperty));
            Assert.Equal(0, Base(knob, CheckboxMark.OutlineThicknessProperty));
        }

        var transform = Base(knob, Visual.RenderTransformProperty);
        Assert.NotNull(transform);
        Assert.Equal(c.TranslateX, transform.Value.M31, 3);
        var track = (Border)box.Part(Checkbox.ToggleTrackPart)!;
        Assert.Equal(c.Track, Colour(Base(track, Border.BackgroundProperty)));
        AssertLabel(box, c.Label);
    }

    private static Scene Show(Checkbox box, string theme, string state)
    {
        var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { box } },
            theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        switch (state)
        {
            case "hover": scene.Hover(box); break;
            case "active": scene.Press(box); break;
            case "focusVisible":
                box.Focus(NavigationMethod.Tab);
                scene.Pump();
                break;
        }

        return scene;
    }

    private static void AssertLabel(Checkbox box, string colour)
    {
        var label = (ContentPresenter)box.Part(Checkbox.LabelPart)!;
        Assert.Equal(colour, Colour(Base(label, ContentPresenter.ForegroundProperty)));
    }

    /// <summary>
    /// A brush's colour as #AARRGGBB, any fully transparent colour as CSS's <c>transparent</c>
    /// (#00000000): Avalonia's Transparent is #00FFFFFF, which draws nothing either.
    /// </summary>
    internal static string Colour(IBrush? brush) =>
        brush is ISolidColorBrush { Color.A: 0 } ? "#00000000" : ButtonColourTests.Colour(brush);

    /// <summary>The value styles give, before any transition.</summary>
    private static T Base<T>(AvaloniaObject o, StyledProperty<T> property) => o.GetBaseValue(property).GetValueOrDefault()!;
}
