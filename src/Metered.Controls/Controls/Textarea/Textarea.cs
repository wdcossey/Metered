using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Metered.Controls;

/// <summary>
/// Base Web's <c>Textarea</c> (baseui 18.2.0, <c>textarea/textarea.js</c>): its root round Base Web's <c>BaseInput</c>
/// (<see cref="BaseInput"/>) rendered as a <c>&lt;textarea&gt;</c>, on an Avalonia <c>TextBox</c> that accepts Enter and
/// wraps its text, with its props, its parts and its overrides.
/// </summary>
/// <remarks>
/// <para>Parts: <c>Root</c> and BaseInput's <c>InputContainer</c>, <c>Input</c>, <c>Before</c>, <c>After</c>,
/// <c>ClearIconContainer</c> and <c>ClearIcon</c> (a textarea has no enhancers and no mask toggle). Base Web's
/// <c>Before</c> and <c>After</c> render nothing unless overridden; here they show the text box's
/// <c>InnerLeftContent</c> and <c>InnerRightContent</c>.</para>
/// <para>Besides BaseInput's states, <c>:resize-none</c>, <c>-both</c>, <c>-horizontal</c>, <c>-vertical</c>, for
/// <see cref="Resize"/> set (none while it is unset, Base Web's default).</para>
/// <para>A long text scrolls inside it, and a touch drag on it scrolls it, not a page round it, unless it is already at
/// its end that way when the drag starts; once the drag has taken the textarea, it stays the textarea's until the finger
/// lifts, as a browser latches a touch's scroll to the first scroller that moves (BaseInput's latch, which Input's sideways
/// scroll shares).</para>
/// </remarks>
[TemplatePart(RootPart, typeof(StyledElement))]
[PseudoClasses(":resize-none", ":resize-both", ":resize-horizontal", ":resize-vertical")]
public class Textarea : BaseInput
{
    public const string RootPart = "Root";

    /// <summary>Every part, by Base Web's names (types.d.ts's TextareaOverrides: Root and BaseInput's).</summary>
    internal static readonly string[] PartNames =
    [
        RootPart, InputContainerPart, InputPart, BeforePart, AfterPart, ClearIconContainerPart, ClearIconPart,
    ];

    public static readonly StyledProperty<int> RowsProperty =
        TextareaField.RowsProperty.AddOwner<Textarea>();

    public static readonly StyledProperty<TextareaResize?> ResizeProperty =
        AvaloniaProperty.Register<Textarea, TextareaResize?>(nameof(Resize));

    public Textarea()
        : base("Textarea", PartNames)
    {
        // A press or a release on the resize grip focuses nothing (#111): on its way down, before the grip.
        AddHandler(PointerPressedEvent, TextareaResizer.PassFocusBy, RoutingStrategies.Tunnel);
        AddHandler(PointerReleasedEvent, TextareaResizer.PassFocusBy, RoutingStrategies.Tunnel);
        UpdateResize();
    }

    /// <summary>Base Web's <c>rows</c>, HTML's: how many lines tall the text is; 3, as Base Web's default.</summary>
    public int Rows { get => GetValue(RowsProperty); set => SetValue(RowsProperty, value); }

    /// <summary>
    /// Base Web's <c>resize</c>, CSS's: which way a user can drag the textarea's size. Unset (null) by default, as Base
    /// Web's; any value, <see cref="TextareaResize.None"/> too, makes the root as wide as the textarea.
    /// </summary>
    public TextareaResize? Resize { get => GetValue(ResizeProperty); set => SetValue(ResizeProperty, value); }

    protected override Type StyleKeyOverride => typeof(Textarea);

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == ResizeProperty) UpdateResize();
    }

    private void UpdateResize()
    {
        var resize = Resize;
        SetState(":resize-none", resize == TextareaResize.None);
        SetState(":resize-both", resize == TextareaResize.Both);
        SetState(":resize-horizontal", resize == TextareaResize.Horizontal);
        SetState(":resize-vertical", resize == TextareaResize.Vertical);
    }
}
