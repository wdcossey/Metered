using System.Globalization;
using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's ProgressBar, as its documentation's examples: basic; a negative fill by override (negative.tsx); the
/// intents; its label, and a custom one (custom-label.tsx: megabytes of 1000); the sizes; the stepped bar with its
/// Previous and Next buttons (steps.tsx); and infinite. Its examples tick the value each second; here the values hold
/// still, so the page's pictures don't change from run to run.
/// </summary>
[SamplePage("ProgressBar", Order = 53)]
public partial class ProgressBarPage : UserControl
{
    /// <summary>custom-label.tsx's MAX_MB_VALUE.</summary>
    private const double MaxMb = 1000;

    /// <summary>steps.tsx: 100 / STEPS, for its 5 steps.</summary>
    private const double StepSize = 100.0 / 5;

    public ProgressBarPage()
    {
        InitializeComponent();
        Custom.GetProgressLabel = (value, _, _) =>
            $"{(MaxMb / 100 * value).ToString(CultureInfo.InvariantCulture)}mb out of {MaxMb.ToString(CultureInfo.InvariantCulture)}mb downloaded";
        Previous.Click += (_, _) => Steps.Value -= StepSize;
        Next.Click += (_, _) => Steps.Value += StepSize;
        Steps.ValueChanged += (_, _) => UpdateButtons();
        UpdateButtons();
    }

    /// <summary>steps.tsx: Previous off at 0 and at 100, Next off at 100, and "Complete" from 80.</summary>
    private void UpdateButtons()
    {
        Previous.IsEnabled = Steps.Value != 0 && Steps.Value != 100;
        Next.IsEnabled = Steps.Value != 100;
        Next.Content = Steps.Value >= 80 ? "Complete" : "Next Step";
    }
}
