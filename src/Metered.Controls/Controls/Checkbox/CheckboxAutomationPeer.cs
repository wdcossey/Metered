using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Metered.Controls;

/// <summary>
/// The Checkbox's automation peer: a check box whose toggle state is On, Off, or Indeterminate (mixed)
/// while it is drawn indeterminate, as Base Web sets its input's <c>indeterminate</c> for assistive
/// technology, whatever <c>IsChecked</c> is. It is the only source of the state's change events, so
/// none says Off or On while the box reads as mixed. A toggle through it is a click: <c>Click</c> and
/// <c>Command</c> run, as Avalonia's own toggle button peer does.
/// </summary>
public class CheckboxAutomationPeer : ContentControlAutomationPeer, IToggleProvider
{
    private ToggleState _state;

    public CheckboxAutomationPeer(Checkbox owner)
        : base(owner)
    {
        _state = Of(owner);
        owner.PropertyChanged += OnOwnerPropertyChanged;
    }

    private new Checkbox Owner => (Checkbox)base.Owner;

    public ToggleState ToggleState => Of(Owner);

    public void Toggle()
    {
        if (!IsEnabled()) throw new ElementNotEnabledException();
        Owner.ClickFromAutomation();
    }

    private void OnOwnerPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Checkbox.IsCheckedProperty && e.Property != Checkbox.IsIndeterminateProperty) return;
        var state = Of(Owner);
        if (state == _state) return;
        RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, _state, state);
        _state = state;
    }

    private static ToggleState Of(Checkbox box) =>
        box.IsIndeterminate || box.IsChecked is null ? ToggleState.Indeterminate
        : box.IsChecked == true ? ToggleState.On : ToggleState.Off;
}
