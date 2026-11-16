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
/// viewport) and render scaling 1, light unless dark is asked for. Closed on dispose.
/// </summary>
internal sealed class Scene : IDisposable
{
    public const double Width = 390;
    public const double Height = 844;

    public Window Window { get; }

    public Scene(Control? content = null, ThemeVariant? variant = null, double width = Width, double height = Height)
    {
        Window = new Window
        {
            Width = width,
            Height = height,
            RequestedThemeVariant = variant ?? ThemeVariant.Light,
        };
        Window.Styles.Add(new BaseTheme());
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
