using System.Text.Json;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own tooltip gives (tools/reference/tooltip.js, written by tools/reference/cases.js): its parts' style
/// functions (tooltip/styled-components.js, over the popover's) on the light and dark themes, and its default props as
/// StatefulTooltip and Tooltip resolve them, in Avalonia's units. Where popper.js puts it is the Popover's
/// (PopoverOracle's tether cases).
/// </summary>
internal static class TooltipOracle
{
    public static TooltipOracleFile Value { get; } = Load();

    private static TooltipOracleFile Load()
    {
        using var stream = typeof(TooltipOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.tooltip.oracle.json")
            ?? throw new InvalidOperationException("tooltip.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<TooltipOracleFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
}

internal sealed record TooltipOracleFile(
    string Baseui, TooltipConstants Constants, TooltipDefaultsFile Defaults, TooltipColours[] Colours, PopoverBodyCase[] Body, TooltipMotionCase[] Motion);

internal sealed record TooltipConstants(double ArrowSize, double ArrowWidth, int AnimateInTime);

/// <summary>StatefulTooltip's resolved defaults, and a (controlled) Tooltip's.</summary>
internal sealed record TooltipDefaultsFile(TooltipDefaults Stateful, TooltipDefaults Tooltip);

internal sealed record TooltipDefaults(
    string AccessibilityType, bool FocusLock, bool AutoFocus, bool ReturnFocus, bool IgnoreBoundary, int OnMouseEnterDelay, int OnMouseLeaveDelay,
    string Placement, bool ShowArrow, string TriggerType, bool? DismissOnClickOutside, bool? DismissOnEsc, double PopoverMargin, int? AnimateOutTime);

internal sealed record TooltipColours(string Theme, PopoverBox Body, PopoverBox Arrow, TooltipInner Inner);

internal sealed record TooltipInner(string Background, double Radius, string Foreground, double[] Padding, TooltipFont Font);

internal sealed record TooltipFont(double FontSize, string FontWeight, double LineHeight, string Family);

internal sealed record TooltipMotionCase(
    bool IsOpen, bool IsAnimating, int AnimateOutTime, bool IsHoverTrigger, double Opacity, string[] Properties, double Seconds, double[] Easing, PopoverHitTest? HitTest)
{
    public override string ToString() => $"{(IsOpen ? "open" : "closed")}{(IsAnimating ? " animating" : "")} out {AnimateOutTime}{(IsHoverTrigger ? " hover" : "")}";
}
