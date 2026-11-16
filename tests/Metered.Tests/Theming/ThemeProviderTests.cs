using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Themes;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Theming;

/// <summary>
/// Base Web's ThemeProvider: a Button in a subtree given a built theme draws that theme's colours, from the
/// subtree's own dictionary, while a Button outside keeps the app's; and the app's light and dark still switch.
/// Every expected colour is Base Web's (Oracle.Themes).
/// </summary>
public class ThemeProviderTests
{
    private static Theme Built(string name) => ThemeBuilderTests.Build(Oracle.Themes[name]);

    private static string Expected(string name, string token) => Oracle.Themes[name].Colours[token]!;

    private static Border Part(Button button) => (Border)button.Part(Button.BaseButtonPart)!;

    private static string Fill(Button button) =>
        Metered.Tests.Controls.ButtonColourTests.Colour(Part(button).GetBaseValue(Border.BackgroundProperty).GetValueOrDefault());

    private static string Text(Button button) =>
        Metered.Tests.Controls.ButtonColourTests.Colour(Part(button).GetBaseValue(TextElement.ForegroundProperty).GetValueOrDefault());

    private static (Button Outside, Button Inside, Border Subtree, Scene Scene) Open(Action<Border> provide, ThemeVariant? variant = null)
    {
        var outside = new Button { Content = "Outside" };
        var inside = new Button { Content = "Inside" };
        var subtree = new Border { Child = inside };
        provide(subtree);
        var scene = new Scene(new StackPanel { Children = { outside, subtree } }, variant);
        return (outside, inside, subtree, scene);
    }

    [AvaloniaFact]
    public void A_subtree_draws_its_theme_and_the_rest_the_apps()
    {
        var (outside, inside, _, scene) = Open(s => ThemeProvider.SetTheme(s, Built("lightFoundation")));
        using (scene)
        {
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryText"), Text(inside));
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(outside));
            Assert.Equal(Expected("light", "colors.buttonPrimaryText"), Text(outside));
            // The foundation and what derives from it differ here: a whole dictionary, not the foundation only.
            Assert.NotEqual(Expected("light", "colors.buttonPrimaryFill"), Expected("lightFoundation", "colors.buttonPrimaryFill"));
        }
    }

    [AvaloniaFact]
    public void A_derived_box_shadow_reaches_a_part_in_the_subtree()
    {
        var outside = new Button { Content = "Outside", IsSelected = true };
        var inside = new Button { Content = "Inside", IsSelected = true };
        var subtree = new Border { Child = inside };
        ThemeProvider.SetTheme(subtree, Built("lightOverrides"));
        using var scene = new Scene(new StackPanel { Children = { outside, subtree } });

        // sizing.scale0 and buttonOuterBorder overridden: Base Web's selected primary ring follows both.
        Assert.Equal(BoxShadows.Parse(Oracle.Themes["lightOverrides"].BoxShadows["button.boxShadow.primary.selected"]),
            Part(inside).GetBaseValue(Border.BoxShadowProperty).GetValueOrDefault());
        Assert.Equal(BoxShadows.Parse(Oracle.Themes["light"].BoxShadows["button.boxShadow.primary.selected"]),
            Part(outside).GetBaseValue(Border.BoxShadowProperty).GetValueOrDefault());
    }

    [AvaloniaFact]
    public void A_theme_and_a_dark_theme_follow_the_apps_variant()
    {
        var (outside, inside, _, scene) = Open(s =>
        {
            ThemeProvider.SetTheme(s, Built("lightFoundation"));
            ThemeProvider.SetDarkTheme(s, Built("darkFoundation"));
        });
        using (scene)
        {
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(outside));

            scene.Window.RequestedThemeVariant = ThemeVariant.Dark;
            scene.Pump();
            Assert.Equal(Expected("darkFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("darkFoundation", "colors.buttonPrimaryText"), Text(inside));
            Assert.Equal(Expected("dark", "colors.buttonPrimaryFill"), Fill(outside));

            scene.Window.RequestedThemeVariant = ThemeVariant.Light;
            scene.Pump();
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(outside));
        }
    }

    [AvaloniaFact]
    public void A_theme_alone_holds_whatever_the_variant_as_Base_Webs_does()
    {
        var (outside, inside, _, scene) = Open(s => ThemeProvider.SetTheme(s, Built("lightFoundation")), ThemeVariant.Dark);
        using (scene)
        {
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("dark", "colors.buttonPrimaryFill"), Fill(outside));
        }
    }

    [AvaloniaFact]
    public void A_dark_theme_in_a_light_app()
    {
        var (outside, inside, _, scene) = Open(s => ThemeProvider.SetTheme(s, Built("darkFoundation")));
        using (scene)
        {
            Assert.Equal(Expected("darkFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(outside));
        }
    }

    [AvaloniaFact]
    public void Changing_or_clearing_the_theme_restyles_the_subtree()
    {
        var (_, inside, subtree, scene) = Open(s => ThemeProvider.SetTheme(s, Built("lightFoundation")));
        using (scene)
        {
            ThemeProvider.SetTheme(subtree, Built("lightOverrides"));
            scene.Pump();
            Assert.Equal(Expected("lightOverrides", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Single(subtree.Resources.MergedDictionaries);

            ThemeProvider.SetTheme(subtree, null);
            scene.Pump();
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Empty(subtree.Resources.MergedDictionaries);
        }
    }

    [AvaloniaFact]
    public void App_wide_on_the_window()
    {
        var button = new Button { Content = "Button", Kind = Metered.Controls.ButtonKind.Secondary };
        using var scene = new Scene(new StackPanel { Children = { button } });
        ThemeProvider.SetTheme(scene.Window, Built("lightOverrides"));
        ThemeProvider.SetDarkTheme(scene.Window, Built("darkFoundation"));
        scene.Pump();
        Assert.Equal(Expected("lightOverrides", "colors.buttonSecondaryFill"), Fill(button));
        Assert.Equal(Expected("lightOverrides", "colors.buttonSecondaryText"), Text(button));

        scene.Window.RequestedThemeVariant = ThemeVariant.Dark;
        scene.Pump();
        Assert.Equal(Expected("darkFoundation", "colors.buttonSecondaryFill"), Fill(button));
    }

    [AvaloniaFact]
    public void The_default_themes_draw_as_the_app_does()
    {
        // BaseTheme.LightTheme given to a subtree changes nothing: it is BaseTheme's light dictionary, written out.
        var (outside, inside, _, scene) = Open(s => ThemeProvider.SetTheme(s, BaseTheme.LightTheme));
        using (scene)
        {
            Assert.Equal(Fill(outside), Fill(inside));
            Assert.Equal(Text(outside), Text(inside));
        }
    }
}
