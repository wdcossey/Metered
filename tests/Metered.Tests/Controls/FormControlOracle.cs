using System.Text.Json;
using Metered.Controls;

namespace Metered.Tests;

/// <summary>
/// What Base Web's own form control gives (tools/reference/form-control.js, written by tools/reference/cases.js), rendered
/// round a child with the defaultProps of Base Web's Input, Textarea, Checkbox or RadioGroup, in Avalonia's units: colours
/// as #AARRGGBB, lengths in pixels.
/// </summary>
internal static class FormControlOracle
{
    public static FormControlOracleFile Value { get; } = Load();

    private static FormControlOracleFile Load()
    {
        using var stream = typeof(FormControlOracle).Assembly.GetManifestResourceStream("Metered.Tests.Reference.form-control.oracle.json")
            ?? throw new InvalidOperationException("form-control.oracle.json isn't embedded: run node tools/reference/cases.js");
        return JsonSerializer.Deserialize<FormControlOracleFile>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }

    /// <summary>
    /// A prop of a case as Metered's value: text, a boolean, or for <c>{ fn: text }</c> a function returning the text that
    /// records the state it is called with under the prop's name.
    /// </summary>
    public static object? Prop(JsonElement value, string name, Dictionary<string, FormControlState> calls) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Object when value.TryGetProperty("fn", out var text) =>
            new Func<FormControlState, object?>(state => { calls[name] = state; return text.GetString(); }),
        _ => throw new InvalidOperationException($"{name}: {value}"),
    };

    /// <summary>Base Web's <c>counter</c> as Metered's: false none, true one, an object its values.</summary>
    public static FormControlCounter? Counter(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.False => null,
        JsonValueKind.True => new FormControlCounter(),
        JsonValueKind.Object => new FormControlCounter
        {
            Length = value.TryGetProperty("length", out var l) ? l.GetInt32() : null,
            MaxLength = value.TryGetProperty("maxLength", out var m) ? m.GetInt32() : null,
            Error = value.TryGetProperty("error", out var e) ? e.GetBoolean() : null,
        },
        _ => throw new InvalidOperationException($"counter {value}"),
    };

    /// <summary>A form control with a case's props (label, caption, error, positive, labelEndEnhancer, disabled, counter).</summary>
    public static FormControl Build(Dictionary<string, JsonElement> props, Dictionary<string, FormControlState> calls)
    {
        var form = new FormControl();
        foreach (var (name, value) in props)
        {
            switch (name)
            {
                case "label": form.Label = Prop(value, name, calls); break;
                case "caption": form.Caption = Prop(value, name, calls); break;
                case "error": form.Error = Prop(value, name, calls); break;
                case "positive": form.Positive = Prop(value, name, calls); break;
                case "labelEndEnhancer": form.LabelEndEnhancer = Prop(value, name, calls); break;
                case "disabled": form.IsEnabled = !value.GetBoolean(); break;
                case "counter": form.Counter = Counter(value); break;
                default: throw new InvalidOperationException(name);
            }
        }

        return form;
    }
}

internal sealed record FormControlOracleFile(
    string Baseui, FormControlColourCase[] Colours, FormControlHintCase[] Hints, FormControlCounterCase[] Counters,
    FormControlChildCase[] Children, FormControlLabelCase[] Labels, FormControlLayout Layout,
    Dictionary<string, InputIconFacts> Icons, FormControlDefaults Defaults);

/// <summary>A state, with a label, a caption and an end enhancer: each part's colour, the icon, the hint and the end's text.</summary>
internal sealed record FormControlColourCase(
    string Theme, string State, Dictionary<string, JsonElement> Props, string? Label, string? LabelEndEnhancer, string? Caption,
    string? Icon, string? Hint, string? EndText)
{
    public override string ToString() => $"{Theme} {State}";
}

/// <summary>The $ props a part or a function is given (form-control.js's sharedProps).</summary>
internal sealed record FormControlStateFacts(bool Disabled, bool Error, bool Positive, int? Length, int? MaxLength, bool CounterError)
{
    public FormControlState ToState() => new(Disabled, Error, Positive, Length, MaxLength, CounterError);
}

