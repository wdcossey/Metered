using Avalonia.Controls;
using Metered.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's radio-v2 Radio and RadioGroup: a group with a description, its selection said under it; horizontal;
/// the label on top; disabled; in error; and a radio on its own.
/// </summary>
[SamplePage("Radio", Order = 35)]
public partial class RadioPage : UserControl
{
    public RadioPage()
    {
        InitializeComponent();
        Basic.PropertyChanged += (_, e) =>
        {
            if (e.Property == RadioGroup.ValueProperty) Selected.Text = $"Selected: {Basic.Value}";
        };
    }
}
