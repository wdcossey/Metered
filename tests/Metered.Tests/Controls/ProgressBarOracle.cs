using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's ProgressBar gives (tools/reference/progress-bar.js, through cases.js): Base Web's own component
/// rendered for each case, its parts' style functions evaluated with the props it gave them, in Avalonia's units:
/// colours as #AARRGGBB, lengths in pixels, transforms and positions in percent. Read as <see cref="SwitchOracle"/>
/// reads the switch's.
/// </summary>
internal static class ProgressBarOracle
{
    public static ProgressBarReference Data { get; } = Load();

    private static ProgressBarReference Load()
    {
        using var stream = typeof(ProgressBarOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.progress-bar.oracle.json")
            ?? throw new InvalidOperationException("progress-bar.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<ProgressBarReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record ProgressBarReference(
    string Baseui, ProgressBarColourCase[] Colours, ProgressBarLayoutCase[] Layout, ProgressBarValueCase[] Values, ProgressBarMotion Motion);

/// <summary>A bar by theme and intent (null for none): the track, the fill, the infinite bars' colour and the label.</summary>
internal sealed record ProgressBarColourCase(string Theme, string? Intent, string Bar, string BarProgress, string Infinite, string Label)
{
    public override string ToString() => $"{Theme} {Intent ?? "no intent"}";
}

/// <summary>A labelled bar by size, of one step or three: its parts' lengths and type, and the infinite bars'.</summary>
internal sealed record ProgressBarLayoutCase(
    string Size, int Steps, double RootGap, ProgressBarBar[] Bars, double[] BarProgressRadii, double[] LabelPadding,
    string LabelAlign, ProgressBarText Label, ProgressBarInfiniteBarCase[] InfiniteBars)
{
    public override string ToString() => $"{Size}, {Steps} step{(Steps == 1 ? "" : "s")}";
}

internal sealed record ProgressBarBar(double Height, double[] Radii, double MarginLeft);

internal sealed record ProgressBarInfiniteBarCase(double Height, double[] Radii);

internal sealed record ProgressBarText(double FontSize, string FontWeight, double LineHeight);

/// <summary>
/// A bar for a value: what Base Web renders for it (each step's fill: its state, null in a bar of one step, and its
/// transform; the infinite bars), its label (null unless shown) and its accessible name and busy state.
/// </summary>
internal sealed record ProgressBarValueCase(
    double Value, double MinValue, double MaxValue, int Steps, bool Infinite, bool ShowLabel, string? AriaLabel,
    string Name, bool Busy, string? Label, ProgressBarStep[] Bars, int InfiniteBars)
{
    public override string ToString() =>
        Infinite ? $"infinite, {Steps} step(s){(ShowLabel ? ", label" : "")}"
        : $"{Value} of {MinValue} to {MaxValue}, {Steps} step(s){(ShowLabel ? ", label" : "")}{(AriaLabel is null ? "" : $", named {AriaLabel}")}";
}

internal sealed record ProgressBarStep(string? State, double Offset);

internal sealed record ProgressBarMotion(ProgressBarTransition Transition, ProgressBarAnimation InProgress, ProgressBarSweep[] Infinite);

internal sealed record ProgressBarTransition(string Property, double Seconds, double[] Curve);

internal sealed record ProgressBarAnimation(double Seconds, string Iterations, double[] Curve, ProgressBarFrame[] Frames);

internal sealed record ProgressBarFrame(double Cue, double Offset, double Opacity);

internal sealed record ProgressBarSweep(
    bool IsLeft, double Seconds, string Iterations, double[] Curve, double Position, double ImageSize, double Angle, double Start, double End,
    ProgressBarSweepFrame[] Frames);

internal sealed record ProgressBarSweepFrame(double Cue, double Position);
