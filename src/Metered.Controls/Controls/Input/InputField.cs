using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a <see cref="BaseInput"/>'s <c>Input</c> part, Base Web's <c>&lt;input&gt;</c> (or Textarea's
/// <c>&lt;textarea&gt;</c>): a box round the text, its padding and background, whose text takes the font and colour
/// it passes down (<c>TextElement</c>'s, inherited), and that holds what an <c>&lt;input&gt;</c>'s style also sets: the
/// caret's colour (CSS's <c>caret-color</c>) and the placeholder's (<c>::placeholder</c>), which the template's text
/// presenter and placeholder take from it.
/// </summary>
public class InputField : Border
{
    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<InputField, IBrush?>(nameof(CaretBrush));

    public static readonly StyledProperty<IBrush?> PlaceholderForegroundProperty =
        AvaloniaProperty.Register<InputField, IBrush?>(nameof(PlaceholderForeground));

    /// <summary>CSS's <c>caret-color</c>.</summary>
    public IBrush? CaretBrush { get => GetValue(CaretBrushProperty); set => SetValue(CaretBrushProperty, value); }

    /// <summary>CSS's <c>::placeholder</c> colour.</summary>
    public IBrush? PlaceholderForeground { get => GetValue(PlaceholderForegroundProperty); set => SetValue(PlaceholderForegroundProperty, value); }
}
