using Avalonia;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Input</c> (baseui 18.2.0, <c>input/input.js</c>): its root and enhancers round Base Web's
/// <c>BaseInput</c> (<see cref="BaseInput"/>), on an Avalonia <c>TextBox</c>, with its props, its twelve named parts
/// and its overrides.
/// </summary>
/// <remarks>
/// <para>Parts: <c>Root</c>, <c>StartEnhancer</c>, <c>EndEnhancer</c>, and BaseInput's (<c>InputContainer</c>,
/// <c>Input</c>, <c>Before</c>, <c>After</c>, <c>ClearIconContainer</c>, <c>ClearIcon</c>, <c>MaskToggleButton</c>,
/// <c>MaskToggleShowIcon</c>, <c>MaskToggleHideIcon</c>). Base Web's <c>Before</c> and <c>After</c> render nothing
/// unless overridden; here they show the text box's <c>InnerLeftContent</c> and <c>InnerRightContent</c>.</para>
/// <para>Besides BaseInput's states, <c>:adjoined-none</c>, <c>-left</c>, <c>-right</c>, <c>-both</c> (which sides have
/// an enhancer: <see cref="Adjoined"/>) and <c>:has-icon-trailing</c> (clearable, or a password: input.js's
/// <c>$hasIconTrailing</c>, which pads the root's end).</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[TemplatePart(StartEnhancerPart, typeof(StyledElement))]
[TemplatePart(EndEnhancerPart, typeof(StyledElement))]
[PseudoClasses(":adjoined-none", ":adjoined-left", ":adjoined-right", ":adjoined-both", ":has-icon-trailing")]
public class Input : BaseInput
{
    public const string RootPart = "Root";
    public const string StartEnhancerPart = "StartEnhancer";
    public const string EndEnhancerPart = "EndEnhancer";

    /// <summary>Every part, by Base Web's names (types.d.ts's InputOverrides).</summary>
    internal static readonly string[] PartNames =
    [
        RootPart, StartEnhancerPart, EndEnhancerPart, InputContainerPart, InputPart, BeforePart, AfterPart,
        ClearIconContainerPart, ClearIconPart, MaskToggleButtonPart, MaskToggleShowIconPart, MaskToggleHideIconPart,
    ];

    public static readonly StyledProperty<object?> StartEnhancerProperty =
        AvaloniaProperty.Register<Input, object?>(nameof(StartEnhancer));

    public static readonly StyledProperty<object?> EndEnhancerProperty =
        AvaloniaProperty.Register<Input, object?>(nameof(EndEnhancer));

    public static readonly StyledProperty<InputType> TypeProperty =
        AvaloniaProperty.Register<Input, InputType>(nameof(Type));

    public static readonly DirectProperty<Input, InputAdjoined> AdjoinedProperty =
        AvaloniaProperty.RegisterDirect<Input, InputAdjoined>(nameof(Adjoined), o => o.Adjoined);

    private InputAdjoined _adjoined;

    public Input()
        : base("Input", PartNames)
    {
        UpdateEnhancers();
        UpdateTrailing();
    }

    /// <summary>Base Web's <c>startEnhancer</c>: content attached before the input, shown when it is something.</summary>
    public object? StartEnhancer { get => GetValue(StartEnhancerProperty); set => SetValue(StartEnhancerProperty, value); }

    /// <summary>Base Web's <c>endEnhancer</c>: content attached after it.</summary>
    public object? EndEnhancer { get => GetValue(EndEnhancerProperty); set => SetValue(EndEnhancerProperty, value); }

    /// <summary>Base Web's <c>type</c>: text (its default) or password.</summary>
    public InputType Type { get => GetValue(TypeProperty); set => SetValue(TypeProperty, value); }

    /// <summary>Base Web's <c>adjoined</c>, as input.js gives it: the sides with an enhancer.</summary>
    public InputAdjoined Adjoined
    {
        get => _adjoined;
        private set => SetAndRaise(AdjoinedProperty, ref _adjoined, value);
    }

    protected override bool IsPassword => Type == InputType.Password;

    protected override System.Type StyleKeyOverride => typeof(Input);

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        UpdateEnhancers();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        var p = change.Property;
        if (p == StartEnhancerProperty || p == EndEnhancerProperty) UpdateEnhancers();
        else if (p == TypeProperty)
        {
            UpdateTrailing();
            UpdateMask();
        }
        else if (p == ClearableProperty) UpdateTrailing();
    }

    /// <summary>
    /// input.js's isEnhancer, <c>Boolean(enhancer || enhancer === 0)</c>: JavaScript's truth, or 0. Not null, an empty
    /// string, false or NaN; 0 is one.
    /// </summary>
    internal static bool IsEnhancer(object? enhancer) =>
        enhancer is not (null or "" or false) && enhancer is not double.NaN && enhancer is not float.NaN;

    /// <summary>input.js renders an enhancer only when it is one, and derives adjoined from the two (getAdjoinedProp).</summary>
    private void UpdateEnhancers()
    {
        var start = IsEnhancer(StartEnhancer);
        var end = IsEnhancer(EndEnhancer);
        Show(StartEnhancerPart, start);
        Show(EndEnhancerPart, end);
        Adjoined = (start, end) switch
        {
            (true, true) => InputAdjoined.Both,
            (true, false) => InputAdjoined.Left,
            (false, true) => InputAdjoined.Right,
            _ => InputAdjoined.None,
        };
        SetState(":adjoined-none", Adjoined == InputAdjoined.None);
        SetState(":adjoined-left", Adjoined == InputAdjoined.Left);
        SetState(":adjoined-right", Adjoined == InputAdjoined.Right);
        SetState(":adjoined-both", Adjoined == InputAdjoined.Both);
    }

    /// <summary>input.js's <c>$hasIconTrailing</c>: clearable or a password, whether or not the icon shows.</summary>
    private void UpdateTrailing() => SetState(":has-icon-trailing", Clearable || Type == InputType.Password);
}
