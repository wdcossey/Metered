namespace Metered.Controls;

/// <summary>
/// Base Web's popover <c>PLACEMENT</c> (<c>popover/constants.js</c>, the same as <c>layer/constants.js</c>'s
/// <c>TETHER_PLACEMENT</c>): where the body goes against its anchor. The first word is the side, the second the end of
/// that side it lines up with; <see cref="Auto"/> is the side with the most room (popper.js's <c>auto</c>).
/// </summary>
public enum PopoverPlacement
{
    /// <summary>Base Web's default.</summary>
    Auto,
    TopLeft,
    Top,
    TopRight,
    RightTop,
    Right,
    RightBottom,
    BottomRight,
    Bottom,
    BottomLeft,
    LeftBottom,
    Left,
    LeftTop,
}

/// <summary>Base Web's <c>TRIGGER_TYPE</c>: what opens a popover that keeps its own state.</summary>
public enum PopoverTriggerType
{
    /// <summary>Base Web's default: a click or a tap on the anchor toggles it.</summary>
    Click,

    /// <summary>A mouse over the anchor opens it, after <see cref="Popover.MouseEnterDelay"/>; a finger never hovers.</summary>
    Hover,
}

/// <summary>Base Web's <c>ACCESSIBILITY_TYPE</c>: what the popover is to assistive technology.</summary>
public enum PopoverAccessibilityType
{
    /// <summary>Nothing is added.</summary>
    None,

    /// <summary>Base Web's default: the anchor has a popup, expanded while open.</summary>
    Menu,

    /// <summary>The body is a tooltip that describes the anchor; it doesn't take focus.</summary>
    Tooltip,
}
