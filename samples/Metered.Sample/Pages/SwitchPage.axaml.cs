using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Switch, as its documentation's examples: basic, its state said under it; multiple lines; disabled; the
/// label left and right; the two sizes; and the checkmark.
/// </summary>
[SamplePage("Switch", Order = 43)]
public partial class SwitchPage : UserControl
{
    public SwitchPage()
    {
        InitializeComponent();
        Say();
        Basic.IsCheckedChanged += (_, _) => Say();
    }

    private void Say() => State.Text = Basic.IsChecked == true ? "On" : "Off";
}
