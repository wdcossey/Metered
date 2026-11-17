namespace Metered.Controls;

/// <summary>Base Web's radio <c>ALIGN</c>: how the radios of a group are laid out, and how a radio's text is held.</summary>
public enum RadioAlign
{
    /// <summary>A column (Base Web's default).</summary>
    Vertical,

    /// <summary>A row that wraps; each label and description at most 240 wide and three lines long.</summary>
    Horizontal,
}

/// <summary>Base Web's radio <c>LABEL_PLACEMENT</c>: where the label sits beside the mark.</summary>
public enum RadioLabelPlacement
{
    Top,

    /// <summary>Base Web's default.</summary>
    Right,

    Bottom,

    Left,
}

/// <summary>How a <see cref="RadioRootPanel"/> aligns its children across its direction: CSS's <c>align-items</c>.</summary>
public enum RadioRootAlignment
{
    /// <summary><c>flex-start</c>.</summary>
    Start,

    /// <summary><c>center</c>.</summary>
    Center,
}
