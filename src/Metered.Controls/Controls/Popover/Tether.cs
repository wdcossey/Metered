using Avalonia;

namespace Metered.Controls;

/// <summary>Where a tethered body goes: the placement chosen, its margin box's top-left and its arrow's offset.</summary>
/// <param name="Placement">The placement popper.js chose (never <see cref="PopoverPlacement.Auto"/>).</param>
/// <param name="Offset">The top-left of the body's margin box, in the coordinates of the anchor's rectangle, floored as
/// Base Web's <c>parsePopperOffset</c> floors it.</param>
/// <param name="ArrowOffset">The arrow's offset along the side it is on (its <c>left</c> on the top and bottom, its
/// <c>top</c> on the left and right; the other is 0), or null with no arrow.</param>
internal readonly record struct TetherResult(PopoverPlacement Placement, Point Offset, Point? ArrowOffset);

/// <summary>
/// Base Web's <c>TetherBehavior</c> (<c>layer/tether.js</c>): where a body goes against its anchor, as popper.js 1.16.1
/// computes it with the options Base Web gives it, ported. For everything Base Web tethers to an anchor (the Popover
/// here).
/// </summary>
/// <remarks>
/// <para>popper.js's first update, its modifiers in their order: <c>auto</c> (the side with the most room that the body
/// fits, else the most room), <c>shift</c> (the placement's start or end), <c>offset</c> (0, a no-op),
/// <c>preventOverflow</c> (kept inside the boundaries, padding 0: Base Web's; off for <c>ignoreBoundary</c>),
/// <c>keepTogether</c>, <c>arrow</c> and <c>flip</c> (to the opposite side when it overlaps the anchor or leaves the
/// viewport less 5 pixels, then the modifiers before it again). Each quirk is popper.js's, kept: a side read after a
/// modifier moved the other is the one computed before it, the body's size is its <c>offsetWidth</c> (rounded) plus its
/// margins, and <c>auto</c>'s areas are the viewport's unpadded width and height.</para>
/// <para>Two boundaries, as popper.js's: the viewport, which <c>auto</c> and <c>flip</c> keep to (the window's client
/// area), and <c>preventOverflow</c>'s, the anchor's scroll parent (the box a scroll viewer shows, as popper.js's
/// scrolling element's), or with none the document, which in an app's window that doesn't scroll is the window.
/// tools/reference/tether.js runs popper.js itself on the same boxes; the tests compare.</para>
/// </remarks>
internal static class Tether
{
    /// <summary>popper.js's <c>flip</c> padding, its default (Base Web doesn't set it), which <c>auto</c> uses too.</summary>
    private const double FlipPadding = 5;

    /// <param name="anchor">The anchor's border box (popper.js's reference: its bounding rectangle).</param>
    /// <param name="body">The body's border box size.</param>
    /// <param name="margin">The body's margins as popper.js reads them (its computed style).</param>
    /// <param name="arrow">The arrow's box size along its side (Base Web's <c>ARROW_WIDTH</c>), or null with none.</param>
    /// <param name="placement">The placement asked for.</param>
    /// <param name="viewport">The viewport: <c>auto</c>'s and <c>flip</c>'s boundaries.</param>
    /// <param name="preventOverflow">popper.js's <c>preventOverflow</c>: Base Web's <c>!ignoreBoundary</c>.</param>
    /// <param name="boundaries"><c>preventOverflow</c>'s boundaries: the viewport unless given.</param>
    public static TetherResult Compute(Rect anchor, Size body, Thickness margin, double? arrow, PopoverPlacement placement, Rect viewport,
        bool preventOverflow, Rect? boundaries = null)
    {
        // offsetWidth and offsetHeight are whole pixels; clientWidth and clientHeight too (the body has no border).
        var width = Whole(body.Width);
        var height = Whole(body.Height);
        var outer = new Size(width + margin.Left + margin.Right, height + margin.Top + margin.Bottom);
        var reference = Box.Of(anchor);

        var (side, variation) = Split(placement);
        if (placement == PopoverPlacement.Auto) side = Auto(reference, width, height, viewport);
        var data = new Data
        {
            Side = side,
            Variation = variation,
            Original = (side, variation),
            Reference = reference,
            Popper = PopperOffsets(outer, reference, side),
            Outer = outer,
            Margin = margin,
            Arrow = arrow,
            Viewport = viewport,
            Boundaries = boundaries ?? viewport,
            PreventOverflow = preventOverflow,
        };

        Run(data, untilFlip: false);
        var offset = new Point(Math.Floor(data.Popper.Left), Math.Floor(data.Popper.Top));
        Point? arrowOffset = arrow is null ? null : new Point(Math.Floor(data.ArrowLeft), Math.Floor(data.ArrowTop));
        return new TetherResult(Join(data.Side, data.Variation), offset, arrowOffset);
    }

