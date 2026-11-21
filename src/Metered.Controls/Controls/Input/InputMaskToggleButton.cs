using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using AvaloniaButton = Avalonia.Controls.Button;

namespace Metered.Controls;

/// <summary>
/// The element of an input's <c>MaskToggleButton</c> part, Base Web's <c>&lt;button type="button"&gt;</c>
/// (<c>StyledMaskToggleButton</c>): an Avalonia <see cref="AvaloniaButton"/>, clicked by a pointer, Enter or Space, a
/// button to UI Automation, that also draws CSS's <c>outline</c> (<see cref="OutlineBrush"/>,
/// <see cref="OutlineThickness"/>, <see cref="OutlineOffset"/>), its focus ring on keyboard focus.
/// </summary>
/// <remarks>
/// Its template is its own, by default (a theme's <c>Template</c> replaces it): radio-v2's mark (<see cref="RadioMark"/>)
/// with the button's background, padding and outline, round its content. Its look is the part's theme,
/// <c>Input.StyledMaskToggleButton</c>.
/// </remarks>
public class InputMaskToggleButton : AvaloniaButton
{
    public static readonly StyledProperty<IBrush?> OutlineBrushProperty =
        RadioMark.OutlineBrushProperty.AddOwner<InputMaskToggleButton>();

    public static readonly StyledProperty<double> OutlineThicknessProperty =
        RadioMark.OutlineThicknessProperty.AddOwner<InputMaskToggleButton>();

    public static readonly StyledProperty<double> OutlineOffsetProperty =
        RadioMark.OutlineOffsetProperty.AddOwner<InputMaskToggleButton>();

    static InputMaskToggleButton()
    {
        TemplateProperty.OverrideDefaultValue<InputMaskToggleButton>(new FuncControlTemplate<InputMaskToggleButton>((button, scope) =>
        {
            var content = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = button[!ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = button[!ContentTemplateProperty],
                [!HorizontalAlignmentProperty] = button[!HorizontalContentAlignmentProperty],
                [!VerticalAlignmentProperty] = button[!VerticalContentAlignmentProperty],
            };
            scope.Register(content.Name, content);
            return new RadioMark
            {
                Name = "PART_Box",
                [!RadioMark.BackgroundProperty] = button[!BackgroundProperty],
                [!RadioMark.BorderBrushProperty] = button[!BorderBrushProperty],
                [!RadioMark.BorderThicknessProperty] = button[!BorderThicknessProperty],
                [!RadioMark.CornerRadiusProperty] = button[!CornerRadiusProperty],
                [!PaddingProperty] = button[!PaddingProperty],
                [!RadioMark.OutlineBrushProperty] = button[!OutlineBrushProperty],
                [!RadioMark.OutlineThicknessProperty] = button[!OutlineThicknessProperty],
                [!RadioMark.OutlineOffsetProperty] = button[!OutlineOffsetProperty],
                Child = content,
            };
        }));
    }

    /// <summary>The outline's colour; none when null.</summary>
    public IBrush? OutlineBrush { get => GetValue(OutlineBrushProperty); set => SetValue(OutlineBrushProperty, value); }

    /// <summary>The outline's width; none when 0.</summary>
    public double OutlineThickness { get => GetValue(OutlineThicknessProperty); set => SetValue(OutlineThicknessProperty, value); }

    /// <summary>The gap between the button and its outline.</summary>
    public double OutlineOffset { get => GetValue(OutlineOffsetProperty); set => SetValue(OutlineOffsetProperty, value); }

    protected override Type StyleKeyOverride => typeof(InputMaskToggleButton);
}
