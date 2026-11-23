using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// The menu's touch, pointer, keys and API beyond the container's state (MenuBehaviourTests): a finger's drag over a long
/// menu scrolls it and chooses nothing, a tap highlights and chooses; a press let go elsewhere chooses nothing; Page Down a
/// browser's; keyboardControlNode as Select uses it; the overrides; an href option; UI Automation; items that change. Each
/// expectation is Base Web's, its file named.
/// </summary>
public class MenuTests
{
    internal static List<object> Numbered(int n, Func<int, bool>? disabled = null) =>
        [.. Enumerable.Range(0, n).Select(i => (object)new MenuItemData { Label = "Item " + i, Disabled = disabled?.Invoke(i) ?? false })];

    internal static OverrideCollection Height(double height, OverrideCollection? more = null)
    {
        var style = new ControlTheme(typeof(MenuList));
        style.Setters.Add(new Setter(Layoutable.HeightProperty, height));
        var all = new OverrideCollection { new Override { Part = Menu.ListPart, Style = style } };
        if (more is not null) all.AddRange(more);
        return all;
    }

    internal static void Tap(Scene scene, Point at)
    {
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        scene.Window.TouchEnd(touch, at, RawInputModifiers.None);
        scene.Pump();
    }

    // ---- Touch first ----

    // A phone's browser sends no mouse events for a drag that scrolls: nothing is focused, highlighted or chosen.
    [AvaloniaFact]
    public void A_fingers_drag_over_a_long_menu_scrolls_it_and_highlights_and_chooses_nothing()
    {
        var menu = MenuKit.Make(Numbered(30), Height(300));
        var chosen = new List<object>();
        menu.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = new Scene(menu);
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        var at = scene.CentreOf(menu.OptionAt(4)!);
        var touch = scene.Window.TouchBegin(at, RawInputModifiers.None);
        scene.Pump();
        for (var i = 1; i <= 15; i++)
        {
            scene.Window.TouchMove(touch, at - new Vector(0, 6 * i), RawInputModifiers.None);
            scene.Pump();
        }

        scene.Window.TouchEnd(touch, at - new Vector(0, 90), RawInputModifiers.None);
        scene.Pump();
        Assert.True(list.Offset.Y > 40, $"the list moved {list.Offset.Y}");
        Assert.Equal(-1, menu.HighlightedIndex);
        Assert.False(menu.IsMenuFocused);
        Assert.Empty(chosen);
    }

    // A tap is the mouse events a phone's browser sends as the finger lifts: mouseover (focusMenu), mouseenter (the
    // highlight: stateful-container.js's mouseEnter change), then the click.
    [AvaloniaFact]
    public void A_tap_highlights_an_option_and_chooses_it_and_a_disabled_one_neither()
    {
        var menu = MenuKit.Make(Numbered(6, i => i == 2));
        var chosen = new List<object>();
        var changes = new List<MenuStateChangeType>();
        menu.ItemSelect += (_, e) => chosen.Add(e.Item);
        menu.StateReducer = (type, next, _) => { changes.Add(type); return next; };
        using var scene = new Scene(menu);
        Tap(scene, scene.CentreOf(menu.OptionAt(3)!));
        Assert.Equal(3, menu.HighlightedIndex);
        Assert.True(menu.IsMenuFocused);
        Assert.Same(menu.FlatItems[3], Assert.Single(chosen));
        Assert.Equal([MenuStateChangeType.Focus, MenuStateChangeType.MouseEnter, MenuStateChangeType.Click], changes);
        // A finger's tap isn't a keyboard's focus: no outline.
        Assert.DoesNotContain(":focus-visible", ((MenuList)menu.Part(Menu.ListPart)!).Classes);

        Tap(scene, scene.CentreOf(menu.OptionAt(2)!));
        Assert.Equal(3, menu.HighlightedIndex);
        Assert.Single(chosen);
    }

