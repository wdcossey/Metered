using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Themes;

namespace Metered.Tests;

/// <summary>
/// A shown window with <see cref="BaseTheme"/>, at a phone's view (390 × 844, Avonic's reference
/// viewport) and render scaling 1, light unless dark is asked for. Closed on dispose. The themes are
/// the window's, not the application's, so a scene changes no other; another list can be given.
/// Its time is the test's <see cref="TestClock"/>, which moves only by <see cref="Advance"/>, unless the
/// scene is asked for real time, as a test that measures real time is.
/// </summary>
internal sealed class Scene : IDisposable
{
    public const double Width = 390;
    public const double Height = 844;

    public Window Window { get; }

    /// <summary>The test's clock, or null for a scene on real time.</summary>
    public TestClock? Clock { get; }

    public Scene(Control? content = null, ThemeVariant? variant = null, double width = Width, double height = Height,
        IEnumerable<IStyle>? themes = null, bool realTime = false)
    {
        if (realTime && TestClock.IsInstalled)
            throw new InvalidOperationException("This test's dispatcher is already on a test clock: a real-time scene can't be made in it.");
        Window = new Window
        {
            Width = width,
            Height = height,
            RequestedThemeVariant = variant ?? ThemeVariant.Light,
        };
        Clock = realTime ? null : TestClock.Of(Window);
        Window.Styles.AddRange(themes ?? [new BaseTheme()]);
        try
        {
            Window.Content = content;
            Window.Show();
            Pump();
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    /// <summary>Runs the dispatcher's queued jobs, a layout pass and a frame (hit-testing reads the drawn scene).</summary>
    public void Pump()
    {
        Dispatcher.UIThread.RunJobs();
        Window.UpdateLayout();
        Window.CaptureRenderedFrame();
        Dispatcher.UIThread.RunJobs();
    }

    /// <summary>
    /// Moves the test's clock on by <paramref name="ms"/>, a millisecond at a time, running at each what comes due then:
    /// the timers at their own times, and the frames at theirs (the media context's and the render timer's, 16 ms apart),
    /// as an app's loop does; then a <see cref="Pump"/>.
    /// </summary>
    public void Advance(int ms)
    {
        var clock = Clock ?? throw new InvalidOperationException("A scene on real time has no clock to advance.");
        for (var i = 0; i < ms; i++)
        {
            clock.Advance(1);
            // The dispatcher runs the timers that are due after each job it runs: one job sets it going.
            Dispatcher.UIThread.Post(static () => { }, DispatcherPriority.Send);
            Dispatcher.UIThread.RunJobs();
        }

        Pump();
    }

    /// <summary>The centre of a control, in the window's coordinates.</summary>
    public Point CentreOf(Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), Window)
        ?? throw new InvalidOperationException("The control isn't in the window.");

    /// <summary>A real mouse move over the control.</summary>
    public void Hover(Visual visual)
    {
        Window.MouseMove(CentreOf(visual));
        Pump();
    }

    /// <summary>A real mouse press on the control, held.</summary>
    public void Press(Visual visual)
    {
        var at = CentreOf(visual);
        Window.MouseMove(at);
        Window.MouseDown(at, MouseButton.Left);
        Pump();
    }

    /// <summary>A real click on the control.</summary>
    public void Click(Visual visual)
    {
        var at = CentreOf(visual);
        Window.MouseMove(at);
        Window.MouseDown(at, MouseButton.Left);
        Window.MouseUp(at, MouseButton.Left);
        Pump();
    }

    public void Key(Key key)
    {
        Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        Pump();
    }

    public WriteableBitmap Capture()
    {
        Pump();
        return Window.CaptureRenderedFrame() ?? throw new InvalidOperationException("The window rendered no frame.");
    }

    public void Dispose()
    {
        Window.Close();
        Window.Content = null;
        Dispatcher.UIThread.RunJobs();
    }
}
