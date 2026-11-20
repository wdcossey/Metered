using System.ComponentModel;
using System.Globalization;

namespace Metered.Controls;

/// <summary>
/// The value of Base Web's spinner <c>$size</c> and <c>$borderWidth</c> (<c>number | string | keyof Sizing | Size</c>):
/// a <see cref="SpinnerSize"/>, a sizing token's name (<c>scale1000</c>), other text (<c>60px</c>), or a number; or
/// unset, Base Web's <c>undefined</c>, which <c>default</c> is.
/// </summary>
/// <remarks>
/// <para>From XAML text: <c>Small</c>, <c>Medium</c> or <c>Large</c> (in any case) is the size; text that is a number
/// (<c>60</c>, <c>0</c>) is the number, as JSX's <c>{60}</c>, XAML having no other way to write one; anything else is
/// text, as Base Web's string; empty text is unset.</para>
/// <para>The <see cref="Spinner"/> reads it as Base Web's style function does: a size by its name; then text as a
/// sizing token's name, if the theme has one; then <see cref="Pixels"/>, which is JavaScript's <c>parseInt</c> of the
/// value (<c>60.7</c> and <c>"60.7px"</c> are 60).</para>
/// </remarks>
[TypeConverter(typeof(SpinnerLengthConverter))]
public readonly struct SpinnerLength : IEquatable<SpinnerLength>
{
    private enum Kind
    {
        Unset,
        Named,
        Text,
        Number,
    }

    private readonly Kind _kind;
    private readonly SpinnerSize _size;
    private readonly string? _text;
    private readonly double _number;

    public SpinnerLength(SpinnerSize size)
    {
        _kind = Kind.Named;
        _size = size;
    }

    /// <summary>A number of pixels, Base Web's <c>number</c>.</summary>
    public SpinnerLength(double pixels)
    {
        _kind = Kind.Number;
        _number = pixels;
    }

    /// <summary>Text, Base Web's <c>string</c>: a sizing token's name or a length such as <c>60px</c>. Empty text is unset.</summary>
    public SpinnerLength(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _kind = text.Length == 0 ? Kind.Unset : Kind.Text;
        _text = text.Length == 0 ? null : text;
    }

    public static implicit operator SpinnerLength(SpinnerSize size) => new(size);

    public static implicit operator SpinnerLength(double pixels) => new(pixels);

    /// <summary>Whether a value is given: Base Web's <c>undefined</c> isn't.</summary>
    public bool IsSet => _kind != Kind.Unset;

    /// <summary>The size, if it is one.</summary>
    public SpinnerSize? Size => _kind == Kind.Named ? _size : null;

    /// <summary>The text, if it is text: a sizing token's name, or a length.</summary>
    public string? Text => _text;

    /// <summary>The number, if it is one.</summary>
    public double? Number => _kind == Kind.Number ? _number : null;

    /// <summary>
    /// <c>parseInt</c> of the number or the text, the length Base Web writes in pixels when the value is neither a size
    /// nor a sizing token: the leading whole number, or NaN for none (and for a size or unset).
    /// </summary>
    public double Pixels => _kind switch
    {
        Kind.Number => double.IsFinite(_number) ? Math.Truncate(_number) : double.NaN,
        Kind.Text => ParseInt(_text!),
        _ => double.NaN,
    };

    /// <summary>
    /// JavaScript's truth of the value, which Base Web's <c>$borderWidth || $size</c> reads: unset, the number 0 and
    /// NaN are false.
    /// </summary>
    internal bool IsTruthy => _kind switch
    {
        Kind.Unset => false,
        Kind.Number => _number != 0 && !double.IsNaN(_number),
        _ => true,
    };

    /// <summary>Reads XAML's text: a size by its name, a number, or other text; empty is unset.</summary>
    public static SpinnerLength Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (text.Length == 0) return default;
        if (Enum.TryParse<SpinnerSize>(text, ignoreCase: true, out var size) && Enum.IsDefined(size) && !char.IsDigit(text[0]) && text[0] != '-')
            return new SpinnerLength(size);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)) return new SpinnerLength(number);
        return new SpinnerLength(text);
    }

    /// <summary>JavaScript's <c>parseInt(text)</c>: leading white space, a sign, then the digits up to the first that isn't one.</summary>
    private static double ParseInt(string text)
    {
        var i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
        var negative = false;
        if (i < text.Length && (text[i] == '+' || text[i] == '-'))
        {
            negative = text[i] == '-';
            i++;
        }

        var start = i;
        double value = 0;
        while (i < text.Length && text[i] is >= '0' and <= '9')
        {
            value = value * 10 + (text[i] - '0');
            i++;
        }

        if (i == start) return double.NaN;
        return negative ? -value : value;
    }

    public bool Equals(SpinnerLength other) =>
        _kind == other._kind && _size == other._size && _text == other._text && _number.Equals(other._number);

    public override bool Equals(object? obj) => obj is SpinnerLength other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(_kind, _size, _text, _number);

    public static bool operator ==(SpinnerLength left, SpinnerLength right) => left.Equals(right);

    public static bool operator !=(SpinnerLength left, SpinnerLength right) => !left.Equals(right);

    public override string ToString() => _kind switch
    {
        Kind.Named => _size.ToString(),
        Kind.Text => _text!,
        Kind.Number => _number.ToString(CultureInfo.InvariantCulture),
        _ => "",
    };
}

/// <summary>Reads a <see cref="SpinnerLength"/> from XAML's text (<see cref="SpinnerLength.Parse"/>).</summary>
public sealed class SpinnerLengthConverter : TypeConverter
{
    public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) =>
        sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);

    public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value) =>
        value is string text ? SpinnerLength.Parse(text) : base.ConvertFrom(context, culture, value);
}