    /// <summary>The side of a placement (<c>top</c> of <c>topLeft</c>), Base Web's <c>splitPlacement</c>'s first word.</summary>
    public static Side SideOf(PopoverPlacement placement) => Split(placement).Side;

    public enum Side
    {
        Top,
        Right,
        Bottom,
        Left,
    }

    private enum Variation
    {
        None,
        Start,
        End,
    }

    private static double Whole(double v) => Math.Round(v, MidpointRounding.AwayFromZero);

    /// <summary>Base Web's <c>toPopperPlacement</c>: <c>Top</c> and <c>Left</c> ends are <c>-start</c>, the others <c>-end</c>.</summary>
    private static (Side Side, Variation Variation) Split(PopoverPlacement p) => p switch
    {
        PopoverPlacement.TopLeft => (Side.Top, Variation.Start),
        PopoverPlacement.Top => (Side.Top, Variation.None),
        PopoverPlacement.TopRight => (Side.Top, Variation.End),
        PopoverPlacement.RightTop => (Side.Right, Variation.Start),
        PopoverPlacement.Right => (Side.Right, Variation.None),
        PopoverPlacement.RightBottom => (Side.Right, Variation.End),
        PopoverPlacement.BottomRight => (Side.Bottom, Variation.End),
        PopoverPlacement.Bottom => (Side.Bottom, Variation.None),
        PopoverPlacement.BottomLeft => (Side.Bottom, Variation.Start),
        PopoverPlacement.LeftBottom => (Side.Left, Variation.End),
        PopoverPlacement.Left => (Side.Left, Variation.None),
        PopoverPlacement.LeftTop => (Side.Left, Variation.Start),
        _ => (Side.Top, Variation.None),
    };

    /// <summary>Base Web's <c>fromPopperPlacement</c>.</summary>
    private static PopoverPlacement Join(Side side, Variation v) => (side, v) switch
    {
        (Side.Top, Variation.Start) => PopoverPlacement.TopLeft,
        (Side.Top, Variation.End) => PopoverPlacement.TopRight,
        (Side.Top, _) => PopoverPlacement.Top,
        (Side.Right, Variation.Start) => PopoverPlacement.RightTop,
        (Side.Right, Variation.End) => PopoverPlacement.RightBottom,
        (Side.Right, _) => PopoverPlacement.Right,
        (Side.Bottom, Variation.Start) => PopoverPlacement.BottomLeft,
        (Side.Bottom, Variation.End) => PopoverPlacement.BottomRight,
        (Side.Bottom, _) => PopoverPlacement.Bottom,
        (Side.Left, Variation.Start) => PopoverPlacement.LeftTop,
        (Side.Left, Variation.End) => PopoverPlacement.LeftBottom,
        _ => PopoverPlacement.Left,
    };

    private static Side Opposite(Side s) => s switch
    {
        Side.Top => Side.Bottom,
        Side.Bottom => Side.Top,
        Side.Left => Side.Right,
        _ => Side.Left,
    };

    /// <summary>A popper.js offsets object: left, top, width, height, and right and bottom as last computed.</summary>
    private sealed class Box
    {
        public double Left, Top, Width, Height, Right, Bottom;

