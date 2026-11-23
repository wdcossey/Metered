using System.Text.Json;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// What Base Web's Menu gives (tools/reference/menu.js, through cases.js): its own StatefulMenu rendered for each case, each
/// part's style function evaluated with the props it was given; its own stateful container driven step by step; and its
/// own scrollItemIntoView, in Avalonia's units: colours as #AARRGGBB, lengths in pixels. Read as
/// <see cref="ProgressBarOracle"/> reads the progress bar's.
/// </summary>
internal static class MenuOracle
{
    public static MenuReference Data { get; } = Load();

    private static MenuReference Load()
    {
        using var stream = typeof(MenuOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.menu.oracle.json")
            ?? throw new InvalidOperationException("menu.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<MenuReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>One of the oracle's lists by name, or the items a case gives as they are, as the menu's items.</summary>
    public static List<object> Items(string? name, JsonElement? given) =>
        Items(name is not null ? Data.Lists[name] : given ?? throw new InvalidOperationException("a case with no items"));

    /// <summary>
    /// Base Web's items as Metered's: an array of items, or grouped (an object of keys, <c>__ungrouped</c> without a header).
    /// An item with <c>label</c>, <c>disabled</c>, <c>divider</c> or <c>href</c> is a <see cref="MenuItemData"/>; grouped.tsx's
    /// <c>{ id, color }</c> a <see cref="Swatch"/>; a profile's <c>{ title, subtitle, body }</c> a <see cref="Person"/>.
    /// </summary>
    public static List<object> Items(JsonElement items)
    {
        if (items.ValueKind == JsonValueKind.String) return Items(Data.Lists[items.GetString()!]);
        if (items.ValueKind == JsonValueKind.Array) return [.. items.EnumerateArray().Select(Item)];
        var all = new List<object>();
        foreach (var key in items.EnumerateObject())
        {
            if (key.Name == "__ungrouped") all.AddRange(key.Value.EnumerateArray().Select(Item));
            else
            {
                var group = new MenuGroup { Label = key.Name };
                group.Items.AddRange(key.Value.EnumerateArray().Select(Item));
                all.Add(group);
            }
        }

        return all;
    }

    private static object Item(JsonElement e)
    {
        string? S(string k) => e.TryGetProperty(k, out var v) ? v.GetString() : null;
        bool B(string k) => e.TryGetProperty(k, out var v) && v.GetBoolean();
        if (e.TryGetProperty("id", out _)) return new Swatch(S("id")!, S("color")!);
        if (e.TryGetProperty("title", out _)) return new Person(S("title"), S("subtitle"), S("body"));
        return new MenuItemData { Label = S("label"), Disabled = B("disabled"), Divider = B("divider"), Href = S("href") };
    }

    /// <summary>The theme variant a case's theme names.</summary>
    public static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;
}

/// <summary>grouped.tsx's item: a colour's name and its value, labelled by its name.</summary>
internal sealed record Swatch(string Id, string Color);

/// <summary>A profile's item: menu-profile-menu.scenario.tsx's title, subtitle and body.</summary>
internal sealed record Person(string? Title, string? Subtitle, string? Body);

internal sealed record MenuReference(
    string Baseui, Dictionary<string, JsonElement> Lists, MenuColourCase[] Colours, MenuSurfaceCase[] Surfaces, MenuLayout Layout,
    MenuStructureCase[] Structure, MenuBehaviourCase[] Behaviour, MenuScrollCase[] Scroll);

/// <summary>An option in a state, by theme: its text and fill colours and its cursor, and its anchor's colour (null where not drawn).</summary>
internal sealed record MenuColourCase(string Theme, string State, string Foreground, string Background, string Cursor, string? Anchor)
{
    public override string ToString() => $"{Theme} {State}";
}

/// <summary>The list, a header, a divider, the empty state and the profile's parts, by theme.</summary>
internal sealed record MenuSurfaceCase(
    string Theme, string List, string ListShadow, string ListFocusOutline, string Header, string Divider, string Empty,
    string? ProfileHover, string ProfileTitle, string ProfileSubtitle, string ProfileBody)
{
    public override string ToString() => Theme;
}

internal sealed record MenuFont(double FontSize, string FontWeight, double LineHeight);

internal sealed record MenuOptionLayout(string Size, double[] Padding, MenuFont Font);

internal sealed record MenuListLayout(double[] Padding, double Radius, double FocusOutlineWidth);

internal sealed record MenuBoxLayout(double[] Padding, MenuFont Font);

internal sealed record MenuDividerLayout(double[] Margin, double BorderWidth, double Height);

internal sealed record MenuEmptyLayout(double[] Padding, MenuFont Font, string TextAlign);

internal sealed record MenuProfileLayout(
    double[] Padding, double[] ImgContainer, string ImgRadius, double LabelsMarginLeft, MenuFont Title, MenuFont Subtitle, MenuFont Body);

internal sealed record MenuTransition(string Property, double Seconds, double[] Curve);

internal sealed record MenuLayout(
    MenuOptionLayout[] Options, MenuListLayout List, MenuBoxLayout Header, MenuDividerLayout Divider, MenuEmptyLayout Empty,
    MenuProfileLayout Profile, MenuTransition Transition, Dictionary<string, string> Locale);

internal sealed record MenuInitialState(int? HighlightedIndex, bool? IsFocused);

/// <summary>A part Base Web rendered, in order: an option's text, size, states and ARIA; a header's or the empty state's text.</summary>
internal sealed record MenuElement(
    string Part, string? Text, string? Size, bool Highlighted, bool Focused, bool Disabled, bool AriaSelected, bool AriaDisabled,
    string? Role, string? Href, string? AriaLive);

internal sealed record MenuProfileText(string Part, string Text);

/// <summary>A case Base Web rendered: what it gives (its items, Option props, state, message, name) and what it rendered.</summary>
internal sealed record MenuStructureCase(
    string Name, string? Items, JsonElement? ItemsGiven, string? Size, string? Label, bool RenderHrefAsAnchor, bool Profile,
    MenuInitialState? InitialState, string? NoResultsMsg, string? AriaLabelGiven, string Role, string AriaLabel, int TabIndex,
    MenuElement[] Elements, MenuProfileText[] ProfileText)
{
    public override string ToString() => Name;
}

/// <summary>A step of a sequence (one of its fields set) and the state Base Web's container is in after it.</summary>
internal sealed record MenuStep(
    string? Key, bool Focus, bool Blur, bool Over, int? Enter, int? Click, int? Wait, JsonElement? Items,
    int HighlightedIndex, bool IsFocused, int[] Selected, string[] Changes)
{
    public override string ToString() =>
        Key is not null ? $"key '{Key}'" : Focus ? "focus" : Blur ? "blur" : Over ? "over" : Enter is { } e ? $"enter {e}"
        : Click is { } c ? $"click {c}" : Wait is { } w ? $"wait {w}" : "items";
}

internal sealed record MenuBehaviourCase(
    string Name, string? Items, JsonElement? ItemsGiven, MenuInitialState? InitialState, bool TypeAhead, bool ForceHighlight,
    string? Reducer, MenuStep[] Steps)
{
    public override string ToString() => Name;
}

internal sealed record MenuScrollStep(string Key, int HighlightedIndex, double ScrollTop);

/// <summary>A list of options of one size and a box height, its first highlight, and the scroll after each key.</summary>
internal sealed record MenuScrollCase(
    string Name, int Count, string Size, double CssHeight, double BoxHeight, int Start, double Item, double InitialScrollTop, MenuScrollStep[] Steps)
{
    public override string ToString() => Name;
}
