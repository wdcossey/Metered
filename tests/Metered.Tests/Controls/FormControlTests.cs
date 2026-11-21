using System.Text.Json;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Avalonia.VisualTree;
using Metered.Controls;
using Metered.Themes;

namespace Metered.Tests.Controls;

/// <summary>
/// The form control's behaviour against Base Web's own FormControl rendered (FormControlOracle): the hint it shows and
/// its icon, which parts render, the state each part and function is given; the counter; what the child is given; the
/// label focusing what it labels by a real click or tap, and not by a press or a drag that scrolls the page; disabled;
/// overrides; UI Automation. All by real input.
/// </summary>
public class FormControlTests
{
    private static readonly string[] Parts =
    [
        FormControl.LabelContainerPart, FormControl.LabelPart, FormControl.LabelEndEnhancerPart, FormControl.ControlContainerPart,
        FormControl.CaptionPart, FormControl.CaptionIconPart, FormControl.CaptionMessagePart,
    ];

    private static Scene Show(Control content) => new(new StackPanel { Margin = new Thickness(20), Children = { content } });

    /// <summary>The parts Base Web renders: the ones shown, in a shown container.</summary>
    private static string[] Rendered(FormControl form) =>
        Parts.Where(p => form.Part(p) is Visual v && v.IsEffectivelyVisible).ToArray();

    private static string? Hint(FormControl form) =>
        form.Part(FormControl.CaptionPart) is Visual { IsEffectivelyVisible: true }
            ? ((ContentPresenter)form.Part(FormControl.CaptionMessagePart)!).Content as string
            : null;

    private static string? Icon(FormControl form) =>
        form.Classes.Contains(":icon-alert") ? "alert" : form.Classes.Contains(":icon-check") ? "check" : null;

    public static TheoryData<int> HintCases => new(Enumerable.Range(0, FormControlOracle.Value.Hints.Length));

    [AvaloniaTheory]
    [MemberData(nameof(HintCases))]
    public void The_hint_its_icon_and_the_parts_are_Base_Webs(int index)
    {
        var c = FormControlOracle.Value.Hints[index];
        var calls = new Dictionary<string, FormControlState>();
        var form = FormControlOracle.Build(c.Props, calls);
        var input = new Input();
        form.Content = input;
        using var scene = Show(form);

        Assert.Equal(c.Rendered, Rendered(form));
        Assert.Equal(c.Hint, Hint(form));
        Assert.Equal(c.Icon, Icon(form));
        Assert.Equal(c.Icon is not null, ((Visual)form.Part(FormControl.CaptionIconPart)!).IsEffectivelyVisible);
        if (c.Icon is not null)
        {
            FormControlColourTests.AssertIcon(c.Icon, (FormControlCaptionIcon)form.Part(FormControl.CaptionIconPart)!);
            var box = FormControlOracle.Value.Layout.Facts.CaptionIcon;
            Assert.Equal((double)c.IconSize!, box.IconSize);
            // aria-hidden: not in UI Automation's views.
            Assert.True(c.IconHidden);
            var peer = ControlAutomationPeer.CreatePeerForElement((Control)form.Part(FormControl.CaptionIconPart)!);
            Assert.False(peer.IsControlElement());
            Assert.False(peer.IsContentElement());
        }

        Assert.Equal(c.State.ToState(), form.State);
        foreach (var part in Parts)
        {
            var classes = ((StyledElement)form.Part(part)!).Classes;
            Assert.Equal(c.State.Error, classes.Contains(":error"));
            Assert.Equal(c.State.Positive, classes.Contains(":positive"));
        }

        Assert.Equal(c.Calls.Keys.Order(), calls.Keys.Order());
        foreach (var (name, state) in c.Calls) Assert.Equal(state.ToState(), calls[name]);

        // aria-describedby and aria-errormessage name the caption: its text is the child's help text.
        Assert.Equal((c.DescribedBy || c.ErrorMessage) ? c.Hint : null, AutomationProperties.GetHelpText(input));
    }

    public static TheoryData<int> CounterCases => new(Enumerable.Range(0, FormControlOracle.Value.Counters.Length));

