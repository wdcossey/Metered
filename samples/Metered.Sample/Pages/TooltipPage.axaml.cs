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
    }
}
