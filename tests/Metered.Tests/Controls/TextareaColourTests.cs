using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// A clearable textarea with a value and a placeholder, in each state Base Web's style functions draw (rest, focused by
/// a real Tab, error, positive, both, focused in error or positive, disabled, disabled in error, read-only, read-only and
/// focused), light and dark: each part's colours, the caret's, the placeholder's and the textarea's cursor against Base
/// Web's own Textarea rendered (TextareaOracle); and the clear icon's focus outline on a keyboard's focus.
/// </summary>
public class TextareaColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, TextareaOracle.Value.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = TextareaOracle.Value.Colours[index];
        var textarea = new Textarea
        {
            Clearable = true, Text = "Value", PlaceholderText = "Placeholder",
            Error = c.State.Contains("rror"), Positive = c.State.Contains("ositive"),
            IsEnabled = !c.State.StartsWith("disabled"), IsReadOnly = c.State.StartsWith("readOnly"),
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { textarea } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        if (c.State.Contains("ocused"))
        {
            scene.Key(Key.Tab);
            Assert.True(textarea.IsFocused);
        }

        var root = (Border)textarea.Part(Textarea.RootPart)!;
        Assert.Equal(c.RootBorder, InputColourTests.Colour(Base(root, Border.BorderBrushProperty)));
        Assert.Equal(c.RootBackground, InputColourTests.Colour(root.Background));
        var container = (Border)textarea.Part(BaseInput.InputContainerPart)!;
        Assert.Equal(c.ContainerForeground, InputColourTests.Colour(container.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.ContainerBackground, InputColourTests.Colour(Base(container, Border.BackgroundProperty)));

        // The text, the caret and the placeholder: the Input part's, and what the text presenter and placeholder draw.
        var field = (TextareaField)textarea.Part(BaseInput.InputPart)!;
        Assert.Equal(c.InputForeground, InputColourTests.Colour(field.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.Caret, InputColourTests.Colour(field.CaretBrush));
        Assert.Equal(c.Placeholder, InputColourTests.Colour(field.PlaceholderForeground));
        var presenter = field.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal(c.InputForeground, InputColourTests.Colour(presenter.Foreground));
        Assert.Equal(c.Caret, InputColourTests.Colour(presenter.CaretBrush));
        var placeholder = field.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Placeholder");
        Assert.Equal(c.Placeholder, InputColourTests.Colour(placeholder.Foreground));
        Assert.Equal(InputColourTests.Cursor(c.InputCursor), field.Cursor?.ToString());

        var clear = (Visual)textarea.Part(BaseInput.ClearIconContainerPart)!;
        if (c.ClearIconContainer is null)
        {
            Assert.False(clear.IsVisible);
        }
        else
        {
            Assert.True(clear.IsVisible);
            Assert.Equal(c.ClearIconContainer, InputColourTests.Colour(clear.GetValue(TextElement.ForegroundProperty)));
            // The icon fills with currentColor.
            Assert.Equal(c.ClearIconContainer, InputColourTests.Colour(((InputIcon)textarea.Part(BaseInput.ClearIconPart)!).Foreground));
        }
    }

    public static TheoryData<int> OutlineCases => new(Enumerable.Range(0, TextareaOracle.Value.Outlines.Length));

    [AvaloniaTheory]
    [MemberData(nameof(OutlineCases))]
    public void The_clear_icon_has_Base_Webs_outline_on_keyboard_focus(int index)
    {
        var c = TextareaOracle.Value.Outlines[index];
        var textarea = new Textarea { Clearable = true, Text = "Value" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { textarea } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var icon = (InputIcon)textarea.Part(BaseInput.ClearIconPart)!;
        if (c.FocusVisible)
        {
            // Tab from the textarea: the clear icon, its next in Base Web's markup.
            scene.Key(Key.Tab);
            Assert.True(textarea.IsFocused);
            scene.Key(Key.Tab);
            Assert.True(icon.IsFocused);
        }
        else
        {
            // A pointer's focus isn't visible.
            icon.Focus(NavigationMethod.Pointer);
            scene.Pump();
            Assert.True(icon.IsFocused);
        }

        if (c.Outline is { } outline)
        {
            Assert.Equal(outline.Colour, InputColourTests.Colour(icon.OutlineBrush));
            Assert.Equal(outline.Thickness, icon.OutlineThickness);
        }
        else
        {
            Assert.True(icon.OutlineBrush is null || icon.OutlineThickness == 0, $"an outline of {icon.OutlineThickness}");
        }
    }

    /// <summary>The value styles give, before any transition.</summary>
    private static T Base<T>(AvaloniaObject o, StyledProperty<T> property) => o.GetBaseValue(property).GetValueOrDefault()!;
}
