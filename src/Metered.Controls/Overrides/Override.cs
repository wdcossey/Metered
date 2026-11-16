using Avalonia.Collections;
using Avalonia.Styling;

namespace Metered.Controls;

/// <summary>
/// One entry of a control's <c>Overrides</c>: Base Web's <c>overrides={{ Part: { style, props, component } }}</c>
/// for one named part of the control's template.
/// </summary>
/// <remarks>
/// Each value is a <see cref="ControlTheme"/> for the part's own type. <see cref="Component"/>
/// replaces the part's theme; <see cref="Style"/> and then <see cref="Props"/> are merged over it,
/// as Base Web deep-merges a style and props over the default. A theme given as <see cref="Style"/>
/// or <see cref="Props"/> with no <c>BasedOn</c> is based on what it is merged over when it is
/// first applied; one with a <c>BasedOn</c> of its own is used as it is (ADR 0001).
/// </remarks>
public sealed class Override
{
    /// <summary>The part's name: Base Web's override key (<c>BaseButton</c>, <c>StartEnhancer</c>, <c>Root</c>).</summary>
    public string Part { get; set; } = "";

    /// <summary>Base Web's <c>component</c>: a theme that replaces the part's own.</summary>
    public ControlTheme? Component { get; set; }

    /// <summary>Base Web's <c>style</c>: setters, and state styles (<c>^:hover</c>), merged over the part's theme.</summary>
    public ControlTheme? Style { get; set; }

    /// <summary>Base Web's <c>props</c>: setters merged over <see cref="Style"/>. A part that is a Metered control takes nested <c>Overrides</c> here.</summary>
    public ControlTheme? Props { get; set; }
}

/// <summary>A control's overrides: Base Web's <c>overrides</c> map, an entry per part.</summary>
public sealed class OverrideCollection : AvaloniaList<Override>
{
}
