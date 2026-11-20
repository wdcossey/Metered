using Avalonia;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.Media;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Spinner</c> (baseui 18.2.0, <c>spinner/</c>): a ring that turns, its one part <c>Spinner</c> Base
/// Web's <c>StyledSpinner</c>, with its props and overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>$size</c> is <see cref="Size"/>, <c>$borderWidth</c> <see cref="BorderWidth"/> and
/// <c>$color</c> <see cref="Color"/>. The spinner reads them as Base Web's style function does into state
/// pseudoclasses, set on the spinner and on its part, which the part's theme (<c>Spinner.StyledSpinner</c>) reads:</para>
/// <list type="bullet">
/// <item><c>:size-small</c>, <c>:size-medium</c>, <c>:size-large</c> for a size; <c>:size-&lt;token&gt;</c>
/// (<c>:size-scale1000</c>) for a sizing token's name the theme has; <c>:size-pixels</c> for a length, which the theme
/// takes from <see cref="SpinnerLength.Pixels"/>; <c>:size-invalid</c> for none of these (<c>NaNpx</c>, or a negative
/// length), which a browser drops.</item>
/// <item><c>:border-width-…</c> the same for the border. Base Web looks the border up by <c>$borderWidth || $size</c>,
/// so an unset or zero <see cref="BorderWidth"/> takes a named <see cref="Size"/>'s border; with a size that isn't a
/// name, an unset one is <c>NaNpx</c>, which a browser drops for CSS's <c>medium</c>: <c>:border-width-invalid</c>.</item>
/// <item><c>:has-color</c> when <see cref="Color"/> is given, which the theme then takes for the top of the ring.</item>
/// </list>
/// </remarks>
[TemplatePart(SpinnerPart, typeof(StyledElement))]
[PseudoClasses(":size-small", ":size-medium", ":size-large", ":size-pixels", ":size-invalid",
    ":border-width-small", ":border-width-medium", ":border-width-large", ":border-width-pixels", ":border-width-invalid",
    ":has-color")]
public class Spinner : TemplatedControl
{
    public const string SpinnerPart = "Spinner";

    public static readonly StyledProperty<SpinnerLength> SizeProperty =
        AvaloniaProperty.Register<Spinner, SpinnerLength>(nameof(Size), SpinnerSize.Medium);

    public static readonly StyledProperty<SpinnerLength> BorderWidthProperty =
        AvaloniaProperty.Register<Spinner, SpinnerLength>(nameof(BorderWidth));

    public static readonly StyledProperty<IBrush?> ColorProperty =
        AvaloniaProperty.Register<Spinner, IBrush?>(nameof(Color));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Spinner, OverrideCollection?>(nameof(Overrides));

    private readonly Parts _parts;
    private string? _sizeState;
    private string? _borderState;

    public Spinner()
    {
        _parts = new Parts(this, "Spinner", [SpinnerPart]);
        UpdateLengths();
        SetState(":has-color", Color is not null);
        // Whether text is a sizing token is the theme's to say: read again when the resources the spinner sees change.
        ResourcesChanged += (_, _) => UpdateLengths();
    }

    /// <summary>Base Web's <c>$size</c>: the box inside the ring's border. Medium by default.</summary>
    public SpinnerLength Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>$borderWidth</c>: the ring's width. Unset by default, which takes a named size's.</summary>
    public SpinnerLength BorderWidth { get => GetValue(BorderWidthProperty); set => SetValue(BorderWidthProperty, value); }

    /// <summary>Base Web's <c>$color</c>: the turning quarter's colour. Unset by default: <c>colors.contentAccent</c>.</summary>
    public IBrush? Color { get => GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

    /// <summary>
    /// The part's overrides: Base Web's spinner is one styled component, restyled with <c>withStyle</c> or
    /// <c>$style</c>; here by part, <c>Spinner</c>.
    /// </summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
    }

    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e)
    {
        base.OnAttachedToLogicalTree(e);
        UpdateLengths();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == SizeProperty || p == BorderWidthProperty) UpdateLengths();
        else if (p == ColorProperty) SetState(":has-color", Color is not null);
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    /// <summary>Base Web's <c>role="progressbar"</c> and <c>aria-label="loading"</c>.</summary>
    protected override AutomationPeer OnCreateAutomationPeer() => new SpinnerAutomationPeer(this);

    /// <summary>The style function's lookups, as states: the box by <c>$size</c>, the border by <c>$borderWidth || $size</c>.</summary>
    private void UpdateLengths()
    {
        // $size = SIZE.medium: JavaScript's default for an undefined parameter.
        var size = Size.IsSet ? Size : SpinnerSize.Medium;
        var border = !BorderWidth.IsTruthy && size.Size is { } named ? new SpinnerLength(named) : BorderWidth;
        Swap(ref _sizeState, ":size-" + Kind(size));
        Swap(ref _borderState, ":border-width-" + Kind(border));
    }

    /// <summary>
    /// A size's name; else a sizing token's name the theme has (<c>$theme.sizing[value]</c>); else <c>pixels</c> for
    /// <c>parseInt</c>'s length; else <c>invalid</c>, for <c>NaNpx</c> or a negative length, which a browser drops.
    /// </summary>
    private string Kind(SpinnerLength value)
    {
        if (value.Size is { } size) return size.ToString().ToLowerInvariant();
        if (value.Text is { } text && this.TryFindResource("sizing." + text, ActualThemeVariant, out var token) && token is double) return text;
        var pixels = value.Pixels;
        return double.IsFinite(pixels) && pixels >= 0 ? "pixels" : "invalid";
    }

    private void Swap(ref string? current, string next)
    {
        if (current == next) return;
        if (current is not null) SetState(current, false);
        current = next;
        SetState(next, true);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }
}
