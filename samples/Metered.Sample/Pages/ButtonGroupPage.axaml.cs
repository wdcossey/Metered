using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>Base Web's ButtonGroup: radio, starting on its first button; checkbox, each click said; and wrapping.</summary>
[SamplePage("ButtonGroup", Order = 2)]
public partial class ButtonGroupPage : UserControl
{
    public ButtonGroupPage()
    {
        InitializeComponent();

        Radio.Selected = [0];
        Checks.Click += (_, e) =>
            Clicked.Text = $"Clicked: {e.Index}; selected: {string.Join(", ", Checks.Selected ?? [])}";
    }
}
