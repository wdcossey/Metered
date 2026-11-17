using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's checkbox-v2: its states, a parent that is indeterminate while some of its children are checked
/// (Base Web's indeterminate example), the two label placements, and a label of several lines.
/// </summary>
[SamplePage("Checkbox", Order = 34)]
public partial class CheckboxPage : UserControl
{
    public CheckboxPage()
    {
        InitializeComponent();

        Cheese.IsCheckedChanged += (_, _) => SyncParent();
        Olives.IsCheckedChanged += (_, _) => SyncParent();
        AllToppings.IsCheckedChanged += (_, _) =>
        {
            if (_syncing) return;
            Cheese.IsChecked = Olives.IsChecked = AllToppings.IsChecked;
        };
        SyncParent();
    }

    private bool _syncing;

    /// <summary>The parent is checked when every child is, indeterminate when only some are.</summary>
    private void SyncParent()
    {
        _syncing = true;
        var all = Cheese.IsChecked == true && Olives.IsChecked == true;
        var some = Cheese.IsChecked == true || Olives.IsChecked == true;
        AllToppings.IsChecked = all;
        AllToppings.IsIndeterminate = some && !all;
        _syncing = false;
    }
}
