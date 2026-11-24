# Input

Base Web's `Input` and its `BaseInput` (baseui 18.2.0, `input/`), as `Metered.Controls.Input`, an Avalonia `TextBox`,
on `Metered.Controls.BaseInput`, the base Textarea (#85) shares ([`docs/controls/Textarea`](../Textarea/README.md)).

<p>
<img src="../../preview/light__input-1.png" width="270" alt="Input, light">
<img src="../../preview/dark__input-1.png" width="270" alt="Input, dark">
</p>

The sample's page, drawn headless by `PreviewGallery` in Instrument Sans, the substitute for Uber Move Text
([ADR 0003](../../decisions/0003-font.md)). The scenarios, from `InputFrames`' gallery (headless): `input-states`
(default, focused by keyboard, positive, error, disabled, read only, with an end enhancer), a placeholder,
`input-sizes`, the documentation's enhancers, `input-clearable`, `input-password` at the four sizes and once shown, and
`input.scenario` (both enhancers, compact; its value reads "example" where the scenario's reads "uber", as the sample page's filled example reads "value": Metered carries no Uber mark):

<p>
<img src="frames/input-gallery-light.png" width="390" alt="Input's scenarios, light">
<img src="frames/input-gallery-dark.png" width="390" alt="Input's scenarios, dark">
</p>

## Which input

Read from `node_modules/baseui/input/` (`input.js`, `base-input.js`, `styled-components.js`, `utils.js`, `constants.js`,
`stateful-container.js`, `types.d.ts`), the icons it renders (`icon/delete-alt.js`, `icon/show.js`, `icon/hide.js`,
`icon/icon.js`, `icon/styled-components.js`), its scenarios and tests (`src/input/__tests__/`, `input.e2e.ts` among
them), and its documentation page and examples (`documentation-site/pages/components/input.mdx`,
`documentation-site/examples/input/`) at commit `3adb0478c96569dc4e593ab39c24f36dc59e39f5`, the 18.2.0 publish. There is
no `input-v2`, the page has no deprecation notice, and no prop is marked deprecated.

Base Web's `<input>` is the element that takes focus, keys and text, so the control is a `TextBox`: Avalonia's caret,
selection, clipboard, undo, IME and soft keyboard, and its touch selection handles, are the input's. Its template is
Base Web's tree of parts round the text box's text presenter.

## API

| Base Web | Metered |
|---|---|
| `value`, `onChange` | `Text`, `TextChanged` (Avalonia's) |
| `placeholder` | `PlaceholderText` (Avalonia's) |
| `disabled`, `readOnly`, `maxLength` | `IsEnabled`, `IsReadOnly`, `MaxLength` (Avalonia's: `MaxLength` limits what is typed or pasted, as HTML's) |
| `size` (`mini`, `compact`, `default`, `large`) | `Size`, an `InputSize`; `Default` by default |
| `startEnhancer`, `endEnhancer` (a node, or a function of the shared props) | `StartEnhancer`, `EndEnhancer`: content, shown when it is something (not null, `""` or `false`; `0` is one, as `isEnhancer` reads it) |
| `adjoined` | `Adjoined`, read-only: input.js overwrites the prop with what the enhancers give (`getAdjoinedProp`) |
| `error`, `positive` | `Error`, `Positive`; both draw error, as Base Web does (and warns) |
| `clearable`, `clearOnEscape` | `Clearable`, `ClearOnEscape` (true by default) |
| `type="password"` | `Type = InputType.Password`; `IsMasked`, read-only, base-input.js's `isMasked` |
| `overrides` (`Root`, `StartEnhancer`, `EndEnhancer`, `InputContainer`, `Input`, `Before`, `After`, `ClearIconContainer`, `ClearIcon`, `MaskToggleButton`, `MaskToggleShowIcon`, `MaskToggleHideIcon`) | `Overrides`, the same twelve parts |
| `overrides.Before`, `overrides.After` (nothing unless overridden) | the text box's `InnerLeftContent`, `InnerRightContent`; or an override's `Content` |
| `onFocus`, `onBlur`, `autoFocus`, `inputRef` | `GotFocus`, `LostFocus`, `Focus()`, the control itself |
| `autoComplete`, `name`, `id`, `pattern`, `required`, `role`, `inputMode`, `min`, `max`, `step`, other `type`s | not mapped: a form's and a browser's; the soft keyboard's kind is Avalonia's `TextInputOptions` on the input |
| `aria-*` | `AutomationProperties` on the input; `aria-invalid` isn't mapped |

`BaseInput` is abstract: Base Web exports its `BaseInput` component, which renders the container and what is inside it
without a root or enhancers; here it is the base of `Input` and, next, `Textarea`, not a control of its own.

The states, set on the input and mirrored onto every part (`Parts`), are the shared props (utils.js's `getSharedProps`):
`:size-mini`, `:size-compact`, `:size-default`, `:size-large`, `:is-focused` (the text box has focus, by any means),
`:error`, `:positive`, `:is-read-only`, `:adjoined-none`, `-left`, `-right`, `-both`, `:has-icon-trailing` (clearable or a
password: input.js's `$hasIconTrailing`), and `:loaded`; `:disabled` is Avalonia's. The clear icon and the toggle take
focus themselves, and their own `:focus-visible` (Avalonia's, on a keyboard's focus) is base-input.js's
`isFocusVisibleForClear` and `isFocusVisibleForMaskToggle`.

## Behaviour, as Base Web's

From its style functions (`styled-components.js`):

- The root: a 2 px border, `inputBorderRadius` (8; `inputBorderRadiusMini`, 4, at mini), fill and border by state:
  disabled (`inputFillDisabled`), then focused (`borderSelected` round `inputFillActive`: the focus ring), then error
  (`inputBorderError`, `inputFillError`), then positive, else `inputBorder` and `inputFill`. 14 px of padding on a side
  with an enhancer, and at the end when there is a trailing icon. The font by size: `font100`, `font200`, `font300`,
  `font400`. Its border's colour moves over `timing200` on `easeOutCurve`.
- The enhancers: padded by size (10, 10, 8, 6 a side), centred, their text `contentPrimary` (`inputEnhancerTextDisabled`
  when disabled), their fill the root's; colour and fill move over `timing200`.
- The container: the rest of the row, its fill by state; the text: padded by size (4, 6, 10, 14 above and below; 14 a
  side), its colour `contentPrimary` (`inputTextDisabled` when disabled), the caret `contentPrimary`, the placeholder
  `inputPlaceholder` (`inputPlaceholderDisabled`), the cursor a text cursor (`not-allowed` when disabled). An input's
  height is its padding and its line: 32, 36, 48 and 60 at the four sizes.
- The clear icon (`delete-alt`): 14, 14, 16, 22 px by size, in a container padded 6, 6, 4, 2 a side, centred in the
  height, `contentPrimary`; a pointer cursor; a 3 px `borderAccent` outline on keyboard focus.
- The mask toggle: a borderless button padded 10, 10, 8, 6 a side, its icon (`show` while masked, `hide` after) 12, 16,
  20, 24 px, `contentPrimary`; the same outline on keyboard focus.

From base-input.js and input.js, and its e2e test:

- The clear icon shows only for a clearable input with a value that is neither disabled nor read-only. A click on it (or
  a finger's tap, or Enter or Space with it focused) clears the value, as an edit (one `TextChanged`, undoable), and
  focuses the input. Escape in a clearable input that isn't read-only clears it too, unless `ClearOnEscape` is false,
  and goes no further (`stopPropagation`); in an input that isn't clearable it does nothing of Base Web's.
- A password is masked until the toggle is clicked; the toggle's label and title are "Show password text", then "Hide
  password text"; it is a button, so it takes the focus, and Enter on it toggles; Enter in the input doesn't. The mask is
  a browser's: Metered draws U+2022, as Chrome does, and tells the soft keyboard it is a password while masked
  (`TextInputOptions.ContentType`), as a browser does for `type="password"`. Copying is off while masked (Avalonia's
  text box, as a browser's).
- The tab order is the input, the clear icon, the toggle: their order in Base Web's markup.
- **A finger's (or a pen's) drag on the input scrolls**, focused or not, as a phone's browser does with an `<input>`
  (Chrome on the Pixel 8, 2026-11-23): a sideways drag on a value longer than the box scrolls its text (a scroll gesture
  on the text's scroller, sideways only, flung as a browser's: #117), and a drag up or down, or one the text can't take
  (it is at its end that way, or fits), scrolls the page round it. Which it is, the drag's way decides once it has gone
  the gesture's start distance: more sideways than up or down, and a way the text can move, is the text's, even in a
  quick flick whose first move is already past that distance sideways; otherwise the whole drag is the page's, even if it
  turns back the way the text could go, as a browser picks a touch's scroller as it starts (`InputScrollGestureRecognizer`;
  Avalonia's own would start on any sideways part, and the text's scroller sees each move before the page's). A fling
  that reaches the text's end stops there. A drag that has moved the text stays the text's until
  the finger lifts, even past its end: a browser's scroll latching. A drag is no tap: it focuses nothing, and the caret
  moves by a tap or its handle, never by a drag, nor where a drag lifts, nor where a finger lifts after another has
  pressed (Avalonia's text box would move it by a drag and keep the page from panning, #94, and its focus manager focuses
  a finger's text box where it lifts, wherever it moved: the release is kept from the focus manager alone, and goes on
  to what is round the input as before; a tap is Avalonia's, a release within the platform's tap size of the press). After a hold, a drag extends the hold's selection, as the text box's. A mouse's drag selects, and a
  press anywhere on the box focuses it (known differences, 1). `BaseInput` does this, so Textarea's text has it too.
- The selection and its touch handles go when the focus leaves for another control, as in a phone's browser, also when a
  long press there has opened that one's menu (#109). Avalonia's text box keeps them while its menu is open, for the
  focus that goes into the menu, and nothing takes them away later ([Avalonia's issues](../../avalonia-issues.md), 1); the
  one menu every input shares (`Input.ContextFlyout`) is open for the other input by then. So only the focus going into
  its own menu keeps them: a menu open for another input is left to that one, and its own, open as the focus goes
  anywhere else (its clear icon too, or nowhere), closes first.
- On a phone (a coarse pointer, `:pointer-coarse`, as the v2 controls read Base Web's `@media (pointer: coarse)`: Android
  and iOS) the cut, copy and paste menu opens without its shortcuts (the theme's `Input.ContextFlyout`, its items'
  `InputGesture` held empty while it is open there, without taking the focus, so the keyboard stays), and above the
  selection, clear of it, as a phone's browser places Android's floating text toolbar (#110; known
  differences, 12). The place is Android's (`aosp-mirror/platform_frameworks_base` at `1cdfff555f`,
  `LocalFloatingToolbarPopup.refreshCoordinatesAndOverflowDirection` with no overflow): 8 above the selection
  (`floating_toolbar_vertical_margin`, 8 dp), or with no room there 8 below its touch handles, or just below them, or
  as high as it fits; centred on the selection and kept 16 from the sides (`floating_toolbar_horizontal_margin`). The
  selection is Chrome's content rect (`SelectionPopupControllerImpl`: its bounds, their bottom extended by the
  handles' height), the part of it the text's box shows, as Blink clips an input's selection and `FloatingActionMode`
  keeps the rect inside its view; with nothing selected, the caret's line. The room is the window's safe area above the soft keyboard (Android's
  visible display frame). While it is open it is placed again as the input moves (a page's scroll, the keyboard making
  room), as its selection or caret moves, as the handles' height changes once they are laid out, and as the keyboard or
  the bars change, as Android's toolbar follows its content. A selection the box doesn't
  show at all places it by the box. An app's own `ContextFlyout` (local or by a style) replaces the menu, keeps its
  items as they are, and is placed the same; one whose opening the app cancels stays shut. A fine pointer keeps the
  desktop's menu, its shortcuts and Avalonia's place. `BaseInput` does this (`TouchMenu`), so Textarea's text has
  it too.
- The icons are Base Web's own paths, rendered from its icons by the generator and the oracle, each drawn in its
  `viewBox` (24 × 24) at the size base-input.js gives it, filled with the colour its container passes down
  (`currentColor`). Metered has no icon set: these three are all Input needs.

## Held by

- `Controls/InputColourTests`: 30 cases: 11 states (rest, focused by a real Tab, error, positive, both, focused in error
  and in positive, disabled, disabled in error, read-only, read-only and focused), light and dark, every part's colours,
  the caret's, the placeholder's and the cursor; and the clear icon's and the toggle's outline with and without keyboard
  focus (8).
- `Controls/InputLayoutTests`: 48: the four sizes × no enhancer, a start one, an end one, both × no trailing icon, the
  clear icon, the toggle: adjoined, the trailing flag, every length and font, the icons' sizes, the height, and where
  each part is.
- `Controls/InputTests`: 112: Base Web's defaults, and `IsMasked` by type and toggle; the parts it renders and their
  labels in 48 cases of clearable, value, disabled, read-only, type and mask; the clear icon by click, touch (and a
  second finger), a press dragged off, Enter and Space (its text not typed, and not eaten later); keys, text, focus, a
  press and a menu on the template's own controls kept from the text box, and the text's selection kept; a long press on
  another input leaving the first with no selection or handles, the shared menu open for the other (the theme's, and an
  app's), its own menu left open closing with its selection when the focus moves on (by Tab, by code, to its clear
  icon, to another popup of its content, to nowhere), the focus going into the menu keeping it, and its own menu after
  another's still opening on its text alone (#109);
  Escape's rules (and that it stops there); the toggle, Enter in the input and on the toggle, hiding it and relabelling
  it and the clear icon by an override; `MaxLength`; typing and `:is-focused` on every part; the tab order; a touch's
  focus; a finger's drag that moves the page and not the caret, focused or not; a sideways one on a long value that
  scrolls its text, focused or not, focusing nothing, the caret left, and back again, the caret's blinking not bringing it
  back; a mostly vertical one on it that is still the page's, in fifteen moves or three; a mostly sideways one in three
  that is the text's; one latched past the text's end as it turns up the page, and a latched scroll at the end that asks
  its fling to stop, the next gesture the page's; `ScrollViewer.IsScrollChainingEnabled` reaching the text's scroller; one
  that sets off towards the text's start and turns back, not the text's; one that nothing scrolls (focusing nothing, its
  release still seen unhandled round the input, the caret left, and a tap after it that focuses and places it), a finger lifted
  after another pressed, and a mouse's that selects; enhancers (JavaScript's truth, NaN
  and 0) and adjoined; Before and After; overrides on six parts; the icons' paths, viewBox and fill rule; transitions after load; UI Automation (the clear icon a button that invokes only where a
  click could, the toggle a button, the input an edit).
- `Controls/InputTouchMenuTests`: 35: the place against Android's in each of its cases (8, worked by hand from its
  source); a coarse pointer by platform and `:pointer-coarse`; on a phone the theme's menu without shortcuts while open
  and with them again after, above the word a long press selects and centred on it, below the handles near the top, 16
  from the side, below the handles under the Pixel's status bar, as high as it fits above a keyboard, placed again as the
  page scrolls, as the keyboard opens, as the selection moves and as the handles' height changes, above the caret of an empty input, the focus left on the input, a long selection by the
  part the box shows and one it doesn't show by the box, an app's own menu (local, or by a style, its items untouched)
  placed the same, a cancelled opening left shut, a placement rect on its popup, the menu's own place left as it was,
  #109's work round on a phone, a Textarea's; a fine pointer's shortcuts and place; the word drawn selected under the
  open menu; and, when `METERED_FRAMES` names a folder, the menu above, below and a desktop's (3).
- `Controls/InputFrames`: the drawn border and fill and the focus ring, the clear icon and its outline, the toggle's ring, the show
  and hide icons, and the gallery above: 6.
- `Sample/InputPageTests`: the sample's page, 4, a finger's tap in the page's ScrollViewer and typing, and a finger's drag
  on an input scrolling the page, among them.

The part themes are generated (`tools/tokens/input.js`): the input's style functions evaluated on Base Web's theme with
each colour replaced by its own name, for every combination of size, adjoined, the trailing flag, focus, disabled,
error, positive and keyboard focus, written by `radio.js`'s theme writer as the fewest layered styles and checked
against Base Web's for each combination. The icons' sizes come from Base Web's Input rendered, and their paths from its
icons rendered. The oracle (`tools/reference/input.js`) renders Base Web's own Input (react-dom/server) with every part
overridden by a recorder, on the real light and dark themes, and evaluates each part's style function with the props
Base Web gave it, so which parts render, their labels and sizes are Base Web's.

## For Textarea (#85)

Base Web's Textarea renders the same `BaseInput` with `StyledTextAreaRoot`, `StyledTextareaContainer` and
`StyledTextarea`, which call Input's `getRootStyles`, `getInputContainerStyles` and `getInputStyles`. What it takes:

- `BaseInput` (C#): derive `Textarea` from it with `base("Textarea", parts)`, its own `Root` and no enhancers or toggle;
  the clear icon, Escape, the states, the parts' guards and the overrides come as they are (`IsPassword` stays false).
- `InputField`, `InputIcon`: the text's element and the clear icon, as they are; the template's `PART_ScrollViewer`
  with `AcceptsReturn` and wrapping, and the clear icon's container aligned to the top (`$alignTop`).
- `tools/tokens/input.js`: `parts({ Root, InputContainer, Input })` makes the part definitions for other styled
  components, and `themes('Textarea', list)` writes them keyed `Textarea.Styled<Part>`; `AXES` is the states. The
  textarea's own differences (`resize`, `$alignTop`, no `$size` on its root) are its generator's.
- `Input.ContextFlyout` (Input.axaml), the cut, copy and paste menu, by its key; `BaseInput` opens it on a phone
  without its shortcuts and above the selection.

## Known differences

1. **A press anywhere on the input focuses it.** Base Web's enhancers, the root's padding and the clear icon's container
   are `div`s outside its `<input>`, which a press doesn't focus; here they are inside the text box's template, and
   Avalonia focuses the text box and moves its caret to the nearest end, as for its own padding. On a phone that makes
   the whole box a target. A control in an enhancer (a button) keeps its own press. A deviation that comes with the text
   box; for the developer to keep (`A_press_on_an_enhancer_focuses_the_input_as_a_text_boxs_own_box_does`).
2. **The selection's colour is Metered's.** Base Web leaves it to the browser; here it is `colors.accent` with
   `colors.contentOnColor` text, by token.
3. **The mask toggle is disabled with the input.** Base Web's toggle has no `disabled`, so a disabled password can still
   be shown; Avalonia disables everything inside a disabled control. Its look is the same.
4. **The cut, copy and paste menu is the app theme's text.** A browser's menu; here `Input.ContextFlyout` with the
   strings Avalonia's Simple and Fluent themes define (`StringTextFlyoutCutText` and the others).
5. **`clearOnEscape` follows `Clearable` now.** base-input.js adds its Escape listener only if the input is clearable when
   it mounts, so turning `clearable` on later doesn't enable Escape; here Escape follows the current value. As Base
   Web's, Escape in a clearable input goes no further even with nothing to clear (`stopPropagation`): in a dialog or a
   flyout that closes on Escape, the input takes it first, as Base Web's input keeps it from a modal.
6. **Before and After** are the text box's inner content, not a component override: an override can't render another
   element (ADR 0001), only restyle or hide one. The documentation's `MaskToggleButton: () => null` is an override whose
   `Props` set `IsVisible` false; its `MaskToggleShowIcon: () => "🐵"` has no equivalent.
7. **Transitions start after load**, as on every Metered control: CSS doesn't transition a first paint.
8. **`IsMasked` follows the type until the toggle is clicked.** base-input.js sets `isMasked` once, from the type the
   input is made with, so an input made as text and turned into a password later is shown unmasked. Avalonia sets
   properties after a control is made, so here it is whether the type is a password until the toggle is clicked, and the
   toggle's after: the same for an input made a password, as every example and scenario does.
9. **The text box's own controls.** The parts that take focus are inside the text box's template, so Metered keeps from
   the text box what Avalonia would give it from them: keys, text, focus, a press, a menu asked for. A finger's tap and
   a held finger on them still reach it: neither can be seen headless, and the Pixel run (#90) didn't check them: whether a tap there leaves a caret handle, and
   whether a long press on an icon selects a word, are still open. A Space that clears from the clear icon
   doesn't type a space into the input it focuses (Base Web's `preventDefault`).
10. **On a phone, tried on a Pixel 8** (Android 17 (API 37), Gboard, Release, 2026-11-21, #90). A tap focuses the input and
   opens the keyboard. A finger's drag from an input scrolls the page and focuses nothing (#94). A long press selects a
   word with Avalonia's handles and opens the menu, and dragging a handle extends the selection without moving the page.
   Copy and paste work. The clear icon clears and keeps the focus. A password asks for a password keyboard (no
   personalised learning), and the toggle takes the focus and closes the keyboard, as in Chrome on Android. A tap on an enhancer
   focuses the input. Problems found there, each its own issue:
   - the keyboard covers a focused input near the page's foot, and the page can't scroll it into view (#108; the sample's
     shell now keeps its page clear with [`KeyboardInset`](../KeyboardInset/README.md), not yet run on the Pixel);
   - selection handles stayed on an input after the focus left it (#109): Avalonia's ([its issues](../../avalonia-issues.md),
     1), worked round since (Behaviour), headless only, not yet tried on the phone;
   - the menu shows desktop shortcuts and covers the selection (#110): on a phone it has none since, and opens above the
     selection (Behaviour; known differences, 12), headless only, not yet tried on the phone (#148).

   Holding isn't raised headless, so the drag after a hold is tested only on the phone.
11. **A long value's sideways scroll is checked headless only.** Since #117 (decided 2026-11-24) a finger's sideways
   drag scrolls a value longer than the box, as Chrome on the Pixel 8 scrolls an `<input>`'s (2026-11-23): focused or not,
   focusing nothing, a vertical drag still the page's; so this is no longer a difference from Base Web in a browser. Not
   checked: its fling (the gesture's inertia runs on a stopwatch, not the tests' clock); a pen (Avalonia.Headless has
   none), whose hover after a tap is kept from scrolling the text, and whose release on a control inside the input (a
   textarea's grip) is that control's; and the phone itself (#140).
12. **A phone's menu is Avalonia's menu, placed as Android's toolbar** (#110, option 1 of the 2026-11-21 late decisions
   page; Android's place as built). A phone's browser shows the platform's floating text toolbar: a row (Android's: Cut, Copy, Paste,
   Select all) above the selection. Here it is the same cut, copy and paste menu as on a desktop, a column, without its
   shortcuts while it is open there, placed where Android places its toolbar (Behaviour). Avalonia's own place for a touch's menu hangs it over
   the selection ([Avalonia's issues](../../avalonia-issues.md), 2), and a fine pointer keeps it: a Windows touch screen
   still gets the desktop's menu, its shortcuts and that place. The menu's items are no taller for a finger, and it has no
   Select all. Not tried on the phone (#148). The alternatives on the decisions page: a row-shaped toolbar of Metered's own, or
   Avalonia's menu left as it was.

