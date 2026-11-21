using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Where a form control puts its boxes round an input, against where a browser's block flow puts Base Web's
/// (FormControlOracle's layout, from its style functions: the margins, collapsed, the lines, the icon's box): with or
/// without a label, an end enhancer and a caption with or without its icon. Then the label's row (the end enhancer at its
/// min-content width, flex: 0, the label the rest), the caption's row (the icon's box, the message beside it), and the
/// fonts each text is drawn in.
/// </summary>
public class FormControlLayoutTests
{
    private const double Width = 350;

    public static TheoryData<int> Cases => new(Enumerable.Range(0, FormControlOracle.Value.Layout.Cases.Length));

    private static FormControl Build(FormControlLayoutCase c)
    {
        var form = new FormControl { Content = new Input() };
        if (c.Label) form.Label = "Label";
        if (c.LabelEndEnhancer) form.LabelEndEnhancer = "End";
        if (c.Hint == "caption") form.Caption = "Caption";
        if (c.Hint == "icon") form.Error = "Error";
        return form;
    }

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Boxes_are_where_a_browser_puts_them(int index)
    {
        var c = FormControlOracle.Value.Layout.Cases[index];
        var form = Build(c);
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var input = (Control)form.Content!;
        var h = input.Bounds.Height;
        Assert.True(h > 0);

        var row = (Control)form.Part(FormControl.LabelContainerPart)!;
        Assert.Equal(c.Label, row.IsVisible);
        if (c.Label)
        {
            var at = InputLayoutTests.At(row, form);
            Assert.Equal(c.LabelTop!.Value, at.Y, 3);
            Assert.Equal(c.LabelRow!.Value, at.Height, 3);
            Assert.Equal(Width, at.Width, 3);
            // The label and its end enhancer are as tall as the row (align-items: stretch).
            Assert.Equal(c.LabelRow.Value, form.Part(FormControl.LabelPart) is Control label ? label.Bounds.Height : 0, 3);
        }

        Assert.Equal(c.ControlTop, InputLayoutTests.At(input, form).Y, 3);
        Assert.Equal(Width, input.Bounds.Width, 3);
        var caption = (Control)form.Part(FormControl.CaptionPart)!;
        Assert.Equal(c.Hint != "none", caption.IsVisible);
        if (c.Hint != "none")
        {
            var at = InputLayoutTests.At(caption, form);
            Assert.Equal(c.ControlTop + h + c.CaptionGap!.Value, at.Y, 3);
            Assert.Equal(c.CaptionRow!.Value, at.Height, 3);
        }

        // Everything to the bottom margin's edge: the caption's margin inside the control container's.
        Assert.Equal(c.ControlTop + h + c.BelowChild, form.Bounds.Height, 3);
    }

    [AvaloniaFact]
    public void The_end_enhancer_is_at_its_min_content_width_and_the_label_takes_the_rest()
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        // flex: 0 and width: 100%: a basis of 0, no growing: the enhancer shrinks to its longest word, so two words wrap.
        Assert.Equal(0, facts.LabelEndEnhancer.Flex);
        var form = new FormControl { Label = "Label", LabelEndEnhancer = "Optional field", Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var end = (Control)form.Part(FormControl.LabelEndEnhancerPart)!;
        var label = (Control)form.Part(FormControl.LabelPart)!;
        var word = Measure("Optional", facts.LabelEndEnhancer.Font);
        // Within a pixel: the layout rounds to whole pixels.
        Assert.InRange(end.Bounds.Width, word - 1, word + 1);
        Assert.Equal(Width, InputLayoutTests.At(end, form).Right, 3);
        Assert.Equal(0, InputLayoutTests.At(label, form).X, 3);
        Assert.Equal(Width - end.Bounds.Width, label.Bounds.Width, 3);
        // Two lines of the enhancer's, drawn so: the row is as tall, and the label stretched to it.
        Assert.Equal(2 * facts.LabelEndEnhancer.Font.LineHeight, end.Bounds.Height, 3);
        Assert.Equal(2, ((TextBlock)((Avalonia.Controls.Presenters.ContentPresenter)end).Child!).TextLayout.TextLines.Count);
        Assert.Equal(end.Bounds.Height, label.Bounds.Height, 3);
    }

