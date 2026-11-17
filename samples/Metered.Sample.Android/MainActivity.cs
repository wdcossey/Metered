using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Avalonia.Android;
using Avalonia.Threading;

namespace Metered.Sample.Android;

/// <summary>
/// The sample's only activity, showing the sample's shell (<see cref="App"/> makes it). Its Java name is fixed, so that
/// <c>tools/android/run.ps1</c> can start it with <c>am start -n io.github.wdcossey.metered.sample/metered.sample.MainActivity</c>.
/// It declares the configuration changes that would otherwise recreate it, so a rotation or a change of theme keeps the
/// page it shows. Single-top, so that an intent sent while it's on top comes to <see cref="OnNewIntent"/> rather than
/// starting another.
/// </summary>
[Activity(
    Name = "metered.sample.MainActivity",
    Label = "Metered",
    Theme = "@style/Metered.NoActionBar",
    MainLauncher = true,
    LaunchMode = LaunchMode.SingleTop,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.ScreenLayout
                           | ConfigChanges.SmallestScreenSize | ConfigChanges.UiMode | ConfigChanges.Density)]
public class MainActivity : AvaloniaMainActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Before Avalonia makes the view, which opens this page: `run.ps1 -Page <title>` starts the activity with
        // `--es page <title>`; with no extra, as from the launcher, the list.
        App.StartPage = Intent?.GetStringExtra(App.PageExtra);
        base.OnCreate(savedInstanceState);
    }

    /// <summary>
    /// An intent for the running app: the page its extra names is opened in the shell, as its button on the list opens
    /// it, and writes its ready line (and in Debug its cost line). <c>run.ps1 -Reopen</c> opens a page warm this way,
    /// after Back to the list. With no extra, as from the launcher, nothing changes. A title the catalogue doesn't have
    /// stops the app, as it does at start, with the titles it has; so does a view that isn't the shell, rather than
    /// leave run.ps1 to wait for a line that won't come. The intent becomes the activity's own, so that if Android
    /// makes the activity again, <see cref="OnCreate"/> opens the page last asked for.
    /// </summary>
    protected override void OnNewIntent(Intent? intent)
    {
        base.OnNewIntent(intent);
        if (intent?.GetStringExtra(App.PageExtra) is not { } title)
            return;

        Intent = intent;
        var shell = Content as SampleShell
            ?? throw new InvalidOperationException($"The activity's view is {Content?.GetType().FullName ?? "null"}, not the sample's shell, so it can't open '{title}'.");
        Dispatcher.UIThread.Post(() => shell.Open(title));
    }
}
