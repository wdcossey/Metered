using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Each colour of the select's parts, light and dark, in each of the control's states, against Base Web's own style
/// functions evaluated with the props select-component.js gave them (SelectOracle's colours); the tag's, the loading
/// ring's and an option's label (its surfaces). The states are reached as a user reaches them: focus, Escape after
/// opening, error and positive, disabled.
/// </summary>
public class SelectColourTests
{
    public static TheoryData<string, string> Cases => new(SelectOracle.Data.Colours.Select(c => (c.Theme, c.State)));

    internal static ThemeVariant Variant(string theme) => theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light;

    private static Color C(string hex) => Color.Parse(hex);

    private static Color Of(IBrush? brush) => Assert.IsAssignableFrom<ISolidColorBrush>(brush).Color;

    /// <summary>The cursor a CSS cursor is: Avalonia's standard one.</summary>
    internal static StandardCursorType Cursor(string css) => css switch
    {
        "pointer" => StandardCursorType.Hand,
        "not-allowed" => StandardCursorType.No,
        "text" => StandardCursorType.Ibeam,
        "default" => StandardCursorType.Arrow,
        "wait" => StandardCursorType.Wait,
        _ => throw new InvalidOperationException(css),
    };

    internal static void AssertCursor(string css, InputElement element) =>
        Assert.Equal(new Cursor(Cursor(css)).ToString(), element.Cursor?.ToString());

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void The_controls_colours_are_Base_Webs(string theme, string state)
    {
        var c = SelectOracle.Data.Colours.Single(x => x.Theme == theme && x.State == state);
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Value = SelectOracle.Value(options, ["Aqua"]), IsLoading = true };
        var empty = new Select { Options = options };
        var search = new Select { Options = options, Type = SelectType.Search };
        foreach (var s in new[] { select, empty, search })
        {
            s.Error = state.Contains("rror", StringComparison.Ordinal);
            s.Positive = state == "positive";
            s.IsEnabled = !state.StartsWith("disabled", StringComparison.Ordinal);
        }

