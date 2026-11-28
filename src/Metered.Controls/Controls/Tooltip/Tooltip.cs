using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Tooltip</c> and <c>StatefulTooltip</c> (baseui 18.2.0, <c>tooltip/tooltip.js</c>,
/// <c>tooltip/stateful-tooltip.js</c>) in one, as <see cref="Popover"/> is: Base Web's tooltip is its popover with its
/// own defaults (<c>tooltip/default-props.js</c>) and its own look (<c>tooltip/styled-components.js</c>), and so is this
/// one. Everything else is the Popover's: the anchoring, the layers and their dismissal, the safe area, a finger's tap
/// toggling it, Back closing it, its states and its three parts, <c>Body</c>, <c>Arrow</c> and <c>Inner</c>, whose themes
/// are <c>Tooltip.Styled{Part}</c> (docs/controls/Tooltip).
/// </summary>
/// <remarks>
/// <para>Its defaults, Base Web's tooltip's: a hover trigger (a finger's or a pen's tap toggles it, a tap elsewhere closes
/// it, as the Popover's, #123), the accessibility type <see cref="PopoverAccessibilityType.Tooltip"/> (its body a tooltip
/// to UI Automation, its text the anchor's help text; it never takes focus, nor locks it), a popover margin of 0, no
/// auto focus and no return of focus. The 200 ms delays, <see cref="PopoverPlacement.Auto"/>, no arrow, no focus lock and
/// the dismissals are the Popover's, which Base Web's tooltip leaves as they are.</para>
/// <para>Its body fades in and out where it is placed: Base Web's tooltip body's transform is getEndPosition in every
/// state, and only its opacity has a transition, so it never slides from towards its anchor as a popover's does.</para>
/// </remarks>
[TemplatePart(AnchorPresenterPart, typeof(ContentPresenter))]
[TemplatePart(PopupPart, typeof(Popup))]
[TemplatePart(BodyPart, typeof(StyledElement))]
[TemplatePart(ArrowPart, typeof(StyledElement))]
[TemplatePart(InnerPart, typeof(ContentPresenter))]
public class Tooltip : Popover
{
    static Tooltip()
    {
        // tooltip/default-props.js, where it differs from popover/default-props.js and StatefulPopover's.
        AccessibilityTypeProperty.OverrideDefaultValue<Tooltip>(PopoverAccessibilityType.Tooltip);
        TriggerTypeProperty.OverrideDefaultValue<Tooltip>(PopoverTriggerType.Hover);
        PopoverMarginProperty.OverrideDefaultValue<Tooltip>(0);
        AutoFocusProperty.OverrideDefaultValue<Tooltip>(false);
        ReturnFocusProperty.OverrideDefaultValue<Tooltip>(false);
    }

    public Tooltip() : base("Tooltip")
    {
    }

    protected override Type StyleKeyOverride => typeof(Tooltip);

    /// <summary>tooltip/styled-components.js's Body: its transform getEndPosition always, its transition opacity's alone.</summary>
    private protected override bool BodySlides => false;
}
