using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Metered.Sample;

/// <summary>
/// The sample app: <c>SimpleTheme</c> under <c>BaseTheme</c>, light. On desktop it shows <see cref="MainWindow"/>; on a
/// phone (a single view or an activity), the same <see cref="SampleShell"/> as the whole view, opened at
/// <see cref="StartPage"/>. Every head calls <see cref="SampleFonts.UseSampleFonts"/> and holds only its entry point.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// The page a phone opens at start, by its catalogue title in any case, or null for the list. A head sets it before
    /// Avalonia makes the view (on Android, from its intent, in the activity's <c>OnCreate</c>).
    /// </summary>
    public static string? StartPage { get; set; }

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        switch (ApplicationLifetime)
        {
            case IClassicDesktopStyleApplicationLifetime desktop:
                desktop.MainWindow = new MainWindow();
                break;
            case IActivityApplicationLifetime activity:
                // Made when the activity is created, after its OnCreate has set StartPage.
                activity.MainViewFactory = () => PhoneView(SampleShell.Pages, StartPage);
                break;
            case ISingleViewApplicationLifetime singleView:
                singleView.MainView = PhoneView(SampleShell.Pages, StartPage);
                break;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// A phone's view: the catalogue of <paramref name="pages"/>, showing the list, or the page titled
    /// <paramref name="startPage"/> if it's given. The platform's back request is Back there.
    /// </summary>
    /// <exception cref="ArgumentException">No page has the title <paramref name="startPage"/>.</exception>
    internal static SampleShell PhoneView(IReadOnlyList<SamplePageEntry> pages, string? startPage)
    {
        var shell = new SampleShell(pages);
        if (startPage is not null)
            shell.Open(startPage);
        return shell;
    }
}
