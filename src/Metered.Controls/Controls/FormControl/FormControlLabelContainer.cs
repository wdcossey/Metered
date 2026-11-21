using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>LabelContainer</c>, a flex row of the label and its end enhancer (<c>display: flex</c>): the enhancer
/// (<c>flex: 0</c>, so a basis of 0 that doesn't grow) at its min-content width, the width of its longest unbreakable
/// run, the label (<c>width: 100%</c>, which flex shrinks) the rest; both as tall as the row, which is as tall as the
/// taller (<c>align-items: stretch</c>).
/// </summary>
/// <remarks>The first child is the label and the second the enhancer, as form-control.js renders them; a hidden one
/// takes no room.</remarks>
public class FormControlLabelContainer : Panel
{
    private Control? Label => Children.Count > 0 && Children[0].IsVisible ? Children[0] : null;

    private Control? End => Children.Count > 1 && Children[1].IsVisible ? Children[1] : null;

    protected override Size MeasureOverride(Size availableSize)
    {
        var (endWidth, endHeight) = MeasureEnd(availableSize.Width);
        var labelHeight = 0.0;
        var labelWidth = 0.0;
        if (Label is { } label)
        {
            label.Measure(new Size(Math.Max(0, availableSize.Width - endWidth), double.PositiveInfinity));
            labelWidth = label.DesiredSize.Width;
            labelHeight = label.DesiredSize.Height;
        }

        return new Size(labelWidth + endWidth, Math.Max(labelHeight, endHeight));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var endWidth = End?.DesiredSize.Width ?? 0;
        Label?.Arrange(new Rect(0, 0, Math.Max(0, finalSize.Width - endWidth), finalSize.Height));
        End?.Arrange(new Rect(Math.Max(0, finalSize.Width - endWidth), 0, endWidth, finalSize.Height));
        return finalSize;
    }

    /// <summary>
    /// The enhancer's min-content width, no wider than the row; then its height at that width. For text (a presenter's
    /// text block) its widest word, the text between spaces, in its font, with the padding and margins round it; for
    /// other content its own width.
    /// </summary>
    private (double Width, double Height) MeasureEnd(double available)
    {
        if (End is not { } end) return (0, 0);
        end.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        var width = end.DesiredSize.Width;
        if (end is ContentPresenter { Child: TextBlock { Text: { } content } text } presenter)
        {
            width = Widest(text, content) + text.Margin.Left + text.Margin.Right + text.Padding.Left + text.Padding.Right
                + presenter.Padding.Left + presenter.Padding.Right + end.Margin.Left + end.Margin.Right;
        }

        // Whole device pixels, so that the label and the enhancer meet at one and the enhancer ends at the row's end.
        var scale = LayoutHelper.GetLayoutScale(this);
        width = Math.Min(Math.Ceiling(width * scale) / scale, available);
        end.Measure(new Size(width, double.PositiveInfinity));
        return (width, end.DesiredSize.Height);
    }

    /// <summary>The white space a line breaks at: all but the no-break spaces (U+00A0, U+2007, U+202F).</summary>
    private static readonly char[] Breaks = Enumerable.Range(0, 0x3001).Select(c => (char)c)
        .Where(c => char.IsWhiteSpace(c) && c is not ('\u00A0' or '\u2007' or '\u202F')).ToArray();

    private (string Text, Typeface Typeface, double Size, double Width)? _widest;

    /// <summary>
    /// The widest word of the text in the text block's font: CSS's min-content width of text, its words broken at the
    /// spaces a line breaks at. The last is kept: a counter's text changes as it is typed, its font doesn't.
    /// </summary>
    private double Widest(TextBlock text, string content)
    {
        var typeface = new Typeface(text.FontFamily, text.FontStyle, text.FontWeight, text.FontStretch);
        if (_widest is { } last && last.Text == content && last.Typeface == typeface && last.Size == text.FontSize) return last.Width;
        var widest = 0.0;
        foreach (var word in content.Split(Breaks, StringSplitOptions.RemoveEmptyEntries))
        {
            using var layout = new TextLayout(word, typeface, text.FontSize, null);
            widest = Math.Max(widest, layout.WidthIncludingTrailingWhitespace);
        }

        _widest = (content, typeface, text.FontSize, widest);
        return widest;
    }
}
