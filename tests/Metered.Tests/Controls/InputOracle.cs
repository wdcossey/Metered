using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own input gives (tools/reference/input.js, written by tools/reference/cases.js), rendered and in
/// Avalonia's units: colours as #AARRGGBB, lengths in pixels.
/// </summary>
internal static class InputOracle
{
    public static InputOracleFile Value { get; } = Load();

    private static InputOracleFile Load()
    {
        using var stream = typeof(InputOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.input.oracle.json")
            ?? throw new InvalidOperationException("input.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<InputOracleFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record InputOracleFile(
    string Baseui, InputColourCase[] Colours, InputOutlineCase[] Outlines, InputLayoutCase[] Layout, InputPartsCase[] Parts,
    Dictionary<string, InputIconFacts> Icons, Dictionary<string, InputTransition> Transitions, InputDefaults Defaults);

/// <summary>
/// A state (rest, focused, error, positive, both, disabled, read-only…) with both enhancers, the clear icon and a
/// password's toggle: each part's colours, the input's cursor; the clear icon's container's colour, or null where Base
/// Web doesn't render it.
/// </summary>
internal sealed record InputColourCase(
    string Theme, string State, string RootBorder, string RootBackground,
    string StartForeground, string StartBackground, string EndForeground, string EndBackground,
    string ContainerForeground, string ContainerBackground, string InputForeground, string Caret, string Placeholder,
    string InputCursor, string? ClearIconContainer, string? MaskToggleButton)
{
    public override string ToString() => $"{Theme} {State}";
}

/// <summary>The clear icon's or the toggle's outline with its own keyboard focus or without; null for none.</summary>
internal sealed record InputOutlineCase(string Theme, string Part, bool FocusVisible, InputOutline? Outline)
{
    public override string ToString() => $"{Theme} {Part}{(FocusVisible ? " focus-visible" : "")}";
}

internal sealed record InputOutline(string Colour, double Thickness);

/// <summary>
/// A size with a set of enhancers (none, start, end, both) and a trailing icon (none, clearable, password): adjoined
/// and the trailing flag as input.js gives them, the root's border [left, top, right, bottom], radius, padding [left,
/// right] and font; each enhancer's padding [left, right] and font, or null; the container's font; the input's padding
/// [left, top, right, bottom] and font; the clear icon's container and the toggle; the height.
/// </summary>
internal sealed record InputLayoutCase(
    string Size, string Enhancers, string Trailing, string Adjoined, bool HasIconTrailing,
    double[] RootBorder, double RootRadius, double[] RootPadding, InputFont RootFont,
    InputEnhancerFacts? StartEnhancer, InputEnhancerFacts? EndEnhancer, InputFont ContainerFont,
    double[] InputPadding, InputFont InputFont, InputClearFacts? Clear, InputToggleFacts? Toggle, double Height)
{
    public override string ToString() => $"{Size} {Enhancers} {Trailing}";
}

internal sealed record InputFont(double FontSize, string FontWeight, double LineHeight, string Family);

internal sealed record InputEnhancerFacts(double[] Padding, InputFont Font);

/// <summary>The clear icon's container: its padding [left, top, right], how it aligns the icon; the icon's size and cursor.</summary>
internal sealed record InputClearFacts(double[] Padding, string AlignItems, double IconSize, string Cursor);

/// <summary>The mask toggle: its padding [left, right], how it aligns its icon; the show and hide icons' sizes.</summary>
internal sealed record InputToggleFacts(double[] Padding, string AlignItems, double ShowIconSize, double HideIconSize);

/// <summary>
/// Props that decide which parts render (clearable, the value, disabled, read-only, the type, the mask state): the parts
/// Base Web renders, the clear icon's label, title, role and tab index, the toggle's label and title, the input's type.
/// </summary>
internal sealed record InputPartsCase(
    bool Clearable, string Value, bool Disabled, bool ReadOnly, string Type, bool Masked, string[] Rendered,
    string? ClearLabel, string? ClearTitle, string? ClearRole, int? ClearTabIndex, string? ToggleLabel, string? ToggleTitle,
    string InputType)
{
    public override string ToString() =>
        $"{Type}{(Masked ? "" : " unmasked")}{(Clearable ? " clearable" : "")}{(Value.Length > 0 ? " value" : "")}{(Disabled ? " disabled" : "")}{(ReadOnly ? " read-only" : "")}";
}

/// <summary>An icon: its one path, its viewBox [x, y, width, height] and the path's SVG fill rule.</summary>
internal sealed record InputIconFacts(string D, double[] ViewBox, string FillRule);

internal sealed record InputTransition(string[] Properties, double Seconds, double[] Curve);

internal sealed record InputDefaults(
    string Size, string Type, bool Clearable, bool ClearOnEscape, bool Error, bool Positive, string Placeholder, bool Disabled, bool ReadOnly);
