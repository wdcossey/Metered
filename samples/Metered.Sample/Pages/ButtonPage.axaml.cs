using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Metered.Sample.Pages;

/// <summary>Base Web's Button: the seven kinds and disabled, the four sizes, the shapes, the states and the enhancers.</summary>
[SamplePage("Button", Order = 1)]
public partial class ButtonPage : UserControl
{
    /// <summary>How long a click on "Click to load" shows the spinner.</summary>
    internal static readonly TimeSpan LoadingFor = TimeSpan.FromSeconds(2);

    public ButtonPage() => InitializeComponent();

    private void OnLoad(object? sender, RoutedEventArgs e) => Load((Metered.Controls.Button)sender!);

    /// <summary>Shows <paramref name="button"/>'s spinner for <see cref="LoadingFor"/>.</summary>
    internal static void Load(Metered.Controls.Button button)
    {
        button.IsLoading = true;
        DispatcherTimer.RunOnce(() => button.IsLoading = false, LoadingFor);
    }
}
