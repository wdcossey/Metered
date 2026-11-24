using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Threading;

namespace Metered.Tests;

/// <summary>
/// The test's time (#127). Avalonia 12.1.4's headless platform reads real time in three places a test sees: the
/// dispatcher's clock, which every <see cref="DispatcherTimer"/> reads (a popover's delays, Avalonia's hold gesture, a
/// caret's blink); the media context's, which every transition and animation reads; and each headless window's, which
/// stamps its input (a scroll gesture's velocity reads it). On a loaded machine real time runs on between two steps of a
/// test: a hold fires between a touch's press and its first move, a fade is over before it is looked at. Installed, each
/// of them stops where it is and moves only by <see cref="Advance"/>, so a test sees the same times on any machine.
/// <para>One per dispatcher, and so per test (the assembly keeps Avalonia's default isolation, a dispatcher per test).
/// Avalonia gives no clock to replace (its <c>IClock</c> and <c>Animatable.Clock</c> are internal, and a dispatcher's
/// implementation can't be swapped once it exists), so this reaches their private fields; an Avalonia upgrade that moves
/// one fails here, naming it.</para>
/// <para>Not held: the dispatcher's own loop (<c>MainLoop</c>) still waits on its implementation's real clock, so on this
/// clock every timer looks due to it and none is: it spins until cancelled and fires nothing (a test on this clock
/// advances it instead). Two clocks of Avalonia's stay real too, and no test here reads them: a scroll gesture's inertia
/// (a stopwatch started at the release, and the velocity tracker's since its last sample), which starts only from a fling
/// whose moves are stamped apart, while a test's moves are stamped the same unless it advances between them; and the
/// headless render timer's tick time, which only the compositor's own animations read, and Metered runs none.</para>
/// </summary>
internal sealed class TestClock
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private static readonly ConditionalWeakTable<Dispatcher, TestClock> Clocks = new();

    private static readonly ConditionalWeakTable<Dispatcher, object> RealTime = new();

    private static readonly FieldInfo StopwatchElapsed = Field(typeof(Stopwatch), "_elapsed");

    private readonly List<(Stopwatch Watch, long Start)> _held = [];
    private readonly ConditionalWeakTable<object, object> _windows = new();
    private readonly long _dispatcherStart;
    private long _ms;

    private TestClock(Dispatcher dispatcher)
    {
        var provider = Field(typeof(Dispatcher), "_timeProvider");
        var real = (Func<long>)(provider.GetValue(dispatcher) ?? throw Moved(typeof(Dispatcher), "_timeProvider"));
        _dispatcherStart = real();
        provider.SetValue(dispatcher, (Func<long>)(() => _dispatcherStart + _ms));

        var mediaContext = typeof(Dispatcher).Assembly.GetType("Avalonia.Media.MediaContext")
            ?? throw new InvalidOperationException("Avalonia has no Avalonia.Media.MediaContext: TestClock can't hold its clock.");
        var instance = mediaContext.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw Moved(mediaContext, "Instance");
        Hold(Watch(instance.GetValue(null)!, "_time"));
    }

    /// <summary>How far the test has moved its clock since it was installed.</summary>
    public TimeSpan Elapsed => TimeSpan.FromMilliseconds(_ms);

    /// <summary>The current test's clock, installed on first use; it holds the window's input clock too.</summary>
    public static TestClock Of(Window window)
    {
        if (RealTime.TryGetValue(Dispatcher.UIThread, out _))
            throw new InvalidOperationException("This test has a scene on real time: its dispatcher can't go on a test clock.");
        var platformImpl = typeof(TopLevel).GetProperty("PlatformImpl", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw Moved(typeof(TopLevel), "PlatformImpl");
        var impl = platformImpl.GetValue(window)
            ?? throw new InvalidOperationException("The window has no platform implementation (closed?): its input clock can't be held.");
        var clock = Clocks.GetValue(Dispatcher.UIThread, static d => new TestClock(d));
        if (!clock._windows.TryGetValue(impl, out _))
        {
            clock._windows.Add(impl, impl);
            clock.Hold(Watch(impl, "_st"));
        }

        return clock;
    }

    /// <summary>Whether the current test's dispatcher is on a test clock.</summary>
    public static bool IsInstalled => Clocks.TryGetValue(Dispatcher.UIThread, out _);

    /// <summary>
    /// Keeps the current test's dispatcher on real time, for a scene that measures it: one test's time is one clock, so
    /// this throws if it is already on a test clock, and <see cref="Of"/> throws after it.
    /// </summary>
    public static void KeepRealTime()
    {
        if (IsInstalled)
            throw new InvalidOperationException("This test's dispatcher is already on a test clock: a real-time scene can't be made in it.");
        RealTime.AddOrUpdate(Dispatcher.UIThread, RealTime);
    }

    /// <summary>
    /// Moves every held clock on by <paramref name="ms"/>. Nothing runs here: what comes due runs when the dispatcher
    /// next runs a job (<see cref="Scene.Advance"/> runs them, a millisecond at a time).
    /// </summary>
    public void Advance(int ms)
    {
        if (ms < 0) throw new ArgumentOutOfRangeException(nameof(ms), ms, "Time doesn't go back.");
        _ms += ms;
        foreach (var (watch, start) in _held) StopwatchElapsed.SetValue(watch, start + _ms * Stopwatch.Frequency / 1000);
    }

    // A stopped stopwatch's elapsed time is its _elapsed, in its own ticks, which Advance sets from where it stopped.
    private void Hold(Stopwatch watch)
    {
        watch.Stop();
        _held.Add((watch, watch.ElapsedTicks - _ms * Stopwatch.Frequency / 1000));
    }

    private static Stopwatch Watch(object owner, string name) =>
        Field(owner.GetType(), name).GetValue(owner) as Stopwatch ?? throw Moved(owner.GetType(), name);

    private static FieldInfo Field(Type type, string name) => type.GetField(name, Private) ?? throw Moved(type, name);

    private static InvalidOperationException Moved(Type type, string name) =>
        new($"{type.FullName} has no {name} as Avalonia 12.1.4 had: TestClock can't hold its time (#127).");
}
