# ButtonGroup

Base Web's `ButtonGroup` and `StatefulButtonGroup` (baseui 18.2.0, `button-group/`), as one
control, `Metered.Controls.ButtonGroup`, an Avalonia `ItemsControl` of `Button`s. See the Button's
gallery for two groups: radio, and checkbox with the outline kind.

## API

| Base Web | Metered |
|---|---|
| `mode` | `Mode` (`None`, `Radio`, `Checkbox`) |
| `selected`, `initialState.selected` | `Selected`, the selected indices, two-way |
| `kind`, `size`, `shape` | `Kind` (secondary by default), `Size`, `Shape`: they replace each button's own |
| `padding` | `PaddingType` |
| `wrap` | `Wrap` (`bool?`) |
| `disabled` | `IsEnabled` |
| `onClick(event, index)` | `Click` (`ButtonGroupClickEventArgs.Index`), after the button's own |
| `overrides` | `Overrides`: `Root` |
| `stateReducer` | not yet |

## Behaviour, as Base Web's

- A click in radio mode selects the button, or clears it if it is the one selected; in checkbox
  mode it toggles it. A group with no mode and no selection selects nothing.
- A button's own `IsSelected` of true is kept; otherwise the group sets it from `Selected`, and
  takes it back when the button leaves.
- Radio: only the selected button (or the first, with none) is a tab stop; the arrow keys move the
  selection and the focus, wrapping; the buttons are announced as radio buttons (checkbox mode: as
  check boxes).
- `Root`: a row with Base Web's gaps (8 between buttons; between rows when wrapping, 12 for the
  extra-small size and 8 otherwise), its padding, and overflow visible, unless `Wrap` is false,
  which scrolls the row sideways with no bar.

## Held by

`Controls/ButtonGroupTests`: the root against Base Web's `StyledRoot` for every size, padding and
wrap (36 cases), and each behaviour above.

## Known differences

1. Base Web's controlled and stateful groups are one: `Selected` is set by a click whatever bound it.
2. `aria-checked` has no Avalonia equivalent on a Button's automation peer; the role is set.
