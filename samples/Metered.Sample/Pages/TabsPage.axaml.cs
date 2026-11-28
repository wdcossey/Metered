using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Tabs (tabs-motion), as its documentation's examples: basic.tsx, keyboard-activation.tsx, enhancer.tsx,
/// vertical-orientation.tsx, fixed.tsx, disabled.tsx and artwork.tsx (three tabs of Dune's litany each), and the scroll
/// scenario's twenty tabs. Each is stateful: a tap, a click or the keys choose a tab.
/// </summary>
[SamplePage("Tabs", Order = 155)]
public partial class TabsPage : UserControl
{
    public TabsPage()
    {
        InitializeComponent();
    }
}
