using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A form control with a label, a caption and an end enhancer round an input, in each state Base Web's style functions
/// draw (rest, disabled, an error or a positive as a boolean or a message, both, a disabled error, a counter past its
/// maximum, disabled too), light and dark: the label's, the end enhancer's and the caption's colours, the icon and the
/// text shown, against Base Web's own FormControl rendered (FormControlOracle); and the icon drawn in the caption's colour.
/// </summary>
public class FormControlColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, FormControlOracle.Value.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = FormControlOracle.Value.Colours[index];
        var form = FormControlOracle.Build(c.Props, []);
        form.Label = "Label";
        form.Caption = "Caption";
        if (!c.Props.ContainsKey("counter")) form.LabelEndEnhancer = "End";
        form.Content = new Input();
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { form } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        Assert.Equal(c.Label, InputColourTests.Colour(((FormControlLabel)form.Part(FormControl.LabelPart)!).Foreground));
        var end = (ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!;
        Assert.True(end.IsVisible);
        Assert.Equal(c.LabelEndEnhancer, InputColourTests.Colour(end.Foreground));
        Assert.Equal(c.EndText, end.Content);

        var caption = (Border)form.Part(FormControl.CaptionPart)!;
        Assert.True(caption.IsVisible);
        Assert.Equal(c.Caption, InputColourTests.Colour(caption.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.Hint, ((ContentPresenter)form.Part(FormControl.CaptionMessagePart)!).Content);
        // The message's text and the icon take the caption's colour.
        var message = (ContentPresenter)form.Part(FormControl.CaptionMessagePart)!;
        Assert.Equal(c.Caption, InputColourTests.Colour(message.Foreground));

        var icon = (FormControlCaptionIcon)form.Part(FormControl.CaptionIconPart)!;
        Assert.Equal(c.Icon is not null, icon.IsVisible);
        if (c.Icon is not null)
        {
            Assert.Equal(c.Caption, InputColourTests.Colour(icon.Foreground));
            AssertIcon(c.Icon, icon);
        }
    }

    /// <summary>The icon's path is Base Web's alert's or check's.</summary>
    internal static void AssertIcon(string name, FormControlCaptionIcon icon)
    {
        var facts = FormControlOracle.Value.Icons[name];
        var expected = Geometry.Parse((facts.FillRule == "evenodd" ? "F0 " : "F1 ") + facts.D);
        Assert.Equal(expected.Bounds, icon.Data!.Bounds);
        Assert.Equal(new Rect(facts.ViewBox[0], facts.ViewBox[1], facts.ViewBox[2], facts.ViewBox[3]), icon.ViewBox);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_alert_is_drawn_in_the_captions_colour_inside_the_icons_padding(string theme)
    {
        var c = FormControlOracle.Value.Colours.Single(x => x.Theme == theme && x.State == "errorMessage");
        var form = new FormControl { Label = "Label", Error = "Error message", Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { form } },
            theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var icon = (FormControlCaptionIcon)form.Part(FormControl.CaptionIconPart)!;
        var facts = FormControlOracle.Value.Layout.Facts.CaptionIcon;
        var frame = scene.Capture();
        var scale = facts.IconSize / 24;

        // The alert's disc at (12, 4.5) of its 24 × 24 view, above the "!": the caption's colour, inside the padding.
        var at = icon.TranslatePoint(new Point(facts.Padding[0] + 12 * scale, facts.Padding[1] + 4.5 * scale), scene.Window)!.Value;
        Assert.Equal(Color.Parse(c.Caption!), Frames.PixelAt(frame, (int)at.X, (int)at.Y));
        // The "!" is cut out of the disc (evenodd): the page shows through at (12, 10).
        var cut = icon.TranslatePoint(new Point(facts.Padding[0] + 12 * scale, facts.Padding[1] + 10 * scale), scene.Window)!.Value;
        Assert.NotEqual(Color.Parse(c.Caption!), Frames.PixelAt(frame, (int)cut.X, (int)cut.Y));
        // Nothing in the padding above the svg's box.
        var above = icon.TranslatePoint(new Point(facts.Padding[0] + 12 * scale, facts.Padding[1] / 2), scene.Window)!.Value;
        Assert.NotEqual(Color.Parse(c.Caption!), Frames.PixelAt(frame, (int)above.X, (int)above.Y));
    }
}
