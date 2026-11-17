using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's switch style functions give (tools/reference/switch.js, through cases.js), in Avalonia's units:
/// colours as #AARRGGBB, lengths in pixels. Read as <see cref="RadioOracle"/> reads the radio's.
/// </summary>
internal static class SwitchOracle
{
    public static SwitchReference Data { get; } = Load();

    private static SwitchReference Load()
    {
        using var stream = typeof(SwitchOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.switch.oracle.json")
            ?? throw new InvalidOperationException("switch.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<SwitchReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record SwitchReference(string Baseui, SwitchColourCase[] Colours, SwitchLayoutCase[] Layout, SwitchTransition Transition);

/// <summary>A switch in one state: its parts' colours, overlays (null for none), focus outline and checkmark.</summary>
internal sealed record SwitchColourCase(
    string Theme, bool Checked, bool Disabled, bool ShowIcon, string State,
    string TrackBackground, string TrackBorder, double TrackBorderWidth, string? TrackOverlay, SwitchOutline? Outline,
    string ToggleBackground, string? ToggleOverlay, SwitchIcon? Icon, string Label)
{
    public override string ToString() =>
        $"{Theme} {(Checked ? "on" : "off")}{(Disabled ? " disabled" : "")}{(ShowIcon ? " icon" : "")} {State}";
}

internal sealed record SwitchOutline(string Colour, double Thickness, double Offset);

internal sealed record SwitchIcon(string Colour, double Size, string Path);

/// <summary>A labelled switch by size, label placement, pointer, checked and disabled: its parts' lengths.</summary>
internal sealed record SwitchLayoutCase(
    string Size, string LabelPlacement, bool Coarse, bool Checked, bool Disabled,
    string RootDirection, string RootAlignItems, double RootPadding,
    double[] Track, double TrackBorder, double[] TrackPadding, double TrackRadius, string TrackAlignItems,
    double Toggle, double ToggleTranslate, double[] LabelPadding, SwitchText Label)
{
    public override string ToString() =>
        $"{Size} {LabelPlacement}{(Coarse ? " coarse" : "")}{(Checked ? " on" : " off")}{(Disabled ? " disabled" : "")}";
}

internal sealed record SwitchText(double FontSize, string FontWeight, double LineHeight);

internal sealed record SwitchTransition(string Property, double Seconds, double[] Curve);
