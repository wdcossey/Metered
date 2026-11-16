using System.Text.Json;
using System.Text.Json.Serialization;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own style functions give (tools/reference/cases.js, from the pinned baseui),
/// in Avalonia's units: colours as #AARRGGBB, box-shadows in Avalonia's syntax, lengths in pixels.
/// </summary>
internal static class Oracle
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static ButtonOracle Button { get; } = Load<ButtonOracle>("button.oracle.json");

    public static ButtonGroupOracle ButtonGroup { get; } = Load<ButtonGroupOracle>("button-group.oracle.json");

    /// <summary>Base Web's createTheme and createDarkTheme (tools/reference/themes.js).</summary>
    public static ThemeOracle Themes { get; } = Load<ThemeOracle>("theme.oracle.json");

    private static T Load<T>(string name)
    {
        using var stream = typeof(Oracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference." + name)
            ?? throw new InvalidOperationException($"{name} isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<T>(stream, Options)!;
    }
}

internal sealed record ButtonOracle(string Baseui, ColourCase[] Colours, LayoutCase[] Layout, SpinnerCase[] Spinner, EnhancerFacts Enhancers);

internal sealed record ColourCase(string Theme, string Kind, string State, bool BackgroundSafe, string Background, string? Foreground, string BoxShadow)
{
    public override string ToString() => $"{Theme} {Kind} {State}{(BackgroundSafe ? " safe" : "")}";
}

internal sealed record LayoutCase(
    string Size, string Shape, string WidthType, double[] Padding,
    [property: JsonConverter(typeof(RadiusConverter))] double? Radius,
    double? Width, double? Height, double? MinWidth, double? MinHeight,
    double FontSize, string FontWeight, double LineHeight)
{
    /// <summary>Null radius is Base Web's 50%.</summary>
    public bool HalfRadius => Radius is null;

    public override string ToString() => $"{Size} {Shape} {WidthType}";
}

internal sealed record SpinnerCase(string Theme, string Kind, string Size, bool Disabled, double Diameter, double Thickness, string Foreground, string Background, double[] ContainerMargin)
{
    public override string ToString() => $"{Theme} {Kind} {Size}{(Disabled ? " disabled" : "")}";
}

internal sealed record EnhancerFacts(double StartMarginRight, double EndMarginLeft, Dictionary<string, double> ArtworkSize);

internal sealed record ButtonGroupOracle(string Baseui, GroupRootCase[] Root);

internal sealed record GroupRootCase(string Size, string Padding, bool? Wrap, double ColumnGap, double RowGap, double? PaddingLeft, double? PaddingRight, string? FlexWrap, string? OverflowX)
{
    public override string ToString() => $"{Size} padding {Padding} wrap {Wrap?.ToString() ?? "unset"}";
}

internal sealed record ThemeOracle(string Baseui, ThemeCase[] Cases)
{
    public ThemeCase this[string name] => Cases.Single(c => c.Name == name);
}

/// <summary>
/// One call of createTheme, createLightTheme or createDarkTheme: its overrides in Metered's keys, every colour
/// (null where Base Web gives no colour), every Button box-shadow, the other groups' leaves that changed, and the
/// groups that didn't change at all.
/// </summary>
internal sealed record ThemeCase(
    string Name, string Create, string ThemeName, OracleToken[] Overrides,
    Dictionary<string, string?> Colours, Dictionary<string, string> BoxShadows, string[] Unchanged, OracleToken[] Changed)
{
    public override string ToString() => Name;
}

/// <summary>A token in Avalonia's terms: its key, its type (colour, double, boxShadows, cornerRadius, …) and value.</summary>
internal sealed record OracleToken(string Key, string Type, JsonElement Value);

/// <summary>Reads a radius that is a number, or "half" for 50% (as null).</summary>
internal sealed class RadiusConverter : JsonConverter<double?>
{
    public override double? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.TokenType == JsonTokenType.String ? null : reader.GetDouble();

    public override void Write(Utf8JsonWriter writer, double? value, JsonSerializerOptions options) => throw new NotSupportedException();
}