        public static Box Of(Rect r) => new() { Left = r.X, Top = r.Y, Width = r.Width, Height = r.Height, Right = r.Right, Bottom = r.Bottom };

        /// <summary>popper.js's <c>getClientRect</c>: right and bottom from left, top, width and height.</summary>
        public void Complete()
        {
            Right = Left + Width;
            Bottom = Top + Height;
        }
    }

    private sealed class Data
    {
        public Side Side;
        public Variation Variation;
        public (Side, Variation) Original;
        public bool Flipped;
        public required Box Reference;
        public required Box Popper;
        public Size Outer;
        public Thickness Margin;
        public double? Arrow;
        public Rect Viewport;
        public Rect Boundaries;
        public bool PreventOverflow;
        public double ArrowLeft, ArrowTop;
    }

    /// <summary>popper.js's <c>computeAutoPlacement</c> with the flip modifier's viewport and padding.</summary>
    private static Side Auto(Box reference, double clientWidth, double clientHeight, Rect viewport)
    {
        var b = Bounds(viewport, FlipPadding);
        // The areas' width and height are the viewport's own: getClientRect's, which the padding doesn't change.
        var rects = new (Side Key, double Width, double Height)[]
        {
            (Side.Top, viewport.Width, reference.Top - b.Top),
            (Side.Right, b.Right - reference.Right, viewport.Height),
            (Side.Bottom, viewport.Width, b.Bottom - reference.Bottom),
            (Side.Left, reference.Left - b.Left, viewport.Height),
        };
        // JavaScript's sort is stable: equal areas keep this order.
        var sorted = rects.OrderByDescending(r => r.Width * r.Height).ToList();
        var fits = sorted.Where(r => r.Width >= clientWidth && r.Height >= clientHeight).ToList();
        return (fits.Count > 0 ? fits[0] : sorted[0]).Key;
    }

    private static Box Bounds(Rect viewport, double padding) => new()
    {
        Left = viewport.Left + padding,
        Top = viewport.Top + padding,
        Right = viewport.Right - padding,
        Bottom = viewport.Bottom - padding,
        Width = viewport.Width,
        Height = viewport.Height,
    };

    /// <summary>popper.js's <c>getPopperOffsets</c>: the body against the anchor's side, centred along it.</summary>
    private static Box PopperOffsets(Size outer, Box reference, Side side)
    {
        var p = new Box { Width = outer.Width, Height = outer.Height };
        var horizontal = side is Side.Left or Side.Right;
        if (horizontal)
        {
            p.Top = reference.Top + reference.Height / 2 - outer.Height / 2;
            p.Left = side == Side.Left ? reference.Left - outer.Width : reference.Right;
        }
        else
        {
            p.Left = reference.Left + reference.Width / 2 - outer.Width / 2;
            p.Top = side == Side.Top ? reference.Top - outer.Height : reference.Bottom;
        }

        return p;
    }

    /// <summary>popper.js's <c>runModifiers</c>: right and bottom made whole before each.</summary>
    private static void Run(Data d, bool untilFlip)
    {
        Step(d, Shift);
        // offset: 0, Base Web gives none.
        if (d.PreventOverflow) Step(d, PreventOverflow);
        Step(d, KeepTogether);
        if (d.Arrow is not null) Step(d, Arrow);
        if (untilFlip) return;
        Step(d, Flip);
        // inner is off; hide changes no offset; computeStyle and applyStyle aren't read (Base Web's applyReactStyle takes
        // data.offsets).
    }

    private static void Step(Data d, Action<Data> modifier)
    {
        d.Popper.Complete();
        d.Reference.Complete();
        modifier(d);
    }

    private static void Shift(Data d)
    {
        if (d.Variation == Variation.None) return;
        var vertical = d.Side is Side.Top or Side.Bottom;
        if (vertical)
        {
            d.Popper.Left = d.Variation == Variation.Start ? d.Reference.Left : d.Reference.Left + d.Reference.Width - d.Popper.Width;
        }
        else
        {
            d.Popper.Top = d.Variation == Variation.Start ? d.Reference.Top : d.Reference.Top + d.Reference.Height - d.Popper.Height;
        }
    }