    [AvaloniaFact]
    public void The_caption_icon_is_its_padding_round_the_svg_and_the_message_is_beside_it()
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        var form = new FormControl { Label = "Label", Error = "Error", Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var caption = (Control)form.Part(FormControl.CaptionPart)!;
        var icon = (Control)form.Part(FormControl.CaptionIconPart)!;
        var message = (Control)form.Part(FormControl.CaptionMessagePart)!;
        var p = facts.CaptionIcon.Padding;
        var box = InputLayoutTests.At(icon, caption);
        Assert.Equal(new Rect(0, 0, p[0] + facts.CaptionIcon.IconSize + p[2], p[1] + facts.CaptionIcon.IconSize + p[3]), box);
        Assert.Equal(new Thickness(p[0], p[1], p[2], p[3]), ((Decorator)icon).Padding);
        var at = InputLayoutTests.At(message, caption);
        Assert.Equal(box.Right, at.X, 3);
        Assert.Equal(Width - box.Width, at.Width, 3);
        Assert.Equal(0, at.Y, 3);
    }

    [AvaloniaFact]
    public void Each_text_is_drawn_in_Base_Webs_font()
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        var form = new FormControl { Label = "Label", LabelEndEnhancer = "End", Caption = "Caption", Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        AssertFont(facts.Label.Font, Text(form, FormControl.LabelPart));
        AssertFont(facts.LabelEndEnhancer.Font, Text(form, FormControl.LabelEndEnhancerPart));
        AssertFont(facts.Caption.Font, Text(form, FormControl.CaptionMessagePart));
    }

