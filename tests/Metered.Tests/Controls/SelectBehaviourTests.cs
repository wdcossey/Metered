using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// Base Web's own Select class driven through each sequence (SelectOracle's behaviour: its state and its value after each
/// step), replayed here with real input: a mouse's click on the control or the input, a finger's tap and drag, the keys,
/// typing, a click on an option in the dropdown, on the clear icon or a tag's action, a click and a tap outside.
/// </summary>
public class SelectBehaviourTests
{
    public static TheoryData<string> Cases => new(SelectOracle.Data.Behaviour.Select(c => c.Name));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void The_state_and_value_after_each_step_are_Base_Webs(string name)
    {
        var c = SelectOracle.Data.Behaviour.Single(x => x.Name == name);
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Value = SelectOracle.Value(options, c.Value, c.Unclearable) };
        SelectKit.Apply(select, c.Props);
        var changes = new List<SelectChangeEventArgs>();
        var opens = new List<string>();
        select.Change += (_, e) => changes.Add(e);
        select.Open += (_, _) => opens.Add("open");
        select.Close += (_, _) => opens.Add("close");
        var (scene, outside) = SelectKit.Page(select);
        using var _ = scene;
        var i = 0;
        foreach (var step in c.Steps)
        {
            changes.Clear();
            opens.Clear();
            Do(scene, select, outside, step);
            var at = $"{name}, step {i++} ({step})";
            Assert.True(step.IsOpen == select.IsOpen, at + ": open " + select.IsOpen);
            Assert.True(step.IsFocused == select.IsSelectFocused, at + ": focused " + select.IsSelectFocused);
            Assert.True(step.IsPseudoFocused == select.IsPseudoFocused, at + ": pseudo-focused " + select.IsPseudoFocused);
            Assert.True(step.InputValue == select.InputValue, at + ": input '" + select.InputValue + "'");
            Assert.True(step.Value.SequenceEqual(SelectKit.Labels(select.Value)), at + ": value " + string.Join(",", SelectKit.Labels(select.Value)));
            Assert.True(step.Changes.Length == changes.Count, at + ": " + changes.Count + " changes");
            foreach (var (want, got) in step.Changes.Zip(changes))
            {
                Assert.Equal(Enum.Parse<SelectChangeType>(want.Type, ignoreCase: true), got.Type);
                Assert.Equal(want.Option, got.Option is null ? null : Select.LabelText(got.Option));
                Assert.Equal(want.Value, SelectKit.Labels(got.Value));
            }

            Assert.True(step.Opens.SequenceEqual(opens), at + ": events " + string.Join(",", opens));
            Assert.True(step.Options.SequenceEqual(SelectKit.Shown(select)), at + ": options " + string.Join(",", SelectKit.Shown(select)));
            Assert.True(step.Placeholder == select.Shown(Select.PlaceholderPart), at + ": placeholder");
            if (!select.Multi && select.Value is { Count: > 0 }) Assert.True(step.ShowValue == select.Shown(Select.SingleValuePart), at + ": single value");
            // The dropdown is shown while it is open.
            Assert.Equal(select.IsOpen, select.DropdownMenu is { IsEffectivelyVisible: true } m && m.IsAttachedToVisualTree());
        }
    }

    private static void Do(Scene scene, Select select, Border outside, SelectStep step)
    {
        if (step.Click == "control") SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        else if (step.Click == "input") scene.Click(select.P<Control>(Select.InputPart));
        else if (step.Tap == "control") SelectKit.Tap(scene, SelectKit.ControlPoint(scene, select));
        else if (step.Drag == "control") SelectKit.Drag(scene, SelectKit.ControlPoint(scene, select));
        else if (step.Clear == true) scene.Click(select.P<Control>(Select.ClearIconPart));
        else if (step.Choose is { } label) scene.Click(SelectKit.Option(select, label));
        else if (step.Remove is { } removed)
        {
            var tag = select.Tags.Single(t => Select.LabelText(t.Value) == removed);
            scene.Click((Control)tag.Part(SelectTag.ActionPart)!);
        }
        else if (step.ClickOutside == true) scene.Click(outside);
        else if (step.TapOutside == true) SelectKit.Tap(scene, scene.CentreOf(outside));
        else if (step.Key is { } key)
        {
            var k = key switch
            {
                "Backspace" => Key.Back, "Tab" => Key.Tab, "Escape" => Key.Escape, " " => Key.Space, "ArrowUp" => Key.Up, "ArrowDown" => Key.Down,
                "PageUp" => Key.PageUp, "PageDown" => Key.PageDown, "End" => Key.End, "Home" => Key.Home, "Delete" => Key.Delete, "Enter" => Key.Enter,
                _ => throw new InvalidOperationException(key),
            };
            if (k == Key.Space && select.Searchable)
            {
                // A space typed: its key and the text it makes.
                SelectKit.Key(scene, k);
                scene.Window.KeyTextInput(" ");
                scene.Pump();
            }
            else SelectKit.Key(scene, k, step.Shift == true);
        }
        else if (step.Type is { } text)
        {
            // Typing: the input's text as the keys leave it.
            var input = select.P<TextBox>(Select.InputPart);
            input.Text = text;
            input.CaretIndex = text.Length;
            scene.Pump();
        }
        else if (step.Focus == true)
        {
            select.FocusTarget!.Focus(NavigationMethod.Tab);
            scene.Pump();
        }
        else throw new InvalidOperationException("a step the tests don't take: " + step);
    }
}
