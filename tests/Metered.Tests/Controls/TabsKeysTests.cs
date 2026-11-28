using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Headless.XUnit;
using Metered.Controls;

namespace Metered.Tests.Controls;

/// <summary>
/// The keys on a focused tab, against Base Web's own handlers driven step by step (TabsOracle: tabs.js's onKeyDown and
/// onClick, with Enter and Space a browser's click on the focused button): after each real key press, the tab focused,
/// the active key, the keys Change gave (onChange's), and, where Base Web prevents the key's default (a vertical list's
/// arrows), the key taken, so the page doesn't scroll.
/// </summary>
public class TabsKeysTests
{
    public static TheoryData<int> Cases => new(Enumerable.Range(0, TabsOracle.Data.Keys.Length));

    private static Key KeyOf(string name) => name switch
    {
        "ArrowLeft" => Key.Left,
        "ArrowRight" => Key.Right,
        "ArrowUp" => Key.Up,
        "ArrowDown" => Key.Down,
        "Home" => Key.Home,
        "End" => Key.End,
        "Enter" => Key.Enter,
        "Space" => Key.Space,
        _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
    };

    [AvaloniaTheory]
    [MemberData(nameof(Cases))]
    public void Keys_do_what_Base_Webs_do(int index)
    {
        var c = TabsOracle.Data.Keys[index];
        var tabs = TabsOracle.Make(c.Input);
        using var scene = new Scene(tabs);
        var changes = new List<string>();
        tabs.Change += (_, e) => changes.Add(e.ActiveKey);
        var taken = false;
        scene.Window.AddHandler(InputElement.KeyDownEvent, (_, e) => taken = e.Handled, RoutingStrategies.Bubble, handledEventsToo: true);
        var buttons = tabs.Elements(Tabs.TabPart).Cast<TabButton>().ToList();
        Assert.True(buttons[c.Start].Focus(NavigationMethod.Tab));
        scene.Pump();

        foreach (var step in c.Steps)
        {
            changes.Clear();
            taken = false;
            scene.Key(KeyOf(step.Key));
            Assert.Same(buttons[step.Focused], scene.Window.FocusManager!.GetFocusedElement());
            Assert.Equal(step.ActiveKey, tabs.ActiveKey);
            Assert.Equal(step.Changes, changes);
            if (step.Prevented) Assert.True(taken, step.Key + " should be taken");
        }
    }
}
