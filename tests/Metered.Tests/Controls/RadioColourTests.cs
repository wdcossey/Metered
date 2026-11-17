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
/// A radio checked or not, in error or not, disabled or not, at rest, under a mouse (over the backplate, and over the
/// label only), pressed by a mouse and by a finger, and focused by keyboard, light and dark: each part's colour, the
/// inner disc's size and the focus outline against Base Web's radio-v2 style functions (RadioOracle). The states are
/// real input.
/// </summary>
public class RadioColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, RadioOracle.Data.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = RadioOracle.Data.Colours[index];
        var radio = new Radio { Value = "a", Content = "Label", Description = "Description", IsEnabled = !c.Disabled };
        var group = new RadioGroup { Value = c.Checked ? "a" : null, Error = c.Error, Items = { radio } };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { group } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var backplate = (Border)radio.Part(Radio.RadioBackplatePart)!;

        switch (c.State)
        {
            case "hover":
                scene.Hover(backplate);
                break;
            case "hoverLabel":
                scene.Hover((Visual)radio.Part(Radio.LabelPart)!);
                break;
            case "active":
                scene.Press(backplate);
                break;
            case "touchActive":
                Touch(scene, backplate);
                break;
            case "focusVisible":
                scene.Key(Key.Tab);
                Assert.True(radio.IsFocused);
                break;
        }

        var outer = (RadioMark)radio.Part(Radio.RadioMarkOuterPart)!;
        var inner = (RadioMark)radio.Part(Radio.RadioMarkInnerPart)!;
        Assert.Equal(c.OuterBackground, Colour(outer.GetBaseValue(RadioMark.BackgroundProperty).GetValueOrDefault()));
        Assert.Equal(c.OuterBorder, Colour(outer.GetBaseValue(RadioMark.BorderBrushProperty).GetValueOrDefault()));
        if (c.Outline is { } outline)
        {
            Assert.Equal(outline.Colour, Colour(outer.OutlineBrush));
            Assert.Equal(outline.Thickness, outer.OutlineThickness);
            Assert.Equal(outline.Offset, outer.OutlineOffset);
        }
        else
        {
            Assert.True(outer.OutlineBrush is null || outer.OutlineThickness == 0, $"an outline of {outer.OutlineThickness} in {Colour(outer.OutlineBrush)}");
        }

        Assert.Equal(c.InnerBackground, Colour(inner.GetBaseValue(RadioMark.BackgroundProperty).GetValueOrDefault()));
        Assert.Equal(c.InnerOverlay, inner.Overlay is null ? null : Colour(inner.Overlay));
        Assert.Equal(c.InnerSize, inner.GetBaseValue(Avalonia.Layout.Layoutable.WidthProperty).GetValueOrDefault());
        Assert.Equal(c.InnerSize, inner.GetBaseValue(Avalonia.Layout.Layoutable.HeightProperty).GetValueOrDefault());
        Assert.Equal(c.BackplateBackground, Colour(backplate.Background));
        Assert.Equal(c.Label, Colour(radio.Part(Radio.LabelPart)!.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.Description, Colour(((TextBlock)radio.Part(Radio.DescriptionPart)!).Foreground));
    }

    /// <summary>A finger pressed on the control, held: a touch pointer, which doesn't hover.</summary>
    internal static void Touch(Scene scene, Control target)
    {
        var touch = new Pointer(Pointer.GetNextFreeId(), PointerType.Touch, true);
        target.RaiseEvent(new PointerPressedEventArgs(target, touch, scene.Window, scene.CentreOf(target), 0,
            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed), KeyModifiers.None));
        scene.Pump();
    }

    /// <summary>A brush's colour as #AARRGGBB; any colour with no alpha is CSS's transparent, #00000000 (Avalonia's is white's).</summary>
    internal static string Colour(IBrush? brush) =>
        brush is ISolidColorBrush { Color.A: 0 } ? "#00000000" : ButtonColourTests.Colour(brush);
}
