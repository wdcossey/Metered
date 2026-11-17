using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Radio page: its place in the catalogue (its issue's number, 35, the last control before the theme),
/// Metered's radios in every group, the first group's selection said under it, by a tap and by the arrow keys, and the
/// horizontal groups within a phone's width.
/// </summary>
public class RadioPageTests
{
    [AvaloniaFact]
    public void The_Radio_Page_Takes_Its_Issues_Number_Before_The_Theme()
    {
        var titles = SampleShell.Pages.Select(page => page.Title).ToList();
        var entry = Assert.Single(SampleShell.Pages, page => page.Type == typeof(RadioPage));

        Assert.Equal("Radio", entry.Title);
        Assert.Equal(35, typeof(RadioPage).GetCustomAttributes(typeof(SamplePageAttribute), false).Cast<SamplePageAttribute>().Single().Order);
        Assert.Equal(titles.IndexOf("Theme") - 1, titles.IndexOf(entry.Title));
    }

    [AvaloniaFact]
    public void Every_Group_On_The_Page_Holds_Metereds_Radios()
    {
        var page = new RadioPage();
        using var scene = new Scene(page);

        var groups = page.GetVisualDescendants().OfType<RadioGroup>().ToList();
        Assert.Equal(5, groups.Count);
        Assert.All(groups, group => Assert.All(group.GetVisualDescendants().OfType<Radio>(),
            radio => Assert.NotNull(radio.Part(Radio.RadioMarkOuterPart))));
        Assert.NotNull(page.FindControl<Radio>("Alone")!.Part(Radio.RadioMarkOuterPart));
    }

    [AvaloniaFact]
    public void The_First_Groups_Selection_Is_Said_Under_It()
    {
        var page = new RadioPage();
        using var scene = new Scene(page);
        var basic = page.FindControl<RadioGroup>("Basic")!;
        var selected = page.FindControl<Avalonia.Controls.TextBlock>("Selected")!;
        var radios = basic.GetVisualDescendants().OfType<Radio>().ToList();
        Assert.Equal("Selected: 2", selected.Text);
        Assert.Equal([false, true, false], radios.Select(r => r.IsChecked == true));

        scene.Click(radios[2]);
        Assert.Equal("Selected: 3", selected.Text);

        scene.Key(Key.Down);
        Assert.Equal("Selected: 1", selected.Text);
    }

    [AvaloniaFact]
    public void The_Horizontal_Groups_Fit_A_Phones_Width()
    {
        var page = new RadioPage();
        using var scene = new Scene(page);
        foreach (var name in new[] { "Horizontal", "Top" })
        {
            var radios = page.FindControl<RadioGroup>(name)!.GetVisualDescendants().OfType<Radio>().ToList();
            Assert.Single(radios.Select(r => r.TranslatePoint(default, scene.Window)!.Value.Y + r.Bounds.Height).Distinct());
            Assert.All(radios, r => Assert.True(r.TranslatePoint(new Point(r.Bounds.Width, 0), scene.Window)!.Value.X <= Scene.Width));
        }
    }
}
