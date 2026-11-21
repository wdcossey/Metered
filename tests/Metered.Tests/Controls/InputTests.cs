using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.TextInput;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The input's behaviour, as Base Web's input.js and base-input.js give it, and its e2e test (input.e2e.ts): which parts
/// render and their labels (InputOracle's rendered cases); the clear icon by click, touch, Enter and Space, and Escape;
/// the mask toggle; maxLength; focus and the tab order; the parts' own keys kept from the text box; enhancers and
/// adjoined; overrides; transitions after load; UI Automation. All by real input.
/// </summary>
public class InputTests
{
    private static readonly IBrush Red = new ImmutableSolidColorBrush(Colors.Red);

    private static Scene Show(params Control[] controls)
    {
        var panel = new StackPanel { Margin = new Thickness(20), Spacing = 8 };
        panel.Children.AddRange(controls);
        return new Scene(panel);
    }

    private static T P<T>(BaseInput input, string part) where T : class => (T)(object)input.Part(part)!;

    private static void Type(Scene scene, string text)
    {
        foreach (var ch in text) scene.Window.KeyTextInput(ch.ToString());
        scene.Pump();
    }

    private static void Tap(Scene scene, Visual target)
    {
        var at = scene.CentreOf(target);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    [AvaloniaFact]
    public void Defaults_are_Base_Webs()
    {
        var d = InputOracle.Value.Defaults;
        var input = new Input();
        Assert.Equal(Enum.Parse<InputSize>(d.Size, ignoreCase: true), input.Size);
        Assert.Equal(Enum.Parse<InputType>(d.Type, ignoreCase: true), input.Type);
        Assert.Equal(d.Clearable, input.Clearable);
        Assert.Equal(d.ClearOnEscape, input.ClearOnEscape);
        Assert.Equal(d.Error, input.Error);
        Assert.Equal(d.Positive, input.Positive);
        Assert.Equal(d.Placeholder, input.PlaceholderText ?? "");
        Assert.Equal(!d.Disabled, input.IsEnabled);
        Assert.Equal(d.ReadOnly, input.IsReadOnly);
        Assert.Equal(InputAdjoined.None, input.Adjoined);
        // base-input.js's isMasked starts as type === 'password'.
        Assert.Equal(d.Type == "password", input.IsMasked);
    }

    [AvaloniaFact]
    public void IsMasked_follows_the_type_until_the_toggle_is_clicked()
    {
        var input = new Input { Type = InputType.Password, Text = "1234" };
        using var scene = Show(input);
        Assert.True(input.IsMasked);
        input.Type = InputType.Text;
        Assert.False(input.IsMasked);
        Assert.Equal(default, input.PasswordChar);
        input.Type = InputType.Password;
        Assert.True(input.IsMasked);
        scene.Click(P<Visual>(input, BaseInput.MaskToggleButtonPart));
        Assert.False(input.IsMasked);
    }

    public static TheoryData<int> PartsCases => new(Enumerable.Range(0, InputOracle.Value.Parts.Length));

    [AvaloniaTheory]
    [MemberData(nameof(PartsCases))]
    public void Renders_the_parts_Base_Web_renders_with_its_labels(int index)
    {
        var c = InputOracle.Value.Parts[index];
        var input = new Input { Clearable = c.Clearable, Text = c.Value, IsReadOnly = c.ReadOnly, Type = Enum.Parse<InputType>(c.Type, ignoreCase: true) };
        using var scene = Show(input);
        if (!c.Masked)
        {
            // The toggle's click unmasks; then it is disabled if the case is.
            scene.Click(P<Visual>(input, BaseInput.MaskToggleButtonPart));
        }

        input.IsEnabled = !c.Disabled;
        scene.Pump();

        var shown = Input.PartNames.Where(name => input.Part(name) is Visual { IsEffectivelyVisible: true }).ToHashSet();
        Assert.Equal(c.Rendered.OrderBy(x => x), shown.OrderBy(x => x));
        // The case's mask is the toggle's; base-input.js's isMasked is only ever true for a password.
        Assert.Equal(c.Masked && c.Type == "password", input.IsMasked);
        Assert.Equal(c.InputType == "password", input.PasswordChar != default);

        var clear = P<InputIcon>(input, BaseInput.ClearIconPart);
        if (c.ClearLabel is not null)
        {
            Assert.Equal(c.ClearLabel, AutomationProperties.GetName(clear));
            Assert.Equal(c.ClearTitle, ToolTip.GetTip(clear));
            Assert.Equal("button", c.ClearRole);
            Assert.Equal(AutomationControlType.Button, ControlAutomationPeer.CreatePeerForElement(clear).GetAutomationControlType());
            // tabIndex 0: in the tab order.
            Assert.Equal(0, c.ClearTabIndex);
            Assert.True(clear.Focusable);
            Assert.True(clear.IsTabStop);
        }

        var toggle = P<InputMaskToggleButton>(input, BaseInput.MaskToggleButtonPart);
        if (c.ToggleLabel is not null)
        {
            Assert.Equal(c.ToggleLabel, AutomationProperties.GetName(toggle));
            Assert.Equal(c.ToggleTitle, ToolTip.GetTip(toggle));
        }
    }

    [AvaloniaFact]
    public void The_clear_icons_click_clears_the_value_and_focuses_the_input()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        var other = new Input { Clearable = true, Text = "Or other" };
        using var scene = Show(input, other);
        // TextChanged is raised from the dispatcher: the initial value's has come and gone.
        var changes = 0;
        input.TextChanged += (_, _) => changes++;
        var clear = P<Visual>(input, BaseInput.ClearIconPart);
        Assert.True(clear.IsEffectivelyVisible);

        scene.Click(clear);

        Assert.Equal("", input.Text);
        Assert.Equal(1, changes);
        Assert.True(input.IsFocused);
        Assert.False(clear.IsEffectivelyVisible);
        // The correct input (input.e2e.ts, Base Web #1643).
        Assert.Equal("Or other", other.Text);
        // Undone as an edit.
        input.Undo();
        Assert.Equal("Thing", input.Text);
    }

