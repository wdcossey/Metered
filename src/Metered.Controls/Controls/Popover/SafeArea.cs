using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;

namespace Metered.Controls;

/// <summary>
/// A top level's safe area: its client area less what the system draws over it (a phone's status and navigation bars, a
/// display cutout), read from <see cref="IInsetsManager.SafeAreaPadding"/>. A phone's browser ends its page above those
/// bars, its viewport the area between them, so a popover is placed in it and cut off at its edges (#129, the
/// developer, 2026-11-24).
/// </summary>
internal static class SafeArea
{
    private static readonly ConditionalWeakTable<TopLevel, ISource> Fakes = [];

    /// <summary>What a popover reads of the platform: the safe area's padding, and when it changes.</summary>
    internal interface ISource
    {
        /// <summary>How far the system's bars reach into the top level's client area on each side.</summary>
        Thickness Padding { get; }

        /// <summary>The padding has changed (a rotation, the bars shown or hidden).</summary>
        event EventHandler? Changed;
    }

    /// <summary>
    /// The source read in place of a top level's insets manager, for a test: Avalonia's headless platform has no system
    /// bars, and C# can't implement <see cref="IInsetsManager"/> (its reference assembly is <c>[NotClientImplementable]</c>,
    /// with an internal member). One made at runtime by <c>DispatchProxy</c> would also do; an interface of its own carries
    /// only what the popover reads. Set before a popover opens; null gives the platform's back.
    /// </summary>
    internal static void Fake(TopLevel top, ISource? source)
    {
        if (source is null) Fakes.Remove(top);
        else Fakes.AddOrUpdate(top, source);
    }

    /// <summary>The safe area's source for a top level: a test's, the platform's insets manager, or none.</summary>
    internal static ISource? Of(TopLevel top) =>
        Fakes.TryGetValue(top, out var fake) ? fake : top.InsetsManager is { } insets ? new Platform(insets) : null;

    /// <summary>The safe area in the top level's coordinates: its client area less the padding, never less than nothing.</summary>
    internal static Rect Area(Size client, Thickness padding)
    {
        var left = Math.Max(0, padding.Left);
        var top = Math.Max(0, padding.Top);
        var width = Math.Max(0, client.Width - left - Math.Max(0, padding.Right));
        var height = Math.Max(0, client.Height - top - Math.Max(0, padding.Bottom));
        return new Rect(Math.Min(left, client.Width), Math.Min(top, client.Height), width, height);
    }

    /// <summary>
    /// The platform's insets manager, as <see cref="ISource"/>: its padding as the last change gave it, which is the one
    /// the change is about, or as it reads before any.
    /// </summary>
    private sealed class Platform(IInsetsManager insets) : ISource
    {
        private EventHandler? _changed;
        private Thickness? _given;

        public Thickness Padding => _given ?? insets.SafeAreaPadding;

        public event EventHandler? Changed
        {
            add
            {
                if (_changed is null) insets.SafeAreaChanged += OnSafeAreaChanged;
                _changed += value;
            }
            remove
            {
                _changed -= value;
                if (_changed is null) insets.SafeAreaChanged -= OnSafeAreaChanged;
            }
        }

        private void OnSafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        {
            _given = e.SafeAreaPadding;
            _changed?.Invoke(this, EventArgs.Empty);
        }
    }
}
