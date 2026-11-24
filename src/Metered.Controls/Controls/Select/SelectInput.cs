using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Metered.Controls;

/// <summary>
/// The element of a searchable select's <c>Input</c> part: Base Web's <c>AutosizeInput</c> (<c>select/autosize-input.js</c>),
/// a bare text box as wide as its text and 2 px (its sizer's <c>scrollWidth + 2</c>), at most its row's width. Its template
/// is the select theme's (<c>Select.InputTemplate</c>); its look its part theme's.
/// </summary>
/// <remarks>To UI Automation it is Base Web's <c>role="combobox"</c>: a combo box that expands and collapses its select.</remarks>
public class SelectInput : TextBox
{
    /// <summary>The sizer's 2 px past the text (autosize-input.js's <c>scrollWidth + 2</c>).</summary>
    internal const double Extra = 2;

    /// <summary>The select it is the input of.</summary>
    internal Select? Owner { get; set; }

    static SelectInput()
    {
        AffectsMeasure<SelectInput>(TextProperty, FontSizeProperty, FontFamilyProperty, FontWeightProperty);
    }

    /// <summary>The text's width, as the sizer's <c>white-space: pre</c> lays it out, rounded up as an element's <c>scrollWidth</c> is.</summary>
    internal double TextWidth
    {
        get
        {
            var text = Text ?? "";
            if (text.Length == 0) return 0;
            using var layout = new TextLayout(text, new Typeface(FontFamily, FontStyle, FontWeight, FontStretch), FontSize, null);
            return Math.Ceiling(layout.WidthIncludingTrailingWhitespace);
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var width = Math.Min(TextWidth + Extra, availableSize.Width);
        var size = base.MeasureOverride(new Size(width, availableSize.Height));
        return new Size(width, size.Height);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new SelectComboAutomationPeer(this, () => Owner);
}

/// <summary>
/// The element of a select's <c>InputContainer</c> part, Base Web's <c>StyledInputContainer</c>: the input's box. A select
/// that isn't searchable focuses it itself (Base Web's <c>tabIndex={0}</c> on it, its hidden input at no width), and it is
/// the combo box to UI Automation then.
/// </summary>
public class SelectInputContainer : Border
{
    /// <summary>The select it is the input's box of.</summary>
    internal Select? Owner { get; set; }

    protected override AutomationPeer OnCreateAutomationPeer() => new SelectComboAutomationPeer(this, () => Owner);
}

/// <summary>
/// A select's focusable element to UI Automation: Base Web's <c>role="combobox"</c> with <c>aria-expanded</c>, named by its
/// <c>aria-label</c>, <c>Selected …. </c> and the select's name; expanded and collapsed as its dropdown opens and closes.
/// </summary>
internal sealed class SelectComboAutomationPeer(Control owner, Func<Select?> select) : ControlAutomationPeer(owner), IExpandCollapseProvider
{
    // The input's box is the combo box only while it takes the focus (a select that isn't searchable); else a plain box,
    // left out of the control view, as the input is while it is hidden.
    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner is SelectInputContainer { Focusable: false } ? AutomationControlType.Group : AutomationControlType.ComboBox;

    protected override string? GetNameCore() => select()?.AccessibleName ?? base.GetNameCore();

    protected override bool IsControlElementCore() => Owner.IsVisible && Owner is not SelectInputContainer { Focusable: false };

    protected override bool IsContentElementCore() => IsControlElementCore();

    public ExpandCollapseState ExpandCollapseState => select()?.IsOpen == true ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;

    public bool ShowsMenu => true;

    public void Expand() => select()?.SetDropdownOpen(true);

    public void Collapse() => select()?.SetDropdownOpen(false);

    /// <summary>The select's open state changed: said to UI Automation.</summary>
    internal void Changed(bool open) =>
        RaisePropertyChangedEvent(ExpandCollapsePatternIdentifiers.ExpandCollapseStateProperty,
            open ? ExpandCollapseState.Collapsed : ExpandCollapseState.Expanded,
            open ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed);
}
