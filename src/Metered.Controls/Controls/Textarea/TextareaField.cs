using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="Textarea"/>'s <c>Input</c> part, Base Web's <c>&lt;textarea&gt;</c>
/// (<c>StyledTextarea</c>): an <see cref="InputField"/> sized as a browser sizes a textarea, its height
/// <see cref="Rows"/> lines of its text and its padding, and whose padding is inside its scroller, so that the text
/// scrolls under it as a textarea's does. <see cref="Resize"/> is CSS's <c>resize</c>, which its grip drags.
/// </summary>
/// <remarks>
/// <para>Its <see cref="Decorator.Padding"/> is the style function's, set by the part's theme, and isn't applied round
/// its child: the template gives it to the scroller's content as a margin, which scrolls with the text.</para>
/// <para>Its width, where nothing stretches it (the root is <c>fit-content</c> once the textarea can be resized), is a
/// browser's for a textarea of HTML's 20 columns (<c>cols</c>, which Base Web never sets): Chrome's 20 times the font's
/// average character width (its <c>OS/2</c> table's <c>xAvgCharWidth</c>, rounded to a pixel; the width of <c>0</c> where
/// a font has none), with no scroll bar's width, as on a phone, whose scroll bars are drawn over the text.</para>
/// </remarks>
public class TextareaField : InputField
{
    /// <summary>HTML's <c>cols</c> when it isn't given, as Base Web never gives it.</summary>
    public const int Cols = 20;

    /// <summary>HTML's <c>rows</c> when the value isn't a number above 0.</summary>
    public const int HtmlDefaultRows = 2;

    /// <summary>Base Web's default <c>rows</c> (textarea.js's defaultProps).</summary>
    public const int DefaultRows = 3;

    public static readonly StyledProperty<int> RowsProperty =
        AvaloniaProperty.Register<TextareaField, int>(nameof(Rows), DefaultRows);

    public static readonly StyledProperty<TextareaResize> ResizeProperty =
        AvaloniaProperty.Register<TextareaField, TextareaResize>(nameof(Resize));

    private static readonly ConditionalWeakTable<GlyphTypeface, StrongBox<short?>> s_averageWidths = new();

    private Size _measuredWith;

    static TextareaField()
    {
        AffectsMeasure<TextareaField>(RowsProperty, TextBlock.LineHeightProperty, TextElement.FontSizeProperty,
            TextElement.FontFamilyProperty, TextElement.FontWeightProperty, TextElement.FontStyleProperty);
    }

    /// <summary>HTML's <c>rows</c>: how many lines of text tall it is.</summary>
    public int Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>CSS's <c>resize</c>, from the part's theme (<c>$resize || 'none'</c>): which way its grip drags.</summary>
    public TextareaResize Resize { get => GetValue(ResizeProperty); set => SetValue(ResizeProperty, value); }

    /// <summary>The lines it is tall: <see cref="Rows"/>, or HTML's 2 for a value that isn't above 0.</summary>
    public int Lines => Rows > 0 ? Rows : HtmlDefaultRows;

    /// <summary>A line of its text: its <c>line-height</c>, or the font's own where it has none.</summary>
    public double LineHeight
    {
        get
        {
            var line = GetValue(TextBlock.LineHeightProperty);
            if (!double.IsNaN(line) && line > 0) return line;
            using var layout = new TextLayout(" ", Typeface, TextElement.GetFontSize(this), null);
            return layout.Height;
        }
    }

    /// <summary>The width a browser gives a textarea of <see cref="Cols"/> columns in its font, and its padding.</summary>
    public double IntrinsicWidth => Cols * AverageCharWidth() + Padding.Left + Padding.Right;

    private Typeface Typeface => new(TextElement.GetFontFamily(this), TextElement.GetFontStyle(this),
        TextElement.GetFontWeight(this), TextElement.GetFontStretch(this));

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var size = new Size(Math.Min(IntrinsicWidth, availableSize.Width), Lines * LineHeight + padding.Top + padding.Bottom);
        MeasureChild(size);
        return size;
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Laid out at another size than it was measured at (stretched, or resized): its text wraps at the width it has.
        if (_measuredWith != finalSize) MeasureChild(finalSize);
        Child?.Arrange(new Rect(finalSize));
        return finalSize;
    }

    private void MeasureChild(Size size)
    {
        _measuredWith = size;
        Child?.Measure(size);
    }

    /// <summary>Chrome's average character width: the <c>OS/2</c> table's, rounded; else the width of <c>0</c>.</summary>
    internal double AverageCharWidth()
    {
        var typeface = Typeface;
        var size = TextElement.GetFontSize(this);
        var glyph = typeface.GlyphTypeface;
        var units = s_averageWidths.GetValue(glyph, g => new StrongBox<short?>(Os2AverageWidth(g))).Value;
        if (units is > 0 && glyph.Metrics.DesignEmHeight > 0)
        {
            return Math.Round(units.Value * size / glyph.Metrics.DesignEmHeight, MidpointRounding.AwayFromZero);
        }

        using var layout = new TextLayout("0", typeface, size, null);
        return layout.WidthIncludingTrailingWhitespace;
    }

    /// <summary>A font file's <c>OS/2</c> <c>xAvgCharWidth</c>, in its design units; null where it can't be read.</summary>
    internal static short? Os2AverageWidth(GlyphTypeface glyph)
    {
        try
        {
            if (!glyph.PlatformTypeface.TryGetStream(out var stream)) return null;
            using (stream)
            {
                Span<byte> header = stackalloc byte[12];
                if (!Read(stream, 0, header)) return null;
                var tables = BinaryPrimitives.ReadUInt16BigEndian(header[4..]);
                Span<byte> record = stackalloc byte[16];
                for (var i = 0; i < tables; i++)
                {
                    if (!Read(stream, 12 + 16 * i, record)) return null;
                    if (record[0] != 'O' || record[1] != 'S' || record[2] != '/' || record[3] != '2') continue;
                    Span<byte> os2 = stackalloc byte[4];
                    var offset = BinaryPrimitives.ReadUInt32BigEndian(record[8..]);
                    return Read(stream, offset, os2) ? BinaryPrimitives.ReadInt16BigEndian(os2[2..]) : null;
                }

                return null;
            }
        }
        catch (Exception e) when (e is IOException or NotSupportedException or ObjectDisposedException)
        {
            return null;
        }
    }

    private static bool Read(Stream stream, long at, Span<byte> into)
    {
        if (!stream.CanSeek || at + into.Length > stream.Length) return false;
        stream.Position = at;
        stream.ReadExactly(into);
        return true;
    }
}
