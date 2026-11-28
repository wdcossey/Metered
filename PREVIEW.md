# Preview

What Metered looks like today: the sample ([`samples/Metered.Sample`](samples/Metered.Sample/SampleShell.axaml)) in
its desktop window at a phone's 390 × 844, its list and each page, light and dark, one screen per picture. The bar
across the top is the sample's own: Back, the page's title, and the Light/Dark switch, made of Metered's Buttons.
Base Web's families are drawn in Instrument Sans and Figtree, the OFL substitutes for Uber Move
([ADR 0003](docs/decisions/0003-font.md)). Each control's comparison with Base Web, what matches, what differs and
why, is in [`docs/controls/`](docs/controls/).

The pictures are drawn headless, with Skia, by
[`PreviewGallery`](tests/Metered.Tests/Sample/PreviewGallery.cs), from the sample's own window, so they are the
theme's own pixels and can be drawn again on any OS:

```sh
METERED_PREVIEW=/absolute/folder dotnet test tests/Metered.Tests --filter FullyQualifiedName~PreviewGallery
```

Unset, it does nothing; set, it must name an absolute folder that is new or empty. Its pictures replace
`docs/preview/`.

## The list

A Button for each page in the catalogue, one per control.

<p>
<img src="docs/preview/light__list-1.png" width="270" alt="The sample's list of pages, light">
<img src="docs/preview/dark__list-1.png" width="270" alt="The sample's list of pages, dark">
</p>

Scrolled to its foot:

<p>
<img src="docs/preview/light__list-2.png" width="270" alt="The sample's list of pages, light, scrolled to its foot">
<img src="docs/preview/dark__list-2.png" width="270" alt="The sample's list of pages, dark, scrolled to its foot">
</p>

## Button

The seven kinds and disabled, the four sizes, the shapes, a button loading (its ring held still for the picture; the
page shows it for two seconds after a click), the selected state, the tap area (an XSmall button that takes taps in a
48 px band), the fill width, and the start and end enhancers.

<p>
<img src="docs/preview/light__button-1.png" width="270" alt="Button, light: kinds, sizes, shapes, states (one button loading) and enhancers">
<img src="docs/preview/dark__button-1.png" width="270" alt="Button, dark: kinds, sizes, shapes, states (one button loading) and enhancers">
</p>

## ButtonGroup

A radio group, a checkbox group of small pills with what was clicked under it, and a group that wraps.

<p>
<img src="docs/preview/light__buttongroup-1.png" width="270" alt="ButtonGroup, light: radio, checkbox and wrapping">
<img src="docs/preview/dark__buttongroup-1.png" width="270" alt="ButtonGroup, dark: radio, checkbox and wrapping">
</p>

## Checkbox

Base Web's checkbox-v2: its states (checked, error, disabled), a parent indeterminate while only some of its children
are checked, the label on the left and on the right, and a label of several lines.

<p>
<img src="docs/preview/light__checkbox-1.png" width="270" alt="Checkbox, light: states, indeterminate, label placement and a long label">
<img src="docs/preview/dark__checkbox-1.png" width="270" alt="Checkbox, dark: states, indeterminate, label placement and a long label">
</p>

## Radio

Base Web's radio-v2: a group with a description and its selection under it, a horizontal group, the label on top
(the marks in a line under labels of different heights), disabled, in error, and a radio on its own.

<p>
<img src="docs/preview/light__radio-1.png" width="270" alt="Radio, light: a group with a description, horizontal, the label on top, disabled and error">
<img src="docs/preview/dark__radio-1.png" width="270" alt="Radio, dark: a group with a description, horizontal, the label on top, disabled and error">
</p>
<p>
<img src="docs/preview/light__radio-2.png" width="270" alt="Radio, light, scrolled: disabled, error and a radio on its own">
<img src="docs/preview/dark__radio-2.png" width="270" alt="Radio, dark, scrolled: disabled, error and a radio on its own">
</p>

## Switch

Base Web's switch, as its documentation's examples: basic with its state under it, a label over several lines,
disabled off and on, the label left and right, the two sizes, and the checkmark.

<p>
<img src="docs/preview/light__switch-1.png" width="270" alt="Switch, light: basic, multiple lines, disabled, alignments and sizes">
<img src="docs/preview/dark__switch-1.png" width="270" alt="Switch, dark: basic, multiple lines, disabled, alignments and sizes">
</p>
<p>
<img src="docs/preview/light__switch-2.png" width="270" alt="Switch, light, scrolled: sizes and the checkmark">
<img src="docs/preview/dark__switch-2.png" width="270" alt="Switch, dark, scrolled: sizes and the checkmark">
</p>

## ProgressBar

Base Web's progress bar, as its documentation's examples: basic, a negative fill by override, the intents with their
labels, the label and a custom one, the three sizes, five steps with their buttons, and infinite. The endless motion
stands still for the pictures at its first keyframe (`:metered-still`), where the step in progress and the infinite
bar's halves haven't come in yet, so they draw nothing here.

