using Avalonia;
using Avalonia.Data;

namespace Metered.Controls;

/// <summary>
/// The values a group holds on one of its children, above the child's own, which stay beneath them: Base Web's
/// <c>cloneElement</c> and group contexts, which replace what a child draws and leave the child's props unchanged
/// (#70, #80). The values are held at Avalonia's <c>Animation</c> priority, the one above a local value; let go, the
/// child's own values are its again, local, bound or styled, including any set on it while it was held.
/// </summary>
internal class Hold(AvaloniaObject target)
{
    private readonly Dictionary<AvaloniaProperty, (object? Value, IDisposable? Undo)> _held = [];

    /// <summary>Holds <paramref name="value"/> over the child's own.</summary>
    public void Set<T>(StyledProperty<T> property, T value)
    {
        if (_held.TryGetValue(property, out var held) && Equals(held.Value, value)) return;
        var undo = target.SetValue(property, value, BindingPriority.Animation);
        // Let go of the old value after the new one is held, so that the child's own doesn't show between.
        held.Undo?.Dispose();
        _held[property] = (value, undo);
    }

    /// <summary>The child's own value shows again.</summary>
    public void Let(AvaloniaProperty property)
    {
        if (_held.Remove(property, out var held)) held.Undo?.Dispose();
    }

    public bool Holds(AvaloniaProperty property) => _held.ContainsKey(property);

    public void LetAll()
    {
        foreach (var property in _held.Keys.ToList()) Let(property);
    }
}
