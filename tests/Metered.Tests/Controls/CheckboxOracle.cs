using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's checkbox-v2 style functions give (tools/reference/checkbox.js, written by tools/reference/cases.js),
/// in Avalonia's units: colours as #AARRGGBB, lengths in pixels.
/// </summary>
internal static class CheckboxOracle
{
    public static CheckboxOracleFile Value { get; } = Load();

    private static CheckboxOracleFile Load()
    {
        using var stream = typeof(CheckboxOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.checkbox.oracle.json")
            ?? throw new InvalidOperationException("checkbox.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<CheckboxOracleFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record CheckboxOracleFile(
    string Baseui, CheckboxColourCase[] Colours, CheckboxLayoutCase[] Layout, Dictionary<string, GlyphFacts> Glyphs, CheckboxTransition Transition);

/// <summary>
/// A state: the Checkmark's fill, border, glyph (its name in <see cref="CheckboxOracleFile.Glyphs"/>) and the glyph's
/// colour, its outline; the CheckmarkContainer's fill; the label's colour.
/// </summary>
internal sealed record CheckboxColourCase(
    string Theme, bool Checked, bool Indeterminate, bool Error, bool Disabled, string State,
    string Background, string Border, string? Glyph, string? Tick, CheckboxOutline? Outline, string Container, string Label)
{
    public override string ToString() =>
        $"{Theme} {State}{(Checked ? " checked" : "")}{(Indeterminate ? " indeterminate" : "")}{(Error ? " error" : "")}{(Disabled ? " disabled" : "")}";
}

internal sealed record CheckboxOutline(string Colour, double Thickness, double Offset);

/// <summary>
/// A label placement on a fine or coarse pointer: the root's direction, alignment, padding and cursor; the
/// CheckmarkContainer's padding [left, top, right, bottom], radius, minimum and size; the Checkmark's size, border and
/// radius; the label's padding [left, top, right, bottom] and type.
/// </summary>
internal sealed record CheckboxLayoutCase(
    string LabelPlacement, bool Coarse,
    string RootDirection, string RootAlignItems, double RootPadding, string RootCursor,
    double[] ContainerPadding, double ContainerRadius, double[] ContainerMin, double[] ContainerSize,
    double[] MarkSize, double MarkBorder, double MarkRadius,
    double[] LabelPadding, CheckboxText Label)
{
    public override string ToString() => $"{LabelPlacement}{(Coarse ? " coarse" : "")}";
}

internal sealed record CheckboxText(double FontSize, string FontWeight, double LineHeight);

/// <summary>An SVG glyph: its one path, its width and height, and its viewBox [x, y, width, height].</summary>
internal sealed record GlyphFacts(string D, double Width, double Height, double[] ViewBox);

/// <summary>The Checkmark's transition: its CSS properties, seconds and cubic-bezier curve.</summary>
internal sealed record CheckboxTransition(string[] Properties, double Seconds, double[] Curve);
