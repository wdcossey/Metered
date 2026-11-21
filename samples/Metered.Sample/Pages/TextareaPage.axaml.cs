using Avalonia.Controls;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Textarea, as its documentation's examples: basic, its value said under it; the states; the sizes; one the
/// user can resize; its scenario's clearable one; and a long text that scrolls inside its three rows.
/// </summary>
[SamplePage("Textarea", Order = 85)]
public partial class TextareaPage : UserControl
{
    public TextareaPage()
    {
        InitializeComponent();
        Long.Text = string.Join("\n", Enumerable.Range(1, 12).Select(i => $"Line {i}: a long text scrolls inside the textarea."));
        Say();
        Basic.TextChanged += (_, _) => Say();
    }

    private void Say() => State.Text = "Value: " + Basic.Text;
}
