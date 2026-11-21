namespace Metered.Controls;

/// <summary>Base Web's input <c>SIZE</c> (<c>input/constants.js</c>): the padding, the font and the icons' sizes.</summary>
public enum InputSize
{
    Mini,
    Compact,

    /// <summary>Base Web's default.</summary>
    Default,

    Large,
}

/// <summary>
/// Base Web's input <c>ADJOINED</c>: which sides have an enhancer. <c>input.js</c> derives it from <c>startEnhancer</c>
/// and <c>endEnhancer</c> (<c>getAdjoinedProp</c>), overwriting the prop, so <see cref="Input.Adjoined"/> is read-only.
/// </summary>
public enum InputAdjoined
{
    None,

    /// <summary>A start enhancer.</summary>
    Left,

    /// <summary>An end enhancer.</summary>
    Right,

    /// <summary>Both.</summary>
    Both,
}

/// <summary>
/// Base Web's input <c>type</c>, as far as Base Web's input reads it: <c>"password"</c> masks the text and adds the mask
/// toggle (<c>base-input.js</c>); every other type is the browser's, here the app's (Avalonia's <c>TextInputOptions</c>
/// for the soft keyboard).
/// </summary>
public enum InputType
{
    /// <summary>Base Web's default, <c>"text"</c>.</summary>
    Text,

    /// <summary><c>"password"</c>.</summary>
    Password,
}
