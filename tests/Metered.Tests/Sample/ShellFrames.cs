using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Metered.Sample;

namespace Metered.Tests.Sample;

/// <summary>What the sample's tests share: frames for the shell's timing, and the platform's back request.</summary>
internal static class ShellFrames
{
    /// <summary>Enough frames for a page to be drawn, the frame after it to come, and the job after that pass to run.</summary>
    public static void Draw()
    {
        for (var frame = 0; frame < 4; frame++)
        {
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }

    /// <summary>What Avalonia does for the platform's back: raises the request, and reads <c>Handled</c>.</summary>
    public static bool Back(Interactive from)
    {
        var e = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        from.RaiseEvent(e);
        Dispatcher.UIThread.RunJobs();
        return e.Handled;
    }

    /// <summary>The list's buttons, one per page.</summary>
    public static IReadOnlyList<Metered.Controls.Button> ListButtons(SampleShell shell) =>
        shell.FindControl<StackPanel>("PageList")!.Children.OfType<Metered.Controls.Button>().ToList();

    /// <summary>A named control in the shell: in its bar, or its footer.</summary>
    public static T InBar<T>(SampleShell shell, string name)
        where T : Control =>
        shell.FindControl<T>(name) ?? throw new InvalidOperationException($"The shell has no '{name}'.");
}
