using Avalonia;
using Avalonia.Animation;
using Avalonia.Automation;
using Avalonia.Automation.Peers;
using Avalonia.Automation.Provider;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Metered.Controls;
using Menu = Metered.Controls.Menu;

namespace Metered.Tests.Controls;

/// <summary>
/// The select beyond the oracle's sequences: touch first (a finger's drag over it in a page that scrolls opens nothing, a tap
/// on an option chooses), typing by the keyboard, the dropdown's keys through the menu, a tag's keys, the back request,
/// controlRef, labels, options that change while it is open, its overrides (its own, its popover's, the dropdown's), UI
/// Automation, and its motion.
/// </summary>
public class SelectTests
{
    private static (Select Select, Scene Scene, Border Outside) Make(Action<Select>? set = null, IEnumerable<string>? value = null)
    {
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Value = SelectOracle.Value(options, value ?? []) };
        set?.Invoke(select);
        var (scene, outside) = SelectKit.Page(select);
        return (select, scene, outside);
    }

    // ---- Touch first ----

    [AvaloniaFact]
    public void A_fingers_drag_over_the_select_scrolls_the_page_and_opens_nothing()
    {
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options };
        var scroller = new ScrollViewer
        {
            Height = 400,
            Content = new StackPanel { Margin = new Thickness(20), Children = { new Border { Height = 100 }, select, new Border { Height = 1200 } } },
        };
        using var scene = SelectKit.Show(scroller);
        SelectKit.Drag(scene, SelectKit.ControlPoint(scene, select), dy: -120);
        Assert.True(scroller.Offset.Y > 60, "the page moved " + scroller.Offset.Y);
        Assert.False(select.IsOpen);
        Assert.False(select.IsSelectFocused);
        Assert.False(select.FocusTarget!.IsFocused);
    }

    [AvaloniaFact]
    public void A_tap_opens_it_and_a_tap_on_an_option_chooses_it()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        SelectKit.Tap(scene, SelectKit.ControlPoint(scene, select));
        PopoverTests.Settle(scene);
        Assert.True(select.IsOpen);
        SelectKit.Tap(scene, scene.CentreOf(SelectKit.Option(select, "Azure")));
        Assert.Equal(["Azure"], SelectKit.Labels(select.Value));
        Assert.False(select.IsOpen);
        Assert.True(select.IsSelectFocused);
    }

    [AvaloniaFact]
    public void A_fingers_drag_in_a_long_dropdown_scrolls_it_and_chooses_nothing()
    {
        var options = Enumerable.Range(1, 60).Select(i => (object)new SelectOption("n" + i, "Option " + i)).ToList();
        var select = new Select { Options = options };
        var (scene, _) = SelectKit.Page(select);
        using var _s = scene;
        SelectKit.Tap(scene, SelectKit.ControlPoint(scene, select));
        PopoverTests.Settle(scene);
        var list = (MenuList)select.DropdownMenu!.Part(Menu.ListPart)!;
        SelectKit.Drag(scene, scene.CentreOf(list), dy: -150);
        Assert.True(list.Offset.Y > 60, "the list moved " + list.Offset.Y);
        Assert.Empty(select.Value ?? []);
        Assert.True(select.IsOpen);
    }

    [AvaloniaFact]
    public void A_finger_down_outside_closes_the_dropdown_even_for_a_drag()
    {
        var (select, scene, outside) = Make();
        using var _s = scene;
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        Assert.True(select.IsOpen);
        // handleTouchOutside: touchstart, whatever follows.
        SelectKit.Drag(scene, scene.CentreOf(outside), dy: -40);
        Assert.False(select.IsOpen);
        Assert.True(select.IsPseudoFocused);
    }

    // ---- Keys and typing ----

    [AvaloniaFact]
    public void Typing_filters_and_the_arrows_and_Enter_choose_through_the_menu()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        select.FocusTarget!.Focus(NavigationMethod.Tab);
        scene.Window.KeyTextInput("aq");
        scene.Pump();
        Assert.Equal("aq", select.InputValue);
        Assert.True(select.IsOpen);
        Assert.Equal(["Aqua", "Aquamarine"], SelectKit.Shown(select));
        // forceHighlight: the first; the arrows move it, the caret stays (the menu takes them).
        var input = select.P<TextBox>(Select.InputPart);
        Assert.Equal(0, select.DropdownMenu!.HighlightedIndex);
        SelectKit.Key(scene, Key.Down);
        Assert.Equal(1, select.DropdownMenu!.HighlightedIndex);
        Assert.Equal(2, input.CaretIndex);
        SelectKit.Key(scene, Key.Enter);
        Assert.Equal(["Aquamarine"], SelectKit.Labels(select.Value));
        Assert.Equal("", select.InputValue);
        Assert.Equal("", input.Text);
        Assert.False(select.IsOpen);
    }

    [AvaloniaFact]
    public void Spaces_key_doesnt_open_a_searchable_select_its_text_does()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        select.SetInputFocus();
        scene.Pump();
        // handleKeyDown's space: a searchable select's is the input's, a character, which opens it as typing does.
        SelectKit.Key(scene, Key.Space);
        Assert.False(select.IsOpen);
        scene.Window.KeyTextInput(" ");
        scene.Pump();
        Assert.True(select.IsOpen);
        Assert.Equal(" ", select.InputValue);
    }

    [AvaloniaFact]
    public void Opened_by_a_key_the_dropdown_highlights_the_value()
    {
        var (select, scene, _) = Make(value: ["Bisque"]);
        using var _s = scene;
        select.FocusTarget!.Focus(NavigationMethod.Tab);
        SelectKit.Key(scene, Key.Down);
        PopoverTests.Settle(scene);
        Assert.True(select.IsOpen);
        var menu = select.DropdownMenu!;
        Assert.Equal("Bisque", Select.LabelText(menu.FlatItems[menu.HighlightedIndex]));
        Assert.True(menu.IsMenuFocused);
        // The input keeps the focus: the menu's list never takes it.
        Assert.True(select.FocusTarget!.IsFocused);
        scene.Click(SelectKit.Option(select, "Black"));
        Assert.True(select.FocusTarget!.IsFocused);
    }

    [AvaloniaFact]
    public void A_tags_Backspace_removes_that_tag_only_and_Tab_reaches_the_tags()
    {
        var (select, scene, _) = Make(s => s.Multi = true, ["Aqua", "Azure", "Bisque"]);
        using var _s = scene;
        var first = select.Tags[0];
        first.Focus(NavigationMethod.Tab);
        scene.Pump();
        Assert.True(first.IsFocused);
        SelectKit.Key(scene, Key.Back);
        Assert.Equal(["Azure", "Bisque"], SelectKit.Labels(select.Value));
        // removeValue's focus(): the input.
        Assert.True(select.FocusTarget!.IsFocused);
        // Shift+Tab from the input goes back to the last tag: Base Web's Tab unfocuses the select (its keydown), and the
        // blur to a tag, in the anchor, changes nothing more.
        SelectKit.Key(scene, Key.Tab, shift: true);
        Assert.True(select.Tags[^1].IsFocused);
        Assert.False(select.IsSelectFocused);
        // Delete on it: that tag, and removeValue's focus() focuses the input again.
        SelectKit.Key(scene, Key.Delete);
        Assert.Equal(["Azure"], SelectKit.Labels(select.Value));
        Assert.True(select.FocusTarget!.IsFocused);
        Assert.True(select.IsSelectFocused);
    }

    [AvaloniaFact]
    public void The_back_request_closes_the_dropdown_as_Escape_does()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        Assert.True(select.IsOpen);
        var back = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        scene.Window.RaiseEvent(back);
        scene.Pump();
        Assert.True(back.Handled);
        Assert.False(select.IsOpen);
        Assert.True(select.IsPseudoFocused);
        // Closed, back is the page's.
        var again = new RoutedEventArgs(TopLevel.BackRequestedEvent);
        scene.Window.RaiseEvent(again);
        Assert.False(again.Handled);
    }

    [AvaloniaFact]
    public void AutoFocus_focuses_the_input_once_it_is_shown_and_opens_nothing()
    {
        var (select, scene, _) = Make(s => s.AutoFocus = true);
        using var _s = scene;
        Assert.True(select.FocusTarget!.IsFocused);
        Assert.True(select.IsSelectFocused);
        Assert.False(select.IsOpen);
    }

    [AvaloniaFact]
    public void A_click_on_the_search_icon_is_the_controls()
    {
        var (select, scene, _) = Make(s => s.Type = SelectType.Search);
        using var _s = scene;
        scene.Click(select.P<Control>(Select.SearchIconPart));
        Assert.True(select.IsOpen);
        Assert.True(select.IsSelectFocused);
    }

    [AvaloniaFact]
    public void Disabled_it_takes_no_focus_and_shows_no_clear_icon()
    {
        var (select, scene, _) = Make(s => s.IsEnabled = false, ["Aqua"]);
        using var _s = scene;
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        Assert.False(select.IsOpen);
        Assert.False(select.FocusTarget!.IsFocused);
        Assert.False(select.Shown(Select.ClearIconPart));
        Assert.Contains(":disabled", select.P<Control>(Select.ControlContainerPart).Classes);
    }

    // ---- controlRef, labels, options ----

    [AvaloniaFact]
    public void ControlRefs_methods_open_set_the_text_focus_and_blur()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        select.SetDropdownOpen(true);
        scene.Pump();
        Assert.True(select.IsOpen);
        select.SetInputValue("bl");
        scene.Pump();
        Assert.Equal("bl", select.P<TextBox>(Select.InputPart).Text);
        Assert.Equal(["AliceBlue", "Black", "BlanchedAlmond"], SelectKit.Shown(select));
        select.SetDropdownOpen(false);
        select.SetInputFocus();
        scene.Pump();
        Assert.True(select.FocusTarget!.IsFocused);
        Assert.True(select.IsSelectFocused);
        select.SetInputBlur();
        scene.Pump();
        Assert.False(select.FocusTarget!.IsFocused);
    }

    [AvaloniaFact]
    public void Labels_come_from_getOptionLabel_and_getValueLabel_and_a_creatable_option_says_Create()
    {
        var states = new List<SelectOptionState>();
        var (select, scene, _) = Make(s =>
        {
            s.Creatable = true;
            s.GetOptionLabel = (o, st) =>
            {
                states.Add(st);
                return "· " + Select.LabelText(o);
            };
            s.GetValueLabel = o => Select.LabelText(o).ToUpperInvariant();
        }, ["Aqua"]);
        using var _s = scene;
        Assert.Equal("AQUA", select.P<ContentPresenter>(Select.SingleValuePart).Content);
        select.FocusTarget!.Focus(NavigationMethod.Tab);
        scene.Window.KeyTextInput("Teal");
        PopoverTests.Settle(scene);
        var option = Assert.Single(select.DropdownMenu!.Elements(Menu.OptionPart).Cast<MenuOptionList>());
        Assert.Equal("· Teal", ((SelectOptionContent)((ContentPresenter)option.Part(MenuOptionList.ListItemPart)!).Content!).Text);
        Assert.Contains(states, s => s.IsHighlighted);

        // getOptionLabel unset: Base Web's own, "Create “Teal”".
        select.GetOptionLabel = null;
        select.SetInputValue("Teal ");
        PopoverTests.Settle(scene);
        option = Assert.Single(select.DropdownMenu!.Elements(Menu.OptionPart).Cast<MenuOptionList>());
        Assert.Equal("Create “Teal”", ((SelectOptionContent)((ContentPresenter)option.Part(MenuOptionList.ListItemPart)!).Content!).Text);
        SelectKit.Key(scene, Key.Enter);
        var created = Assert.IsType<SelectOption>(Assert.Single(select.Value!));
        Assert.Equal("Teal", created.Label);
        Assert.True(created.IsCreatable);
        Assert.Equal("TEAL", select.P<ContentPresenter>(Select.SingleValuePart).Content);
    }

    [AvaloniaFact]
    public void Options_that_change_while_it_is_open_are_listed_and_a_highlight_past_the_end_goes_back_to_the_first()
    {
        var options = new AvaloniaList<object>(SelectOracle.Options("OPTIONS"));
        var select = new Select { Options = options };
        var (scene, _) = SelectKit.Page(select);
        using var _s = scene;
        select.SetDropdownOpen(true);
        PopoverTests.Settle(scene);
        SelectKit.Key(scene, Key.End);
        select.FocusTarget!.Focus(NavigationMethod.Tab);
        SelectKit.Key(scene, Key.End);
        Assert.Equal(7, select.DropdownMenu!.HighlightedIndex);
        options.RemoveAt(7);
        options.RemoveAt(6);
        scene.Pump();
        Assert.Equal(6, select.DropdownMenu!.Elements(Menu.OptionPart).Count);
        Assert.Equal(0, select.DropdownMenu!.HighlightedIndex);
    }

    [AvaloniaFact]
    public void The_dropdown_follows_the_selects_width_and_a_height_in_vh_the_windows()
    {
        var (select, scene, _) = Make(s => s.MaxDropdownHeight = "25vh");
        using var _s = scene;
        select.SetDropdownOpen(true);
        PopoverTests.Settle(scene);
        var list = (MenuList)select.DropdownMenu!.Part(Menu.ListPart)!;
        Assert.Equal(scene.Window.ClientSize.Height / 4, list.MaxHeight, 3);
        select.Width = 250;
        scene.Pump();
        Assert.Equal(250, ((Control)select.Part(Select.DropdownContainerPart)!).Bounds.Width);
        select.MaxDropdownHeight = "100px";
        scene.Pump();
        Assert.Equal(100, ((MenuList)select.DropdownMenu!.Part(Menu.ListPart)!).MaxHeight);
    }

    // ---- Overrides ----

    [AvaloniaFact]
    public void Overrides_reach_its_parts_its_popover_and_the_dropdowns()
    {
        var root = new ControlTheme(typeof(Border)) { Setters = { new Setter(Border.BackgroundProperty, Brushes.Transparent), new Setter(Layoutable.MarginProperty, new Thickness(3)) } };
        var popover = new ControlTheme(typeof(Popover)) { Setters = { new Setter(Popover.PlacementProperty, PopoverPlacement.Top) } };
        var dropdown = new ControlTheme(typeof(MenuList)) { Setters = { new Setter(Layoutable.MaxHeightProperty, 90.0) } };
        var option = new ControlTheme(typeof(MenuOptionList)) { Setters = { new Setter(MenuOptionList.SizeProperty, MenuOptionSize.Compact) } };
        var item = new ControlTheme(typeof(ContentPresenter)) { Setters = { new Setter(Layoutable.MinHeightProperty, 50.0) } };
        var content = new ControlTheme(typeof(ContentPresenter)) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(4)) } };
        var tag = new ControlTheme(typeof(SelectTag)) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(5)) } };
        var (select, scene, _) = Make(s =>
        {
            s.Multi = true;
            s.Overrides =
            [
                new Override { Part = Select.RootPart, Style = root },
                new Override { Part = Select.PopoverPart, Props = popover },
                new Override { Part = Select.DropdownPart, Style = dropdown },
                new Override { Part = Select.DropdownOptionPart, Props = option },
                new Override { Part = Select.DropdownListItemPart, Style = item },
                new Override { Part = Select.OptionContentPart, Style = content },
                new Override { Part = Select.MultiValuePart, Style = tag },
            ];
        }, ["Aqua"]);
        using var _s = scene;
        Assert.Equal(new Thickness(3), select.P<Control>(Select.RootPart).Margin);
        // MultiValue reaches the tag, as Base Web's overrides.Tag || overrides.MultiValue.
        Assert.Equal(new Thickness(5), select.Tags[0].Margin);
        select.SetDropdownOpen(true);
        PopoverTests.Settle(scene);
        // The app's props win over select-component.js's own, as Base Web spreads them after.
        Assert.Equal(PopoverPlacement.Top, select.DropdownPopover!.Placement);
        Assert.Equal(0, select.DropdownPopover!.PopoverMargin);
        var menu = select.DropdownMenu!;
        Assert.Equal(90, ((MenuList)menu.Part(Menu.ListPart)!).MaxHeight);
        var first = menu.Elements(Menu.OptionPart).Cast<MenuOptionList>().First();
        Assert.Equal(MenuOptionSize.Compact, first.Size);
        // The dropdown's getItemLabel stays under the app's option props.
        var label = Assert.IsType<SelectOptionContent>(((ContentPresenter)first.Part(MenuOptionList.ListItemPart)!).Content);
        Assert.Equal(new Thickness(4), label.Margin);
        Assert.Equal(50, ((Control)first.Part(MenuOptionList.ListItemPart)!).MinHeight);
    }

    [AvaloniaFact]
    public void A_tag_shows_any_content_its_value_label_gives()
    {
        var (select, scene, _) = Make(s =>
        {
            s.Multi = true;
            s.GetValueLabel = o => new Border { Width = 30, Height = 10, Child = new TextBlock { Text = Select.LabelText(o) } };
        }, ["Aqua"]);
        using var _s = scene;
        var text = (ContentPresenter)select.Tags[0].Part(SelectTag.TextPart)!;
        var border = Assert.IsType<Border>(text.Content);
        Assert.True(border.IsEffectivelyVisible && border.Bounds.Width == 30);
        // tag.js's aria-label: "Remove" and its children only when they are a string.
        Assert.Equal("Remove", ControlAutomationPeer.CreatePeerForElement(select.Tags[0]).GetName());
    }

    [AvaloniaFact]
    public void A_tags_label_of_the_apps_own_type_is_drawn_by_the_apps_data_template()
    {
        // Only a label React renders as text gets the tag's line (SelectTag.TextTemplate); the app's template draws its own type.
        var (select, scene, _) = Make(s =>
        {
            s.Multi = true;
            s.GetValueLabel = o => new Chip(Select.LabelText(o));
            s.DataTemplates.Add(new Avalonia.Controls.Templates.FuncDataTemplate<Chip>((c, _) => new Border { Width = 30, Height = 10, Tag = c.Name }));
        }, ["Aqua"]);
        using var _s = scene;
        var text = (ContentPresenter)select.Tags[0].Part(SelectTag.TextPart)!;
        var border = Assert.IsType<Border>(text.Child);
        Assert.Equal("Aqua", border.Tag);
    }

    private sealed record Chip(string Name);

    [AvaloniaFact]
    public void Overrides_set_before_it_is_shown_follow_their_collections_changes()
    {
        var overrides = new OverrideCollection();
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options, Overrides = overrides };
        var (scene, _) = SelectKit.Page(select);
        using var _s = scene;
        overrides.Add(new Override { Part = Select.RootPart, Style = new ControlTheme(typeof(Border)) { Setters = { new Setter(Layoutable.MarginProperty, new Thickness(7)) } } });
        scene.Pump();
        Assert.Equal(new Thickness(7), select.P<Control>(Select.RootPart).Margin);
    }

    [AvaloniaFact]
    public void Opened_out_of_the_tree_it_opens_when_shown_again_at_its_windows_height()
    {
        var options = SelectOracle.Options("OPTIONS");
        var select = new Select { Options = options };
        var host = new ContentControl { Content = select };
        var (scene, _) = SelectKit.Page(new Select());
        using var _s = scene;
        var page = (StackPanel)scene.Window.Content!;
        page.Children.Insert(0, host);
        scene.Pump();
        host.Content = null;
        scene.Pump();
        select.SetDropdownOpen(true);
        host.Content = select;
        PopoverTests.Settle(scene);
        Assert.True(select.DropdownMenu is { } m && m.IsAttachedToVisualTree());
        Assert.Equal(scene.Window.ClientSize.Height * 0.4, ((MenuList)select.DropdownMenu!.Part(Menu.ListPart)!).MaxHeight, 3);
    }

    [AvaloniaFact]
    public void A_new_template_while_it_is_open_shows_the_dropdown_in_its_new_popover()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        select.SetDropdownOpen(true);
        PopoverTests.Settle(scene);
        var before = select.DropdownMenu;
        select.Template = new Avalonia.Controls.Templates.FuncControlTemplate<Select>((_, _) => new Border());
        scene.Pump();
        select.Template = ((ControlTheme)scene.Window.FindResource(typeof(Select))!).Setters.OfType<Setter>().Single(s => s.Property == TemplatedControl.TemplateProperty).Value as Avalonia.Controls.Templates.IControlTemplate;
        PopoverTests.Settle(scene);
        Assert.NotSame(before, select.DropdownMenu);
        Assert.True(select.DropdownMenu is { } m && m.IsAttachedToVisualTree());
        Assert.Same(select.Part(Select.DropdownContainerPart), select.DropdownPopover!.Content);
    }

    [AvaloniaFact]
    public void A_click_on_the_focused_control_puts_the_caret_at_the_texts_end_the_text_kept()
    {
        var (select, scene, _) = Make(s => { s.OnCloseResetsInput = false; s.OnBlurResetsInput = false; });
        using var _s = scene;
        select.FocusTarget!.Focus(NavigationMethod.Tab);
        scene.Window.KeyTextInput("aq");
        scene.Pump();
        SelectKit.Key(scene, Key.Escape);
        var input = select.P<TextBox>(Select.InputPart);
        input.CaretIndex = 0;
        SelectKit.ClickAt(scene, SelectKit.ControlPoint(scene, select));
        Assert.Equal("aq", input.Text);
        Assert.Equal(2, input.CaretIndex);
        Assert.Equal("aq", select.InputValue);
    }

    [AvaloniaFact]
    public void Grouped_options_after_a_group_highlight_the_value_in_the_menus_order()
    {
        var black = new SelectOption("Black");
        var group = new SelectGroup { Label = "Blueish" };
        group.Items.AddRange([new SelectOption("AliceBlue"), new SelectOption("Aqua")]);
        var select = new Select { Options = new List<object> { group, black }, Value = [black] };
        var (scene, _) = SelectKit.Page(select);
        using var _s = scene;
        select.SetDropdownOpen(true);
        PopoverTests.Settle(scene);
        var menu = select.DropdownMenu!;
        Assert.Same(black, menu.FlatItems[menu.HighlightedIndex]);
    }

    [AvaloniaFact]
    public void Removing_tags_one_after_another_keeps_it_focused()
    {
        var (select, scene, _) = Make(s => s.Multi = true, ["Aqua", "Azure", "Bisque"]);
        using var _s = scene;
        // Focused, its dropdown opened by a key and closed by Escape: the page's listener is on.
        select.SetInputFocus();
        scene.Pump();
        SelectKit.Key(scene, Key.Down);
        SelectKit.Key(scene, Key.Escape);
        Assert.True(select.IsSelectFocused);
        foreach (var expected in new[] { new[] { "Azure", "Bisque" }, ["Bisque"] })
        {
            // A tag's action stops its click: the page's listener never hears it, though its tag is gone by the release.
            scene.Click((Control)select.Tags[0].Part(SelectTag.ActionPart)!);
            Assert.Equal(expected, SelectKit.Labels(select.Value));
            Assert.True(select.IsSelectFocused);
            Assert.True(select.FocusTarget!.IsFocused);
        }
    }

    [AvaloniaFact]
    public void A_taller_value_container_centres_a_line_of_tags_and_a_wrapping_label_keeps_its_line()
    {
        var tall = new ControlTheme(typeof(SelectValueContainer)) { Setters = { new Setter(Layoutable.MinHeightProperty, 120.0) } };
        var (select, scene, _) = Make(s =>
        {
            s.Multi = true;
            s.Overrides = [new Override { Part = Select.ValueContainerPart, Style = tall }];
        }, ["Aqua"]);
        using var _s = scene;
        var row = select.P<SelectValueContainer>(Select.ValueContainerPart);
        var tag = (Control)select.Tags[0].Part(SelectTag.RootPart)!;
        var middle = tag.TranslatePoint(new Point(0, tag.Bounds.Height / 2), row)!.Value.Y;
        Assert.Equal(row.Padding.Top + (row.Bounds.Height - row.Padding.Top - row.Padding.Bottom) / 2, middle, 3);

        var narrow = new Select
        {
            Options = SelectOracle.Options("OPTIONS"),
            Width = 140,
            HorizontalAlignment = HorizontalAlignment.Left,
            GetValueLabel = o => new TextBlock { Text = "A long label that wraps onto lines", TextWrapping = TextWrapping.Wrap },
        };
        narrow.Value = [narrow.Options!.Cast<object>().First()];
        ((StackPanel)scene.Window.Content!).Children.Insert(0, narrow);
        scene.Pump();
        var single = narrow.P<ContentPresenter>(Select.SingleValuePart);
        var box = narrow.P<SelectValueContainer>(Select.ValueContainerPart);
        Assert.True(single.Bounds.Top >= box.Padding.Top - 0.01, "the label's top " + single.Bounds.Top);
        Assert.True(single.Bounds.Bottom <= box.Bounds.Height - box.Padding.Bottom + 0.01, "the label's foot " + single.Bounds.Bottom);
    }

    [AvaloniaFact]
    public void Made_searchable_or_not_at_run_time_the_focusable_element_is_the_combo_box()
    {
        var (select, scene, _) = Make();
        using var _s = scene;
        var container = select.P<Control>(Select.InputContainerPart);
        var peer = ControlAutomationPeer.CreatePeerForElement(container);
        Assert.False(peer.IsControlElement());
        select.Searchable = false;
        scene.Pump();
        Assert.Same(container, select.FocusTarget);
        Assert.True(peer.IsControlElement());
        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        Assert.False(ControlAutomationPeer.CreatePeerForElement(select.P<Control>(Select.InputPart)).IsControlElement());
    }

    [AvaloniaFact]
    public void An_override_based_on_another_theme_keeps_what_it_is_based_on()
    {
        var based = new ControlTheme(typeof(Popover)) { Setters = { new Setter(Popover.ShowArrowProperty, true) } };
        var style = new ControlTheme(typeof(Popover)) { BasedOn = based, Setters = { new Setter(Popover.PlacementProperty, PopoverPlacement.Top) } };
        var (select, scene, _) = Make(s => s.Overrides = [new Override { Part = Select.PopoverPart, Style = style }]);
        using var _s = scene;
        Assert.True(select.DropdownPopover!.ShowArrow);
        Assert.Equal(PopoverPlacement.Top, select.DropdownPopover!.Placement);
    }

    // ---- UI Automation ----

    [AvaloniaFact]
    public void To_UI_Automation_it_is_a_combo_box_that_expands_and_its_tags_buttons()
    {
        var (select, scene, _) = Make(s => s.Multi = true, ["Aqua"]);
        using var _s = scene;
        AutomationProperties.SetName(select, "Colours");
        scene.Pump();
        var peer = ControlAutomationPeer.CreatePeerForElement((Control)select.FocusTarget!);
        Assert.Equal(AutomationControlType.ComboBox, peer.GetAutomationControlType());
        Assert.Equal("Selected Aqua. Colours", peer.GetName());
        var expand = (IExpandCollapseProvider)peer;
        expand.Expand();
        scene.Pump();
        Assert.True(select.IsOpen);
        Assert.Equal(ExpandCollapseState.Expanded, expand.ExpandCollapseState);
        expand.Collapse();
        scene.Pump();
        Assert.False(select.IsOpen);
        var tag = ControlAutomationPeer.CreatePeerForElement(select.Tags[0]);
        Assert.Equal("Remove Aqua", tag.GetName());
        ((IInvokeProvider)tag).Invoke();
        scene.Pump();
        Assert.Empty(select.Value!);
        var clear = ControlAutomationPeer.CreatePeerForElement(select.P<Control>(Select.ClearIconPart));
        Assert.Equal("Clear all", clear.GetName());
    }

    // ---- Motion ----

    [AvaloniaFact]
    public void The_control_transitions_its_border_and_fill_once_loaded_and_the_ring_turns()
    {
        var (select, scene, _) = Make(s => s.IsLoading = true);
        using var _s = scene;
        var control = select.P<Border>(Select.ControlContainerPart);
        Assert.Contains(":loaded", control.Classes);
        var t = SelectOracle.Data.Transition;
        Assert.Equal("border, box-shadow, background-color", t.Property);
        var transitions = control.Transitions!.OfType<BrushTransition>().ToList();
        Assert.Equal([Border.BorderBrushProperty, Border.BackgroundProperty], transitions.Select(x => x.Property));
        Assert.All(transitions, x => Assert.Equal(TimeSpan.FromSeconds(t.Seconds), x.Duration));
        var ring = select.P<SpinnerRing>(Select.LoadingIndicatorPart);
        var turn = (RotateTransform)ring.RenderTransform!;
        PopoverTests.Settle(scene, 300);
        Assert.NotEqual(0, turn.Angle);
        ((IPseudoClasses)ring.Classes).Add(":metered-still");
        PopoverTests.Settle(scene, 50);
        Assert.Equal(0, turn.Angle);
    }
}
