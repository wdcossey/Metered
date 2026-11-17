using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's radio-v2 style functions give (tools/reference/radio.js, through cases.js), in Avalonia's units:
/// colours as #AARRGGBB, lengths in pixels. Read as <see cref="Oracle"/> reads the others.
/// </summary>
internal static class RadioOracle
{
    public static RadioReference Data { get; } = Load();

    private static RadioReference Load()
    {
        using var stream = typeof(RadioOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.radio.oracle.json")
            ?? throw new InvalidOperationException("radio.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<RadioReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record RadioReference(string Baseui, RadioColourCase[] Colours, RadioLayoutCase[] Layout, RadioGroupCase[] Group, RadioTransition Transition);

/// <summary>A radio in one state: its parts' colours, the inner disc's size, the focus outline (null for none).</summary>
internal sealed record RadioColourCase(
    string Theme, bool Checked, bool Error, bool Disabled, string State,
    string OuterBackground, string OuterBorder, RadioOutline? Outline,
    string InnerBackground, string? InnerOverlay, double InnerSize,
    string BackplateBackground, string Label, string Description)
{
    public override string ToString() =>
        $"{Theme} {(Checked ? "checked" : "unchecked")}{(Error ? " error" : "")}{(Disabled ? " disabled" : "")} {State}";
}

internal sealed record RadioOutline(string Colour, double Thickness, double Offset);

/// <summary>A radio with a label and a description, by label placement, align and pointer.</summary>
internal sealed record RadioLayoutCase(
    string LabelPlacement, string Align, bool Coarse,
    string RootDirection, string RootAlignItems, double? RootColumnGap, double? RootRowGap, double RootPadding, string RootCursor,
    double[] BackplatePadding, double BackplateRadius, double[] BackplateMin, double[] BackplateSize,
    double OuterSize, double OuterBorder,
    double WrapperPaddingTop, double WrapperRowGap, double? WrapperMaxWidth,
    RadioText Label, RadioText Description)
{
    public override string ToString() => $"{LabelPlacement} {Align}{(Coarse ? " coarse" : "")}";
}

internal sealed record RadioText(double FontSize, string FontWeight, double LineHeight, int MaxLines);

internal sealed record RadioGroupCase(string Align, string LabelPlacement, string FlexDirection, string FlexWrap, string AlignItems, double ColumnGap, double RowGap)
{
    public override string ToString() => $"{Align} {LabelPlacement}";
}

internal sealed record RadioTransition(string[] Properties, double Seconds, double[] Curve);
