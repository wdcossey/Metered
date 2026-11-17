namespace Metered.Controls;

/// <summary>Base Web's switch <c>SIZE</c>: the track, the knob and the label's type.</summary>
public enum SwitchSize
{
    /// <summary>A 52 × 32 track, <c>ParagraphMedium</c> (Base Web's default).</summary>
    Default,

    /// <summary>A 40 × 24 track, <c>ParagraphSmall</c>.</summary>
    Small,
}

/// <summary>Base Web's switch <c>LABEL_PLACEMENT</c>: which side of the track the label sits.</summary>
public enum SwitchLabelPlacement
{
    Left,

    /// <summary>Base Web's default.</summary>
    Right,
}
