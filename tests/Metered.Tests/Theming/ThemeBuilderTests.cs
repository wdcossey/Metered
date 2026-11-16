using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Metered.Themes;

namespace Metered.Tests.Theming;

/// <summary>
/// <see cref="Theme"/>'s builders against Base Web's own createTheme, createLightTheme and createDarkTheme
/// (Oracle.Themes, tools/reference/themes.js), token for token: every colour, every Button box-shadow, and every
/// other token, which is either a leaf Base Web's deep merge changed or the theme's own (the generated
/// dictionaries, from Base Web's LightTheme and DarkTheme).
/// </summary>
public class ThemeBuilderTests
{
    public static TheoryData<string> Cases => new(Oracle.Themes.Cases.Select(c => c.Name));

    internal static Theme Build(ThemeCase c)
    {
        var overrides = c.Overrides.ToDictionary(t => t.Key, Value, StringComparer.Ordinal);
        return c.Create switch
        {
            "createTheme" => BaseTheme.CreateTheme(overrides),
            "createLightTheme" => BaseTheme.CreateLightTheme(overrides),
            "createDarkTheme" => BaseTheme.CreateDarkTheme(overrides),
            _ => throw new InvalidOperationException(c.Create),
        };
    }

    /// <summary>An oracle value as the theme holds it.</summary>
    internal static object Value(OracleToken t) => t.Type switch
    {
        "colour" => Color.Parse(t.Value.GetString()!),
        "double" => t.Value.GetDouble(),
        "boxShadows" => BoxShadows.Parse(t.Value.GetString()!),
        "cornerRadius" => new CornerRadius(t.Value.GetDouble()),
        "thickness" => new Thickness(t.Value.GetDouble()),
        "string" => t.Value.GetString()!,
        "bool" => t.Value.GetBoolean(),
        "fontWeight" => Enum.Parse<FontWeight>(t.Value.GetString()!),
        "fontFamily" => new FontFamily(t.Value.GetString()!),
        _ => throw new InvalidOperationException($"{t.Key}: a {t.Type}"),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Every_token_is_Base_Webs(string name)
    {
        var c = Oracle.Themes[name];
        if (c.Colours.Any(p => p.Value is null))
        {
            // Base Web gives no colour (''): the builder says so rather than give one.
            var none = c.Colours.First(p => p.Value is null).Key;
            Assert.Contains(none, Assert.Throws<ArgumentException>(() => Build(c)).Message);
            return;
        }

        var theme = Build(c);
        Assert.Equal(c.ThemeName, theme.Name);
        var defaults = ThemeDefaults.For(theme.Variant);
        var changed = c.Changed.ToDictionary(t => t.Key, Value, StringComparer.Ordinal);
        var wrong = new List<string>();

        foreach (var (key, want) in c.Colours)
        {
            object expected = key.EndsWith("RampUnit", StringComparison.Ordinal) ? want! : Color.Parse(want!);
            Check(key, expected);
        }

        foreach (var (key, want) in c.BoxShadows)
        {
            Check(key, BoxShadows.Parse(want));
        }

        foreach (var key in theme.Tokens.Keys.Where(k => !c.Colours.ContainsKey(k) && !c.BoxShadows.ContainsKey(k)))
        {
            var group = key[..key.IndexOf('.')];
            if (c.Unchanged.Contains(group))
            {
                Check(key, defaults[key]);
            }
            else
            {
                Check(key, changed.TryGetValue(key, out var v) ? v : defaults[key]);
            }
        }

        // Every leaf Base Web changed is a token here.
        wrong.AddRange(changed.Keys.Where(k => !theme.Tokens.ContainsKey(k)).Select(k => $"{k}: missing"));
        Assert.True(wrong.Count == 0, $"{wrong.Count} tokens differ from Base Web's:\n" + string.Join("\n", wrong));

        void Check(string key, object expected)
        {
            if (!theme.Tokens.TryGetValue(key, out var actual))
            {
                wrong.Add($"{key}: missing");
            }
            else if (!Equals(expected, actual))
            {
                wrong.Add($"{key}: {actual}, Base Web {expected}");
            }
        }
    }

    [AvaloniaFact]
    public void A_theme_has_every_token_of_the_generated_dictionaries_and_no_other()
    {
        Assert.Equal(ThemeDefaults.For(ThemeVariant.Light).Keys.Order(), BaseTheme.LightTheme.Tokens.Keys.Order());
        Assert.Equal(ThemeDefaults.For(ThemeVariant.Dark).Keys.Order(), BaseTheme.DarkTheme.Tokens.Keys.Order());
        Assert.Equal(ThemeVariant.Light, BaseTheme.CreateTheme().Variant);
        Assert.Equal(ThemeVariant.Dark, BaseTheme.CreateDarkTheme().Variant);
    }

    [AvaloniaFact]
    public void The_foundation_colours_are_Base_Webs()
    {
        // themes/utils.js: getFoundationColorOverrides.
        Assert.Equal(["primaryA", "primaryB", "primary", "accent", "negative", "warning", "positive"], BaseTheme.FoundationColors);
    }

    [AvaloniaFact]
    public void An_override_is_taken_as_its_tokens_type()
    {
        var accent = Color.Parse(Oracle.Themes["lightFoundation"].Colours["colors.accent"]!);
        var theme = BaseTheme.CreateTheme(new Dictionary<string, object>
        {
            ["colors.accent"] = new SolidColorBrush(accent),
            ["sizing.scale600"] = 20,
            ["colors.myBrand"] = accent,
        });
        Assert.Equal(accent, theme["colors.backgroundAccent"]);
        Assert.Equal(20.0, theme["sizing.scale600"]);
        // A key Base Web's theme hasn't is kept, as deepMerge keeps it.
        Assert.Equal(accent, theme["colors.myBrand"]);

        Assert.Throws<ArgumentException>(() => BaseTheme.CreateTheme(new Dictionary<string, object> { ["sizing.scale600"] = "20px" }));
        Assert.Throws<ArgumentException>(() => BaseTheme.CreateTheme(new Dictionary<string, object> { ["colors.accent"] = 1.0 }));
        Assert.Throws<ArgumentException>(() =>
            BaseTheme.CreateTheme(new Dictionary<string, object> { ["colors.accent"] = new SolidColorBrush(accent, 0.5) }));
    }

    [AvaloniaFact]
    public void The_resource_dictionary_holds_every_token_colours_as_solid_brushes()
    {
        var theme = Build(Oracle.Themes["lightFoundation"]);
        var dictionary = theme.ToResourceDictionary();
        Assert.Equal(theme.Tokens.Count, dictionary.Count);
        var fill = Assert.IsAssignableFrom<ISolidColorBrush>(dictionary["colors.buttonPrimaryFill"]);
        Assert.Equal(theme["colors.buttonPrimaryFill"], fill.Color);
        Assert.Equal(theme["sizing.scale600"], dictionary["sizing.scale600"]);

        // A dictionary has one owner: each call gives a new one.
        Assert.NotSame(dictionary, theme.ToResourceDictionary());
    }
}