    [AvaloniaFact]
    public void A_finger_on_the_clear_icon_clears_and_a_press_dragged_off_it_doesnt()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        using var scene = Show(input);
        var clear = P<Visual>(input, BaseInput.ClearIconPart);

        // Pressed, then let go away from it: no click.
        var at = scene.CentreOf(clear);
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseMove(new Point(at.X, at.Y + 40));
        scene.Window.MouseUp(new Point(at.X, at.Y + 40), MouseButton.Left);
        scene.Pump();
        Assert.Equal("Thing", input.Text);
        // A pointer pressed on it isn't the text box's: no caret moved, nothing selected.
        Assert.Equal(0, input.SelectionEnd - input.SelectionStart);

        Tap(scene, clear);
        Assert.Equal("", input.Text);
        Assert.True(input.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(Key.Enter)]
    [InlineData(Key.Space)]
    public void Enter_or_Space_on_the_clear_icon_clears(Key key)
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        using var scene = Show(input);
        var clear = P<InputIcon>(input, BaseInput.ClearIconPart);
        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        Assert.True(clear.IsFocused);

        // A real key: its press, the text a platform sends with it, its release.
        scene.Window.KeyPress(key, RawInputModifiers.None, PhysicalKey.None, null);
        scene.Window.KeyTextInput(key == Key.Space ? " " : "\r");
        scene.Window.KeyRelease(key, RawInputModifiers.None, PhysicalKey.None, null);
        scene.Pump();

