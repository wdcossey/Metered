using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own textarea gives (tools/reference/textarea.js, written by tools/reference/cases.js), rendered and
/// in Avalonia's units: colours as #AARRGGBB, lengths in pixels.
/// </summary>
internal static class TextareaOracle
{
    public static TextareaOracleFile Value { get; } = Load();

    private static TextareaOracleFile Load()
    {
        using var stream = typeof(TextareaOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.textarea.oracle.json")
            ?? throw new InvalidOperationException("textarea.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<TextareaOracleFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record TextareaOracleFile(
    string Baseui, TextareaColourCase[] Colours, TextareaOutlineCase[] Outlines, TextareaLayoutCase[] Layout,
    TextareaPartsCase[] Parts, Dictionary<string, InputIconFacts> Icons, Dictionary<string, InputTransition> Transitions,
    TextareaDefaults Defaults);

/// <summary>
/// A state (rest, focused, error, positive, both, disabled, read-only…), clearable with a value: each part's colours,
/// the textarea's cursor; the clear icon's container's colour, or null where Base Web doesn't render it.
/// </summary>
internal sealed record TextareaColourCase(
    string Theme, string State, string RootBorder, string RootBackground, string ContainerForeground, string ContainerBackground,
    string InputForeground, string Caret, string Placeholder, string InputCursor, string? ClearIconContainer)
{
    public override string ToString() => $"{Theme} {State}";
}

/// <summary>The clear icon's outline with its own keyboard focus or without; null for none.</summary>
internal sealed record TextareaOutlineCase(string Theme, bool FocusVisible, InputOutline? Outline)
{
    public override string ToString() => $"{Theme}{(FocusVisible ? " focus-visible" : "")}";
}

/// <summary>
/// A size, a resize value (null: unset) and the rows Base Web gave the textarea, clearable with a value: the root's
/// border [left, top, right, bottom], radius, padding [left, right] and CSS width; the container's font; the textarea's
/// padding [left, top, right, bottom], font and CSS resize; the clear icon's container and icon; the height.
/// </summary>
internal sealed record TextareaLayoutCase(
    string Size, string? Resize, int Rows, double[] RootBorder, double RootRadius, double[] RootPadding, string RootWidth,
    InputFont ContainerFont, double[] InputPadding, InputFont InputFont, string InputResize, InputClearFacts Clear, double Height)
{
    public override string ToString() => $"{Size} resize {Resize ?? "unset"} rows {Rows}";
}

/// <summary>
/// Props that decide which parts render (clearable, the value, disabled, read-only): the parts Base Web renders, the clear
/// icon's label, title, role, tab index and its container's alignTop, the textarea's type and rows.
/// </summary>
internal sealed record TextareaPartsCase(
    bool Clearable, string Value, bool Disabled, bool ReadOnly, string[] Rendered, string? ClearLabel, string? ClearTitle,
    string? ClearRole, int? ClearTabIndex, bool? AlignTop, string InputType, int Rows)
{
    public override string ToString() =>
        $"{(Clearable ? "clearable" : "plain")}{(Value.Length > 0 ? " value" : "")}{(Disabled ? " disabled" : "")}{(ReadOnly ? " read-only" : "")}";
}

internal sealed record TextareaDefaults(
    int Rows, string Size, string? Resize, bool Clearable, bool ClearOnEscape, bool Error, bool Positive, string Placeholder,
    bool Disabled, bool ReadOnly);