    // A click is a press and a release on the same element: let go on another, it is the list's, not an option's.
    [AvaloniaFact]
    public void A_press_let_go_on_another_option_chooses_nothing_and_a_click_chooses_once()
    {
        var menu = MenuKit.Make(Numbered(6));
        var chosen = new List<object>();
        menu.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = new Scene(menu);
        var from = scene.CentreOf(menu.OptionAt(1)!);
        var to = scene.CentreOf(menu.OptionAt(3)!);
        scene.Window.MouseMove(from);
        scene.Window.MouseDown(from, MouseButton.Left);
        scene.Window.MouseMove(to);
        scene.Window.MouseUp(to, MouseButton.Left);
        scene.Pump();
        Assert.Empty(chosen);

        scene.Click(menu.OptionAt(4)!);
        Assert.Same(menu.FlatItems[4], Assert.Single(chosen));
        // A right click isn't a click.
        var at = scene.CentreOf(menu.OptionAt(5)!);
        scene.Window.MouseDown(at, MouseButton.Right);
        scene.Window.MouseUp(at, MouseButton.Right);
        scene.Pump();
        Assert.Single(chosen);
    }

    // menu.js gives every option the menu's $isFocused as it changes: the highlighted one's text and its selection follow.
    [AvaloniaFact]
    public void The_menus_focus_reaches_every_option_as_it_changes()
    {
        var menu = MenuKit.Make(Numbered(3), initial: new MenuInitialState(1, null));
        var elsewhere = new Border { Focusable = true, Height = 20 };
        using var scene = new Scene(new StackPanel { Children = { elsewhere, menu } });
        var boxes = Enumerable.Range(0, 3).Select(i => (StyledElement)((MenuOptionList)menu.OptionAt(i)!).Part(MenuOptionList.ListItemPart)!).ToList();
        var selected = MenuKit.Peer(menu.OptionAt(1)!).GetProvider<ISelectionItemProvider>()!;
        Assert.All(boxes, b => Assert.DoesNotContain(":is-focused", b.Classes));
        Assert.False(selected.IsSelected);
        ((MenuList)menu.Part(Menu.ListPart)!).Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.All(boxes, b => Assert.Contains(":is-focused", b.Classes));
        Assert.True(selected.IsSelected);
        elsewhere.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.All(boxes, b => Assert.DoesNotContain(":is-focused", b.Classes));
        Assert.False(selected.IsSelected);
    }

    // menu.js's onKeyDown: the keys are the container's only while the menu is focused, its list's focus or not (here a
    // reducer refuses the focus its list's onFocus asks for).
    [AvaloniaFact]
    public void Keys_on_the_list_do_nothing_while_the_menu_isnt_focused()
    {
        var menu = MenuKit.Make(Numbered(5));
        menu.StateReducer = (type, next, current) => type == MenuStateChangeType.Focus ? current : next;
        using var scene = new Scene(menu);
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        list.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(list.IsFocused);
        Assert.False(menu.IsMenuFocused);
        scene.Key(Key.Down);
        MenuKit.Press(scene, "i");
        Assert.Equal(-1, menu.HighlightedIndex);
    }

    // Base Web doesn't prevent Page Up, Page Down or Space: a browser scrolls the focused list by them.
    [AvaloniaFact]
    public void Page_Down_and_Space_scroll_the_focused_list_as_a_browser_does_and_move_no_highlight()
    {
        var menu = MenuKit.Make(Numbered(30), Height(300));
        using var scene = new Scene(menu);
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        list.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.Equal(0, menu.HighlightedIndex);
        scene.Key(Key.PageDown);
        var page = list.Offset.Y;
        Assert.True(page > 0);
        Assert.Equal(0, menu.HighlightedIndex);
        // Each is a letter of type-ahead's too ("PageDown", then "PageDown "), which matches nothing and keeps the
        // highlight, scrolled into view as handleAlphaDown does (to the top), before the browser's page.
        MenuKit.Press(scene, " ");
        Assert.Equal(page, list.Offset.Y);
        Assert.Equal(0, menu.HighlightedIndex);
        scene.Key(Key.PageUp);
        Assert.Equal(0, list.Offset.Y);
    }


    // ---- A mouse at rest ----