    // CSS collapses the label row's bottom margin, the control container's top (0) and the child's own top margin into
    // one; and the child's bottom margin with the caption's top margin. Each the largest of the positive ones.
    [AvaloniaFact]
    public void A_childs_own_margins_collapse_with_the_rows_round_it()
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        var row = FormControlOracle.Value.Layout.Cases.Single(c => c.Label && !c.LabelEndEnhancer && c.Hint == "caption");
        var input = new Input { Margin = new Thickness(0, 12, 0, 20) };
        var form = new FormControl { Label = "Label", Caption = "Caption", Content = input };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        double Collapse(params double[] m) => Math.Max(0, m.Max()) + Math.Min(0, m.Min());
        var labelBottom = row.LabelTop!.Value + row.LabelRow!.Value;
        var top = labelBottom + Collapse(facts.LabelContainer.Margin[3], facts.ControlContainer.Margin[1], 12);
        Assert.Equal(top, InputLayoutTests.At(input, form).Y, 3);
        var caption = (Control)form.Part(FormControl.CaptionPart)!;
        Assert.Equal(top + input.Bounds.Height + Collapse(20, facts.Caption.Margin[1]), InputLayoutTests.At(caption, form).Y, 3);
    }

    // A margin moved from the child's top to its bottom leaves its presenter's size as it was: laid out again all the same.
    [AvaloniaFact]
    public void A_childs_margin_moved_is_laid_out_again()
    {
        var input = new Input { Margin = new Thickness(0, 12, 0, 0) };
        var form = new FormControl { Label = "Label", Content = input };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var before = InputLayoutTests.At(input, form).Y;
        input.Margin = new Thickness(0, 0, 0, 12);
        scene.Pump();
        var facts = FormControlOracle.Value.Layout.Facts;
        Assert.Equal(before - 12 + facts.LabelContainer.Margin[3], InputLayoutTests.At(input, form).Y, 3);
    }

    // Found on a Pixel 8 (411 dp at 2.625): typing "hey" into the counter's input drew its "3" cut in half. Instrument
    // Sans' figures are proportional: "1" is narrower than "0", the row shrank to it, and a wider "2" or "3" was clamped to
    // that width, its desired size unchanged, so the row was never measured again. Each text is at its own min-content
    // width, at the row's end, and drawn whole.
    [AvaloniaTheory]
    [InlineData(1.0)]
    [InlineData(2.625)]
    public void The_counter_is_measured_again_as_it_changes(double scaling)
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        var input = new Input { Size = InputSize.Compact };
        var form = new FormControl { Label = "Enable counter", Caption = "Input caption", Counter = true, Content = input };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(16, 0), Children = { form } }, width: 411);
        scene.Window.SetRenderScaling(scaling);
        scene.Pump();
        var end = (Avalonia.Controls.Presenters.ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!;
        scene.Click(input);
        foreach (var (key, count) in new[] { ("h", "1"), ("e", "2"), ("y", "3"), ("", "2") })
        {
            if (key == "") scene.Key(Avalonia.Input.Key.Back);
            else scene.Window.KeyTextInput(key);
            scene.Pump();
            var text = (TextBlock)end.Child!;
            Assert.Equal(count, text.Text);
            var width = Measure(count, facts.LabelEndEnhancer.Font);
            // Within a pixel: the layout rounds to whole device pixels.
            Assert.InRange(end.Bounds.Width, width - 1, width + 1);
            Assert.True(text.Bounds.Width >= text.TextLayout.WidthIncludingTrailingWhitespace, $"{count}: {text.Bounds.Width} for {text.TextLayout.WidthIncludingTrailingWhitespace}");
            Assert.Equal(form.Bounds.Width, InputLayoutTests.At(end, form).Right, 3);
        }
    }

    // The same for an app's own end enhancer: a text block whose text grows, other content in its place, and a text block
    // inside a control.
    [AvaloniaFact]
    public void An_end_enhancer_that_grows_is_measured_again()
    {
        var text = new TextBlock { Text = "1" };
        var form = new FormControl { Label = "Label", LabelEndEnhancer = text, Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var end = (Control)form.Part(FormControl.LabelEndEnhancerPart)!;
        var one = end.Bounds.Width;

        text.Text = "1234";
        scene.Pump();
        Assert.True(end.Bounds.Width > one, $"{end.Bounds.Width} after {one}");
        Assert.True(text.Bounds.Width >= text.TextLayout.WidthIncludingTrailingWhitespace);

        var box = new Border { Width = 80, Height = 10 };
        form.LabelEndEnhancer = box;
        scene.Pump();
        Assert.Equal(80, end.Bounds.Width, 3);
        Assert.Equal(80, box.Bounds.Width, 3);

        // A text block inside a control of the app's, its text grown.
        var inner = new TextBlock { Text = "1" };
        form.LabelEndEnhancer = new Border { Child = inner };
        scene.Pump();
        var small = end.Bounds.Width;
        inner.Text = "1234";
        scene.Pump();
        Assert.True(end.Bounds.Width > small, $"{end.Bounds.Width} after {small}");
        Assert.True(inner.Bounds.Width >= inner.TextLayout.WidthIncludingTrailingWhitespace);
    }

    [AvaloniaFact]
    public void A_no_break_space_doesnt_break_the_end_enhancer()
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        var form = new FormControl { Label = "Label", LabelEndEnhancer = "Optional\u00A0field", Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var end = (Control)form.Part(FormControl.LabelEndEnhancerPart)!;
        Assert.InRange(end.Bounds.Width, Measure("Optional\u00A0field", facts.LabelEndEnhancer.Font) - 1, Measure("Optional\u00A0field", facts.LabelEndEnhancer.Font) + 1);
        Assert.Equal(facts.LabelEndEnhancer.Font.LineHeight, end.Bounds.Height, 3);
    }

    [AvaloniaFact]
    public void A_long_label_and_caption_wrap_at_their_words_inside_the_width()
    {
        var facts = FormControlOracle.Value.Layout.Facts;
        var words = string.Join(" ", Enumerable.Repeat("label", 40));
        var form = new FormControl { Label = words, Caption = words, Content = new Input() };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Width = Width, Children = { form } });
        var label = Text(form, FormControl.LabelPart);
        var caption = Text(form, FormControl.CaptionMessagePart);
        Assert.True(label.Bounds.Height >= 3 * facts.Label.Font.LineHeight);
        Assert.True(label.Bounds.Width <= Width);
        Assert.True(caption.Bounds.Height >= 3 * facts.Caption.Font.LineHeight);
        Assert.Equal(label.Bounds.Height, ((Control)form.Part(FormControl.LabelContainerPart)!).Bounds.Height, 3);
    }

    internal static TextBlock Text(FormControl form, string part) =>
        ((Visual)form.Part(part)!).GetVisualDescendants().OfType<TextBlock>().Single();

    private static void AssertFont(InputFont font, TextBlock text)
    {
        Assert.Equal(font.FontSize, text.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(font.FontWeight), text.FontWeight);
        Assert.Equal(font.LineHeight, text.LineHeight);
        Assert.Equal(FontFamily.Parse(font.Family), text.FontFamily);
        Assert.Equal(TextWrapping.WrapWithOverflow, text.TextWrapping);
    }

    /// <summary>A word's width in that font, as a text block draws it on one line.</summary>
    private static double Measure(string word, InputFont font)
    {
        var text = new TextBlock
        {
            Text = word, FontSize = font.FontSize, FontWeight = Enum.Parse<FontWeight>(font.FontWeight),
            FontFamily = FontFamily.Parse(font.Family), LineHeight = font.LineHeight,
        };
        text.Measure(Size.Infinity);
        return text.DesiredSize.Width;
    }
}
