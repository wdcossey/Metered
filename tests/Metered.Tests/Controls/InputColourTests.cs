using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// An input with both enhancers, clearable, a password, a value and a placeholder, in each state Base Web's style
/// functions draw (rest, focused by a real Tab, error, positive, both, focused in error or positive, disabled, disabled
/// in error, read-only, read-only and focused), light and dark: each part's colours, the caret's, the placeholder's and
/// the input's cursor against Base Web's own Input rendered (InputOracle); and the clear icon's and the toggle's focus
/// outline on a keyboard's focus.
/// </summary>
public class InputColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, InputOracle.Value.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Parts_match_Base_Web(int index)
    {
        var c = InputOracle.Value.Colours[index];
        var input = new Input
        {
            StartEnhancer = "@", EndEnhancer = ".com", Clearable = true, Type = InputType.Password, Text = "Value",
            PlaceholderText = "Placeholder",
            Error = c.State.Contains("rror"), Positive = c.State.Contains("ositive"),
            IsEnabled = !c.State.StartsWith("disabled"), IsReadOnly = c.State.StartsWith("readOnly"),
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { input } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        if (c.State.Contains("ocused"))
        {
            scene.Key(Key.Tab);
            Assert.True(input.IsFocused);
        }

        var root = (Border)input.Part(Input.RootPart)!;
        Assert.Equal(c.RootBorder, Colour(Base(root, Border.BorderBrushProperty)));
        Assert.Equal(c.RootBackground, Colour(root.Background));
        var start = (ContentPresenter)input.Part(Input.StartEnhancerPart)!;
        Assert.Equal(c.StartForeground, Colour(Base(start, ContentPresenter.ForegroundProperty)));
        Assert.Equal(c.StartBackground, Colour(Base(start, ContentPresenter.BackgroundProperty)));
        var end = (ContentPresenter)input.Part(Input.EndEnhancerPart)!;
        Assert.Equal(c.EndForeground, Colour(Base(end, ContentPresenter.ForegroundProperty)));
        Assert.Equal(c.EndBackground, Colour(Base(end, ContentPresenter.BackgroundProperty)));
        var container = (Border)input.Part(BaseInput.InputContainerPart)!;
        Assert.Equal(c.ContainerForeground, Colour(container.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.ContainerBackground, Colour(Base(container, Border.BackgroundProperty)));

        // The text, the caret and the placeholder: the Input part's, and what the text presenter and placeholder draw.
        var field = (InputField)input.Part(BaseInput.InputPart)!;
        Assert.Equal(c.InputForeground, Colour(field.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(c.Caret, Colour(field.CaretBrush));
        Assert.Equal(c.Placeholder, Colour(field.PlaceholderForeground));
        var presenter = field.GetVisualDescendants().OfType<TextPresenter>().Single();
        Assert.Equal(c.InputForeground, Colour(presenter.Foreground));
        Assert.Equal(c.Caret, Colour(presenter.CaretBrush));
        var placeholder = field.GetVisualDescendants().OfType<TextBlock>().Single(t => t.Name == "PART_Placeholder");
        Assert.Equal(c.Placeholder, Colour(placeholder.Foreground));
        Assert.Equal(Cursor(c.InputCursor), field.Cursor?.ToString());

        var clear = (Visual)input.Part(BaseInput.ClearIconContainerPart)!;
        if (c.ClearIconContainer is null)
        {
            Assert.False(clear.IsVisible);
        }
        else
        {
            Assert.True(clear.IsVisible);
            Assert.Equal(c.ClearIconContainer, Colour(clear.GetValue(TextElement.ForegroundProperty)));
            // The icon fills with currentColor.
            Assert.Equal(c.ClearIconContainer, Colour(((InputIcon)input.Part(BaseInput.ClearIconPart)!).Foreground));
        }

        var toggle = (InputMaskToggleButton)input.Part(BaseInput.MaskToggleButtonPart)!;
        Assert.Equal(c.MaskToggleButton, Colour(toggle.Foreground));
        Assert.Equal(c.MaskToggleButton, Colour(((InputIcon)input.Part(BaseInput.MaskToggleShowIconPart)!).Foreground));
    }

    public static TheoryData<int> OutlineCases => new(Enumerable.Range(0, InputOracle.Value.Outlines.Length));

    [AvaloniaTheory]
    [MemberData(nameof(OutlineCases))]
    public void The_clear_icon_and_the_toggle_have_Base_Webs_outline_on_keyboard_focus(int index)
    {
        var c = InputOracle.Value.Outlines[index];
        var input = new Input { Clearable = true, Type = InputType.Password, Text = "Value" };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { input } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);
        var part = (InputElement)input.Part(c.Part)!;
        if (c.FocusVisible)
        {
            // Tab from the text box: the clear icon, then the toggle, as their order in Base Web's markup.
            scene.Key(Key.Tab);
            Assert.True(input.IsFocused);
            scene.Key(Key.Tab);
            if (c.Part == BaseInput.MaskToggleButtonPart) scene.Key(Key.Tab);
            Assert.True(part.IsFocused);
        }
        else
        {
            // A pointer's focus isn't visible.
            part.Focus(NavigationMethod.Pointer);
            scene.Pump();
            Assert.True(part.IsFocused);
        }

        var (brush, thickness) = part switch
        {
            InputIcon icon => (icon.OutlineBrush, icon.OutlineThickness),
            InputMaskToggleButton button => (button.OutlineBrush, button.OutlineThickness),
            _ => throw new InvalidOperationException(c.Part),
        };
        if (c.Outline is { } outline)
        {
            Assert.Equal(outline.Colour, Colour(brush));
            Assert.Equal(outline.Thickness, thickness);
        }
        else
        {
            Assert.True(brush is null || thickness == 0, $"an outline of {thickness} in {Colour(brush)}");
        }
    }

    /// <summary>CSS's cursor as Avalonia's, written as its ToString.</summary>
    internal static string Cursor(string css) => new Cursor(css switch
    {
        "text" => StandardCursorType.Ibeam,
        "not-allowed" => StandardCursorType.No,
        "pointer" => StandardCursorType.Hand,
        _ => throw new ArgumentException(css),
    }).ToString()!;

    internal static string Colour(IBrush? brush) => RadioColourTests.Colour(brush);

    /// <summary>The value styles give, before any transition.</summary>
    private static T Base<T>(AvaloniaObject o, StyledProperty<T> property) => o.GetBaseValue(property).GetValueOrDefault()!;
}
