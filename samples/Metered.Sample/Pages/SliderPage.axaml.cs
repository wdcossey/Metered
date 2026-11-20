using System.Globalization;
using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Slider, as its documentation's examples: basic, its value said under it; marks every ten; a range; disabled;
/// a custom min, max and step; a custom label (its custom ticks example's kilometres); and labels always shown (its
/// always-show-label scenario).
/// </summary>
[SamplePage("Slider", Order = 55)]
public partial class SliderPage : UserControl
{
    public SliderPage()
    {
        InitializeComponent();
        Basic.Value = [60];
        Marks.Value = [60];
        Range.Value = [25, 75];
        Disabled.Value = [40];
        Stepped.Value = [90];
        Distance.Value = [4500];
        Distance.ValueToLabel = v => (v / 1000).ToString("0.0", CultureInfo.InvariantCulture) + "km";
        Persistent.Value = [25, 60];
        Say();
        Basic.Change += (_, _) => Say();
    }

    private void Say() => State.Text = "Value: " + string.Join(", ", Basic.Value.Select(v => v.ToString(CultureInfo.InvariantCulture)));
}
