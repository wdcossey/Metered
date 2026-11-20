using System.Text.Json;
using Metered.Controls;

namespace Metered.Tests;

/// <summary>
/// What Base Web's spinner style function gives (tools/reference/spinner.js, through cases.js), in Avalonia's units:
/// colours as #AARRGGBB, lengths in pixels. Read as <see cref="SwitchOracle"/> reads the switch's.
/// </summary>
internal static class SpinnerOracle
{
    public static SpinnerReference Data { get; } = Load();

    private static SpinnerReference Load()
    {
        using var stream = typeof(SpinnerOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.spinner.oracle.json")
            ?? throw new InvalidOperationException("spinner.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<SpinnerReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record SpinnerReference(
    string Baseui, SpinnerColourCase[] Colours, SpinnerLayoutCase[] Layout, SpinnerMotion Motion,
    string Cursor, string Display, string BorderRadius, string BorderStyle);

/// <summary>A spinner's colours, light or dark, by <c>$color</c>: none, or the theme's colour of that name.</summary>
internal sealed record SpinnerColourCase(string Theme, string? Color, string Foreground, string Background)
{
    public override string ToString() => $"{Theme} {Color ?? "default"}";
}

/// <summary>
/// A spinner by <c>$size</c> and <c>$borderWidth</c>: the box Base Web's style function gives (null for one a browser
/// drops) and the border a browser draws (CSS's <c>medium</c>, 3, for one it drops).
/// </summary>
internal sealed record SpinnerLayoutCase(SpinnerValue Size, SpinnerValue BorderWidth, double? Box, double Border)
{
    public override string ToString() => $"size {Size}, borderWidth {BorderWidth}";
}

/// <summary>A value of <c>$size</c> or <c>$borderWidth</c>: unset, a size by name, text, or a number.</summary>
internal sealed record SpinnerValue(string Kind, JsonElement Value)
{
    /// <summary>The value as Metered's: what an app writes for Base Web's.</summary>
    public SpinnerLength ToLength() => Kind switch
    {
        "unset" => default,
        "size" => Enum.Parse<SpinnerSize>(Value.GetString()!, ignoreCase: true),
        "text" => new SpinnerLength(Value.GetString()!),
        "number" => Value.GetDouble(),
        _ => throw new InvalidOperationException($"A value of kind {Kind}"),
    };

    public override string ToString() => Kind == "unset" ? "unset" : $"{Kind} {Value}";
}

/// <summary>Base Web's <c>spin</c>: from and to in degrees, the duration, the curve and the count.</summary>
internal sealed record SpinnerMotion(double From, double To, double Seconds, string Timing, string Iterations);
