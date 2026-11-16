using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using AvaloniaButton = Avalonia.Controls.Button;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Button</c> (baseui 18.2.0, <c>button/button.tsx</c>): an Avalonia
/// <see cref="AvaloniaButton"/> with Base Web's props, its named parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Base Web's <c>disabled</c> is <c>IsEnabled</c>; <c>onClick</c> is <c>Click</c> and <c>Command</c>;
/// <c>children</c> is <c>Content</c>.</para>
/// <para>The state pseudoclasses, set on the button and on each part, are Base Web's <c>$</c>
/// props: <c>:kind-*</c>, <c>:size-*</c> (the four sizes by their newer names), <c>:shape-*</c>
/// (<c>rectangular</c> as <c>default</c>, <c>rounded</c> as <c>pill</c>), <c>:icon-shape</c>
/// (square, circle, round), <c>:fixed-shape</c> (square, circle), <c>:width-fill</c>,
/// <c>:hit-tap</c>, <c>:background-safe</c>, <c>:is-loading</c>, <c>:is-selected</c>, and
/// CSS's <c>:hover</c> (a mouse over it: Base Web's hover styles apply only where the pointer
/// hovers), <c>:active</c> (pressed) and <c>:focus-visible</c>. <c>:disabled</c> is Avalonia's own.
/// <c>:loaded</c> is set once the button is loaded, so that its background's transition doesn't
/// run on its first appearance, as CSS's doesn't.</para>
/// </remarks>
[TemplatePart(BaseButtonPart, typeof(StyledElement))]
[TemplatePart(StartEnhancerPart, typeof(StyledElement))]
[TemplatePart(EndEnhancerPart, typeof(StyledElement))]
[TemplatePart(StartEnhancerButtonContentContainerPart, typeof(StyledElement))]
[TemplatePart(LoadingSpinnerContainerPart, typeof(StyledElement))]
[TemplatePart(LoadingSpinnerPart, typeof(StyledElement))]
[PseudoClasses(":loaded", ":hover", ":active", ":focus-visible", ":is-loading", ":is-selected", ":background-safe", ":width-fill", ":hit-tap", ":icon-shape", ":fixed-shape")]
public class Button : AvaloniaButton
{
    public const string BaseButtonPart = "BaseButton";
    public const string StartEnhancerPart = "StartEnhancer";
    public const string EndEnhancerPart = "EndEnhancer";
    public const string StartEnhancerButtonContentContainerPart = "StartEnhancerButtonContentContainer";
    public const string LoadingSpinnerContainerPart = "LoadingSpinnerContainer";
    public const string LoadingSpinnerPart = "LoadingSpinner";

    public static readonly StyledProperty<ButtonKind> KindProperty =
        AvaloniaProperty.Register<Button, ButtonKind>(nameof(Kind), ButtonKind.Primary);

    public static readonly StyledProperty<ButtonSize> SizeProperty =
        AvaloniaProperty.Register<Button, ButtonSize>(nameof(Size), ButtonSize.Medium);

    public static readonly StyledProperty<ButtonShape> ShapeProperty =
        AvaloniaProperty.Register<Button, ButtonShape>(nameof(Shape), ButtonShape.Rectangular);

    public static readonly StyledProperty<ButtonWidthType> WidthTypeProperty =
        AvaloniaProperty.Register<Button, ButtonWidthType>(nameof(WidthType), ButtonWidthType.Hug);

    public static readonly StyledProperty<ButtonMinHitArea> MinHitAreaProperty =
        AvaloniaProperty.Register<Button, ButtonMinHitArea>(nameof(MinHitArea), ButtonMinHitArea.Click);

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<Button, bool>(nameof(IsLoading));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<Button, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> BackgroundSafeProperty =
        AvaloniaProperty.Register<Button, bool>(nameof(BackgroundSafe));

    public static readonly StyledProperty<object?> StartEnhancerProperty =
        AvaloniaProperty.Register<Button, object?>(nameof(StartEnhancer));

    public static readonly StyledProperty<object?> EndEnhancerProperty =
        AvaloniaProperty.Register<Button, object?>(nameof(EndEnhancer));

    public static readonly StyledProperty<OverrideCollection?> OverridesProperty =
        AvaloniaProperty.Register<Button, OverrideCollection?>(nameof(Overrides));

    public static readonly DirectProperty<Button, double> ArtworkSizeProperty =
        AvaloniaProperty.RegisterDirect<Button, double>(nameof(ArtworkSize), b => b.ArtworkSize);

    private static readonly string[] Kinds = Enum.GetNames<ButtonKind>().Select(n => ":kind-" + char.ToLowerInvariant(n[0]) + n[1..]).ToArray();
    private static readonly string[] Sizes = [":size-xsmall", ":size-small", ":size-medium", ":size-large"];
    private static readonly string[] Shapes = [":shape-default", ":shape-pill", ":shape-round", ":shape-circle", ":shape-square"];

