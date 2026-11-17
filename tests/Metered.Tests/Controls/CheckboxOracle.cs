using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own style functions give for its v1 Checkbox (tools/reference/checkbox.js, written by
/// tools/reference/cases.js), in Avalonia's units: colours as #AARRGGBB, box-shadows in Avalonia's syntax,
/// lengths in pixels.
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
    string Baseui, CheckmarkCase[] Checkmark, ToggleCase[] Toggle, PlacementCase[] Layout,
    Dictionary<string, string> DefaultPlacement, CheckboxSizes Sizes, Dictionary<string, GlyphFacts> Glyphs);

/// <summary>A state of the default checkbox: the Checkmark part's colours, its glyph and outline, and the label's colour.</summary>
internal sealed record CheckmarkCase(
    string Theme, bool Checked, bool Indeterminate, bool Error, bool Disabled, string State,
    string Background, string Border, string? Glyph, string? Tick, double OutlineWidth, string? Outline, string Label)
{
    public override string ToString() =>
        $"{Theme} {State}{(Checked ? " checked" : "")}{(Indeterminate ? " indeterminate" : "")}{(Error ? " error" : "")}{(Disabled ? " disabled" : "")}";
}

/// <summary>A state of the toggle: the Toggle part's fill, box-shadow and translation, the track's fill, the label's colour.</summary>
internal sealed record ToggleCase(
    string Theme, bool Checked, bool Error, bool Disabled, string State,
    string Background, string BoxShadow, double TranslateX, string Track, string Label)
{
    public override string ToString() =>
        $"{Theme} {State}{(Checked ? " checked" : "")}{(Error ? " error" : "")}{(Disabled ? " disabled" : "")}";
}

/// <summary>The Root's flex direction and cross alignment, and the label's padding [left, top, right, bottom], by placement.</summary>
internal sealed record PlacementCase(string Placement, string Direction, string AlignItems, double[] LabelPadding)
{
    public override string ToString() => Placement;
}

internal sealed record CheckboxSizes(MarkBox Checkmark, MarkBox Track, MarkBox Toggle, LabelFont Label);

/// <summary>A box's size; margin [left, top, right, bottom] where it has one.</summary>
internal sealed record MarkBox(double Width, double Height, double[]? Margin, double BorderWidth, double Radius);

internal sealed record LabelFont(double FontSize, string FontWeight, double LineHeight);

/// <summary>An SVG glyph: its one path, and its width and height (its viewBox).</summary>
internal sealed record GlyphFacts(string D, double Width, double Height);
