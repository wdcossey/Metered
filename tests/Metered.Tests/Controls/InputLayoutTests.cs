using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// An input with a value at each size, with no enhancer, a start one, an end one and both, and with no trailing icon,
/// the clear icon or a password's toggle: adjoined and the trailing flag, each part's lengths and font, the icons'
/// sizes, and where layout puts them, against Base Web's own Input rendered (InputOracle), laid out as a browser lays
/// out its flex rows.
/// </summary>
public class InputLayoutTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, InputOracle.Value.Layout.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_are_sized_and_placed_as_Base_Webs(int index)
    {
        var c = InputOracle.Value.Layout[index];
        var input = new Input
        {
            Size = Enum.Parse<InputSize>(c.Size, ignoreCase: true),
            Text = "Value",
            StartEnhancer = c.Enhancers switch { "start" => "@", "both" => "$", _ => null },
            EndEnhancer = c.Enhancers switch { "end" => ".com", "both" => ".00", _ => null },
            Clearable = c.Trailing == "clearable",
            Type = c.Trailing == "password" ? InputType.Password : InputType.Text,
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { input } });

        Assert.Equal(Enum.Parse<InputAdjoined>(c.Adjoined, ignoreCase: true), input.Adjoined);
        Assert.Equal(c.HasIconTrailing, input.Classes.Contains(":has-icon-trailing"));

        // Root: its border, radius, padding and font; the box Base Web lays out, its height its tallest item's.
        var root = (Border)input.Part(Input.RootPart)!;
        Assert.Equal(new Thickness(c.RootBorder[0], c.RootBorder[1], c.RootBorder[2], c.RootBorder[3]), root.BorderThickness);
        Assert.Equal(new CornerRadius(c.RootRadius), root.CornerRadius);
        Assert.Equal(new Thickness(c.RootPadding[0], 0, c.RootPadding[1], 0), root.Padding);
        AssertFont(c.RootFont, root);
        Assert.Equal(c.Height, input.Bounds.Height);
        Assert.Equal(c.Height, root.Bounds.Height);
        Assert.Equal(Scene.Width - 40, root.Bounds.Width);

        // The enhancers, at the root's start and end inside its padding, and the container between them.
        var container = (Border)input.Part(BaseInput.InputContainerPart)!;
        AssertFont(c.ContainerFont, container);
        var inner = root.Bounds.Height - c.RootBorder[1] - c.RootBorder[3];
        var left = c.RootBorder[0] + c.RootPadding[0];
        var right = root.Bounds.Width - c.RootBorder[2] - c.RootPadding[1];
        var start = (ContentPresenter)input.Part(Input.StartEnhancerPart)!;
        var end = (ContentPresenter)input.Part(Input.EndEnhancerPart)!;
        AssertEnhancer(c.StartEnhancer, start, root);
        AssertEnhancer(c.EndEnhancer, end, root);
        var from = c.StartEnhancer is null ? left : At(start, root).Right;
        var to = c.EndEnhancer is null ? right : At(end, root).X;
        if (c.StartEnhancer is not null) Assert.Equal(left, At(start, root).X);
        if (c.EndEnhancer is not null) Assert.Equal(right, At(end, root).Right);
        Assert.Equal(new Rect(from, c.RootBorder[1], to - from, inner), At(container, root));

        // The text: its padding and font, the text presenter's too.
        var field = (InputField)input.Part(BaseInput.InputPart)!;
        Assert.Equal(new Thickness(c.InputPadding[0], c.InputPadding[1], c.InputPadding[2], c.InputPadding[3]), field.Padding);
        AssertFont(c.InputFont, field);
        var presenter = field.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal(c.InputFont.FontSize, presenter.FontSize);
        Assert.Equal(c.InputFont.LineHeight, presenter.LineHeight);
        Assert.Equal(c.InputFont.LineHeight, presenter.Bounds.Height);
        Assert.Equal(c.InputPadding[1] + c.InputFont.LineHeight + c.InputPadding[3], field.Bounds.Height);
        Assert.Equal(At(container, root).X, At(field, root).X);

        // The clear icon, after the text: its container padded, the icon centred in its height.
        var clear = (Border)input.Part(BaseInput.ClearIconContainerPart)!;
        var clearIcon = (InputIcon)input.Part(BaseInput.ClearIconPart)!;
        Assert.Equal(c.Clear is not null, clear.IsVisible);
        if (c.Clear is { } x)
        {
            Assert.Equal("center", x.AlignItems);
            Assert.Equal(new Thickness(x.Padding[0], x.Padding[1], x.Padding[2], 0), clear.Padding);
            Assert.Equal(new Size(x.IconSize, x.IconSize), clearIcon.Bounds.Size);
            Assert.Equal(x.Padding[0] + x.IconSize + x.Padding[2], clear.Bounds.Width);
            Assert.Equal(At(field, root).Right, At(clear, root).X);
            Assert.Equal(At(clear, root).Center.Y, At(clearIcon, root).Center.Y, 3);
            Assert.Equal(InputColourTests.Cursor(x.Cursor), clearIcon.Cursor?.ToString());
        }

        // The toggle, after the clear icon: padded, its icon centred in its height; the hide icon once it is clicked.
        var toggle = (InputMaskToggleButton)input.Part(BaseInput.MaskToggleButtonPart)!;
        Assert.Equal(c.Toggle is not null, toggle.IsVisible);
        if (c.Toggle is { } t)
        {
            Assert.Equal("center", t.AlignItems);
            Assert.Equal(new Thickness(t.Padding[0], 0, t.Padding[1], 0), toggle.Padding);
            Assert.Equal(inner, toggle.Bounds.Height);
            var show = (InputIcon)input.Part(BaseInput.MaskToggleShowIconPart)!;
            Assert.Equal(new Size(t.ShowIconSize, t.ShowIconSize), show.Bounds.Size);
            Assert.Equal(t.Padding[0] + t.ShowIconSize + t.Padding[1], toggle.Bounds.Width);
            Assert.Equal(At(toggle, root).Center.Y, At(show, root).Center.Y, 3);
            Assert.Equal(At(field, root).Right, At(toggle, root).X);
            Assert.Equal(At(container, root).Right, At(toggle, root).Right);

            scene.Click(toggle);
            var hide = (InputIcon)input.Part(BaseInput.MaskToggleHideIconPart)!;
            Assert.True(hide.IsVisible);
            Assert.Equal(new Size(t.HideIconSize, t.HideIconSize), hide.Bounds.Size);
        }
        else if (c.Clear is null)
        {
            Assert.Equal(At(container, root).Right, At(field, root).Right);
        }
    }

    private static void AssertEnhancer(InputEnhancerFacts? facts, ContentPresenter part, Visual root)
    {
        Assert.Equal(facts is not null, part.IsVisible);
        if (facts is null) return;
        Assert.Equal(new Thickness(facts.Padding[0], 0, facts.Padding[1], 0), part.Padding);
        Assert.Equal(facts.Font.FontSize, part.FontSize);
        Assert.Equal(Enum.Parse<FontWeight>(facts.Font.FontWeight), part.FontWeight);
        Assert.Equal(facts.Font.LineHeight, part.LineHeight);
        Assert.Equal(FontFamily.Parse(facts.Font.Family), part.FontFamily);
        // Stretched to the root's height inside its border, its text centred.
        var text = part.GetVisualDescendants().OfType<TextBlock>().Single();
        Assert.Equal(At(part, root).Center.Y, At(text, root).Center.Y, 3);
        Assert.Equal(At(part, root).Center.X, At(text, root).Center.X, 3);
    }

    /// <summary>The font an element passes down to its text (TextElement's, and TextBlock's line height).</summary>
    private static void AssertFont(InputFont font, AvaloniaObject element)
    {
        Assert.Equal(font.FontSize, element.GetValue(TextElement.FontSizeProperty));
        Assert.Equal(Enum.Parse<FontWeight>(font.FontWeight), element.GetValue(TextElement.FontWeightProperty));
        Assert.Equal(font.LineHeight, element.GetValue(TextBlock.LineHeightProperty));
        Assert.Equal(FontFamily.Parse(font.Family), element.GetValue(TextElement.FontFamilyProperty));
    }

    /// <summary>A part's box in the root's coordinates.</summary>
    internal static Rect At(Visual part, Visual root) =>
        new(part.TranslatePoint(default, root)!.Value, part.Bounds.Size);
}
