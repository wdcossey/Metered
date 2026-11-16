using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's ButtonGroup page: after Button in the catalogue, the radio group starting on its first button, each
/// click in the checkbox group said under it, and the wrapping group wrapping at a phone's width.
/// </summary>
public class ButtonGroupPageTests
{
    [AvaloniaFact]
    public void The_ButtonGroup_Page_Comes_After_The_Button_Page()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(ButtonGroupPage));

        Assert.Equal("ButtonGroup", entry.Title);
        Assert.Equal(titles.IndexOf("Button") + 1, titles.IndexOf(entry.Title));
    }

    [AvaloniaFact]
    public void Every_Group_On_The_Page_Holds_Metereds_Buttons()
    {
        var page = new ButtonGroupPage();
        using var scene = new Scene(page);

        var groups = page.GetVisualDescendants().OfType<ButtonGroup>().ToList();
        Assert.Equal(3, groups.Count);
        Assert.All(groups, group => Assert.All(Buttons(group), button => Assert.NotNull(button.Part(Button.BaseButtonPart))));
    }

    [AvaloniaFact]
    public void The_Radio_Group_Starts_On_Its_First_Button()
    {
        var page = new ButtonGroupPage();
        using var scene = new Scene(page);
        var radio = page.FindControl<ButtonGroup>("Radio")!;

        Assert.Equal([0], radio.Selected!);
        Assert.Equal([true, false, false], Buttons(radio).Select(button => button.IsSelected));
    }

    [AvaloniaFact]
    public void A_Click_In_The_Checkbox_Group_Is_Said_Under_It()
    {
        var page = new ButtonGroupPage();
        using var scene = new Scene(page);
        var checks = page.FindControl<ButtonGroup>("Checks")!;
        var clicked = page.FindControl<Avalonia.Controls.TextBlock>("Clicked")!;
        Assert.Equal("Clicked: none", clicked.Text);

        scene.Click(Buttons(checks)[1]);
        Assert.Equal("Clicked: 1; selected: 1", clicked.Text);

        scene.Click(Buttons(checks)[3]);
        Assert.Equal("Clicked: 3; selected: 1, 3", clicked.Text);
    }

    // Six buttons are wider than a phone: the group wraps rather than run off the page.
    [AvaloniaFact]
    public void The_Wrapping_Group_Wraps_At_A_Phones_Width()
    {
        var page = new ButtonGroupPage();
        using var scene = new Scene(page);
        var wrapping = page.FindControl<ButtonGroup>("Wrapping")!;

        var rows = Buttons(wrapping).Select(button => button.TranslatePoint(default, scene.Window)!.Value.Y).Distinct().Count();
        Assert.True(rows > 1, $"{rows} row");
        Assert.True(wrapping.Bounds.Width <= Scene.Width);
    }

    private static List<Button> Buttons(ButtonGroup group) => group.GetVisualDescendants().OfType<Button>().ToList();
}