    // mouseenter and mouseover fire as the mouse comes onto an element, not as it moves on it: the keys' highlight stays.
    [AvaloniaFact]
    public void A_mouse_moving_on_one_option_doesnt_take_the_highlight_back_from_the_keys_nor_focus_the_menu_again()
    {
        var menu = MenuKit.Make(Numbered(8));
        var elsewhere = new Border { Focusable = true, Height = 20 };
        using var scene = new Scene(new StackPanel { Children = { elsewhere, menu } });
        var at = scene.CentreOf(menu.OptionAt(2)!);
        scene.Window.MouseMove(at);
        scene.Pump();
        Assert.Equal(2, menu.HighlightedIndex);
        Assert.True(menu.IsMenuFocused);
        scene.Key(Key.Down);
        scene.Key(Key.Down);
        Assert.Equal(4, menu.HighlightedIndex);
        scene.Window.MouseMove(at + new Vector(1, 1));
        scene.Pump();
        Assert.Equal(4, menu.HighlightedIndex);

        elsewhere.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.False(menu.IsMenuFocused);
        scene.Window.MouseMove(at + new Vector(2, 2));
        scene.Pump();
        Assert.False(menu.IsMenuFocused);
        // Onto another option: mouseover, then mouseenter.
        scene.Window.MouseMove(scene.CentreOf(menu.OptionAt(3)!));
        scene.Pump();
        Assert.True(menu.IsMenuFocused);
        Assert.Equal(3, menu.HighlightedIndex);
    }

    // ---- Made again ----

