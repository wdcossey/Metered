using System.Text.RegularExpressions;
using Avalonia.Controls;
using Metered.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's FormControl, as its documentation's examples (an input and a textarea in one, the email validation, the
/// states) and its scenarios: a disabled label, the counters (on, with a maximum, round a textarea, given its own
/// length and maximum), a label for another control (htmlFor), the input's form control states (a caption function
/// under a positive and an error message), and a checkbox and a radio group in one.
/// </summary>
[SamplePage("FormControl", Order = 88)]
public partial class FormControlPage : UserControl
{
    /// <summary>
    /// The validation example's check. Base Web's uses the email-validator package; the sample has no dependency for it,
    /// so a plain check of a name, an @ and a domain with a dot.
    /// </summary>
    private static readonly Regex Address = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.CultureInvariant);

    private bool _visited;

    public FormControlPage()
    {
        InitializeComponent();

        // input-form-control-states.scenario.tsx: the caption is a function, the messages win over it.
        Success.Caption = new Func<FormControlState, object?>(_ => "caption");
        Failure.Caption = new Func<FormControlState, object?>(_ => "caption");

        // The validation example: an error once the field has been left with an invalid address, until it is valid.
        Email.LostFocus += (_, _) =>
        {
            _visited = true;
            Validate();
        };
        Email.TextChanged += (_, _) => Validate();

        // form-control-with-counter.scenario.tsx's "Override counter": the length given, not read from the input.
        CounterOverrideInput.TextChanged += (_, _) => CounterOverride.Counter!.Length = CounterOverrideInput.Text?.Length ?? 0;
    }

    private void Validate()
    {
        var error = !Address.IsMatch(Email.Text ?? "") && _visited;
        Validation.Error = error ? "Please input a valid email address" : null;
        Email.Error = error;
    }
}
