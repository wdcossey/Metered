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
  while A's is), and so does Metered's `Input.ContextFlyout`, a `StaticResource` in a theme setter. A long press on text
  box B opens that one menu for B as the hold fires (`TextBox.OnHolding`, then `TextSelectionHandleCanvas.ShowFlyout`), before the finger lifts,
  and a finger's focus moves on its release (`FocusManager`), so A loses the focus while "its" menu is open.
- **Its own menu left open** while the focus goes elsewhere without a press (the light-dismiss layer takes a press, and
  closes the menu first): a key, the soft keyboard's next, code.

**Measured headless** (Avalonia.Headless, the test clock's hold, 2026-11-24): two plain Avalonia `TextBox`es under the
Simple theme alone, and two Metered `Input`s, behave the same. A hold on A's "world" selects it, with both handles and the
menu; a tap on nothing closes the menu; a hold on B selects B's word and focuses B, and A is left unfocused with 6 to 11
selected and its handles shown, still after the menu closes, 6 s later and after a tap on B. With A's menu open,
`B.Focus()` leaves them on A the same way. A tap on B straight after the hold on A doesn't reach B at all: the
light-dismiss layer takes it and closes the menu.

**Metered's work round** (`BaseInput.OnLostFocus`): only the focus going into the input's own menu keeps them. A menu
open for another input (the shared one another's hold has just opened, the theme's or an app's) is hidden from the text
box's check while it loses the focus, so Avalonia lets them go as with no menu, and the menu stays the other's. Its own
menu, open as the focus goes anywhere but into it (another control, its own clear icon, where Avalonia lets them go with
the menu closed, another popup of its content, or nowhere), closes first, so Avalonia's own cleanup runs. Focus into the
menu (a mouse's right click focuses it) keeps the selection, as Avalonia's does; so does a tap on Copy, which leaves the
focus on the input, and the menu a right click focused gives the focus back to the input when it closes (measured). Held
by `InputTests` (`A_long_press_on_another_input_leaves_the_first_with_no_selection_or_handles`,
`An_apps_one_menu_for_two_inputs_stays_open_for_the_one_that_opened_it`,
`Its_menu_left_open_closes_with_its_selection_when_the_focus_moves_on` (by Tab and by code),
`Its_menu_closes_with_its_selection_when_the_focus_goes_to_its_clear_icon`,
`Its_menu_closes_with_its_selection_when_the_focus_goes_nowhere`,
`The_focus_going_into_another_popup_of_its_content_closes_its_menu`, `The_focus_going_into_its_menu_keeps_the_selection`,
`After_another_inputs_menu_its_own_opens_on_its_text_alone`)
and `TextareaTests` (`A_long_press_on_another_textarea_leaves_the_first_with_no_selection_or_handles`).

**Not covered:** a `ContextMenu` (Metered gives none), and a flyout that isn't a `PopupFlyoutBase`, whose inside can't
be told from elsewhere, still meet Avalonia's behaviour. Not yet tried on the phone.

**Look again** when Avalonia is upgraded: if `TextBox.OnLostFocus` checks that the open menu is its own and that the
focus went into it, the work round can go. Not reported upstream yet; the nearest open issues are
[#12878](https://github.com/AvaloniaUI/Avalonia/issues/12878) and [#16132](https://github.com/AvaloniaUI/Avalonia/issues/16132)
(touch selection on mobile in general).

## 2. A touch's cut, copy and paste menu opens over the selection (#110)

**Seen** on the Pixel 8 (Android 17 (API 37), Release, 2026-11-21): a long press on a word selects it and opens the menu
over the word, hiding what was just selected.

**Why.** `TextSelectionHandleCanvas.ShowFlyout`, which opens a text box's `ContextFlyout` after a hold selects a word or a
handle is let go, places it with `PlacementMode.Custom`: its anchor's top left at the handles' midpoint, the line's
height and 16 above their top (`LineHeight`, or `FontSize` when it is unset, plus 16), and no gravity, so the popup is
centred on that point and hangs down over the line. A hold on an empty text, or on its selection, asks for the menu at the
pointer, under the finger.

**Measured headless** (Avalonia.Headless, the test clock's hold, 2026-11-24, an `Input` 100 from the window's top under the
Simple theme and Metered's, a fine pointer): a hold on "world" selects it at y 112 to 136, and the menu is drawn at y 71 to
145, over it (`InputTouchMenuTests.Gallery`'s `desktop` frame).

**Metered's work round** (`BaseInput`, `TouchMenu`): on a coarse pointer (a phone) the input opens the menu itself, before
Avalonia's flyout handler, with a place of its own: Android's floating toolbar's, above the selection (Input's README,
known differences 12). Held by `InputTouchMenuTests`. A fine pointer keeps Avalonia's place, so a Windows touch screen's
long press still covers the word.

**Look again** when Avalonia is upgraded: if `ShowFlyout` gives its place a gravity that puts the menu above the
selection, the fine pointer's touch could use it. Not reported upstream yet.
