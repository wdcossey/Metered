using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// The menu's state against Base Web's own MenuStatefulContainer driven through the same steps (MenuOracle.Behaviour): real
/// keys on the focused list (arrows, Home, End, Enter, type-ahead's letters and key names, its 500 ms), keyboard focus and
/// blur, a real mouse over the list, over options and clicking, new items, initialState, forceHighlight, typeAhead and a
/// stateReducer. After each step, the highlighted index, whether the menu is focused, the items chosen and the change
/// types the reducer saw (each run of one type once: React calls focusMenu again on the focus event its own focus() raises,
/// with its state not yet applied, where Metered's has it).
/// </summary>
public class MenuBehaviourTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, MenuOracle.Data.Behaviour.Length));

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void It_does_what_Base_Webs_container_does(int index)
    {
        var c = MenuOracle.Data.Behaviour[index];
        var menu = MenuKit.Make(MenuOracle.Items(c.Items, c.ItemsGiven), initial: c.InitialState);
        menu.TypeAhead = c.TypeAhead;
        menu.ForceHighlight = c.ForceHighlight;
        var changes = new List<string>();
        var selected = new List<int>();
        menu.StateReducer = (type, next, current) =>
        {
            changes.Add(Camel(type));
            // tools/reference/menu.js's noCharacter: type-ahead's change refused.
            return c.Reducer == "noCharacter" && type == MenuStateChangeType.Character ? current : next;
        };
        menu.ItemSelect += (_, e) => selected.Add(menu.FlatItems.ToList().IndexOf(e.Item));
        var elsewhere = new Border { Focusable = true, Height = 20 };
        using var scene = new Scene(new StackPanel { Children = { elsewhere, menu } });
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        var mouse = new Point(Scene.Width - 5, Scene.Height - 5);
        scene.Window.MouseMove(mouse);
        scene.Pump();

        foreach (var step in c.Steps)
        {
            changes.Clear();
            var before = selected.Count;
            if (step.Key is { } key) MenuKit.Press(scene, key);
            else if (step.Focus) Focus(scene, list);
            else if (step.Blur) Focus(scene, elsewhere);
            else if (step.Over) Move(scene, ref mouse, list.TranslatePoint(new Point(list.Bounds.Width / 2, 3), scene.Window)!.Value);
            else if (step.Enter is { } enter) Move(scene, ref mouse, MenuKit.CentreOf(scene, menu, enter));
            else if (step.Click is { } click)
            {
                Assert.Equal(MenuKit.CentreOf(scene, menu, click), mouse);
                scene.Window.MouseDown(mouse, MouseButton.Left);
                scene.Window.MouseUp(mouse, MouseButton.Left);
                scene.Pump();
            }
            else if (step.Wait is { } ms) MenuKit.Wait(scene, ms);
            else if (step.Items is { } items)
            {
                menu.ItemsSource = MenuOracle.Items(items);
                scene.Pump();
            }

            var at = $"after {step}";
            Assert.True(step.HighlightedIndex == menu.HighlightedIndex, $"{at}: highlighted {menu.HighlightedIndex}, Base Web's {step.HighlightedIndex}");
            Assert.True(step.IsFocused == menu.IsMenuFocused, $"{at}: focused {menu.IsMenuFocused}");
            Assert.True(step.Selected.SequenceEqual(selected.Skip(before)), $"{at}: chose [{string.Join(",", selected.Skip(before))}], Base Web's [{string.Join(",", step.Selected)}]");
            Assert.True(Runs(step.Changes).SequenceEqual(Runs(changes)), $"{at}: changes [{string.Join(",", changes)}], Base Web's [{string.Join(",", step.Changes)}]");
        }
    }

    private static void Focus(Scene scene, InputElement element)
    {
        element.Focus(NavigationMethod.Tab);
        scene.Pump();
    }

    private static void Move(Scene scene, ref Point mouse, Point to)
    {
        mouse = to;
        scene.Window.MouseMove(to);
        scene.Pump();
    }

    /// <summary>Each run of one change type once.</summary>
    private static IEnumerable<string> Runs(IEnumerable<string> types) =>
        types.Aggregate(new List<string>(), (list, t) => { if (list.Count == 0 || list[^1] != t) list.Add(t); return list; });

    /// <summary>Base Web's name of a change type: STATE_CHANGE_TYPES's.</summary>
    private static string Camel(MenuStateChangeType type) => char.ToLowerInvariant(type.ToString()[0]) + type.ToString()[1..];
}
