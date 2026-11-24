using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace Metered.Controls;

/// <summary>
/// The element of a select's <c>OptionContent</c> part, Base Web's <c>StyledOptionContent</c>: an option's label in the
/// dropdown, which <c>dropdown.js</c> gives the menu's option as its <c>getItemLabel</c>. Its states are dropdown.js's
/// <c>$selected</c>, <c>$disabled</c> and <c>$isHighlighted</c> (<c>:is-selected</c>, <c>:is-disabled</c>,
/// <c>:is-highlighted</c>): the highlight its option's, followed as the menu moves it.
/// </summary>
[PseudoClasses(":is-selected", ":is-disabled", ":is-highlighted")]
public class SelectOptionContent : ContentPresenter
{
    private MenuOptionBase? _option;
    private IDisposable? _watch;

    /// <summary>The option it labels.</summary>
    internal object? Option { get; init; }

    /// <summary>Its label for a state: the select's getOptionLabel, called again as the state changes, as Base Web renders it again.</summary>
    internal Func<SelectOptionState, object?>? Label { get; init; }

    /// <summary>Base Web's <c>$selected</c>: the option is among the value.</summary>
    internal bool Selected { get; private set; }

    /// <summary>Base Web's <c>$disabled</c>.</summary>
    internal bool Disabled { get; private set; }

    /// <summary>Base Web's <c>$isHighlighted</c>: its option's.</summary>
    internal bool IsHighlighted { get; private set; }

    /// <summary>The text it shows, for UI Automation and type-ahead: its label's.</summary>
    internal string? Text { get; private set; }

    protected override Type StyleKeyOverride => typeof(ContentPresenter);

    internal void SetState(bool selected, bool disabled)
    {
        Selected = selected;
        Disabled = disabled;
        Update();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _option = this.FindAncestorOfType<MenuOptionBase>();
        _watch?.Dispose();
        _watch = _option?.GetObservable(MenuOptionBase.IsHighlightedProperty).Subscribe(new Watcher(this));
        Update();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _watch?.Dispose();
        _watch = null;
        _option = null;
    }

    private void Update()
    {
        IsHighlighted = _option?.IsHighlighted ?? false;
        PseudoClasses.Set(":is-selected", Selected);
        PseudoClasses.Set(":is-disabled", Disabled);
        PseudoClasses.Set(":is-highlighted", IsHighlighted);
        if (Label is { } label)
        {
            var content = label(new SelectOptionState(Selected, Disabled, IsHighlighted));
            if (!Equals(content, Content)) Content = content;
            Text = content as string ?? content?.ToString();
        }
    }

    public override string ToString() => Text ?? "";

    private sealed class Watcher(SelectOptionContent owner) : IObserver<bool>
    {
        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
        }

        public void OnNext(bool value) => owner.Update();
    }
}
