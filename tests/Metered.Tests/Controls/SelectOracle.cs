using System.Text.Json;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// What Base Web's Select gives (tools/reference/select.js, through cases.js): its own Select rendered for each case with
/// its dropdown and tags, each part's style function evaluated with the props it was given; its own class driven step by
/// step; and its defaultFilterOptions, in Avalonia's units: colours as #AARRGGBB, lengths in pixels. Read as
/// <see cref="MenuOracle"/> reads the menu's.
/// </summary>
internal static class SelectOracle
{
    public static SelectReference Data { get; } = Load();

    private static SelectReference Load()
    {
        using var stream = typeof(SelectOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.select.oracle.json")
            ?? throw new InvalidOperationException("select.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<SelectReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>One of the oracle's lists by name, or the options a case gives as they are, as the select's options.</summary>
    public static List<object> Options(string? name, JsonElement? given = null) =>
        Options(name is not null ? Data.Lists[name] : given ?? throw new InvalidOperationException("a case with no options"));

    /// <summary>Base Web's options as Metered's: an array, or grouped (an object of keys, <c>__ungrouped</c> without a header).</summary>
    public static List<object> Options(JsonElement options)
    {
        if (options.ValueKind == JsonValueKind.Array) return [.. options.EnumerateArray().Select(Option)];
        var all = new List<object>();
        foreach (var key in options.EnumerateObject())
        {
            if (key.Name == "__ungrouped") all.AddRange(key.Value.EnumerateArray().Select(Option));
            else
            {
                var group = new SelectGroup { Label = key.Name };
                group.Items.AddRange(key.Value.EnumerateArray().Select(Option));
                all.Add(group);
            }
        }

        return all;
    }

    private static object Option(JsonElement e) => new SelectOption
    {
        Id = e.GetProperty("id").GetString(),
        Label = e.GetProperty("label").GetString(),
        Disabled = e.TryGetProperty("disabled", out var d) && d.GetBoolean(),
    };

    /// <summary>The value a case names by its labels, each the option of that label among these options (a new one where none is).</summary>
    public static List<object> Value(IEnumerable<object> options, IEnumerable<string>? labels, string? unclearable = null)
    {
        var flat = options.SelectMany(o => o is SelectGroup g ? g.Items : [o]).OfType<SelectOption>().ToList();
        return [.. (labels ?? []).Select(l =>
        {
            var o = flat.FirstOrDefault(x => (string?)x.Label == l) ?? new SelectOption(l);
            if (l == unclearable) o.ClearableValue = false;
            return (object)o;
        })];
    }

    public static SelectSize Size(string s) => Enum.Parse<SelectSize>(s, ignoreCase: true);
}

public sealed record SelectReference(
    string Baseui, Dictionary<string, JsonElement> Lists, SelectLocale Locale, SelectStructure[] Structure, SelectDropdown[] Dropdown,
    SelectColour[] Colours, SelectSurfaces[] Surfaces, SelectLayout[] Layout, SelectBehaviour[] Behaviour, SelectFilter[] Filtering,
    SelectTransition Transition, Dictionary<string, double> ClearSizes);

public sealed record SelectLocale(string NoResultsMsg, string Placeholder, string Create);

public sealed record SelectPopoverProps(string AccessibilityType, bool AutoFocus, bool FocusLock, double PopoverMargin, string Placement);

/// <summary>A rendered part: what the tests check of it (the rest null).</summary>
public sealed record SelectElement(
    string Part, string? Text, bool? Disabled, string? AriaLabel, string? Role, int? TabIndex, bool? Action, double? ActionIconSize,
    double? Size, string? Title, bool? Highlighted, bool? Selected, bool? AriaSelected, bool? AriaExpanded, string? AriaHaspopup,
    bool? AriaMultiselectable, bool? AriaRequired, string? Value);

public sealed record SelectStructure(
    string Name, string[] Value, bool? Clearable, bool? Disabled, bool? IsLoading, bool? Searchable, string? Type, bool? Multi, string? Unclearable,
    string? Placeholder, bool? Required, SelectPopoverProps Popover, SelectElement[] Elements)
{
    [System.Text.Json.Serialization.JsonPropertyName("aria-label")]
    public string? AriaLabelGiven { get; init; }

    public override string ToString() => Name;
}

public sealed record SelectDropdown(
    string Name, string? Options, JsonElement? OptionsGiven, string[] Value, bool? Multi, bool? FilterOutSelected, string? NoResultsMsg, string? Size,
    string? MaxDropdownHeight, string? MaxHeight, bool ListId, string ContainerWidth, SelectElement[] Elements)
{
    public override string ToString() => Name;
}

public sealed record SelectColour(string Theme, string State, string Foreground, string Border, string Background, string Placeholder, string Input,
    string Arrow, string Search, string Cursor)
{
    public override string ToString() => Theme + " " + State;
}

public sealed record SelectTagColours(string Foreground, string Background, string? Border, double BorderWidth, string? Hover, string? Active,
    string? FocusRing, string Cursor, string ActionCursor);

public sealed record SelectOptionColours(string? Selected, string? SelectedHighlighted, string? Plain, string ListItem, string ListItemHighlighted,
    string SelectedWeight, string PlainWeight, string DisabledCursor, string Cursor);

public sealed record SelectRing(string Top, string Track);

public sealed record SelectSurfaces(string Theme, SelectTagColours Tag, SelectTagColours TagDisabled, SelectRing Ring, SelectOptionColours Option);

public sealed record SelectFont(double FontSize, string FontWeight, double LineHeight);

public sealed record SelectInputContainerBox(double MarginLeft, double? Height, double MaxHeight);

public sealed record SelectInputBox(string Width, SelectFont Font);

public sealed record SelectSingleValueBox(double MarginLeft, double? LineHeight);

public sealed record SelectSearchBox(double Left, double Size, double Icon);

public sealed record SelectRingBox(double Box, double Border);

public sealed record SelectTagBox(double Height, double[] Margin, double[] Padding, double Border, double Radius, SelectFont Font, double TextMaxWidth,
    double ActionMargin, double ActionRadius, double ActionIcon, string ActionIconViewBox);

public sealed record SelectLayout(
    string Size, string Kind, bool Multi, int Values, string Type, bool Searchable, SelectFont Font, double Radius, double[] Border, double[] Padding,
    double IconsPadding, SelectInputContainerBox InputContainer, SelectInputBox? Input, SelectSingleValueBox? SingleValue, bool Placeholder,
    SelectSearchBox? Search, SelectRingBox Ring, double? ClearIcon, double? Arrow, SelectTagBox? Tag, double Height)
{
    public override string ToString() => Size + " " + Kind;
}

public sealed record SelectChange(string Type, string? Option, string[] Value);

public sealed record SelectStep(
    string? Click, string? Tap, string? Drag, bool? Clear, string? Choose, string? Remove, bool? ClickOutside, bool? TapOutside, string? Key, bool? Shift,
    string? Type, bool? Focus, bool? Blur, bool IsOpen, bool IsFocused, bool IsPseudoFocused, string InputValue, string[] Value, SelectChange[] Changes,
    string[] Opens, string[] Options, bool Placeholder, bool ShowValue)
{
    public override string ToString() =>
        Click is not null ? "click " + Click : Tap is not null ? "tap " + Tap : Drag is not null ? "drag " + Drag : Clear == true ? "clear"
        : Choose is not null ? "choose " + Choose : Remove is not null ? "remove " + Remove : ClickOutside == true ? "click outside"
        : TapOutside == true ? "tap outside" : Key is not null ? "key " + (Shift == true ? "Shift+" : "") + Key : Type is not null ? "type '" + Type + "'"
        : Focus == true ? "focus" : "blur";
}

public sealed record SelectBehaviour(string Name, string[] Value, string? Unclearable, Dictionary<string, JsonElement> Props, SelectStep[] Steps)
{
    public override string ToString() => Name;
}

public sealed record SelectFilter(string Filter, string[]? Exclude, bool IgnoreCase, string[] Labels);

public sealed record SelectTransition(string Property, double Seconds, double[] Curve);
