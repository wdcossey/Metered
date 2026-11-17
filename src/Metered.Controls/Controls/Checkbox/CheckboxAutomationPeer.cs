using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Metered.Controls;

/// <summary>
/// The Checkbox's automation peer: a box drawn indeterminate is reported so (mixed), as Base Web sets
/// its input's <c>indeterminate</c> for assistive technology, whatever <c>IsChecked</c> is.
/// </summary>
public class CheckboxAutomationPeer : ToggleButtonAutomationPeer, IToggleProvider
{
    private ToggleState _state;

    public CheckboxAutomationPeer(Checkbox owner)
        : base(owner)
    {
        _state = Of(owner);
    }

    private new Checkbox Owner => (Checkbox)base.Owner;

    ToggleState IToggleProvider.ToggleState => Of(Owner);

    void IToggleProvider.Toggle()
    {
        if (!IsEnabled()) throw new ElementNotEnabledException();
        Owner.ToggleFromAutomation();
    }

    /// <summary>Says a change of state the base peer, which reads only <c>IsChecked</c>, doesn't see.</summary>
    internal void StateChanged()
    {
        var state = Of(Owner);
        if (state == _state) return;
        RaisePropertyChangedEvent(TogglePatternIdentifiers.ToggleStateProperty, _state, state);
        _state = state;
    }

    private static ToggleState Of(Checkbox box) =>
        box.IsIndeterminate || box.IsChecked is null ? ToggleState.Indeterminate
        : box.IsChecked == true ? ToggleState.On : ToggleState.Off;
}