/// <summary>
/// A label, caption, error and positive (each unset, a boolean, text or a function): the parts Base Web renders, the hint
/// it shows and its icon, the icon's size and aria-hidden, the parts' state, which of aria-describedby and
/// aria-errormessage the child is given, and the state each function was called with.
/// </summary>
internal sealed record FormControlHintCase(
    Dictionary<string, JsonElement> Props, string[] Rendered, string? Hint, string? Icon, int? IconSize, bool? IconHidden,
    FormControlStateFacts State, bool DescribedBy, bool ErrorMessage, Dictionary<string, FormControlStateFacts> Calls)
{
    public override string ToString() => string.Join(" ", Props.Select(p => $"{p.Key}={p.Value}")) is { Length: > 0 } s ? s : "nothing";
}

/// <summary>
/// A counter round a child with a value and a maxLength or not, the label and the caption functions: the end enhancer's
/// text, the state, the child's error, and the state the label's and the caption's functions were called with.
/// </summary>
internal sealed record FormControlCounterCase(
    JsonElement Counter, Dictionary<string, JsonElement> Child, string? LabelEndEnhancer, string? EndText, FormControlStateFacts State,
    bool? ChildError, FormControlStateFacts LabelCall, FormControlStateFacts CaptionCall)
{
    public override string ToString() =>
        $"counter {Counter} child {string.Join(",", Child.Select(p => $"{p.Key}={p.Value}"))}{(LabelEndEnhancer is null ? "" : " end " + LabelEndEnhancer)}";
}

/// <summary>What cloneElement gives a child of that type: its error, positive and disabled (null: none given).</summary>
internal sealed record FormControlChildCase(
    string Type, Dictionary<string, JsonElement> Form, Dictionary<string, JsonElement> Own, bool? Error, bool? Positive, bool? Disabled)
{
    public override string ToString() =>
        $"{Type} form {string.Join(",", Form.Select(p => $"{p.Key}={p.Value}"))} own {string.Join(",", Own.Select(p => $"{p.Key}={p.Value}"))}";
}

/// <summary>The label's htmlFor for the form control's htmlFor and the child's id.</summary>
internal sealed record FormControlLabelCase(string? HtmlFor, string? Id, string? LabelFor);

internal sealed record FormControlLayout(FormControlLayoutFacts Facts, FormControlLayoutCase[] Cases);

internal sealed record FormControlLayoutFacts(
    FormControlBox LabelContainer, FormControlText Label, FormControlEnd LabelEndEnhancer, FormControlBox ControlContainer,
    FormControlCaptionFacts Caption, FormControlIconFacts CaptionIcon);

/// <summary>A box's margins [left, top, right, bottom].</summary>
internal sealed record FormControlBox(double[] Margin, string Width);

internal sealed record FormControlText(InputFont Font, string Display, string Width);

internal sealed record FormControlEnd(InputFont Font, double Flex, string Width);

internal sealed record FormControlCaptionFacts(double[] Margin, InputFont Font, string Display);

/// <summary>The icon's div's padding [left, top, right, bottom] and the svg's size.</summary>
internal sealed record FormControlIconFacts(double[] Padding, double FlexShrink, string Display, double IconSize);

/// <summary>
/// With or without a label, an end enhancer, a caption (with or without its icon): where a browser puts each box for a
/// child of any height H, from the top margin's edge: the label container's row, the child, the caption's gap and row,
/// and the height below the child.
/// </summary>
internal sealed record FormControlLayoutCase(
    bool Label, bool LabelEndEnhancer, string Hint, double? LabelTop, double? LabelRow, double ControlTop, double? CaptionGap,
    double? CaptionRow, double BelowChild)
{
    public override string ToString() => $"{(Label ? "label" : "no label")}{(LabelEndEnhancer ? " end" : "")} hint {Hint}";
}

internal sealed record FormControlDefaults(string? Label, string? Caption, bool Disabled, bool Counter);