    private static void PreventOverflow(Data d)
    {
        var b = Bounds(d.Boundaries, 0);
        var p = d.Popper;
        // priority: left, right, top, bottom. A secondary reads the right or bottom computed before the modifier.
        if (p.Left < b.Left) p.Left = Math.Max(p.Left, b.Left);
        if (p.Right > b.Right) p.Left = Math.Min(p.Left, b.Right - p.Width);
        if (p.Top < b.Top) p.Top = Math.Max(p.Top, b.Top);
        if (p.Bottom > b.Bottom) p.Top = Math.Min(p.Top, b.Bottom - p.Height);
    }

    private static void KeepTogether(Data d)
    {
        var p = d.Popper;
        var r = d.Reference;
        if (d.Side is Side.Top or Side.Bottom)
        {
            if (p.Right < Math.Floor(r.Left)) p.Left = Math.Floor(r.Left) - p.Width;
            if (p.Left > Math.Floor(r.Right)) p.Left = Math.Floor(r.Right);
        }
        else
        {
            if (p.Bottom < Math.Floor(r.Top)) p.Top = Math.Floor(r.Top) - p.Height;
            if (p.Top > Math.Floor(r.Bottom)) p.Top = Math.Floor(r.Bottom);
        }
    }

    private static void Arrow(Data d)
    {
        var size = d.Arrow!.Value;
        var p = d.Popper;
        var r = d.Reference;
        var vertical = d.Side is Side.Left or Side.Right;
        // side: left (or top), opSide: right (or bottom), len: width (or height).
        double PSide() => vertical ? p.Top : p.Left;
        void SetPSide(double v) { if (vertical) p.Top = v; else p.Left = v; }
        var pOp = vertical ? p.Bottom : p.Right;
        var rSide = vertical ? r.Top : r.Left;
        var rOp = vertical ? r.Bottom : r.Right;
        var rLen = vertical ? r.Height : r.Width;
        var pLen = vertical ? p.Height : p.Width;

        if (rOp - size < PSide()) SetPSide(PSide() - (PSide() - (rOp - size)));
        if (rSide + size > pOp) SetPSide(PSide() + (rSide + size - pOp));
        p.Complete();

        var center = rSide + rLen / 2 - size / 2;
        var marginSide = vertical ? d.Margin.Top : d.Margin.Left;
        var value = center - PSide() - marginSide;
        value = Math.Max(Math.Min(pLen - size, value), 0);
        // Math.round: halves up.
        var rounded = Math.Floor(value + 0.5);
        if (vertical) { d.ArrowTop = rounded; d.ArrowLeft = 0; }
        else { d.ArrowLeft = rounded; d.ArrowTop = 0; }
    }

    private static void Flip(Data d)
    {
        if (d.Flipped && (d.Side, d.Variation) == d.Original) return;
        var b = Bounds(d.Viewport, FlipPadding);
        var p = d.Popper;
        var r = d.Reference;
        var side = d.Side;
        Func<double, double> f = Math.Floor;
        var overlapsRef = (side == Side.Left && f(p.Right) > f(r.Left)) || (side == Side.Right && f(p.Left) < f(r.Right))
            || (side == Side.Top && f(p.Bottom) > f(r.Top)) || (side == Side.Bottom && f(p.Top) < f(r.Bottom));
        var overflows = (side == Side.Left && f(p.Left) < f(b.Left)) || (side == Side.Right && f(p.Right) > f(b.Right))
            || (side == Side.Top && f(p.Top) < f(b.Top)) || (side == Side.Bottom && f(p.Bottom) > f(b.Bottom));
        if (!overlapsRef && !overflows) return;

        d.Flipped = true;
        d.Side = Opposite(side);
        var moved = PopperOffsets(d.Outer, r, d.Side);
        p.Left = moved.Left;
        p.Top = moved.Top;
        p.Width = moved.Width;
        p.Height = moved.Height;
        Run(d, untilFlip: true);
    }
}
