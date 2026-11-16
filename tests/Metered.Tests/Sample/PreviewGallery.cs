using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.Threading;
using Metered.Sample.Desktop;
using Metered.Themes;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The pictures for <c>PREVIEW.md</c>: the desktop sample's own <see cref="MainWindow"/>, at its 390 × 844, light and
/// then dark (by the sample's own switch), one phone screen per picture, from the top of the page to its end.
/// <c>PREVIEW.md</c> and <c>README.md</c> show them, committed under <c>docs/preview/</c>. Drawn headless with Skia,
/// as the tests' frames are, so they are the theme's own pixels on any OS, at a render scaling of 2. By hand, as
/// <c>METERED_FRAMES</c> is: it asserts nothing, and does nothing unless <c>METERED_PREVIEW</c> names an absolute,
/// new or empty folder.
/// <code>
/// METERED_PREVIEW=/absolute/folder dotnet test tests/Metered.Tests --filter FullyQualifiedName~PreviewGallery
/// </code>
/// Each picture is <c>&lt;variant&gt;__screen-&lt;n&gt;.png</c>, 780 × 1688: a screen each rather than one tall
/// picture of the page, so they sit side by side in Markdown at a phone's proportions, light beside dark, and a reader
/// sees each at the size a phone shows it.
/// </summary>
public class PreviewGallery
{
    public const string Variable = "METERED_PREVIEW";

    /// <summary>
    /// The render scaling of the pictures, so 780 × 1688: sharp on a high-density screen at the 250 to 300 wide the
    /// Markdown shows them, and small enough to commit.
    /// </summary>
    private const double Scaling = 2;

    /// <summary>
    /// Real time to let pass before each picture: past the longest transition in the theme, the Button's background
    /// (200 ms, Base Web's <c>transition: background 200ms linear</c>), which the sample's Light/Dark switch starts.
    /// A longer one needs this raised; <see cref="Shoot"/> refuses a picture that is still moving.
    /// </summary>
    private static readonly TimeSpan RealTimeSettle = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// The sample's default family (<c>Program.BuildAvaloniaApp</c>'s <c>DefaultFamilyName</c>), for the text the
    /// tokens don't give a family: its headings and labels. The tests keep the platform's default (TestApp), so the
    /// window is given it, as the sample's whole app is.
    /// </summary>
    private static readonly FontFamily SampleDefaultFamily = new($"{TestApp.FontCollection}#{TestApp.TextFamily}");

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { Length: > 0 } folder)
            return;
        if (!Path.IsPathFullyQualified(folder))
            throw new ArgumentException($"{Variable} must be an absolute folder; it is '{folder}'.");

        // A fresh folder, so a picture of a screen the page no longer has can't sit beside the new ones.
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new InvalidOperationException($"{Variable} must be a new or empty folder; '{folder}' isn't.");
        Directory.CreateDirectory(folder);

        // App.axaml's themes, where it has them: the application's, so they are there as the window's XAML builds its
        // controls (a control finds its theme once, as it joins the tree; given to the window afterwards, they'd
        // never be found). Simple first, for the Window, ScrollViewer and TextBlock BaseTheme doesn't theme yet, then
        // BaseTheme. Taken off again at the end: this body runs on the UI thread without yielding, so no other test
        // sees them.
        var app = Application.Current ?? throw new InvalidOperationException("No Avalonia application.");
        IStyle[] themes = [new SimpleTheme(), new BaseTheme()];
        MainWindow? window = null;
        try
        {
            app.Styles.AddRange(themes);
            window = new MainWindow { RequestedThemeVariant = ThemeVariant.Light, FontFamily = SampleDefaultFamily };
            window.SetRenderScaling(Scaling);
            window.Show();
            Shoot(window, folder, "light");

            // Dark as a user gets it: the sample's own switch, whose handler also relabels it.
            var variant = window.FindControl<Button>("Variant")
                ?? throw new InvalidOperationException("The sample has no 'Variant' switch: update PreviewGallery.");
            variant.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            if (window.ActualThemeVariant != ThemeVariant.Dark)
                throw new InvalidOperationException("The sample's switch didn't turn the window dark.");
            Shoot(window, folder, "dark");
        }
        finally
        {
            window?.Close();
            Dispatcher.UIThread.RunJobs();
            foreach (var theme in themes)
                app.Styles.Remove(theme);
        }
    }

    /// <summary>
    /// Each screen of the page, from the top to the end, at rest, saved as <c>&lt;name&gt;__screen-&lt;n&gt;.png</c>.
    /// </summary>
    private static void Shoot(Window window, string folder, string name)
    {
        var scroller = window.Content as ScrollViewer
            ?? throw new InvalidOperationException("The sample's window isn't a ScrollViewer: update PreviewGallery.");

        Settle(window);
        var viewport = scroller.Viewport.Height;
        var end = Math.Max(0, scroller.Extent.Height - viewport);
        var screens = (int)Math.Ceiling(scroller.Extent.Height / viewport);
        for (var n = 0; n < screens; n++)
        {
            scroller.Offset = new Vector(0, Math.Min(n * viewport, end));
            Settle(window);
            using var frame = Draw(window);

            // Stable pixels, or none: a picture drawn mid-transition would change on every run.
            Settle(window);
            using var again = Draw(window);
            if (!Frames.SameAs(frame, again))
                throw new InvalidOperationException($"{name} screen {n + 1} is still moving after {RealTimeSettle.TotalMilliseconds} ms: raise RealTimeSettle.");

            frame.Save(Path.Combine(folder, $"{name}__screen-{n + 1}.png"), PngBitmapEncoderOptions.Default);
        }
    }

    /// <summary>
    /// Avalonia's transitions run on the render loop's clock, which is real time: draw frames over
    /// <see cref="RealTimeSettle"/> of it, so the picture is at rest.
    /// </summary>
    private static void Settle(Window window)
    {
        var settled = DateTime.UtcNow + RealTimeSettle;
        while (DateTime.UtcNow < settled)
        {
            Thread.Sleep(50);
            Draw(window).Dispose();
        }
    }

    private static WriteableBitmap Draw(Window window)
    {
        Dispatcher.UIThread.RunJobs();
        window.UpdateLayout();
        var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The window rendered no frame.");
        Dispatcher.UIThread.RunJobs();
        return frame;
    }
}
