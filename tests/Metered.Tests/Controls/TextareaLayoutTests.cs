using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A clearable textarea with a value at each size and each resize value, and with other rows: each part's lengths and
/// font, the textarea's resize, the clear icon's size, and where layout puts them, against Base Web's own Textarea
/// rendered (TextareaOracle), laid out as a browser lays out a textarea of its rows.
/// </summary>
public class TextareaLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, TextareaOracle.Value.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = TextareaOracle.Value.Layout[index];
        var textarea = new Textarea
        {
            Size = Enum.Parse<InputSize>(c.Size, ignoreCase: true),
            Resize = c.Resize is null ? null : Enum.Parse<TextareaResize>(c.Resize, ignoreCase: true),
            Rows = c.Rows,
            Text = "Value",
            Clearable = true,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { textarea } });

        // Root: its border, radius and padding, and no font of its own (textarea.js gives it no size).
        var root = (Border)textarea.Part(Textarea.RootPart)!;
        Assert.Equal(new Thickness(c.RootBorder[0], c.RootBorder[1], c.RootBorder[2], c.RootBorder[3]), root.BorderThickness);
        Assert.Equal(new CornerRadius(c.RootRadius), root.CornerRadius);
        Assert.Equal(new Thickness(c.RootPadding[0], 0, c.RootPadding[1], 0), root.Padding);
        Assert.False(root.IsSet(TextElement.FontSizeProperty));
        Assert.False(root.IsSet(TextBlock.LineHeightProperty));
        Assert.Equal(c.Height, textarea.Bounds.Height);
        Assert.Equal(c.Height, root.Bounds.Height);

        var container = (Border)textarea.Part(BaseInput.InputContainerPart)!;
        AssertFont(c.ContainerFont, container);
        var inner = root.Bounds.Height - c.RootBorder[1] - c.RootBorder[3];
        Assert.Equal(new Rect(c.RootBorder[0], c.RootBorder[1], root.Bounds.Width - c.RootBorder[0] - c.RootBorder[2], inner),
            InputLayoutTests.At(container, root));

        // The textarea: its padding and font, its resize, its height its rows' lines and its padding; its text inside the
        // padding, which is the scroller's.
        var field = (TextareaField)textarea.Part(BaseInput.InputPart)!;
        Assert.Equal(new Thickness(c.InputPadding[0], c.InputPadding[1], c.InputPadding[2], c.InputPadding[3]), field.Padding);
        AssertFont(c.InputFont, field);
        Assert.Equal(Enum.Parse<TextareaResize>(c.InputResize, ignoreCase: true), field.Resize);
        var lines = c.Rows > 0 ? c.Rows : 2;
        Assert.Equal(c.InputPadding[1] + lines * c.InputFont.LineHeight + c.InputPadding[3], field.Bounds.Height);
        var presenter = field.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal(c.InputFont.FontSize, presenter.FontSize);
        Assert.Equal(c.InputFont.LineHeight, presenter.LineHeight);
        Assert.Equal(new Point(c.InputPadding[0], c.InputPadding[1]), presenter.TranslatePoint(default, field));
        var scroller = field.GetVisualDescendants().OfType<ScrollViewer>().Single();
        Assert.Equal(field.Bounds.Size, scroller.Bounds.Size);
        Assert.Equal(InputLayoutTests.At(container, root).X, InputLayoutTests.At(field, root).X);

        // The root's width: 100%, or (any resize set) fit-content, the textarea's own: a browser's 20 columns.
        if (c.RootWidth == "100%")
        {
            Assert.Equal(HorizontalAlignment.Stretch, root.HorizontalAlignment);
            Assert.Equal(Scene.Width - 40, root.Bounds.Width);
        }
        else
        {
            Assert.Equal("fit-content", c.RootWidth);
            Assert.Equal(HorizontalAlignment.Left, root.HorizontalAlignment);
            var clearWidth = c.Clear.Padding[0] + c.Clear.IconSize + c.Clear.Padding[2];
            Assert.Equal(field.IntrinsicWidth, field.Bounds.Width);
            Assert.Equal(c.RootBorder[0] + field.IntrinsicWidth + clearWidth + c.RootBorder[2], root.Bounds.Width);
        }

        // The grip: shown for a resize that drags.
        var grip = field.GetVisualDescendants().OfType<TextareaResizer>().Single();
        Assert.Equal(c.InputResize != "none", grip.IsEffectivelyVisible);

        // The clear icon, after the text: its container padded, the icon at its top (alignItems flex-start, $alignTop).
        var clear = (Border)textarea.Part(BaseInput.ClearIconContainerPart)!;
        var icon = (InputIcon)textarea.Part(BaseInput.ClearIconPart)!;
        Assert.True(clear.IsVisible);
        Assert.Equal("flex-start", c.Clear.AlignItems);
        Assert.Equal(new Thickness(c.Clear.Padding[0], c.Clear.Padding[1], c.Clear.Padding[2], 0), clear.Padding);
        Assert.Equal(new Size(c.Clear.IconSize, c.Clear.IconSize), icon.Bounds.Size);
        Assert.Equal(c.Clear.Padding[0] + c.Clear.IconSize + c.Clear.Padding[2], clear.Bounds.Width);
        Assert.Equal(inner, clear.Bounds.Height);
        Assert.Equal(InputLayoutTests.At(field, root).Right, InputLayoutTests.At(clear, root).X);
        Assert.Equal(InputLayoutTests.At(clear, root).Y + c.Clear.Padding[1], InputLayoutTests.At(icon, root).Y);
        Assert.Equal(InputLayoutTests.At(container, root).Right, InputLayoutTests.At(clear, root).Right);
        Assert.Equal(InputColourTests.Cursor(c.Clear.Cursor), icon.Cursor?.ToString());
    }

    /// <summary>The font an element passes down to its text (TextElement's, and TextBlock's line height).</summary>
    private static void AssertFont(InputFont font, AvaloniaObject element)
    {
        Assert.Equal(font.FontSize, element.GetValue(TextElement.FontSizeProperty));
        Assert.Equal(Enum.Parse<FontWeight>(font.FontWeight), element.GetValue(TextElement.FontWeightProperty));
        Assert.Equal(font.LineHeight, element.GetValue(TextBlock.LineHeightProperty));
        Assert.Equal(FontFamily.Parse(font.Family), element.GetValue(TextElement.FontFamilyProperty));
    }
}