        Assert.Equal("", input.Text);
        Assert.True(input.IsFocused);
        // Base Web's preventDefault: the key's text isn't typed into the input it focused.
        Type(scene, "a");
        Assert.Equal("a", input.Text);
    }

    [AvaloniaFact]
    public void A_Space_whose_text_never_comes_doesnt_eat_a_later_one()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        using var scene = Show(input);
        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        // Space down on the icon clears; its text and its release never come here.
        scene.Window.KeyPress(Key.Space, RawInputModifiers.None, PhysicalKey.Space, null);
        scene.Pump();
        Assert.Equal("", input.Text);

        scene.Window.KeyPress(Key.A, RawInputModifiers.None, PhysicalKey.A, null);
        scene.Window.KeyTextInput("a");
        scene.Window.KeyRelease(Key.A, RawInputModifiers.None, PhysicalKey.A, null);
        scene.Window.KeyTextInput(" ");
        scene.Pump();

        Assert.Equal("a ", input.Text);
    }

    [AvaloniaFact]
    public void A_second_finger_on_the_clear_icon_doesnt_take_the_firsts_tap()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        using var scene = Show(input);
        var at = scene.CentreOf(P<Visual>(input, BaseInput.ClearIconPart));
        var first = scene.Window.TouchBegin(at, RawInputModifiers.None);
        var second = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();

        scene.Window.TouchEnd(first, at, RawInputModifiers.None);
        scene.Pump();

        Assert.Equal("", input.Text);
        scene.Window.TouchEnd(second, at, RawInputModifiers.None);
        scene.Pump();
    }

    [AvaloniaFact]
    public void An_override_relabels_the_clear_icon_and_the_toggle()
    {
        // base-input.js spreads an override's props after its own aria-label and title.
        ControlTheme Labels<T>(string label) => new(typeof(T))
        {
            Setters = { new Setter(AutomationProperties.NameProperty, label), new Setter(ToolTip.TipProperty, label) },
        };
        var input = new Input
        {
            Clearable = true, Type = InputType.Password, Text = "1234",
            Overrides =
            [
                new Override { Part = BaseInput.ClearIconPart, Props = Labels<InputIcon>("Effacer") },
                new Override { Part = BaseInput.MaskToggleButtonPart, Props = Labels<InputMaskToggleButton>("Afficher") },
            ],
        };
        using var scene = Show(input);
        Assert.Equal("Effacer", AutomationProperties.GetName(P<Control>(input, BaseInput.ClearIconPart)));
        Assert.Equal("Effacer", ToolTip.GetTip(P<Control>(input, BaseInput.ClearIconPart)));
        Assert.Equal("Afficher", AutomationProperties.GetName(P<Control>(input, BaseInput.MaskToggleButtonPart)));
        scene.Click(P<Visual>(input, BaseInput.MaskToggleButtonPart));
        Assert.Equal("Afficher", ToolTip.GetTip(P<Control>(input, BaseInput.MaskToggleButtonPart)));
    }

    private sealed class PasswordOnly : BaseInput
    {
        public PasswordOnly() : base("Input", Input.PartNames) { }

        protected override bool IsPassword => true;
    }

    [AvaloniaFact]
    public void A_password_is_masked_before_its_template()
    {
        var input = new PasswordOnly();
        Assert.True(input.IsMasked);
        Assert.Equal(BaseInput.MaskCharacter, input.PasswordChar);
    }

    [AvaloniaFact]
    public void Keys_and_text_on_the_clear_icon_arent_the_text_boxs()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        using var scene = Show(input);
        var clear = P<InputIcon>(input, BaseInput.ClearIconPart);
        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        Assert.True(clear.IsFocused);
        Assert.DoesNotContain(":is-focused", input.Classes);
        // Its focus isn't the text box's: Avalonia's text box would select everything on a Tab's focus.
        Assert.Equal(input.SelectionStart, input.SelectionEnd);

        Type(scene, "x");
        scene.Key(Key.Back);
        scene.Key(Key.Delete);

        Assert.Equal("Thing", input.Text);
        Assert.True(clear.IsFocused);
    }

    [AvaloniaFact]
    public void The_texts_selection_stays_while_the_focus_moves_between_the_icons()
    {
        // A browser keeps an input's selection when the focus is elsewhere; the clear icon's losing it isn't the text box's.
        var input = new Input { Clearable = true, Type = InputType.Password, Text = "Thing" };
        using var scene = Show(input);
        scene.Key(Key.Tab);
        scene.Key(Key.Tab);
        Assert.True(P<InputElement>(input, BaseInput.ClearIconPart).IsFocused);
        input.SelectionStart = 1;
        input.SelectionEnd = 3;

        scene.Key(Key.Tab);

        Assert.True(P<InputElement>(input, BaseInput.MaskToggleButtonPart).IsFocused);
        Assert.Equal((1, 3), (input.SelectionStart, input.SelectionEnd));
    }

    [AvaloniaFact]
    public void A_press_on_a_control_of_its_own_doesnt_move_the_caret()
    {
        var mine = new Border { Focusable = true, Width = 20, Height = 20, Background = Brushes.Transparent };
        var input = new Input { Text = "Thing", InnerLeftContent = mine };
        using var scene = Show(input);
        input.CaretIndex = 3;

        scene.Click(mine);

        Assert.True(mine.IsFocused);
        Assert.Equal(3, input.CaretIndex);
    }

    [AvaloniaFact]
    public void A_finger_held_on_the_clear_icon_selects_nothing()
    {
        var input = new Input { Clearable = true, Text = "Thing word" };
        using var scene = Show(input);
        var at = scene.CentreOf(P<Visual>(input, BaseInput.ClearIconPart));
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        // Held past Avalonia's holding delay.
        for (var i = 0; i < 10; i++)
        {
            Thread.Sleep(100);
            scene.Pump();
        }

        Assert.Equal(input.SelectionStart, input.SelectionEnd);
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    [AvaloniaFact]
    public void A_menu_asked_for_on_the_icons_isnt_the_text_boxs()
    {
        var input = new Input { Clearable = true, Type = InputType.Password, Text = "Thing" };
        // A popup needs the app theme's window, its overlay layer.
        using var scene = new Scene(new StackPanel { Margin = new Thickness(20), Children = { input } },
            themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        var flyout = Assert.IsAssignableFrom<Avalonia.Controls.Primitives.FlyoutBase>(input.ContextFlyout);
        foreach (var part in new[] { BaseInput.ClearIconPart, BaseInput.MaskToggleButtonPart })
        {
            P<Control>(input, part).RaiseEvent(new ContextRequestedEventArgs());
            scene.Pump();
            Assert.False(flyout.IsOpen, part);
        }

        // On the text it is: the cut, copy and paste menu.
        P<Control>(input, BaseInput.InputPart).RaiseEvent(new ContextRequestedEventArgs());
        scene.Pump();
        Assert.True(flyout.IsOpen);
        flyout.Hide();
    }

    [AvaloniaFact]
    public void Escape_clears_a_clearable_input_and_goes_no_further()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        var reached = 0;
        var panel = new StackPanel { Margin = new Thickness(20), Children = { input } };
        panel.KeyDown += (_, e) => { if (e.Key == Key.Escape) reached++; };
        using var scene = new Scene(panel);
        scene.Key(Key.Tab);

        scene.Key(Key.Escape);

        Assert.Equal("", input.Text);
        Assert.Equal(0, reached);
        Assert.False(P<Visual>(input, BaseInput.ClearIconPart).IsEffectivelyVisible);
    }

    [AvaloniaTheory]
    [InlineData(false, false, false)]
    [InlineData(true, true, false)]
    [InlineData(true, false, true)]
    public void Escape_leaves_the_value_unless_clearable_clearOnEscape_and_editable(bool clearable, bool readOnly, bool noEscape)
    {
        var input = new Input { Clearable = clearable, ClearOnEscape = !noEscape, IsReadOnly = readOnly, Text = "Thing" };
        var reached = 0;
        var panel = new StackPanel { Margin = new Thickness(20), Children = { input } };
        panel.KeyDown += (_, e) => { if (e.Key == Key.Escape) reached++; };
        using var scene = new Scene(panel);
        scene.Key(Key.Tab);
        Assert.True(input.IsFocused);

        scene.Key(Key.Escape);

        Assert.Equal("Thing", input.Text);
        Assert.Equal(1, reached);
    }

    [AvaloniaFact]
    public void The_toggle_shows_and_hides_a_passwords_text()
    {
        var input = new Input { Type = InputType.Password, Text = "1234" };
        using var scene = Show(input);
        var toggle = P<InputMaskToggleButton>(input, BaseInput.MaskToggleButtonPart);
        Assert.Equal(BaseInput.MaskCharacter, input.PasswordChar);
        Assert.Equal(TextInputContentType.Password, TextInputOptions.GetContentType(input));
        Assert.False(input.CanCopy);

        scene.Click(toggle);
        Assert.False(input.IsMasked);
        Assert.Equal(default, input.PasswordChar);
        Assert.NotEqual(TextInputContentType.Password, TextInputOptions.GetContentType(input));
        Assert.Equal(BaseInput.HidePasswordLabel, AutomationProperties.GetName(toggle));
        Assert.True(P<Visual>(input, BaseInput.MaskToggleHideIconPart).IsEffectivelyVisible);
        Assert.False(P<Visual>(input, BaseInput.MaskToggleShowIconPart).IsEffectivelyVisible);
        // Base Web's toggle is a button: it takes the focus from the input.
        Assert.True(toggle.IsFocused);

        // Shown text stays shown when the focus goes (Avalonia's own RevealPassword would hide it).
        input.Focus();
        scene.Pump();
        toggle.Focus();
        scene.Pump();
        Assert.Equal(default, input.PasswordChar);

        scene.Click(toggle);
        Assert.True(input.IsMasked);
        Assert.Equal(BaseInput.MaskCharacter, input.PasswordChar);
    }

    [AvaloniaFact]
    public void Enter_in_the_input_doesnt_toggle_and_Enter_on_the_toggle_does()
    {
        // input.e2e.ts: "while focusing input, enter does not toggle password masking"; "while focusing mask toggle, enter
        // does not submit the form".
        var input = new Input { Type = InputType.Password, Text = "1234" };
        using var scene = Show(input);
        scene.Key(Key.Tab);
        Assert.True(input.IsFocused);
        scene.Key(Key.Enter);
        Assert.True(input.IsMasked);

        scene.Key(Key.Tab);
        Assert.True(P<InputElement>(input, BaseInput.MaskToggleButtonPart).IsFocused);
        scene.Key(Key.Enter);
        Assert.False(input.IsMasked);
        Assert.Equal("1234", input.Text);
    }

    [AvaloniaFact]
    public void An_app_hides_the_toggle_by_an_override()
    {
        // The documentation's opt-out, MaskToggleButton: () => null.
        var input = new Input
        {
            Type = InputType.Password, Text = "1234",
            Overrides = [new Override { Part = BaseInput.MaskToggleButtonPart, Props = Theme<InputMaskToggleButton>(Visual.IsVisibleProperty, false) }],
        };
        using var scene = Show(input);
        Assert.False(P<Visual>(input, BaseInput.MaskToggleButtonPart).IsEffectivelyVisible);
        Assert.True(input.IsMasked);
    }

    [AvaloniaFact]
    public void MaxLength_stops_typing_at_it()
    {
        var input = new Input { MaxLength = 4 };
        using var scene = Show(input);
        scene.Click(P<Visual>(input, BaseInput.InputPart));
        Assert.True(input.IsFocused);

        Type(scene, "123456");

        Assert.Equal("1234", input.Text);
    }

    [AvaloniaFact]
    public void Typing_and_focus_reach_the_text_and_every_part_takes_is_focused()
    {
        var input = new Input { StartEnhancer = "@", Clearable = true, Type = InputType.Password };
        using var scene = Show(input);
        Assert.All(Input.PartNames, name => Assert.DoesNotContain(":is-focused", input.Part(name)!.Classes));

        scene.Click(P<Visual>(input, BaseInput.InputPart));
        Assert.True(input.IsFocused);
        Assert.All(Input.PartNames, name => Assert.Contains(":is-focused", input.Part(name)!.Classes));
        Type(scene, "ab");
        Assert.Equal("ab", input.Text);
        Assert.True(P<Visual>(input, BaseInput.ClearIconPart).IsEffectivelyVisible);
    }

    [AvaloniaFact]
    public void Tab_goes_from_the_text_to_the_clear_icon_then_the_toggle_then_on()
    {
        var input = new Input { Clearable = true, Type = InputType.Password, Text = "1234" };
        var next = new Input();
        using var scene = Show(input, next);
        scene.Key(Key.Tab);
        Assert.True(input.IsFocused);
        scene.Key(Key.Tab);
        Assert.True(P<InputElement>(input, BaseInput.ClearIconPart).IsFocused);
        scene.Key(Key.Tab);
        Assert.True(P<InputElement>(input, BaseInput.MaskToggleButtonPart).IsFocused);
        scene.Key(Key.Tab);
        Assert.True(next.IsFocused);
    }

    [AvaloniaFact]
    public void A_touch_on_the_text_focuses_it()
    {
        var input = new Input { Text = "Thing" };
        using var scene = Show(input);
        Tap(scene, P<Visual>(input, BaseInput.InputPart));
        Assert.True(input.IsFocused);
        Assert.Contains(":is-focused", input.Part(Input.RootPart)!.Classes);
    }

    [AvaloniaFact]
    public void A_press_on_an_enhancer_focuses_the_input_as_a_text_boxs_own_box_does()
    {
        // Metered's: Base Web's enhancer is a div outside its input (docs/controls/Input, known differences).
        var input = new Input { StartEnhancer = "@", Text = "Thing" };
        using var scene = Show(input);
        scene.Click(P<Visual>(input, Input.StartEnhancerPart));
        Assert.True(input.IsFocused);
    }

    [AvaloniaTheory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData(false, false)]
    [InlineData(0, true)]
    [InlineData("@", true)]
    [InlineData(true, true)]
    [InlineData(double.NaN, false)]
    [InlineData(0.0, true)]
    public void An_enhancer_is_what_JavaScripts_truth_or_0_makes_one(object? value, bool shown)
    {
        Assert.Equal(shown, Input.IsEnhancer(value));
        var input = new Input { EndEnhancer = value };
        using var scene = Show(input);
        Assert.Equal(shown, P<Visual>(input, Input.EndEnhancerPart).IsEffectivelyVisible);
        Assert.Equal(shown ? InputAdjoined.Right : InputAdjoined.None, input.Adjoined);
    }

    [AvaloniaFact]
    public void Adjoined_and_the_trailing_flag_follow_the_props()
    {
        var input = new Input();
        using var scene = Show(input);
        input.StartEnhancer = "@";
        Assert.Equal(InputAdjoined.Left, input.Adjoined);
        Assert.Contains(":adjoined-left", input.Part(Input.RootPart)!.Classes);
        input.EndEnhancer = ".com";
        Assert.Equal(InputAdjoined.Both, input.Adjoined);
        input.StartEnhancer = null;
        Assert.Equal(InputAdjoined.Right, input.Adjoined);
        Assert.DoesNotContain(":adjoined-left", input.Part(Input.RootPart)!.Classes);

        Assert.DoesNotContain(":has-icon-trailing", input.Classes);
        input.Clearable = true;
        Assert.Contains(":has-icon-trailing", input.Part(Input.RootPart)!.Classes);
        input.Clearable = false;
        input.Type = InputType.Password;
        Assert.Contains(":has-icon-trailing", input.Part(Input.RootPart)!.Classes);
        input.Type = InputType.Text;
        Assert.DoesNotContain(":has-icon-trailing", input.Classes);
        Assert.False(P<Visual>(input, BaseInput.MaskToggleButtonPart).IsVisible);
    }

    [AvaloniaFact]
    public void Before_and_After_show_the_text_boxs_inner_content()
    {
        var before = new TextBlock { Text = "B" };
        var after = new TextBlock { Text = "A" };
        var input = new Input { InnerLeftContent = before, InnerRightContent = after };
        using var scene = Show(input);
        Assert.Same(input.Part(BaseInput.BeforePart), before.GetVisualParent());
        Assert.Same(input.Part(BaseInput.AfterPart), after.GetVisualParent());
        Assert.True(InputLayoutTests.At(before, input).Right <= InputLayoutTests.At(P<Visual>(input, BaseInput.InputPart), input).X);
    }

    [AvaloniaFact]
    public void Overrides_merge_over_each_parts_theme()
    {
        var input = new Input
        {
            StartEnhancer = "@", Clearable = true, Type = InputType.Password, Text = "Value",
            Overrides =
            [
                new Override { Part = Input.RootPart, Style = Theme<Border>(Border.BorderBrushProperty, Red) },
                new Override { Part = Input.StartEnhancerPart, Style = Theme<ContentPresenter>(ContentPresenter.BackgroundProperty, Red) },
                new Override { Part = BaseInput.InputContainerPart, Style = Theme<Border>(Border.BackgroundProperty, Red) },
                new Override { Part = BaseInput.InputPart, Style = Theme<InputField>(InputField.CaretBrushProperty, Red) },
                new Override { Part = BaseInput.ClearIconPart, Style = Theme<InputIcon>(InputIcon.ForegroundProperty, Red) },
                new Override { Part = BaseInput.MaskToggleShowIconPart, Style = Theme<InputIcon>(Layoutable.WidthProperty, 30.0) },
            ],
        };
        using var scene = Show(input);
        Assert.Same(Red, P<Border>(input, Input.RootPart).BorderBrush);
        Assert.Same(Red, P<ContentPresenter>(input, Input.StartEnhancerPart).Background);
        Assert.Same(Red, P<Border>(input, BaseInput.InputContainerPart).Background);
        Assert.Same(Red, P<InputField>(input, BaseInput.InputPart).CaretBrush);
        Assert.Same(Red, P<InputIcon>(input, BaseInput.ClearIconPart).Foreground);
        Assert.Equal(30, P<InputIcon>(input, BaseInput.MaskToggleShowIconPart).Bounds.Width);
        // Merged over the part's theme: the root keeps its radius, the icon its height and path.
        Assert.Equal(new CornerRadius(InputOracle.Value.Layout.First(c => c.Size == "default").RootRadius), P<Border>(input, Input.RootPart).CornerRadius);
        var size = InputOracle.Value.Layout.First(c => c.Size == "default" && c.Toggle is not null).Toggle!.ShowIconSize;
        Assert.Equal(size, P<InputIcon>(input, BaseInput.MaskToggleShowIconPart).Bounds.Height);
        Assert.NotNull(P<InputIcon>(input, BaseInput.MaskToggleShowIconPart).Data);

        // Even over a state style: an override's plain setter wins (ADR 0001, item 6).
        scene.Key(Key.Tab);
        Assert.Same(Red, P<Border>(input, Input.RootPart).BorderBrush);
    }

    [AvaloniaFact]
    public void The_icons_are_Base_Webs_paths()
    {
        var input = new Input { Clearable = true, Type = InputType.Password, Text = "1234" };
        using var scene = Show(input);
        scene.Click(P<Visual>(input, BaseInput.MaskToggleButtonPart));
        foreach (var (part, name) in new[] { (BaseInput.ClearIconPart, "clear"), (BaseInput.MaskToggleShowIconPart, "show"), (BaseInput.MaskToggleHideIconPart, "hide") })
        {
            var facts = InputOracle.Value.Icons[name];
            var icon = P<InputIcon>(input, part);
            var expected = Geometry.Parse(facts.D);
            Assert.NotNull(icon.Data);
            Assert.Equal(expected.Bounds, icon.Data.Bounds);
            Assert.Equal(new Rect(facts.ViewBox[0], facts.ViewBox[1], facts.ViewBox[2], facts.ViewBox[3]), icon.ViewBox);
            // Filled by the SVG's rule: the same points inside as the path drawn by that rule, over the whole viewBox.
            var byRule = Geometry.Parse((facts.FillRule == "evenodd" ? "F0 " : "F1 ") + facts.D);
            for (var x = 0.25; x < facts.ViewBox[2]; x += 0.5)
            {
                for (var y = 0.25; y < facts.ViewBox[3]; y += 0.5)
                {
                    Assert.True(byRule.FillContains(new Point(x, y)) == icon.Data.FillContains(new Point(x, y)), $"{name} at ({x}, {y})");
                }
            }
        }
    }

    [AvaloniaFact]
    public void Transitions_wait_for_load()
    {
        var input = new Input { StartEnhancer = "@" };
        var host = new Border { Child = input };
        using var scene = Show(host);
        var t = InputOracle.Value.Transitions;
        void Check(string part, string[] css, params AvaloniaProperty[] properties)
        {
            var element = P<Animatable>(input, part);
            var transitions = Assert.IsType<Transitions>(element.Transitions);
            Assert.Equal(properties, transitions.OfType<BrushTransition>().Select(x => (AvaloniaProperty)x.Property!));
            var facts = t[css[0]];
            Assert.Equal(css[1..], facts.Properties);
            Assert.All(transitions.OfType<BrushTransition>(), x => Assert.Equal(TimeSpan.FromSeconds(facts.Seconds), x.Duration));
        }

        Check(Input.RootPart, ["root", "border"], Border.BorderBrushProperty);
        Check(Input.StartEnhancerPart, ["enhancer", "color", "background-color"], ContentPresenter.ForegroundProperty, ContentPresenter.BackgroundProperty);
        Check(BaseInput.InputContainerPart, ["container", "background-color"], Border.BackgroundProperty);

        host.Child = null;
        scene.Pump();
        Assert.Null(P<Animatable>(input, Input.RootPart).Transitions);
    }

    [AvaloniaFact]
    public void The_clear_icon_is_a_button_to_UI_Automation_and_its_invoke_clears()
    {
        var input = new Input { Clearable = true, Text = "Thing" };
        using var scene = Show(input);
        var peer = ControlAutomationPeer.CreatePeerForElement(P<Control>(input, BaseInput.ClearIconPart));
        Assert.Equal(BaseInput.ClearLabel, peer.GetName());
        Assert.True(peer.IsControlElement());
        Assert.True(peer.IsContentElement());
        ((IInvokeProvider)peer).Invoke();
        scene.Pump();
        Assert.Equal("", input.Text);

        var toggleInput = new Input { Type = InputType.Password };
        ((StackPanel)input.Parent!).Children.Add(toggleInput);
        scene.Pump();
        var toggle = ControlAutomationPeer.CreatePeerForElement(P<Control>(toggleInput, BaseInput.MaskToggleButtonPart));
        Assert.Equal(AutomationControlType.Button, toggle.GetAutomationControlType());
        Assert.Equal(BaseInput.ShowPasswordLabel, toggle.GetName());
        ((IInvokeProvider)toggle).Invoke();
        scene.Pump();
        Assert.False(toggleInput.IsMasked);

        // Not where a click can't: a read-only input has no clear icon.
        input.Text = "Thing";
        input.IsReadOnly = true;
        scene.Pump();
        ((IInvokeProvider)peer).Invoke();
        Assert.Equal("Thing", input.Text);

        // The input itself is Avalonia's text box to UI Automation: an edit with its value.
        Assert.Equal(AutomationControlType.Edit, ControlAutomationPeer.CreatePeerForElement(input).GetAutomationControlType());
    }

    private static ControlTheme Theme<T>(AvaloniaProperty property, object value) =>
        new(typeof(T)) { Setters = { new Setter(property, value) } };
}
