using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// The element of a form control's <c>CaptionIcon</c> part: Base Web's div round the caption's icon and the icon's svg
/// in it, one element. The div's padding (<see cref="Avalonia.Controls.Decorator.Padding"/>) is round the icon's box, in
/// which Base Web's alert or check icon (<see cref="InputIcon.Data"/>, its theme's by the <c>:icon-alert</c> or
/// <c>:icon-check</c> state) is drawn in the caption's colour, CSS's <c>currentColor</c>.
/// </summary>
/// <remarks>Base Web renders the icon <c>aria-hidden</c>: it isn't in UI Automation's views.</remarks>
public class FormControlCaptionIcon : InputIcon
{
    /// <summary>The icon, inside the padding: the svg's box in the div's.</summary>
    protected override void RenderImage(DrawingContext context, RoundedRect border, Rect padding) =>
        base.RenderImage(context, border, padding.Deflate(Padding));

    protected override AutomationPeer OnCreateAutomationPeer() => new NoneAutomationPeer(this);
}
