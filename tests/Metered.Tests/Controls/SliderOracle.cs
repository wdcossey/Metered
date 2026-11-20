using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's slider style functions give, and react-range's normalizeValue, which Base Web's slider moves its
/// thumbs through (tools/reference/slider.js, through cases.js), in Avalonia's units: colours as #AARRGGBB, lengths in
/// pixels. Read as <see cref="RadioOracle"/> reads the radio's.
/// </summary>
internal static class SliderOracle
{
    public static SliderReference Data { get; } = Load();

    private static SliderReference Load()
    {
        using var stream = typeof(SliderOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.slider.oracle.json")
            ?? throw new InvalidOperationException("slider.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<SliderReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record SliderReference(string Baseui, SliderColourCase[] Colours, SliderLayout Layout, SliderNormalizeCase[] Normalize, SliderNumber[] Numbers);

/// <summary>A number, and JavaScript's String(n) of it.</summary>
internal sealed record SliderNumber(double Value, string Text);

/// <summary>
/// A slider of one thumb or two in one state, on its first thumb: the parts' colours; the inner track's runs, each
/// colour up to where it stops (in percent); the thumb's shadow, or its ring (null for none).
/// </summary>
internal sealed record SliderColourCase(
    string Theme, double[] Value, string State, string Track, SliderRun[] InnerTrack, string Mark, string Thumb,
    SliderRing? Ring, string? BoxShadow, string ThumbValue, string ThumbValueText, string InnerThumb, string Tick)
{
    public override string ToString() => $"{Theme} [{string.Join(", ", Value)}] {State}";
}

internal sealed record SliderRun(string Colour, double To);

internal sealed record SliderRing(string Colour, double Thickness);

/// <summary>Every length of the parts: paddings as left, top, right, bottom; sizes as width, height.</summary>
internal sealed record SliderLayout(
    double[] TrackPadding, double InnerTrackHeight, double InnerTrackRadius, double[] Thumb, double ThumbRadius,
    double ThumbValueTop, double[] ThumbValuePadding, double ThumbValueRadius, SliderText ThumbValueText,
    double InnerThumbTop, double[] InnerThumb, double[] Mark, double MarkMarginLeft, double[] TickBarPadding, SliderText Tick);

internal sealed record SliderText(double FontSize, string FontWeight, double LineHeight);

/// <summary>react-range's normalizeValue(value, index, min, max, step, false, values), and what it gave.</summary>
internal sealed record SliderNormalizeCase(double Value, int Index, double Min, double Max, double Step, double[] Values, double Expected)
{
    public override string ToString() => $"{Value} for thumb {Index} of [{string.Join(", ", Values)}] on {Min}..{Max} by {Step}";
}
