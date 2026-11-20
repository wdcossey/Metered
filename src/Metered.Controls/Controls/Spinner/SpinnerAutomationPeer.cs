using Avalonia.Automation.Peers;

namespace Metered.Controls;

/// <summary>
/// The Spinner's automation peer: Base Web's <c>role="progressbar"</c> and <c>aria-label="loading"</c>, which an app's
/// own name replaces (Base Web spreads its props after its own). A control and a content element, so UI Automation's
/// views include it. <c>aria-busy</c> has no Avalonia equivalent; no value is reported, as Base Web's has none.
/// </summary>
public class SpinnerAutomationPeer : ControlAutomationPeer
{
    /// <summary>Base Web's <c>aria-label</c>.</summary>
    public const string DefaultName = "loading";

    public SpinnerAutomationPeer(Spinner owner)
        : base(owner)
    {
    }

    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ProgressBar;

    protected override string? GetNameCore() => base.GetNameCore() is { Length: > 0 } name ? name : DefaultName;

    protected override bool IsControlElementCore() => true;

    protected override bool IsContentElementCore() => true;
}