        using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Spacing = 300, Children = { select, empty, search } }, Variant(theme));
        if (state is "focused" or "errorFocused")
        {
            select.FocusTarget!.Focus(NavigationMethod.Tab);
            scene.Pump();
            Assert.True(select.IsSelectFocused);
        }
        else if (state == "pseudoFocused")
        {
            // Opened, then Escape: closed, pseudo-focused.
            SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
            SelectKit.Key(scene, Key.Escape);
            Assert.True(select.IsPseudoFocused);
            Assert.False(select.IsOpen);
        }

        // The border's and the fill's transition run out first.
        PopoverTests.Settle(scene, 350);
        var control = select.P<Border>(Select.ControlContainerPart);
        Assert.Equal(C(c.Border), Of(control.BorderBrush));
        Assert.Equal(C(c.Background), Of(control.Background));
        Assert.Equal(C(c.Foreground), Of(control.GetValue(TextElement.ForegroundProperty)));
        SelectColourTests.AssertCursor(c.Cursor, control);
        // The single value and the clear icon take the control's colour (currentColor).
        Assert.Equal(C(c.Foreground), Of(select.P<ContentPresenter>(Select.SingleValuePart).GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(C(c.Placeholder), Of(empty.P<TextBlock>(Select.PlaceholderPart).Foreground));
        Assert.Equal(C(c.Input), Of(select.P<TextBox>(Select.InputPart).Foreground));
        Assert.Equal(C(c.Arrow), Of(select.P<InputIcon>(Select.SelectArrowPart).Foreground));
        Assert.Equal(C(c.Search), Of(search.P<Border>(Select.SearchIconContainerPart).GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(C(c.Search), Of(search.P<InputIcon>(Select.SearchIconPart).Foreground));
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void A_tag_is_Base_Webs_gray_primary_tag(string theme)
    {
        var s = SelectOracle.Data.Surfaces.Single(x => x.Theme == theme);
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Multi = true, Value = SelectOracle.Value(options, ["Aqua", "Azure"]) };
        var disabled = new Select { Options = options, Multi = true, Value = SelectOracle.Value(SelectOracle.Options("OPTIONS"), ["Aqua"]), IsEnabled = false };
        using var scene = SelectKit.Show(new StackPanel { Margin = new Thickness(20), Spacing = 20, Children = { select, disabled } }, Variant(theme));
        var tag = select.Tags[0];
        var root = (RadioMark)tag.Part(SelectTag.RootPart)!;
        Assert.Equal(C(s.Tag.Background), Of(root.Background));
        Assert.Equal(C(s.Tag.Border!), Of(root.BorderBrush));
        Assert.Equal(new Thickness(s.Tag.BorderWidth), root.BorderThickness);
        Assert.Equal(C(s.Tag.Foreground), Of(root.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(C(s.Tag.Foreground), Of(((TextBlock)tag.Part(SelectTag.TextPart)!).Foreground));
        AssertCursor(s.Tag.Cursor, root);
        AssertCursor(s.Tag.ActionCursor, (InputElement)tag.Part(SelectTag.ActionPart)!);
        Assert.Equal(Colors.Transparent, Of(root.Overlay));
        Assert.Null(root.OutlineBrush);

        // A mouse over it: the hover overlay; pressed: the press's.
        scene.Hover(root);
        Assert.Equal(C(s.Tag.Hover!), Of(root.Overlay));
        scene.Press(root);
        Assert.Equal(C(s.Tag.Active!), Of(root.Overlay));
        scene.Window.MouseUp(scene.CentreOf(root), MouseButton.Left);
        scene.Window.MouseMove(new Point(1, 1));
        scene.Pump();
        Assert.Equal(Colors.Transparent, Of(root.Overlay));

        // Focused by the keys: its ring (the press focused it by the pointer: no ring).
        Assert.Null(root.OutlineBrush);
        select.FocusTarget!.Focus(NavigationMethod.Tab);
        tag.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.Equal(C(s.Tag.FocusRing!), Of(root.OutlineBrush));
        Assert.Equal(3, root.OutlineThickness);

        // Disabled: its colours, no border, no overlay under a mouse.
        var off = disabled.Tags[0];
        var offRoot = (RadioMark)off.Part(SelectTag.RootPart)!;
        Assert.Equal(C(s.TagDisabled.Background), Of(offRoot.Background));
        Assert.Equal(C(s.TagDisabled.Foreground), Of(offRoot.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(new Thickness(s.TagDisabled.BorderWidth), offRoot.BorderThickness);
        Assert.Null(s.TagDisabled.Hover);
        scene.Hover(offRoot);
        Assert.Equal(Colors.Transparent, Of(offRoot.Overlay));
        AssertCursor(s.TagDisabled.Cursor, offRoot);
        Assert.False(off.Focusable && off.IsEffectivelyEnabled);
    }

    [AvaloniaTheory]
    [InlineData("Light")]
    [InlineData("Dark")]
    public void The_loading_ring_and_an_options_label_are_Base_Webs(string theme)
    {
        var s = SelectOracle.Data.Surfaces.Single(x => x.Theme == theme);
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Value = SelectOracle.Value(options, ["Aqua"]), IsLoading = true };
        var (scene, _) = SelectKit.Page(select, Variant(theme));
        using var _s = scene;
        var ring = select.P<SpinnerRing>(Select.LoadingIndicatorPart);
        Assert.Equal(C(s.Ring.Top), Of(ring.Foreground));
        Assert.Equal(C(s.Ring.Track), Of(ring.Background));

        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        PopoverTests.Settle(scene, 350);
        var aqua = (SelectOptionContent)SelectKit.Option(select, "Aqua").Part(MenuOptionList.ListItemPart)!.As<ContentPresenter>().Content!;
        var azure = (SelectOptionContent)SelectKit.Option(select, "Azure").Part(MenuOptionList.ListItemPart)!.As<ContentPresenter>().Content!;
        // The value highlighted at first: bold, the option's own colour (none of its own).
        Assert.True(aqua.IsHighlighted && aqua.Selected);
        Assert.Null(s.Option.SelectedHighlighted);
        Assert.Equal(Enum.Parse<FontWeight>(s.Option.SelectedWeight), aqua.GetValue(TextElement.FontWeightProperty));
        Assert.Equal(C(s.Option.ListItemHighlighted), Of(aqua.GetValue(TextElement.ForegroundProperty)));
        Assert.Equal(Enum.Parse<FontWeight>(s.Option.PlainWeight), azure.GetValue(TextElement.FontWeightProperty));
        Assert.Null(s.Option.Plain);
        Assert.Equal(C(s.Option.ListItem), Of(azure.GetValue(TextElement.ForegroundProperty)));
        AssertCursor(s.Option.Cursor, aqua);
        AssertCursor(s.Option.DisabledCursor, (SelectOptionContent)SelectKit.Option(select, "Beige").Part(MenuOptionList.ListItemPart)!.As<ContentPresenter>().Content!);

        // Moved off it: selected, not highlighted, menuFontSelected.
        SelectKit.Key(scene, Key.Down);
        PopoverTests.Settle(scene, 350);
        Assert.False(aqua.IsHighlighted);
        Assert.Equal(C(s.Option.Selected!), Of(aqua.GetValue(TextElement.ForegroundProperty)));
        Assert.True(((SelectOptionContent)SelectKit.Option(select, "Aquamarine").Part(MenuOptionList.ListItemPart)!.As<ContentPresenter>().Content!).IsHighlighted);
    }
}

internal static class AsExtension
{
    public static T As<T>(this object o) where T : class => (T)o;
}
