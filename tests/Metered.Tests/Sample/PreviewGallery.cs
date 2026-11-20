using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Metered.Sample;
using Button = Metered.Controls.Button;

namespace Metered.Tests.Sample;

/// <summary>
/// The pictures for <c>PREVIEW.md</c>: the sample's own <see cref="MainWindow"/>, at its 390 × 844, light and then dark
/// (by the shell's own switch), its list and then each page of its catalogue, one phone screen per picture, from the top
/// of the list or page to its end. <c>PREVIEW.md</c> and <c>README.md</c> show them, committed under
/// <c>docs/preview/</c>. Drawn headless with Skia, as the tests' frames are, so they are the theme's own pixels on any
/// OS, at a render scaling of 2. By hand, as <c>METERED_FRAMES</c> is: it asserts nothing and does nothing while
/// <c>METERED_PREVIEW</c> is unset; set, it must name an absolute, new or empty folder, or the test fails.
/// <code>
/// METERED_PREVIEW=/absolute/folder dotnet test tests/Metered.Tests --filter FullyQualifiedName~PreviewGallery
/// </code>
/// Each picture is <c>&lt;variant&gt;__&lt;page&gt;-&lt;n&gt;.png</c>, 780 × 1688, where the page is its catalogue title
/// in lower case, or <c>list</c>: a screen each rather than one tall picture, so they sit side by side in Markdown at a
/// phone's proportions, light beside dark, and a reader sees each at the size a phone shows it. The shell's Debug line
/// in its footer is turned off, as its times would change every picture on every run.
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
    /// (200 ms, Base Web's <c>transition: background 200ms linear</c>), which the shell's Light/Dark switch starts.
    /// A longer one needs this raised; <see cref="Shoot"/> refuses a picture that is still moving.
    /// </summary>
    private static readonly TimeSpan RealTimeSettle = TimeSpan.FromMilliseconds(600);

    /// <summary>
    /// The sample's default family (<see cref="SampleFonts.DefaultFamilyName"/>), for the text the tokens don't give a
    /// family: its headings. The tests keep the platform's default (TestApp), so the window is given it, as the
    /// sample's whole app is. The tests have the sample's fonts, Metered.Fonts', so the name finds the same font.
    /// </summary>
    private static readonly FontFamily SampleDefaultFamily = new(SampleFonts.DefaultFamilyName);

    [AvaloniaFact]
    public void Save()
    {
        if (Environment.GetEnvironmentVariable(Variable) is not { Length: > 0 } folder)
            return;
        if (!Path.IsPathFullyQualified(folder))
            throw new ArgumentException($"{Variable} must be an absolute folder; it is '{folder}'.");

        // A fresh folder, so a picture of a screen the sample no longer has can't sit beside the new ones.
        if (Directory.Exists(folder) && Directory.EnumerateFileSystemEntries(folder).Any())
            throw new InvalidOperationException($"{Variable} must be a new or empty folder; '{folder}' isn't.");
        Directory.CreateDirectory(folder);

        // App.axaml's own styles, loaded from it, where it has them: on the application, so they are there as the
        // window's XAML builds its controls (a control finds its theme once, as it joins the tree; given to the window
        // afterwards, they'd never be found). Taken off again at the end: this body runs on the UI thread without
        // yielding, so no other test sees them.
        var app = Application.Current ?? throw new InvalidOperationException("No Avalonia application.");
        var sample = new App();
        sample.Initialize();
        IStyle[] themes = [.. sample.Styles];
        sample.Styles.Clear();
        if (themes.Length == 0)
            throw new InvalidOperationException("The sample's App.axaml has no styles: update PreviewGallery.");
        MainWindow? window = null;
        try
        {
            app.Styles.AddRange(themes);
            window = new MainWindow { RequestedThemeVariant = sample.RequestedThemeVariant, FontFamily = SampleDefaultFamily };
            var shell = window.Content as SampleShell
                ?? throw new InvalidOperationException("The sample's window doesn't hold its shell: update PreviewGallery.");
            shell.ShowsCost = false;
            shell.Report = _ => { };
            window.SetRenderScaling(Scaling);
            window.Show();
            if (window.ActualThemeVariant != ThemeVariant.Light)
                throw new InvalidOperationException("The sample doesn't start light: update PreviewGallery.");
            ShootAll(window, shell, folder, "light");

            // Dark as a user gets it: the shell's own switch, whose handler also relabels it.
            var variant = shell.FindControl<Button>("Variant")
                ?? throw new InvalidOperationException("The shell has no 'Variant' switch: update PreviewGallery.");
            variant.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
            if (window.ActualThemeVariant != ThemeVariant.Dark)
                throw new InvalidOperationException("The shell's switch didn't turn the window dark.");
            ShootAll(window, shell, folder, "dark");
        }
        finally
        {
            window?.Close();
            Dispatcher.UIThread.RunJobs();
            foreach (var theme in themes)
                app.Styles.Remove(theme);
        }
    }

    /// <summary>The list, then each page of the catalogue, opened as its button on the list opens it, then the list again.</summary>
    private static void ShootAll(Window window, SampleShell shell, string folder, string variant)
    {
        shell.ShowIndex();
        Shoot(window, Scroller(shell.Shown), folder, $"{variant}__{ListName}");
        foreach (var page in shell.Catalogue)
        {
            shell.Open(page);
            Shoot(window, Scroller(shell.Shown), folder, $"{variant}__{PictureName(page.Title)}");
        }

        shell.ShowIndex();
    }

    /// <summary>What the list's pictures are called.</summary>
    private const string ListName = "list";

    /// <summary>A page's pictures' name: its title in lower case, which must be a file's name and not the list's.</summary>
    internal static string PictureName(string title)
    {
        var name = title.ToLowerInvariant();
        if (name == ListName || name.Length == 0 || name.IndexOfAny([.. Path.GetInvalidFileNameChars(), '/', '\\']) >= 0)
            throw new InvalidOperationException($"The page '{title}' can't name its pictures: update PreviewGallery.");
        return name;
    }

    /// <summary>The scroller of the list or a page: the list is one, and a page holds one as its content.</summary>
    private static ScrollViewer Scroller(Control? shown) =>
        shown as ScrollViewer ?? (shown as UserControl)?.Content as ScrollViewer
        ?? throw new InvalidOperationException($"{shown?.GetType().Name ?? "Nothing"} isn't a page that scrolls: update PreviewGallery.");

    /// <summary>
    /// Each screen of what <paramref name="scroller"/> holds, a viewport apart from the top, the last ending it, at rest,
    /// saved as <c>&lt;name&gt;-&lt;n&gt;.png</c>.
    /// </summary>
    private static void Shoot(Window window, ScrollViewer scroller, string folder, string name)
    {
        Draw(window).Dispose();
        // Once laid out, so the page's templates have made the parts that move.
        HoldStill(scroller);
        var viewport = scroller.Viewport.Height;
        if (viewport <= 0)
            throw new InvalidOperationException($"The page has no viewport ({scroller.Viewport}).");
        var end = Math.Max(0, scroller.Extent.Height - viewport);
        // Within half a pixel of the end is the end, so layout rounding can't add a screen that repeats the last.
        var screens = 1 + (int)Math.Ceiling(Math.Max(0, end - 0.5) / viewport);
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

            frame.Save(Path.Combine(folder, $"{name}-{n + 1}.png"), PngBitmapEncoderOptions.Default);
        }
    }

    /// <summary>What a theme's motion that never ends (a spinner's turn) runs without: <see cref="HoldStill"/> sets it.</summary>
    internal const string Still = ":metered-still";

    /// <summary>
    /// Stops the motion that never ends, so a picture can be at rest: <see cref="Still"/> on every element of the screen.
    /// A theme runs such motion only while its element isn't <c>:metered-still</c> (the Spinner's ring then stands at
    /// 0deg, Base Web's <c>from</c>), as Avalonia 12's animation clock is internal and can't be held (#13).
    /// </summary>
    private static void HoldStill(Visual root)
    {
        foreach (var element in root.GetSelfAndVisualDescendants().OfType<StyledElement>())
            ((IPseudoClasses)element.Classes).Set(Still, true);
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
