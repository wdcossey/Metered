using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Checkbox page: after the pilot's pages and before the theme's, every checkbox on it Metered's, the
/// parent indeterminate while only some of its children are checked and checking or clearing them all, and its toggles.
/// </summary>
public class CheckboxPageTests
{
    [AvaloniaFact]
    public void The_Checkbox_Page_Comes_After_ButtonGroup_And_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(CheckboxPage));

        Assert.Equal("Checkbox", entry.Title);
        Assert.True(titles.IndexOf("ButtonGroup") < titles.IndexOf(entry.Title));
        Assert.True(titles.IndexOf(entry.Title) < titles.IndexOf("Theme"));
    }

    [AvaloniaFact]
    public void Every_Checkbox_On_The_Page_Has_Its_Parts()
    {
        var page = new CheckboxPage();
        using var scene = new Scene(page);

        var boxes = page.GetVisualDescendants().OfType<Checkbox>().ToList();
        Assert.True(boxes.Count >= 10, $"{boxes.Count}");
        Assert.All(boxes, box => Assert.NotNull(box.Part(Checkbox.RootPart)));
        Assert.Equal(4, boxes.Count(box => box.CheckmarkType == CheckboxStyleType.Toggle));
        Assert.Equal(
            [LabelPlacement.Top, LabelPlacement.Left, LabelPlacement.Right, LabelPlacement.Bottom],
            boxes.Where(box => box.LabelPlacement is not null).Select(box => box.ActualLabelPlacement));
    }

    [AvaloniaFact]
    public void The_Parent_Is_Indeterminate_Until_Its_Children_Agree()
    {
        var page = new CheckboxPage();
        using var scene = new Scene(page);
        var parent = page.FindControl<Checkbox>("AllToppings")!;
        var cheese = page.FindControl<Checkbox>("Cheese")!;
        var olives = page.FindControl<Checkbox>("Olives")!;
        Assert.True(parent.IsIndeterminate);
        Assert.False(parent.IsChecked);

        scene.Click(olives);
        Assert.True(parent.IsChecked);
        Assert.False(parent.IsIndeterminate);

        scene.Click(parent);
        Assert.False(cheese.IsChecked);
        Assert.False(olives.IsChecked);
        Assert.False(parent.IsIndeterminate);

        scene.Click(cheese);
        Assert.True(parent.IsIndeterminate);
        scene.Click(parent);
        Assert.True(cheese.IsChecked);
        Assert.True(olives.IsChecked);
        Assert.True(parent.IsChecked);
    }

    [AvaloniaFact]
    public void The_Toggle_Toggles()
    {
        var page = new CheckboxPage();
        using var scene = new Scene(page);
        var toggle = page.FindControl<Checkbox>("Toggle")!;

        scene.Click(toggle);
        Assert.True(toggle.IsChecked);
    }
}
