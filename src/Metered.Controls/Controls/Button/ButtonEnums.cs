namespace Metered.Controls;

/// <summary>Base Web's <c>KIND</c>, and <c>BUTTON_GROUP_EXCLUSIVE_KINDS.outline</c>.</summary>
public enum ButtonKind
{
    Primary,
    Secondary,
    Tertiary,
    DangerPrimary,
    DangerSecondary,
    DangerTertiary,

    /// <summary>Base Web's <c>BUTTON_GROUP_EXCLUSIVE_KINDS.outline</c>, meant for a <see cref="ButtonGroup"/>.</summary>
    Outline,
}

/// <summary>Base Web's <c>SIZE</c>: four sizes under seven names, the older and the newer.</summary>
public enum ButtonSize
{
    /// <summary>The same as <see cref="XSmall"/>.</summary>
    Mini,

    /// <summary>The same as <see cref="Medium"/>.</summary>
    Default,

    /// <summary>The same as <see cref="Small"/>.</summary>
    Compact,
    Large,
    XSmall,
    Small,
    Medium,
}

/// <summary>Base Web's <c>SHAPE</c>.</summary>
public enum ButtonShape
{
    /// <summary>The same as <see cref="Rectangular"/>.</summary>
    Default,
    Rectangular,

    /// <summary>The same as <see cref="Pill"/>.</summary>
    Rounded,
    Pill,

    /// <summary>Deprecated in Base Web: use <see cref="Circle"/> or <see cref="Square"/> for an icon-only button.</summary>
    Round,
    Circle,
    Square,
}

/// <summary>Base Web's <c>WIDTH_TYPE</c>.</summary>
public enum ButtonWidthType
{
    Hug,
    Fill,
}

/// <summary>Base Web's <c>MIN_HIT_AREA</c>.</summary>
public enum ButtonMinHitArea
{
    Click,

    /// <summary>Takes touches up to 48 tall, centred on the button, however small it is.</summary>
    Tap,
}
