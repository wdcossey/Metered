using Avalonia.Controls;
using Metered.Controls;
using MeteredButton = Metered.Controls.Button;

namespace Metered.Sample.Pages;

/// <summary>
/// Base Web's Popover, as its documentation's examples: always open (stateless.tsx), opened by a click
/// (stateful-click.tsx) and with an arrow (with-arrow.tsx), by a hover (stateful-hover.tsx), closed from its content
/// (dismiss.tsx), each placement (placements.tsx) and its overrides (overrides.tsx).
/// </summary>
[SamplePage("Popover", Order = 106)]
public partial class PopoverPage : UserControl
{
    public PopoverPage()
    {
        InitializeComponent();

        // dismiss.tsx: the content is a function of a close callback, which its button calls.
        Dismiss.Content = new Func<Action, object?>(close =>
        {
            var dismiss = new MeteredButton { Content = "Dismiss" };
            dismiss.Click += (_, _) => close();
            return new StackPanel
            {
                Margin = new Avalonia.Thickness(12),
                MaxWidth = 300,
                Children =
                {
                    new TextBlock
                    {
                        Classes = { "paragraph" },
                        Margin = new Avalonia.Thickness(0, 0, 0, 10),
                        Text = "content render prop is passed a close() callback so it you can manually trigger popover close from within",
                    },
                    dismiss,
                },
            };
        });

        // placements.tsx: Object.values(PLACEMENT), auto first.
        foreach (var placement in Enum.GetValues<PopoverPlacement>())
        {
            var name = char.ToLowerInvariant(placement.ToString()[0]) + placement.ToString()[1..];
            Placements.Children.Add(new Popover
            {
                Placement = placement,
                AccessibilityType = PopoverAccessibilityType.Tooltip,
                Content = new TextBlock { Classes = { "paragraph" }, Margin = new Avalonia.Thickness(8), Text = $"PLACEMENT.{name}" },
                Anchor = new MeteredButton { Content = name },
            });
        }
    }
}
