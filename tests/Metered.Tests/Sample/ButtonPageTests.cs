using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Sample;
using Metered.Sample.Pages;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The sample's Button page: first in the catalogue, every button on it Metered's and drawn by its theme, each of Base
/// Web's kinds and sizes shown, and "Click to load" showing the spinner.
/// </summary>
public class ButtonPageTests
{
    [AvaloniaFact]
    public void The_Button_Page_Is_First_In_The_Catalogue()
    {
        var entry = SampleShell.Pages[0];

        Assert.Equal(typeof(ButtonPage), entry.Type);
        Assert.Equal("Button", entry.Title);
    }

    // Every button but the scroll bar's own: Metered's, with its template's parts.
    [AvaloniaFact]
    public void Every_Button_On_The_Page_Is_Metereds_Drawn_By_Its_Theme()
    {
        var page = new ButtonPage();
        using var scene = new Scene(page);

        var buttons = page.GetVisualDescendants().OfType<Avalonia.Controls.Button>().Where(button => button is not RepeatButton).ToList();
        Assert.NotEmpty(buttons);
        Assert.All(buttons, button => Assert.NotNull(Assert.IsType<Button>(button).Part(Button.BaseButtonPart)));
    }

    // Base Web's KIND and BUTTON_GROUP_EXCLUSIVE_KINDS.outline, all seven; and its four sizes, under their newer names.
    [AvaloniaFact]
    public void The_Page_Shows_Every_Kind_And_Each_Size()
    {
        var page = new ButtonPage();
        using var scene = new Scene(page);
        var buttons = page.GetVisualDescendants().OfType<Button>().ToList();

        Assert.Equal(Enum.GetValues<ButtonKind>().ToHashSet(), buttons.Where(b => b.IsEnabled).Select(b => b.Kind).ToHashSet());
        Assert.Superset(new HashSet<ButtonSize> { ButtonSize.XSmall, ButtonSize.Small, ButtonSize.Medium, ButtonSize.Large },
            buttons.Select(b => b.Size).ToHashSet());
        Assert.Contains(buttons, b => !b.IsEnabled);
    }

    [AvaloniaFact]
    public void Click_To_Load_Shows_The_Spinner()
    {
        var page = new ButtonPage();
        using var scene = new Scene(page);
        var loading = page.FindControl<Button>("Loading")!;
        Assert.False(loading.IsLoading);

        scene.Click(loading);

        Assert.True(loading.IsLoading);
        Assert.True(loading.Part(Button.LoadingSpinnerPart) is Visual { IsEffectivelyVisible: true });
    }
}
