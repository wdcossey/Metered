using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Metered.Controls;

namespace Metered.Tests;

/// <summary>
/// What Base Web's tabs-motion gives (tools/reference/tabs.js, through cases.js): its Tabs and StatefulTabs rendered for
/// each case, each part's props recorded; its own key handlers driven step by step; its style functions evaluated, in
/// Avalonia's units: colours as #AARRGGBB, lengths in pixels.
/// </summary>
internal static class TabsOracle
{
    public static TabsReference Data { get; } = Load();

    private static TabsReference Load()
    {
        using var stream = typeof(TabsOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.tabs.oracle.json")
            ?? throw new InvalidOperationException("tabs.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<TabsReference>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>A case's tabs built as Metered's: the same tabs, keys, titles, artwork, content and props.</summary>
    public static Tabs Make(TabsInput input)
    {
        var tabs = new Tabs
        {
            Disabled = input.Disabled ?? false,
            Orientation = input.Orientation == "vertical" ? Orientation.Vertical : Orientation.Horizontal,
            Fill = input.Fill == "fixed" ? TabsFill.Fixed : TabsFill.Intrinsic,
            ActivateOnFocus = input.ActivateOnFocus ?? true,
            RenderAll = input.RenderAll ?? false,
            EndEnhancer = input.EndEnhancer,
        };
        for (var i = 0; i < input.Tabs.Length; i++)
        {
            var t = input.Tabs[i];
            tabs.Items.Add(new Tab
            {
                Title = t.Title,
                Key = t.Key,
                Disabled = t.Disabled,
                // The oracle's artwork renders no text of its own: a box of the artwork's size.
                Artwork = t.Artwork == true ? new Border { Width = 20, Height = 20 } : null,
                Content = t.Content ?? "Content " + i,
            });
        }

        if (input.Stateful == true)
        {
            if (input.InitialActiveKey is not null) tabs.ActiveKey = input.InitialActiveKey;
        }
        else
        {
            // Tabs' own default activeKey, '0'; a number is compared as text (key == activeKey).
            tabs.ActiveKey = input.ActiveKey is { } k ? (k.ValueKind == JsonValueKind.String ? k.GetString() : k.GetRawText()) : "0";
        }

        return tabs;
    }
}

internal sealed record TabsReference(
    string Baseui, TabsStructureCase[] Structure, TabsKeysCase[] Keys, TabsColourCase[] Colours, TabsLayoutCase[] Layout, TabsMotion Motion);

internal sealed record TabsInput(
    TabsInputTab[] Tabs, JsonElement? ActiveKey, bool? Disabled, string? Orientation, string? Fill, bool? ActivateOnFocus,
    bool? RenderAll, string? EndEnhancer, bool? Stateful, string? InitialActiveKey);

internal sealed record TabsInputTab(string? Title, string? Key, bool? Disabled, bool? Artwork, string? Content);

/// <summary>A case rendered: the parts in order, each tab's and panel's state, the list's, the bar's and the highlight's.</summary>
internal sealed record TabsStructureCase(
    string Name, TabsInput Input, string[] Parts, TabsListFacts TabList, bool HasEndEnhancer, bool EndEnhancerShown,
    TabsTabFacts[] Tabs, TabsPanelFacts[] Panels, TabsHighlightFacts Highlight, TabsArtwork? ArtworkProps)
{
    public override string ToString() => Name;
}

internal sealed record TabsListFacts(string Role, string Orientation);

internal sealed record TabsTabFacts(string Text, bool Artwork, bool Selected, bool TabStop, bool Disabled, string Key, bool ControlsItsPanel);

internal sealed record TabsPanelFacts(bool Hidden, string? Content, bool LabelledByItsTab);

internal sealed record TabsHighlightFacts(string Role, bool Hidden, double Length, double Distance, bool Animate);

internal sealed record TabsArtwork(double Size, string Color);

/// <summary>Keys pressed on a focused tab, and after each: the tab focused, the active key, onChange's keys, preventDefault.</summary>
internal sealed record TabsKeysCase(string Name, TabsInput Input, int Start, TabsStep[] Steps)
{
    public override string ToString() => Name;
}

internal sealed record TabsStep(string Key, int Focused, string ActiveKey, string[] Changes, bool Prevented);

internal sealed record TabsColourCase(
    string Theme, string State, string Background, string Foreground, string Cursor, TabsOutline? Outline, string Highlight, string Border)
{
    public override string ToString() => Theme + " " + State;
}

internal sealed record TabsOutline(double Width, string Colour, double Offset);

internal sealed record TabsLayoutCase(
    string Orientation, string Fill, string RootDisplay, string TabBarDisplay, string TabBarDisplayWithEnhancer, TabsListLayout TabList,
    TabsTabLayout Tab, double[] ArtworkMargin, TabsEndLayout EndEnhancer, TabsHighlightLayout Highlight, TabsBorderLayout Border,
    double[] PanelPadding)
{
    public override string ToString() => Orientation + " " + Fill;
}

internal sealed record TabsListLayout(
    string FlexDirection, string FlexWrap, double FlexGrow, double[] Padding, double[] Margin, string OverflowX, string OverflowY, string? ScrollbarWidth);

internal sealed record TabsTabLayout(
    double[] Padding, string JustifyContent, string AlignItems, double FlexGrow, JsonElement FlexBasis, double FontSize, string FontWeight, double LineHeight);

internal sealed record TabsEndLayout(double[] Margin, string AlignItems, string JustifyContent);

internal sealed record TabsHighlightLayout(double? Width, double? Height, string? Left, string? Right, string? Bottom, string Transform, int ZIndex);

internal sealed record TabsBorderLayout(double? Width, double? Height);

internal sealed record TabsMotion(string? HighlightStill, TabsTransition Highlight, TabsTransition Tab);

internal sealed record TabsTransition(string Property, double Seconds, double[] Curve);
