using System.Reflection;
using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using AvaloniaLabel = Avalonia.Controls.Label;

namespace Metered.Controls;

/// <summary>
/// The element of a form control's <c>Label</c> part, Base Web's <c>&lt;label htmlFor&gt;</c>: an Avalonia
/// <see cref="AvaloniaLabel"/> whose <see cref="AvaloniaLabel.Target"/> is the element it labels, activated when the label
/// is clicked or tapped, as a browser activates a label on <c>click</c>: a press on it and a release on it, the pointer
/// not taken from it in between. It focuses the target, then clicks it: a checkbox or a switch toggles, a radio is
/// checked, a button is clicked.
/// </summary>
/// <remarks>
/// <para>Avalonia's label focuses its target on a press. A browser's doesn't, and on a phone that matters: a finger that
/// lands on a label to scroll the page would focus the text box under it and open the keyboard. Here a press does
/// nothing; the release does, unless a scroller has taken the pointer for a drag (its capture, which the label loses),
/// or the pointer is let go elsewhere. Each click of a double click is one, as a browser's are.</para>
/// <para>The click is the target's own, through its automation peer: its toggle (Avalonia's toggle button, Metered's
/// Checkbox, Radio and Switch), else its invoke (a button), each of which runs the control's click, its <c>Click</c> event
/// and its <c>Command</c>, as a browser's label runs the labelled control's activation. A target with neither (a text box)
/// is only focused, and its soft keyboard asked for, as a tap on it asks (#124); a disabled one is left alone. A press on
/// a control inside the label's content (a link, a button) is that control's: it handles the press, which the label never
/// sees.</para>
/// <para>Its hit area is its box widened over the label row's top and bottom margins (Metered's, for a finger: Base Web's
/// label box is the row's 16 px, 20 with an end enhancer; here 32, or 36), the row's width less the end enhancer's.
/// Nothing moves: the margins stay empty space, and the child, after the label in the tree, keeps a press where its own
/// margin pulls it over them.</para>
/// <para>Its template is its own, by default (a theme's <c>Template</c> replaces it): a content presenter with the
/// label's background, padding and content alignment. Its text keeps an underscore: a browser's label has no access
/// key in its text.</para>
/// </remarks>
public class FormControlLabel : AvaloniaLabel, ICustomHitTest
{
    static FormControlLabel()
    {
        // A templated control clips to its bounds, hit tests too: its hit area is wider. Its content is still clipped to
        // its box, by its presenter.
        ClipToBoundsProperty.OverrideDefaultValue<FormControlLabel>(false);
        TemplateProperty.OverrideDefaultValue<FormControlLabel>(new FuncControlTemplate<FormControlLabel>((label, scope) =>
        {
            var content = new ContentPresenter
            {
                Name = "PART_ContentPresenter",
                ClipToBounds = true,
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
        // Another button pressed meanwhile doesn't cancel the main one's click, as a browser's; nor another finger.
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed) return;
        if (_pressed is not null && _pressed != e.Pointer) return;
        _pressed = e.Pointer;
        // The text under the pointer took it on the press; the label takes it, so that a scroller that takes it for a drag
        // takes it from the label, which hears that (capture lost isn't routed).
        _pressed.Capture(this);
    }

    /// <summary>
    /// The click: the release of the main button (a mouse's left, a finger, a pen's tip) of the pointer that pressed it,
    /// on it: where its hit test takes the pointer, not where the child, pulled over the row's margin, is.
    /// </summary>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        // Another finger's is not its click.
        if (e.Pointer != _pressed) return;
        // Another button let go meanwhile isn't the click, as a browser's: the main one's release is. Avalonia's mouse lets
        // the capture go on any button's release; the click goes on.
        if (e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased)
        {
            _otherReleased = e.Pointer.Captured == this;
            return;
        }

        _pressed = null;
        if (Target is not { } target || !HitTest(e.GetPosition(this))) return;
        e.Handled = true;
        Activate(target);
    }

    /// <summary>
    /// A browser's label activation: the labelled control focused, then, if it is a button (Avalonia's, so a toggle button,
    /// a checkbox, a radio or a switch too, as a browser's label clicks a button or an input it labels), clicked by its
    /// automation peer's toggle or invoke, which run its own click; nothing for a disabled one.
    /// </summary>
    internal static void Activate(IInputElement target)
    {
        if (!target.IsEffectivelyEnabled) return;
        target.Focus(NavigationMethod.Pointer);
        ShowInputPane(target);
        if (target is not Avalonia.Controls.Button control) return;
        var peer = ControlAutomationPeer.CreatePeerForElement(control);
        if (peer.GetProvider<IToggleProvider>() is { } toggle) toggle.Toggle();
        else if (peer.GetProvider<IInvokeProvider>() is { } invoke) invoke.Invoke();
    }

    // Protected on Avalonia's public TextInputMethodClient (12.1.4): FormControlTests' keyboard tests go red if an upgrade
    // renames it.
    private static readonly MethodInfo? RequestInputPane = typeof(TextInputMethodClient).GetMethod(
        "RaiseInputPaneActivationRequested", BindingFlags.Instance | BindingFlags.NonPublic, Type.EmptyTypes);

    /// <summary>
    /// The soft keyboard asked for, for the text box the label has focused, as a tap on the text box asks for it (#124).
    /// Focusing a text box shows the keyboard, but a focused one isn't focused again, so once the keyboard has been put away
    /// (Android's Back) a label's tap would leave it closed, where a browser's opens it again. Avalonia's text box asks on
    /// its own tap (its input method client's <c>InputPaneActivationRequested</c>, which Android's input method answers
    /// with <c>ShowSoftInput</c>); the label asks that client, the one Avalonia's input method manager takes for the
    /// focused element, asked for as the manager asks (raised on the focused element), through the client's own protected
    /// raiser (no public member asks). The focused element is the target or inside it (a searchable Select's input, a text
    /// box in a control <c>HtmlFor</c> names); nothing where the focus is elsewhere, or the focused element has no client
    /// (a read-only text box, a button). Android's input method listens only to the client the manager has given it, which
    /// is none where the focused element's input method is turned off.
    /// </summary>
    internal static void ShowInputPane(IInputElement target)
    {
        if (target is not Visual visual || TopLevel.GetTopLevel(visual)?.FocusManager?.GetFocusedElement() is not InputElement focused) return;
        if (focused != visual && !visual.IsVisualAncestorOf(focused)) return;
        var request = new TextInputMethodClientRequestedEventArgs { RoutedEvent = TextInputMethodClientRequestedEvent };
        focused.RaiseEvent(request);
        if (request.Client is { } client) RequestInputPane?.Invoke(client, null);
    }

    /// <summary>
    /// Its box, from the label row's top margin's edge to its bottom margin's edge: the room the row's margins keep above
    /// and below it, in its own coordinates. Only in a form control's label row (a template that puts it elsewhere keeps
    /// its box).
    /// </summary>
    internal Rect HitArea
    {
        get
        {
            var box = new Rect(Bounds.Size);
            if (this.GetVisualParent() is not FormControlLabelContainer row) return box;
            // The margins as laid out: a neighbour's own margin collapsed with them (an override's negative margin) leaves
            // less room, which is the neighbour's.
            var (before, after) = Neighbours(row);
            var top = Math.Max(0, Math.Min(row.Margin.Top, row.Bounds.Top - before));
            var bottom = Math.Max(0, Math.Min(row.Margin.Bottom, after - row.Bounds.Bottom));
            var above = top + Math.Max(0, Bounds.Top);
            var below = bottom + Math.Max(0, row.Bounds.Height - Bounds.Bottom);
            return new Rect(0, -above, Bounds.Width, Bounds.Height + above + below);
        }
    }

    /// <summary>Where the row's neighbours in its flow end above it and start below it (the flow's edges without one).</summary>
    private static (double Before, double After) Neighbours(Layoutable row)
    {
        if (row.GetVisualParent() is not Panel flow) return (double.NegativeInfinity, double.PositiveInfinity);
        var shown = flow.Children.Where(c => c.IsVisible).ToList();
        var i = shown.IndexOf((Control)row);
        var before = i > 0 ? shown[i - 1].Bounds.Bottom : 0;
        var after = i >= 0 && i < shown.Count - 1 ? shown[i + 1].Bounds.Top : flow.Bounds.Height;
        return (before, after);
    }

    private bool _probing;

    /// <summary>
    /// Avalonia's compositor asks it for any point, outside its bounds too: its box, and its row's margins where they are
    /// empty space. A point there that anything else takes (a control above whose tap band reaches over the margin, the
    /// child pulled over it by its own margin, an overlay), other than what the label is inside, is that element's.
    /// </summary>
    public bool HitTest(Point point)
    {
        if (new Rect(Bounds.Size).Contains(point)) return true;
        if (_probing || !HitArea.Contains(point) || TopLevel.GetTopLevel(this) is not Visual root) return false;
        if (this.TranslatePoint(point, root) is not { } at) return false;
        // What else input would hit there, the label left out (it answers false while it asks). Something that reaches into
        // the margin from beside it (a tap band, a child pulled over by its margin) takes the point; something under the
        // whole label (a card's background) doesn't. Something in front of the label is hit first whatever it answers.
        _probing = true;
        try
        {
            var box = new Rect(at - point, Bounds.Size).Deflate(0.5);
            return !root.GetVisualsAt(at, v => v.IsVisible && (v is not InputElement e || (e.IsHitTestVisible && e.IsEffectivelyEnabled)))
                .Any(v => !v.IsVisualAncestorOf(this) && !this.IsVisualAncestorOf(v)
                    && !(v.TranslatePoint(default, root) is { } origin && new Rect(origin, v.Bounds.Size).Contains(box)));
        }
        finally
        {
            _probing = false;
        }
    }

    /// <summary>A label taken out of the tree mid-click lets the click go.</summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        StopWatching();
        _pressed = null;
        _otherReleased = false;
    }

    /// <summary>
    /// A scroller that takes the pointer for a drag takes the click with it. Another button's release lets the capture go
    /// too, the main button still held: the click goes on, and the main button's release ends it wherever it is.
    /// </summary>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        var other = _otherReleased;
        _otherReleased = false;
        if (e.Pointer != _pressed) return;
        if (other && TopLevel.GetTopLevel(this) is { } top)
        {
            if (_watched is null)
            {
                _watched = top;
                top.AddHandler(PointerReleasedEvent, OnAnyRelease, RoutingStrategies.Bubble, handledEventsToo: true);
            }

            return;
        }

        _pressed = null;
    }

    private bool _otherReleased;
    private TopLevel? _watched;

    /// <summary>The main button's release anywhere, after the label has had it: the click is over.</summary>
    private void OnAnyRelease(object? sender, PointerReleasedEventArgs e)
    {
        if (e.Pointer != _pressed && _pressed is not null) return;
        if (_pressed is not null && e.GetCurrentPoint(this).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonReleased) return;
        _pressed = null;
        StopWatching();
    }

    private void StopWatching()
    {
        _watched?.RemoveHandler(PointerReleasedEvent, OnAnyRelease);
        _watched = null;
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