    // An option's template is a theme's to replace, after its item has come: it keeps the theme's priority.
    [AvaloniaFact]
    public void An_Option_overrides_template_replaces_the_options_own()
    {
        var style = new ControlTheme(typeof(MenuOptionList));
        style.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
            new Avalonia.Controls.Templates.FuncControlTemplate<MenuOptionList>((o, _) => new TextBlock { Text = "custom " + o.Item })));
        var menu = MenuKit.Make(["a", "b"], [new Override { Part = Menu.OptionPart, Style = style }]);
        using var scene = new Scene(menu);
        Assert.Equal("custom a", Assert.IsType<TextBlock>(menu.OptionAt(0)!.GetVisualChildren().Single()).Text);
    }

    // Made again (a new label function here), an option's template is still a theme's to give: no local value over it.
    [AvaloniaFact]
    public void An_option_made_again_still_takes_a_themes_template()
    {
        var menu = MenuKit.Make(["a", "b"]);
        using var scene = new Scene(menu);
        var option = (MenuOptionList)menu.OptionAt(0)!;
        option.GetItemLabel = item => "label " + item;
        scene.Pump();
        Assert.Equal("label a", ((ContentPresenter)option.Part(MenuOptionList.ListItemPart)!).Content);
        var theme = new ControlTheme(typeof(MenuOptionList));
        theme.Setters.Add(new Setter(TemplatedControl.TemplateProperty,
            new Avalonia.Controls.Templates.FuncControlTemplate<MenuOptionList>((o, _) => new TextBlock { Text = "themed" })));
        option.Theme = theme;
        scene.Pump();
        Assert.Equal("themed", Assert.IsType<TextBlock>(option.GetVisualChildren().Single()).Text);
    }

    // A control has one parent: the options made again for the same items take their labels, their child menus and the
    // empty state's message from the ones before.
    [AvaloniaFact]
    public void Made_again_the_options_take_the_controls_of_the_ones_before()
    {
        var label = new TextBlock { Text = "a control" };
        var child = new Menu { ItemsSource = new List<object> { "x", "y" } };
        var props = new ControlTheme(typeof(MenuOptionList));
        props.Setters.Add(new Setter(MenuOptionBase.GetChildMenuProperty, (Func<object, object?>)(item => item is "parent" ? child : null)));
        var items = new ObservableCollection<object> { new MenuItemData { Label = label }, "parent" };
        var menu = new Menu { ItemsSource = items, Overrides = [new Override { Part = Menu.OptionPart, Props = props }] };
        using var scene = PopoverTests.Show(menu);
        items.Add("more");
        scene.Pump();
        Assert.Same(label, ((ContentPresenter)((MenuOptionList)menu.OptionAt(0)!).Part(MenuOptionList.ListItemPart)!).Content);
        Assert.True(label.IsAttachedToVisualTree());
        menu.HighlightedIndex = 1;
        PopoverTests.Settle(scene);
        Assert.Same(child, menu.OptionAt(1)!.ChildMenuPopover!.Content);
        Assert.True(child.IsAttachedToVisualTree());

        var message = new TextBlock { Text = "none" };
        var empty = new Menu { NoResultsMsg = message };
        using var other = new Scene(empty);
        empty.Overrides = [];
        other.Pump();
        Assert.Same(message, ((ContentPresenter)empty.Part(Menu.EmptyStatePart)!).Content);
        Assert.True(message.IsAttachedToVisualTree());
    }

    // ---- Leaving the tree ----

    [AvaloniaFact]
    public void Out_of_the_tree_a_menu_lets_its_letters_and_its_collection_go()
    {
        var items = new ObservableCollection<object>(MenuOracle.Items("FRUIT", null));
        var menu = new Menu { ItemsSource = items };
        var host = new Border { Child = menu };
        using var scene = new Scene(host);
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        list.Focus(NavigationMethod.Tab);
        scene.Pump();
        MenuKit.Press(scene, "b");
        Assert.Equal(2, menu.HighlightedIndex);
        host.Child = null;
        scene.Pump();
        items.Add(new MenuItemData { Label = "Date" });
        Assert.Equal(6, menu.Elements(Menu.OptionPart).Count);
        host.Child = menu;
        scene.Pump();
        Assert.Equal(7, menu.Elements(Menu.OptionPart).Count);
        // Within type-ahead's 500 ms, but its letters went with it: "c", not "bc".
        ((MenuList)menu.Part(Menu.ListPart)!).Focus(NavigationMethod.Tab);
        scene.Pump();
        MenuKit.Press(scene, "c");
        Assert.Equal(4, menu.HighlightedIndex);
    }

    // ---- A child's keys ----

    // A key the child's content leaves goes through its option to the parent's list: not the parent's to scroll by.
    [AvaloniaFact]
    public void Space_in_a_child_menus_content_doesnt_scroll_the_parent()
    {
        var field = new Input();
        var props = new ControlTheme(typeof(MenuOptionList));
        props.Setters.Add(new Setter(MenuOptionBase.GetChildMenuProperty, (Func<object, object?>)(item => item is "Item 1" ? field : null)));
        var parent = new Menu
        {
            ItemsSource = Enumerable.Range(0, 30).Select(i => (object)("Item " + i)).ToList(),
            Overrides = Height(200, [new Override { Part = Menu.OptionPart, Props = props }]),
            Width = 200, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top,
        };
        using var scene = new Scene(parent, width: 600, themes: [new Avalonia.Themes.Simple.SimpleTheme(), new Metered.Themes.BaseTheme()]);
        parent.HighlightedIndex = 1;
        PopoverTests.Settle(scene);
        field.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(field.IsFocused);
        MenuKit.Press(scene, " ");
        scene.Key(Key.PageDown);
        Assert.Equal(0, ((MenuList)parent.Part(Menu.ListPart)!).Offset.Y);
    }

    // ---- keyboardControlNode (dropdown.js: Select's input keeps the focus, the menu follows its keys) ----

    [AvaloniaFact]
    public void A_keyboard_control_node_drives_the_menu_while_it_is_focused()
    {
        var input = new Input();
        var menu = MenuKit.Make(Numbered(5), initial: new MenuInitialState(1, true));
        menu.KeyboardControlNode = input;
        menu.TypeAhead = false;
        menu.ForceHighlight = true;
        var chosen = new List<object>();
        menu.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = PopoverTests.Show(new StackPanel { Children = { input, menu } });
        input.Focus(NavigationMethod.Tab);
        scene.Pump();
        scene.Key(Key.Down);
        scene.Key(Key.Down);
        Assert.Equal(3, menu.HighlightedIndex);
        scene.Key(Key.Enter);
        Assert.Same(menu.FlatItems[3], Assert.Single(chosen));
        Assert.True(input.IsFocused);

        // The list's own keys are the node's now: focused, it takes none.
        var list = (MenuList)menu.Part(Menu.ListPart)!;
        list.Focus(NavigationMethod.Tab);
        scene.Pump();
        scene.Key(Key.Down);
        Assert.Equal(3, menu.HighlightedIndex);

        // A key the menu takes is its own (Base Web's preventDefault): the text's caret stays.
        input.Focus(NavigationMethod.Tab);
        input.Text = "abc";
        input.CaretIndex = 3;
        menu.IsMenuFocused = true;
        scene.Pump();
        scene.Key(Key.Home);
        Assert.Equal(0, menu.HighlightedIndex);
        Assert.Equal(3, input.CaretIndex);

        // Unfocused, the menu follows no key of the node's.
        list.Focus(NavigationMethod.Tab);
        input.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.False(menu.IsMenuFocused);
        scene.Key(Key.Down);
        Assert.Equal(0, menu.HighlightedIndex);
    }

    // ---- Overrides ----

    // An override's plain value replaces the default's however the default reached it (ADR 0001, item 6).
    [AvaloniaFact]
    public void Overrides_reach_the_list_each_option_header_divider_and_the_empty_state()
    {
        Override Style(string part, Type type, AvaloniaProperty property, object value)
        {
            var theme = new ControlTheme(type);
            theme.Setters.Add(new Setter(property, value));
            return new Override { Part = part, Style = theme };
        }

        var fill = new SolidColorBrush(Colors.Transparent);
        var overrides = new OverrideCollection
        {
            Style(Menu.ListPart, typeof(MenuList), Layoutable.WidthProperty, 222.0),
            Style(MenuOptionList.ListItemPart, typeof(ContentPresenter), ContentPresenter.BackgroundProperty, fill),
            Style(Menu.OptgroupHeaderPart, typeof(TextBlock), TextBlock.PaddingProperty, new Thickness(1)),
            Style(Menu.MenuDividerPart, typeof(Border), Layoutable.MarginProperty, new Thickness(2)),
            Style(Menu.EmptyStatePart, typeof(ContentPresenter), ContentPresenter.PaddingProperty, new Thickness(3)),
        };
        var group = new MenuGroup { Label = "G", Items = { new MenuItemData { Label = "a" }, new MenuItemData { Divider = true } } };
        var menu = MenuKit.Make([group], overrides, new MenuInitialState(0, true));
        var empty = MenuKit.Make([], overrides);
        using var scene = new Scene(new StackPanel { Children = { menu, empty } });
        Assert.Equal(222, ((MenuList)menu.Part(Menu.ListPart)!).Bounds.Width);
        // Over the highlight's own fill, a state style's.
        Assert.Same(fill, ((ContentPresenter)((MenuOptionList)menu.OptionAt(0)!).Part(MenuOptionList.ListItemPart)!).Background);
        Assert.Equal(new Thickness(1), ((TextBlock)menu.Part(Menu.OptgroupHeaderPart)!).Padding);
        Assert.Equal(new Thickness(2), ((Border)menu.Part(Menu.MenuDividerPart)!).Margin);
        Assert.Equal(new Thickness(3), ((ContentPresenter)empty.Part(Menu.EmptyStatePart)!).Padding);
    }

    // menu.js spreads the Option override's props last: an option's own overrides replace the menu's for its parts
    // (dropdown.js gives the options a ListItem of its own this way).
    [AvaloniaFact]
    public void An_options_own_overrides_replace_the_menus_for_its_parts()
    {
        var own = new ControlTheme(typeof(ContentPresenter));
        own.Setters.Add(new Setter(ContentPresenter.PaddingProperty, new Thickness(5)));
        var menus = new ControlTheme(typeof(ContentPresenter));
        menus.Setters.Add(new Setter(ContentPresenter.PaddingProperty, new Thickness(9)));
        var props = new ControlTheme(typeof(MenuOptionList));
        props.Setters.Add(new Setter(MenuOptionBase.OverridesProperty, new OverrideCollection { new Override { Part = MenuOptionList.ListItemPart, Style = own } }));
        var menu = MenuKit.Make(Numbered(2), [new Override { Part = MenuOptionList.ListItemPart, Style = menus }, new Override { Part = Menu.OptionPart, Props = props }]);
        using var scene = new Scene(menu);
        Assert.Equal(new Thickness(5), ((ContentPresenter)((MenuOptionList)menu.OptionAt(0)!).Part(MenuOptionList.ListItemPart)!).Padding);
    }

    // The documentation's Overrides note: an Option override's component, OptionProfile, makes profile options.
    [AvaloniaFact]
    public void An_Option_override_whose_theme_targets_the_profile_option_makes_profile_options()
    {
        var menu = MenuKit.Make([new Person("A", "B", "C")], MenuKit.OptionProps(profile: true));
        using var scene = new Scene(menu);
        Assert.IsType<MenuOptionProfile>(menu.OptionAt(0));
        menu.Overrides = null;
        scene.Pump();
        Assert.IsType<MenuOptionList>(menu.OptionAt(0));
        menu.Overrides = [new Override { Part = Menu.OptionPart, Component = new ControlTheme(typeof(MenuOptionProfile)) }];
        scene.Pump();
        Assert.IsType<MenuOptionProfile>(menu.OptionAt(0));
    }

    // ---- Links ----

    // option-list.js: an href with renderHrefAsAnchor is a link round the label; choosing it is a click on the option all
    // the same. A link without a scheme is opened by https; a relative one, which a page would resolve, not at all.
    [AvaloniaFact]
    public void An_href_option_is_chosen_by_a_click_on_its_link_and_its_link_is_resolved_as_a_browser_would()
    {
        var menu = MenuKit.Make(MenuOracle.Items("HREF", null));
        var chosen = new List<object>();
        menu.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = new Scene(menu);
        var anchor = (Visual)((MenuOptionList)menu.OptionAt(1)!).Part(MenuOptionList.ListItemAnchorPart)!;
        scene.Click(anchor);
        Assert.Equal("Orange", ((MenuItemData)Assert.Single(chosen)).Label);
        Assert.Equal(new Uri("https://www.example.com/orange"), MenuOptionList.LinkOf("//www.example.com/orange"));
        Assert.Equal(new Uri("mailto:a@example.com"), MenuOptionList.LinkOf("mailto:a@example.com"));
        Assert.Null(MenuOptionList.LinkOf("#a"));
        Assert.Null(MenuOptionList.LinkOf("/apple"));
    }


    // A browser follows the link a click was on whatever onItemSelect does: here it makes the menu again.
    [AvaloniaFact]
    public void A_links_click_follows_it_though_ItemSelect_makes_the_menu_again()
    {
        var menu = MenuKit.Make(MenuOracle.Items("HREF", null));
        menu.ItemSelect += (_, _) => menu.ItemsSource = new List<object> { "other" };
        var followed = new List<Uri>();
        void Follow(Uri u) => followed.Add(u);
        MenuOptionList.Followed += Follow;
        try
        {
            using var scene = new Scene(menu);
            scene.Click((Visual)((MenuOptionList)menu.OptionAt(2)!).Part(MenuOptionList.ListItemAnchorPart)!);
            Assert.Equal(new Uri("https://www.example.com/strawberry"), Assert.Single(followed));
            Assert.Equal("other", Assert.Single(menu.FlatItems));
        }
        finally
        {
            MenuOptionList.Followed -= Follow;
        }
    }

    /// <summary>A collection that counts who follows it.</summary>
    private sealed class Counted : List<object>, System.Collections.Specialized.INotifyCollectionChanged
    {
        private System.Collections.Specialized.NotifyCollectionChangedEventHandler? _changed;

        public int Followers { get; private set; }

        public event System.Collections.Specialized.NotifyCollectionChangedEventHandler? CollectionChanged
        {
            add { _changed += value; Followers++; }
            remove { _changed -= value; Followers--; }
        }
    }

    [AvaloniaFact]
    public void A_menu_follows_its_collection_only_while_it_is_in_the_tree()
    {
        var items = new Counted { "a" };
        var menu = new Menu { ItemsSource = items };
        Assert.Equal(0, items.Followers);
        var host = new Border { Child = menu };
        using var scene = new Scene(host);
        Assert.Equal(1, items.Followers);
        host.Child = null;
        Assert.Equal(0, items.Followers);
    }

    // A picture that isn't an image is a browser's broken image: no picture, the option drawn.
    [AvaloniaFact]
    public void A_profile_picture_that_isnt_an_image_is_none()
    {
        var path = Path.Combine(Path.GetTempPath(), $"metered-menu-{Guid.NewGuid():N}.txt");
        File.WriteAllText(path, "not an image");
        try
        {
            var props = new ControlTheme(typeof(MenuOptionProfile));
            props.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemLabelsProperty, (Func<object?, MenuProfileLabels?>)(_ => new MenuProfileLabels("Title"))));
            props.Setters.Add(new Setter(MenuOptionProfile.GetProfileItemImgProperty, (Func<object?, object?>)(_ => path)));
            var menu = MenuKit.Make(["a"], [new Override { Part = Menu.OptionPart, Props = props }]);
            using var scene = new Scene(menu);
            var option = (MenuOptionProfile)menu.OptionAt(0)!;
            Assert.Null(option.Part(MenuOptionProfile.ProfileImgPart));
            Assert.Equal("Title", ((TextBlock)option.Part(MenuOptionProfile.ProfileTitlePart)!).Text);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- UI Automation ----

    [AvaloniaFact]
    public void UI_Automation_names_the_list_and_selecting_an_option_chooses_it_but_not_a_disabled_one()
    {
        var menu = MenuKit.Make(Numbered(3, i => i == 1));
        var chosen = new List<object>();
        menu.ItemSelect += (_, e) => chosen.Add(e.Item);
        using var scene = new Scene(menu);
        var list = MenuKit.Peer((Control)menu.Part(Menu.ListPart)!);
        Assert.Equal("Menu", list.GetName());
        AutomationProperties.SetName(menu, "Actions");
        Assert.Equal("Actions", list.GetName());
        Assert.True(list.IsControlElement());
        Assert.Contains(list.GetChildren(), p => p.GetName() == "Item 0");
        // The menu round it is no element of its own in the control view.
        Assert.False(MenuKit.Peer(menu).IsControlElement());

        MenuKit.Peer(menu.OptionAt(2)!).GetProvider<ISelectionItemProvider>()!.Select();
        Assert.Same(menu.FlatItems[2], Assert.Single(chosen));
        Assert.Equal(2, menu.HighlightedIndex);
        Assert.Throws<ElementNotEnabledException>(() => MenuKit.Peer(menu.OptionAt(1)!).GetProvider<ISelectionItemProvider>()!.Select());
        Assert.Single(chosen);
    }

    // ---- Items ----

    [AvaloniaFact]
    public void The_options_follow_the_items_their_collection_and_a_groups()
    {
        var items = new ObservableCollection<object> { new MenuItemData { Label = "a" } };
        var menu = new Menu { ItemsSource = items };
        using var scene = new Scene(menu);
        Assert.Single(menu.Elements(Menu.OptionPart));
        items.Add(new MenuItemData { Label = "b" });
        scene.Pump();
        Assert.Equal(2, menu.Elements(Menu.OptionPart).Count);
        var group = new MenuGroup { Label = "G" };
        items.Add(group);
        scene.Pump();
        Assert.Single(menu.Elements(Menu.OptgroupHeaderPart));
        group.Items.Add("c");
        scene.Pump();
        Assert.Equal(3, menu.Elements(Menu.OptionPart).Count);
        // A string is its own label.
        Assert.Equal("c", ((ContentPresenter)((MenuOptionList)menu.OptionAt(2)!).Part(MenuOptionList.ListItemPart)!).Content);

        // XAML's content, when there is no ItemsSource.
        var own = new Menu { Items = { "x", new MenuItemData { Divider = true }, "y" } };
        using var other = new Scene(own);
        Assert.Equal(2, own.Elements(Menu.OptionPart).Count);
        Assert.Single(own.Elements(Menu.MenuDividerPart));
        own.ItemsSource = items;
        other.Pump();
        Assert.Equal(3, own.Elements(Menu.OptionPart).Count);
    }

    [AvaloniaFact]
    public void The_empty_states_message_is_any_content()
    {
        var message = new TextBlock { Text = "Nothing" };
        var menu = new Menu { NoResultsMsg = message };
        using var scene = new Scene(menu);
        Assert.Same(message, ((ContentPresenter)menu.Part(Menu.EmptyStatePart)!).Content);
        Assert.True(message.IsVisible && message.Bounds.Width > 0);
    }

    // ---- Keys by name ----

    // stateful-container.js reads event.key: a printable key's character, any other's name.
    [AvaloniaFact]
    public void A_key_is_named_as_the_DOM_names_it()
    {
        static string Name(Key key, string? symbol = null) => MenuKeys.Name(new KeyEventArgs { Key = key, KeySymbol = symbol });
        Assert.Equal("a", Name(Key.A));
        Assert.Equal("Q", Name(Key.Q, "Q"));
        Assert.Equal("é", Name(Key.Oem7, "é"));
        Assert.Equal(" ", Name(Key.Space));
        Assert.Equal("Shift", Name(Key.RightShift));
        Assert.Equal("Control", Name(Key.LeftCtrl));
        Assert.Equal("Meta", Name(Key.LWin));
        Assert.Equal("F5", Name(Key.F5));
        Assert.Equal("Escape", Name(Key.Escape, "\x1b"));
        Assert.Equal("Backspace", Name(Key.Back, "\b"));
        Assert.Equal("7", Name(Key.D7));
        // An input method's key, its committing Enter too: "Process" (keyCode 229), never Enter's choice.
        Assert.Equal("Process", Name(Key.ImeProcessed));
    }
}
