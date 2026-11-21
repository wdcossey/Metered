using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;
using Avalonia.Rendering;

namespace Metered.Controls;

/// <summary>
/// The element of an input's icon parts (<c>ClearIcon</c>, <c>MaskToggleShowIcon</c>, <c>MaskToggleHideIcon</c>): one
/// of Base Web's icons, an SVG of one path. <see cref="Data"/> is the path, in the coordinates of
/// <see cref="ViewBox"/>, drawn over the whole box as an SVG with that <c>viewBox</c> is (scaled to fit, centred:
/// <c>preserveAspectRatio</c>'s default) in <see cref="Foreground"/>, inherited as CSS's <c>currentColor</c> is. It is
/// radio-v2's mark (<see cref="RadioMark"/>) underneath, for CSS's <c>outline</c>: the clear icon's focus ring.
/// </summary>
/// <remarks>
/// Metered has no icon set: the three paths Input needs are Base Web's own (<c>icon/delete-alt.js</c>,
/// <c>icon/show.js</c>, <c>icon/hide.js</c>, MIT), written into the part themes by <c>tools/tokens/input.js</c>.
/// A focusable icon (the clear icon, Base Web's <c>role="button"</c> with <c>tabIndex={0}</c>) is a button to UI
/// Automation, invoked as its click.
/// </remarks>
public class InputIcon : RadioMark, ICustomHitTest
{
    public static readonly StyledProperty<Geometry?> DataProperty =
        AvaloniaProperty.Register<InputIcon, Geometry?>(nameof(Data));

    public static readonly StyledProperty<Rect> ViewBoxProperty =
        AvaloniaProperty.Register<InputIcon, Rect>(nameof(ViewBox));

    public static readonly StyledProperty<IBrush?> ForegroundProperty =
        TextElement.ForegroundProperty.AddOwner<InputIcon>();

    static InputIcon()
    {
        AffectsRender<InputIcon>(DataProperty, ViewBoxProperty, ForegroundProperty);
    }

    /// <summary>The icon's path, in the coordinates of <see cref="ViewBox"/>; nothing is drawn when null.</summary>
    public Geometry? Data { get => GetValue(DataProperty); set => SetValue(DataProperty, value); }

    /// <summary>The SVG's <c>viewBox</c>: the part of <see cref="Data"/>'s plane the box shows.</summary>
    public Rect ViewBox { get => GetValue(ViewBoxProperty); set => SetValue(ViewBoxProperty, value); }

    /// <summary>The fill: CSS's <c>currentColor</c>, the colour the icon's parent passes down.</summary>
    public IBrush? Foreground { get => GetValue(ForegroundProperty); set => SetValue(ForegroundProperty, value); }

    /// <summary>What an invoke does, for UI Automation: the owner's click (the clear icon's clear).</summary>
    internal Action? Invoked { get; set; }

    /// <summary>Where <see cref="ViewBox"/> is drawn: scaled to fit the box, centred.</summary>
    internal Rect ViewRect(Rect box)
    {
        var view = ViewBox;
        if (view.Width <= 0 || view.Height <= 0) return default;
        var scale = Math.Min(box.Width / view.Width, box.Height / view.Height);
        var size = new Size(view.Width * scale, view.Height * scale);
        return new Rect(box.Center.X - size.Width / 2, box.Center.Y - size.Height / 2, size.Width, size.Height);
    }

    protected override void RenderImage(DrawingContext context, RoundedRect border, Rect padding)
    {
        if (Data is not { } data || Foreground is not { } fill) return;
        var view = ViewBox;
        var at = ViewRect(padding);
        if (at.Width <= 0 || at.Height <= 0) return;
        using (context.PushTransform(Matrix.CreateTranslation(-view.X, -view.Y)
            * Matrix.CreateScale(at.Width / view.Width, at.Height / view.Height)
            * Matrix.CreateTranslation(at.X, at.Y)))
        {
            context.DrawGeometry(fill, null, data);
        }
    }

    /// <summary>The whole box takes the pointer, as an <c>&lt;svg&gt;</c> element's box does, drawn or not.</summary>
    public bool HitTest(Point point) => new Rect(Bounds.Size).Contains(point);

    protected override AutomationPeer OnCreateAutomationPeer() => new InputIconAutomationPeer(this);
}

/// <summary>
/// An <see cref="InputIcon"/> to UI Automation: a focusable one (the clear icon, Base Web's <c>role="button"</c>) a
/// button, named by its <c>AutomationProperties.Name</c> (Base Web's <c>aria-label</c>), that invokes its click; any
/// other an image.
/// </summary>
internal sealed class InputIconAutomationPeer(InputIcon owner) : ControlAutomationPeer(owner), IInvokeProvider
{
    private new InputIcon Owner => (InputIcon)base.Owner;

    protected override AutomationControlType GetAutomationControlTypeCore() =>
        Owner.Focusable ? AutomationControlType.Button : AutomationControlType.Image;

    // ControlAutomationPeer's own leave a control out of UI Automation's control view (LESSONS: Metered #56).
    protected override bool IsControlElementCore() => Owner.IsVisible;

    protected override bool IsContentElementCore() => Owner.IsVisible && Owner.Focusable;

    public void Invoke()
    {
        if (!IsEnabled()) throw new ElementNotEnabledException();
        Owner.Invoked?.Invoke();
    }
}