    static Button()
    {
        AffectsArrange<Button>(MinHitAreaProperty);
        AffectsRender<Button>(MinHitAreaProperty);
    }

    private readonly Parts _parts;
    private double _artworkSize;
    private bool _mouseOver;

    public Button()
    {
        _parts = new Parts(this, "Button",
            [BaseButtonPart, StartEnhancerPart, EndEnhancerPart, StartEnhancerButtonContentContainerPart, LoadingSpinnerContainerPart, LoadingSpinnerPart],
            new Dictionary<string, string> { ["Root"] = BaseButtonPart });
        UpdateAllStates();
    }

    /// <summary>Base Web's <c>kind</c>. Primary by default.</summary>
    public ButtonKind Kind { get => GetValue(KindProperty); set => SetValue(KindProperty, value); }

    /// <summary>Base Web's <c>size</c>. Medium by default.</summary>
    public ButtonSize Size { get => GetValue(SizeProperty); set => SetValue(SizeProperty, value); }

    /// <summary>Base Web's <c>shape</c>. Rectangular by default.</summary>
    public ButtonShape Shape { get => GetValue(ShapeProperty); set => SetValue(ShapeProperty, value); }

    /// <summary>Base Web's <c>widthType</c>: <see cref="ButtonWidthType.Fill"/> stretches the button and gives it a minimum size.</summary>
    public ButtonWidthType WidthType { get => GetValue(WidthTypeProperty); set => SetValue(WidthTypeProperty, value); }

    /// <summary>Base Web's <c>minHitArea</c>.</summary>
    public ButtonMinHitArea MinHitArea { get => GetValue(MinHitAreaProperty); set => SetValue(MinHitAreaProperty, value); }

    /// <summary>Base Web's <c>isLoading</c>: a spinner in place of the content, which keeps its size, and no click.</summary>
    public bool IsLoading { get => GetValue(IsLoadingProperty); set => SetValue(IsLoadingProperty, value); }

    /// <summary>Base Web's <c>isSelected</c>.</summary>
    public bool IsSelected { get => GetValue(IsSelectedProperty); set => SetValue(IsSelectedProperty, value); }

    /// <summary>Base Web's <c>backgroundSafe</c>: a shadow under the button, for a busy background.</summary>
    public bool BackgroundSafe { get => GetValue(BackgroundSafeProperty); set => SetValue(BackgroundSafeProperty, value); }

    /// <summary>Base Web's <c>startEnhancer</c>: content before the button's, an icon or text. Bind an icon's size to <see cref="ArtworkSize"/>.</summary>
    public object? StartEnhancer { get => GetValue(StartEnhancerProperty); set => SetValue(StartEnhancerProperty, value); }

    /// <summary>Base Web's <c>endEnhancer</c>.</summary>
    public object? EndEnhancer { get => GetValue(EndEnhancerProperty); set => SetValue(EndEnhancerProperty, value); }

    /// <summary>Base Web's <c>overrides</c>, by part: <c>BaseButton</c> (or <c>Root</c>), <c>StartEnhancer</c>, <c>EndEnhancer</c>,
    /// <c>StartEnhancerButtonContentContainer</c>, <c>LoadingSpinnerContainer</c>, <c>LoadingSpinner</c>.</summary>
    public OverrideCollection? Overrides { get => GetValue(OverridesProperty); set => SetValue(OverridesProperty, value); }

    /// <summary>Base Web's <c>artworkSize</c>, given to an enhancer: 12, 16, 20 or 24 by size (the theme's <c>sizing.scale500</c> to <c>scale800</c>).</summary>
    public double ArtworkSize { get => _artworkSize; private set => SetAndRaise(ArtworkSizeProperty, ref _artworkSize, value); }

    /// <summary>A part of the current template by its Base Web name, for tests and tools.</summary>
    internal StyledElement? Part(string name) => _parts[name];

    internal Parts PartSet => _parts;

    /// <summary>The size's newer name: mini is xSmall, compact small, default medium.</summary>
    public static ButtonSize Canonical(ButtonSize size) => size switch
    {
        ButtonSize.Mini => ButtonSize.XSmall,
        ButtonSize.Compact => ButtonSize.Small,
        ButtonSize.Default => ButtonSize.Medium,
        _ => size,
    };

