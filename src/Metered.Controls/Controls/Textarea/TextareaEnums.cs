namespace Metered.Controls;

/// <summary>
/// CSS's <c>resize</c>, which Base Web's textarea takes as its <c>resize</c> prop (<c>Properties['resize']</c>) and gives
/// its <c>&lt;textarea&gt;</c> (<c>StyledTextarea</c>: <c>$resize || 'none'</c>): which way a user can drag its size. Left
/// unset (<see cref="Textarea.Resize"/> null) is Base Web's default, which isn't <see cref="None"/>: any value set makes
/// the root as wide as the textarea (<c>StyledTextAreaRoot</c>: <c>$resize ? 'fit-content' : '100%'</c>).
/// </summary>
public enum TextareaResize
{
    /// <summary>CSS's <c>none</c>: no grip.</summary>
    None,

    /// <summary>CSS's <c>both</c>: wider or narrower, taller or shorter.</summary>
    Both,

    /// <summary>CSS's <c>horizontal</c>: its width.</summary>
    Horizontal,

    /// <summary>CSS's <c>vertical</c>: its height.</summary>
    Vertical,
}
