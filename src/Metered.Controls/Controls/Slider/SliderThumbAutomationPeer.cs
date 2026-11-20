using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;

namespace Metered.Controls;

/// <summary>
/// A slider thumb's automation peer: react-range's <c>role="slider"</c> on each thumb, with its value
/// (<c>aria-valuenow</c>) and its range, which a neighbour bounds (<c>aria-valuemin</c> is the thumb before's value or the
/// minimum, <c>aria-valuemax</c> the thumb after's or the maximum). A step is the slider's <c>step</c>, a large change
/// ten (react-range's PageUp and PageDown). Its name is the slider's own.
/// </summary>
public class SliderThumbAutomationPeer : ControlAutomationPeer, IRangeValueProvider
{
    private double _value;

    public SliderThumbAutomationPeer(SliderThumb owner)
        : base(owner)
    {
        _value = Value;
        if (owner.Owner is { } slider) slider.PropertyChanged += OnSliderPropertyChanged;
    }

    private new SliderThumb Owner => (SliderThumb)base.Owner;

    private Slider? Slider => Owner.Owner;

    public bool IsReadOnly => Slider is not { IsEffectivelyEnabled: true };

    public double Minimum => Slider is { } s && Owner.Index > 0 && Owner.Index - 1 < s.Value.Count ? s.Value[Owner.Index - 1] : Slider?.Min ?? 0;

    public double Maximum => Slider is { } s && Owner.Index + 1 < s.Value.Count ? s.Value[Owner.Index + 1] : Slider?.Max ?? 0;

    public double Value => Slider is { } s && Owner.Index < s.Value.Count ? s.Value[Owner.Index] : 0;

    public double SmallChange => Slider?.Step ?? 0;

    public double LargeChange => (Slider?.Step ?? 0) * 10;

    public void SetValue(double value)
    {
        if (Slider is not { } s || IsReadOnly) throw new ElementNotEnabledException();
        s.SetFromAutomation(Owner.Index, value);
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Slider;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;

    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } own ? own
        : Slider is { } s ? AutomationProperties.GetName(s) : null;

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != Slider.ValueProperty) return;
        var value = Value;
        if (value == _value) return;
        RaisePropertyChangedEvent(RangeValuePatternIdentifiers.ValueProperty, _value, value);
        _value = value;
    }
}
