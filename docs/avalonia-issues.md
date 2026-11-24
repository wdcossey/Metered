# Avalonia's own issues

What Metered found to be Avalonia's, not its own: each with how it was measured, what Metered does about it, and when to
look again. Avalonia 12.1.4 throughout; the source read is its decompiled assemblies (no `v12.1.4` sources are vendored).

## 1. A text box keeps its selection and touch handles after the focus leaves it while a menu is open (#109)

**Seen** on the Pixel 8 (Android 17 (API 37), Release, 2026-11-21): a long press on a word in one input selects it with
Avalonia's two handles; a long press on another input moves the focus there, and the first keeps its selection highlight
and its handles, unfocused, until it is touched again.

**Why.** `TextBox.OnLostFocus` clears the selection (`ClearSelectionOnLostFocus`), removes the touch handles
(`TextPresenter.RemoveTextSelectionCanvas`) and hides a revealed password only when neither its `ContextFlyout` nor its
`ContextMenu` is open, so that the focus going into the cut, copy and paste menu keeps the selection the menu is for.
Nothing does it later, when the menu closes. Two ways that leaves them on a text box that isn't focused:

- **The menu is shared.** Avalonia's Simple theme gives every text box the same `MenuFlyout` (measured: B's menu is open
  while A's is), and so did Metered's `Input.ContextFlyout`, a `StaticResource` in a theme setter. A long press on text box B opens that one menu for B
  as the hold fires (`TextBox.OnHolding`, then `TextSelectionHandleCanvas.ShowFlyout`), before the finger lifts, and a
  finger's focus moves on its release (`FocusManager`), so A loses the focus while "its" menu is open.
- **Its own menu left open** while the focus goes elsewhere without a press (the light-dismiss layer takes a press, and
  closes the menu first): a key, the soft keyboard's next, code.

**Measured headless** (Avalonia.Headless, the test clock's hold, 2026-11-24): two plain Avalonia `TextBox`es under the
Simple theme alone, and two Metered `Input`s, behave the same. A hold on A's "world" selects it, with both handles and the
menu; a tap on nothing closes the menu; a hold on B selects B's word and focuses B, and A is left unfocused with 6 to 11
selected and its handles shown, still after the menu closes, 6 s later and after a tap on B. With A's menu open,
`B.Focus()` leaves them on A the same way. A tap on B straight after the hold on A doesn't reach B at all: the
light-dismiss layer takes it and closes the menu.

**Metered's work round** (`BaseInput`, `Input.axaml`): each input and textarea has its own menu (`x:Shared="False"`,
given by `DynamicResource`), and an input whose own menu is open when the focus goes to an element outside it and its
menu closes the menu first, so Avalonia's own cleanup runs. Focus into the menu (a mouse's right click, a menu item)
keeps the selection, as Avalonia's does. Held by `InputTests` (`Each_input_has_a_menu_of_its_own`,
`A_long_press_on_another_input_leaves_the_first_with_no_selection_or_handles`,
`Its_menu_left_open_closes_with_its_selection_when_the_focus_moves_on`,
`The_focus_going_into_its_menu_keeps_the_selection`) and `TextareaTests`
(`A_long_press_on_another_textarea_leaves_the_first_with_no_selection_or_handles`, `Its_menu_is_Inputs`).

**Not covered:** an app that gives several inputs one menu of its own (a shared `ContextFlyout`), or a `ContextMenu`,
still meets Avalonia's behaviour; so does a focus change that names no element. Not yet tried on the phone.

**Look again** when Avalonia is upgraded: if `TextBox.OnLostFocus` checks that the focus went into its menu (or the
themes stop sharing it), the work round can go. Not reported upstream yet; the nearest open issues are
[#12878](https://github.com/AvaloniaUI/Avalonia/issues/12878) and [#16132](https://github.com/AvaloniaUI/Avalonia/issues/16132)
(touch selection on mobile in general).
