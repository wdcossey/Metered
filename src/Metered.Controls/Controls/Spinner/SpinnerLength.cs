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
/// text, as Base Web's string, empty text included (Base Web's <c>""</c> isn't <c>undefined</c>).</para>
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

    /// <summary>
    /// Text, Base Web's <c>string</c>: <c>SIZE</c>'s own strings (<c>"small"</c>, <c>"medium"</c>, <c>"large"</c>) are the
    /// sizes, as Base Web's lookup finds them; other text is a sizing token's name or a length such as <c>60px</c>. Empty
    /// text is text, which JavaScript reads as false and <c>parseInt</c> as NaN.
    /// </summary>
    public SpinnerLength(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Named(text, StringComparison.Ordinal) is { } size)
        {
            _kind = Kind.Named;
            _size = size;
            return;
        }

        _kind = Kind.Text;
        _text = text;
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
        Kind.Number => ParseIntOf(_number),
        Kind.Text => ParseInt(_text!),
        _ => double.NaN,
    };

    /// <summary>
    /// JavaScript's truth of the value, which Base Web's <c>$borderWidth || $size</c> reads: unset, the number 0, NaN
    /// and empty text are false.
    /// </summary>
    internal bool IsTruthy => _kind switch
    {
        Kind.Unset => false,
        Kind.Number => _number != 0 && !double.IsNaN(_number),
        Kind.Text => _text!.Length > 0,
        _ => true,
    };

    /// <summary>Reads XAML's text: a size by its name, a number, or other text.</summary>
    public static SpinnerLength Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (Named(text, StringComparison.OrdinalIgnoreCase) is { } size) return new SpinnerLength(size);
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && double.IsFinite(number))
            return new SpinnerLength(number);
        return new SpinnerLength(text);
    }

    /// <summary>A size by Base Web's <c>SIZE</c> string (<c>small</c>), the whole text and nothing else; in any case for XAML.</summary>
    private static SpinnerSize? Named(string text, StringComparison comparison)
    {
        foreach (var size in Enum.GetValues<SpinnerSize>())
        {
            if (string.Equals(text, size.ToString().ToLowerInvariant(), comparison)) return size;
        }

        return null;
    }

    /// <summary>
    /// JavaScript's <c>parseInt(text)</c>: leading white space, a sign, then the digits up to the first that isn't one,
    /// in hexadecimal after <c>0x</c>.
    /// </summary>
    private static double ParseInt(string text)
    {
        var i = 0;
        while (i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == '\uFEFF')) i++;
        var negative = false;
        if (i < text.Length && (text[i] == '+' || text[i] == '-'))
        {
            negative = text[i] == '-';
            i++;
        }

        var radix = 10;
        if (i + 1 < text.Length && text[i] == '0' && text[i + 1] is 'x' or 'X')
        {
            radix = 16;
            i += 2;
        }

        var start = i;
        double value = 0;
        while (i < text.Length && Digit(text[i]) is { } digit && digit < radix)
        {
            value = value * radix + digit;
            i++;
        }

        if (i == start) return double.NaN;
        return negative ? -value : value;
    }

    /// <summary>
    /// <c>parseInt(number)</c>, which reads the number's text: whole for most, but from 1e21 up and under 1e-6 JavaScript
    /// writes an exponent (<c>1e+21</c>, <c>5e-7</c>), so only the digit before it is read.
    /// </summary>
    private static double ParseIntOf(double number)
    {
        if (!double.IsFinite(number)) return double.NaN;
        var magnitude = Math.Abs(number);
        if (magnitude == 0 || (magnitude < 1e21 && magnitude >= 1e-6)) return Math.Truncate(number);
        var lead = Math.Truncate(magnitude / Math.Pow(10, Math.Floor(Math.Log10(magnitude))));
        return Math.Sign(number) * Math.Min(lead, 9);
    }

    private static int? Digit(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => null,
    };

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
