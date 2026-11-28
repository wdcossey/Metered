using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Three tabs in each orientation and fill, an artwork and an end enhancer: the parts' lengths and boxes against Base
/// Web's style functions (TabsOracle), and where layout puts them, as a browser lays out Base Web's boxes: the tabs in a
/// line, their padding round their LabelSmall line; the tab list's padding under them (beside them vertically) and its
/// margin as much the other way, so the border meets the tabs and the highlight, which is the active tab's width and
/// place (tabs.js's getLayoutParams: clientWidth and offsetLeft), lies over it; the panel after the border, padded.
/// </summary>
public class TabsLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, TabsOracle.Data.Layout.Length));

    private static Thickness T(double[] s) => new(s[0], s[1], s[2], s[3]);

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = TabsOracle.Data.Layout[index];
        var vertical = c.Orientation == "vertical";
        var tabs = new Tabs
        {
            Orientation = vertical ? Orientation.Vertical : Orientation.Horizontal,
            Fill = c.Fill == "fixed" ? TabsFill.Fixed : TabsFill.Intrinsic,
            ActiveKey = "1",
            EndEnhancer = "End",
            Items =
            {
                new Tab { Title = "Robot", Artwork = new Border { Width = 20, Height = 20 }, Content = "One" },
                new Tab { Title = "Monster", Content = "Two" },
                new Tab { Title = "Watermelon", Content = "Three" },
            },
        };
        // In a page's flow, as tall as it is (a window's content would stretch it).
        using var scene = new Scene(new StackPanel { Children = { tabs } });

        // Root: a block, or vertical's flex row.
        Assert.Equal(c.RootDisplay == "flex" ? Orientation.Horizontal : Orientation.Vertical, ((TabsRootPanel)tabs.Part(Tabs.RootPart)!).Orientation);
        Assert.Equal(c.TabBarDisplayWithEnhancer == "flex", tabs.Part(Tabs.TabBarPart)!.Classes.Contains(":has-end-enhancer") || c.TabBarDisplay == "flex");

        // TabList: a line in its direction that doesn't wrap, its padding and margin, scrolling with no bar (or not at all).
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        Assert.Equal("nowrap", c.TabList.FlexWrap);
        Assert.Equal(c.TabList.FlexDirection == "row" ? Orientation.Horizontal : Orientation.Vertical, list.Orientation);
        Assert.Equal(T(c.TabList.Padding), list.Padding);
        Assert.Equal(T(c.TabList.Margin), list.Margin);
        ScrollBarVisibility Bar(string overflow) => overflow == "scroll" ? ScrollBarVisibility.Hidden : ScrollBarVisibility.Disabled;
        if (c.TabList.OverflowX == "scroll" || c.TabList.OverflowY == "scroll") Assert.Equal("none", c.TabList.ScrollbarWidth);
        Assert.Equal(Bar(c.TabList.OverflowX), list.HorizontalScrollBarVisibility);
        Assert.Equal(Bar(c.TabList.OverflowY), list.VerticalScrollBarVisibility);

        // The tabs: padding, type, flex, their content's place.
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        foreach (var b in buttons)
        {
            Assert.Equal(T(c.Tab.Padding), b.Padding);
            Assert.Equal(c.Tab.FontSize, b.FontSize);
            Assert.Equal(Enum.Parse<FontWeight>(c.Tab.FontWeight), b.FontWeight);
            Assert.Equal(c.Tab.LineHeight, TextBlock.GetLineHeight(b));
            Assert.Equal(c.Tab.FlexGrow, b.FlexGrow);
            if (c.Tab.FlexBasis.ValueKind == JsonValueKind.String) Assert.True(double.IsNaN(b.FlexBasis));
            else Assert.Equal(c.Tab.FlexBasis.GetDouble(), b.FlexBasis);
            Assert.Equal(c.Tab.JustifyContent == "center" ? HorizontalAlignment.Center : HorizontalAlignment.Right, b.HorizontalContentAlignment);
            Assert.Equal("center", c.Tab.AlignItems);
            Assert.Equal(VerticalAlignment.Center, b.VerticalContentAlignment);
        }

        // The line: the first tab's artwork (tab.js renders it at size 20, Tabs.ArtworkSize) is taller than a line of
        // LabelSmall, and every tab is as tall as the tallest (align-items: stretch).
        var content = Math.Max(c.Tab.LineHeight, 20);
        var lineHeight = content + c.Tab.Padding[1] + c.Tab.Padding[3];
        if (!vertical) Assert.All(buttons, b => Assert.Equal(lineHeight, b.Bounds.Height));
        else
        {
            Assert.Equal(lineHeight, buttons[0].Bounds.Height);
            Assert.All(buttons.Skip(1), b => Assert.Equal(c.Tab.LineHeight + c.Tab.Padding[1] + c.Tab.Padding[3], b.Bounds.Height));
        }
        Assert.Equal(T(c.ArtworkMargin), ((Layoutable)tabs.Part(Tabs.ArtworkContainerPart)!).Margin);

        // The end enhancer, horizontal only: its margin, its content centred and at the end.
        var end = (ContentPresenter)tabs.Part(Tabs.EndEnhancerContainerPart)!;
        Assert.Equal(!vertical, end.IsVisible);
        Assert.Equal(T(c.EndEnhancer.Margin), end.Margin);
        Assert.Equal(HorizontalAlignment.Right, end.HorizontalContentAlignment);
        Assert.Equal(VerticalAlignment.Center, end.VerticalContentAlignment);

        // The highlight and the border: their thickness, colours aside (TabsColourTests).
        var highlight = (TabHighlight)tabs.Part(Tabs.TabHighlightPart)!;
        var border = (Border)tabs.Part(Tabs.TabBorderPart)!;
        Assert.Equal(c.Highlight.ZIndex, highlight.ZIndex);
        var active = buttons[1];
        var at = active.TranslatePoint(default, list)!.Value;
        var hl = highlight.TranslatePoint(default, list)!.Value;
        var bp = border.TranslatePoint(default, tabs)!.Value;
        var lp = list.TranslatePoint(default, tabs)!.Value;
        if (!vertical)
        {
            Assert.Equal(c.Highlight.Height, highlight.Bounds.Height);
            Assert.Equal("0px", c.Highlight.Bottom);
            Assert.Equal(c.Border.Height, border.Bounds.Height);
            // getLayoutParams: the tab's clientWidth and offsetLeft, at the bottom of the list's padding box.
            Assert.Equal(Math.Round(active.Bounds.Width), highlight.Bounds.Width);
            Assert.Equal(Math.Round(at.X), hl.X);
            Assert.Equal(list.Bounds.Height - c.Highlight.Height!.Value, hl.Y);
            Assert.Equal(lineHeight + c.TabList.Padding[3], list.Bounds.Height);
            // The border starts where the tabs end, under the highlight, which is drawn over it.
            Assert.Equal(lineHeight, bp.Y);
            Assert.Equal(lp.Y + hl.Y, bp.Y);
            Assert.Equal(Scene.Width, border.Bounds.Width);
            var panel = (ContentPresenter)tabs.Elements(Tabs.TabPanelPart)[1];
            Assert.Equal(lineHeight + c.Border.Height!.Value, panel.TranslatePoint(default, tabs)!.Value.Y);
        }
        else
        {
            Assert.Equal(c.Highlight.Width, highlight.Bounds.Width);
            Assert.Equal("0px", c.Highlight.Right);
            Assert.Equal(c.Border.Width, border.Bounds.Width);
            Assert.Equal(Math.Round(active.Bounds.Height), highlight.Bounds.Height);
            Assert.Equal(Math.Round(at.Y), hl.Y);
            Assert.Equal(list.Bounds.Width - c.Highlight.Width!.Value, hl.X);
            // The tabs as wide as the line, the border after them, under the highlight, the panel after the border.
            Assert.All(buttons, b => Assert.Equal(buttons.Max(x => x.Bounds.Width), b.Bounds.Width));
            Assert.Equal(buttons[0].Bounds.Width, bp.X);
            Assert.Equal(lp.X + hl.X, bp.X);
            var panel = (ContentPresenter)tabs.Elements(Tabs.TabPanelPart)[1];
            Assert.Equal(bp.X + c.Border.Width!.Value, panel.TranslatePoint(default, tabs)!.Value.X);
            Assert.Equal(Scene.Width - bp.X - c.Border.Width!.Value, panel.Bounds.Width);
        }

        foreach (var p in tabs.Elements(Tabs.TabPanelPart).Cast<ContentPresenter>()) Assert.Equal(T(c.PanelPadding), p.Padding);

        // Fixed: the line's room shared equally (flex-grow 1, flex-basis 0), horizontally the window's width.
        if (c.Fill == "fixed" && !vertical)
        {
            Assert.All(buttons, b => Assert.InRange(b.Bounds.Width, (Scene.Width - end.Bounds.Width - end.Margin.Left) / 3 - 1, (Scene.Width - end.Bounds.Width - end.Margin.Left) / 3 + 1));
        }
    }

    // Fixed and vertical, in a root 250 px high (tabs-motion-fixed-vertical.scenario.tsx): the column shared equally.
    [AvaloniaFact]
    public void Fixed_vertical_tabs_share_the_height()
    {
        var root = new ControlThemeBuilder(typeof(TabsRootPanel)).Set(Layoutable.HeightProperty, 250.0).Theme;
        var tabs = new Tabs
        {
            Orientation = Orientation.Vertical,
            Fill = TabsFill.Fixed,
            Overrides = [new Override { Part = Tabs.RootPart, Style = root }],
            Items = { new Tab { Title = "Robot" }, new Tab { Title = "Monster" }, new Tab { Title = "Watermelon" } },
        };
        using var scene = new Scene(new StackPanel { Children = { tabs } });
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        Assert.All(buttons, b => Assert.InRange(b.Bounds.Height, 250.0 / 3 - 1, 250.0 / 3 + 1));
        Assert.Equal(250, buttons.Sum(b => b.Bounds.Height), 6);
    }

    // Intrinsic tabs that don't fit shrink, as CSS's flex items do (flex-shrink 1), to no less than their min-content
    // width (min-width: auto: the widest word of the title, with the padding), their titles wrapping; then they overflow.
    [AvaloniaFact]
    public void Intrinsic_tabs_shrink_to_their_longest_word_then_overflow()
    {
        var tabs = new Tabs();
        for (var i = 0; i < 12; i++) tabs.Items.Add(new Tab { Title = "Tab number " + i });
        using var scene = new Scene(tabs);
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        var text = (TextBlock)buttons[0].TitlePresenter!.Child!;
        var widest = new Avalonia.Media.TextFormatting.TextLayout("number", new Typeface(text.FontFamily, text.FontStyle, text.FontWeight), text.FontSize, null).WidthIncludingTrailingWhitespace;
        Assert.All(buttons, b => Assert.InRange(b.Bounds.Width, widest + 32, widest + 33));
        // Three lines each.
        Assert.All(buttons, b => Assert.Equal(3 * 16 + 32, b.Bounds.Height));
        Assert.True(list.Extent.Width > list.Viewport.Width);
    }

    // The scroll scenario's single words don't wrap: each tab its title's width, the line overflowing.
    [AvaloniaFact]
    public void Single_words_keep_their_width_and_overflow()
    {
        var tabs = new Tabs();
        foreach (var w in new[] { "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten" }) tabs.Items.Add(new Tab { Title = w });
        using var scene = new Scene(tabs);
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        Assert.All(buttons, b => Assert.Equal(48, b.Bounds.Height));
        var list = (TabList)tabs.Part(Tabs.TabListPart)!;
        Assert.Equal(buttons.Sum(b => b.Bounds.Width), list.Extent.Width, 0);
        Assert.Equal(Scene.Width, list.Viewport.Width);
    }
}

/// <summary>A theme of setters, for the tests' overrides.</summary>
internal sealed class ControlThemeBuilder(Type type)
{
    public Avalonia.Styling.ControlTheme Theme { get; } = new(type);

    public ControlThemeBuilder Set(AvaloniaProperty property, object? value)
    {
        Theme.Setters.Add(new Avalonia.Styling.Setter(property, value));
        return this;
    }
}
