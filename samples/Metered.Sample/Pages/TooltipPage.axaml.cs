using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Tooltip, as its documentation's examples and its scenarios: open from the start (stateful-tooltip.test.tsx's
/// props), the stateful example in a line of text (stateful.tsx), complex content (stateful-complex-content.tsx), an arrow
/// and a margin (tooltip-arrow-margin.scenario.tsx), and two buttons (tooltip-interactive-element.scenario.tsx).
/// </summary>
[SamplePage("Tooltip", Order = 154)]
public partial class TooltipPage : UserControl
{
    public TooltipPage()
    {
        InitializeComponent();
        // The examples' text round their anchors, a word at a time, so the anchor flows in it as an inline span does and
        // sits on its line (an inline control in Avalonia's text sits on the baseline by its foot).
        Flow(StatefulText, "You can use tooltips in many places, including inline text",
            ". Tooltips are essentially just a Popover with a few style tweaks, so you can use all the features that Popover supports.");
        Flow(ComplexText, "You can use tooltips in many places, including inline text",
            ". Tooltips are essentially just a Popover with a few style tweaks, so you can use all the features that Popover supports. "
            + "Tooltips with interactive content, such as links or buttons, should use the autoFocus or focusLock props. This ensures "
            + "the content is within the keyboard tab order of the page.");
    }

    /// <summary>The words before the anchor, each followed by its space, and the words after it.</summary>
    private static void Flow(WrapPanel line, string before, string after)
    {
        var at = 0;
        foreach (var word in before.Split(' ')) line.Children.Insert(at++, Word(word + " "));
        foreach (var word in after.Split(' ')) line.Children.Add(Word(word + " "));
    }

    private static TextBlock Word(string text) => new() { Classes = { "paragraph" }, Text = text };
}
