namespace Metered.Controls;

/// <summary>Base Web's progress bar <c>SIZE</c>: the bars' thickness and the label's type.</summary>
public enum ProgressBarSize
{
    /// <summary>2 px bars, <c>LabelSmall</c>.</summary>
    Small,

    /// <summary>4 px bars, <c>LabelMedium</c> (Base Web's default).</summary>
    Medium,

    /// <summary>8 px bars, <c>LabelLarge</c>.</summary>
    Large,
}

/// <summary>
/// Base Web's progress bar <c>INTENT</c>: the fill's colour. A bar with none (<c>Intent</c> null, Base Web's prop left
/// out) draws <c>backgroundAccent</c>, Base Web's colour from before intents.
/// </summary>
public enum ProgressBarIntent
{
    /// <summary><c>contentAccent</c>.</summary>
    Default,

    /// <summary><c>contentPositive</c>.</summary>
    Positive,

    /// <summary><c>contentWarning</c>.</summary>
    Warning,

    /// <summary><c>contentNegative</c>.</summary>
    Negative,

    /// <summary><c>brandBorderAccessible</c>.</summary>
    Brand,
}
