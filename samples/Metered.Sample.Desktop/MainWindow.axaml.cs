using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Themes;

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

        Loading.Click += (_, _) => Load(Loading);
        BrandedLoading.Click += (_, _) => Load(BrandedLoading);

        Checks.Click += (_, e) =>
            Clicked.Text = $"Clicked: {e.Index}; selected: {string.Join(", ", Checks.Selected ?? [])}";

        // Base Web's createTheme({ colors: { primaryA, primaryB, accent } }), and createDarkTheme's: every
        // semantic and component colour follows the foundation (docs/theming.md).
        ThemeProvider.SetTheme(Branded, BaseTheme.CreateTheme(new Dictionary<string, object>
        {
            ["colors.primaryA"] = Color.Parse("#1F2A44"),
            ["colors.primaryB"] = Color.Parse("#FFFDF7"),
            ["colors.accent"] = Color.Parse("#7356BF"),
        }));
        ThemeProvider.SetDarkTheme(Branded, BaseTheme.CreateDarkTheme(new Dictionary<string, object>
        {
            ["colors.primaryA"] = Color.Parse("#F5F1E8"),
            ["colors.primaryB"] = Color.Parse("#121826"),
            ["colors.accent"] = Color.Parse("#9F8AE0"),
        }));
    }

    private static void Load(Metered.Controls.Button button)
    {
        button.IsLoading = true;
        DispatcherTimer.RunOnce(() => button.IsLoading = false, TimeSpan.FromSeconds(2));
    }
}
