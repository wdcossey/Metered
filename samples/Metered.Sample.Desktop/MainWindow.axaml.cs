using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;

namespace Metered.Sample.Desktop;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Radio.Selected = [0];

        Variant.Click += (_, _) =>
        {
            var dark = ActualThemeVariant == ThemeVariant.Dark;
            RequestedThemeVariant = dark ? ThemeVariant.Light : ThemeVariant.Dark;
            Variant.Content = dark ? "Dark" : "Light";
        };

        Loading.Click += (_, _) =>
        {
            Loading.IsLoading = true;
            DispatcherTimer.RunOnce(() => Loading.IsLoading = false, TimeSpan.FromSeconds(2));
        };

        Checks.Click += (_, e) =>
            Clicked.Text = $"Clicked: {e.Index}; selected: {string.Join(", ", Checks.Selected ?? [])}";
    }
}
