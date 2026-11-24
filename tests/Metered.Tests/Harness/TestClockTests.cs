using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Metered.Tests;

/// <summary>
/// The harness's <see cref="TestClock"/> (#127): each of the three real clocks Avalonia's headless platform reads stands
/// still however long the test takes, and moves only by <see cref="Scene.Advance"/>. Each waits on real time first
/// (<see cref="Thread.Sleep(int)"/>), as a loaded machine does, and checks nothing moved.
/// </summary>
public class TestClockTests
{
    private const int RealWait = 400;

    private static (Border Box, Scene Scene) Box()
    {
        var box = new Border { Width = 100, Height = 100, Background = Brushes.Transparent };
        return (box, new Scene(box));
    }

    // The dispatcher's clock: a timer fires at its own time on the test's clock, not on real time.
    [AvaloniaFact]
    public void A_dispatcher_timer_fires_when_the_clock_reaches_it_and_not_before()
    {
        var (_, scene) = Box();
        using var _ = scene;
        TimeSpan? firedAt = null;
        DispatcherTimer.RunOnce(() => firedAt = scene.Clock!.Elapsed, TimeSpan.FromMilliseconds(100));

        Thread.Sleep(RealWait);
        scene.Pump();
        Assert.Null(firedAt);
        scene.Advance(99);
        Assert.Null(firedAt);
        scene.Advance(1);
        Assert.Equal(TimeSpan.FromMilliseconds(100), firedAt);
    }

    // Each timer at its own time, not at the next frame's.
    [AvaloniaFact]
    public void Advance_runs_each_timer_at_its_own_millisecond()
    {
        var (_, scene) = Box();
        using var _ = scene;
        var fired = new List<TimeSpan>();
        foreach (var ms in new[] { 5, 21, 37 })
            DispatcherTimer.RunOnce(() => fired.Add(scene.Clock!.Elapsed), TimeSpan.FromMilliseconds(ms));

        scene.Advance(40);
        Assert.Equal([TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(21), TimeSpan.FromMilliseconds(37)], fired);
    }

    // The media context's clock: a transition stands where it is while real time passes, and runs with the test's.
    [AvaloniaFact]
    public void A_transition_runs_on_the_test_clock_only()
    {
        var (box, scene) = Box();
        using var _ = scene;
        box.Transitions = [new DoubleTransition { Property = Visual.OpacityProperty, Duration = TimeSpan.FromMilliseconds(100) }];
        box.Opacity = 0;
        scene.Pump();
        var start = box.Opacity;

        Thread.Sleep(RealWait);
        scene.Pump();
        Assert.Equal(start, box.Opacity);

        scene.Advance(50);
        var half = box.Opacity;
        Assert.InRange(half, 0.01, 0.99);
        Thread.Sleep(RealWait);
        scene.Pump();
        Assert.Equal(half, box.Opacity);

        scene.Advance(100);
        Assert.Equal(0, box.Opacity);
    }

    // A window's clock: its input's timestamps stand still with the test's clock and move by as much as it does.
    [AvaloniaFact]
    public void Input_timestamps_follow_the_test_clock()
    {
        var (box, scene) = Box();
        using var _ = scene;
        var stamps = new List<ulong>();
        box.PointerPressed += (_, e) => stamps.Add(e.Timestamp);

        scene.Click(box);
        Thread.Sleep(RealWait);
        scene.Click(box);
        scene.Advance(30);
        scene.Click(box);

        Assert.Equal(3, stamps.Count);
        Assert.Equal(stamps[0], stamps[1]);
        Assert.Equal(stamps[0] + 30, stamps[2]);
    }

    // The flake behind #127's textarea drags: Avalonia's hold is a dispatcher timer of the platform's hold wait (300 ms)
    // from a touch's press. On real time, a slow step between the press and the first move let it fire, and the drag
    // became the hold's selection. On the test's clock it fires only once the test has waited for it.
    [AvaloniaFact]
    public void A_hold_comes_only_once_the_test_clock_reaches_the_hold_wait()
    {
        var (box, scene) = Box();
        using var _ = scene;
        var holds = new List<HoldingState>();
        box.AddHandler(InputElement.HoldingEvent, (_, e) => holds.Add(e.HoldingState));
        var touch = scene.Window.TouchBegin(scene.CentreOf(box), RawInputModifiers.None);
        scene.Pump();

        Thread.Sleep(RealWait);
        scene.Pump();
        Assert.Empty(holds);
        scene.Advance(299);
        Assert.Empty(holds);
        scene.Advance(1);
        Assert.Equal([HoldingState.Started], holds);
        scene.Window.TouchEnd(touch, scene.CentreOf(box), RawInputModifiers.None);
    }

    // A scene can be asked for real time, as PageTimingTests' are; it has no clock, and the dispatcher's loop runs its timers.
    [AvaloniaFact]
    public void A_scene_on_real_time_has_no_clock_and_its_timers_run_on_real_time()
    {
        using var scene = new Scene(new Border(), realTime: true);
        Assert.Null(scene.Clock);
        Assert.False(TestClock.IsInstalled);
        Assert.Throws<InvalidOperationException>(() => scene.Advance(1));
        var fired = false;
        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        DispatcherTimer.RunOnce(() =>
        {
            fired = true;
            stop.Cancel();
        }, TimeSpan.FromMilliseconds(20));
        Dispatcher.UIThread.MainLoop(stop.Token);
        Assert.True(fired);
    }

    // One test's time is one clock: a scene on real time can't join a test already on the test's clock.
    [AvaloniaFact]
    public void A_real_time_scene_cant_join_a_test_on_the_test_clock()
    {
        using var scene = new Scene(new Border());
        Assert.True(TestClock.IsInstalled);
        Assert.Throws<InvalidOperationException>(() => new Scene(new Border(), realTime: true));
    }

    // And the other way: a test with a scene on real time can't put another on the test's clock.
    [AvaloniaFact]
    public void A_test_on_real_time_cant_go_on_the_test_clock()
    {
        using var scene = new Scene(new Border(), realTime: true);
        Assert.Throws<InvalidOperationException>(() => new Scene(new Border()));
        Assert.False(TestClock.IsInstalled);
    }

    [AvaloniaFact]
    public void Time_doesnt_go_back()
    {
        using var scene = new Scene(new Border());
        scene.Advance(5);
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.Advance(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => scene.Clock!.Advance(-1));
        Assert.Equal(TimeSpan.FromMilliseconds(5), scene.Clock!.Elapsed);
    }

    // A closed window has no input clock to hold: said so, not blamed on an Avalonia upgrade.
    [AvaloniaFact]
    public void A_closed_window_is_refused_as_closed()
    {
        var window = new Window();
        window.Show();
        window.Close();
        Dispatcher.UIThread.RunJobs();
        var refused = Assert.Throws<InvalidOperationException>(() => TestClock.Of(window));
        Assert.Contains("no platform implementation", refused.Message);
    }

    // Two scenes in one test share its clock, and the second window's input follows it from where the clock is.
    [AvaloniaFact]
    public void Two_scenes_in_a_test_share_its_clock()
    {
        using var first = new Scene(new Border());
        first.Advance(10);
        var box = new Border { Width = 100, Height = 100, Background = Brushes.Transparent };
        using var second = new Scene(box);
        Assert.Same(first.Clock, second.Clock);
        var stamps = new List<ulong>();
        box.PointerPressed += (_, e) => stamps.Add(e.Timestamp);
        second.Click(box);
        first.Advance(25);
        second.Click(box);
        Assert.Equal(stamps[0] + 25, stamps[1]);
    }
}
