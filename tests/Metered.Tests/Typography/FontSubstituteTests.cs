using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Metered.Themes;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Typography;

/// <summary>
/// Base Web's families draw in the substitutes chosen for Uber Move (docs/decisions/0003-font.md): each
/// ramp token's family, at its own weight, resolves to the embedded face of that weight, so no text
/// falls back to another font or to a synthesised weight. UberMoveMono, the Mono styles' family, has
/// no substitute (the ADR). The expected families aren't Base Web's, whose value is Uber Move itself:
/// they are the substitutes ADR 0003 chose by measurement against baseweb.design.
/// </summary>
public class FontSubstituteTests
{
    private const string Primary = "UberMoveText";
    private const string Secondary = "UberMove";
    private const string Mono = "UberMoveMono";

    // A static Medium or SemiBold face names its family "Instrument Sans Medium" (name ID 1) and
    // the whole family only as its typographic family (ID 16), which Regular and Bold don't carry.
    private static string Family(GlyphTypeface face) =>
        string.IsNullOrEmpty(face.TypographicFamilyName) ? face.FamilyName : face.TypographicFamilyName;

    // Base Web's ramp (baseui/themes/shared/typography.js); the named styles are aliases of these.
    public static TheoryData<string> Ramp =>
    [
        "font100", "font150", "font200", "font250", "font300", "font350", "font400", "font450",
        "font550", "font650", "font750", "font850", "font950", "font1050", "font1150", "font1250", "font1350", "font1450",
    ];

    [AvaloniaTheory]
    [MemberData(nameof(Ramp))]
    public void A_ramp_token_draws_in_its_substitute_at_its_weight(string token)
    {
        using var scene = new Scene();
        var family = (FontFamily)scene.Window.FindResource($"typography.{token}.fontFamily")!;
        Assert.NotEqual(Mono, family.FamilyNames[0]);
        Assert.Null(Mismatch(scene, token));
    }

    // Every generated style, the named ones (ParagraphMedium, HeadingLarge, ...) as well as the ramp,
    // read from the theme's own keys; the Mono styles have no substitute (the ADR).
    [AvaloniaFact]
    public void Every_generated_style_draws_in_its_substitute_at_its_weight()
    {
        using var scene = new Scene();
        var styles = new BaseTheme().Resources.MergedDictionaries.OfType<ResourceInclude>()
            .Select(include => include.Loaded).OfType<ResourceDictionary>()
            .SelectMany(d => d.Keys).OfType<string>()
            .Select(k => Regex.Match(k, @"^typography\.(\w+)\.fontFamily$")).Where(m => m.Success).Select(m => m.Groups[1].Value)
            .Where(s => ((FontFamily)scene.Window.FindResource($"typography.{s}.fontFamily")!).FamilyNames[0] != Mono)
            .ToList();

        Assert.Contains("ParagraphMedium", styles);
        Assert.Contains("DisplayLarge", styles);
        Assert.Empty(styles.Select(s => Mismatch(scene, s)).OfType<string>());
    }

    // What's wrong with a style's face, or null: its family, at its weight, must resolve to the
    // substitute's face of exactly that weight, unsynthesised.
    private static string? Mismatch(Scene scene, string style)
    {
        var family = (FontFamily)scene.Window.FindResource($"typography.{style}.fontFamily")!;
        var weight = (FontWeight)scene.Window.FindResource($"typography.{style}.fontWeight")!;
        var expected = family.FamilyNames[0] switch
        {
            Primary => TestApp.TextFamily,
            Secondary => TestApp.DisplayFamily,
            var other => $"an unexpected family, {other}",
        };
        if (!FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var face))
        {
            return $"{style}: no face";
        }

        return Family(face) == expected && face.Weight == weight && face.FontSimulations == FontSimulations.None
            ? null
            : $"{style}: {Family(face)} {face.Weight} ({face.FontSimulations}), expected {expected} {weight}";
    }

    // A family Avalonia can't find falls back to the default; were that a substitute, the ramp test
    // couldn't tell a mapped family from a missing mapping (TestApp leaves the default to the platform).
    [AvaloniaFact]
    public void A_family_with_no_mapping_does_not_draw_in_a_substitute()
    {
        using var scene = new Scene();
        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface("Metered-NoSuchFamily"), out var face));
        Assert.DoesNotContain(Family(face), new[] { TestApp.TextFamily, TestApp.DisplayFamily });
    }

    // A medium Button's label is LabelMedium (baseui/button/styled-components.js, getFontStyles by
    // size): UberMoveText at 500 (baseui/themes/shared/typography.js), as the generated token has it.
    [AvaloniaFact]
    public void A_Buttons_label_draws_in_the_text_substitute_at_LabelMediums_weight()
    {
        var button = new Button { Content = "Button" };
        using var scene = new Scene(new StackPanel { Children = { button } });
        var weight = (FontWeight)scene.Window.FindResource("typography.LabelMedium.fontWeight")!;
        Assert.Equal(Primary, ((FontFamily)scene.Window.FindResource("typography.LabelMedium.fontFamily")!).FamilyNames[0]);
        var label = button.GetVisualDescendants().OfType<TextBlock>().Single();

        var runs = label.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>().ToList();
        Assert.NotEmpty(runs);
        Assert.All(runs, run =>
        {
            Assert.Equal(TestApp.TextFamily, Family(run.GlyphRun.GlyphTypeface));
            Assert.Equal(weight, run.GlyphRun.GlyphTypeface.Weight);
            Assert.Equal(FontSimulations.None, run.GlyphRun.GlyphTypeface.FontSimulations);
        });
    }
}
