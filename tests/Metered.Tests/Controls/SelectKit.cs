using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Simple;
using Metered.Controls;
using Metered.Themes;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>What the select's tests share: a window with an overlay layer, a select's props from the oracle, its parts, real input.</summary>
internal static class SelectKit
{
    /// <summary>A window with a template (SimpleTheme's), whose overlay layer the dropdown is shown in, as an app's.</summary>
    public static Scene Show(Control content, ThemeVariant? variant = null) =>
        new(content, variant, themes: [new SimpleTheme(), new BaseTheme()]);

    /// <summary>A select in a page: a column with room below it, and a box outside it to click.</summary>
    public static (Scene Scene, Border Outside) Page(Select select, ThemeVariant? variant = null)
    {
        var outside = new Border { Height = 200, Background = Brushes.Transparent };
        var page = new StackPanel { Margin = new Thickness(20), Spacing = 400, Children = { select, outside } };
        return (Show(page, variant), outside);
    }

    /// <summary>Base Web's props by name, as the oracle's cases give them.</summary>
    public static void Apply(Select select, IReadOnlyDictionary<string, JsonElement>? props)
    {
        foreach (var (k, v) in props ?? new Dictionary<string, JsonElement>())
        {
            switch (k)
            {
                case "searchable": select.Searchable = v.GetBoolean(); break;
                case "openOnClick": select.OpenOnClick = v.GetBoolean(); break;
                case "closeOnSelect": select.CloseOnSelect = v.GetBoolean(); break;
                case "ignoreCase": select.IgnoreCase = v.GetBoolean(); break;
                case "escapeClearsValue": select.EscapeClearsValue = v.GetBoolean(); break;
                case "onBlurResetsInput": select.OnBlurResetsInput = v.GetBoolean(); break;
                case "onCloseResetsInput": select.OnCloseResetsInput = v.GetBoolean(); break;
                case "onSelectResetsInput": select.OnSelectResetsInput = v.GetBoolean(); break;
                case "multi": select.Multi = v.GetBoolean(); break;
                case "backspaceClearsInputValue": select.BackspaceClearsInputValue = v.GetBoolean(); break;
                case "filterOutSelected": select.FilterOutSelected = v.GetBoolean(); break;
                case "creatable": select.Creatable = v.GetBoolean(); break;
                case "disabled": select.IsEnabled = !v.GetBoolean(); break;
                case "value": break;
                default: throw new InvalidOperationException("a prop the tests don't set: " + k);
            }
        }
    }

    public static T P<T>(this Select select, string part) where T : class => (select.Part(part) as T) ?? throw new InvalidOperationException("no part " + part);

    public static bool Shown(this Select select, string part) => select.Part(part) is Visual { IsVisible: true } v && v.IsEffectivelyVisible;

    /// <summary>A point in the control's box, off the values, the input and the icons: its middle, a little to the right.</summary>
    public static Point ControlPoint(Scene scene, Select select)
    {
        var control = select.P<Control>(Select.ControlContainerPart);
        return control.TranslatePoint(new Point(control.Bounds.Width * 0.6, control.Bounds.Height / 2), scene.Window)!.Value;
    }

    public static void ClickAt(Scene scene, Point at)
    {
        scene.Window.MouseMove(at);
        scene.Window.MouseDown(at, MouseButton.Left);
        scene.Window.MouseUp(at, MouseButton.Left);
        scene.Pump();
    }

    /// <summary>A finger's tap. Nothing runs between its down and its up: a machine under load would make it a hold (Avalonia's Holding), which no tap follows.</summary>
    public static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    /// <summary>A finger's drag, its first move straight after its down, before a hold could start.</summary>
    public static void Drag(Scene scene, Point at, double dy = -90)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        for (var i = 1; i <= 10; i++)
        {
            scene.Window.TouchMove(touch, at + new Vector(0, dy * i / 10), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, at + new Vector(0, dy), RawInputModifiers.None);
        scene.Pump();
    }

    public static void Key(Scene scene, Key key, bool shift = false)
    {
        var m = shift ? RawInputModifiers.Shift : RawInputModifiers.None;
        scene.Window.KeyPress(key, m, PhysicalKey.None, null);
        scene.Window.KeyRelease(key, m, PhysicalKey.None, null);
        scene.Pump();
    }

    /// <summary>The option of that label in the open dropdown.</summary>
    public static MenuOptionBase Option(Select select, string label)
    {
        var menu = select.DropdownMenu ?? throw new InvalidOperationException("the dropdown is closed");
        return menu.Elements(Menu.OptionPart).OfType<MenuOptionBase>().Single(o => (o.Item is SelectOption s && (string?)s.Label == label)
            || (o.Item is SelectOption { IsCreatable: true } c && label == "Create " + c.Label));
    }

    /// <summary>The labels of the options the dropdown lists now, a creatable one as Base Web's getOptionLabel says it.</summary>
    public static string[] Shown(Select select) =>
        [.. select.ShownOptions.Select(o => o is SelectOption { IsCreatable: true } c ? Select.CreateLabel + " “" + c.Label + "”" : Select.LabelText(o))];

    public static string[] Labels(IEnumerable<object>? value) => [.. (value ?? []).Select(Select.LabelText)];
}
