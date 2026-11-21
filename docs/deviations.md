# Deviations from Base Web

Every place where Metered does something other than what Base Web 18.2.0 does, in one list, so any of them can be found
and revisited. Each row is short. The detail, the measurement and the test are in the item it links to, in the
control's `README.md` (its **Known differences**, item *n*, or the section named) or in an ADR.

There are three kinds:

- **Chosen:** Avalonia doesn't force it. Metered does it for fit with Avalonia or for usability, and it's the
  developer's call (PROCESS §2). **Decided** gives the date and where. "As built" means the PR documented it and
  merged, but it hasn't been put to the developer on its own yet.
- **Forced:** Avalonia, the platform, the licence or a rule of this repository (CLAUDE.md) leaves no faithful way, or
  the faithful way would be a brittle workaround.
- **Not yet:** not done, or not tried on a device.

**A PR that adds, changes or removes a deviation changes its row here too**, and the review checks the row (PROCESS §3, item 6).
A deviation Avalonia doesn't force goes in the PR's **For you to decide**, and its row says "as built" until the
developer has answered.

## Chosen

These are the ones to revisit first.

| Control | Base Web | Metered | Decided | Detail |
|---|---|---|---|---|
| Checkbox, Radio | A finger held on the control never shows the box's (Radio: the inner disc's) pressed tint: a phone's browser sends `mousedown` only after the finger lifts | A held finger shows it and the backplate's, as on a Button | 2026-11-18, #49 | [Checkbox 2](controls/Checkbox/README.md#known-differences), [Radio 3](controls/Radio/README.md#known-differences) |
| Switch | No pressed tint at all | The pressed tint Radio and Checkbox have, from Base Web's own `getOverlayColor` | 2026-11-20, #68 | [Switch 12](controls/Switch/README.md#known-differences) |
| Checkbox, Radio, Switch | `$isHovered` from `mouseenter`, which a phone's browser fires on a tap and keeps | A finger never hovers; a press is any pointer's | ADR 0001 | [ADR 0001](decisions/0001-parts-states-and-overrides.md); [Checkbox 2](controls/Checkbox/README.md#known-differences), [Radio 3](controls/Radio/README.md#known-differences), [Switch 2](controls/Switch/README.md#known-differences) |
| Checkbox, Switch | Toggles on any Enter released on it, including one pressed elsewhere, and lets the key go on to the page | Only an Enter pressed on it toggles it, and the control takes the key, so a default button doesn't fire too | 2026-11-18, #49 | [Checkbox 5](controls/Checkbox/README.md#known-differences), [Switch 11](controls/Switch/README.md#known-differences) |
| ButtonGroup | Controlled and stateful groups are separate | One group: a click sets `Selected` whatever bound it | As built | [ButtonGroup 1](controls/ButtonGroup/README.md#known-differences) |
| ButtonGroup | `cloneElement` leaves a button's own props alone | The group holds its values over the button's own at `Animation` priority. A `TwoWay` binding writes the group's value back to its source, and a held value changed by `SetCurrentValue` is put back at the group's next update | 2026-11-21, #70 (PR #78); the shared `Hold`, 2026-11-21 evening (PR #92) | [ButtonGroup 3 to 5](controls/ButtonGroup/README.md#known-differences), [ButtonGroup's behaviour](controls/ButtonGroup/README.md) |
| ButtonGroup | `child.props.role || isRadio ? 'radio' : …` gives any child with a role of its own the role "radio", in a plain group too | A button's own role is kept | 2026-11-21, #70 (PR #78) | [ButtonGroup 2](controls/ButtonGroup/README.md#known-differences) |
| RadioGroup | The group context never changes a radio's own `checked`; grouped radios share the group's `name` | Held as ButtonGroup's. A `TwoWay` `IsChecked` follows the group, and a held radio's `GroupName` is its own, `metered-radio-group-<n>`, so Avalonia's own grouping doesn't fight the hold | 2026-11-21 evening, #80 (PR #92) | [Radio 13, 14](controls/Radio/README.md#known-differences), [Radio's `name` row](controls/Radio/README.md) |
| Radio, RadioGroup | A radio is controlled only; controlled and stateful groups are separate | A radio on its own checks itself on a click, as an Avalonia `RadioButton` does; one group, whose `Value` a click sets whatever bound it | As built | [Radio 2](controls/Radio/README.md#known-differences) |
| Button | A loading button is as high as its spinner: the content is squashed to no height | As high as the larger of the content and the spinner | As built | [Button 2](controls/Button/README.md#known-differences) |
| Button | `round` (deprecated) is a 50% radius, an ellipse on a button that isn't square | A 999 radius | As built | [Button 6](controls/Button/README.md#known-differences) |
| Button | A `transform` override on the ring applies while it doesn't turn | An override's `RenderTransform` on the ring does nothing: the turn's local value outranks it (Avalonia 12.1.4's `TransformAnimator`) | 2026-11-21, #77 (left documented) | [Button 5](controls/Button/README.md#known-differences) |
| Button, Spinner, ProgressBar | No such state | `:metered-still` holds endless motion still for the preview pictures | 2026-11-20 (kept until #13's test clock replaces it) | [Spinner 8](controls/Spinner/README.md#known-differences), [Button 5](controls/Button/README.md#known-differences), [ProgressBar's motion](controls/ProgressBar/README.md) |
| Spinner | A `$size` that isn't a size draws a box the browser drops (as wide as its container) | No ring | As built | [Spinner 3](controls/Spinner/README.md#known-differences) |
| Slider | react-range lets two thumbs cross at a neighbour at 0 (a truthiness test) | A neighbour at 0 holds a thumb, as any other does | 2026-11-20 (PR #65) | [Slider 1](controls/Slider/README.md#known-differences) |
| Slider | Throws for invalid values | Coerces what it can, as `RangeBase` does, and throws for the rest | 2026-11-20 (PR #65) | [Slider 9](controls/Slider/README.md#known-differences) |
| Slider | A held key's drag ends only on its release, which never comes if the focus leaves first | The drag ends when the focus or the slider's use ends, with a final change | 2026-11-20 (PR #65) | [Slider 10](controls/Slider/README.md#known-differences) |
| Slider | Moves are put off to the next frame; follows `touches[0]` | Each move taken as it comes; the pointer that started the drag | As built | [Slider 6, 7](controls/Slider/README.md#known-differences) |
| ProgressBar | Clamps nothing: 150 of 100 reads "150% Complete" | The value is clamped, as Avalonia's `RangeBase` clamps it | 2026-11-20, #67 | [ProgressBar 11](controls/ProgressBar/README.md#known-differences) |
| ProgressBar | An empty range reads "NaN% Complete" over a full fill | "0% Complete" over an empty fill, Base Web's own bar at its minimum | 2026-11-21, #82 | [ProgressBar 12](controls/ProgressBar/README.md#known-differences) |
| Input, Textarea | A press on an enhancer or the padding doesn't focus the input | A press anywhere on the box focuses it, which makes the whole box a touch target | 2026-11-21 evening, #84 (PR #93) | [Input 1](controls/Input/README.md#known-differences), [Textarea 4](controls/Textarea/README.md#known-differences) |
| Input | The selection's colour is the browser's | `colors.accent` with `colors.contentOnColor` text | 2026-11-21 evening, #84 (PR #93) | [Input 2](controls/Input/README.md#known-differences) |
| Input | `isMasked` is set once, from the type at mount | Follows `Type` until the toggle is clicked | 2026-11-21 evening, #84 (PR #93) | [Input 8](controls/Input/README.md#known-differences) |
| Input | Any HTML `type`; `BaseInput` exported as a component | `Type` is Text or Password; the soft keyboard's kind is `TextInputOptions`; `BaseInput` is abstract | 2026-11-21 evening, #84 (PR #93) | [Input's props](controls/Input/README.md) |
| Input | An `<input>`'s long value may scroll sideways under a finger (for #90) | A long value doesn't scroll sideways under a finger: its text's scroller has no scroll gesture, and a finger's drag never moves the caret (#94), so a sideways drag moves nothing | As built (#94, PR #101) | [Input 11](controls/Input/README.md#known-differences) |
| Input | Escape is listened for only if the input was clearable when it mounted | Escape follows `Clearable` as it is now | As built | [Input 5](controls/Input/README.md#known-differences) |
| Textarea | A phone's browser: a finger's drag scrolls the text, a tap places the caret | The same, a pen's drag too (Avalonia's text box would move the caret by a drag, and a pen's would select); a long press selects | 2026-11-21 evening, #85 (PR #95) | [Textarea's behaviour](controls/Textarea/README.md) |
| Textarea | No grip of its own (the browser's, if any) | Metered draws a browser's grip, 16 px, in `colors.contentTertiary`, on every device | 2026-11-21 evening, #85 (PR #95) | [Textarea 2](controls/Textarea/README.md#known-differences) |
| Textarea | The browser's scroll bar | None | 2026-11-21 evening, #85 (PR #95) | [Textarea 1](controls/Textarea/README.md#known-differences) |
| Textarea | 20 columns as the browser measures them (Uber Move on baseweb.design) | Chrome's rule, 20 of the font's OS/2 average character (Instrument Sans), only where the root is `fit-content`; with no OS/2 width, 20 widths of `0` | 2026-11-21 evening, #85 (PR #95) | [Textarea 5](controls/Textarea/README.md#known-differences) |
| Textarea | A resized textarea can overflow its parent | The grip keeps it within the room it is given | As built | [Textarea 3](controls/Textarea/README.md#known-differences) |

## Forced

| What | Why | Detail |
|---|---|---|
| **Fonts:** Instrument Sans for UberMoveText, Figtree for UberMove | Uber Move can't ship | [ADR 0003](decisions/0003-font.md); the Fonts item of Button, Checkbox, ProgressBar, Radio, Slider and Switch |
| **An override restyles or hides a part; it never renders another element** (`component`, `() => null`, a custom `TickBar`); Spinner, which has none in Base Web (`withStyle`, `$style`), takes one too | An Avalonia override is a theme for the part's element | [ADR 0001](decisions/0001-parts-states-and-overrides.md); [Button 1](controls/Button/README.md#known-differences), [Slider 8](controls/Slider/README.md#known-differences), [Input 6](controls/Input/README.md#known-differences), [Spinner 1](controls/Spinner/README.md#known-differences) |
| **`@media (pointer: coarse)` is the platform** (Android and iOS) | Avalonia has no such media query | [Checkbox 3](controls/Checkbox/README.md#known-differences), [Radio 4](controls/Radio/README.md#known-differences), [Switch 4](controls/Switch/README.md#known-differences) |
| **No hidden `Input` part**: the control is the native input | There is no hidden native input to keep | [Checkbox 1](controls/Checkbox/README.md#known-differences), [Radio 1](controls/Radio/README.md#known-differences), [Switch 1](controls/Switch/README.md#known-differences) |
| **A disabled control shows no not-allowed cursor, and a disabled switch no hover overlay** | Avalonia doesn't hit-test a disabled control | [Checkbox 8](controls/Checkbox/README.md#known-differences), [Radio 11](controls/Radio/README.md#known-differences), [Switch 3](controls/Switch/README.md#known-differences), [Slider 4](controls/Slider/README.md#known-differences) |
| **A control in a label keeps its own press** (`containsInteractiveElement`) | Avalonia's controls take their own press | [Checkbox 7](controls/Checkbox/README.md#known-differences), [Radio 9](controls/Radio/README.md#known-differences) |
| **The hover area is the rectangle**, not the rounded corners | Avalonia's hit test is the bounds | [Checkbox 6](controls/Checkbox/README.md#known-differences), [Radio 8](controls/Radio/README.md#known-differences) |
| **Drawn by Metered's own elements:** overlays, outlines, checkmarks, fills, gradients, the focus ring; Button's `outline` hover `::after` as the button's own box-shadows | A `Border`'s box-shadows and brushes can't take tokens or CSS's percentages; Avalonia has no `::after` | [Radio 10](controls/Radio/README.md#known-differences), [Switch 6](controls/Switch/README.md#known-differences), [ProgressBar 3](controls/ProgressBar/README.md#known-differences), [Slider 3](controls/Slider/README.md#known-differences), [Button 4](controls/Button/README.md#known-differences) |
| **A ring's arc ends square** | A CSS border changes colour on a diagonal mitre | [Button 5](controls/Button/README.md#known-differences), [Spinner 6](controls/Spinner/README.md#known-differences) |
| **Literal colours are tokens** (the slider thumb's shadow) | CLAUDE.md rule 5 | [Slider 2](controls/Slider/README.md#known-differences) |
| **Layout is Avalonia's:** label-first panels, items panels, rounding to device pixels, no width of its own, the slider's mark margin spent, the bar's steps made in code and its fill's offset set by the bar, Button's `StartEnhancerButtonContentContainer` always in the template | Flexbox and markup order have no direct equivalent | [Radio 5 to 7](controls/Radio/README.md#known-differences), [Checkbox 4](controls/Checkbox/README.md#known-differences), [Switch 5](controls/Switch/README.md#known-differences), [ProgressBar 1, 2, 4, 8](controls/ProgressBar/README.md#known-differences), [Slider 5](controls/Slider/README.md#known-differences), [Button 3, 7](controls/Button/README.md#known-differences) |
| **Transitions start after load**, and an off switch's knob has a transform | CSS doesn't transition a first paint; Avalonia's transition needs two ends | [Input 7](controls/Input/README.md#known-differences), [Textarea 7](controls/Textarea/README.md#known-differences), [Switch 7](controls/Switch/README.md#known-differences) |
| **Accessibility is Avalonia's peers'**: no `aria-checked`, `role="switch"`, `aria-busy`; names from `AutomationProperties.Name` | No counterpart in Avalonia's automation | [ButtonGroup 2](controls/ButtonGroup/README.md#known-differences), [Switch 9](controls/Switch/README.md#known-differences), [ProgressBar 5, 6](controls/ProgressBar/README.md#known-differences), [Spinner 7](controls/Spinner/README.md#known-differences), [Slider 12](controls/Slider/README.md#known-differences) |
| **The text box's own behaviour:** the mask toggle disabled with the input, the app theme's cut/copy/paste strings, focus and keys kept from the parts inside its template | Avalonia's `TextBox` template | [Input 3, 4, 9](controls/Input/README.md#known-differences) |
| **Cursors:** no grab cursors | Avalonia has none | [Slider 4](controls/Slider/README.md#known-differences) |
| **XAML and names:** a number is a number; the names' lengths are written in the theme; Spinner's content box is read, not measured; `Transparent` is `#00FFFFFF`; Avalonia has a `Spinner` of its own | XAML and Avalonia's types | [Spinner 2, 4, 5, 9](controls/Spinner/README.md#known-differences), [Checkbox 11](controls/Checkbox/README.md#known-differences) |

## Not yet

| What | Detail |
|---|---|
| **Right to left** | [Checkbox 9](controls/Checkbox/README.md#known-differences), [Switch 8](controls/Switch/README.md#known-differences), [ProgressBar 7](controls/ProgressBar/README.md#known-differences), [Slider 11](controls/Slider/README.md#known-differences) |
| **`borders.useRoundedCorners` false** | [ProgressBar 9](controls/ProgressBar/README.md#known-differences) |
| **A group's items from `ItemsSource` with a template** | [Radio 12](controls/Radio/README.md#known-differences) |
| **On a phone:** the slider's touch drag (#64); Input's and Textarea's caret, selection handles, soft keyboard and drags, and the drag after a hold (#90) | [Slider 14](controls/Slider/README.md#known-differences), [Input 10](controls/Input/README.md#known-differences), [Textarea 6](controls/Textarea/README.md#known-differences) |
