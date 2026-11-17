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
            Assert.Single(subtree.Styles);

            ThemeProvider.SetTheme(subtree, null);
            scene.Pump();
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Empty(subtree.Styles);
        }
    }

    [AvaloniaFact]
    public void The_theme_outlasts_the_elements_resources_being_replaced()
    {
        // As XAML does: the attribute first, then a <Border.Resources><ResourceDictionary> property element.
        var (outside, inside, subtree, scene) = Open(s =>
        {
            ThemeProvider.SetTheme(s, Built("lightFoundation"));
            s.Resources = new ResourceDictionary();
        });
        using (scene)
        {
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(outside));

            subtree.Resources = new ResourceDictionary();
            scene.Pump();
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Single(subtree.Styles);
        }
    }

    [AvaloniaFact]
    public void A_buttons_artwork_size_follows_a_theme_given_after_it_is_attached()
    {
        // button/styled-components.js, getArtworkSize: a small button's artwork is the theme's sizing.scale600.
        var small = Oracle.Themes["lightOverrides"].Changed.Single(t => t.Key == "sizing.scale600").Value.GetDouble();
        var (outside, inside, subtree, scene) = Open(_ => { });
        outside.Size = inside.Size = Metered.Controls.ButtonSize.Small;
        using (scene)
        {
            scene.Pump();
            var before = inside.ArtworkSize;
            Assert.NotEqual(small, before);

            ThemeProvider.SetTheme(subtree, Built("lightOverrides"));
            scene.Pump();
            Assert.Equal(small, inside.ArtworkSize);
            Assert.Equal(before, outside.ArtworkSize);
        }
    }

    [AvaloniaFact]
    public void A_buttons_artwork_size_follows_the_variant_between_a_theme_and_a_dark_theme()
    {
        // getArtworkSize as above; the dark theme here is lightOverrides, whose sizing.scale600 Base Web changed.
        var small = Oracle.Themes["lightOverrides"].Changed.Single(t => t.Key == "sizing.scale600").Value.GetDouble();
        var (_, inside, _, scene) = Open(s =>
        {
            ThemeProvider.SetTheme(s, Built("light"));
            ThemeProvider.SetDarkTheme(s, Built("lightOverrides"));
        });
        inside.Size = Metered.Controls.ButtonSize.Small;
        using (scene)
        {
            scene.Pump();
            var light = inside.ArtworkSize;
            Assert.NotEqual(small, light);

            scene.Window.RequestedThemeVariant = ThemeVariant.Dark;
            scene.Pump();
            Assert.Equal(small, inside.ArtworkSize);

            scene.Window.RequestedThemeVariant = ThemeVariant.Light;
            scene.Pump();
            Assert.Equal(light, inside.ArtworkSize);
        }
    }

    [AvaloniaFact]
    public void The_theme_outranks_styles_added_after_it_and_outlasts_their_clearing()
    {
        var button = new Button { Content = "Button" };
        using var scene = new Scene(new StackPanel { Children = { button } });
        // As <Window metered:ThemeProvider.Theme="…"><Window.Styles><BaseTheme/> applies them: the attribute first.
        ThemeProvider.SetTheme(scene.Window, Built("lightFoundation"));
        scene.Window.Styles.Add(new BaseTheme());
        scene.Pump();
        Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(button));
        Assert.NotEqual(Expected("light", "colors.buttonPrimaryFill"), Fill(button));

        var (_, inside, subtree, sub) = Open(s => ThemeProvider.SetTheme(s, Built("lightFoundation")));
        using (sub)
        {
            subtree.Styles.Clear();
            sub.Pump();
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(inside));
            Assert.Single(subtree.Styles);
        }
    }

    [AvaloniaFact]
    public void A_buttons_tap_band_follows_the_variant_between_a_theme_and_a_dark_theme()
    {
        // button/styled-components.js, getMinHitAreaStyles: the tap band is the theme's sizing.scale1200 tall;
        // lightOverrides changes it.
        var band = Oracle.Themes["lightOverrides"].Changed.Single(t => t.Key == "sizing.scale1200").Value.GetDouble();
        var (_, inside, _, scene) = Open(s =>
        {
            ThemeProvider.SetTheme(s, Built("light"));
            ThemeProvider.SetDarkTheme(s, Built("lightOverrides"));
        });
        inside.Size = Metered.Controls.ButtonSize.XSmall;
        inside.MinHitArea = Metered.Controls.ButtonMinHitArea.Tap;
        using (scene)
        {
            scene.Pump();
            var light = inside.Bounds.Height;
            Assert.NotEqual(band, light);

            scene.Window.RequestedThemeVariant = ThemeVariant.Dark;
            scene.Pump();
            Assert.Equal(band, inside.Bounds.Height, 3);

            scene.Window.RequestedThemeVariant = ThemeVariant.Light;
            scene.Pump();
            Assert.Equal(light, inside.Bounds.Height, 3);
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

    /// <summary>
    /// <paramref name="test"/> with <see cref="BaseTheme"/> in the application's styles, as the sample's <c>App.axaml</c>
    /// has it, and <paramref name="resources"/> in its resources, as docs/theming.md tells an app to. The application
    /// looks in its resources before its styles. Avalonia.Headless gives each test an application of its own (its
    /// default isolation, <c>AvaloniaTestIsolationLevel.PerTest</c>; this assembly sets none) and runs one test at a
    /// time on its thread, so this can't reach another test. Both are taken out again so that it still couldn't under
    /// <c>PerAssembly</c> isolation.
    /// </summary>
    private static void WithAppResources(ResourceDictionary resources, Action<Avalonia.Application> test)
    {
        var app = Avalonia.Application.Current!;
        var theme = new BaseTheme();
        app.Styles.Add(theme);
        app.Resources.MergedDictionaries.Add(resources);
        try
        {
            test(app);
        }
        finally
        {
            app.Resources.MergedDictionaries.Remove(resources);
            app.Styles.Remove(theme);
        }
    }

    /// <summary>
    /// A precondition: the two oracle cases differ in each of these tokens, so a colour asserted to be the one's can't
    /// pass as the other's. Not every token differs (dark and darkFoundation share the primary fill).
    /// </summary>
    private static void Distinct(string one, string other, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            Assert.NotEqual(Expected(one, token), Expected(other, token));
        }
    }

    [AvaloniaFact]
    public void App_wide_through_the_applications_resources_in_every_window()
    {
        const string tertiaryText = "colors.buttonTertiaryText";
        const string secondaryText = "colors.buttonSecondaryText";
        const string dangerFill = "colors.buttonDangerPrimaryFill";
        Distinct("light", "lightFoundation", tertiaryText, secondaryText, dangerFill);
        Distinct("dark", "darkFoundation", tertiaryText, secondaryText, dangerFill);
        Distinct("lightFoundation", "darkFoundation", tertiaryText, secondaryText, dangerFill);

        var resources = ThemeProvider.CreateResources(Built("lightFoundation"), Built("darkFoundation"));
        WithAppResources(resources, app =>
        {
            var top = new Button { Content = "Top", Kind = Metered.Controls.ButtonKind.Tertiary };
            var nested = new Button { Content = "Nested", Kind = Metered.Controls.ButtonKind.Secondary };
            var other = new Button { Content = "Other window", Kind = Metered.Controls.ButtonKind.DangerPrimary };
            using var scene = new Scene(new StackPanel
            {
                Children = { top, new Border { Child = new StackPanel { Children = { nested } } } },
            }, themes: []);
            using var second = new Scene(other, themes: []);

            void Drawn(string theme)
            {
                Assert.Equal(Expected(theme, tertiaryText), Text(top));
                Assert.Equal(Expected(theme, secondaryText), Text(nested));
                Assert.Equal(Expected(theme, dangerFill), Fill(other));
            }

            Drawn("lightFoundation");

            scene.Window.RequestedThemeVariant = second.Window.RequestedThemeVariant = ThemeVariant.Dark;
            scene.Pump();
            second.Pump();
            Drawn("darkFoundation");

            // Taken out, the application's own dark tokens: the colours above were the dictionary's.
            app.Resources.MergedDictionaries.Remove(resources);
            scene.Pump();
            second.Pump();
            Drawn("dark");

            // Added again with the windows open, as an app changing its theme at run time does.
            app.Resources.MergedDictionaries.Add(resources);
            scene.Pump();
            second.Pump();
            Drawn("darkFoundation");
        });
    }

    [AvaloniaFact]
    public void App_wide_a_theme_alone_holds_whatever_the_variant()
    {
        Distinct("dark", "lightFoundation", "colors.buttonPrimaryFill", "colors.buttonPrimaryText");
        WithAppResources(ThemeProvider.CreateResources(Built("lightFoundation")), _ =>
        {
            var button = new Button { Content = "Button" };
            using var scene = new Scene(button, ThemeVariant.Dark, themes: []);
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryFill"), Fill(button));
            Assert.Equal(Expected("lightFoundation", "colors.buttonPrimaryText"), Text(button));
        });
    }

    [AvaloniaFact]
    public void App_wide_gives_way_to_a_BaseTheme_in_a_windows_styles()
    {
        // docs/theming.md: a window's styles are looked in before the application, so BaseTheme's own tokens win there.
        Distinct("light", "lightFoundation", "colors.buttonPrimaryFill");
        WithAppResources(ThemeProvider.CreateResources(Built("lightFoundation")), _ =>
        {
            var button = new Button { Content = "Button" };
            using var scene = new Scene(button);
            Assert.Equal(Expected("light", "colors.buttonPrimaryFill"), Fill(button));
        });
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
