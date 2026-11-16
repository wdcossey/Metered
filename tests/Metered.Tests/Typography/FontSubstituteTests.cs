using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.VisualTree;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Typography;

/// <summary>
/// Base Web's families draw in the substitutes chosen for Uber Move (docs/decisions/0003-font.md): each
/// generated typography token's family, at its own weight, resolves to the embedded face of that
/// weight, so no text falls back to another font or to a synthesised weight.
/// </summary>
public class FontSubstituteTests
{
    private const string Primary = "UberMoveText";
    private const string Secondary = "UberMove";

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
        var weight = (FontWeight)scene.Window.FindResource($"typography.{token}.fontWeight")!;

        Assert.True(FontManager.Current.TryGetGlyphTypeface(new Typeface(family, FontStyle.Normal, weight), out var face));

        var expected = family.FamilyNames[0] switch
        {
            Primary => TestApp.TextFamily,
            Secondary => TestApp.DisplayFamily,
            var other => throw new InvalidOperationException($"{token}: an unexpected family, {other}"),
        };
        Assert.Equal(expected, Family(face));
        Assert.Equal(weight, face.Weight);
        Assert.Equal(FontSimulations.None, face.FontSimulations);
    }

    [AvaloniaFact]
    public void A_Buttons_label_draws_in_the_text_substitute_at_500()
    {
        var button = new Button { Content = "Button" };
        using var scene = new Scene(new StackPanel { Children = { button } });
        var label = button.GetVisualDescendants().OfType<TextBlock>().Single();

        var runs = label.TextLayout.TextLines.SelectMany(l => l.TextRuns).OfType<ShapedTextRun>().ToList();
        Assert.NotEmpty(runs);
        Assert.All(runs, run =>
        {
            Assert.Equal(TestApp.TextFamily, Family(run.GlyphRun.GlyphTypeface));
            Assert.Equal(FontWeight.Medium, run.GlyphRun.GlyphTypeface.Weight);
        });
    }
}
