using System.Text.Json;
using Metered.Controls;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own popover gives (tools/reference/popover.js, written by tools/reference/cases.js): its parts' style
/// functions on the light and dark themes, its helpers, constants and default props, and where popper.js puts the body
/// and arrow (tools/reference/tether.js runs popper.js itself), in Avalonia's units.
/// </summary>
internal static class PopoverOracle
{
    public static PopoverOracleFile Value { get; } = Load();

    private static PopoverOracleFile Load()
    {
        using var stream = typeof(PopoverOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.popover.oracle.json")
            ?? throw new InvalidOperationException("popover.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<PopoverOracleFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>Base Web's placement name (<c>topLeft</c>) as Metered's.</summary>
    public static PopoverPlacement Placement(string name) => Enum.Parse<PopoverPlacement>(name, ignoreCase: true);

    /// <summary>The body's margin by placement, arrow and popoverMargin, as Avalonia's thickness.</summary>
    public static Avalonia.Thickness Margin(string placement, bool showArrow, double popoverMargin)
    {
        var c = Value.Body.Single(b => b.Placement == placement && b.ShowArrow == showArrow && b.PopoverMargin == popoverMargin);
        return new Avalonia.Thickness(c.Margin[0], c.Margin[1], c.Margin[2], c.Margin[3]);
    }
}

internal sealed record PopoverOracleFile(
    string Baseui, PopoverConstants Constants, PopoverDefaults Defaults, double[] Padding, PopoverColours[] Colours, PopoverBodyCase[] Body,
    PopoverMotionCase[] Motion, PopoverArrowFacts Arrow, PopoverTetherFile Tether);

internal sealed record PopoverConstants(double PopoverMargin, double ArrowSize, double ArrowWidth, int AnimateInTime, int AnimateOutTime);

internal sealed record PopoverDefaults(
    string AccessibilityType, bool FocusLock, bool AutoFocus, bool ReturnFocus, bool IgnoreBoundary, int OnMouseEnterDelay, int OnMouseLeaveDelay,
    string Placement, bool ShowArrow, string TriggerType, bool DismissOnClickOutside, bool DismissOnEsc, double PopoverMargin);

internal sealed record PopoverColours(string Theme, PopoverBox Body, PopoverBox Arrow, PopoverBox Inner);

internal sealed record PopoverBox(string Background, double? Radius, string? BoxShadow, string? Foreground);

internal sealed record PopoverBodyCase(string Placement, bool ShowArrow, double PopoverMargin, double[] Margin, double[] Start, double[] End)
{
    public override string ToString() => $"{Placement}{(ShowArrow ? " arrow" : "")} margin {PopoverMargin}";
}

internal sealed record PopoverMotionCase(bool IsOpen, bool IsAnimating, int AnimateOutTime, bool IsHoverTrigger, double Opacity, double Seconds, double[] Easing, PopoverHitTest? HitTest)
{
    public override string ToString() => $"{(IsOpen ? "open" : "closed")}{(IsAnimating ? " animating" : "")} out {AnimateOutTime}{(IsHoverTrigger ? " hover" : "")}";
}

internal sealed record PopoverHitTest(double Seconds, JsonElement[][] Frames);

internal sealed record PopoverArrowFacts(double Width, double Height, double Rotate, PopoverArrowCase[] Cases);

internal sealed record PopoverArrowCase(string Placement, double? Left, double? Top, double? Right, double? Bottom);

internal sealed record PopoverTetherFile(string Popper, PopoverView View, double ArrowWidth, PopoverTetherCase[] Cases);

internal sealed record PopoverView(double Width, double Height);

internal sealed record PopoverTetherCase(
    string Anchor, double X, double Y, double Width, double Height, double BodyWidth, double BodyHeight, string Placement, bool ShowArrow,
    double PopoverMargin, bool IgnoreBoundary, string Result, double Left, double Top, double? ArrowLeft, double? ArrowTop,
    string SteadyResult, double SteadyLeft, double SteadyTop, double? SteadyArrowLeft, double? SteadyArrowTop)
{
    public override string ToString() =>
        $"{Anchor} {BodyWidth}x{BodyHeight} {Placement}{(ShowArrow ? " arrow" : "")} margin {PopoverMargin}{(IgnoreBoundary ? " ignoreBoundary" : "")}";
}
