using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A select at each size, single and multi, empty or with values, select, search and not searchable: each part's lengths
/// and fonts, the icons' sizes, the tags' boxes, and where layout puts them, against Base Web's own Select rendered
/// (SelectOracle's layout), its height a browser's: the borders, the padding and the line's tallest item.
/// </summary>
public class SelectLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, SelectOracle.Data.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = SelectOracle.Data.Layout[index];
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select
        {
            Options = options,
            Size = SelectOracle.Size(c.Size),
            Multi = c.Multi,
            Searchable = c.Searchable,
            Type = c.Type == "search" ? SelectType.Search : SelectType.Select,
            IsLoading = true,
            Value = SelectOracle.Value(options, c.Values == 0 ? [] : c.Values == 1 ? ["Aqua"] : ["Aqua", "Azure"]),
        };
        using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Children = { select } });
        var at = c.ToString();

        // The control's border, radius and the values' padding; the whole height.
        var control = select.P<Border>(Select.ControlContainerPart);
        Assert.Equal(new Thickness(c.Border[0], c.Border[1], c.Border[2], c.Border[3]), control.BorderThickness);
        Assert.Equal(new CornerRadius(c.Radius), control.CornerRadius);
        var values = select.P<SelectValueContainer>(Select.ValueContainerPart);
        Assert.Equal(new Thickness(c.Padding[0], c.Padding[1], c.Padding[2], c.Padding[3]), values.Padding);
        Assert.Equal(c.Multi, values.Wrap);
        Assert.True(c.Height == select.Bounds.Height, at + ": height " + select.Bounds.Height);
        Assert.Equal(Scene.Width - 40, select.Bounds.Width);

        // The root's font, which the text inside takes.
        var root = select.P<Border>(Select.RootPart);
        Assert.Equal(c.Font.FontSize, root.GetValue(TextElement.FontSizeProperty));
        Assert.Equal(Enum.Parse<FontWeight>(c.Font.FontWeight), root.GetValue(TextElement.FontWeightProperty));
        Assert.Equal(c.Font.LineHeight, root.GetValue(TextBlock.LineHeightProperty));

        // The icons: their padding at the end, the ring and the arrow.
        var icons = select.P<StackPanel>(Select.IconsContainerPart);
        Assert.Equal(c.IconsPadding, icons.Margin.Right);
        var ring = select.P<SpinnerRing>(Select.LoadingIndicatorPart);
        Assert.Equal(c.Ring.Box, ring.BoxSize);
        Assert.Equal(c.Ring.Border, ring.Thickness);
        Assert.Equal(c.Ring.Box + 2 * c.Ring.Border, ring.Bounds.Width);
        var right = control.Bounds.Width - c.Border[2] - c.IconsPadding;
        var arrow = select.P<InputIcon>(Select.SelectArrowPart);
        if (c.Arrow is { } a)
        {
            Assert.True(arrow.IsVisible);
            Assert.Equal(a, arrow.Bounds.Width);
            Assert.Equal(a, arrow.Bounds.Height);
            Assert.Equal(right, arrow.TranslatePoint(new Point(arrow.Bounds.Width, 0), control)!.Value.X, 3);
            // Centred in the control's height (alignItems center, alignSelf stretch).
            Assert.Equal(control.Bounds.Height / 2, arrow.TranslatePoint(new Point(0, a / 2), control)!.Value.Y, 3);
        }
        else Assert.False(arrow.IsVisible);

        // The input's box: its margin and height; the input's font.
        var box = select.P<SelectInputContainer>(Select.InputContainerPart);
        Assert.Equal(c.InputContainer.MarginLeft, box.Margin.Left);
        Assert.Equal(c.InputContainer.MaxHeight, box.MaxHeight);
        if (c.InputContainer.Height is { } h) Assert.Equal(h, box.Height);
        else Assert.True(double.IsNaN(box.Height));
        var input = select.P<TextBox>(Select.InputPart);
        Assert.Equal(c.Searchable, input.IsVisible);
        if (c.Input is { } i)
        {
            Assert.Equal(i.Font.FontSize, input.FontSize);
            Assert.Equal(i.Font.LineHeight, input.LineHeight);
            // AutosizeInput: no text is its 2 px.
            Assert.Equal(SelectInput.Extra, input.Bounds.Width);
        }

        // The single value: its margin, its place after the padding.
        var single = select.P<ContentPresenter>(Select.SingleValuePart);
        if (c.SingleValue is { } sv)
        {
            Assert.True(single.IsVisible);
            Assert.Equal(sv.MarginLeft, single.Margin.Left);
            Assert.Equal(c.Border[0] + c.Padding[0] + sv.MarginLeft, single.TranslatePoint(default, control)!.Value.X, 3);
            Assert.Equal(c.Font.LineHeight, single.GetValue(TextBlock.LineHeightProperty));
        }

        Assert.Equal(c.Placeholder, select.Shown(Select.PlaceholderPart));

        // The search icon: over the root's start, its whole height.
        var search = select.P<Border>(Select.SearchIconContainerPart);
        Assert.Equal(c.Search is not null, search.IsVisible);
        if (c.Search is { } s)
        {
            Assert.Equal(s.Left, search.TranslatePoint(default, root)!.Value.X);
            Assert.Equal(s.Size, search.Bounds.Width);
            Assert.Equal(root.Bounds.Height, search.Bounds.Height);
            var icon = select.P<InputIcon>(Select.SearchIconPart);
            Assert.Equal(s.Icon, icon.Bounds.Width);
        }

        // The tags: Base Web's Tag's box, as multi-value.js gives it its margins.
        Assert.Equal(c.Tag is null ? 0 : c.Values, select.Tags.Count);
        if (c.Tag is { } t)
        {
            var first = select.Tags[0];
            var r = (RadioMark)first.Part(SelectTag.RootPart)!;
            Assert.Equal(t.Height, r.Bounds.Height);
            Assert.Equal(new Thickness(t.Margin[0], t.Margin[1], t.Margin[2], t.Margin[3]), r.Margin);
            Assert.Equal(new Thickness(t.Padding[0], t.Padding[1], t.Padding[2], t.Padding[3]), r.Padding);
            Assert.Equal(new Thickness(t.Border), r.BorderThickness);
            Assert.Equal(new CornerRadius(t.Radius), r.CornerRadius);
            Assert.Equal(t.Font.FontSize, r.GetValue(TextElement.FontSizeProperty));
            Assert.Equal(Enum.Parse<FontWeight>(t.Font.FontWeight), r.GetValue(TextElement.FontWeightProperty));
            Assert.Equal(t.TextMaxWidth, ((TextBlock)first.Part(SelectTag.TextPart)!).MaxWidth);
            var action = (Border)first.Part(SelectTag.ActionPart)!;
            Assert.Equal(t.ActionMargin, action.Margin.Left);
            Assert.Equal(new CornerRadius(t.ActionRadius), action.CornerRadius);
            var icon = (InputIcon)first.Part(SelectTag.ActionIconPart)!;
            Assert.Equal(t.ActionIcon, icon.Bounds.Width);
            Assert.Equal(Rect.Parse(t.ActionIconViewBox.Replace(' ', ',')), icon.ViewBox);
            // The first tag at the padding's start, its margin in; the second after it.
            Assert.Equal(c.Border[0] + c.Padding[0] + t.Margin[0], r.TranslatePoint(default, control)!.Value.X, 3);
            var second = (Visual)select.Tags[1].Part(SelectTag.RootPart)!;
            Assert.Equal(r.TranslatePoint(new Point(r.Bounds.Width, 0), control)!.Value.X + t.Margin[2] + t.Margin[0], second.TranslatePoint(default, control)!.Value.X, 3);
        }
    }

    [AvaloniaFact]
    public void The_clear_icon_is_sized_by_the_selects_size()
    {
        foreach (var (size, px) in SelectOracle.Data.ClearSizes)
        {
            var options = SelectOracle.Options("OPTIONS");
            var select = new Select { Options = options, Size = SelectOracle.Size(size), Value = SelectOracle.Value(options, ["Aqua"]) };
            using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Children = { select } });
            var clear = select.P<InputIcon>(Select.ClearIconPart);
            Assert.True(clear.IsVisible);
            Assert.Equal(px, clear.Bounds.Width);
            Assert.Equal(px, clear.Bounds.Height);
        }
    }

    [AvaloniaFact]
    public void Tags_wrap_onto_new_lines_and_a_long_value_is_cut_with_an_ellipsis()
    {
        var options = SelectOracle.Options("OPTIONS");
        var multi = new Select { Options = options, Multi = true, Value = SelectOracle.Value(options, ["AliceBlue", "Aqua", "Aquamarine", "Azure", "Bisque", "Black", "BlanchedAlmond"]) };
        var single = new Select { Options = new List<object> { new SelectOption("x", new string('W', 200)) }, Value = [new SelectOption("x")] };
        using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Spacing = 20, Children = { multi, single } });
        var rows = multi.Tags.Select(t => Math.Round(((Visual)t.Part(SelectTag.RootPart)!).TranslatePoint(default, multi)!.Value.Y)).Distinct().Count();
        Assert.True(rows >= 2, rows + " rows");
        var tag = SelectOracle.Data.Layout.First(l => l.Tag is not null && l.Size == "default").Tag!;
        var line = tag.Height + tag.Margin[1] + tag.Margin[3];
        var padding = SelectOracle.Data.Layout.First(l => l.Tag is not null && l.Size == "default").Padding;
        Assert.Equal(4 + padding[1] + padding[3] + rows * line, multi.Bounds.Height);
        // The single value shrinks to its row: its text cut, the select its own height.
        var value = single.P<ContentPresenter>(Select.SingleValuePart);
        var row = single.P<SelectValueContainer>(Select.ValueContainerPart);
        Assert.True(value.Bounds.Right <= row.Bounds.Width - row.Padding.Right + 0.01);
        Assert.Equal(48, single.Bounds.Height);
    }
}
