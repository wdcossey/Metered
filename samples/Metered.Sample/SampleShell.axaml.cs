using System.Diagnostics;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Reactive;
using Avalonia.Styling;
using Avalonia.Threading;
using Metered.Controls;
using Metered.Sample.Diagnostics;

namespace Metered.Sample;

/// <summary>
/// The sample's catalogue: the list of <see cref="SamplePageAttribute"/> pages, or one of them, under a bar with Back,
/// the page's title and the Light/Dark switch. <see cref="MainWindow"/> holds one on desktop, and <see cref="App"/> shows
/// one as a phone's view. Back is the Back button, or Avalonia's back request (Alt+Left on desktop, the back button or
/// gesture on a phone) once nothing else on its route has taken it. From Cornflower's, without its page transition and
/// its rail: Base Web has neither, so a page takes the list's place at once. The list, or the page in its place, is in a
/// <see cref="KeyboardInset"/>, so that on a phone it ends above the soft keyboard and its focused input is brought into
/// view, as a phone's browser does (#108).
/// <para>
/// The shell times every page it opens, from before the page is made to its first drawn frame, and writes the page's
/// <c>METERED ready</c> line to standard output (logcat's <c>DOTNET</c> tag on Android), so that a tool which reads it
/// knows a frame it takes after it is the page at rest. A page has no timing code of its own. In a Debug build it also
/// writes what the page cost to open, a <c>METERED cost</c> line, and shows it in a footer over the page's foot, wrapped
/// to the view's width (<see cref="PerformanceMonitor"/>). The footer takes nothing from the page's size, which is the
/// same in every build and doesn't change when the line appears; a tap on it hides it until the next page.
/// </para>
/// </summary>
public partial class SampleShell : UserControl
{
    /// <summary>What every ready line starts with.</summary>
    internal const string ReadyMarker = "METERED ready";

    /// <summary>What the Debug line starts with, written after the page's ready line.</summary>
    internal const string CostMarker = "METERED cost";

    /// <summary>The bar's title on the list.</summary>
    internal const string IndexTitle = "Metered";

    private static IReadOnlyList<SamplePageEntry>? _pages;

    /// <summary>
    /// Every page in the sample, found by its attribute. Found on first use rather than in a static initialiser, so that
    /// a page that can't be made fails with its own message, not a type initialiser's.
    /// </summary>
    internal static IReadOnlyList<SamplePageEntry> Pages => _pages ??= SamplePageCatalog.Discover(typeof(SampleShell).Assembly);

    private TopLevel? _top;
    private IDisposable? _backRequests;

    /// <summary>The page being opened, from when it's shown until its ready line is written, or until it's left; then null.</summary>
    private Opening? _opening;

    /// <summary>The sample's catalogue, <see cref="Pages"/>.</summary>
    public SampleShell()
        : this(Pages)
    {
    }

    /// <summary>A catalogue of <paramref name="catalogue"/>, for a test's own pages.</summary>
    internal SampleShell(IReadOnlyList<SamplePageEntry> catalogue)
    {
        Catalogue = catalogue;
        InitializeComponent();
        foreach (var page in catalogue)
        {
            var open = new Metered.Controls.Button { Content = page.Title, Kind = ButtonKind.Secondary, WidthType = ButtonWidthType.Fill };
            open.Click += (_, _) => Open(page);
            PageList.Children.Add(open);
        }

        PageTitle.Text = IndexTitle;
        ActualThemeVariantChanged += (_, _) => LabelVariant();
        LabelVariant();
    }

    /// <summary>The pages on the list.</summary>
    internal IReadOnlyList<SamplePageEntry> Catalogue { get; }

    /// <summary>Where each ready line, and in Debug each cost line, goes. Standard output; a test replaces it.</summary>
    internal Action<string> Report { get; set; } = Console.WriteLine;

