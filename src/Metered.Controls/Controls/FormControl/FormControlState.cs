using System.ComponentModel;
using System.Globalization;

namespace Metered.Controls;

/// <summary>
/// The props Base Web's form control gives each of its parts and each function it is given (form-control.js's
/// <c>sharedProps</c>): <c>$disabled</c>, <c>$error</c>, <c>$positive</c>, and with a counter <c>$length</c>,
/// <c>$maxLength</c> and <c>$counterError</c>.
/// </summary>
/// <param name="Disabled">The form control is disabled.</param>
/// <param name="Error">An error: its <see cref="FormControl.Error"/>, or a counter past its maximum.</param>
/// <param name="Positive">Its <see cref="FormControl.Positive"/>.</param>
/// <param name="Length">The counter's length; null without a counter.</param>
/// <param name="MaxLength">What form-control.js gives as <c>$maxLength</c>: the counter's length, not its maximum
/// (<c>sharedProps.$maxLength = length</c>), when it has a maximum; null otherwise. Base Web's, kept as it is.</param>
/// <param name="CounterError">The counter is past its maximum, or its <see cref="FormControlCounter.Error"/> says so.</param>
public sealed record FormControlState(bool Disabled, bool Error, bool Positive, int? Length = null, int? MaxLength = null, bool CounterError = false);

/// <summary>
/// Base Web's <c>counter</c> as an object, <c>{ length, maxLength, error }</c>: what it doesn't give is read from the
/// child, a text box's <c>Text</c> and <c>MaxLength</c> (Base Web's child's <c>value</c> and <c>maxLength</c>).
/// <c>counter={true}</c> is one with nothing given (<c>True</c> in XAML); <c>counter={false}</c>, Base Web's default,
/// is none: a null <see cref="FormControl.Counter"/> (<c>False</c>).
/// </summary>
[TypeConverter(typeof(FormControlCounterConverter))]
public sealed class FormControlCounter : INotifyPropertyChanged
{
    private int? _length;
    private int? _maxLength;
    private bool? _error;

    /// <summary>A value changed: the form control showing the counter renders again, as Base Web's does with new props.</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The length shown; the child's text's length when unset.</summary>
    public int? Length { get => _length; set => Set(ref _length, value); }

    /// <summary>The maximum shown after the length; the child's <c>MaxLength</c> when unset or 0 (JavaScript's falsy).</summary>
    public int? MaxLength { get => _maxLength; set => Set(ref _maxLength, value); }

    /// <summary>The counter's error; when unset, a length past the maximum.</summary>
    public bool? Error { get => _error; set => Set(ref _error, value); }

    private void Set<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary><c>counter={true}</c>: on, everything read from the child; <c>false</c>: off (null).</summary>
    public static implicit operator FormControlCounter?(bool on) => on ? new FormControlCounter() : null;
}

/// <summary>Reads a <see cref="FormControlCounter"/> from XAML's text: <c>True</c> for one, <c>False</c> for none.</summary>
public sealed class FormControlCounterConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || sourceType == typeof(bool) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) => value switch
    {
        bool on => on ? new FormControlCounter() : null,
        string text when bool.TryParse(text.Trim(), out var on) => on ? new FormControlCounter() : null,
        string text => throw new FormatException($"A counter is True or False, not \"{text}\"."),
        _ => base.ConvertFrom(context, culture, value),
    };
}
