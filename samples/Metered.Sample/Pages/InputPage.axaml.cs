using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Input, as its documentation's examples: basic, its value said under it; the states; the sizes; enhancers;
/// clearable; a password with its mask toggle; and its overrides example, the root's border by state.
/// </summary>
[SamplePage("Input", Order = 84)]
public partial class InputPage : UserControl
{
    public InputPage()
    {
        InitializeComponent();
        Say();
        Basic.TextChanged += (_, _) => Say();
    }

    private void Say() => State.Text = "Value: " + Basic.Text;
}
