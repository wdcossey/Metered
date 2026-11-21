using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using AvaloniaLabel = Avalonia.Controls.Label;

namespace Metered.Controls;

/// <summary>
/// The element of a form control's <c>Label</c> part, Base Web's <c>&lt;label htmlFor&gt;</c>: an Avalonia
/// <see cref="AvaloniaLabel"/> whose <see cref="AvaloniaLabel.Target"/> is the element it labels, focused when the label
/// is clicked or tapped, as a browser activates a label on <c>click</c>: a press on it and a release on it, the pointer
/// not taken from it in between.
/// </summary>
/// <remarks>
/// <para>Avalonia's label focuses its target on a press. A browser's doesn't, and on a phone that matters: a finger that
/// lands on a label to scroll the page would focus the text box under it and open the keyboard. Here a press does
/// nothing; the release does, unless a scroller has taken the pointer for a drag (its capture, which the label loses),
/// or the pointer is let go elsewhere. Each click of a double click is one, as a browser's are.</para>
/// <para>Its whole box takes the pointer, a label element's box: the row's width, less the end enhancer's.</para>
/// <para>Its template is its own, by default (a theme's <c>Template</c> replaces it): a content presenter with the
/// label's background, padding and content alignment. Its text keeps an underscore: a browser's label has no access
/// key in its text.</para>
/// </remarks>
public class FormControlLabel : AvaloniaLabel
{
    static FormControlLabel()
    {
        TemplateProperty.OverrideDefaultValue<FormControlLabel>(new FuncControlTemplate<FormControlLabel>((label, scope) =>
        {
            var content = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                [!ContentPresenter.ContentProperty] = label[!ContentProperty],
                [!ContentPresenter.ContentTemplateProperty] = label[!ContentTemplateProperty],
                [!ContentPresenter.BackgroundProperty] = label[!BackgroundProperty],
                [!ContentPresenter.PaddingProperty] = label[!PaddingProperty],
                [!ContentPresenter.HorizontalContentAlignmentProperty] = label[!HorizontalContentAlignmentProperty],
                [!ContentPresenter.VerticalContentAlignmentProperty] = label[!VerticalContentAlignmentProperty],
            };
            scope.Register(content.Name, content);
            return content;
        }));
    }

    private IPointer? _pressed;

    protected override Type StyleKeyOverride => typeof(FormControlLabel);

    /// <summary>
    /// Nothing on a press but noting it: a browser's label waits for the click (Avalonia's label focuses its target here).
    /// A mouse's main button, a finger or a pen's tip.
    /// </summary>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        _pressed = e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed ? e.Pointer : null;
        // The text under the pointer took it on the press; the label takes it, so that a scroller that takes it for a drag
        // takes it from the label, which hears that (capture lost isn't routed).
        _pressed?.Capture(this);
    }

    /// <summary>The click: the release of the pointer that pressed it, on it.</summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        var pressed = _pressed;
        _pressed = null;
        if (pressed != e.Pointer || Target is null || !new Rect(Bounds.Size).Contains(e.GetPosition(this))) return;
        Target.Focus(NavigationMethod.Pointer);
        e.Handled = true;
    }

    /// <summary>A scroller that takes the pointer for a drag takes the click with it.</summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        if (e.Pointer == _pressed) _pressed = null;
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FormControlLabelAutomationPeer(this);
}

/// <summary>
/// A form control's label to UI Automation: Avalonia's label's (text, named by its text), named also by content that
/// isn't a string, by the text its presenter shows or the content's own name. The element it labels takes its name
/// (<c>AutomationProperties.LabeledBy</c>).
/// </summary>
internal sealed class FormControlLabelAutomationPeer(FormControlLabel owner) : LabelAutomationPeer(owner)
{
    protected override string? GetNameCore()
    {
        if (base.GetNameCore() is { Length: > 0 } name) return name;
        var child = ((FormControlLabel)Owner).Presenter?.Child;
        if (child is TextBlock { Text: { Length: > 0 } text }) return text;
        return child is null ? null : CreatePeerForElement(child).GetName();
    }
}
