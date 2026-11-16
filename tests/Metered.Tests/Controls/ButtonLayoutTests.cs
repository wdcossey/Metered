using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.Media;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>
/// Each size, shape and width type: the BaseButton part's padding, radius, size and type against
/// Base Web's style function (Oracle.Button.Layout), and the laid-out height, which doesn't depend
/// on the font: padding plus the line height, or the shape's fixed size.
/// </summary>
public class ButtonLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, Oracle.Button.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void BaseButton_matches_Base_Web(int index)
    {
        var c = Oracle.Button.Layout[index];
        var button = new Button
        {
            Content = c.Shape is "circle" or "square" or "round" ? "+" : "Button",
            Size = Enum.Parse<ButtonSize>(c.Size, ignoreCase: true),
            Shape = Enum.Parse<ButtonShape>(c.Shape, ignoreCase: true),
            WidthType = Enum.Parse<ButtonWidthType>(c.WidthType, ignoreCase: true),
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } });
        var part = (Border)button.Part(Button.BaseButtonPart)!;

        Assert.Equal(new Thickness(c.Padding[0], c.Padding[1], c.Padding[2], c.Padding[3]), part.Padding);
        if (c.HalfRadius)
        {
            // 50%: a radius of at least half the side, which the drawing clamps to a circle.
            Assert.True(part.CornerRadius.TopLeft >= part.Bounds.Height / 2, $"radius {part.CornerRadius.TopLeft}");
        }
        else
        {
            Assert.Equal(new CornerRadius(c.Radius!.Value), part.CornerRadius);
        }

        Assert.Equal(c.Width ?? double.NaN, part.Width);
        Assert.Equal(c.Height ?? double.NaN, part.Height);
        Assert.Equal(c.MinWidth ?? 0, part.MinWidth);
        Assert.Equal(c.MinHeight ?? 0, part.MinHeight);
        Assert.Equal(c.FontSize, part.GetValue(TextElement.FontSizeProperty));
        Assert.Equal(Enum.Parse<FontWeight>(c.FontWeight), part.GetValue(TextElement.FontWeightProperty));
        Assert.Equal(c.LineHeight, part.GetValue(TextBlock.LineHeightProperty));

        // The laid-out height: Base Web's box-sizing border-box, with min-height for fill.
        var expectedHeight = c.Height ?? c.Padding[1] + c.LineHeight + c.Padding[3];
        if (c.MinHeight is { } min) expectedHeight = Math.Max(expectedHeight, min);
        Assert.Equal(expectedHeight, part.Bounds.Height, 3);
        if (c.Width is { } width) Assert.Equal(Math.Max(width, c.MinWidth ?? 0), part.Bounds.Width, 3);

        // display: inline-flex hugs; width 100% fills, unless the shape fixes the width.
        var fills = c.WidthType == "fill" && c.Width is null;
        Assert.Equal(fills ? 390 - 40 : part.Bounds.Width, button.Bounds.Width, 3);
    }

    [AvaloniaTheory]
    [InlineData(ButtonSize.Mini, ButtonSize.XSmall)]
    [InlineData(ButtonSize.Compact, ButtonSize.Small)]
    [InlineData(ButtonSize.Default, ButtonSize.Medium)]
    public void Older_size_names_are_the_newer(ButtonSize older, ButtonSize newer)
    {
        var a = new Button { Content = "A", Size = older };
        var b = new Button { Content = "A", Size = newer };
        using var scene = new Scene(new StackPanel { Children = { a, b } });
        Assert.Equal(b.Bounds.Size, a.Bounds.Size);
        Assert.Equal(((Border)b.Part("BaseButton")!).Padding, ((Border)a.Part("BaseButton")!).Padding);
        Assert.Equal(b.ArtworkSize, a.ArtworkSize);
    }

    [AvaloniaTheory]
    [InlineData(ButtonShape.Default, ButtonShape.Rectangular)]
    [InlineData(ButtonShape.Rounded, ButtonShape.Pill)]
    public void Older_shape_names_are_the_newer(ButtonShape older, ButtonShape newer)
    {
        var a = new Button { Content = "A", Shape = older };
        var b = new Button { Content = "A", Shape = newer };
        using var scene = new Scene(new StackPanel { Children = { a, b } });
        Assert.Equal(((Border)b.Part("BaseButton")!).CornerRadius, ((Border)a.Part("BaseButton")!).CornerRadius);
    }

    [AvaloniaFact]
    public void Hugs_its_content_by_default()
    {
        var button = new Button { Content = "Button" };
        using var scene = new Scene(new StackPanel { Children = { button } });
        Assert.Equal(HorizontalAlignment.Left, button.HorizontalAlignment);
        Assert.True(button.Bounds.Width < 200);
    }
}