    [AvaloniaTheory]
    [MemberData(nameof(CounterCases))]
    public void The_counter_is_Base_Webs(int index)
    {
        var c = FormControlOracle.Value.Counters[index];
        var calls = new Dictionary<string, FormControlState>();
        var input = new Input();
        if (c.Child.TryGetValue("value", out var value)) input.Text = value.GetString();
        if (c.Child.TryGetValue("maxLength", out var max)) input.MaxLength = max.GetInt32();
        var form = new FormControl
        {
            Label = new Func<FormControlState, object?>(s => { calls["label"] = s; return "Label"; }),
            Caption = new Func<FormControlState, object?>(s => { calls["caption"] = s; return "Caption"; }),
            Counter = FormControlOracle.Counter(c.Counter),
            LabelEndEnhancer = c.LabelEndEnhancer,
            Content = input,
        };
        using var scene = Show(form);

        var end = (ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!;
        Assert.Equal(c.EndText is not null, end.IsEffectivelyVisible);
        if (c.EndText is not null) Assert.Equal(c.EndText, end.Content);
        Assert.Equal(c.State.ToState(), form.State);
        Assert.Equal(c.State.CounterError, end.Classes.Contains(":counter-error"));
        Assert.Equal(c.ChildError ?? false, input.Error);
        // The hint's function is called before the counter changes the shared props, the label's after.
        Assert.Equal(c.CaptionCall.ToState(), calls["caption"]);
        Assert.Equal(c.LabelCall.ToState(), calls["label"]);
    }

    [AvaloniaFact]
    public void The_counter_follows_the_text_as_it_is_typed_and_its_maximum()
    {
        var input = new Input { MaxLength = 5 };
        var form = new FormControl { Label = "Label", Counter = true, Content = input };
        using var scene = Show(form);
        var end = (ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!;
        Assert.Equal("0/5", end.Content);

        scene.Click(input);
        foreach (var ch in "abc") scene.Window.KeyTextInput(ch.ToString());
        scene.Pump();
        Assert.Equal("3/5", end.Content);
        Assert.False(form.State.CounterError);

        // Past the maximum (a shorter one than the text): an error, the child's too.
        input.MaxLength = 2;
        scene.Pump();
        Assert.Equal("3/2", end.Content);
        Assert.True(form.State.CounterError);
        Assert.True(input.Error);
        Assert.Contains(":counter-error", end.Classes);

        input.MaxLength = 0;
        scene.Pump();
        Assert.Equal("3", end.Content);
        Assert.False(input.Error);
    }

    [AvaloniaFact]
    public void Counter_in_XAML_is_True_or_False()
    {
        var converter = new FormControlCounterConverter();
        Assert.IsType<FormControlCounter>(converter.ConvertFrom(null, null, "True"));
        Assert.Null(converter.ConvertFrom(null, null, "False"));
        Assert.Throws<FormatException>(() => converter.ConvertFrom(null, null, "5"));
    }

    public static TheoryData<int> ChildCases => new(Enumerable.Range(0, FormControlOracle.Value.Children.Length));

    [AvaloniaTheory]
    [MemberData(nameof(ChildCases))]
    public void The_child_is_given_what_cloneElement_gives_it(int index)
    {
        var c = FormControlOracle.Value.Children[index];
        Control child = c.Type switch
        {
            "Input" => new Input(),
            "Textarea" => new Textarea(),
            "Checkbox" => new Checkbox(),
            "RadioGroup" => new RadioGroup(),
            _ => throw new InvalidOperationException(c.Type),
        };
        var error = AvaloniaPropertyRegistry.Instance.FindRegistered(child, "Error")!;
        var positive = AvaloniaPropertyRegistry.Instance.FindRegistered(child, "Positive");
        foreach (var (name, value) in c.Own)
        {
            switch (name)
            {
                case "error": child.SetValue(error, value.GetBoolean()); break;
                case "positive": if (positive is not null) child.SetValue(positive, value.GetBoolean()); break;
                case "disabled": child.IsEnabled = !value.GetBoolean(); break;
            }
        }

        var form = FormControlOracle.Build(c.Form, []);
        form.Content = child;
        using var scene = Show(form);

        Assert.Equal(c.Error ?? false, child.GetValue(error));
        // Base Web gives a positive to any child; Metered's Checkbox and RadioGroup have none to take it.
        if (positive is not null) Assert.Equal(c.Positive ?? false, child.GetValue(positive));
        Assert.Equal(!(c.Disabled ?? false), child.IsEffectivelyEnabled);

        // Let go when the form control's state is: the child's own value is back.
        form.Error = null;
        form.Positive = null;
        form.IsEnabled = true;
        scene.Pump();
        var own = c.Own.TryGetValue("error", out var e) && e.GetBoolean();
        Assert.Equal(own, child.GetValue(error));
    }

    [AvaloniaFact]
    public void A_child_taken_out_is_let_go()
    {
        var input = new Input();
        var form = new FormControl { Label = "Label", Caption = "Caption", Error = true, Positive = true, Content = input };
        using var scene = Show(form);
        Assert.True(input.Error);
        Assert.True(input.Positive);
        Assert.NotNull(AutomationProperties.GetLabeledBy(input));
        Assert.Equal("Caption", AutomationProperties.GetHelpText(input));

        form.Content = new Input();
        scene.Pump();
        Assert.False(input.Error);
        Assert.False(input.Positive);
        Assert.Null(AutomationProperties.GetLabeledBy(input));
        Assert.Null(AutomationProperties.GetHelpText(input));
        Assert.True(((Input)form.Content).Error);
    }

    [AvaloniaFact]
    public void Defaults_are_Base_Webs()
    {
        var d = FormControlOracle.Value.Defaults;
        var form = new FormControl();
        Assert.Equal(d.Label, form.Label);
        Assert.Equal(d.Caption, form.Caption);
        Assert.Equal(!d.Disabled, form.IsEnabled);
        Assert.False(d.Counter);
        Assert.Null(form.Counter);
        Assert.Null(form.Error);
        Assert.Null(form.Positive);
        Assert.Null(form.LabelEndEnhancer);
        Assert.Null(form.HtmlFor);
    }

    // htmlFor: the FormControl's, else the child's id (form-control-id.scenario.tsx). Avalonia has no id: the child.
    [AvaloniaFact]
    public void The_label_is_for_HtmlFor_or_the_child()
    {
        Assert.Equal("two", FormControlOracle.Value.Labels.Single(l => l.HtmlFor == "two" && l.Id == "one").LabelFor);
        Assert.Equal("one", FormControlOracle.Value.Labels.Single(l => l.HtmlFor is null && l.Id == "one").LabelFor);
        var child = new Input();
        var other = new Input();
        var form = new FormControl { Label = "Label", Content = child };
        using var scene = Show(new StackPanel { Children = { form, other } });
        var label = (FormControlLabel)form.Part(FormControl.LabelPart)!;
        Assert.Same(child, label.Target);

        form.HtmlFor = other;
        scene.Pump();
        Assert.Same(other, label.Target);
        Assert.Same(label, AutomationProperties.GetLabeledBy(other));
        Assert.Null(AutomationProperties.GetLabeledBy(child));
    }

    [AvaloniaFact]
    public void A_click_on_the_label_focuses_its_control_and_a_press_alone_doesnt()
    {
        var input = new Input();
        var form = new FormControl { Label = "Input label", Content = input };
        using var scene = Show(form);
        var label = (Visual)form.Part(FormControl.LabelPart)!;

        // A browser's label waits for the click: a press is nothing yet.
        scene.Press(label);
        Assert.False(input.IsFocused);
        scene.Window.MouseUp(scene.CentreOf(label), MouseButton.Left);
        scene.Pump();
        Assert.True(input.IsFocused);
    }

    [AvaloniaFact]
    public void A_tap_on_the_label_focuses_its_control_anywhere_on_the_labels_row()
    {
        var input = new Input();
        var form = new FormControl { Label = "Label", Content = input };
        using var scene = Show(form);
        var label = (Visual)form.Part(FormControl.LabelPart)!;
        // Its box is the row's width, not its text's: a tap far past the word.
        var at = label.TranslatePoint(new Point(label.Bounds.Width - 4, label.Bounds.Height / 2), scene.Window)!.Value;
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        Assert.False(input.IsFocused);
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
        Assert.True(input.IsFocused);
    }

    // On a phone a finger lands on a label to scroll the page: that drag scrolls it, and focuses nothing.
    [AvaloniaFact]
    public void A_touch_drag_from_the_label_scrolls_the_page_and_focuses_nothing()
    {
        var input = new Input();
        var form = new FormControl { Label = "Label", Caption = "Caption", Content = input };
        var page = new ScrollViewer { Content = new StackPanel { Margin = new Thickness(20, 200, 20, 1600), Children = { form } } };
        using var scene = new Scene(page, themes: [new SimpleTheme(), new BaseTheme()]);
        var at = scene.CentreOf((Visual)form.Part(FormControl.LabelPart)!);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        for (var i = 1; i <= 15; i++)
        {
            scene.Window.TouchMove(touch, at - new Vector(0, 6 * i), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, at - new Vector(0, 90), RawInputModifiers.None);
        scene.Pump();
        Assert.True(page.Offset.Y > 40, $"the page moved {page.Offset.Y}");
        Assert.False(input.IsFocused);
    }

    [AvaloniaFact]
    public void A_disabled_form_control_disables_its_child_and_its_label_focuses_nothing()
    {
        var input = new Input();
        var form = new FormControl { Label = "Label", Caption = "Caption", IsEnabled = false, Content = input };
        using var scene = Show(form);
        Assert.False(input.IsEffectivelyEnabled);
        Assert.Contains(":disabled", input.Classes);
        Assert.True(form.State.Disabled);
        foreach (var part in Parts) Assert.True(((StyledElement)form.Part(part)!).Classes.Contains(":disabled"), part);
        scene.Click((Visual)form.Part(FormControl.LabelPart)!);
        Assert.False(input.IsFocused);

        form.IsEnabled = true;
        scene.Pump();
        Assert.False(form.State.Disabled);
        scene.Click((Visual)form.Part(FormControl.LabelPart)!);
        Assert.True(input.IsFocused);
    }

    // A press the label noted, then the pointer taken by something else (a scroller, a drag), isn't a click when it comes
    // back to the label and is let go there.
    [AvaloniaFact]
    public void A_press_whose_pointer_is_taken_from_the_label_is_no_click()
    {
        var input = new Input();
        var other = new Border { Width = 10, Height = 10 };
        var form = new FormControl { Label = "Label", Content = input };
        using var scene = Show(new StackPanel { Children = { form, other } });
        var label = (FormControlLabel)form.Part(FormControl.LabelPart)!;
        IPointer? pointer = null;
        label.AddHandler(InputElement.PointerPressedEvent, (_, e) => pointer = e.Pointer, handledEventsToo: true);
        scene.Press(label);
        Assert.Same(label, pointer!.Captured);
        pointer.Capture(other);
        pointer.Capture(label);
        scene.Window.MouseUp(scene.CentreOf(label), MouseButton.Left);
        scene.Pump();
        Assert.False(input.IsFocused);
    }

    [AvaloniaFact]
    public void A_form_control_taken_out_of_the_tree_lets_its_label_go()
    {
        var five = new Input();
        var form = new FormControl { Label = "Label", Content = new Input(), HtmlFor = five };
        var page = new StackPanel { Children = { form, five } };
        using var scene = Show(page);
        Assert.Same(form.Part(FormControl.LabelPart), AutomationProperties.GetLabeledBy(five));

        page.Children.Remove(form);
        scene.Pump();
        Assert.Null(AutomationProperties.GetLabeledBy(five));

        page.Children.Insert(0, form);
        scene.Pump();
        Assert.Same(form.Part(FormControl.LabelPart), AutomationProperties.GetLabeledBy(five));
    }

    // Each keystroke changes the counter's text from one value to the next, never through nothing.
    [AvaloniaFact]
    public void The_counter_goes_from_one_text_to_the_next()
    {
        var input = new Input { MaxLength = 40 };
        var form = new FormControl { Label = "Label", Counter = true, Content = input };
        using var scene = Show(form);
        var end = (ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!;
        var seen = new List<object?>();
        end.PropertyChanged += (_, e) => { if (e.Property == ContentPresenter.ContentProperty) seen.Add(e.NewValue); };
        input.Text = "a";
        input.Text = "ab";
        scene.Pump();
        Assert.Equal(["1/40", "2/40"], seen);
    }

    [AvaloniaFact]
    public void A_function_giving_text_is_called_and_shows_no_icon()
    {
        var form = new FormControl { Label = "Label", Error = new Func<FormControlState, string>(s => s.Error ? "Called" : "?"), Content = new Input() };
        using var scene = Show(form);
        Assert.Equal("Called", Hint(form));
        Assert.Null(Icon(form));
    }

    // A browser's textarea value has a line break as "\n", one character; Avalonia's text box may have "\r\n".
    [AvaloniaFact]
    public void A_line_break_counts_as_one()
    {
        var textarea = new Textarea { MaxLength = 40, Text = "a\r\nb\nc" };
        var form = new FormControl { Label = "Label", Counter = true, Content = textarea };
        using var scene = Show(form);
        Assert.Equal("5/40", ((ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!).Content);
    }

    [AvaloniaFact]
    public void A_change_to_the_counter_it_has_renders_it_again()
    {
        var form = new FormControl { Label = "Label", Counter = new FormControlCounter { Length = 0, MaxLength = 100 }, Content = new Input() };
        using var scene = Show(form);
        var end = (ContentPresenter)form.Part(FormControl.LabelEndEnhancerPart)!;
        Assert.Equal("0/100", end.Content);
        form.Counter!.Length = 120;
        scene.Pump();
        Assert.Equal("120/100", end.Content);
        Assert.True(form.State.CounterError);

        // One it no longer has is no longer heard.
        var old = form.Counter;
        form.Counter = new FormControlCounter { Length = 1 };
        old.Length = 7;
        scene.Pump();
        Assert.Equal("1", end.Content);
    }

    [AvaloniaFact]
    public void Out_of_the_tree_it_gives_nothing_to_an_element_outside_it()
    {
        var five = new Input();
        var form = new FormControl { Label = "Label", Content = new Input(), HtmlFor = five };
        var page = new StackPanel { Children = { form, five } };
        using var scene = Show(page);
        page.Children.Remove(form);
        form.Label = "Another label";
        form.HtmlFor = null;
        form.HtmlFor = five;
        scene.Pump();
        Assert.Null(AutomationProperties.GetLabeledBy(five));
    }

    [AvaloniaFact]
    public void Another_button_pressed_meanwhile_doesnt_cancel_the_click()
    {
        var input = new Input();
        var form = new FormControl { Label = "Label", Content = input };
        using var scene = Show(form);
        var at = scene.CentreOf((Visual)form.Part(FormControl.LabelPart)!);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Window.MouseUp(at, MouseButton.Right);
        scene.Window.MouseUp(at, MouseButton.Left);
        scene.Pump();
        Assert.True(input.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(0, false)]
    [InlineData(3, true)]
    public void A_number_is_something_unless_it_is_0(int n, bool something)
    {
        foreach (var value in new object[] { (decimal)n, (short)n, (byte)n, (uint)n, (ulong)n, (double)n, n })
        {
            var form = new FormControl { Label = "Label", Error = value, Content = new Input() };
            using var scene = Show(form);
            Assert.Equal(something, form.State.Error);
            Assert.Equal(something, ((Visual)form.Part(FormControl.CaptionPart)!).IsEffectivelyVisible);
        }
    }

    // A browser's click needs the press and the release on the label: one let go elsewhere is no click.
    [AvaloniaFact]
    public void A_press_on_the_label_let_go_elsewhere_is_no_click()
    {
        var input = new Input();
        var other = new Border { Height = 40, Background = Avalonia.Media.Brushes.Transparent };
        var form = new FormControl { Label = "Label", Content = input };
        using var scene = Show(new StackPanel { Children = { form, other } });
        scene.Press((Visual)form.Part(FormControl.LabelPart)!);
        var away = scene.CentreOf(other);
        scene.Window.MouseMove(away);
        scene.Window.MouseUp(away, MouseButton.Left);
        scene.Pump();
        Assert.False(input.IsFocused);
    }

    // Out of the tree it renders nothing, so a child taken out then is let go when it is taken out.
    [AvaloniaFact]
    public void A_child_taken_out_while_out_of_the_tree_is_let_go()
    {
        var input = new Input();
        var form = new FormControl { Label = "Label", Caption = "Caption", Error = true, Content = input };
        var page = new StackPanel { Children = { form } };
        using var scene = Show(page);
        Assert.True(input.Error);
        page.Children.Remove(form);
        form.Content = new Input();
        Assert.False(input.Error);
        Assert.Null(AutomationProperties.GetHelpText(input));
        Assert.Null(AutomationProperties.GetLabeledBy(input));
    }

    [AvaloniaFact]
    public void A_label_without_a_focusable_target_does_nothing()
    {
        var form = new FormControl { Label = "Label", Content = new TextBlock { Text = "Not a control" } };
        using var scene = Show(form);
        var focused = scene.Window.FocusManager?.GetFocusedElement();
        scene.Click((Visual)form.Part(FormControl.LabelPart)!);
        Assert.Equal(focused, scene.Window.FocusManager?.GetFocusedElement());
    }

    [AvaloniaFact]
    public void An_override_restyles_a_part_and_CaptionIcon_and_CaptionMessage_take_only_a_component()
    {
        var red = new SolidColorBrush(Color.Parse("#FF102030"));
        var styled = new ControlTheme(typeof(FormControlLabel)) { Setters = { new Setter(Avalonia.Controls.Primitives.TemplatedControl.ForegroundProperty, red) } };
        var ignored = new ControlTheme(typeof(FormControlCaptionIcon)) { Setters = { new Setter(Avalonia.Layout.Layoutable.WidthProperty, 40.0) } };
        var replaced = new ControlTheme(typeof(ContentPresenter)) { Setters = { new Setter(ContentPresenter.ForegroundProperty, red) } };
        var form = new FormControl
        {
            Label = "Label", Error = "Error", Content = new Input(),
            Overrides =
            [
                new Override { Part = FormControl.LabelPart, Style = styled },
                new Override { Part = FormControl.CaptionIconPart, Style = ignored, Props = ignored },
                new Override { Part = FormControl.CaptionMessagePart, Component = replaced },
            ],
        };
        using var scene = Show(form);
        Assert.Same(red, ((FormControlLabel)form.Part(FormControl.LabelPart)!).Foreground);
        Assert.Equal(FormControlOracle.Value.Layout.Facts.CaptionIcon.Padding[0] + FormControlOracle.Value.Layout.Facts.CaptionIcon.IconSize
            + FormControlOracle.Value.Layout.Facts.CaptionIcon.Padding[2], ((Control)form.Part(FormControl.CaptionIconPart)!).Bounds.Width);
        Assert.Same(red, ((ContentPresenter)form.Part(FormControl.CaptionMessagePart)!).Foreground);

        // A change to the collection is followed.
        form.Overrides.RemoveAt(0);
        scene.Pump();
        Assert.NotSame(red, ((FormControlLabel)form.Part(FormControl.LabelPart)!).Foreground);
    }

    [AvaloniaFact]
    public void The_label_names_its_control_to_UI_Automation()
    {
        var input = new Input();
        var form = new FormControl { Label = "Your email", Caption = "We never share it", Content = input };
        using var scene = Show(form);
        var label = (Control)form.Part(FormControl.LabelPart)!;
        var peer = ControlAutomationPeer.CreatePeerForElement(label);
        Assert.Equal(AutomationControlType.Text, peer.GetAutomationControlType());
        Assert.Equal("Your email", peer.GetName());
        var field = ControlAutomationPeer.CreatePeerForElement(input);
        Assert.Equal("Your email", field.GetName());
        Assert.Same(peer, field.GetLabeledBy());
        Assert.Equal("We never share it", field.GetHelpText());

        // Content that isn't a string: named by its text.
        form.Label = new TextBlock { Text = "Your name" };
        scene.Pump();
        Assert.Equal("Your name", ControlAutomationPeer.CreatePeerForElement(input).GetName());

        // An app's own name wins (Base Web's aria-label beside a label).
        AutomationProperties.SetName(input, "Email address");
        Assert.Equal("Email address", ControlAutomationPeer.CreatePeerForElement(input).GetName());

        // No label: nothing names it.
        AutomationProperties.SetName(input, null);
        form.Label = null;
        scene.Pump();
        Assert.Null(AutomationProperties.GetLabeledBy(input));
    }

    [AvaloniaFact]
    public void A_function_is_called_with_the_state_and_shown_and_called_again_when_the_state_changes()
    {
        var seen = new List<FormControlState>();
        var form = new FormControl
        {
            Label = "Label",
            Caption = new Func<FormControlState, object?>(s => { seen.Add(s); return s.Disabled ? "Disabled" : "Enabled"; }),
            Content = new Input(),
        };
        using var scene = Show(form);
        Assert.Equal("Enabled", Hint(form));
        form.IsEnabled = false;
        scene.Pump();
        Assert.Equal("Disabled", Hint(form));
        Assert.Contains(seen, s => s.Disabled);
    }

    [AvaloniaFact]
    public void Base_Webs_scenario_states_render()
    {
        // input-form-control-states.scenario.tsx: a caption function under a positive message and an error message.
        var positive = new FormControl { Label = "Positive", Caption = new Func<FormControlState, object?>(_ => "caption"), Positive = "Success!", Content = new Input() };
        var error = new FormControl { Label = "Error", Caption = new Func<FormControlState, object?>(_ => "caption"), Error = "Error!", Content = new Input() };
        using var scene = Show(new StackPanel { Children = { positive, error } });
        Assert.Equal("Success!", Hint(positive));
        Assert.Equal("check", Icon(positive));
        Assert.True(((Input)positive.Content!).Positive);
        Assert.Equal("Error!", Hint(error));
        Assert.Equal("alert", Icon(error));
        Assert.True(((Input)error.Content!).Error);
    }
}