    /// <summary>
    /// How long a page whose view has no size yet waits for one before its line is written anyway, with the size it has;
    /// a test shortens it. Ten seconds: a phone's view has its size well within a second of its start, and
    /// <c>tools/android/run.ps1</c> waits 60 for the line, so a view that never gets one is still reported, as such. The
    /// wait itself is the platform's, not the page's, so it is left out of the page's time.
    /// </summary>
    internal TimeSpan SizeWait { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Whether a Debug build shows the cost line in its footer; on by default. The preview turns it off, so that its
    /// pictures don't change with each run's times. It is still written to <see cref="Report"/>.
    /// </summary>
    internal bool ShowsCost { get; set; } = true;

    /// <summary>Whether a page is shown, rather than the list.</summary>
    public bool IsShowingPage => !ReferenceEquals(Host.Child, Index);

    /// <summary>The page shown, or the list.</summary>
    internal Control? Shown => Host.Child;

    /// <summary>Shows the list.</summary>
    public void ShowIndex()
    {
        Leave(toPage: false);
        Host.Child = Index;
        PageTitle.Text = IndexTitle;
    }

    /// <summary>
    /// Makes the page called <paramref name="title"/> and shows it, as its button on the list does. Titles are matched
    /// without regard to case, as the catalogue already keeps apart titles that differ only in case.
    /// </summary>
    /// <exception cref="ArgumentException">No page has the title; the message lists those there are.</exception>
    public void Open(string title)
    {
        var page = Catalogue.FirstOrDefault(entry => string.Equals(entry.Title, title, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException(
                $"The sample has no page titled '{title}'. Its pages are: {string.Join(", ", Catalogue.Select(entry => $"'{entry.Title}'"))}.",
                nameof(title));
        Open(page);
    }

    /// <summary>Makes <paramref name="page"/> and shows it, timed from before it's made, as its styles apply when it's attached.</summary>
    public void Open(SamplePageEntry page)
    {
        var started = Stopwatch.GetTimestamp();
        Show(page.Create(), page.Title, started);
    }

    /// <summary>Shows <paramref name="page"/>, made by a test, under <paramref name="title"/>, timed from now.</summary>
    internal void ShowPage(Control page, string title) => Show(page, title, Stopwatch.GetTimestamp());

    /// <summary>The ready line: the time to open, what the device reports, and the page's title, last, as it can have spaces.</summary>
    internal static string ReadyLine(TimeSpan opened, string facts, string title) =>
        string.Create(CultureInfo.InvariantCulture, $"{ReadyMarker} open={opened.TotalMilliseconds:0}ms {facts} title={title}");

    private void Show(Control page, string title, long started)
    {
        Leave(toPage: true);
        Host.Child = page;
        PageTitle.Text = title;
        _opening = new Opening(page, title, started, SizeWait);
        if (_top is null)
            _opening.ShownOutOfWindow();
        AwaitFirstFrame();
    }

    /// <summary>What leaving the page shown means, whichever way: its opening unreported, its cost hidden, and Back.</summary>
    private void Leave(bool toPage)
    {
        _opening?.Dispose();
        _opening = null;
        Footer.IsVisible = false;
        PerformanceLine.Text = null;
        BackButton.IsVisible = toPage;
    }

    /// <summary>
    /// Asks for the frame after the one that first draws the page being opened, if the shell is in a window; a shell
    /// that isn't in one yet, as on a phone at start, asks once it is.
    /// </summary>
    private void AwaitFirstFrame()
    {
        if (_opening is not { } opening || _top is not { } top)
            return;

        // A page shown before its shell was in a window (a phone's start page) is timed as if shown now: the time it took
        // to make, and from here to its first frame, not the platform's own start between.
        opening.InWindow();
#if DEBUG
        // Only now, in a window, so that a shell never shown has no listener; layout and render can't come before.
        opening.Measure();
#endif
        AwaitFrames(opening, top);
    }

    /// <summary>The callback runs before the next frame is drawn; the one it asks for runs after it.</summary>
    private void AwaitFrames(Opening opening, TopLevel top) =>
        top.RequestAnimationFrame(_ => top.RequestAnimationFrame(_ => Drawn(opening, top)));

    /// <summary>
    /// The page's first frame has drawn. An animation-frame callback runs at the start of a render pass, before that
    /// pass's layout, so a view or safe area that changed since (as a phone's does while it starts) would be in the facts
    /// but not yet in the page's size. So the line waits for this pass to finish: a job just below the render pass's
    /// priority runs once its layout and frame are done (Cornflower's finding, on its Pixel).
    /// </summary>
    private void Drawn(Opening opening, TopLevel top)
    {
        if (IsCurrent(opening, top))
            Dispatcher.UIThread.Post(() => Ready(opening, top), DispatcherPriority.Loaded);
    }

    /// <summary>
    /// The ready line, with the time to the page's first frame, and in Debug the cost line; or, if the view has no size
    /// yet, a wait for one first.
    /// <para>
    /// Avalonia's animation frames are the process's, not the view's: a frame request runs whether or not its top level
    /// renders or has a size. On Android the view is a 1 × 1 placeholder at scaling 1 until its surface is made and
    /// changed (Avalonia 12.1.4's <c>InvalidationAwareSurfaceView</c>), and the shell is in it from the activity's start,
    /// so on a slow start its frames come and go before then: a line read <c>view=1x1 … page=1x0</c>, render 0.0 (#37).
    /// So a page waits for its top level's size, and then for the frames after it: the resize lays the page out
    /// (<c>TopLevel.HandleResized</c>), and the next frames draw it at that size. A view that gets no size within
    /// <see cref="SizeWait"/> is reported as it is, its <c>view=</c> and <c>page=</c> saying so. Either way the time
    /// spent waiting is left out of the line's, as the platform's start before the shell is in a window is.
    /// </para>
    /// <para>
    /// A size means the view renders: Android's comes only from its surface, whose creation starts the view's rendering
    /// (<c>AvaloniaView.InternalView_SurfaceWindowCreated</c>) before the surface's change gives the top level its size
    /// and then its scaling, in one callback (<c>TopLevelImpl.SurfaceViewImpl.SurfaceChanged</c>).
    /// </para>
    /// </summary>
    private void Ready(Opening opening, TopLevel top)
    {
        if (!IsCurrent(opening, top))
            return;

        if (Unsized(top.ClientSize) && !opening.SizeWaitOver)
        {
            opening.AwaitSize(top, sized: () => AwaitFrames(opening, top), over: () => Ready(opening, top));
            return;
        }

        _opening = null;
        try
        {
            var opened = Stopwatch.GetElapsedTime(opening.Started);
            var ready = ReadyLine(opened, DeviceFacts.Of(top, opening.Page), opening.Title);
#if DEBUG
            var cost = opening.Cost(opened);
            // Shown before either line is written, so that a frame a tool takes after the ready line has it.
            if (ShowsCost)
                ShowCost(cost);
            Report(ready);
            Report($"{CostMarker} {cost}");
#else
            Report(ready);
#endif
        }
        finally
        {
            opening.Dispose();
        }
    }

    /// <summary>
    /// Whether a view has no room for a page: Android's placeholder before its surface (1 × 1 at scaling 1), or none.
    /// No real view is a pixel wide or high.
    /// </summary>
    internal static bool Unsized(Size view) => view.Width <= 1 || view.Height <= 1;

    /// <summary>
    /// Whether <paramref name="opening"/> is still the page being opened, in <paramref name="top"/>: frame requests and
    /// posted jobs can't be cancelled, and a page left, or a shell that has left its window since, says nothing.
    /// </summary>
    private bool IsCurrent(Opening opening, TopLevel top) => ReferenceEquals(opening, _opening) && ReferenceEquals(top, _top);

    /// <summary>
    /// Shows <paramref name="cost"/> in the footer over the page's foot, as a Debug build does once the page is ready;
    /// a test shows one in any build. Hidden again by a tap on it, or as the page is left.
    /// </summary>
    internal void ShowCost(string cost)
    {
        PerformanceLine.Text = cost;
        Footer.IsVisible = true;
    }

    /// <summary>A tap on the footer hides it, so that what it covers can be seen and pressed, until the next page.</summary>
    private void OnFooterTapped(object? sender, TappedEventArgs e)
    {
        Footer.IsVisible = false;
        e.Handled = true;
    }

    private void OnBack(object? sender, RoutedEventArgs e) => ShowIndex();

    /// <summary>The other variant, for the shell's whole top level, as an app's own switch would set it.</summary>
    private void OnVariant(object? sender, RoutedEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } top)
            top.RequestedThemeVariant = ActualThemeVariant == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
    }

    /// <summary>The switch says what it turns to.</summary>
    private void LabelVariant() => Variant.Content = ActualThemeVariant == ThemeVariant.Dark ? "Light" : "Dark";

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _top = TopLevel.GetTopLevel(this);
        // After every handler on the route, so that anything a page shows over itself goes back first.
        _backRequests = TopLevel.BackRequestedEvent.RouteFinished.Subscribe(new AnonymousObserver<RoutedEventArgs>(OnBackRequestFinished));
        AwaitFirstFrame();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _backRequests?.Dispose();
        _backRequests = null;
        _top = null;
        // A page still opening when its shell leaves the window is never reported: its time would include the gap.
        _opening?.Dispose();
        _opening = null;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// What's left of a back request once everything else has seen it: from a page, the list. On the list it's left
    /// alone, so that a phone leaves the app.
    /// </summary>
    private void OnBackRequestFinished(RoutedEventArgs e)
    {
        // RouteFinished is for every top level's request, so only this one's.
        if (e.Handled || !IsShowingPage || _top is null || e.Source is not Visual source || WindowOf(source) != _top)
            return;

        e.Handled = true;
        ShowIndex();
    }

    /// <summary>The window <paramref name="source"/> is in: for a popup, the window it was opened from.</summary>
    private static TopLevel? WindowOf(Visual source)
    {
        var top = TopLevel.GetTopLevel(source);
        while (top is PopupRoot popup && popup.ParentTopLevel is { } parent && parent != top)
            top = parent;
        return top;
    }

    /// <summary>
    /// A page being opened: what it is, what it's called, when it began, its wait for its view's size, and in Debug its
    /// cost.
    /// </summary>
    private sealed class Opening(Control page, string title, long started, TimeSpan sizeWait) : IDisposable
    {
        /// <summary>How long the page took to make, if it was shown before its shell was in a window; else null.</summary>
        private long? _making;

        /// <summary>The watch on the view's size, from the page's first wait for one until it's disposed; else null.</summary>
        private IDisposable? _sizeWatch;

        /// <summary>The end of the wait for a size, while one runs; else null.</summary>
        private IDisposable? _sizeDeadline;

        /// <summary>When the wait for a size began, while one runs; else null.</summary>
        private long? _waitingSince;

        /// <summary>What's left of the bound on the waits for a size, the shell's <see cref="SizeWait"/> as the page is shown.</summary>
        private TimeSpan _waitLeft = sizeWait;

        public Control Page { get; } = page;

        public string Title { get; } = title;

        /// <summary>When the page began, as the ready line counts it.</summary>
        public long Started { get; private set; } = started;

        /// <summary>The page is shown while its shell is in no window: remembers how long it took to make.</summary>
        public void ShownOutOfWindow() => _making = Stopwatch.GetTimestamp() - Started;

        /// <summary>The shell is in a window: a page shown before is counted from now, plus the time it took to make.</summary>
        public void InWindow()
        {
            if (_making is not { } making)
                return;
            Started = Stopwatch.GetTimestamp() - making;
            _making = null;
        }

        /// <summary>Whether the wait for a size has run out: the page is reported with the size it has.</summary>
        public bool SizeWaitOver => _waitLeft <= TimeSpan.Zero;

        /// <summary>
        /// Waits for <paramref name="top"/> to have a size: <paramref name="sized"/> once it gets one, or
        /// <paramref name="over"/> if what's left of the bound passes first. A view that loses its size again waits
        /// again, for what's left. The time waited is left out of the page's (<see cref="Started"/>).
        /// </summary>
        public void AwaitSize(TopLevel top, Action sized, Action over)
        {
            if (_waitingSince is not null || SizeWaitOver)
                return;

            _waitingSince = Stopwatch.GetTimestamp();
            _sizeDeadline = DispatcherTimer.RunOnce(() =>
            {
                if (!EndWait())
                    return;
                _waitLeft = TimeSpan.Zero;
                over();
            }, _waitLeft);
            // The observable gives the size it has at once: none, as the shell waits only then; if it had one, that would
            // end the wait at once, as a later one does.
            _sizeWatch ??= top.GetObservable(TopLevel.ClientSizeProperty).Subscribe(new AnonymousObserver<Size>(size =>
            {
                if (!Unsized(size) && EndWait())
                    sized();
            }));
        }

        /// <summary>Ends the wait for a size, if one runs, its time left out of the page's and taken from the bound.</summary>
        private bool EndWait()
        {
            if (_waitingSince is not { } since)
                return false;

            var now = Stopwatch.GetTimestamp();
            Started += now - since;
            _waitLeft = TimeSpan.FromTicks(Math.Max(0, (_waitLeft - Stopwatch.GetElapsedTime(since, now)).Ticks));
            _waitingSince = null;
            _sizeDeadline?.Dispose();
            _sizeDeadline = null;
            return true;
        }

        public void Dispose()
        {
            _sizeWatch?.Dispose();
            _sizeWatch = null;
            _sizeDeadline?.Dispose();
            _sizeDeadline = null;
#if DEBUG
            _performance?.Dispose();
            _performance = null;
#endif
        }

#if DEBUG
        /// <summary>Avalonia's layout and render times for this page; a process-wide listener, so disposed with it.</summary>
        private PerformanceMonitor? _performance;

        public void Measure() => _performance ??= new PerformanceMonitor();

        /// <summary>What the page cost to its first frame. Measured from <see cref="AwaitFirstFrame"/>, which comes first.</summary>
        public string Cost(TimeSpan opened) => _performance!.Read(Title, Page, opened);
#endif
    }
}