    protected override Type StyleKeyOverride => typeof(Button);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        _parts.Attach(e.NameScope);
        UpdateArtworkSize();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateArtworkSize();
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        SetState(":loaded", true);
    }

    protected override void OnUnloaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        SetState(":loaded", false);
    }

    protected override void OnClick()
    {
        // Base Web: a loading button prevents the click and doesn't call onClick.
        if (IsLoading) return;
        base.OnClick();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == KindProperty) UpdateKind();
        else if (p == SizeProperty) { UpdateSize(); UpdateArtworkSize(); }
        else if (p == ShapeProperty) UpdateShape();
        else if (p == WidthTypeProperty) SetState(":width-fill", WidthType == ButtonWidthType.Fill);
        else if (p == MinHitAreaProperty) SetState(":hit-tap", MinHitArea == ButtonMinHitArea.Tap);
        else if (p == IsLoadingProperty) SetState(":is-loading", IsLoading);
        else if (p == IsSelectedProperty) SetState(":is-selected", IsSelected);
        else if (p == BackgroundSafeProperty) SetState(":background-safe", BackgroundSafe);
        else if (p == IsPressedProperty) SetState(":active", IsPressed);
        else if (p == OverridesProperty) _parts.SetOverrides(Overrides);
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        SetMouseOver(e.Pointer.Type == PointerType.Mouse);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        SetMouseOver(e.Pointer.Type == PointerType.Mouse);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetMouseOver(false);
    }

    protected override void OnGotFocus(FocusChangedEventArgs e)
    {
        base.OnGotFocus(e);
        SetState(":focus-visible", e.NavigationMethod is NavigationMethod.Tab or NavigationMethod.Directional);
    }

    protected override void OnLostFocus(FocusChangedEventArgs e)
    {
        base.OnLostFocus(e);
        SetState(":focus-visible", false);
    }

    /// <summary>
    /// <c>minHitArea="tap"</c>: Base Web's absolutely positioned <c>::before</c>, as wide as the
    /// button and the theme's <c>sizing.scale1200</c> (48) tall, centred on it, which takes the
    /// pointer above and below a shorter button without taking room in layout. Avalonia hit-tests a
    /// child only inside its parent's bounds, so the button's own bounds grow to the band after
    /// layout has placed it, and its template is arranged back where layout put it.
    /// </summary>
    protected override void ArrangeCore(Rect finalRect)
    {
        base.ArrangeCore(finalRect);
        if (MinHitArea != ButtonMinHitArea.Tap || VisualChildren.Count == 0) return;
        if (!this.TryFindResource("sizing.scale1200", ActualThemeVariant, out var value) || value is not double target) return;
        var b = Bounds;
        var extra = Math.Max(0, (target - b.Height) / 2);
        if (extra <= 0) return;
        Bounds = new Rect(b.X, b.Y - extra, b.Width, b.Height + 2 * extra);
        foreach (var child in VisualChildren.OfType<Layoutable>())
        {
            child.Arrange(new Rect(0, extra, b.Width, b.Height));
        }
    }

    public override void Render(Avalonia.Media.DrawingContext context)
    {
        // The tap band beyond the template: transparent, but drawn, so that the pointer finds it.
        if (MinHitArea == ButtonMinHitArea.Tap)
        {
            context.FillRectangle(Avalonia.Media.Brushes.Transparent, new Rect(Bounds.Size));
        }
    }

    private void SetMouseOver(bool over)
    {
        if (_mouseOver == over) return;
        _mouseOver = over;
        SetState(":hover", over);
    }

    private void SetState(string pseudoClass, bool on)
    {
        PseudoClasses.Set(pseudoClass, on);
        _parts.SetState(pseudoClass, on);
    }

    private void UpdateAllStates()
    {
        UpdateKind();
        UpdateSize();
        UpdateShape();
        SetState(":width-fill", WidthType == ButtonWidthType.Fill);
        SetState(":hit-tap", MinHitArea == ButtonMinHitArea.Tap);
        SetState(":is-loading", IsLoading);
        SetState(":is-selected", IsSelected);
        SetState(":background-safe", BackgroundSafe);
    }

    private void UpdateKind()
    {
        var kind = Kinds[(int)Kind];
        foreach (var k in Kinds) SetState(k, k == kind);
    }

    private void UpdateSize()
    {
        var size = Canonical(Size) switch
        {
            ButtonSize.XSmall => Sizes[0],
            ButtonSize.Small => Sizes[1],
            ButtonSize.Large => Sizes[3],
            _ => Sizes[2],
        };
        foreach (var s in Sizes) SetState(s, s == size);
    }

    private void UpdateShape()
    {
        var shape = Shape switch
        {
            ButtonShape.Rounded or ButtonShape.Pill => Shapes[1],
            ButtonShape.Round => Shapes[2],
            ButtonShape.Circle => Shapes[3],
            ButtonShape.Square => Shapes[4],
            _ => Shapes[0],
        };
        foreach (var s in Shapes) SetState(s, s == shape);
        SetState(":icon-shape", Shape is ButtonShape.Round or ButtonShape.Circle or ButtonShape.Square);
        SetState(":fixed-shape", Shape is ButtonShape.Circle or ButtonShape.Square);
    }

    private void UpdateArtworkSize()
    {
        var key = Canonical(Size) switch
        {
            ButtonSize.XSmall => "sizing.scale500",
            ButtonSize.Small => "sizing.scale600",
            ButtonSize.Large => "sizing.scale800",
            _ => "sizing.scale700",
        };
        if (this.TryFindResource(key, ActualThemeVariant, out var value) && value is double size) ArtworkSize = size;
    }
}