<p>
<img src="docs/preview/light__progressbar-1.png" width="270" alt="ProgressBar, light: basic, negative, intents, labels and sizes">
<img src="docs/preview/dark__progressbar-1.png" width="270" alt="ProgressBar, dark: basic, negative, intents, labels and sizes">
</p>
<p>
<img src="docs/preview/light__progressbar-2.png" width="270" alt="ProgressBar, light, scrolled: sizes, steps and infinite">
<img src="docs/preview/dark__progressbar-2.png" width="270" alt="ProgressBar, dark, scrolled: sizes, steps and infinite">
</p>

## Spinner

Base Web's spinner, as its documentation's examples and its scenario: basic, the three sizes, border widths and
lengths (a number with a named border, a named size with a 20 border, sizing tokens, and a length with no border,
which a browser draws at CSS's `medium`, 3), two colours, and the documentation's customised one, by an override. The
rings turn; the pictures hold them still at 0°, Base Web's `from`.

<p>
<img src="docs/preview/light__spinner-1.png" width="270" alt="Spinner, light: basic, sizes, border widths and lengths, colours">
<img src="docs/preview/dark__spinner-1.png" width="270" alt="Spinner, dark: basic, sizes, border widths and lengths, colours">
</p>
<p>
<img src="docs/preview/light__spinner-2.png" width="270" alt="Spinner, light, scrolled: colours and the customised spinner">
<img src="docs/preview/dark__spinner-2.png" width="270" alt="Spinner, dark, scrolled: colours and the customised spinner">
</p>

## Slider

Base Web's slider, as its documentation's examples: basic with its value under it, marks every ten, a range, disabled,
a custom min, max and step, a custom label, and its scenario's labels always shown.

<p>
<img src="docs/preview/light__slider-1.png" width="270" alt="Slider, light: basic, marks, range, disabled, custom min, max and step">
<img src="docs/preview/dark__slider-1.png" width="270" alt="Slider, dark: basic, marks, range, disabled, custom min, max and step">
</p>
<p>
<img src="docs/preview/light__slider-2.png" width="270" alt="Slider, light, scrolled: a custom label and labels always shown">
<img src="docs/preview/dark__slider-2.png" width="270" alt="Slider, dark, scrolled: a custom label and labels always shown">
</p>

## Input

Base Web's input, as its documentation's examples: basic with its value under it, the states, the sizes, enhancers,
clearable, a password with its mask toggle, and the overrides example's root.

<p>
<img src="docs/preview/light__input-1.png" width="270" alt="Input, light: basic, the states, the sizes">
<img src="docs/preview/dark__input-1.png" width="270" alt="Input, dark: basic, the states, the sizes">
</p>
<p>
<img src="docs/preview/light__input-2.png" width="270" alt="Input, light, scrolled: enhancers, clearable, a password, an override">
<img src="docs/preview/dark__input-2.png" width="270" alt="Input, dark, scrolled: enhancers, clearable, a password, an override">
</p>

## Textarea

Base Web's textarea, as its documentation's examples: basic with its value under it, the states, the sizes, one the user
can resize (its grip in the corner), its scenario's clearable one, and a long text that scrolls inside its three rows.

<p>
<img src="docs/preview/light__textarea-1.png" width="270" alt="Textarea, light: basic, the states, the sizes">
<img src="docs/preview/dark__textarea-1.png" width="270" alt="Textarea, dark: basic, the states, the sizes">
</p>
<p>
<img src="docs/preview/light__textarea-2.png" width="270" alt="Textarea, light, scrolled: sizes, resizable, clearable, a long text">
<img src="docs/preview/dark__textarea-2.png" width="270" alt="Textarea, dark, scrolled: sizes, resizable, clearable, a long text">
</p>

## FormControl

Base Web's form control round an input and a textarea, as its documentation's examples: basic, the validation example
and the states; then its scenarios: the boolean states, the counters, a label for another control (`HtmlFor`), the
input's form control states, and a checkbox and a radio group in one. A tap on a label focuses its control.

<p>
<img src="docs/preview/light__formcontrol-1.png" width="270" alt="FormControl, light: an input, a textarea, the validation example">
<img src="docs/preview/dark__formcontrol-1.png" width="270" alt="FormControl, dark: an input, a textarea, the validation example">
</p>
<p>
<img src="docs/preview/light__formcontrol-2.png" width="270" alt="FormControl, light, scrolled: the states and the counters">
<img src="docs/preview/dark__formcontrol-2.png" width="270" alt="FormControl, dark, scrolled: the states and the counters">
</p>
<p>
<img src="docs/preview/light__formcontrol-3.png" width="270" alt="FormControl, light, scrolled: the counters, a label for another control, the form control states">
<img src="docs/preview/dark__formcontrol-3.png" width="270" alt="FormControl, dark, scrolled: the counters, a label for another control, the form control states">
</p>
<p>
<img src="docs/preview/light__formcontrol-4.png" width="270" alt="FormControl, light, at its end: a checkbox and a radio group">
<img src="docs/preview/dark__formcontrol-4.png" width="270" alt="FormControl, dark, at its end: a checkbox and a radio group">
</p>

## Select

Base Web's select, as its documentation's examples: basic, search single and multi, creatable single and multi, its four
sizes, grouped options, a select that isn't searchable, and its states (error, positive, disabled, loading). A tap opens
a select's dropdown below it; a tap there chooses; a finger's drag over the page scrolls it and opens nothing.

<p>
<img src="docs/preview/light__select-1.png" width="270" alt="Select, light: basic, search single and multi, creatable, sizes">
<img src="docs/preview/dark__select-1.png" width="270" alt="Select, dark: basic, search single and multi, creatable, sizes">
</p>
<p>
<img src="docs/preview/light__select-2.png" width="270" alt="Select, light, scrolled: grouped, not searchable, the states">
<img src="docs/preview/dark__select-2.png" width="270" alt="Select, dark, scrolled: grouped, not searchable, the states">
</p>
<p>
<img src="docs/preview/light__select-3.png" width="270" alt="Select, light, at its end: not searchable, the states">
<img src="docs/preview/dark__select-3.png" width="270" alt="Select, dark, at its end: not searchable, the states">
</p>

## Popover

Base Web's popover, as its documentation's examples: always open, opened by a click and with an arrow, by a hover, closed
from its content, each placement, and the overrides example open. A tap on an anchor opens its popover and a tap outside
closes it; a hover popover opens under a mouse or by keyboard focus.

<p>
<img src="docs/preview/light__popover-1.png" width="270" alt="Popover, light: always open, the click, hover and dismiss examples' anchors">
<img src="docs/preview/dark__popover-1.png" width="270" alt="Popover, dark: always open, the click, hover and dismiss examples' anchors">
</p>
<p>
<img src="docs/preview/light__popover-2.png" width="270" alt="Popover, light, at its end: the placements' anchors and the overrides example, open">
<img src="docs/preview/dark__popover-2.png" width="270" alt="Popover, dark, at its end: the placements' anchors and the overrides example, open">
</p>

## Menu

Base Web's menu, as its documentation's examples: basic (250 px high, two options disabled), compact, the profile option,
a child menu, grouped, a long list, links, dividers, and the empty state. A finger's drag over a menu scrolls it; a tap
highlights an option and chooses it, and a tap on Open Recent or New Breakpoint opens its child menu beside it.

<p>
<img src="docs/preview/light__menu-1.png" width="270" alt="Menu, light: basic, compact, the profile option">
<img src="docs/preview/dark__menu-1.png" width="270" alt="Menu, dark: basic, compact, the profile option">
</p>
<p>
<img src="docs/preview/light__menu-2.png" width="270" alt="Menu, light, scrolled: the profile option, a child menu's parent, grouped">
<img src="docs/preview/dark__menu-2.png" width="270" alt="Menu, dark, scrolled: the profile option, a child menu's parent, grouped">
</p>
<p>
<img src="docs/preview/light__menu-3.png" width="270" alt="Menu, light, scrolled: grouped, the long list">
<img src="docs/preview/dark__menu-3.png" width="270" alt="Menu, dark, scrolled: grouped, the long list">
</p>
<p>
<img src="docs/preview/light__menu-4.png" width="270" alt="Menu, light, at its end: links, dividers, the empty state">
<img src="docs/preview/dark__menu-4.png" width="270" alt="Menu, dark, at its end: links, dividers, the empty state">
</p>

## Tooltip

Base Web's tooltip, as its documentation's examples and scenarios: one open from the start (with the arrow, above its
anchor), the stateful example in a line of text, complex content in the dark theme with a link, the arrow with a margin
of 20, and two on buttons. A tap on an anchor opens its tooltip and another, or a tap elsewhere, closes it; a tooltip
opens under a mouse or by keyboard focus.

<p>
<img src="docs/preview/light__tooltip-1.png" width="270" alt="Tooltip, light: one open from the start, the examples' anchors">
<img src="docs/preview/dark__tooltip-1.png" width="270" alt="Tooltip, dark: one open from the start, the examples' anchors">
</p>

## Theme

Buttons under the app's theme, then the same in a subtree given its own by `ThemeProvider`, built by
`BaseTheme.CreateTheme` and `CreateDarkTheme` from three foundation colours ([`docs/theming.md`](docs/theming.md)).

<p>
<img src="docs/preview/light__theme-1.png" width="270" alt="A subtree's own theme, light">
<img src="docs/preview/dark__theme-1.png" width="270" alt="A subtree's own theme, dark">
</p>

## Not shown

The pages at rest only. Hover, press and the keyboard's focus ring are in the Button's frames
([`docs/controls/Button/`](docs/controls/Button/)); the Button's loading spinner and the Spinner's rings turn (held still at 0° for the pictures by `:metered-still`; no page shows a Button loading at rest), and the background's 200 ms
transition moves, which still pictures can't show. The shell's Debug footer (what a page cost to open,
[`docs/performance/README.md`](docs/performance/README.md)) is turned off for the pictures, as its times change on
every run. The scroll bar is Avalonia's Simple theme, which the sample puts under Metered's for the controls Metered
doesn't theme yet.
