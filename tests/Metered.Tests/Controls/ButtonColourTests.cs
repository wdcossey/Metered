using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Controls;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Controls;

/// <summary>
/// Each kind in each state, light and dark, with and without backgroundSafe: the BaseButton part's
/// background, text colour and box-shadows against what Base Web's own style function gives
/// (Oracle.Button.Colours). Hover, press and keyboard focus are real input.
/// </summary>
public class ButtonColourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, Oracle.Button.Colours.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void BaseButton_matches_Base_Web(int index)
    {
        var c = Oracle.Button.Colours[index];
        var button = new Button
        {
            Content = "Button",
            Kind = Enum.Parse<ButtonKind>(c.Kind, ignoreCase: true),
            BackgroundSafe = c.BackgroundSafe,
            IsSelected = c.State.StartsWith("selected", StringComparison.Ordinal) || c.State == "disabledSelected",
            IsLoading = c.State.StartsWith("loading", StringComparison.Ordinal),
            IsEnabled = !c.State.StartsWith("disabled", StringComparison.Ordinal),
        };
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { button } },
            c.Theme == "Dark" ? ThemeVariant.Dark : ThemeVariant.Light);

        if (c.State.EndsWith("Hover", StringComparison.Ordinal) || c.State == "hover") scene.Hover(button);
        if (c.State == "active") scene.Press(button);
        if (c.State.EndsWith("ocusVisible", StringComparison.Ordinal))
        {
            button.Focus(NavigationMethod.Tab);
            scene.Pump();
        }

        var part = (Border)button.Part(Button.BaseButtonPart)!;
        Assert.Equal(c.Background, Colour(part.GetBaseValue(Border.BackgroundProperty).GetValueOrDefault()));
        if (c.Foreground is not null)
        {
            Assert.Equal(c.Foreground, Colour(part.GetBaseValue(TextElement.ForegroundProperty).GetValueOrDefault()));
        }

        Assert.Equal(BoxShadows.Parse(c.BoxShadow), part.GetBaseValue(Border.BoxShadowProperty).GetValueOrDefault());
    }

    internal static string Colour(IBrush? brush) =>
        brush is ISolidColorBrush solid ? "#" + solid.Color.ToUInt32().ToString("X8") : $"not a solid brush: {brush}";
}
